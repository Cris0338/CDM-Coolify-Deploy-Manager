// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Windows;
using System.Windows.Controls;

namespace CoolifyDeployManager;

public partial class SettingsWindow : Window
{
    private readonly UserSettings original;
    internal UserSettings Result { get; private set; }
    internal SettingsWindow(UserSettings settings)
    {
        InitializeComponent();
        original = settings.Copy();
        Result = settings.Copy();
        UrlInput.Text = original.ServerUrl;
        ReadToken.Password = original.ReadToken;
        DeployToken.Password = original.DeployToken;
        SshAlias.Text = original.SshHost;
        LocalPort.Text = original.SshLocalPort.ToString();
        RemotePort.Text = original.SshRemotePort.ToString();
        RemoteHost.Text = original.SshRemoteHost;
        ConnectionChooser.SelectedIndex = (int)original.ConnectionMode;
        foreach (var (code, nativeName) in Loc.Languages)
            LanguageChooser.Items.Add(new ComboBoxItem { Content = code == "system" ? Loc.T("SystemLanguage") : nativeName, Tag = code });
        foreach (ComboBoxItem item in LanguageChooser.Items)
            if ((string)item.Tag == original.Language) { LanguageChooser.SelectedItem = item; break; }
        LanguageChooser.SelectedItem ??= LanguageChooser.Items[0];
        UpdateMode();
    }
    private void Mode_Changed(object sender, SelectionChangedEventArgs e) => UpdateMode();
    private void UpdateMode()
    {
        if (SshFields is null || UrlInput is null || UrlHint is null) return;
        var mode = ConnectionChooser.SelectedIndex;
        SshFields.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        UrlInput.IsEnabled = mode == 0;
        UrlHint.Text = mode == 0 ? Loc.T("UrlDefaultHint")
            : mode == 1 ? Loc.T("UrlManagedHint")
            : Loc.T("UrlExternalHint");
    }
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int localPort = int.Parse(LocalPort.Text.Trim());
            int remotePort = int.Parse(RemotePort.Text.Trim());
            if (localPort is < 1024 or > 65535 || remotePort is < 1 or > 65535)
                throw new ArgumentException(Loc.T("InvalidPorts"));
            var mode = (ConnectionMode)Math.Max(0, ConnectionChooser.SelectedIndex);
            if (mode == ConnectionMode.Https) _ = CoolifyApi.NormalizeUrl(UrlInput.Text);
            if (mode == ConnectionMode.ManagedSshTunnel && string.IsNullOrWhiteSpace(SshAlias.Text))
                throw new ArgumentException(Loc.T("NeedSshAlias"));
            if (string.IsNullOrWhiteSpace(ReadToken.Password) || string.IsNullOrWhiteSpace(DeployToken.Password))
                throw new ArgumentException(Loc.T("NeedTokens"));
            var saved = original.Copy();
            saved.ConnectionMode = mode;
            saved.Language = (LanguageChooser.SelectedItem as ComboBoxItem)?.Tag as string ?? "system";
            saved.ServerUrl = UrlInput.Text.Trim();
            saved.ReadToken = ReadToken.Password.Trim();
            saved.DeployToken = DeployToken.Password.Trim();
            saved.SshHost = SshAlias.Text.Trim();
            saved.SshLocalPort = localPort;
            saved.SshRemoteHost = RemoteHost.Text.Trim();
            saved.SshRemotePort = remotePort;
            Result = saved;
            DialogResult = true;
            Close();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            MessageBox.Show(this, ex.Message, Loc.T("InvalidSettings"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
