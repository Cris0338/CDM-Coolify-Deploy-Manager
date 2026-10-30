// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;

namespace CoolifyDeployManager;

public partial class AboutWindow : Window
{
    public AboutWindow() => InitializeComponent();

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
