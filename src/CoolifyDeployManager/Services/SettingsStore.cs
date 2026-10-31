// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
// Generic CDM.exe has no profile; configured clones store everything inside their own EXE.
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace CoolifyDeployManager;

internal static class SettingsStore
{
    private static readonly string LegacyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CoolifyDeployManager", "settings.json");
    private static UserSettings? cached;
    private static bool isClone;
    private static bool updaterStarted;
    private static string? stagedPath;
    public static bool HasPortableProfile => isClone;
    public static bool LaunchedClone { get; private set; }

    public static bool Initialize()
    {
        isClone = ExecutableProfile.TryRead(out var saved);
        if (isClone) cached = Normalize(saved ?? new UserSettings());
        else
        {
            // An unreadable legacy DPAPI file must not prevent the generic
            // template from opening the new setup wizard.
            try { cached = Normalize(LoadLegacy()); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or CryptographicException or JsonException or FormatException)
            {
                cached = Normalize(new UserSettings());
            }
        }
        return true;
    }

    public static UserSettings Load() => (cached ?? new UserSettings()).Copy();

    public static void Save(UserSettings settings)
    {
        var normalized = Normalize(settings.Copy());
        if (!normalized.IsConfigured)
            throw new ArgumentException("Enter a Coolify token and complete the connection details.");
        if (!isClone)
        {
            var target = ExecutableProfile.NextClonePath(normalized.ServerLabel);
            ExecutableProfile.Build(ExecutableProfile.OwnPath, target, normalized);
            // Verify the file was created before changing the current session.
            if (new FileInfo(target).Length <= new FileInfo(ExecutableProfile.OwnPath).Length)
                throw new InvalidDataException("The personalized executable is incomplete.");
            ExecutableProfile.Launch(target);
            cached = normalized;
            LaunchedClone = true;
            // Preserve older releases' DPAPI settings until the new clone has been
            // verified on Windows; never erase a user's previous working recovery path.
            Application.Current.Dispatcher.BeginInvoke(new Action(Application.Current.Shutdown));
            return;
        }

        var pending = stagedPath ?? Path.Combine(
            Path.GetDirectoryName(ExecutableProfile.OwnPath)!,
            "." + Path.GetFileName(ExecutableProfile.OwnPath) + ".pending");
        // Stage safely before updating the running session. A helper replaces the EXE on exit.
        ExecutableProfile.Build(ExecutableProfile.OwnPath, pending, normalized);
        if (!updaterStarted)
        {
            ExecutableProfile.ScheduleReplaceOnExit(pending);
            updaterStarted = true;
        }
        stagedPath = pending;
        cached = normalized;
    }

    private static UserSettings Normalize(UserSettings value)
    {
        value.Overrides = new(value.Overrides ?? new(), StringComparer.OrdinalIgnoreCase);
        value.CardOrders = (value.CardOrders ?? new()).ToDictionary(
            p => p.Key, p => p.Value ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        value.Language ??= "system";
        value.ReadToken ??= "";
        value.DeployToken = string.IsNullOrWhiteSpace(value.DeployToken)
            ? value.ReadToken : value.DeployToken;
        value.ServerUrl ??= "";
        value.ServerLabel = string.IsNullOrWhiteSpace(value.ServerLabel)
            ? "coolify" : value.ServerLabel;
        value.SshHost ??= "";
        value.SshUsername ??= "";
        value.SshPrivateKey ??= "";
        value.SshKnownHosts ??= "";
        value.SshRemoteHost ??= "127.0.0.1";
        return value;
    }

    // Read old Windows DPAPI settings only to prefill the first-run wizard.
    // No new application-controlled configuration is written to AppData.
    private static UserSettings LoadLegacy()
    {
        if (!File.Exists(LegacyPath)) return new();
        var saved = JsonSerializer.Deserialize<LegacySettings>(File.ReadAllText(LegacyPath)) ?? new();
        return Normalize(new UserSettings {
            ServerUrl = saved.ServerUrl ?? "",
            Language = saved.Language ?? "system",
            ReadToken = Unprotect(saved.ReadTokenProtected),
            DeployToken = Unprotect(saved.DeployTokenProtected),
            Overrides = saved.Overrides ?? new(),
            CardOrders = saved.CardOrders ?? new(),
            ConnectionMode = saved.ConnectionMode,
            SshHost = saved.SshHost ?? "",
            SshLocalPort = saved.SshLocalPort is > 0 and <= 65535 ? saved.SshLocalPort : 18000,
            SshRemoteHost = saved.SshRemoteHost ?? "127.0.0.1",
            SshRemotePort = saved.SshRemotePort is > 0 and <= 65535 ? saved.SshRemotePort : 8000,
        });
    }

    private static string Unprotect(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : Encoding.UTF8.GetString(
            ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));

    private sealed class LegacySettings {
        public string? ServerUrl { get; set; }
        public string? Language { get; set; }
        public string? ReadTokenProtected { get; set; }
        public string? DeployTokenProtected { get; set; }
        public Dictionary<string, DeploymentGroup>? Overrides { get; set; }
        public Dictionary<string, List<string>>? CardOrders { get; set; }
        public ConnectionMode ConnectionMode { get; set; }
        public string? SshHost { get; set; }
        public int SshLocalPort { get; set; }
        public string? SshRemoteHost { get; set; }
        public int SshRemotePort { get; set; }
    }
}
