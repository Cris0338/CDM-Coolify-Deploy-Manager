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

    // Save-to-self is a two-phase operation: the live clone writes a complete,
    // checksum-protected .pending EXE; this detached helper waits for that clone
    // to exit and replaces it in the SAME directory. The helper retries only
    // transient Windows file-lock errors; it never deletes an unapplied pending
    // profile or passes credentials as command-line arguments.
    public static void ScheduleReplaceOnExit(string pendingPath)
    {
        var target = OwnPath;
        var absolutePending = Path.GetFullPath(pendingPath);
        if (string.Equals(target, absolutePending, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetDirectoryName(target),
                Path.GetDirectoryName(absolutePending), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The staged clone must be beside the running clone.");

        // Fail before the caller marks the helper as scheduled if the staged
        // EXE does not contain a valid, configured, intact embedded profile.
        if (!TryReadFile(absolutePending, out var saved) || saved?.IsConfigured != true)
            throw new InvalidDataException("The staged clone does not contain valid settings.");

        static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
        var updaterError = absolutePending + ".updater-error.txt";
        // A single, bounded save helper. A previous failed save remains
        // recoverable, and the next attempt clears its diagnostic on success.
        var script = "$ErrorActionPreference='Stop'; " +
            $"$pending={Quote(absolutePending)}; $target={Quote(target)}; $errorFile={Quote(updaterError)}; " +
            $"Wait-Process -Id {Environment.ProcessId} -ErrorAction SilentlyContinue; " +
            "$last=$null; for($attempt=0; $attempt -lt 160; $attempt++) { " +
            "try { [System.IO.File]::Replace($pending,$target,$null); " +
            "Remove-Item -LiteralPath $errorFile -Force -ErrorAction SilentlyContinue; exit 0 } " +
            "catch [System.IO.IOException] { $last=$_.Exception } " +
            "catch [System.UnauthorizedAccessException] { $last=$_.Exception } " +
            "Start-Sleep -Milliseconds 300 }; " +
            "try { [System.IO.File]::WriteAllText($errorFile, " +
            "'CDM could not finish the profile save after retrying Windows file locks: ' + $last.Message) } " +
            "catch { }; exit 1";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        foreach (var argument in new[]
        {
            "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-WindowStyle", "Hidden", "-EncodedCommand", encoded
        }) start.ArgumentList.Add(argument);

        using var process = Process.Start(start)
            ?? throw new IOException("Unable to launch the detached profile-save helper.");
    }

}
