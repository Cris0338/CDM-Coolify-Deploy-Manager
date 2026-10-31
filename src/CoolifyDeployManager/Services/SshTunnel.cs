// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Principal;

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
            try
            {
                File.WriteAllText(identity, settings.SshPrivateKey);
                // Windows OpenSSH rejects inherited read access by extra local groups.
                // Restrict the temporary private key to the current Windows SID
                // before ssh.exe can read it (no admin rights required).
                await ProtectPrivateKeyAsync(identity, ct);
                File.WriteAllText(knownHosts, settings.SshKnownHosts + Environment.NewLine);
            }
            catch
            {
                Stop();
                throw;
            }
        }
        var start = new ProcessStartInfo("ssh.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
        };
        // Do not inherit the computer owner's aliases or global SSH defaults.
        // The personalized EXE carries everything needed for this connection.
        start.ArgumentList.Add("-F");
        start.ArgumentList.Add("none");
        if (identity is not null)
        {
            foreach (var arg in new[] { "-i", identity,
                "-o", "IdentitiesOnly=yes",
                "-o", $"UserKnownHostsFile={knownHosts}",
                "-o", "GlobalKnownHostsFile=none",
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
        // Keep the stderr reader so that an early SSH failure can be shown in
        // the wizard instead of the unhelpful "tunnel exited" message.
        Task<string>? sshErrors = null;
        try
        {
            process = Process.Start(start) ?? throw new IOException("Unable to start ssh.exe.");
            sshErrors = process.StandardError.ReadToEndAsync();
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
                if (!IsRunning)
                {
                    var exitCode = process?.ExitCode;
                    string detail;
                    try { detail = sshErrors is null ? "" : (await sshErrors).Trim(); }
                    catch (IOException) { detail = "Unable to read SSH error output."; }
                    throw new IOException(
                        $"SSH tunnel exited (ssh.exe code {exitCode})." +
                        (detail.Length > 0 ? "\n\nOpenSSH: " + detail : "\nNo diagnostic output from OpenSSH."));
                }
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
    private static async Task ProtectPrivateKeyAsync(string identity, CancellationToken ct)
    {
        // icacls ships with Windows 10/11; using the numeric SID avoids
        // depending on the operating system's display language or group names.
        var sid = WindowsIdentity.GetCurrent().User?.Value
            ?? throw new IOException("Cannot determine the current Windows account SID.");
        var start = new ProcessStartInfo("icacls.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // Remove all inherited ACEs (including groups such as CodexSandboxUsers).
        // Grant the actual user explicit full control over the private key.
        foreach (var arg in new[] { identity, "/inheritance:r", "/grant:r", "*" + sid + ":(F)" })
            start.ArgumentList.Add(arg);
        try
        {
            using var acl = Process.Start(start)
                ?? throw new IOException("Cannot start icacls.exe to protect the SSH private key.");
            var output = acl.StandardOutput.ReadToEndAsync();
            var errors = acl.StandardError.ReadToEndAsync();
            await acl.WaitForExitAsync(ct);
            var detail = (await errors).Trim();
            if (acl.ExitCode != 0)
                throw new IOException("Cannot restrict SSH private-key permissions (icacls exit " +
                    acl.ExitCode + "): " + detail + " " + (await output).Trim());
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new IOException("Windows icacls.exe is unavailable; cannot protect the SSH private key.", ex);
        }
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
