// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows;

namespace CoolifyDeployManager;

internal sealed class AppCardView : INotifyPropertyChanged
{
    public CoolifyApplication App { get; }
    public string Uuid => App.Uuid;
    public string Name => App.Name;
    public string Environment => $"{App.Project} · {App.Environment}";
    public string Branch => string.IsNullOrWhiteSpace(App.Branch) ? Loc.T("BranchUnknown") : App.Branch;
    public string FullDescription => $"{App.Name}\n{App.Branch}\nUUID: {App.Uuid}";
    public DeploymentGroup Group { get; }
    public string DeployCaption => Group == DeploymentGroup.Production ? Loc.T("DeployProd") : Group == DeploymentGroup.Staging ? Loc.T("DeployStage") : Loc.T("DeployUnknown");
    public Brush DeployBrush => Group == DeploymentGroup.Production ? Brushes.IndianRed : Group == DeploymentGroup.Staging ? Brush("#377DE6") : Brush("#667085");
    public Brush GroupBrush => DeployBrush;
    public string RunningLabel { get; }
    public string HealthLabel { get; }
    public Brush RunningBrush { get; }
    public Brush HealthBrush { get; }
    public string RawStatus => App.Status;
    private string lastDeploy = Loc.T("LastLoading");
    public string LastDeploy { get => lastDeploy; private set { lastDeploy = value; Changed(); } }
    private string lastStatus = "";
    public string LastStatus { get => lastStatus; private set { lastStatus = value; Changed(); } }
    public LatestDeploy? Latest { get; private set; }
    public bool Running => RunningLabel == "RUNNING";
    public bool HasProblem => RunningLabel == "STOPPED" || HealthLabel == "UNHEALTHY";
    public bool Busy { get; set; }
    public Visibility SelectionVisibility => Group == DeploymentGroup.Staging ? Visibility.Visible : Visibility.Collapsed;
    private bool isSelected;
    public bool IsSelected { get => isSelected; set { if (isSelected == value) return; isSelected = value; Changed(); } }
    private bool positionLocked;
    public bool PositionLocked { get => positionLocked; set { if (positionLocked == value) return; positionLocked = value; Changed(); } }
    private bool deployLocked;
    public bool DeployLocked { get => deployLocked; set { if (deployLocked == value) return; deployLocked = value; Changed(); Changed(nameof(CanDeploy)); } }
    public bool CanDeploy => Group != DeploymentGroup.Unknown && !DeployLocked;
    public AppCardView(CoolifyApplication app, DeploymentGroup group)
    {
        App = app; Group = group;
        var state = (app.Status ?? "").ToLowerInvariant();
        var first = state.Split(':')[0].Trim();
        RunningLabel = first == "running" ? "RUNNING" : first is "exited" or "stopped" or "dead" ? "STOPPED" : "RUNNING N/D";
        RunningBrush = RunningLabel switch { "RUNNING" => Brush("#164D3C"), "STOPPED" => Brush("#643036"), _ => Brush("#343C4A") };
        var parts = state.Split(':').Skip(1).ToArray();
        HealthLabel = parts.Contains("healthy") ? "HEALTHY" : parts.Contains("unhealthy") ? "UNHEALTHY" : "HEALTH N/D";
        HealthBrush = HealthLabel switch { "HEALTHY" => Brush("#164D3C"), "UNHEALTHY" => Brush("#643036"), _ => Brush("#343C4A") };
    }
    public void UpdateDeploy(LatestDeploy? info)
    {
        Latest = info;
        if (info is null) { LastDeploy = Loc.T("LastUnavailable"); LastStatus = ""; return; }
        if (!info.HasDeploy) { LastDeploy = Loc.T("LastNone"); LastStatus = ""; return; }
        LastDeploy = Loc.T("LastPrefix") + Relative(info.When);
        LastStatus = info.Status + (string.IsNullOrWhiteSpace(info.Commit) ? "" : " · " + info.Commit[..Math.Min(9, info.Commit.Length)]);
    }
    public void UpdateClock() { if (Latest is { HasDeploy: true }) LastDeploy = Loc.T("LastPrefix") + Relative(Latest.When); }
    private static string Relative(DateTimeOffset? date)
    {
        if (date is null) return Loc.T("UnknownDate");
        var span = DateTimeOffset.Now - date.Value;
        if (span.TotalSeconds < 0) return Loc.T("Soon");
        if (span.TotalMinutes < 1) return Loc.T("Now");
        if (span.TotalHours < 1) return Loc.F("MinutesAgo", (int)span.TotalMinutes);
        if (span.TotalDays < 1) return Loc.F("HoursAgo", (int)span.TotalHours);
        return Loc.F("DaysAgo", (int)span.TotalDays);
    }
    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
