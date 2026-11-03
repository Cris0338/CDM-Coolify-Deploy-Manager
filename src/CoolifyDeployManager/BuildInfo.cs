// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// This metadata is part of the compiled program, not the appended personal
// profile. A clone therefore retains the exact same source/build identity
// as the generic CDM.exe from which it was created or upgraded.
using System.Reflection;

namespace CoolifyDeployManager;

internal static class BuildInfo
{
    private static readonly Assembly SourceAssembly = typeof(BuildInfo).Assembly;

    public static string Version
    {
        get
        {
            var version = SourceAssembly.GetName().Version;
            return version is { Build: >= 0 }
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "unknown";
        }
    }

    private static string Metadata(string key) =>
        SourceAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?.Value ?? "unspecified";

    public static string Commit => Metadata("CDM.SourceCommit");
    public static string BuildUtc => Metadata("CDM.BuildUtc");

    public static string DiagnosticIdentity =>
        $"CDM {Version} | Git {Commit} | Built {BuildUtc}";
}
