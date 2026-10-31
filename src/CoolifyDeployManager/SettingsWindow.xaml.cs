// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CoolifyDeployManager;

public partial class SettingsWindow : Window
{
    private readonly UserSettings original;
    private string privateKey = "";
    private string knownHosts = "";
    internal UserSettings Result { get; private set; }

    internal SettingsWindow(UserSettings settings)
    {
        InitializeComponent();
        original = settings.Copy();
        Result = settings.Copy();
        ServerLabel.Text = original.ServerLabel;
        UrlInput.Text = original.ServerUrl;
        ReadToken.Password = original.ReadToken;
        SshAlias.Text = original.SshHost;
        SshUsername.Text = original.SshUsername;
        SshPort.Text = original.SshPort.ToString();
        LocalPort.Text = original.SshLocalPort.ToString();
        RemotePort.Text = original.SshRemotePort.ToString();
        RemoteHost.Text = original.SshRemoteHost;
        privateKey = original.SshPrivateKey;
        knownHosts = original.SshKnownHosts;
        ConnectionChooser.SelectedIndex = original.ConnectionMode == ConnectionMode.Https ? 0 : 1;
        foreach (var (code, nativeName) in Loc.Languages)
            LanguageChooser.Items.Add(new ComboBoxItem { Content = code == "system" ? Loc.T("SystemLanguage") : nativeName, Tag = code });
        foreach (ComboBoxItem item in LanguageChooser.Items)
            if ((string)item.Tag == original.Language) { LanguageChooser.SelectedItem = item; break; }
        LanguageChooser.SelectedItem ??= LanguageChooser.Items[0];
        SshStatus.Text = privateKey.Length > 0
            ? "Dedicated SSH identity loaded from the EXE." : "Generate a new dedicated SSH identity.";
        UpdateMode();
    }

