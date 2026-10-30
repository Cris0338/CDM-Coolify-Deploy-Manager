// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace CoolifyDeployManager;

/// <summary>
/// Usa il client OpenSSH di Windows già configurato dall'utente, senza mostrare una console.
/// Non disabilita la verifica delle host key e non trasferisce token su un IP remoto in HTTP.
/// </summary>
internal sealed class SshTunnel : IAsyncDisposable
{
    private Process? process;
    private string? configSignature;
    public bool IsRunning => process is { HasExited: false };
    public string LocalUrl { get; private set; } = "";
    public event Action<string>? StatusChanged;

    public async Task EnsureRunningAsync(UserSettings settings, CancellationToken ct)
    {
        if (settings.ConnectionMode != ConnectionMode.ManagedSshTunnel) return;
        Validate(settings);
        var signature = $"{settings.SshHost}|{settings.SshLocalPort}|{settings.SshRemoteHost}|{settings.SshRemotePort}";
        if (IsRunning && signature == configSignature)
        {
            if (await HealthCheckAsync(settings.SshLocalPort, ct)) return;
            throw new IOException("Tunnel SSH attivo ma API Coolify non raggiungibile. Controllare SSH e il servizio remoto.");
        }
        Stop();
        if (!IsPortFree(settings.SshLocalPort))
            throw new IOException($"La porta locale {settings.SshLocalPort} è già occupata. Chiudi il vecchio tunnel SSH oppure scegli un'altra porta.");
        var start = new ProcessStartInfo("ssh.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
        };
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
            process = Process.Start(start) ?? throw new IOException("Impossibile avviare ssh.exe.");
            var ownedProcess = process;
            _ = Task.Run(async () =>
            {
                try
                {
                    string errors = await ownedProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(errors)) StatusChanged?.Invoke("SSH: " + errors.Trim());
                }
                catch (Exception) { /* Il processo può terminare durante la chiusura. */ }
            });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new IOException("OpenSSH Client (ssh.exe) non è disponibile in Windows. Verifica la funzionalità opzionale OpenSSH Client.", ex);
        }
        configSignature = signature;
        LocalUrl = $"http://127.0.0.1:{settings.SshLocalPort}";
        StatusChanged?.Invoke("Avvio tunnel SSH verso " + settings.SshHost + "...");
        try
        {
            for (int attempt = 0; attempt < 25; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                if (!IsRunning) throw new IOException("Il tunnel SSH si è chiuso. Controlla la chiave SSH, known_hosts e l'alias configurato.");
                if (await HealthCheckAsync(settings.SshLocalPort, ct))
                {
                    StatusChanged?.Invoke("Tunnel SSH verificato: " + LocalUrl);
                    return;
                }
                await Task.Delay(350, ct);
            }
        }
        catch { Stop(); throw; }
        Stop();
        throw new TimeoutException("Tunnel SSH aperto, ma /api/health non risponde. Verifica porta remota e server Coolify.");
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
            throw new ArgumentException("Alias SSH non valido.");
        if (s.SshLocalPort is < 1024 or > 65535 || s.SshRemotePort is < 1 or > 65535)
            throw new ArgumentException("Porta SSH locale o remota non valida.");
        if (string.IsNullOrWhiteSpace(s.SshRemoteHost) || s.SshRemoteHost.StartsWith('-') || s.SshRemoteHost.Any(char.IsWhiteSpace))
            throw new ArgumentException("Host remoto SSH non valido.");
    }
    public void Stop()
    {
        var p = process;
        process = null;
        configSignature = null;
        LocalUrl = "";
        if (p is null) return;
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally { p.Dispose(); }
    }
    public ValueTask DisposeAsync() { Stop(); return ValueTask.CompletedTask; }
}
