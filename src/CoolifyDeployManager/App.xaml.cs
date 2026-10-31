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
        dashboard.Show();
    }
}
