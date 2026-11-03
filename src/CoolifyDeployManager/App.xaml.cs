// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Windows;
using System.IO;
using System.Windows.Media.Animation;

namespace CoolifyDeployManager;

public partial class App : Application
{
    private SingleInstanceGate? runningInstance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Loc.Apply("system");
        try
        {
            SettingsStore.Initialize();
            Loc.Apply(SettingsStore.Load().Language);
        }
        catch (Exception ex)
        {
            MessageBox.Show("CDM could not read its embedded profile.\n\n" + ex.Message,
                "CDM configuration", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        // Same-v3 maintenance is allowed AFTER independent v3 provisioning
        // has succeeded. This is NOT a migration from v2.x.
        if (e.Args.Length == 2 &&
            (e.Args[0] == "--refresh-v3-clone" ||
             e.Args[0] == "--refresh-v3-clone-silent"))
        {
            bool silent = e.Args[0] == "--refresh-v3-clone-silent";
            try
            {
                if (SettingsStore.HasPortableProfile)
                    throw new InvalidOperationException(
                        "Use the empty CDM-v3.0.2.exe, not a personalized EXE.");
                var refreshedPath = V3CloneRefresher.Refresh(e.Args[1]);
                if (!silent)
                    MessageBox.Show("Your personal CDM has been updated to version 3.0.2. " +
                        "The restricted SSH key, Coolify token and preferences were preserved. " +
                        "New EXE: " + Path.GetFileName(refreshedPath) + ". " +
                        "No VPS changes were made.", "CDM v3.0.2 refresh",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown(0);
            }
            catch (Exception ex)
            {
                if (silent)
                {
                    try { File.WriteAllText(e.Args[1] + ".refresh-error",
                        ex.GetType().Name + ": " + ex.Message); }
                    catch (IOException) { }
                }
                else
                    MessageBox.Show(ex.Message, "CDM v3 refresh failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown(1);
            }
            return;
        }

        // CDM 3.x requires independent v3 provisioning. Never import a 2.x clone, SSH key,
        // settings or API token, even when an old CLI upgrade switch is used.
        if (e.Args.Length == 2 &&
            (e.Args[0] == "--upgrade-clone" || e.Args[0] == "--upgrade-clone-silent"))
        {
            const string message = "CDM 3.0.2 forbids migration from 2.x. To refresh an existing v3 personal EXE, use --refresh-v3-clone with CDM-v3.0.2.exe.";
            if (e.Args[0] == "--upgrade-clone-silent")
            {
                try { File.WriteAllText(e.Args[1] + ".upgrade-error", message); }
                catch (IOException) { }
            }
            else
                MessageBox.Show(message, "CDM 3.0.2 — fresh setup required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        // Local smoke-test entry points: verify that a *published* .NET single-file
        // executable can still start after the personalized trailer is appended.
        // No network calls, real credentials, or GitHub Actions are involved.
        if (e.Args.Length == 2 &&
            (e.Args[0] == "--emit-self-test-clone" ||
             e.Args[0] == "--verify-self-test-clone" ||
             e.Args[0] == "--verify-template-empty" ||
             e.Args[0] == "--save-self-test-clone" ||
             e.Args[0] == "--verify-saved-self-test-clone" ||
             e.Args[0] == "--verify-health-viewmodel" ||
             e.Args[0] == "--verify-tunnel-recovery-policy" ||
             e.Args[0] == "--verify-live-deploy-ui" ||
             e.Args[0] == "--verify-live-spinner-retemplate" ||
             e.Args[0] == "--verify-versioned-executable-names" ||
             e.Args[0] == "--verify-tray-ui" ||
             e.Args[0] == "--verify-deploy-shell-indicator" ||
             e.Args[0] == "--verify-deployment-log-follower" ||
              e.Args[0] == "--verify-v3-ssh-bootstrap" ||
              e.Args[0] == "--verify-project-selection-persistence" ||
              e.Args[0] == "--verify-manual-copy-feedback" ||
              e.Args[0] == "--verify-first-run-guide" ||
             e.Args[0] == "--verify-window-placement" ||
             e.Args[0] == "--verify-build-info"))
        {
            try
            {
                if (e.Args[0] == "--emit-self-test-clone")
                {
                    var sample = new UserSettings {
                        ServerLabel = "smoke-test", ServerUrl = "https://example.invalid",
                        ReadToken = "__CDM_SELFTEST__", DeployToken = "__CDM_SELFTEST__"
                    };
                    ExecutableProfile.Build(ExecutableProfile.OwnPath, e.Args[1], sample);
                }
                else if (e.Args[0] == "--verify-template-empty")
                {
                    var current = SettingsStore.Load();
                    if (SettingsStore.HasPortableProfile ||
                        current.IsConfigured ||
                        !string.IsNullOrWhiteSpace(current.ReadToken) ||
                        !string.IsNullOrWhiteSpace(current.DeployToken) ||
                        !string.IsNullOrWhiteSpace(current.ServerUrl) ||
                        !string.IsNullOrWhiteSpace(current.SshPrivateKey))
                        throw new InvalidDataException("Generic CDM.exe unexpectedly loaded credentials or configuration.");
                    File.WriteAllText(e.Args[1], "CDM-TEMPLATE-CLEAN");
                }
                else if (e.Args[0] == "--save-self-test-clone")
                {
                    if (!SettingsStore.HasPortableProfile ||
                        SettingsStore.Load().ServerLabel != "smoke-test")
                        throw new InvalidDataException("Only the disposable test clone can exercise pending saves.");
                    var updated = SettingsStore.Load();
                    updated.ServerLabel = "smoke-test-saved";
                    updated.CardOrders["ALL|Staging"] = new List<string> { "test-b", "test-a" };
                    updated.LastSelectedProjectUuid = "sample-project-uuid";
                    updated.LockedPositions.Add("test-b");
                    updated.WindowPlacement = new WindowPlacementState
                    {
                        Left = 140, Top = 110, Width = 1120, Height = 740,
                        Maximized = true
                    };
                    SettingsStore.Save(updated);
                    // During the running clone the staged file must already be
                    // complete, but the updater must not have been launched.
                    var staged = SettingsStore.StagedPathForDiagnostics;
                    if (staged is null || !File.Exists(staged))
                        throw new InvalidDataException("The profile change was not staged.");
                    if (File.Exists(staged + ".updater-started.txt"))
                        throw new InvalidDataException(
                            "An updater started before the disposable clone exited.");
                    File.WriteAllText(e.Args[1], "CDM-PENDING-STAGED");
                }
                else if (e.Args[0] == "--verify-build-info")
                {
                    if (BuildInfo.Version == "unknown" ||
                        BuildInfo.Commit.Length < 7 ||
                        BuildInfo.BuildUtc == "unspecified")
                        throw new InvalidDataException("Published CDM has incomplete source/build metadata.");
                    File.WriteAllText(e.Args[1],
                        $"{BuildInfo.Version}|{BuildInfo.Commit}|{BuildInfo.BuildUtc}");
                }
                else if (e.Args[0] == "--verify-first-run-guide")
                {
                    // Multi-monitor layout checks simulate a monitor to the
                    // right of primary, one with negative coordinates, and
                    // a narrow display (no second physical monitor required).
                    CompanionWindowPlacement.VerifyForSmoke();
                    // Exercise only pure classification in this path. The real WPF
                    // registration modal is tested below with synthetic entries.
                    WindowsStartup.VerifyRegistryManagerForSmoke();
                    // Both the generic first-run wizard and a previously
                    // configured personal clone must keep an interactive guide
                    // alongside the modal Settings dialog, without SSH/API I/O.
                    var profiles = new[]
                    {
                        new UserSettings(),
                        new UserSettings
                        {
                            ServerUrl = "https://example.invalid",
                            ReadToken = "__CDM_TEST_READ__",
                            DeployToken = "__CDM_TEST_DEPLOY__"
                        }
                    };
                    if (profiles[0].IsConfigured || !profiles[1].IsConfigured)
                        throw new InvalidDataException("Guide smoke profiles are incorrectly configured.");
                    foreach (var profile in profiles)
                    {
                        bool passed = false;
                        var wizard = new SettingsWindow(profile)
                        {
                            ShowCompanionGuideOnOpen = true
                        };
                        wizard.Loaded += (sender, args) =>
                            Dispatcher.BeginInvoke(
                                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                                new Action(() =>
                                {
                                    GuideWindow? side = null;
                                    foreach (Window owned in wizard.OwnedWindows)
                                        if (owned is GuideWindow found) { side = found; break; }
                                    bool modalPassed = false;
                                    if (side is { IsVisible: true, IsEnabled: true })
                                    {
                                        var current = new StartupRegistration(
                                            Microsoft.Win32.RegistryHive.CurrentUser,
                                            Microsoft.Win32.RegistryView.Default,
                                            @"Software\Microsoft\Windows\CurrentVersion\Run",
                                            WindowsStartup.ValueName, Microsoft.Win32.RegistryValueKind.String,
                                            WindowsStartup.CommandFor(@"C:\CDM\current.exe"),
                                            @"C:\CDM\current.exe", true);
                                        var prior = new StartupRegistration(
                                            Microsoft.Win32.RegistryHive.CurrentUser,
                                            Microsoft.Win32.RegistryView.Default,
                                            @"Software\Microsoft\Windows\CurrentVersion\Run",
                                            "CDM-older", Microsoft.Win32.RegistryValueKind.String,
                                            WindowsStartup.CommandFor(@"C:\CDM\previous.exe"),
                                            @"C:\CDM\previous.exe", false);
                                        var secondary = new StartupRegistration(
                                            Microsoft.Win32.RegistryHive.LocalMachine,
                                            Microsoft.Win32.RegistryView.Registry64,
                                            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                                            "CDM-second", Microsoft.Win32.RegistryValueKind.String,
                                            WindowsStartup.CommandFor(@"D:\CDM\backup.exe"),
                                            @"D:\CDM\backup.exe", false);
                                        side.StartupWindowFactory = () =>
                                        {
                                            var dialog = new StartupRegistryWindow(
                                                new[] { current, prior, secondary });
                                            dialog.Loaded += (_, _) =>
                                                Dispatcher.BeginInvoke(
                                                    System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                                                    new Action(() =>
                                                    {
                                                        dialog.VerifyModalForSmoke();
                                                        modalPassed = dialog.IsVisible && dialog.Owner == side;
                                                        dialog.Close();
                                                    }));
                                            return dialog;
                                        };
                                        side.OpenStartupManagerButton.RaiseEvent(new RoutedEventArgs(
                                            System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                                    }
                                    passed = wizard.IsVisible &&
                                        side is { IsVisible: true, IsEnabled: true } &&
                                        side.ResizeMode == ResizeMode.CanResize &&
                                        side.OpenStartupManagerButton is not null &&
                                        modalPassed &&
                                        !wizard.ShowCompanionGuideOnOpen;
                                    wizard.DialogResult = false;
                                }));
                        wizard.ShowDialog();
                        if (!passed)
                            throw new InvalidDataException(
                                "Settings and guide were not simultaneously interactive for " +
                                (profile.IsConfigured ? "configured personal clone." : "generic first run."));
                    }
                    File.WriteAllText(e.Args[1], "CDM-FIRST-RUN-GUIDE-OK");
                }
                else if (e.Args[0] == "--verify-manual-copy-feedback")
                {
                    // A real WPF visual test without clipboard, SSH or API access.
                    var dialog = new SettingsWindow(new UserSettings());
                    dialog.VerifyManualCopyFeedbackForSmoke();
                    dialog.Close();
                    File.WriteAllText(e.Args[1], "CDM-MANUAL-COPY-FEEDBACK-OK");
                }
                else if (e.Args[0] == "--verify-project-selection-persistence")
                {
                    var sampleProjects = new List<CoolifyProject> {
                        new("project-a-uuid", "First"),
                        new("project-b-uuid", "Renamed Project")
                    };
                    if (global::CoolifyDeployManager.MainWindow.RestoreSavedProject(
                            "project-b-uuid", sampleProjects) != "project-b-uuid" ||
                        global::CoolifyDeployManager.MainWindow.RestoreSavedProject(
                            "PROJECT-B-UUID", sampleProjects) != "project-b-uuid" ||
                        global::CoolifyDeployManager.MainWindow.RestoreSavedProject(
                            "deleted-project", sampleProjects) != "ALL" ||
                        global::CoolifyDeployManager.MainWindow.RestoreSavedProject(
                            "", sampleProjects) != "ALL" ||
                        global::CoolifyDeployManager.MainWindow.RestoreSavedProject(
                            "ALL", sampleProjects) != "ALL")
                        throw new InvalidDataException("Saved project selection did not restore by UUID or fall back to All.");

                    var preference = new UserSettings { LastSelectedProjectUuid = "project-b-uuid" };
                    var serialized = System.Text.Json.JsonSerializer.Serialize(preference.Copy());
                    var restored = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(serialized);
                    if (restored?.LastSelectedProjectUuid != "project-b-uuid" ||
                        new UserSettings().LastSelectedProjectUuid != "ALL")
                        throw new InvalidDataException("Project selection did not survive profile JSON serialization.");
                    File.WriteAllText(e.Args[1], "CDM-PROJECT-SELECTION-OK");
                }
                else if (e.Args[0] == "--verify-v3-ssh-bootstrap")
                {
                    SshBootstrap.VerifyForSmoke();
                    File.WriteAllText(e.Args[1], "CDM-V3-SSH-BOOTSTRAP-OK");
                }
                else if (e.Args[0] == "--verify-tunnel-recovery-policy")
                {
                    static bool Eligible(ConnectionMode mode, bool configured,
                        bool busy, bool running, bool recovering, bool closing, bool configuring = false) =>
                        global::CoolifyDeployManager.MainWindow.ShouldRecoverManagedTunnel(mode, configured,
                            busy, running, recovering, closing, configuring);
                    if (!Eligible(ConnectionMode.ManagedSshTunnel, true, false,
                            false, false, false) ||
                        Eligible(ConnectionMode.ManagedSshTunnel, true, false,
                            true, false, false) ||
                        Eligible(ConnectionMode.ManagedSshTunnel, true, true,
                            false, false, false) ||
                        Eligible(ConnectionMode.ManagedSshTunnel, false, false,
                            false, false, false) ||
                        Eligible(ConnectionMode.ManagedSshTunnel, true, false,
                            false, true, false) ||
                        Eligible(ConnectionMode.ManagedSshTunnel, true, false,
                            false, false, true) ||
                        // A modal Settings + companion Guide must reserve this port
                        // even after several dashboard clock ticks.
                        Eligible(ConnectionMode.ManagedSshTunnel, true, false,
                            false, false, false, configuring: true) ||
                        Eligible(ConnectionMode.Https, true, false,
                            false, false, false) ||
                        Eligible(ConnectionMode.ExternalLocalTunnel, true, false,
                            false, false, false))
                        throw new InvalidDataException(
                            "SSH auto-recovery policy unexpectedly enables or blocks a reconnect.");
                    File.WriteAllText(e.Args[1], "CDM-TUNNEL-RECOVERY-POLICY-OK");
                }
                else if (e.Args[0] == "--verify-live-deploy-ui")
                {
                    var sample = new CoolifyApplication("live-deploy-test", "live-deploy-test",
                        "running:healthy", "main", "test", "test-project", "staging", "");
                    var card = new AppCardView(sample, DeploymentGroup.Staging);
                    int activityNotifications = 0;
                    card.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(AppCardView.IsLiveDeploymentActive))
                            activityNotifications++;
                    };
                    if (card.IsLiveDeploymentActive ||
                        !global::CoolifyDeployManager.MainWindow.CanReorderWhile(false, "", 0) ||
                        global::CoolifyDeployManager.MainWindow.CanReorderWhile(true, "", 0) ||
                        global::CoolifyDeployManager.MainWindow.CanReorderWhile(false, "filtered", 0) ||
                        global::CoolifyDeployManager.MainWindow.CanReorderWhile(false, "", 1))
                        throw new InvalidDataException("Initial deploy activity or card drag policy is invalid.");

                    foreach (var state in new[] { "queued", "in_progress", "pending", "starting" })
                    {
                        card.UpdateLiveDeployment(state);
                        if (!card.IsLiveDeploymentActive || card.LiveDeploymentStatus != state)
                            throw new InvalidDataException("Confirmed active deployment did not animate: " + state);
                        // A running deployment is *not* an inventory refresh:
                        // drag remains available unless the layout is filtered.
                        if (!global::CoolifyDeployManager.MainWindow.CanReorderWhile(false, "", 0))
                            throw new InvalidDataException("Card drag was blocked during deployment.");
                    }
                    foreach (var state in new[] { "finished", "failed", "cancelled", "cancelled-by-user", "error", "unknown" })
                    {
                        card.UpdateLiveDeployment(state);
                        if (card.IsLiveDeploymentActive)
                            throw new InvalidDataException("Terminal/unknown deployment still animates: " + state);
                    }
                    card.UpdateLiveDeployment(null);
                    if (card.IsLiveDeploymentActive || card.LiveDeploymentStatus != "" ||
                        activityNotifications < 2)
                        throw new InvalidDataException("Deploy indicator did not clear or notify WPF bindings.");
                    File.WriteAllText(e.Args[1], "CDM-LIVE-DEPLOY-UI-OK");
                }
                else if (e.Args[0] == "--verify-versioned-executable-names")
                {
                    VersionedExecutableName.VerifyForSmoke();
                    V3CloneRefresher.VerifyUpgradePolicyForSmoke();
                    if (!Path.GetFileName(ExecutableProfile.OwnPath).Equals(
                            VersionedExecutableName.Generic(BuildInfo.Version),
                            StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException(
                            "Published generic EXE name does not match embedded version.");
                    File.WriteAllText(e.Args[1], "CDM-VERSIONED-FILENAMES-OK");
                }
                else if (e.Args[0] == "--verify-tray-ui")
                {
                    // Only pure registry command/classification validation and local WPF UI;
                    // DO NOT modify Windows startup keys or connect to Coolify.
                    WindowsStartup.VerifyRegistryManagerForSmoke();
                    WindowsStartup.VerifyRegistryManagerForSmoke();
                    var cmd = WindowsStartup.CommandFor(
                        @"C:\Program Files\CDM\Personal CDM.exe");
                    if (cmd != "\"C:\\Program Files\\CDM\\Personal CDM.exe\" --startup-tray" ||
                        SingleInstanceGate.IdentityFor(@"C:\CDM\one.exe") ==
                        SingleInstanceGate.IdentityFor(@"C:\CDM\two.exe"))
                        throw new InvalidDataException(
                            "Windows startup command or per-clone identity is invalid.");

                    // Exercise the named mutex + reopen signal without
                    // launching a second CDM or connecting to the real server.
                    var fakeClonePath = Path.Combine(Path.GetTempPath(),
                        "CDM-instance-test-" + Guid.NewGuid().ToString("N") + ".exe");
                    using var reopened = new ManualResetEventSlim(false);
                    using (var firstInstance = SingleInstanceGate.TryCreate(
                        fakeClonePath, () => reopened.Set())
                        ?? throw new InvalidDataException("Could not create first test instance."))
                    {
                        using var duplicate = SingleInstanceGate.TryCreate(
                            fakeClonePath, () => { });
                        if (duplicate is not null || !reopened.Wait(TimeSpan.FromSeconds(4)))
                            throw new InvalidDataException(
                                "A second CDM launch did not signal the original instance.");
                    }

                    var trayDashboard = new MainWindow { SkipStartupConnectionForSmoke = true };
                    MainWindow = trayDashboard;
                    trayDashboard.Show();
                    try
                    {
                        trayDashboard.WindowState = WindowState.Minimized;
                        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                        if (trayDashboard.IsVisible || trayDashboard.ShowInTaskbar)
                            throw new InvalidDataException(
                                "Minimization did not hide the real WPF window to notification area.");
                        trayDashboard.RestoreFromTray();
                        if (!trayDashboard.IsVisible || !trayDashboard.ShowInTaskbar ||
                            trayDashboard.WindowState != WindowState.Normal)
                            throw new InvalidDataException(
                                "Tray restore did not reopen the real WPF dashboard.");
                        File.WriteAllText(e.Args[1], "CDM-TRAY-UI-OK");
                    }
                    finally { trayDashboard.Close(); }
                }
                else if (e.Args[0] == "--verify-deployment-log-follower")
                {
                    // Exercise actual Coolify log JSON parsing, hidden filtering,
                    // and incremental output without server, SSH, or credentials.
                    DeploymentLogFollower.VerifyForSmoke();
                    File.WriteAllText(e.Args[1], "CDM-DEPLOYMENT-LOG-FOLLOWER-OK");
                }
                else if (e.Args[0] == "--verify-deploy-shell-indicator")
                {
                    // Real WPF TaskbarItemInfo and WinForms tray icon surfaces,
                    // without calling Coolify/SSH or touching user configuration.
                    var shellSmokeWindow = new MainWindow { SkipStartupConnectionForSmoke = true };
                    MainWindow = shellSmokeWindow;
                    shellSmokeWindow.Show();
                    try
                    {
                        shellSmokeWindow.VerifyDeployShellIndicatorForSmoke();
                        File.WriteAllText(e.Args[1], "CDM-SHELL-INDICATOR-OK");
                    }
                    finally { shellSmokeWindow.Close(); }
                }
                else if (e.Args[0] == "--verify-window-placement")
                {
                    // Test real WPF window placement, tray state, and portable
                    // settings JSON without API, SSH, registry or profile writes.
                    var desktopX = SystemParameters.VirtualScreenLeft;
                    var desktopY = SystemParameters.VirtualScreenTop;
                    var saved = new WindowPlacementState
                    {
                        Left = desktopX + 35, Top = desktopY + 35,
                        Width = 1050, Height = 720, Maximized = true
                    };
                    if (!saved.IntersectsDesktop(desktopX, desktopY,
                            SystemParameters.VirtualScreenWidth,
                            SystemParameters.VirtualScreenHeight) ||
                        new WindowPlacementState {
                            Left = -50000, Top = -50000,
                            Width = 1050, Height = 720
                        }.IntersectsDesktop(desktopX, desktopY,
                            SystemParameters.VirtualScreenWidth,
                            SystemParameters.VirtualScreenHeight))
                        throw new InvalidDataException("Saved window placement screen validation failed.");

                    var serialized = System.Text.Json.JsonSerializer.Serialize(
                        new UserSettings { WindowPlacement = saved });
                    var restored = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(serialized);
                    if (restored?.Copy().WindowPlacement != saved)
                        throw new InvalidDataException("Portable window placement JSON was not preserved.");

                    var placementWindow = new MainWindow { SkipStartupConnectionForSmoke = true };
                    placementWindow.ApplyWindowPlacementForSmoke(saved);
                    MainWindow = placementWindow;
                    if (placementWindow.WindowState != WindowState.Maximized ||
                        placementWindow.WindowStartupLocation != WindowStartupLocation.Manual)
                        throw new InvalidDataException("Maximized placement was not restored on construction.");
                    placementWindow.Show();
                    try
                    {
                        placementWindow.WindowState = WindowState.Minimized;
                        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                        var hidden = placementWindow.SnapshotWindowPlacement();
                        if (placementWindow.IsVisible || placementWindow.ShowInTaskbar ||
                            hidden?.Maximized != true)
                            throw new InvalidDataException(
                                "Minimize-to-tray discarded the last maximized presentation.");
                        placementWindow.RestoreFromTray();
                        if (!placementWindow.IsVisible || !placementWindow.ShowInTaskbar ||
                            placementWindow.WindowState != WindowState.Maximized)
                            throw new InvalidDataException(
                                "Tray restore did not recover maximized presentation.");

                        placementWindow.WindowState = WindowState.Normal;
                        placementWindow.Left = desktopX + 50;
                        placementWindow.Top = desktopY + 50;
                        placementWindow.Width = 1040;
                        placementWindow.Height = 700;
                        placementWindow.UpdateLayout();
                        var normal = placementWindow.SnapshotWindowPlacement();
                        if (normal is null || normal.Maximized ||
                            Math.Abs(normal.Left - (desktopX + 50)) > 2 ||
                            Math.Abs(normal.Top - (desktopY + 50)) > 2)
                            throw new InvalidDataException(
                                "Normal window coordinates or state were not captured.");

                        placementWindow.WindowState = WindowState.Minimized;
                        var preserved = placementWindow.SnapshotWindowPlacement();
                        if (preserved != normal)
                            throw new InvalidDataException(
                                "Minimizing overwrote the last normal position.");
                        placementWindow.RestoreFromTray();
                        if (placementWindow.WindowState != WindowState.Normal ||
                            !placementWindow.IsVisible || !placementWindow.ShowInTaskbar)
                            throw new InvalidDataException(
                                "Tray restore failed to recover normal window presentation.");

                        File.WriteAllText(e.Args[1], "CDM-WINDOW-PLACEMENT-OK");
                    }
                    finally { placementWindow.Close(); }
                }
                else if (e.Args[0] == "--verify-live-spinner-retemplate")
                {
                    // Display an actual WPF card template without API/SSH calls.
                    // This explicitly exercises the drag/rebuild crash path.
                    var smokeDashboard = new MainWindow { SkipStartupConnectionForSmoke = true };
                    MainWindow = smokeDashboard;
                    smokeDashboard.Show();
                    try
                    {
                        smokeDashboard.VerifyLiveSpinnerRetemplateForSmoke();
                        File.WriteAllText(e.Args[1], "CDM-WPF-SPINNER-RETEMPLATE-OK");
                    }
                    finally { smokeDashboard.Close(); }
                }
                else if (e.Args[0] == "--verify-health-viewmodel")
                {
                    var sampleApp = new CoolifyApplication("offline-health-test", "health-test",
                        "running:unhealthy", "main", "test-project", "test-project-uuid",
                        "staging", "");
                    var card = new AppCardView(sampleApp, DeploymentGroup.Staging);
                    bool notified = false;
                    card.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(AppCardView.HealthLabel))
                            notified = true;
                    };
                    card.UpdateDeploy(new LatestDeploy(DateTimeOffset.UtcNow,
                        "finished", "abcdef012345", true));
                    if (card.HealthLabel != "UNHEALTHY" || !card.HasProblem)
                        throw new InvalidDataException("Initial unhealthy state was not represented.");
                    card.UpdateApplicationStatus("running:healthy");
                    if (!notified || card.HealthLabel != "HEALTHY" ||
                        !card.Running || card.HasProblem ||
                        !card.LastStatus.StartsWith("finished", StringComparison.Ordinal))
                        throw new InvalidDataException(
                            "Health sync failed or modified deployment history.");
                    File.WriteAllText(e.Args[1], "CDM-HEALTH-UPDATED");
                }
                else if (e.Args[0] == "--verify-saved-self-test-clone")
                {
                    var updated = SettingsStore.Load();
                    if (!SettingsStore.HasPortableProfile ||
                        updated.ServerLabel != "smoke-test-saved" ||
                        updated.ReadToken != "__CDM_SELFTEST__" ||
                        updated.DeployToken != "__CDM_SELFTEST__" ||
                        updated.LastSelectedProjectUuid != "sample-project-uuid" ||
                        !updated.CardOrders.TryGetValue("ALL|Staging", out var order) ||
                        !order.SequenceEqual(new[] { "test-b", "test-a" }) ||
                        !updated.LockedPositions.Contains("test-b") ||
                        updated.WindowPlacement is not {
                            Left: 140, Top: 110, Width: 1120, Height: 740, Maximized: true
                        })
                        throw new InvalidDataException(
                            "The deferred profile save lost card or window placement data.");
                    File.WriteAllText(e.Args[1], "CDM-PENDING-APPLIED");
                }
                else
                {
                    if (!SettingsStore.HasPortableProfile ||
                        SettingsStore.Load().ServerLabel != "smoke-test" ||
                        SettingsStore.Load().ReadToken != "__CDM_SELFTEST__")
                        throw new InvalidDataException("The published clone lost its embedded test profile.");
                    File.WriteAllText(e.Args[1], "CDM-CLONE-STARTED");
                }
                Shutdown(0);
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(e.Args[1] + ".error", ex.ToString()); }
                catch (Exception) { }
                Shutdown(1);
            }
            return;
        }

        // A second launch of this exact clone reopens the running window;
        // never open a competing managed SSH forward to the same local port.
        if (SettingsStore.HasPortableProfile)
        {
            runningInstance = SingleInstanceGate.TryCreate(ExecutableProfile.OwnPath, () =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (MainWindow is MainWindow current)
                            current.RestoreFromTray();
                    }));
                }
                catch (TaskCanceledException) { }
            });
            if (runningInstance is null)
            {
                Shutdown(0);
                return;
            }
        }

