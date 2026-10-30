// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;
namespace CoolifyDeployManager;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        string lang = "system";
        try { lang = SettingsStore.Load().Language; } catch { /* damaged settings are reported in MainWindow */ }
        Loc.Apply(lang);
        base.OnStartup(e);
    }
}
