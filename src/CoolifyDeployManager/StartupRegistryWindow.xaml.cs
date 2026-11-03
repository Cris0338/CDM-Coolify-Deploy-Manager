// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CoolifyDeployManager;

internal sealed class StartupRegistryRow : INotifyPropertyChanged
{
    internal StartupRegistration Registration { get; }
    private bool selected;
    public event PropertyChangedEventHandler? PropertyChanged;

    internal StartupRegistryRow(StartupRegistration registration)
    {
        Registration = registration;
    }

    public bool Selected
    {
        get => selected;
        set
        {
            if (selected == value) return;
            selected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
        }
    }

    public string DisplayName => Registration.ExecutablePath is { } path
        ? Path.GetFileName(path) : Registration.ValueName;
    public string Command => Registration.Command;
    public string RegistryLocation => Registration.Location + "  ·  " + Registration.ValueName;
    public Visibility CurrentVisibility => Registration.IsCurrentClone
        ? Visibility.Visible : Visibility.Collapsed;
}

public partial class StartupRegistryWindow : Window
{
    private readonly ObservableCollection<StartupRegistryRow> rows = new();
    private readonly IReadOnlyList<StartupRegistration>? smokeFixture;
    private StartupRegistration[]? pendingRemoval;
    private bool busy;

    public StartupRegistryWindow()
    {
        InitializeComponent();
        RegistrationList.ItemsSource = rows;
        Loaded += async (_, _) => await RefreshAsync();
    }

    // A completely in-memory fixture for WPF smoke testing. No registry reads/writes.
    internal StartupRegistryWindow(IReadOnlyList<StartupRegistration> fixture) : this()
    {
        smokeFixture = fixture;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task RefreshAsync()
    {
        if (busy) return;
        busy = true;
        SetPending(false);
        RefreshButton.IsEnabled = false;
        StatusMessage.Text = Loc.T("StartupManagerScanning");
        try
        {
            StartupRegistryScan scan;
            if (smokeFixture is not null)
                scan = new StartupRegistryScan(smokeFixture, Array.Empty<string>());
            else
                scan = await Task.Run(() => WindowsStartup.ScanRegistrations(ExecutableProfile.OwnPath));
            if (!IsLoaded) return;
            rows.Clear();
            foreach (var entry in scan.Entries)
                rows.Add(new StartupRegistryRow(entry));
            FoundCount.Text = Loc.F("StartupManagerCount", rows.Count);
            EmptyMessage.Text = rows.Count == 0 ? Loc.T("StartupManagerNone") : "";
            StatusMessage.Text = scan.Warnings.Count > 0
                ? Loc.F("StartupManagerReadWarnings", scan.Warnings.Count) +
                    "\n" + string.Join("\n", scan.Warnings)
                : Loc.T("StartupManagerScanDone");
        }
        catch (Exception ex)
        {
            if (IsLoaded)
                StatusMessage.Text = Loc.F("StartupManagerError", ex.Message);
        }
        finally
        {
            busy = false;
            if (IsLoaded)
            {
                RefreshButton.IsEnabled = true;
                UpdateSelection();
            }
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void Selection_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox check && check.DataContext is StartupRegistryRow row)
            row.Selected = check.IsChecked == true;
        if (!busy) UpdateSelection();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (busy || pendingRemoval is not null) return;
        foreach (var row in rows) row.Selected = true;
        UpdateSelection();
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        if (busy || pendingRemoval is not null) return;
        foreach (var row in rows) row.Selected = false;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        int selected = rows.Count(r => r.Selected);
        SelectionCount.Text = Loc.F("StartupManagerSelected", selected);
        DeleteSelectedButton.IsEnabled = !busy && pendingRemoval is null && selected > 0;
        SelectAllButton.IsEnabled = !busy && pendingRemoval is null && rows.Count > 0;
        ClearSelectionButton.IsEnabled = !busy && pendingRemoval is null && selected > 0;
    }

    private void SetPending(bool enabled)
    {
        if (!enabled)
        {
            pendingRemoval = null;
            ConfirmPanel.Visibility = Visibility.Collapsed;
        }
        RegistrationList.IsEnabled = !enabled;
        SelectAllButton.IsEnabled = !enabled;
        ClearSelectionButton.IsEnabled = !enabled;
        RefreshButton.IsEnabled = !enabled;
        DeleteSelectedButton.IsEnabled = !enabled;
        if (!enabled) UpdateSelection();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (busy || pendingRemoval is not null) return;
        var selected = rows.Where(r => r.Selected)
            .Select(r => r.Registration).ToArray();
        if (selected.Length == 0) return;
        pendingRemoval = selected;
        ConfirmText.Text = Loc.F("StartupManagerConfirmText", selected.Length);
        SetPending(true);
        ConfirmPanel.Visibility = Visibility.Visible;
    }

    private void CancelDelete_Click(object sender, RoutedEventArgs e) => SetPending(false);

    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
    {
        var selected = pendingRemoval;
        if (busy || selected is null || selected.Length == 0) return;
        if (smokeFixture is not null)
            throw new InvalidOperationException("Smoke fixture cannot change the Windows registry.");

        busy = true;
        SetPending(false);
        RefreshButton.IsEnabled = false;
        StatusMessage.Text = Loc.T("StartupManagerRemoving");
        try
        {
            var outcome = await Task.Run(() => WindowsStartup.RemoveSelected(selected));
            busy = false;
            await RefreshAsync();
            if (IsLoaded)
                StatusMessage.Text = Loc.F("StartupManagerResult", outcome.Removed,
                    outcome.Changed, outcome.Errors.Count) +
                    (outcome.Errors.Count > 0 ? "\n" + string.Join("\n", outcome.Errors) : "");
        }
        catch (Exception ex)
        {
            if (IsLoaded) StatusMessage.Text = Loc.F("StartupManagerError", ex.Message);
        }
        finally
        {
            busy = false;
            if (IsLoaded)
            {
                RefreshButton.IsEnabled = true;
                UpdateSelection();
            }
        }
    }

    // Verifies actual WPF click wiring, selection, inline confirmation and cancellation.
    // Test fixture means ZERO registry I/O; never press the destructive confirm button.
    internal void VerifyModalForSmoke()
    {
        if (rows.Count != 3 || !rows[0].Registration.IsCurrentClone ||
            rows.Count(r => r.Registration.IsCurrentClone) != 1)
            throw new InvalidDataException("Startup modal did not list three entries / mark current EXE.");

        SelectAllButton.RaiseEvent(new RoutedEventArgs(
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if (rows.Count(r => r.Selected) != 3 || !DeleteSelectedButton.IsEnabled)
            throw new InvalidDataException("Startup modal did not select all registrations.");

        DeleteSelectedButton.RaiseEvent(new RoutedEventArgs(
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if (pendingRemoval?.Length != 3 || ConfirmPanel.Visibility != Visibility.Visible)
            throw new InvalidDataException("Startup modal did not show deletion confirmation.");

        CancelDeleteButton.RaiseEvent(new RoutedEventArgs(
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if (pendingRemoval is not null || ConfirmPanel.Visibility != Visibility.Collapsed)
            throw new InvalidDataException("Startup modal failed to cancel deletion.");

        ClearSelectionButton.RaiseEvent(new RoutedEventArgs(
            System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if (rows.Any(r => r.Selected) || DeleteSelectedButton.IsEnabled)
            throw new InvalidDataException("Startup modal could not deselect all entries.");
    }
}
