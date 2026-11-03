// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// A versioned filename is informational. Embedded assembly metadata remains
// authoritative; the name never grants a profile, token or executable identity.
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CoolifyDeployManager;

internal static class VersionedExecutableName
{
    private static readonly Regex PreviousVersionSuffix = new(
        @"-v\d+\.\d+\.\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static string Generic(string version) => $"CDM-v{Validate(version)}.exe";

    internal static string ForClone(string existingPath, string version)
    {
        var absolute = Path.GetFullPath(existingPath);
        if (!absolute.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Expected a personalized executable.", nameof(existingPath));
        var stem = Path.GetFileNameWithoutExtension(absolute);
        // The same clone is upgraded by replacing its old version suffix,
        // not by adding another version on every successive update.
        stem = PreviousVersionSuffix.Replace(stem, "");
        return Path.Combine(Path.GetDirectoryName(absolute)!,
            stem + "-v" + Validate(version) + ".exe");
    }

    private static string Validate(string version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            !Regex.IsMatch(version, @"^\d+\.\d+\.\d+$",
                RegexOptions.CultureInvariant))
            throw new ArgumentException("Expected a semantic version X.Y.Z.", nameof(version));
        return version;
    }

    internal static void VerifyForSmoke()
    {
        const string folder = @"C:\CDM";
        if (Generic("2.2.3") != "CDM-v2.2.3.exe" ||
            ForClone(Path.Combine(folder, "CDM-demo-user-demo-server.exe"), "2.2.3") !=
                Path.Combine(folder, "CDM-demo-user-demo-server-v2.2.3.exe") ||
            ForClone(Path.Combine(folder, "CDM-demo-user-demo-server-v2.2.2.exe"), "2.2.3") !=
                Path.Combine(folder, "CDM-demo-user-demo-server-v2.2.3.exe") ||
            ForClone(Path.Combine(folder, "CDM-demo-user-demo-server-v2.2.3.exe"), "2.2.3") !=
                Path.Combine(folder, "CDM-demo-user-demo-server-v2.2.3.exe"))
            throw new InvalidOperationException("Versioned CDM filenames are incorrect.");
    }
}
