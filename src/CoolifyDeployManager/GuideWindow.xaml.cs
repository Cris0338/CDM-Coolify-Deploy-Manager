// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System;
using System.Windows;

namespace CoolifyDeployManager;

public partial class GuideWindow : Window
{
    // Injectable only for isolated WPF smoke. Normal use always opens the real manager.
    internal Func<StartupRegistryWindow>? StartupWindowFactory { get; set; }

    public GuideWindow() { InitializeComponent(); }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenStartupManager_Click(object sender, RoutedEventArgs e)
    {
        var manager = StartupWindowFactory?.Invoke() ?? new StartupRegistryWindow();
        // The Guide can itself be a companion of the modal Settings wizard.
        // Owning this second-level dialog makes it interactive in front of the Guide.
        manager.Owner = this;
        manager.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        manager.ShowDialog();
    }
}
