// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;
using System.IO;

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
                    // Windows PowerShell's File.Replace failed on some Windows paths.
                    // Copy the staged EXE only after creating a separate full backup.
                    File.Copy(staged, target, overwrite: true);
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
            (e.Args[0] == "--emit-self-test-clone" || e.Args[0] == "--verify-self-test-clone" || e.Args[0] == "--verify-template-empty"))
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

    private static async Task ShowDashboardAfterSplashAsync(MainWindow dashboard, Window splash)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        if (Application.Current is null) return;
        dashboard.Show();
        splash.Close();
    }
}
