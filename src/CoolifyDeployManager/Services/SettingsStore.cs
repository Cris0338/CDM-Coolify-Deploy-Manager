// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// The generic CDM.exe must NEVER load old settings or auto-connect.
// Only a personalized clone reads the profile embedded in its own executable.
using System.IO;
using System.Text.Json;
using System.Windows;

namespace CoolifyDeployManager;

internal static class SettingsStore
{
    private static UserSettings? cached;
    private static bool isClone;
    private static string? stagedPath;

    public static bool HasPortableProfile => isClone;
    public static bool LaunchedClone { get; private set; }

    public static bool Initialize()
    {
        isClone = ExecutableProfile.TryRead(out var embedded);
        cached = Normalize(isClone
            ? embedded ?? throw new InvalidDataException("The embedded profile is empty.")
            : new UserSettings());
        // Do not inspect or migrate %LOCALAPPDATA%, CDM.profile,
        // SSH configuration, the registry, or previous-version credentials.
        return true;
    }

    public static UserSettings Load() =>
        (cached ?? throw new InvalidOperationException("Settings are not initialized.")).Copy();

    public static void Save(UserSettings settings, bool? startWithWindows = null)
    {
        var normalized = Normalize(settings.Copy());
        if (!normalized.IsConfigured)
            throw new ArgumentException("Enter a Coolify token and complete the connection details.");

        if (!isClone)
        {
            // The clean template is never modified and never acquires credentials.
            var target = ExecutableProfile.NextClonePath(normalized.ServerLabel);
            ExecutableProfile.Build(ExecutableProfile.OwnPath, target, normalized);
            if (new FileInfo(target).Length <= new FileInfo(ExecutableProfile.OwnPath).Length)
                throw new InvalidDataException("The personalized executable is incomplete.");
            if (startWithWindows == true) WindowsStartup.SetRegistered(target, true);
            ExecutableProfile.Launch(target);
            LaunchedClone = true;
            // Previous versions' configuration files are left completely untouched.
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(Application.Current.Shutdown));
            return;
        }

        var pending = stagedPath ?? Path.Combine(
            Path.GetDirectoryName(ExecutableProfile.OwnPath)!,
            "." + Path.GetFileName(ExecutableProfile.OwnPath) + ".pending");

        ExecutableProfile.Build(ExecutableProfile.OwnPath, pending, normalized);
        stagedPath = pending;
        cached = normalized;
        // Machine-local opt-in; card-order and automatic profile saves never
        // rewrite Windows startup configuration.
        if (startWithWindows is bool enabled)
            WindowsStartup.SetRegistered(ExecutableProfile.OwnPath, enabled);
    }

    // During a running session, every Save replaces the complete staged EXE,
    // without spawning a short-lived updater that can exhaust its retries
    // before the owner has finished using CDM. Only the application Exit
    // event schedules the on-disk replacement, when Windows is about to
    // release the executable. A crash leaves .pending recoverable.
    internal static string? StagedPathForDiagnostics => stagedPath;

    internal static void SchedulePendingAtExit()
    {
        if (!isClone || stagedPath is null) return;
        ExecutableProfile.ScheduleReplaceOnExit(stagedPath);
        stagedPath = null;
    }

    private static UserSettings Normalize(UserSettings value)
    {
        value.Overrides = new(value.Overrides ?? new(), StringComparer.OrdinalIgnoreCase);
        value.CardOrders = (value.CardOrders ?? new()).ToDictionary(
            pair => pair.Key, pair => pair.Value ?? new List<string>(),
            StringComparer.OrdinalIgnoreCase);
        value.LockedPositions = new(value.LockedPositions ?? new(), StringComparer.OrdinalIgnoreCase);
        value.BlockedDeploys = new(value.BlockedDeploys ?? new(), StringComparer.OrdinalIgnoreCase);
        value.Language ??= "system";
        value.ReadToken ??= "";
        value.DeployToken = string.IsNullOrWhiteSpace(value.DeployToken)
            ? value.ReadToken : value.DeployToken;
        value.ServerUrl ??= "";
        value.ServerLabel = string.IsNullOrWhiteSpace(value.ServerLabel)
            ? "coolify" : value.ServerLabel;
        value.SshHost ??= "";
        value.SshUsername ??= "";
        value.SshPrivateKey ??= "";
        value.SshKnownHosts ??= "";
        value.SshRemoteHost ??= "127.0.0.1";
        return value;
    }
}
