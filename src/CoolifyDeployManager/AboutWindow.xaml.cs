// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Diagnostics;
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

    private void Support_Click(object sender, RoutedEventArgs e)
    {
        const string url = "https://www.paypal.com/paypalme/Cris0338/";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PayPal.Me", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
