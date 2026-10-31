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
        var dashboard = new MainWindow();
        MainWindow = dashboard;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        dashboard.Show();
    }
}
