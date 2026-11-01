// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Windows;
using System.IO;
using System.Windows.Media.Animation;

namespace CoolifyDeployManager;

public partial class App : Application
{
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
        // Explicit, opt-in clone upgrade. The generic CDM.exe never reads old
        // profiles on normal launch. Credentials are migrated ONLY when the user
        // supplies a particular existing clone path on the command line.
        if (e.Args.Length == 2 &&
            (e.Args[0] == "--upgrade-clone" || e.Args[0] == "--upgrade-clone-silent"))
        {
            bool silent = e.Args[0] == "--upgrade-clone-silent";
            try
            {
                if (SettingsStore.HasPortableProfile)
                    throw new InvalidOperationException("Run upgrade from generic CDM.exe, not from a personalized clone.");
                string target = Path.GetFullPath(e.Args[1]);
                if (target.Equals(Path.GetFullPath(ExecutableProfile.OwnPath),
                        StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("The generic CDM.exe cannot upgrade itself.");
                if (!File.Exists(target) || !target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Select an existing CDM personalized .exe.", target);
                if (!ExecutableProfile.TryReadFile(target, out var saved) || saved is null ||
                    !saved.IsConfigured)
                    throw new InvalidDataException("The supplied executable has no valid personalized CDM profile.");
                var staged = target + ".upgrade-in-progress";
                if (File.Exists(staged))
                    throw new IOException("An upgrade file is already present: " + staged);
                try
                {
                    ExecutableProfile.Build(ExecutableProfile.OwnPath, staged, saved);
                    if (!ExecutableProfile.TryReadFile(staged, out var verify) || verify is null ||
                        !saved.ReadToken.Equals(verify.ReadToken, StringComparison.Ordinal) ||
                        !saved.SshPrivateKey.Equals(verify.SshPrivateKey, StringComparison.Ordinal) ||
                        !saved.SshKnownHosts.Equals(verify.SshKnownHosts, StringComparison.Ordinal))
                        throw new InvalidDataException("Clone upgrade failed to preserve embedded credentials.");
                    var backup = target + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
                    File.Copy(target, backup, overwrite: false);
                    // Windows can transiently lock an EXE after it exits.
                    // The replacement is atomic and stays on the same volume;
                    // the timestamped backup above is already recoverable.
                    // Exhausted retries must fail rather than report success.
                    const int maxAttempts = 40;
                    for (int attempt = 0; ; attempt++)
                    {
                        try
                        {
                            // The original already has a timestamped backup.
                            // A same-directory atomic replace avoids a copy-overwrite
                            // race when an executable has just finished running.
                            File.Replace(staged, target, null);
                            break;
                        }
                        catch (Exception ex) when (
                            (ex is IOException || ex is UnauthorizedAccessException) &&
                            attempt < maxAttempts - 1)
                        {
                            // Windows may report transient executable-file locks
                            // as either sharing violations or access denied.
                            // Retry briefly, then preserve and report real failures.
                            System.Threading.Thread.Sleep(250);
                        }
                    }
                    if (!ExecutableProfile.TryReadFile(target, out var installed) ||
                        installed is null || installed.ReadToken != saved.ReadToken ||
                        installed.SshPrivateKey != saved.SshPrivateKey)
                        throw new InvalidDataException(
                            "Upgraded clone did not preserve its embedded credentials; " +
                            "restore the timestamped .bak copy.");
                    if (!silent)
                        MessageBox.Show("Clone upgraded successfully. Existing token and SSH key preserved.\n\n" +
                            "Backup: " + backup, "CDM clone upgrade", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                }
                finally { if (File.Exists(staged)) File.Delete(staged); }
                Shutdown(0);
            }
            catch (Exception ex)
            {
                if (silent)
                {
                    try { File.WriteAllText(e.Args[1] + ".upgrade-error", ex.ToString()); }
                    catch (Exception) { }
                }
                else MessageBox.Show("Clone upgrade failed. Original executable and backup (if created) were not intentionally removed.\n\n" +
                        ex.Message, "CDM clone upgrade", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
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
                    updated.LockedPositions.Add("test-b");
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
                else if (e.Args[0] == "--verify-tunnel-recovery-policy")
                {
                    static bool Eligible(ConnectionMode mode, bool configured,
                        bool busy, bool running, bool recovering, bool closing) =>
                        global::CoolifyDeployManager.MainWindow.ShouldRecoverManagedTunnel(mode, configured,
                            busy, running, recovering, closing);
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
                        !updated.CardOrders.TryGetValue("ALL|Staging", out var order) ||
                        !order.SequenceEqual(new[] { "test-b", "test-a" }) ||
                        !updated.LockedPositions.Contains("test-b"))
                        throw new InvalidDataException("The deferred profile save lost configuration data.");
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

        var dashboard = new MainWindow();
        MainWindow = dashboard;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        // Splash is deliberately skipped by the offline smoke-test entry points.
        // The connection is established only after the dashboard becomes visible.
        var splash = new SplashWindow();
        splash.Show();
        _ = ShowDashboardAfterSplashAsync(dashboard, splash);
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
