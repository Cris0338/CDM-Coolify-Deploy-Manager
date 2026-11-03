// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoolifyDeployManager;

/// <summary>
/// One-time bootstrap for a NEW, restricted SSH account. Never imports an old CDM key.
/// Administrator aliases and credentials are never saved in the personalized clone.
/// </summary>
internal static class SshBootstrap
{
    private static readonly Regex AccountName = new("^[a-z_][a-z0-9_-]{0,23}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PublicKeyData = new("^[A-Za-z0-9+/]+={0,2}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static string RestrictedUser(string administrator)
    {
        var name = administrator.Trim();
        if (!AccountName.IsMatch(name) || name.EndsWith("-cdm", StringComparison.Ordinal))
            throw new ArgumentException("Enter a valid original VPS account (not a CDM service account).");
        return name + "-cdm";
    }

    internal static string BuildScript(string administrator, string publicKey, string remoteHost, int remotePort)
    {
        var target = RestrictedUser(administrator);
        if (remoteHost != "127.0.0.1" || remotePort is < 1 or > 65535)
            throw new ArgumentException("CDM 3 accepts only a verified loopback Coolify destination.");
        var parts = publicKey.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || parts[0] != "ssh-ed25519" ||
            !PublicKeyData.IsMatch(parts[1]))
            throw new ArgumentException("Generate a valid ED25519 key before provisioning.");
        try { _ = Convert.FromBase64String(parts[1]); }
        catch (FormatException) { throw new ArgumentException("Invalid ED25519 public key."); }
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "CoolifyDeployManager.SshBootstrap.sh")
            ?? throw new IOException("CDM v3 bootstrap script missing from executable.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var template = reader.ReadToEnd();
        return template.Replace("__CDM_USER__", target, StringComparison.Ordinal)
            .Replace("__CDM_PUBLIC_KEY__", parts[0] + " " + parts[1], StringComparison.Ordinal)
            .Replace("__CDM_PORT__", remotePort.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    internal static void VerifyForSmoke()
    {
        var script = BuildScript("demo", "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIGZha2V0ZXN0a2V5bWF0ZXJpYWwxMjM0NTY3ODkwAA==",
            "127.0.0.1", 8000);
        if ((!script.Contains("new_user='demo-cdm'", StringComparison.Ordinal) ||
             !script.Contains("Match User $new_user", StringComparison.Ordinal)) ||
            !script.Contains("MaxSessions 0", StringComparison.Ordinal) ||
            !script.Contains("AllowTcpForwarding local", StringComparison.Ordinal) ||
            !script.Contains("PermitOpen $destination", StringComparison.Ordinal) ||
            !script.Contains("PasswordAuthentication no", StringComparison.Ordinal) ||
            !script.Contains("restrict,port-forwarding,permitopen=", StringComparison.Ordinal) ||
            script.Contains("__CDM_", StringComparison.Ordinal) ||
            !ManualCommand(script).Contains("sudo bash <<'CDM_V3_PROVISION'", StringComparison.Ordinal))
            throw new IOException("CDM v3 SSH bootstrap policy/resource regression.");
        foreach (var user in new[] { "root;exit", "demo-cdm", "../../root", "" })
        {
            try { _ = RestrictedUser(user); throw new IOException("Unsafe SSH admin username accepted."); }
            catch (ArgumentException) { }
        }
        try
        {
            _ = BuildScript("demo", "ssh-ed25519 AAAA",
                "example.com", 8000);
            throw new IOException("Remote non-loopback forwarding destination was accepted.");
        }
        catch (ArgumentException) { }
    }

    internal static string ManualCommand(string script) =>
        "sudo bash <<'CDM_V3_PROVISION'\n" + script.TrimEnd('\n') +
        "\nCDM_V3_PROVISION";

    internal static async Task VerifyRemoteShellBlockedAsync(UserSettings connection, CancellationToken ct)
    {
        // The tunnel must have been successfully authenticated before this test.
        var folder = Directory.CreateTempSubdirectory("cdm-v3-verify-").FullName;
        try
        {
            var identity = Path.Combine(folder, "identity");
            var hosts = Path.Combine(folder, "known_hosts");
            await File.WriteAllTextAsync(identity, connection.SshPrivateKey, ct);
            await SshTunnel.ProtectPrivateKeyAsync(identity, ct);
            await File.WriteAllTextAsync(hosts, connection.SshKnownHosts + "\n", ct);
            var info = new ProcessStartInfo("ssh.exe")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var arg in new[] { "-F", "none", "-T", "-i", identity,
                "-o", "IdentitiesOnly=yes", "-o", "BatchMode=yes",
                "-o", "StrictHostKeyChecking=yes", "-o", "ConnectTimeout=8",
                "-o", "UserKnownHostsFile=" + hosts, "-o", "GlobalKnownHostsFile=none",
                "-p", connection.SshPort.ToString(), "-l", connection.SshUsername,
                connection.SshHost, "printf CDM_UNEXPECTED_SHELL" })
                info.ArgumentList.Add(arg);
            using var p = Process.Start(info) ?? throw new IOException("Cannot launch ssh.exe.");
            var stdout = p.StandardOutput.ReadToEndAsync(ct);
            var stderr = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            var output = await stdout;
            _ = await stderr; // Never display remote diagnostic text containing host policy.
            if (p.ExitCode == 0 || output.Contains("CDM_UNEXPECTED_SHELL", StringComparison.Ordinal))
                throw new IOException("CDM account can execute a remote command: setup rejected.");
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
