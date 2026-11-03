// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CoolifyDeployManager;

public partial class SettingsWindow : Window
{
    private readonly UserSettings original;
    private readonly SshTunnel wizardTunnel = new();
    private string privateKey = "";
    private string knownHosts = "";
    private string? restrictedVerifiedSignature;
    private DispatcherTimer? manualCopyFeedbackTimer;
    // The administrative account is used only in the server-side manual bootstrap.
    internal bool ShowCompanionGuideOnOpen { get; set; }
    internal UserSettings Result { get; private set; }
    internal bool StartWithWindows => StartWithWindowsCheck.IsChecked == true;

    internal SettingsWindow(UserSettings settings)
    {
        InitializeComponent();
        original = settings.Copy();
        Result = settings.Copy();
        // Local Windows preference, not part of portable credentials/profile.
        StartWithWindowsCheck.IsChecked = WindowsStartup.IsRegisteredForCurrentClone();
        ServerLabel.Text = original.ServerLabel;
        UrlInput.Text = original.ServerUrl;
        ReadToken.Password = original.ReadToken;
        SshHostInput.Text = original.SshHost;
        SshUsername.Text = original.SshUsername;
        SshPort.Text = original.SshPort.ToString();
        LocalPort.Text = original.SshLocalPort.ToString();
        RemotePort.Text = original.SshRemotePort.ToString();
        RemoteHost.Text = original.SshRemoteHost;
        privateKey = original.SshPrivateKey;
        knownHosts = original.SshKnownHosts;
        UpdateManualAuthorizeCommand();
        ConnectionChooser.SelectedIndex = original.ConnectionMode == ConnectionMode.Https ? 0 : 1;
        foreach (var (code, nativeName) in Loc.Languages)
            LanguageChooser.Items.Add(new ComboBoxItem { Content = code == "system" ? Loc.T("SystemLanguage") : nativeName, Tag = code });
        foreach (ComboBoxItem item in LanguageChooser.Items)
            if ((string)item.Tag == original.Language) { LanguageChooser.SelectedItem = item; break; }
        LanguageChooser.SelectedItem ??= LanguageChooser.Items[0];
        SshStatus.Text = privateKey.Length > 0
            ? Loc.T("SetupSshLoaded") : Loc.T("SetupGenerateHint");
        UpdateMode();
        Loaded += ShowSettingsCompanionGuide;
    }