    private void Mode_Changed(object sender, SelectionChangedEventArgs e) => UpdateMode();
    private void UpdateMode()
    {
        if (SshFields is null || UrlInput is null || UrlHint is null) return;
        bool useSsh = ConnectionChooser.SelectedIndex == 1;
        SshFields.Visibility = useSsh ? Visibility.Visible : Visibility.Collapsed;
        UrlInput.IsEnabled = !useSsh;
        UrlHint.Text = useSsh ? "CDM will open its own localhost tunnel." : "HTTPS must have a trusted certificate.";
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void OpenCoolify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string url = UrlInput.Text.Trim();
            if (url.Length == 0) url = "https://coolify.io/docs/api-reference/authorization";
            else url = CoolifyApi.NormalizeUrl(url);
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Coolify browser", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void GenerateKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Directory.CreateTempSubdirectory("cdm-keygen-").FullName;
            try
            {
                var path = Path.Combine(folder, "identity");
                using var process = new Process { StartInfo = new ProcessStartInfo("ssh-keygen.exe") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true } };
                foreach (var argument in new[] { "-q", "-t", "ed25519", "-f", path,
                    "-N", "", "-C", $"cdm-{Environment.UserName}" })
                    process.StartInfo.ArgumentList.Add(argument);
                process.Start();
                var errors = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode != 0) throw new IOException(errors);
                privateKey = await File.ReadAllTextAsync(path);
                PublicKeyText.Text = (await File.ReadAllTextAsync(path + ".pub")).Trim();
                knownHosts = "";
                SshStatus.Text = "SSH key generated. Authorize the public key on the VPS, then scan its fingerprint.";
            }
            finally { Directory.Delete(folder, recursive: true); }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "SSH key generation", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void CopyPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PublicKeyText.Text))
            Clipboard.SetText(PublicKeyText.Text.Trim());
    }

    private async void ScanHostKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string host = SshAlias.Text.Trim();
            if (host.Length == 0 || host.StartsWith('-') || host.Any(char.IsWhiteSpace))
                throw new ArgumentException("Enter the VPS hostname or IP first.");
            if (!int.TryParse(SshPort.Text.Trim(), out int port) || port is < 1 or > 65535)
                throw new ArgumentException("Invalid SSH port.");
            using var process = new Process { StartInfo = new ProcessStartInfo("ssh-keyscan.exe") {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true } };
            foreach (var arg in new[] { "-T", "5", "-p", port.ToString(), host })
                process.StartInfo.ArgumentList.Add(arg);
            process.Start();
            string stdout = await process.StandardOutput.ReadToEndAsync();
            _ = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !line.StartsWith('#') && line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
                .ToArray();
            if (lines.Length == 0) throw new IOException("SSH host key scan returned no public keys.");
            var selected = lines.FirstOrDefault(line => line.Contains(" ssh-ed25519 ", StringComparison.Ordinal))
                ?? lines[0];
            var parts = selected.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fingerprint = Convert.ToBase64String(SHA256.HashData(Convert.FromBase64String(parts[2]))).TrimEnd('=');
            var message = $"SHA256:{fingerprint}\n\nCompare with the VPS SSH host key fingerprint via an independent trusted channel. On the VPS:\nssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub\n\nDo they match?";
            if (MessageBox.Show(this, message, "Confirm VPS identity", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;
            knownHosts = string.Join("\n", lines);
            SshStatus.Text = "VPS host key pinned. SSH identity can now be verified.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "SSH host verification", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        try
        {
            var mode = ConnectionChooser.SelectedIndex == 1 ? ConnectionMode.ManagedSshTunnel : ConnectionMode.Https;
            var next = original.Copy();
            next.Language = (LanguageChooser.SelectedItem as ComboBoxItem)?.Tag as string ?? "system";
            next.ServerLabel = ServerLabel.Text.Trim();
            next.ConnectionMode = mode;
            next.ServerUrl = UrlInput.Text.Trim();
            next.ReadToken = ReadToken.Password.Trim();
            next.DeployToken = next.ReadToken;
            if (next.ReadToken.Length == 0) throw new ArgumentException("Create one Coolify API token with READ + DEPLOY permissions.");
            if (mode == ConnectionMode.Https) _ = CoolifyApi.NormalizeUrl(next.ServerUrl);
            else
            {
                next.SshHost = SshAlias.Text.Trim();
                next.SshUsername = SshUsername.Text.Trim();
                next.SshPort = int.Parse(SshPort.Text.Trim());
                next.SshLocalPort = int.Parse(LocalPort.Text.Trim());
                next.SshRemoteHost = RemoteHost.Text.Trim();
                next.SshRemotePort = int.Parse(RemotePort.Text.Trim());
                next.SshPrivateKey = privateKey;
                next.SshKnownHosts = knownHosts;
                if (string.IsNullOrWhiteSpace(next.SshHost) || string.IsNullOrWhiteSpace(next.SshUsername)
                    || string.IsNullOrWhiteSpace(next.SshPrivateKey) || string.IsNullOrWhiteSpace(next.SshKnownHosts))
                    throw new ArgumentException("Complete the dedicated SSH key setup and verify the VPS host key first.");
            }
            SaveStatus.Text = "Verifying connection...";
            using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(22));
            await using var tunnel = new SshTunnel();
            if (mode == ConnectionMode.ManagedSshTunnel)
                await tunnel.EnsureRunningAsync(next, ct.Token);
            var baseUrl = CoolifyApi.NormalizeUrl(next.EffectiveUrl);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", next.ReadToken);
            using var response = await http.GetAsync(baseUrl + "/api/v1/version", ct.Token);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Coolify API refused the token (HTTP {(int)response.StatusCode}). Check API Tokens and Allowed API IPs.");
            Result = next;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            SaveStatus.Text = "Connection not verified";
            MessageBox.Show(this, ex.Message, "CDM connection setup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { SaveButton.IsEnabled = true; }
    }
}
