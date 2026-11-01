// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Text.RegularExpressions;

namespace CoolifyDeployManager;

internal enum DeploymentGroup { Unknown, Staging, Production }
internal enum ConnectionMode { Https, ManagedSshTunnel, ExternalLocalTunnel }

internal sealed record CoolifyApplication(string Uuid, string Name, string Status,
    string Branch, string Project, string ProjectUuid, string Environment, string Url);
internal sealed record CoolifyProject(string Uuid, string Name);
internal sealed record CoolifyInventory(IReadOnlyList<CoolifyProject> Projects,
    IReadOnlyList<CoolifyApplication> Applications);
internal sealed record DeploymentInfo(string Uuid, string Status, string Commit);
internal sealed record LatestDeploy(DateTimeOffset? When, string Status, string Commit, bool HasDeploy);

// Immutable geometry, serialized only in the personalized executable profile.
// Minimized-to-tray is transient: save the last Normal/Maximized presentation.
internal sealed record WindowPlacementState
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public bool Maximized { get; init; }

    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top) &&
        double.IsFinite(Width) && double.IsFinite(Height) &&
        Math.Abs(Left) < 100000 && Math.Abs(Top) < 100000 &&
        Width >= 850 && Height >= 570 && Width <= 20000 && Height <= 20000;

    public bool IntersectsDesktop(double x, double y, double width, double height) =>
        IsValid && double.IsFinite(x) && double.IsFinite(y) &&
        width > 0 && height > 0 &&
        Math.Min(Left + Width, x + width) - Math.Max(Left, x) >= 90 &&
        Math.Min(Top + Height, y + height) - Math.Max(Top, y) >= 70;
}

internal sealed class UserSettings
{
    public string ServerUrl { get; set; } = "";
    public string ServerLabel { get; set; } = "coolify";
    public string Language { get; set; } = "system";
    public WindowPlacementState? WindowPlacement { get; set; }
    public string ReadToken { get; set; } = "";
    public string DeployToken { get; set; } = "";
    public Dictionary<string, DeploymentGroup> Overrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> CardOrders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> LockedPositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> BlockedDeploys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Https;
    public string SshHost { get; set; } = "";
    public string SshUsername { get; set; } = "";
    public int SshPort { get; set; } = 22;
    public string SshPrivateKey { get; set; } = "";
    public string SshKnownHosts { get; set; } = "";
    public int SshLocalPort { get; set; } = 18000;
    public string SshRemoteHost { get; set; } = "127.0.0.1";
    public int SshRemotePort { get; set; } = 8000;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ReadToken) &&
        !string.IsNullOrWhiteSpace(DeployToken) &&
        (ConnectionMode != ConnectionMode.Https || !string.IsNullOrWhiteSpace(ServerUrl));
    public string EffectiveUrl => ConnectionMode == ConnectionMode.Https
        ? ServerUrl : $"http://127.0.0.1:{SshLocalPort}";

    public UserSettings Copy() => new()
    {
        Language = Language, ServerUrl = ServerUrl, ServerLabel = ServerLabel, ReadToken = ReadToken, DeployToken = DeployToken,
        WindowPlacement = WindowPlacement,
        Overrides = new(Overrides, StringComparer.OrdinalIgnoreCase),
        CardOrders = CardOrders.ToDictionary(k => k.Key, k => new List<string>(k.Value), StringComparer.OrdinalIgnoreCase),
        LockedPositions = new(LockedPositions, StringComparer.OrdinalIgnoreCase),
        BlockedDeploys = new(BlockedDeploys, StringComparer.OrdinalIgnoreCase),
        ConnectionMode = ConnectionMode, SshHost = SshHost, SshUsername = SshUsername, SshPort = SshPort,
        SshPrivateKey = SshPrivateKey, SshKnownHosts = SshKnownHosts, SshLocalPort = SshLocalPort,
        SshRemoteHost = SshRemoteHost, SshRemotePort = SshRemotePort
    };
}

internal static class Classifier
{
    private static readonly HashSet<string> Stage = new(StringComparer.OrdinalIgnoreCase) { "staging", "stage", "test", "dev" };
    private static readonly HashSet<string> Prod = new(StringComparer.OrdinalIgnoreCase) { "production", "prod", "live", "release" };
    private static (bool stage, bool prod) Tokens(string input)
    {
        var words = Regex.Split(input ?? "", "[^a-zA-Z0-9]+", RegexOptions.CultureInvariant);
        return (words.Any(Stage.Contains), words.Any(Prod.Contains));
    }

    // Names and branches take precedence: Coolify environments may be called "services" or "production"
    // regardless of the application's actual deployment target.
    public static DeploymentGroup Classify(CoolifyApplication app, UserSettings settings)
    {
        if (settings.Overrides.TryGetValue(app.Uuid, out var saved)) return saved;
        var name = Tokens(app.Name);
        var branch = Tokens(app.Branch);
        var project = Tokens(app.Project);
        var environment = Tokens(app.Environment);
        bool stage = name.stage || branch.stage || project.stage;
        bool prod = name.prod || branch.prod || project.prod;
        if (stage && prod) return DeploymentGroup.Unknown;
        if (stage) return DeploymentGroup.Staging;
        if (prod) return DeploymentGroup.Production;
        if (environment.stage && environment.prod) return DeploymentGroup.Unknown;
        if (environment.stage) return DeploymentGroup.Staging;
        if (environment.prod) return DeploymentGroup.Production;
        return DeploymentGroup.Unknown;
    }
    public static string Explain(CoolifyApplication a, DeploymentGroup g) => g == DeploymentGroup.Unknown
        ? $"{a.Name} (branch={a.Branch}; env={a.Environment}): no unambiguous classification signal, or conflicting signals"
        : $"{a.Name}: {g} (branch={a.Branch}, env={a.Environment})";
}