        bool startInTray = e.Args.Length == 1 &&
            e.Args[0] == WindowsStartup.TrayArgument &&
            SettingsStore.HasPortableProfile;
        var dashboard = new MainWindow { StartHiddenToTray = startInTray };
        MainWindow = dashboard;
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        if (startInTray)
        {
            // Windows Run startup must never show the splash or occupy
            // the taskbar. Loaded still starts SSH and the inventory refresh.
            dashboard.ShowActivated = false;
            dashboard.ShowInTaskbar = false;
            dashboard.WindowState = WindowState.Minimized;
            dashboard.Show();
            dashboard.Hide();
            return;
        }

        // Ordinary manual launches retain the existing animated splash.
        var splash = new SplashWindow();
        splash.Show();
        _ = ShowDashboardAfterSplashAsync(dashboard, splash);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        runningInstance?.Dispose();
        runningInstance = null;
        // The clone is already closing: only now spawn a detached helper to
        // install its latest verified .pending profile. Generic CDM.exe and
        // clones with no changes do not spawn a helper.
        try
        {
            SettingsStore.SchedulePendingAtExit();
        }
        catch (Exception ex)
        {
            // Never delete the unapplied .pending and never log its contents.
            var staged = SettingsStore.StagedPathForDiagnostics;
            if (staged is not null)
            {
                try
                {
                    File.WriteAllText(staged + ".updater-error.txt",
                        "Could not launch the CDM profile updater: " + ex.GetType().Name);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            e.ApplicationExitCode = 1;
        }
        base.OnExit(e);
    }

    private static async Task ShowDashboardAfterSplashAsync(MainWindow dashboard, Window splash)
    {
        // At 2.25 seconds the electron collision begins inside the splash.
        // As the particles expand, fade the real window up behind the logo.
        await Task.Delay(TimeSpan.FromMilliseconds(2630));
        if (Application.Current is null) return;
        dashboard.Opacity = 0;
        dashboard.Show();
        dashboard.BeginAnimation(Window.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(370))
            { FillBehavior = FillBehavior.HoldEnd });
        await Task.Delay(TimeSpan.FromMilliseconds(400));
        if (splash.IsVisible) splash.Close();
    }
}
