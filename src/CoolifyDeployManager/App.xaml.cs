// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;

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
            if (!SettingsStore.Initialize())
            {
                Shutdown();
                return;
            }
            Loc.Apply(SettingsStore.Load().Language);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.T("VaultStartupFailed") + "\n\n" + ex.Message,
                Loc.T("VaultUnlockTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var dashboard = new MainWindow();
        MainWindow = dashboard;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        dashboard.Show();
    }
}
