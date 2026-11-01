// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Autostart is a per-Windows-user choice, never part of the portable secret profile.
// The registry contains ONLY a quoted executable path and the startup-to-tray flag.
using Microsoft.Win32;
using System.IO;

namespace CoolifyDeployManager;

internal static class WindowsStartup
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "CDM-Coolify-Deploy-Manager";
    internal const string TrayArgument = "--startup-tray";

    internal static string CommandFor(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.Contains('"') ||
            !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Expected an absolute executable path.", nameof(executable));
        return "\"" + Path.GetFullPath(executable) + "\" " + TrayArgument;
    }

    internal static bool IsRegisteredFor(string executable)
    {
        using var run = Registry.CurrentUser.OpenSubKey(RunPath);
        var value = run?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            as string;
        return string.Equals(value, CommandFor(executable), StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsRegisteredForCurrentClone() =>
        SettingsStore.HasPortableProfile &&
        IsRegisteredFor(ExecutableProfile.OwnPath);

    internal static void SetRegistered(string executable, bool enabled)
    {
        var command = CommandFor(executable);
        if (enabled)
        {
            if (!File.Exists(executable))
                throw new FileNotFoundException("The personalized CDM executable was not found.", executable);
            using var run = Registry.CurrentUser.CreateSubKey(RunPath, writable: true)
                ?? throw new IOException("Cannot open the Windows user startup registry key.");
            run.SetValue(ValueName, command, RegistryValueKind.String);
        }
        else
        {
            // A separate CDM clone may own the single user autostart slot.
            // Never silently unregister another clone.
            using var run = Registry.CurrentUser.OpenSubKey(RunPath, writable: true);
            if (string.Equals(
                run?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string,
                command, StringComparison.OrdinalIgnoreCase))
                run!.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
