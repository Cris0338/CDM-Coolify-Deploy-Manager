// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Windows autostart is per-user/per-machine registry state, never embedded in the EXE.
// The registry manager reads only startup values, not executable files or SSH credentials.
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;

namespace CoolifyDeployManager;

internal sealed record StartupRegistration(
    RegistryHive Hive, RegistryView View, string SubKeyPath,
    string ValueName, RegistryValueKind Kind, string Command, string? ExecutablePath,
    bool IsCurrentClone)
{
    internal string Location => (Hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM") +
        "\\" + SubKeyPath + (Hive == RegistryHive.LocalMachine
        ? (View == RegistryView.Registry32 ? " (32-bit)" : " (64-bit)") : "");
}

internal sealed record StartupRegistryScan(
    IReadOnlyList<StartupRegistration> Entries, IReadOnlyList<string> Warnings);

internal sealed record StartupRemovalOutcome(int Removed, int Changed, IReadOnlyList<string> Errors);

internal static class WindowsStartup
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOncePath = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
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
            // Only unregister the clone that owns the slot; never silently unregister another.
            using var run = Registry.CurrentUser.OpenSubKey(RunPath, writable: true);
            if (string.Equals(
                run?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string,
                command, StringComparison.OrdinalIgnoreCase))
                run!.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    // Retarget the single standard HKCU CDM autostart value only when its
    // current command exactly belongs to the original personal executable.
    // Used by 3.0.0 -> 3.0.2 clone refresh after creating the new EXE.
    // No change to RunOnce, HKLM, or unrelated user Run registrations.
    internal static bool RetargetIfRegistered(string oldExecutable, string newExecutable)
    {
        var oldCommand = CommandFor(oldExecutable);
        var newCommand = CommandFor(newExecutable);
        if (!File.Exists(newExecutable))
            throw new FileNotFoundException("The updated CDM executable was not found.", newExecutable);
        using var run = Registry.CurrentUser.OpenSubKey(RunPath, writable: true);
        var current = run?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            as string;
        if (!string.Equals(current, oldCommand, StringComparison.OrdinalIgnoreCase) ||
            run!.GetValueKind(ValueName) != RegistryValueKind.String)
            return false;
        run.SetValue(ValueName, newCommand, RegistryValueKind.String);
        return true;
    }

    // Only the common Windows registry startup locations. Do not inspect EXEs on disk.
    private static readonly (RegistryHive Hive, RegistryView View, string Path)[] Locations =
    {
        (RegistryHive.CurrentUser, RegistryView.Default, RunPath),
        (RegistryHive.CurrentUser, RegistryView.Default, RunOncePath),
        (RegistryHive.LocalMachine, RegistryView.Registry64, RunPath),
        (RegistryHive.LocalMachine, RegistryView.Registry64, RunOncePath),
        (RegistryHive.LocalMachine, RegistryView.Registry32, RunPath),
        (RegistryHive.LocalMachine, RegistryView.Registry32, RunOncePath)
    };

    internal static StartupRegistryScan ScanRegistrations(string currentExe)
    {
        var entries = new List<StartupRegistration>();
        var warnings = new List<string>();
        foreach (var location in Locations)
        {
            string area = (location.Hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM") +
                "\\" + location.Path;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(location.Hive, location.View);
                using var key = baseKey.OpenSubKey(location.Path, writable: false);
                if (key is null) continue;
                foreach (string name in key.GetValueNames())
                {
                    try
                    {
                        RegistryValueKind kind = key.GetValueKind(name);
                        if (kind != RegistryValueKind.String && kind != RegistryValueKind.ExpandString)
                            continue;
                        if (key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
                            is not string command)
                            continue;
                        if (!IsCdmRegistration(name, command)) continue;
                        var executable = ExtractExecutablePath(command);
                        entries.Add(new StartupRegistration(location.Hive, location.View,
                            location.Path, name, kind, command, executable,
                            IsSameExecutable(executable, currentExe)));
                    }
                    catch (Exception e) when (e is IOException or SecurityException
                        or UnauthorizedAccessException or ArgumentException)
                    {
                        warnings.Add(area + "\\" + name + ": " + e.Message);
                    }
                }
            }
            catch (Exception e) when (e is IOException or SecurityException
                or UnauthorizedAccessException)
            {
                warnings.Add(area + ": " + e.Message);
            }
        }
        entries.Sort((a, b) =>
        {
            int current = b.IsCurrentClone.CompareTo(a.IsCurrentClone);
            return current != 0 ? current :
                string.Compare(a.Location + a.ValueName, b.Location + b.ValueName,
                    StringComparison.OrdinalIgnoreCase);
        });
        return new StartupRegistryScan(entries, warnings);
    }

    // Recognize CDM-owned names OR a command pointing to an EXE with the CDM name.
    // An unrecognized startup value never appears as a deletion target.
    internal static bool IsCdmRegistration(string name, string command)
    {
        if (string.Equals(name, ValueName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "CDM", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("CDM-", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("CoolifyDeployManager", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Coolify Deploy Manager", StringComparison.OrdinalIgnoreCase))
            return true;
        string? path = ExtractExecutablePath(command);
        if (path is null) return false;
        string filename = Path.GetFileName(path);
        return filename.StartsWith("CDM-", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(filename, "CDM.exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(filename, "CoolifyDeployManager.exe", StringComparison.OrdinalIgnoreCase);
    }

    // Extract the launch target solely from the registered command string; no File.Exists.
    internal static string? ExtractExecutablePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        var match = Regex.Match(command, @"^\s*(?:""(?<quoted>[^""]+\.exe)""|(?<plain>(?:[a-zA-Z]:\\|\\\\|%[^%]+%\\)[^\r\n]*?\.exe))(?=\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
        return match.Success ? (match.Groups["quoted"].Success
            ? match.Groups["quoted"].Value : match.Groups["plain"].Value) : null;
    }

    internal static bool IsSameExecutable(string? registeredPath, string currentExe)
    {
        if (registeredPath is null) return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(Environment.ExpandEnvironmentVariables(registeredPath)),
                Path.GetFullPath(currentExe), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException
            or PathTooLongException)
        {
            return false;
        }
    }

    // Exact-value compare-and-delete; no bulk deletion, no registry keys or EXEs deleted.
    internal static StartupRemovalOutcome RemoveSelected(
        IReadOnlyList<StartupRegistration> selected)
    {
        int removed = 0, changed = 0;
        var errors = new List<string>();
        foreach (var entry in selected)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(entry.Hive, entry.View);
                using var key = baseKey.OpenSubKey(entry.SubKeyPath, writable: true);
                if (key is null || Array.IndexOf(key.GetValueNames(), entry.ValueName) < 0)
                {
                    changed++;
                    continue;
                }
                RegistryValueKind kind = key.GetValueKind(entry.ValueName);
                string? command = key.GetValue(entry.ValueName, null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                if (kind != entry.Kind || command is null ||
                    !string.Equals(command, entry.Command, StringComparison.Ordinal) ||
                    !IsCdmRegistration(entry.ValueName, command))
                {
                    changed++;
                    continue;
                }
                key.DeleteValue(entry.ValueName, throwOnMissingValue: false);
                removed++;
            }
            catch (Exception e) when (e is IOException or SecurityException
                or UnauthorizedAccessException or ArgumentException)
            {
                errors.Add(entry.Location + "\\" + entry.ValueName + ": " + e.Message);
            }
        }
        return new StartupRemovalOutcome(removed, changed, errors);
    }

    // No registry I/O: classification and exact current-EXE matching, including
    // three simultaneous entries and an unregistered current EXE.
    internal static void VerifyRegistryManagerForSmoke()
    {
        const string now = @"C:\CDM\CDM-v3.0.0.exe";
        const string earlier = @"C:\Old\CDM-v2.2.3.exe";
        const string other = @"D:\Tools\CDM-test.exe";
        var simulated = new[]
        {
            ("CDM-Coolify-Deploy-Manager", CommandFor(earlier)),
            ("Old CDM entry", CommandFor(other)),
            ("CDM-v3", CommandFor(now))
        };
        if (simulated.Length != 3 ||
            !IsCdmRegistration(simulated[0].Item1, simulated[0].Item2) ||
            !IsCdmRegistration(simulated[1].Item1, simulated[1].Item2) ||
            !IsCdmRegistration(simulated[2].Item1, simulated[2].Item2) ||
            IsCdmRegistration("OtherApp", @"""C:\Apps\OtherApp.exe"" --quiet") ||
            ExtractExecutablePath(CommandFor(earlier)) != earlier ||
            !IsSameExecutable(ExtractExecutablePath(simulated[2].Item2), now) ||
            IsSameExecutable(ExtractExecutablePath(simulated[0].Item2), now) ||
            simulated.Any(x => IsSameExecutable(ExtractExecutablePath(x.Item2),
                @"C:\BrandNew\CDM-v3.0.0.exe")))
            throw new InvalidDataException("CDM startup registry manager classification failed.");
    }
}
