// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Windows;
namespace CoolifyDeployManager;
public partial class GuideWindow : Window
{
    public GuideWindow() { InitializeComponent(); }
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
