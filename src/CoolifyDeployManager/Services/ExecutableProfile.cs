// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// The executable is the portable profile container. The template contains no secrets.
// Trailer: [UTF8 settings JSON][SHA256 hash (32)][payload length Int64 LE][CDMPF01! (8)].
// Data is tamper-detectable, NOT secret. Anyone who has the EXE can extract its token/SSH key.
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

namespace CoolifyDeployManager;

internal static class ExecutableProfile
{
    private static readonly byte[] Magic = "CDMPF01!"u8.ToArray();
    private const int FooterLength = 48;
    private const int MaxPayload = 1024 * 1024;

    public static string OwnPath => Environment.ProcessPath
        ?? throw new InvalidOperationException("Cannot locate the running executable.");

    public static bool TryRead(out UserSettings? settings) => TryReadFile(OwnPath, out settings);

    internal static bool TryReadFile(string path, out UserSettings? settings)
    {
        settings = null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var offset = FindPayload(stream, out int length);
        if (offset < 0) return false;
        var bytes = new byte[length];
        stream.Position = offset;
        stream.ReadExactly(bytes);
        var sha = new byte[32];
        stream.ReadExactly(sha);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), sha))
            throw new InvalidDataException("The embedded CDM profile checksum is invalid.");
        settings = JsonSerializer.Deserialize<UserSettings>(bytes)
            ?? throw new InvalidDataException("The embedded CDM profile is empty.");
        return true;
    }

    private static long FindPayload(FileStream stream, out int length)
    {
        length = 0;
        if (stream.Length < FooterLength) return -1;
        Span<byte> tail = stackalloc byte[16];
        stream.Position = stream.Length - tail.Length;
        stream.ReadExactly(tail);
        if (!tail[8..].SequenceEqual(Magic)) return -1;
        var size = BinaryPrimitives.ReadInt64LittleEndian(tail[..8]);
        if (size < 2 || size > MaxPayload || size > stream.Length - FooterLength)
            throw new InvalidDataException("Invalid embedded CDM profile length.");
        length = (int)size;
        return stream.Length - FooterLength - size;
    }

    public static void Build(string sourceExe, string destination, UserSettings settings)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(settings);
        if (payload.Length > MaxPayload)
            throw new InvalidDataException("CDM profile is too large.");
        using var input = new FileStream(sourceExe, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var existing = FindPayload(input, out _);
        var templateLength = existing >= 0 ? existing : input.Length;
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                input.Position = 0;
                var buffer = new byte[128 * 1024];
                var remaining = templateLength;
                while (remaining > 0)
                {
                    int amount = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (amount == 0) throw new EndOfStreamException("Template executable is incomplete.");
                    output.Write(buffer, 0, amount);
                    remaining -= amount;
                }
                output.Write(payload);
                output.Write(SHA256.HashData(payload));
                Span<byte> footer = stackalloc byte[16];
                BinaryPrimitives.WriteInt64LittleEndian(footer[..8], payload.Length);
                Magic.CopyTo(footer[8..]);
                output.Write(footer);
                output.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static string NextClonePath(string serverName)
    {
        static string Safe(string input)
        {
            var s = new string((input ?? "").Select(c =>
                char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray()).Trim('-');
            return s.Length > 45 ? s[..45] : s.Length == 0 ? "coolify" : s;
        }
        var folder = Path.GetDirectoryName(OwnPath)!;
        var baseName = $"CDM-{Safe(Environment.UserName)}-{Safe(serverName)}";
        for (int number = 1; number < 1000; number++)
        {
            var path = Path.Combine(folder, baseName + (number == 1 ? "" : $"-{number}") + ".exe");
            if (!File.Exists(path)) return path;
        }
        throw new IOException("Too many CDM clones in the same directory.");
    }

    public static void Launch(string executable)
    {
        _ = Process.Start(new ProcessStartInfo(executable) {
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = true
        }) ?? throw new IOException("Cannot launch the personalized CDM executable.");
    }

    // Save-to-self is a two-phase operation. The live clone writes a verified
    // .pending EXE; a separate PowerShell process applies it after Windows
    // releases the original image. It retries in a bounded loop instead of
    // waiting indefinitely for a PID that may never signal. No credential
    // content is included in subprocess arguments or diagnostics.
    public static void ScheduleReplaceOnExit(string pendingPath)
    {
        var target = OwnPath;
        var pending = Path.GetFullPath(pendingPath);
        if (string.Equals(target, pending, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetDirectoryName(target), Path.GetDirectoryName(pending),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The staged clone must be beside the running clone.");
        if (!TryReadFile(pending, out var saved) || saved?.IsConfigured != true)
            throw new InvalidDataException("The staged clone does not contain valid settings.");

        static string PsLiteral(string value) => "'" + value.Replace("'", "''") + "'";
        var errorPath = pending + ".updater-error.txt";
        var statePath = pending + ".updater-started.txt";
        // Status marker proves that the helper actually began executing.
        // The helper deletes it when replacement completes successfully.
        // If a retry limit expires, only a generic error is recorded.
        var script = "$ErrorActionPreference='Stop'; " +
            $"$pending={PsLiteral(pending)}; $target={PsLiteral(target)}; " +
            $"$errorPath={PsLiteral(errorPath)}; $statePath={PsLiteral(statePath)}; " +
            "try { [IO.File]::WriteAllText($statePath,'started'); " +
            "$last=$null; for($attempt=0; $attempt -lt 200; $attempt++) { " +
            "try { [IO.File]::Replace($pending,$target,$null); " +
            "Remove-Item -LiteralPath $statePath,$errorPath -Force -ErrorAction SilentlyContinue; exit 0 } " +
            "catch { if ($_.Exception -isnot [IO.IOException] -and " +
            "$_.Exception -isnot [UnauthorizedAccessException]) { throw }; " +
            "$last=$_.Exception }; Start-Sleep -Milliseconds 250 }; " +
            "throw [IO.IOException]::new('Replacement still blocked after bounded retries: ' + " +
            "$last.GetType().Name) } " +
            "catch { try { [IO.File]::WriteAllText($errorPath, " +
            "'CDM update helper failed: ' + $_.Exception.GetType().Name + " +
            "' - ' + $_.Exception.Message) } catch { }; exit 1 }";
        // Windows PowerShell's -EncodedCommand avoids argument-boundary and
        // quoting differences for paths containing spaces or apostrophes.
        // This contains only local paths and code, never credential values.
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        foreach (var argument in new[]
        {
            "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-WindowStyle", "Hidden", "-EncodedCommand", encodedCommand
        }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
            ?? throw new IOException("Unable to launch the detached profile-save helper.");
    }

}
