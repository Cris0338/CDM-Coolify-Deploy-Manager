// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows;

namespace CoolifyDeployManager;

// A single mutable representation of the application's observed runtime
// status. Deployment history and health are independent: a finished build is
// not necessarily a healthy running container.
internal sealed class AppCardView : INotifyPropertyChanged
{
    private static readonly Brush HealthyBrush = ColorBrush("#164D3C");
    private static readonly Brush UnhealthyBrush = ColorBrush("#643036");
    private static readonly Brush UnknownBrush = ColorBrush("#343C4A");

    public CoolifyApplication App { get; }
    public string Uuid => App.Uuid;
    public string Name => App.Name;
    public string Environment => $"{App.Project} · {App.Environment}";
    public string Branch => string.IsNullOrWhiteSpace(App.Branch) ? Loc.T("BranchUnknown") : App.Branch;
    public string FullDescription => $"{App.Name}\n{App.Branch}\nUUID: {App.Uuid}";
    public DeploymentGroup Group { get; }
    public string DeployCaption => Group == DeploymentGroup.Production ? Loc.T("DeployProd")
        : Group == DeploymentGroup.Staging ? Loc.T("DeployStage") : Loc.T("DeployUnknown");
    public Brush DeployBrush => Group == DeploymentGroup.Production ? Brushes.IndianRed
        : Group == DeploymentGroup.Staging ? ColorBrush("#377DE6") : ColorBrush("#667085");
    public Brush GroupBrush => DeployBrush;

    private string currentStatus;
    public string RawStatus => currentStatus;
    private string[] StatusParts => (currentStatus ?? "").Split(':', StringSplitOptions.TrimEntries);

    public string RunningLabel => StatusParts[0].ToLowerInvariant() switch
    {
        "running" => "RUNNING",
        "exited" or "stopped" or "dead" => "STOPPED",
        _ => "RUNNING N/D"
    };
    public string HealthLabel => StatusParts.Skip(1).Contains("healthy", StringComparer.OrdinalIgnoreCase)
        ? "HEALTHY" : StatusParts.Skip(1).Contains("unhealthy", StringComparer.OrdinalIgnoreCase)
        ? "UNHEALTHY" : "HEALTH N/D";
    public Brush RunningBrush => RunningLabel switch
    {
        "RUNNING" => HealthyBrush,
        "STOPPED" => UnhealthyBrush,
        _ => UnknownBrush
    };
    public Brush HealthBrush => HealthLabel switch
    {
        "HEALTHY" => HealthyBrush,
        "UNHEALTHY" => UnhealthyBrush,
        _ => UnknownBrush
    };
    public bool Running => RunningLabel == "RUNNING";
    public bool HasProblem => RunningLabel == "STOPPED" || HealthLabel == "UNHEALTHY";

    private string lastDeploy = Loc.T("LastLoading");
    public string LastDeploy
    {
        get => lastDeploy;
        private set { lastDeploy = value; Changed(); }
    }
    private string lastStatus = "";
    public string LastStatus
    {
        get => lastStatus;
        private set { lastStatus = value; Changed(); }
    }
    public LatestDeploy? Latest { get; private set; }
    // Only a Coolify-confirmed queued/active job is animated. The status
    // comes from POST /deploy or GET /deployments/{uuid}, never a timer.
    private string liveDeploymentStatus = "";
    private bool isLiveDeploymentActive;
    public string LiveDeploymentStatus => liveDeploymentStatus;
    public bool IsLiveDeploymentActive => isLiveDeploymentActive;
    public void UpdateLiveDeployment(string? serverStatus)
    {
        string next = serverStatus?.Trim() ?? "";
        bool active = DeploymentActivity.IsActive(next);
        if (!string.Equals(liveDeploymentStatus, next, StringComparison.Ordinal))
        {
            liveDeploymentStatus = next;
            Changed(nameof(LiveDeploymentStatus));
        }
        if (isLiveDeploymentActive != active)
        {
            isLiveDeploymentActive = active;
            Changed(nameof(IsLiveDeploymentActive));
        }
    }

    public bool Busy { get; set; }
    public Visibility SelectionVisibility => Group == DeploymentGroup.Staging
        ? Visibility.Visible : Visibility.Collapsed;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set { if (isSelected == value) return; isSelected = value; Changed(); }
    }

    private bool positionLocked;
    public bool PositionLocked
    {
        get => positionLocked;
        set { if (positionLocked == value) return; positionLocked = value; Changed(); }
    }

    private bool deployLocked;
    public bool DeployLocked
    {
        get => deployLocked;
        set
        {
            if (deployLocked == value) return;
            deployLocked = value;
            Changed();
            Changed(nameof(CanDeploy));
        }
    }
    public bool CanDeploy => Group != DeploymentGroup.Unknown && !DeployLocked;

    public AppCardView(CoolifyApplication app, DeploymentGroup group)
    {
        App = app;
        Group = group;
        currentStatus = app.Status ?? "";
    }

    // Called on the WPF UI thread after a read-only Coolify health refresh.
    public void UpdateApplicationStatus(string status)
    {
        status ??= "";
        if (string.Equals(currentStatus, status, StringComparison.Ordinal)) return;
        currentStatus = status;
        Changed(nameof(RawStatus));
        Changed(nameof(RunningLabel));
        Changed(nameof(RunningBrush));
        Changed(nameof(HealthLabel));
        Changed(nameof(HealthBrush));
        Changed(nameof(Running));
        Changed(nameof(HasProblem));
    }

    public void UpdateDeploy(LatestDeploy? info)
    {
        Latest = info;
        if (info is null) { LastDeploy = Loc.T("LastUnavailable"); LastStatus = ""; return; }
        if (!info.HasDeploy) { LastDeploy = Loc.T("LastNone"); LastStatus = ""; return; }
        LastDeploy = Loc.T("LastPrefix") + Relative(info.When);
        LastStatus = info.Status + (string.IsNullOrWhiteSpace(info.Commit)
            ? "" : " · " + info.Commit[..Math.Min(9, info.Commit.Length)]);
    }

    public void UpdateClock()
    {
        if (Latest is { HasDeploy: true })
            LastDeploy = Loc.T("LastPrefix") + Relative(Latest.When);
    }

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

    private static Brush ColorBrush(string color) =>
        (Brush)new BrushConverter().ConvertFromString(color)!;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
