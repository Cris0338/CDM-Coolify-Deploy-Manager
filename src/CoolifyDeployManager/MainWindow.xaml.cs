// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
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
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using ShapePath = System.Windows.Shapes.Path;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace CoolifyDeployManager;

public partial class MainWindow : Window
{
    private UserSettings settings = new();
    private readonly SshTunnel ssh = new();
    private CoolifyInventory inventory = new(Array.Empty<CoolifyProject>(), Array.Empty<CoolifyApplication>());
    private readonly Dictionary<string, AppCardView> allCards = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer clock;
    private CancellationTokenSource? operation;
    private CancellationTokenSource? postDeployHealthSync;
    private readonly SemaphoreSlim sshConnectionGate = new(1, 1);
    private CancellationTokenSource? tunnelRecovery;
    private bool reconnectingTunnel;
    private bool closingWindow;
    private bool busy;
    // A server deploy is network-busy but must not lock dashboard layout.
    // Only an inventory refresh recreates card view-models.
    private bool inventoryRefreshing;
    // Offline test mode: render WPF cards without creating SSH/API traffic.
    internal bool SkipStartupConnectionForSmoke { get; set; }
    private string selectedProjectUuid = "ALL";
    private readonly List<ProjectChoice> projectChoices = new();
    private Point dragOrigin;
    private string? dragUuid;
    private GridLength lastVisibleLogHeight = new(160);
    private HwndSource? windowSource;

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
        clock.Tick += async (_, _) =>
        {
            foreach (var card in allCards.Values) card.UpdateClock();
            await CheckTunnelStateAsync();
        };
        clock.Start();
        ProjectPickerButton.Content = Loc.T("AllProjects");
        LogBox.Text = "";
        StageCount.Text = Loc.F("ApplicationsCount", 0);
        ProductionCount.Text = Loc.F("ApplicationsCount", 0);
        UnknownCount.Text = Loc.F("ApplicationsCount", 0);
        InventoryCount.Text = Loc.F("ApplicationsCount", 0);
        UpdateProfileStatus();
    }

    private void UpdateProfileStatus()
    {
        var status = Loc.T(SettingsStore.HasPortableProfile && settings.IsConfigured
            ? "VaultConfigured" : "VaultNotConfigured");
        Title = Loc.T("AppTitle") + " " + BuildInfo.Version + " · " + status;
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
        if (SkipStartupConnectionForSmoke) return;
        if (settings.IsConfigured) await RefreshAsync();
        else _ = Dispatcher.BeginInvoke(new Action(() => OpenSettings()));
    }
    // Every non-interactive pixel of the titlebar is a drag handle; no "move"
    // crosshair cursor and no accidental dragging when pressing a window button.
    private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            sender is not DependencyObject bar ||
            IsInteractiveCardElement(e.OriginalSource as DependencyObject, bar))
            return;

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            e.Handled = true;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (MaximizeGlyph is null || MaximizeButton is null) return;
        bool maximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.Data = Geometry.Parse(maximized
            ? "M 3,5 H 11 V 13 H 3 Z M 5,5 V 3 H 13 V 11 H 11"
            : "M 2,2 H 14 V 14 H 2 Z");
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    // WindowChrome + WindowStyle=None can otherwise maximize across the Windows
    // taskbar. WM_GETMINMAXINFO constrains the maximized client to the work area
    // of the monitor containing the window (correct for multiple monitors).
    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left; public int Top; public int Right; public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref NativeMonitorInfo info);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        windowSource = PresentationSource.FromVisual(this) as HwndSource;
        windowSource?.AddHook(WindowMessage);
    }

    private static IntPtr WindowMessage(IntPtr hwnd, int msg, IntPtr wParam,
        IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        if (msg != WM_GETMINMAXINFO || lParam == IntPtr.Zero) return IntPtr.Zero;

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) return IntPtr.Zero;
        var info = new NativeMonitorInfo
        {
            Size = Marshal.SizeOf<NativeMonitorInfo>()
        };
        if (!GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;

        var value = Marshal.PtrToStructure<NativeMinMaxInfo>(lParam);
        value.MaxPosition.X = info.Work.Left - info.Monitor.Left;
        value.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
        value.MaxSize.X = info.Work.Right - info.Work.Left;
        value.MaxSize.Y = info.Work.Bottom - info.Work.Top;
        Marshal.StructureToPtr(value, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        windowSource?.RemoveHook(WindowMessage);
        windowSource = null;
        base.OnClosed(e);
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        closingWindow = true;
        tunnelRecovery?.Cancel();
        postDeployHealthSync?.Cancel();
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
    // The clock ticks every 60 seconds. A terminated ssh.exe must be
    // recovered automatically rather than leaving the dashboard disconnected
    // until the owner presses Refresh. Never reconnect during deployment,
    // while configuring the tunnel or after the window begins closing.
    internal static bool ShouldRecoverManagedTunnel(ConnectionMode mode,
        bool configured, bool busy, bool running, bool recovering, bool closing) =>
        mode == ConnectionMode.ManagedSshTunnel && configured && !busy &&
        !running && !recovering && !closing;

    private async Task CheckTunnelStateAsync()
    {
        if (!ShouldRecoverManagedTunnel(settings.ConnectionMode, settings.IsConfigured,
                busy, ssh.IsRunning, reconnectingTunnel, closingWindow)) return;

        reconnectingTunnel = true;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        tunnelRecovery = timeout;
        SetConnection(false, Loc.T("TunnelStopped"));
        AddLog("Managed SSH tunnel stopped: " + ssh.DescribeStoppedProcess());
        AddLog("Reconnecting managed SSH automatically...");
        try
        {
            await EnsureConnectionAsync(timeout.Token);
            if (!closingWindow) AddLog("Managed SSH automatically reconnected.");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            if (!closingWindow)
                AddLog("Managed SSH automatic reconnect timed out; next status check will retry.");
        }
        catch (Exception ex)
        {
            if (!closingWindow)
            {
                SetConnection(false, Loc.T("TunnelStopped"));
                AddLog("Managed SSH automatic reconnect failed: " + ex.Message);
            }
        }
        finally
        {
            tunnelRecovery = null;
            reconnectingTunnel = false;
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (settings.ConnectionMode == ConnectionMode.ManagedSshTunnel)
        {
            // A manual Refresh can be pressed during automatic recovery.
            // Serialize both callers so they cannot kill each other's
            // newly started SSH processes or fight over the local port.
            await sshConnectionGate.WaitAsync(ct);
            try { await ssh.EnsureRunningAsync(settings, ct); }
            finally { sshConnectionGate.Release(); }
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
        inventoryRefreshing = true;
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
        finally
        {
            inventoryRefreshing = false;
            busy = false;
            RefreshButton.IsEnabled = true;
        }
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

    private bool KeepsFixedPositions(IReadOnlyList<string> before, IReadOnlyList<string> after) =>
        before.Count == after.Count &&
        !before.Where((uuid, index) => settings.LockedPositions.Contains(uuid) &&
            !uuid.Equals(after[index], StringComparison.OrdinalIgnoreCase)).Any();
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

    // Each visible card owns a new, mutable RotateTransform. The old XAML
    // Storyboard animated a Freezable shared/frozen by the WPF template during
    // a drop-induced card rebuild and crashed the entire process. Directly
    // animating a per-instance transform avoids that shared Freezable path.
    private static void SetLiveDeploymentSpinner(ShapePath path, bool shouldSpin)
    {
        if (path.RenderTransform is not RotateTransform rotate || rotate.IsFrozen)
        {
            rotate = new RotateTransform(0);
            path.RenderTransform = rotate;
        }
        rotate.BeginAnimation(RotateTransform.AngleProperty, shouldSpin
            ? new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
              { RepeatBehavior = RepeatBehavior.Forever }
            : null);
    }

    private void LiveDeploymentSpinner_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ShapePath path)
            SetLiveDeploymentSpinner(path, path.IsVisible);
    }

    private void LiveDeploymentSpinner_IsVisibleChanged(object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (sender is ShapePath path)
            SetLiveDeploymentSpinner(path, path.IsVisible);
    }

    private void LiveDeploymentSpinner_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ShapePath path)
            SetLiveDeploymentSpinner(path, false);
    }

    // Exercise an actual loaded WPF card template, not just the view model.
    // The previous XAML Storyboard threw XamlParseException when RenderCards
    // recreated an active spinner during a drag/drop. No API, SSH or save.
    internal void VerifyLiveSpinnerRetemplateForSmoke()
    {
        var first = new CoolifyApplication("offline-spinner-1", "spinner-one",
            "running:healthy", "main", "test", "test-project", "staging", "");
        var second = new CoolifyApplication("offline-spinner-2", "spinner-two",
            "running:healthy", "main", "test", "test-project", "staging", "");
        var active = new AppCardView(first, DeploymentGroup.Staging);
        var other = new AppCardView(second, DeploymentGroup.Staging);
        active.UpdateLiveDeployment("in_progress");
        allCards.Clear();
        allCards.Add(first.Uuid, active);
        allCards.Add(second.Uuid, other);
        inventory = new CoolifyInventory(Array.Empty<CoolifyProject>(),
            new[] { first, second });

        // Force several actual WPF DataTemplate unload/load cycles while the
        // server-observed spinner is active, as a card reorder does in practice.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            settings.CardOrders[OrderKey(DeploymentGroup.Staging)] = attempt % 2 == 0
                ? new List<string> { first.Uuid, second.Uuid }
                : new List<string> { second.Uuid, first.Uuid };
            RenderCards();
            UpdateLayout();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            UpdateLayout();
            var spinner = FindLiveSpinner(StagingItems, "in_progress");
            if (spinner is null || !spinner.IsVisible ||
                spinner.RenderTransform is not RotateTransform rotate ||
                rotate.IsFrozen || !rotate.HasAnimatedProperties)
                throw new InvalidOperationException(
                    "The loaded, re-ordered card does not own a mutable animated spinner.");
        }

        active.UpdateLiveDeployment("finished");
        UpdateLayout();
        Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
        var stopped = FindLiveSpinner(StagingItems, "finished");
        if (stopped is null || stopped.IsVisible ||
            stopped.RenderTransform is not RotateTransform stoppedRotate ||
            stoppedRotate.HasAnimatedProperties)
            throw new InvalidOperationException(
                "Terminal Coolify state did not stop the real rendered spinner.");
    }

    private static ShapePath? FindLiveSpinner(DependencyObject root, string tooltip)
    {
        if (root is ShapePath path && string.Equals(
            path.ToolTip?.ToString(), tooltip, StringComparison.Ordinal))
            return path;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindLiveSpinner(VisualTreeHelper.GetChild(root, i), tooltip);
            if (found is not null) return found;
        }
        return null;
    }

    // One drag-and-drop implementation. A card can be picked up from any
    // non-interactive area; hovering over the left/right half of another card
    // inserts it before/after that card. Empty section space appends to the end.
    private Border? insertionCueCard;
    private CardDragPreview? dragPreview;
    private AdornerLayer? dragPreviewLayer;

    // An adorner is drawn inside WPF's own adorner layer. It never receives
    // mouse hits and therefore cannot steal drop events from the cards below.
    private sealed class CardDragPreview : Adorner
    {
        private readonly ImageBrush snapshot;
        private readonly Size size;
        private readonly Point grabOffset;
        private Point pointer;

        public CardDragPreview(UIElement host, Border card, Point grabOffset) : base(host)
        {
            IsHitTestVisible = false;
            this.grabOffset = grabOffset;
            size = new Size(card.ActualWidth, card.ActualHeight);
            var bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(size.Width)),
                Math.Max(1, (int)Math.Ceiling(size.Height)),
                96, 96, PixelFormats.Pbgra32);
            bitmap.Render(card);
            snapshot = new ImageBrush(bitmap) { Stretch = Stretch.Fill };
        }

        public void Move(Point position)
        {
            pointer = position;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bounds = new Rect(pointer.X - grabOffset.X, pointer.Y - grabOffset.Y,
                size.Width, size.Height);
            drawingContext.PushOpacity(0.8);
            drawingContext.DrawRoundedRectangle(snapshot,
                new Pen(Brushes.DodgerBlue, 2), bounds, 13, 13);
            drawingContext.Pop();
        }
    }

    private void ShowDragPreview(Border card, Point grabOffset, Point pointer)
    {
        dragPreviewLayer = AdornerLayer.GetAdornerLayer(DashboardRoot);
        if (dragPreviewLayer is null) return;
        dragPreview = new CardDragPreview(DashboardRoot, card, grabOffset);
        dragPreview.Move(pointer);
        dragPreviewLayer.Add(dragPreview);
    }

    private void UpdateDragPreview(DragEventArgs e) =>
        dragPreview?.Move(e.GetPosition(DashboardRoot));

    private void ClearDragPreview()
    {
        if (dragPreview is not null && dragPreviewLayer is not null)
            dragPreviewLayer.Remove(dragPreview);
        dragPreview = null;
        dragPreviewLayer = null;
    }

    // Do not confuse server activity with a frozen WPF UI: card positioning
    // remains available throughout deployment. An inventory rebuild or an
    // active search/filter still protects the existing drag ordering semantics.
    internal static bool CanReorderWhile(bool inventoryLoading, string? search, int statusFilter) =>
        !inventoryLoading && string.IsNullOrWhiteSpace(search) && statusFilter == 0;

    private bool CanReorderCards =>
        CanReorderWhile(inventoryRefreshing, SearchBox.Text, StatusFilter.SelectedIndex);

    private static bool IsInteractiveCardElement(DependencyObject? source, DependencyObject card)
    {
        for (var node = source; node is not null && node != card;)
        {
            if (node is System.Windows.Controls.Primitives.ButtonBase
                or TextBox or PasswordBox or ComboBox or MenuItem)
                return true;
            node = node is Visual ? VisualTreeHelper.GetParent(node) : null;
        }
        return false;
    }

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragUuid = null;
        if (!CanReorderCards || sender is not Border card ||
            card.DataContext is not AppCardView model || model.PositionLocked ||
            IsInteractiveCardElement(e.OriginalSource as DependencyObject, card))
            return;
        dragUuid = model.Uuid;
        dragOrigin = e.GetPosition(this);
        Mouse.Capture(card, CaptureMode.SubTree);
    }

    private void Card_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        dragUuid = null;
        if (Mouse.Captured == sender) Mouse.Capture(null);
    }

    private void Card_LostMouseCapture(object sender, MouseEventArgs e) => dragUuid = null;

    private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (dragUuid is not null && Mouse.Captured == sender) Mouse.Capture(null);
            dragUuid = null;
            return;
        }
        if (!CanReorderCards || dragUuid is null || sender is not Border card) return;
        var delta = e.GetPosition(this) - dragOrigin;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var uuid = dragUuid;
        dragUuid = null;
        // Use a snapshot of the actual card as the mouse-following drag image.
        var grabOffset = e.GetPosition(card);
        var pointer = e.GetPosition(DashboardRoot);
        Mouse.Capture(null);
        try
        {
            ShowDragPreview(card, grabOffset, pointer);
            DragDrop.DoDragDrop(card, new DataObject("CDM_CardUuid", uuid), DragDropEffects.Move);
        }
        finally
        {
            ClearInsertionCue();
            ClearDragPreview();
        }
        e.Handled = true;
    }

    private static string? DraggedCardUuid(DragEventArgs e) =>
        e.Data.GetDataPresent("CDM_CardUuid")
            ? e.Data.GetData("CDM_CardUuid") as string : null;

    private bool TryPlanCardMove(DeploymentGroup group, string sourceUuid,
        string? destinationUuid, bool insertAfter, out List<string> order)
    {
        order = new List<string>();
        if (!CanReorderCards || !allCards.TryGetValue(sourceUuid, out var source) ||
            source.PositionLocked || source.Group != group ||
            (selectedProjectUuid != "ALL" && source.App.ProjectUuid != selectedProjectUuid))
            return false;

        if (destinationUuid is not null &&
            (!allCards.TryGetValue(destinationUuid, out var destination) ||
             destination.PositionLocked || destination.Group != group ||
             sourceUuid.Equals(destinationUuid, StringComparison.OrdinalIgnoreCase)))
            return false;

        var before = FilterAndOrder(group).Select(c => c.Uuid).ToList();
        if (!before.Contains(sourceUuid)) return false;
        order = new List<string>(before);
        order.Remove(sourceUuid);

        if (destinationUuid is null)
        {
            order.Add(sourceUuid);
        }
        else
        {
            var targetIndex = order.FindIndex(id =>
                id.Equals(destinationUuid, StringComparison.OrdinalIgnoreCase));
            if (targetIndex < 0) return false;
            order.Insert(targetIndex + (insertAfter ? 1 : 0), sourceUuid);
        }

        return KeepsFixedPositions(before, order);
    }

    private void SetInsertionCue(Border? card, bool insertAfter = false)
    {
        if (insertionCueCard?.Child is Grid previous)
        {
            foreach (var marker in previous.Children.OfType<Border>())
                if (marker.Tag is "InsertBefore" or "InsertAfter")
                    marker.Visibility = Visibility.Collapsed;
        }
        insertionCueCard = card;
        if (card?.Child is not Grid grid) return;
        foreach (var marker in grid.Children.OfType<Border>())
            if (marker.Tag is "InsertBefore" or "InsertAfter")
                marker.Visibility = marker.Tag.Equals(insertAfter ? "InsertAfter" : "InsertBefore")
                    ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClearInsertionCue() => SetInsertionCue(null);

    private void Card_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragPreview(e);
        e.Effects = DragDropEffects.None;
        if (sender is Border target && target.DataContext is AppCardView destination &&
            DraggedCardUuid(e) is string sourceUuid)
        {
            var insertAfter = e.GetPosition(target).X >= target.ActualWidth / 2;
            if (TryPlanCardMove(destination.Group, sourceUuid, destination.Uuid,
                insertAfter, out _))
            {
                SetInsertionCue(target, insertAfter);
                e.Effects = DragDropEffects.Move;
            }
            else ClearInsertionCue();
        }
        else ClearInsertionCue();
        e.Handled = true;
    }

    private void Card_DragLeave(object sender, DragEventArgs e)
    {
        if (ReferenceEquals(sender, insertionCueCard)) ClearInsertionCue();
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        // The card handles its own preview drop; it cannot fall through to the
        // parent ItemsControl's append-to-end handler.
        e.Handled = true;
        if (sender is Border target && target.DataContext is AppCardView destination &&
            DraggedCardUuid(e) is string sourceUuid)
        {
            var insertAfter = e.GetPosition(target).X >= target.ActualWidth / 2;
            ClearInsertionCue();
            CommitCardMove(destination.Group, sourceUuid, destination.Uuid, insertAfter);
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            ClearInsertionCue();
            e.Effects = DragDropEffects.None;
        }
    }

    private void EmptyArea_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragPreview(e);
        e.Effects = DragDropEffects.None;
        ClearInsertionCue();
        if (sender is FrameworkElement element &&
            Enum.TryParse<DeploymentGroup>(element.Tag as string, out var group) &&
            DraggedCardUuid(e) is string sourceUuid &&
            TryPlanCardMove(group, sourceUuid, null, false, out _))
            e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void EmptyArea_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ClearInsertionCue();
        if (sender is FrameworkElement element &&
            Enum.TryParse<DeploymentGroup>(element.Tag as string, out var group) &&
            DraggedCardUuid(e) is string sourceUuid)
        {
            CommitCardMove(group, sourceUuid, null, false);
            e.Effects = DragDropEffects.Move;
        }
        else e.Effects = DragDropEffects.None;
    }

    private void CommitCardMove(DeploymentGroup group, string sourceUuid,
        string? destinationUuid, bool insertAfter)
    {
        if (!TryPlanCardMove(group, sourceUuid, destinationUuid, insertAfter,
            out var planned))
        {
            // Locked positions retain their original visual index.
            // Local layout feedback belongs in the status bar, not the
            // Coolify/SSH communication log. Do not overwrite deploy status.
            if (!busy) OperationText.Text = Loc.T("LockedPositionPrevented");
            return;
        }

        var before = FilterAndOrder(group).Select(c => c.Uuid).ToList();
        if (before.SequenceEqual(planned, StringComparer.OrdinalIgnoreCase)) return;

        var key = OrderKey(group);
        settings.CardOrders.TryGetValue(key, out var previous);
        settings.CardOrders[key] = planned;
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
        if (busy || reconnectingTunnel)
        {
            MessageBox.Show(this, Loc.T("WaitOperation"));
            return;
        }
        ssh.Stop(); // The wizard may need the same local tunnel port.
        var dlg = new SettingsWindow(settings) { Owner = this };
        if (dlg.ShowDialog() != true) return;
        try { SettingsStore.Save(dlg.Result, dlg.StartWithWindows); }
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
        SetConnection(false, Loc.T("NeedsCheck"));
        await RefreshAsync();
    }
    private void Guide_Click(object sender, RoutedEventArgs e) => new GuideWindow { Owner = this }.ShowDialog();
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void ToggleLog_Click(object sender, RoutedEventArgs e)
    {
        if (LogPanel.Visibility == Visibility.Visible)
        {
            lastVisibleLogHeight = LogRow.Height;
            LogSplitter.Visibility = Visibility.Collapsed;
            LogPanel.Visibility = Visibility.Collapsed;
            LogRow.MinHeight = 0;
            LogRow.Height = new GridLength(0);
        }
        else
        {
            LogRow.MinHeight = 75;
            LogRow.Height = lastVisibleLogHeight.Value > 0
                ? lastVisibleLogHeight : new GridLength(160);
            LogPanel.Visibility = Visibility.Visible;
            LogSplitter.Visibility = Visibility.Visible;
        }
    }
    private void CopyLog_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(LogBox.Text)) Clipboard.SetText(LogBox.Text); }
    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Clear();
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
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
        postDeployHealthSync?.Cancel();
        busy = true;
        operation = new CancellationTokenSource();
        CancelButton.IsEnabled = true;
        var successfullyDeployed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await EnsureConnectionAsync(operation.Token);
            using var api = new CoolifyApi(EffectiveSettings());

            foreach (var card in cards)
            {
                operation.Token.ThrowIfCancellationRequested();
                if (settings.BlockedDeploys.Contains(card.Uuid))
                {
                    // This is a local policy decision, not a Coolify request.
                    OperationText.Text = Loc.F("DeploySkippedLocked", card.Name);
                    continue;
                }

                // Deploy operations stay exclusively here, behind the existing
                // confirmation flow. Health synchronization is read-only.
                await EnsureConnectionAsync(operation.Token);
                AddLog($"POST /deploy: {card.App.Project}/{card.Name} ({card.Uuid})");
                OperationText.Text = "Deploy: " + card.Name;
                var deployment = await api.StartDeployAsync(card.Uuid, operation.Token);
                // A valid deployment UUID confirms server-side queue entry.
                // The spinner starts only after that actual Coolify response.
                card.UpdateLiveDeployment("queued");
                AddLog(Loc.F("DeploymentQueued", deployment));

                var startedAt = DateTimeOffset.UtcNow;
                string lastState = "";
                while (true)
                {
                    operation.Token.ThrowIfCancellationRequested();
                    if (DateTimeOffset.UtcNow - startedAt > TimeSpan.FromMinutes(30))
                        throw new TimeoutException(Loc.T("MonitorTimeout"));

                    await Task.Delay(TimeSpan.FromSeconds(4), operation.Token);
                    await EnsureConnectionAsync(operation.Token);
                    var state = await api.GetDeploymentAsync(deployment, operation.Token);
                    // The same read-only API response that drives the queue
                    // also controls the corresponding card animation.
                    card.UpdateLiveDeployment(state.Status);
                    if (!string.Equals(state.Status, lastState, StringComparison.OrdinalIgnoreCase))
                    {
                        AddLog($"  {card.Name}: {state.Status} · commit {state.Commit}");
                        lastState = state.Status;
                    }

                    if (state.Status.Equals("finished", StringComparison.OrdinalIgnoreCase))
                    {
                        // "finished" is the deployment-job state, NOT proof
                        // that its container has become running:healthy.
                        successfullyDeployed.Add(card.Uuid);
                        AddLog(Loc.T("DeploymentFinished"));
                        break;
                    }
                    if (state.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                        state.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
                        state.Status.Equals("error", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            Loc.F("DeploymentFailed", card.Name, state.Status));
                }
            }
            OperationText.Text = Loc.T("QueueFinished");
        }
        catch (OperationCanceledException)
        {
            OperationText.Text = Loc.T("StatusWaitingShort");
        }
        catch (Exception ex)
        {
            AddLog(Loc.T("ErrorPrefix") + ex.Message);
            OperationText.Text = Loc.T("OperationFailed");
        }
        finally
        {
            // When local monitoring ends (success, error or Stop Queue), do not
            // keep animating based on an old server snapshot. Stop Queue does
            // not cancel the remote Coolify job; Refresh can verify its state.
            foreach (var card in cards) card.UpdateLiveDeployment(null);
            operation?.Dispose();
            operation = null;
            busy = false;
            CancelButton.IsEnabled = false;
        }

        // The first inventory read may precede Coolify's health-check refresh.
        // Follow finished applications for a bounded interval rather than
        // leaving a stale red health badge until manual Refresh is clicked.
        await RefreshAsync();
        if (successfullyDeployed.Count > 0 && IsLoaded)
            StartPostDeployHealthSync(successfullyDeployed);
    }

    private void StartPostDeployHealthSync(IReadOnlyCollection<string> appUuids)
    {
        postDeployHealthSync?.Cancel();
        var source = new CancellationTokenSource();
        postDeployHealthSync = source;
        _ = SyncPostDeployHealthAsync(appUuids, source);
    }

    // Read-only GET /api/v1/applications/{uuid}. The background task runs
    // only while this CDM window remains open, and never sends a deploy.
    private async Task SyncPostDeployHealthAsync(
        IReadOnlyCollection<string> appUuids, CancellationTokenSource source)
    {
        var pending = new HashSet<string>(appUuids, StringComparer.OrdinalIgnoreCase);
        try
        {
            pending.RemoveWhere(uuid =>
                !allCards.TryGetValue(uuid, out var card) || card.HealthLabel == "HEALTHY");
            if (pending.Count == 0) return;

            AddLog("Post-deploy: checking application health until Coolify confirms it.");
            using var api = new CoolifyApi(EffectiveSettings());
            // Maximum 18 checks at five-second intervals (~90 s);
            // individual read errors do not erase the last known status.
            for (int attempt = 0; attempt < 18 && pending.Count > 0; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), source.Token);
                foreach (var uuid in pending.ToArray())
                {
                    source.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var status = await api.GetApplicationStatusAsync(uuid, source.Token);
                        if (!allCards.TryGetValue(uuid, out var card))
                        {
                            pending.Remove(uuid);
                            continue;
                        }
                        if (!string.Equals(card.RawStatus, status, StringComparison.Ordinal))
                        {
                            card.UpdateApplicationStatus(status);
                            AddLog($"  {card.Name}: {status}");
                            // The view model raises binding notifications.
                            // Only filtered lists must be repopulated.
                            if (StatusFilter.SelectedIndex != 0) RenderCards();
                        }
                        if (card.HealthLabel == "HEALTHY") pending.Remove(uuid);
                    }
                    catch (OperationCanceledException) when (source.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // The next read may succeed once Coolify/tunnel recovers.
                        if (attempt == 17) AddLog($"  Health check {uuid}: {ex.Message}");
                    }
                }
            }
            if (pending.Count != 0)
                AddLog("Post-deploy health is not confirmed for all applications yet. Refresh can check again.");
            else
                AddLog("Post-deploy health synchronized with Coolify.");
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            // Closing the window or starting another deployment stops this monitor.
        }
        catch (Exception ex)
        {
            AddLog("Post-deploy health polling ended: " + ex.Message);
        }
        finally
        {
            if (ReferenceEquals(postDeployHealthSync, source))
                postDeployHealthSync = null;
            source.Dispose();
        }
    }

}