    // Created after ShowDialog begins so both guide and wizard are interactive.
    private void ShowSettingsCompanionGuide(object sender, RoutedEventArgs e)
    {
        if (!ShowCompanionGuideOnOpen) return;
        ShowCompanionGuideOnOpen = false;
        var guide = new GuideWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.Manual };
        // SystemParameters.WorkArea refers to the PRIMARY monitor and was
        // moving Settings off the dashboard's secondary monitor. Resolve
        // the work area of the actual owner HWND (with its DPI transform).
        var reference = Owner ?? this;
        var work = CompanionWindowPlacement.WorkAreaFor(reference);
        var anchor = reference == this || reference.WindowState != WindowState.Normal
            ? new Point(work.Left + work.Width / 2, work.Top + work.Height / 2)
            : new Point(reference.Left + reference.ActualWidth / 2,
                reference.Top + reference.ActualHeight / 2);
        var layout = CompanionWindowPlacement.Arrange(work, anchor,
            new Size(Width, Height), new Size(guide.Width, guide.Height),
            MinWidth, MinHeight, guide.MinWidth, guide.MinHeight);

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = layout.Settings.Left;
        Top = layout.Settings.Top;
        Width = layout.Settings.Width;
        Height = layout.Settings.Height;
        guide.Left = layout.Guide.Left;
        guide.Top = layout.Guide.Top;
        guide.Width = layout.Guide.Width;
        guide.Height = layout.Guide.Height;
        guide.Show();
        Activate();
    }

    private void Mode_Changed(object sender, SelectionChangedEventArgs e) => UpdateMode();
    private void UrlInput_TextChanged(object sender, TextChangedEventArgs e) => UpdateMode();

    // An HTTP VPS endpoint is reachable, but not suitable for sending bearer tokens
    // directly over the public Internet. Offer the verified SSH forwarding route.
    private static bool TryGetRemoteHttp(string input, out Uri? endpoint)
    {
        endpoint = null;
        var value = input.Trim();
        if (value.Length == 0) return false;
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            // Accept common address-bar input such as 203.0.113.42:8000.
            if (!value.Contains(':')) return false;
            value = "http://" + value;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            parsed.Scheme != Uri.UriSchemeHttp || CoolifyApi.IsLocalHttpUri(parsed) ||
            parsed.Host.Length == 0 || parsed.UserInfo.Length > 0)
            return false;
        endpoint = parsed;
        return true;
    }

    private void UseSshForUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetRemoteHttp(UrlInput.Text, out var endpoint) || endpoint is null) return;
        SshHostInput.Text = endpoint.Host;
        RemotePort.Text = endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        RemoteHost.Text = "127.0.0.1";
        ConnectionChooser.SelectedIndex = 1;
        SshStatus.Text = Loc.T("SetupSshPrefilled");
    }

    private void UpdateMode()
    {
        if (SshFields is null || UrlInput is null || UrlHint is null ||
            PublicHttpPanel is null || ConnectionChooser is null) return;
        bool useSsh = ConnectionChooser.SelectedIndex == 1;
        SshFields.Visibility = useSsh ? Visibility.Visible : Visibility.Collapsed;
        PublicHttpPanel.Visibility = !useSsh && TryGetRemoteHttp(UrlInput.Text, out _)
            ? Visibility.Visible : Visibility.Collapsed;
        UrlInput.IsEnabled = !useSsh;
        UrlHint.Text = useSsh ? Loc.T("SetupSshHint") : Loc.T("SetupHttpsHint");
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    protected override void OnClosed(EventArgs e)
    {
        manualCopyFeedbackTimer?.Stop();
        wizardTunnel.Stop();
        base.OnClosed(e);
    }

    // For private installations the browser also travels through the dedicated SSH tunnel.
    private async void OpenCoolify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string url;
            if (ConnectionChooser.SelectedIndex == 1)
            {
                var connection = BuildSshConnection();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await wizardTunnel.EnsureRunningAsync(connection, timeout.Token);
                url = connection.EffectiveUrl;
            }
            else
            {
                url = UrlInput.Text.Trim();
                if (url.Length == 0)
                    throw new ArgumentException("Enter the Coolify HTTPS dashboard URL first.");
                url = CoolifyApi.NormalizeUrl(url);
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Coolify browser", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private UserSettings BuildSshConnection()
    {
        var connection = original.Copy();
        connection.ConnectionMode = ConnectionMode.ManagedSshTunnel;
        connection.SshHost = SshHostInput.Text.Trim();
        connection.SshUsername = SshUsername.Text.Trim();
        connection.SshPort = int.Parse(SshPort.Text.Trim());
        connection.SshLocalPort = int.Parse(LocalPort.Text.Trim());
        connection.SshRemoteHost = RemoteHost.Text.Trim();
        connection.SshRemotePort = int.Parse(RemotePort.Text.Trim());
        connection.SshPrivateKey = privateKey;
        connection.SshKnownHosts = knownHosts;
        if (string.IsNullOrWhiteSpace(connection.SshPrivateKey) ||
            string.IsNullOrWhiteSpace(connection.SshKnownHosts))
            throw new ArgumentException("Generate and authorize the SSH key, then verify the VPS host key.");
        return connection;
    }


    private void BootstrapInputsChanged(object sender, TextChangedEventArgs e)
    {
        ResetManualCopyFeedback();
        restrictedVerifiedSignature = null;
        UpdateManualAuthorizeCommand();
    }

    private string CurrentBootstrapSignature()
    {
        var material = string.Join("|", SshHostInput.Text.Trim(), SshUsername.Text.Trim(),
            SshPort.Text.Trim(), LocalPort.Text.Trim(), RemotePort.Text.Trim(),
            RemoteHost.Text.Trim(), privateKey, knownHosts);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private bool OriginalRestrictedProfileUnchanged()
    {
        return original.RestrictedSshProvisioned &&
            original.SshUsername == SshUsername.Text.Trim() &&
            original.SshHost == SshHostInput.Text.Trim() &&
            original.SshPort.ToString() == SshPort.Text.Trim() &&
            original.SshLocalPort.ToString() == LocalPort.Text.Trim() &&
            original.SshRemotePort == int.Parse(RemotePort.Text.Trim()) &&
            original.SshRemoteHost == RemoteHost.Text.Trim() &&
            original.SshPrivateKey == privateKey && original.SshKnownHosts == knownHosts;
    }

    // The manual installer must have run first if no alias was available.
    // Only an authenticated live tunnel PLUS rejected remote command earns
    // the 'restricted verified' marker; a pasted success message is not trusted.
    private async void VerifyRestricted_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var username = SshUsername.Text.Trim();
            var restricted = username.EndsWith("-cdm", StringComparison.Ordinal)
                ? username : SshBootstrap.RestrictedUser(username);
            var candidate = BuildSshConnection();
            candidate.SshUsername = restricted;
            using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await wizardTunnel.EnsureRunningAsync(candidate, ct.Token);
            await SshBootstrap.VerifyRemoteShellBlockedAsync(candidate, ct.Token);
            SshUsername.Text = restricted;
            restrictedVerifiedSignature = CurrentBootstrapSignature();
            SshStatus.Text = Loc.T("V3RestrictedVerified");
        }
        catch (Exception ex)
        {
            restrictedVerifiedSignature = null;
            MessageBox.Show(this, ex.Message, "CDM v3 verification",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
                restrictedVerifiedSignature = null;
                PublicKeyText.Text = (await File.ReadAllTextAsync(path + ".pub")).Trim();
                UpdateManualAuthorizeCommand();
                knownHosts = "";
                SshStatus.Text = Loc.T("SetupKeyGenerated");
            }
            finally { Directory.Delete(folder, recursive: true); }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "SSH key generation", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }


    private void UpdateManualAuthorizeCommand()
    {
        if (ManualAuthorizeCommand is null || PublicKeyText is null ||
            SshUsername is null || RemoteHost is null || RemotePort is null) return;
        try
        {
            var script = SshBootstrap.BuildScript(SshUsername.Text, PublicKeyText.Text,
                RemoteHost.Text.Trim(), int.Parse(RemotePort.Text.Trim()));
            ManualAuthorizeCommand.Text = SshBootstrap.ManualCommand(script);
            if (ManualCopyButton is not null) ManualCopyButton.IsEnabled = true;
        }
        catch (Exception)
        {
            ManualAuthorizeCommand.Text = "";
            if (ManualCopyButton is not null) ManualCopyButton.IsEnabled = false;
        }
    }

    private void ResetManualCopyFeedback()
    {
        manualCopyFeedbackTimer?.Stop();
        manualCopyFeedbackTimer = null;
        if (ManualCopyButton is null) return;
        ManualCopyButton.Content = Loc.T("SetupCopyManualAuthorize");
        ManualCopyButton.ClearValue(BackgroundProperty);
    }

    // The command is copied exactly as generated; the animation is a pure
    // presentation effect, with no provisioning or SSH actions.
    private void ShowManualCopyFeedback()
    {
        ResetManualCopyFeedback();
        ManualCopyButton.Content = Loc.T("V3ManualCommandCopied");
        ManualCopyButton.Background = new SolidColorBrush(Color.FromRgb(29, 117, 93));
        ManualCopyButton.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = ManualCopyButton.RenderTransform as ScaleTransform;
        if (scale is null)
        {
            scale = new ScaleTransform(1.0, 1.0);
            ManualCopyButton.RenderTransform = scale;
        }
        var pulse = new DoubleAnimation(1.0, 1.07, TimeSpan.FromMilliseconds(170))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        SshStatus.Text = Loc.T("V3ManualCommandCopyFeedback");
        manualCopyFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        manualCopyFeedbackTimer.Tick += (_, _) => ResetManualCopyFeedback();
        manualCopyFeedbackTimer.Start();
    }

    // Smoke must not touch Windows Clipboard or a real SSH server.
    internal void VerifyManualCopyFeedbackForSmoke()
    {
        ShowManualCopyFeedback();
        if (!Equals(ManualCopyButton.Content, Loc.T("V3ManualCommandCopied")) ||
            ManualCopyButton.RenderTransform is not ScaleTransform ||
            manualCopyFeedbackTimer?.IsEnabled != true ||
            !SshStatus.Text.Equals(Loc.T("V3ManualCommandCopyFeedback"), StringComparison.Ordinal))
            throw new InvalidDataException("The VPS copy button failed to display its animated confirmation.");
        ResetManualCopyFeedback();
        if (!Equals(ManualCopyButton.Content, Loc.T("SetupCopyManualAuthorize")) ||
            manualCopyFeedbackTimer is not null)
            throw new InvalidDataException("The VPS copy button did not restore its normal state.");
    }

    private void CopyManualCommand_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateManualAuthorizeCommand();
            if (string.IsNullOrWhiteSpace(ManualAuthorizeCommand.Text)) return;
            Clipboard.SetText(ManualAuthorizeCommand.Text);
            ShowManualCopyFeedback();
        }
        catch (Exception ex)
        {
            ResetManualCopyFeedback();
            MessageBox.Show(this, ex.Message, "CDM v3 clipboard",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyPublicKey_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PublicKeyText.Text))
            Clipboard.SetText(PublicKeyText.Text.Trim());
    }

    // Windows OpenSSH ssh-keyscan.exe may advertise a post-quantum KEX which
    // that build cannot actually negotiate. ssh.exe is not affected by this bug.
    // Capture a host key during its unauthenticated handshake in a disposable
    // known_hosts file. The expected SSH authentication failure is irrelevant.
    private static async Task<string[]> DiscoverHostKeysAsync(string host, int port)
    {
        string folder = Directory.CreateTempSubdirectory("cdm-hostkey-").FullName;
        try
        {
            var file = Path.Combine(folder, "known_hosts");
            var start = new ProcessStartInfo("ssh.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            foreach (var arg in new[]
            {
                "-F", "none",
                "-o", "StrictHostKeyChecking=accept-new",
                "-o", $"UserKnownHostsFile={file}",
                "-o", "GlobalKnownHostsFile=none",
                "-o", "HashKnownHosts=no",
                "-o", "UpdateHostKeys=no",
                "-o", "BatchMode=yes",
                "-o", "PreferredAuthentications=none",
                "-o", "NumberOfPasswordPrompts=0",
                "-o", "ConnectTimeout=8",
                "-T",
                "-p", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "-l", "cdm-host-key-probe",
                host
            }) start.ArgumentList.Add(arg);

            using var process = Process.Start(start)
                ?? throw new IOException("Unable to start Windows OpenSSH (ssh.exe).");
            var errors = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(14));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new TimeoutException("SSH host-key probe timed out.");
            }
            var stderr = await errors;
            if (!File.Exists(file))
                throw new IOException("Unable to obtain the SSH server host key: " + stderr.Trim());

            var lines = (await File.ReadAllLinesAsync(file))
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#') &&
                    line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
                .ToArray();
            if (lines.Length == 0)
                throw new IOException("SSH handshake completed without saving a host key: " + stderr.Trim());
            return lines;
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    // A standard MessageBox is not discoverably copyable. The verification
    // dialog makes both the fingerprint and the VPS console command selectable
    // and gives each its own explicit copy action.
    private bool ConfirmHostFingerprint(string algorithm, string fingerprint, string command)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = Loc.T("SetupVerifyHostTitle"),
            Width = 680,
            Height = 425,
            MinWidth = 570,
            MinHeight = 410,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        var root = new DockPanel { Margin = new Thickness(24) };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        DockPanel.SetDock(actions, Dock.Bottom);
        var cancel = new Button
        {
            Content = Loc.T("SetupHostNoMatch"),
            Style = (Style)Application.Current.FindResource("ActionButton"),
            MinWidth = 110,
            IsCancel = true,
            Margin = new Thickness(0, 0, 12, 0)
        };
        cancel.Click += (_, _) => dialog.DialogResult = false;
        var accept = new Button
        {
            Content = Loc.T("SetupHostMatches"),
            Style = (Style)Application.Current.FindResource("ActionButton"),
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("Purple"),
            MinWidth = 150
        };
        accept.Click += (_, _) => dialog.DialogResult = true;
        actions.Children.Add(cancel);
        actions.Children.Add(accept);
        root.Children.Add(actions);

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = Loc.T("SetupVerifyHostIntro"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        AddCopyableVerificationRow(body,
            Loc.T("SetupVerifyHostFingerprint") + " (" + algorithm + ")",
            fingerprint);
        body.Children.Add(new TextBlock
        {
            Text = Loc.T("SetupVerifyHostInstructions"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 12)
        });
        AddCopyableVerificationRow(body, Loc.T("SetupVerifyHostCommand"), command);
        root.Children.Add(body);
        dialog.Content = root;
        return dialog.ShowDialog() == true;
    }

    private static void AddCopyableVerificationRow(StackPanel parent, string label, string value)
    {
        parent.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 5)
        });
        var line = new DockPanel { LastChildFill = true };
        var copy = new Button
        {
            Content = Loc.T("Copy"),
            Style = (Style)Application.Current.FindResource("ActionButton"),
            MinWidth = 92,
            Margin = new Thickness(10, 0, 0, 0)
        };
        DockPanel.SetDock(copy, Dock.Right);
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(value);
            copy.Content = Loc.T("SetupHostCopied");
        };
        line.Children.Add(copy);
        line.Children.Add(new TextBox
        {
            Text = value,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = 40,
            ToolTip = Loc.T("SetupVerifyHostCopyHint")
        });
        parent.Children.Add(line);
    }

    private async void ScanHostKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string host = SshHostInput.Text.Trim();
            if (host.Length == 0 || host.StartsWith('-') || host.Any(char.IsWhiteSpace))
                throw new ArgumentException("Enter the VPS hostname or IP first.");
            if (!int.TryParse(SshPort.Text.Trim(), out int port) || port is < 1 or > 65535)
                throw new ArgumentException("Invalid SSH port.");
            var lines = await DiscoverHostKeysAsync(host, port);
            var selected = lines.FirstOrDefault(line => line.Contains(" ssh-ed25519 ", StringComparison.Ordinal))
                ?? lines[0];
            var parts = selected.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fingerprint = Convert.ToBase64String(
                SHA256.HashData(Convert.FromBase64String(parts[2]))).TrimEnd('=');
            var hostKeyPath = parts[1] switch
            {
                "ssh-ed25519" => "/etc/ssh/ssh_host_ed25519_key.pub",
                var k when k.StartsWith("ecdsa-", StringComparison.Ordinal) =>
                    "/etc/ssh/ssh_host_ecdsa_key.pub",
                _ => "/etc/ssh/ssh_host_rsa_key.pub"
            };
            var fullFingerprint = "SHA256:" + fingerprint;
            var command = $"ssh-keygen -lf {hostKeyPath} -E sha256";
            if (!ConfirmHostFingerprint(parts[1], fullFingerprint, command)) return;
            knownHosts = string.Join("\n", lines);
            restrictedVerifiedSignature = null;
            SshStatus.Text = Loc.T("SetupHostPinned");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "SSH host verification",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
            if (mode == ConnectionMode.Https)
                _ = CoolifyApi.NormalizeUrl(next.ServerUrl);
            else
            {
                var tunnelSettings = BuildSshConnection();
                next.SshHost = tunnelSettings.SshHost;
                next.SshUsername = tunnelSettings.SshUsername;
                next.SshPort = tunnelSettings.SshPort;
                next.SshLocalPort = tunnelSettings.SshLocalPort;
                next.SshRemoteHost = tunnelSettings.SshRemoteHost;
                next.SshRemotePort = tunnelSettings.SshRemotePort;
                next.SshPrivateKey = tunnelSettings.SshPrivateKey;
                next.SshKnownHosts = tunnelSettings.SshKnownHosts;
                // Never allow the new v3 clone to carry an unverified SSH identity.
                if (restrictedVerifiedSignature != CurrentBootstrapSignature() &&
                    !OriginalRestrictedProfileUnchanged())
                    throw new IOException(Loc.T("V3RestrictionRequired"));
                next.RestrictedSshProvisioned = true;
                if (string.IsNullOrWhiteSpace(next.SshHost) || string.IsNullOrWhiteSpace(next.SshUsername)
                    || string.IsNullOrWhiteSpace(next.SshPrivateKey) || string.IsNullOrWhiteSpace(next.SshKnownHosts))
                    throw new ArgumentException("Complete the dedicated SSH key setup and verify the VPS host key first.");
            }
            SaveStatus.Text = Loc.T("SetupChecking");
            using var ct = new CancellationTokenSource(TimeSpan.FromSeconds(22));
            if (mode == ConnectionMode.ManagedSshTunnel)
                await wizardTunnel.EnsureRunningAsync(next, ct.Token);
            var baseUrl = CoolifyApi.NormalizeUrl(next.EffectiveUrl);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", next.ReadToken);
            using var response = await http.GetAsync(baseUrl + "/api/v1/teams/current", ct.Token);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(Loc.T("V3TokenInvalid"));
            Result = next;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            SaveStatus.Text = Loc.T("SetupCheckFailed");
            MessageBox.Show(this, ex.Message, "CDM connection setup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { SaveButton.IsEnabled = true; }
    }
}
