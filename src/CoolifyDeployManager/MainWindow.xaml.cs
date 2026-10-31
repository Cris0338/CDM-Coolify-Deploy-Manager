// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace CoolifyDeployManager;

public partial class MainWindow : Window
{
    private UserSettings settings = new();
    private readonly SshTunnel ssh = new();
    private CoolifyInventory inventory = new(Array.Empty<CoolifyProject>(), Array.Empty<CoolifyApplication>());
    private readonly Dictionary<string, AppCardView> allCards = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer clock;
    private CancellationTokenSource? operation;
    private bool busy;
    private bool rebuilding;
    private string selectedProjectUuid = "ALL";
    private readonly List<ProjectChoice> projectChoices = new();
    private Point dragOrigin;
    private string? dragUuid;

    internal sealed record ProjectChoice(string Uuid, string Name);

    public MainWindow()
    {
        InitializeComponent();
        try { settings = SettingsStore.Load(); }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.T("CouldNotLoad") + "\n\n" + ex.Message,
                Loc.T("Configuration"), MessageBoxButton.OK, MessageBoxImage.Warning);
            settings = new();
        }
        ssh.StatusChanged += s => Dispatcher.BeginInvoke(new Action(() => AddLog(s)));
        clock = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        clock.Tick += (_, _) => { foreach (var card in allCards.Values) card.UpdateClock(); CheckTunnelState(); };
        clock.Start();
        ProjectPickerButton.Content = Loc.T("AllProjects");
        LogBox.Text = "";
        StageCount.Text = Loc.F("ApplicationsCount", 0);
        ProductionCount.Text = Loc.F("ApplicationsCount", 0);
        UnknownCount.Text = Loc.F("ApplicationsCount", 0);
        InventoryCount.Text = Loc.F("ApplicationsCount", 0);
        UpdateProfileStatus();
        AddLog("Coolify Deploy Manager WPF 2.2.0 · Startup.");
    }

    private void UpdateProfileStatus()
    {
        var status = Loc.T(SettingsStore.HasPortableProfile && settings.IsConfigured
            ? "VaultConfigured" : "VaultNotConfigured");
        Title = Loc.T("AppTitle") + " 2.2.0 · " + status;
        ProfileBadge.Text = status;
        if (SettingsStore.HasPortableProfile && settings.IsConfigured)
        {
            string user = settings.ConnectionMode == ConnectionMode.ManagedSshTunnel
                ? settings.SshUsername : Environment.UserName;
            string server = settings.ConnectionMode == ConnectionMode.ManagedSshTunnel
                ? settings.SshHost : settings.ServerUrl;
            ProfileIdentity.Text = Loc.F("ProfileIdentity", user, server);
            ProfileIdentity.Visibility = Visibility.Visible;
        }
        else
        {
            ProfileIdentity.Text = "";
            ProfileIdentity.Visibility = Visibility.Collapsed;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (settings.IsConfigured) await RefreshAsync();
        else _ = Dispatcher.BeginInvoke(new Action(() => OpenSettings()));
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        operation?.Cancel();
        clock.Stop();
        ssh.Stop();
    }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
    }
    private void AddLog(string message)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(() => AddLog(message))); return; }
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        LogBox.AppendText(line);
        if (LogBox.Text.Length > 150_000) LogBox.Text = LogBox.Text[^120_000..];
        LogBox.ScrollToEnd();
    }
    private void SetConnection(bool ok, string text)
    {
        ConnectionDot.Fill = (Brush)new BrushConverter().ConvertFromString(ok ? "#3AC994" : "#C94858")!;
        ConnectionText.Text = text;
    }
    private void CheckTunnelState()
    {
        if (settings.ConnectionMode == ConnectionMode.ManagedSshTunnel && !ssh.IsRunning && !busy)
            SetConnection(false, Loc.T("TunnelStopped"));
    }
    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (settings.ConnectionMode == ConnectionMode.ManagedSshTunnel)
        {
            await ssh.EnsureRunningAsync(settings, ct);
        }
        else if (settings.ConnectionMode == ConnectionMode.ExternalLocalTunnel)
        {
            if (!await SshTunnel.HealthCheckAsync(settings.SshLocalPort, ct))
                throw new IOException(Loc.F("ExternalTunnelDown", settings.SshLocalPort));
        }
        else
        {
            var url = CoolifyApi.NormalizeUrl(settings.ServerUrl);
            // Always validate TLS certificates; never bypass trust checks.
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            try
            {
                using var res = await http.GetAsync(url + "/api/health", ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                if (!res.IsSuccessStatusCode || !body.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase))
                    throw new IOException(Loc.F("HealthHttpStatus", (int)res.StatusCode));
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new IOException(Loc.T("HttpsTimeout"), ex);
            }
            catch (HttpRequestException ex) when (HasTlsError(ex))
            {
                throw new IOException(Loc.T("HttpsCertificateInvalid"), ex);
            }
            catch (HttpRequestException ex)
            {
                throw new IOException(Loc.F("HttpsConnectionFailed", ex.Message), ex);
            }
        }
        SetConnection(true, settings.ConnectionMode switch
        {
            ConnectionMode.ManagedSshTunnel => Loc.T("ManagedConnected"),
            ConnectionMode.ExternalLocalTunnel => Loc.T("ExternalConnected"),
            _ => Loc.T("HttpsConnected")
        });
    }
    private static bool HasTlsError(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
            if (current is AuthenticationException) return true;
        return false;
    }
    private UserSettings EffectiveSettings()
    {
        var s = settings.Copy();
        s.ServerUrl = s.EffectiveUrl;
        return s;
    }
    private async Task RefreshAsync()
    {
        if (busy) { OperationText.Text = Loc.T("AlreadyBusy"); return; }
        if (!settings.IsConfigured) { OpenSettings(); return; }
        busy = true;
        RefreshButton.IsEnabled = false;
        OperationText.Text = Loc.T("LoadingInventory");
        using var ct = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await EnsureConnectionAsync(ct.Token);
            AddLog(Loc.T("ReadingInventory"));
            using var api = new CoolifyApi(EffectiveSettings());
            var next = await api.GetInventoryAsync(ct.Token);
            var latest = await api.GetLatestDeploysAsync(next.Applications, ct.Token);
            inventory = next;
            allCards.Clear();
            foreach (var app in inventory.Applications)
            {
                var group = Classifier.Classify(app, settings);
                var card = new AppCardView(app, group)
                {
                    PositionLocked = settings.LockedPositions.Contains(app.Uuid),
                    DeployLocked = settings.BlockedDeploys.Contains(app.Uuid)
                };
                latest.Values.TryGetValue(app.Uuid, out var info);
                card.UpdateDeploy(info);
                allCards[app.Uuid] = card;
                if (group == DeploymentGroup.Unknown) AddLog(Loc.F("UnclassifiedExplanation", Classifier.Explain(app, group)));
            }
            RebuildNavigation();
            RenderCards();
            AddLog(Loc.F("InventoryLog", next.Projects.Count, next.Applications.Count, latest.Unavailable));
            OperationText.Text = Loc.F("InventoryUpdated", next.Applications.Count, DateTime.Now.ToString("HH:mm"));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            SetConnection(false, Loc.T("NotConnected"));
            AddLog(Loc.T("ErrorPrefix") + ex.Message);
            OperationText.Text = Loc.T("ConnectionError");
        }
        finally { busy = false; RefreshButton.IsEnabled = true; }
    }
    private void RebuildNavigation()
    {
        projectChoices.Clear();
        projectChoices.Add(new("ALL", Loc.T("AllProjects")));
        projectChoices.AddRange(inventory.Projects.Select(p => new ProjectChoice(p.Uuid, p.Name)));
        if (!projectChoices.Any(c => c.Uuid == selectedProjectUuid)) selectedProjectUuid = "ALL";
        ProjectPickerButton.Content = projectChoices.First(x => x.Uuid == selectedProjectUuid).Name +
            "  ▾";
        FilterProjectChoices();
    }

    private void FilterProjectChoices()
    {
        var search = ProjectSearchBox?.Text?.Trim() ?? "";
        ProjectPickerList.ItemsSource = projectChoices
            .Where(x => search.Length == 0 || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void ChangeProject(string uuid)
    {
        if (rebuilding) return;
        selectedProjectUuid = uuid;
        var choice = projectChoices.FirstOrDefault(x => x.Uuid == uuid);
        ProjectPickerButton.Content = (choice?.Name ?? Loc.T("AllProjects")) + "  ▾";
        ProjectPickerPopup.IsOpen = false;
        RenderCards();
    }

    private void ProjectPicker_Click(object sender, RoutedEventArgs e)
    {
        ProjectSearchBox.Text = "";
        ProjectPickerPopup.IsOpen = !ProjectPickerPopup.IsOpen;
        if (ProjectPickerPopup.IsOpen)
            Dispatcher.BeginInvoke(new Action(() => { ProjectSearchBox.Focus(); Keyboard.Focus(ProjectSearchBox); }));
    }
    private void ProjectSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ProjectPickerList is not null) FilterProjectChoices();
    }
    private void ProjectPickerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectPickerList.SelectedItem is not ProjectChoice choice) return;
        ProjectPickerList.SelectedItem = null;
        ChangeProject(choice.Uuid);
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) RenderCards(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) RenderCards(); }
    private string OrderKey(DeploymentGroup group) => selectedProjectUuid + "|" + group;
    private IEnumerable<AppCardView> FilterAndOrder(DeploymentGroup group)
    {
        var text = SearchBox.Text?.Trim() ?? "";
        var status = StatusFilter.SelectedIndex;
        var matches = allCards.Values.Where(c => c.Group == group &&
            (selectedProjectUuid == "ALL" || c.App.ProjectUuid == selectedProjectUuid) &&
            (text.Length == 0 || c.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             c.Branch.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             c.Environment.Contains(text, StringComparison.OrdinalIgnoreCase)) &&
            (status == 0 || status == 1 && c.Running || status == 2 && c.HasProblem || status == 3 && group == DeploymentGroup.Unknown));
        var order = settings.CardOrders.TryGetValue(OrderKey(group), out var list) ? list : new List<string>();
        return matches.OrderBy(c => { var index = order.FindIndex(x => x.Equals(c.Uuid, StringComparison.OrdinalIgnoreCase)); return index < 0 ? int.MaxValue : index; })
                      .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    private void RenderCards()
    {
        if (!IsLoaded) return;
        var stage = FilterAndOrder(DeploymentGroup.Staging).ToList();
        var prod = FilterAndOrder(DeploymentGroup.Production).ToList();
        var unknown = FilterAndOrder(DeploymentGroup.Unknown).ToList();
        StagingItems.ItemsSource = stage;
        ProductionItems.ItemsSource = prod;
        UnknownItems.ItemsSource = unknown;
        StageSection.Visibility = stage.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        ProductionSection.Visibility = prod.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        UnknownSection.Visibility = unknown.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        StageCount.Text = Loc.F("ApplicationsCount", stage.Count);
        ProductionCount.Text = Loc.F("ApplicationsCount", prod.Count);
        UnknownCount.Text = Loc.F("ApplicationsCount", unknown.Count);
        InventoryCount.Text = Loc.F("VisibleCount", stage.Count + prod.Count + unknown.Count, inventory.Applications.Count);
        EmptyLabel.Visibility = stage.Count + prod.Count + unknown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Card_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Border card) return;
        card.Opacity = 0;
        var shift = new TranslateTransform(0, 12);
        card.RenderTransform = shift;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1,
            TimeSpan.FromMilliseconds(210)) { EasingFunction = ease });
        shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0,
            TimeSpan.FromMilliseconds(210)) { EasingFunction = ease });
    }

    private void Grip_Down(object sender, MouseButtonEventArgs e)
    {
        dragOrigin = e.GetPosition(this);
        dragUuid = (sender as FrameworkElement)?.DataContext is AppCardView c && !c.PositionLocked ? c.Uuid : null;
    }
    private void Grip_Move(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || dragUuid is null || busy) return;
        if (SearchBox.Text.Length > 0 || StatusFilter.SelectedIndex != 0) return;
        var diff = e.GetPosition(this) - dragOrigin;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var uuid = dragUuid;
        dragUuid = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("CDM_CardUuid", uuid), DragDropEffects.Move);
    }
    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (busy || SearchBox.Text.Length > 0 || StatusFilter.SelectedIndex != 0 || !e.Data.GetDataPresent("CDM_CardUuid")) return;
        e.Handled = true; // Do not treat a rejected drop on a locked card as an empty-area drop.
        if (e.Data.GetData("CDM_CardUuid") is not string sourceUuid || (sender as FrameworkElement)?.DataContext is not AppCardView destination) return;
        if (!allCards.TryGetValue(sourceUuid, out var source) || source.Group != destination.Group ||
            source.PositionLocked || destination.PositionLocked || sourceUuid == destination.Uuid) return;
        if (selectedProjectUuid != "ALL" && source.App.ProjectUuid != selectedProjectUuid) return;
        var order = FilterAndOrder(destination.Group).Select(c => c.Uuid).ToList();
        order.Remove(sourceUuid);
        var position = order.IndexOf(destination.Uuid);
        if (position < 0) return;
        order.Insert(position, sourceUuid);
        var key = OrderKey(destination.Group);
        settings.CardOrders.TryGetValue(key, out var previous);
        settings.CardOrders[key] = order;
        try { SettingsStore.Save(settings); }
        catch (Exception ex)
        {
            if (previous is null) settings.CardOrders.Remove(key);
            else settings.CardOrders[key] = previous;
            MessageBox.Show(this, ex.Message, Loc.T("VaultSaveFailed"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        RenderCards();
        AddLog(Loc.T("OrderSaved"));
    }
    private void EmptyArea_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        if (busy || SearchBox.Text.Length > 0 || StatusFilter.SelectedIndex != 0 ||
            !e.Data.GetDataPresent("CDM_CardUuid")) return;
        if (sender is not FrameworkElement element ||
            !Enum.TryParse<DeploymentGroup>(element.Tag as string, out var group)) return;
        if (e.Data.GetData("CDM_CardUuid") is string uuid &&
            allCards.TryGetValue(uuid, out var card) &&
            card.Group == group && !card.PositionLocked)
            e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void EmptyArea_Drop(object sender, DragEventArgs e)
    {
        if (busy || SearchBox.Text.Length > 0 || StatusFilter.SelectedIndex != 0 ||
            !e.Data.GetDataPresent("CDM_CardUuid")) return;
        if (sender is not FrameworkElement element ||
            !Enum.TryParse<DeploymentGroup>(element.Tag as string, out var group) ||
            e.Data.GetData("CDM_CardUuid") is not string uuid ||
            !allCards.TryGetValue(uuid, out var card) ||
            card.PositionLocked || card.Group != group) return;
        e.Handled = true;
        // Drop on an unused slot in this section appends within the same group.
        var order = FilterAndOrder(group).Select(c => c.Uuid).ToList();
        order.Remove(uuid);
        order.Add(uuid);
        var key = OrderKey(group);
        settings.CardOrders.TryGetValue(key, out var previous);
        settings.CardOrders[key] = order;
        try { SettingsStore.Save(settings); }
        catch (Exception ex)
        {
            if (previous is null) settings.CardOrders.Remove(key);
            else settings.CardOrders[key] = previous;
            MessageBox.Show(this, ex.Message, Loc.T("VaultSaveFailed"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        RenderCards();
        AddLog(Loc.T("OrderSaved"));
    }

    private void TogglePositionLock_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is not AppCardView card) return;
        var old = card.PositionLocked;
        card.PositionLocked = !old;
        if (card.PositionLocked) settings.LockedPositions.Add(card.Uuid);
        else settings.LockedPositions.Remove(card.Uuid);
        try { SettingsStore.Save(settings); }
        catch (Exception ex)
        {
            card.PositionLocked = old;
            if (old) settings.LockedPositions.Add(card.Uuid);
            else settings.LockedPositions.Remove(card.Uuid);
            MessageBox.Show(this, ex.Message, Loc.T("VaultSaveFailed"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ToggleDeployLock_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is not AppCardView card) return;
        var old = card.DeployLocked;
        card.DeployLocked = !old;
        if (card.DeployLocked) { settings.BlockedDeploys.Add(card.Uuid); card.IsSelected = false; }
        else settings.BlockedDeploys.Remove(card.Uuid);
        try { SettingsStore.Save(settings); }
        catch (Exception ex)
        {
            card.DeployLocked = old;
            if (old) settings.BlockedDeploys.Add(card.Uuid);
            else settings.BlockedDeploys.Remove(card.Uuid);
            MessageBox.Show(this, ex.Message, Loc.T("VaultSaveFailed"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private async void OpenSettings()
    {
        if (busy) { MessageBox.Show(this, Loc.T("WaitOperation")); return; }
        ssh.Stop(); // The wizard may need the same local tunnel port.
        var dlg = new SettingsWindow(settings) { Owner = this };
        if (dlg.ShowDialog() != true) return;
        try { SettingsStore.Save(dlg.Result); }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("VaultSaveFailed"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SettingsStore.LaunchedClone) return;
        ssh.Stop();
        settings = dlg.Result;
        Loc.Apply(settings.Language);
        UpdateProfileStatus();
        foreach (var app in inventory.Applications)
        {
            var card = new AppCardView(app, Classifier.Classify(app, settings))
            {
                PositionLocked = settings.LockedPositions.Contains(app.Uuid),
                DeployLocked = settings.BlockedDeploys.Contains(app.Uuid)
            };
            if (allCards.TryGetValue(app.Uuid, out var former))
            {
                card.UpdateDeploy(former.Latest);
                card.IsSelected = former.IsSelected;
            }
            allCards[app.Uuid] = card;
        }
        RebuildNavigation();
        RenderCards();
        AddLog(Loc.T("SettingsSaved"));
        SetConnection(false, Loc.T("NeedsCheck"));
        await RefreshAsync();
    }
    private void Guide_Click(object sender, RoutedEventArgs e) => new GuideWindow { Owner = this }.ShowDialog();
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void ToggleLog_Click(object sender, RoutedEventArgs e) => LogPanel.Visibility = LogPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    private void CopyLog_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(LogBox.Text)) Clipboard.SetText(LogBox.Text); }
    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Clear();
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        AddLog(Loc.T("LocalQueueCancelled"));
        operation?.Cancel();
    }
    private async void Deploy_Click(object sender, RoutedEventArgs e)
    {
        if (busy || (sender as FrameworkElement)?.Tag is not AppCardView card) return;
        if (card.DeployLocked) return;
        var title = Loc.T(card.Group == DeploymentGroup.Production ? "ConfirmationProd" : "ConfirmationTitle");
        var text = Loc.F("ConfirmationText", card.Name, card.App.Project, card.App.Environment, card.Branch).Replace("\\n", "\n");
        if (card.Group == DeploymentGroup.Unknown)
        {
            MessageBox.Show(this, Loc.T("UnknownDeployWarning"),
                Loc.T("UnknownDeployTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show(this, text, title, MessageBoxButton.YesNo,
                card.Group == DeploymentGroup.Production ? MessageBoxImage.Warning : MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await DeployQueueAsync(new[] { card });
    }
    private async void Batch_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !settings.IsConfigured) return;
        var selected = allCards.Values.Where(c => c.Group == DeploymentGroup.Staging &&
            c.IsSelected && !c.DeployLocked &&
            (selectedProjectUuid == "ALL" || c.App.ProjectUuid == selectedProjectUuid))
            .OrderBy(c => c.App.Project).ThenBy(c => c.Name).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, Loc.T("NoSelectedStaging")); return; }
        await ConfirmAndDeployStagingAsync(selected);
    }

    private async void BatchAll_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !settings.IsConfigured) return;
        var all = allCards.Values.Where(c => c.Group == DeploymentGroup.Staging &&
            !c.DeployLocked &&
            (selectedProjectUuid == "ALL" || c.App.ProjectUuid == selectedProjectUuid))
            .OrderBy(c => c.App.Project).ThenBy(c => c.Name).ToList();
        if (all.Count == 0) { MessageBox.Show(this, Loc.T("NoStage")); return; }
        await ConfirmAndDeployStagingAsync(all);
    }

    private async Task ConfirmAndDeployStagingAsync(List<AppCardView> selected)
    {
        var projectLabel = selectedProjectUuid == "ALL"
            ? Loc.T("AllProjectsUpper") : selected.First().App.Project;
        if (MessageBox.Show(this,
            Loc.F("BatchConfirm", selected.Count, projectLabel).Replace("\\n", "\n"),
            Loc.T("BatchConfirmTitle"), MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await DeployQueueAsync(selected);
    }

    private async Task DeployQueueAsync(IReadOnlyList<AppCardView> cards)
    {
        if (busy) return;
        busy = true;
        operation = new CancellationTokenSource();
        CancelButton.IsEnabled = true;
        try
        {
            await EnsureConnectionAsync(operation.Token);
            using var api = new CoolifyApi(EffectiveSettings());
            foreach (var card in cards)
            {
                operation.Token.ThrowIfCancellationRequested();
                if (settings.BlockedDeploys.Contains(card.Uuid))
                {
                    AddLog(Loc.F("DeploySkippedLocked", card.Name));
                    continue;
                }
                // Revalidate the SSH tunnel before each deployment, not just once per queue.
                await EnsureConnectionAsync(operation.Token);
                AddLog($"POST /deploy: {card.App.Project}/{card.Name} ({card.Uuid})");
                OperationText.Text = "Deploy: " + card.Name;
                var deployment = await api.StartDeployAsync(card.Uuid, operation.Token);
                AddLog(Loc.F("DeploymentQueued", deployment));
                var start = DateTimeOffset.UtcNow;
                string last = "";
                while (true)
                {
                    operation.Token.ThrowIfCancellationRequested();
                    if (DateTimeOffset.UtcNow - start > TimeSpan.FromMinutes(30))
                        throw new TimeoutException(Loc.T("MonitorTimeout"));
                    await Task.Delay(TimeSpan.FromSeconds(4), operation.Token);
                    await EnsureConnectionAsync(operation.Token);
                    var state = await api.GetDeploymentAsync(deployment, operation.Token);
                    if (state.Status != last)
                    {
                        AddLog($"  {card.Name}: {state.Status} · commit {state.Commit}");
                        last = state.Status;
                    }
                    if (state.Status.Equals("finished", StringComparison.OrdinalIgnoreCase))
                    {
                        AddLog(Loc.T("DeploymentFinished"));
                        break;
                    }
                    if (state.Status is "failed" or "cancelled" or "error")
                        throw new InvalidOperationException(Loc.F("DeploymentFailed", card.Name, state.Status));
                }
            }
            OperationText.Text = Loc.T("QueueFinished");
            AddLog(Loc.T("LastStatusLog"));
        }
        catch (OperationCanceledException)
        {
            AddLog(Loc.T("StatusWaiting"));
            OperationText.Text = Loc.T("StatusWaitingShort");
        }
        catch (Exception ex)
        {
            AddLog(Loc.T("ErrorPrefix") + ex.Message);
            OperationText.Text = Loc.T("OperationFailed");
        }
        finally
        {
            operation.Dispose(); operation = null; busy = false; CancelButton.IsEnabled = false;
        }
        await RefreshAsync();
    }
}
