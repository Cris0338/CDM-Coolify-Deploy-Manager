// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace CoolifyDeployManager;

/// <summary>
/// Uses the user's configured Windows OpenSSH client without showing a console.
/// Enforces SSH host key checks and does not send tokens over public HTTP.
/// </summary>
internal sealed class SshTunnel : IAsyncDisposable
{
    private Process? process;
    private string? ephemeralIdentityFolder;
    private string? configSignature;
    public bool IsRunning => process is { HasExited: false };
    public string LocalUrl { get; private set; } = "";
    public event Action<string>? StatusChanged;

    public async Task EnsureRunningAsync(UserSettings settings, CancellationToken ct)
    {
        if (settings.ConnectionMode != ConnectionMode.ManagedSshTunnel) return;
        Validate(settings);
        var signature = $"{settings.SshHost}|{settings.SshUsername}|{settings.SshPort}|{settings.SshLocalPort}|{settings.SshRemoteHost}|{settings.SshRemotePort}";
        if (IsRunning && signature == configSignature)
        {
            if (await HealthCheckAsync(settings.SshLocalPort, ct)) return;
            throw new IOException("SSH tunnel is active, but the Coolify API is unreachable. Check SSH and the remote service.");
        }
        Stop();
        if (!IsPortFree(settings.SshLocalPort))
            throw new IOException($"Local port {settings.SshLocalPort} is already in use. Close the old SSH tunnel or choose another port.");
        // SSH requires temporary files; long-lived credentials remain inside the clone EXE.
        string? identity = null;
        string? knownHosts = null;
        if (!string.IsNullOrWhiteSpace(settings.SshPrivateKey))
        {
            if (string.IsNullOrWhiteSpace(settings.SshKnownHosts) ||
                string.IsNullOrWhiteSpace(settings.SshUsername))
                throw new IOException("Portable SSH needs a username and verified host key.");
            ephemeralIdentityFolder = Directory.CreateTempSubdirectory("cdm-ssh-").FullName;
            identity = Path.Combine(ephemeralIdentityFolder, "identity");
            knownHosts = Path.Combine(ephemeralIdentityFolder, "known_hosts");
            File.WriteAllText(identity, settings.SshPrivateKey);
            File.WriteAllText(knownHosts, settings.SshKnownHosts + Environment.NewLine);
        }
        var start = new ProcessStartInfo("ssh.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
        };
        if (identity is not null)
        {
            foreach (var arg in new[] { "-i", identity,
                "-o", "IdentitiesOnly=yes",
                "-o", $"UserKnownHostsFile={knownHosts}",
                "-p", settings.SshPort.ToString(),
                "-l", settings.SshUsername })
                start.ArgumentList.Add(arg);
        }
        foreach (var arg in new[]
        {
            "-N", "-T", "-o", "BatchMode=yes", "-o", "ExitOnForwardFailure=yes",
            "-o", "StrictHostKeyChecking=yes", "-o", "ServerAliveInterval=30",
            "-o", "ServerAliveCountMax=3", "-o", "ConnectTimeout=8",
            "-L", $"127.0.0.1:{settings.SshLocalPort}:{settings.SshRemoteHost}:{settings.SshRemotePort}",
            settings.SshHost
        }) start.ArgumentList.Add(arg);
        try
        {
            process = Process.Start(start) ?? throw new IOException("Unable to start ssh.exe.");
            var ownedProcess = process;
            _ = Task.Run(async () =>
            {
                try
                {
                    string errors = await ownedProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(errors)) StatusChanged?.Invoke("SSH: " + errors.Trim());
                }
                catch (Exception) { /* The process may exit while being disposed. */ }
            });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Stop();
            throw new IOException("OpenSSH Client (ssh.exe) is unavailable. Enable Windows OpenSSH Client.", ex);
        }
        configSignature = signature;
        LocalUrl = $"http://127.0.0.1:{settings.SshLocalPort}";
        StatusChanged?.Invoke("Starting SSH tunnel to " + settings.SshHost + "...");
        try
        {
            for (int attempt = 0; attempt < 25; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                if (!IsRunning) throw new IOException("The SSH tunnel exited. Verify the SSH key, known_hosts and configured host alias.");
                if (await HealthCheckAsync(settings.SshLocalPort, ct))
                {
                    StatusChanged?.Invoke("SSH tunnel verified: " + LocalUrl);
                    return;
                }
                await Task.Delay(350, ct);
            }
        }
        catch { Stop(); throw; }
        Stop();
        throw new TimeoutException("SSH tunnel opened, but /api/health is not responding. Verify the remote port and Coolify service.");
    }
    public static async Task<bool> HealthCheckAsync(int port, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var response = await http.GetAsync($"http://127.0.0.1:{port}/api/health", ct);
            return response.IsSuccessStatusCode && (await response.Content.ReadAsStringAsync(ct)).Trim().Equals("OK", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException) { return false; }
    }
    private static bool IsPortFree(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        try { listener.Start(); return true; }
        catch (SocketException) { return false; }
        finally { listener.Stop(); }
    }
    private static void Validate(UserSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.SshHost) || s.SshHost.StartsWith('-') || s.SshHost.Any(char.IsWhiteSpace))
            throw new ArgumentException("Invalid SSH host alias.");
        if (s.SshLocalPort is < 1024 or > 65535 || s.SshRemotePort is < 1 or > 65535 || s.SshPort is < 1 or > 65535)
            throw new ArgumentException("Invalid local or remote SSH port.");
        if (string.IsNullOrWhiteSpace(s.SshRemoteHost) || s.SshRemoteHost.StartsWith('-') || s.SshRemoteHost.Any(char.IsWhiteSpace))
            throw new ArgumentException("Invalid remote SSH host.");
    }
    public void Stop()
    {
        var p = process;
        process = null;
        configSignature = null;
        LocalUrl = "";
        if (p is not null)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            finally { p.Dispose(); }
        }
        if (ephemeralIdentityFolder is { } folder)
        {
            ephemeralIdentityFolder = null;
            try { Directory.Delete(folder, recursive: true); }
            catch (IOException) { /* Windows may briefly retain a file handle. */ }
            catch (UnauthorizedAccessException) { }
        }
    }
    public ValueTask DisposeAsync() { Stop(); return ValueTask.CompletedTask; }
}
