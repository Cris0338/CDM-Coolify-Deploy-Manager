// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace CoolifyDeployManager;

internal static class SettingsStore
{
    private static readonly string LegacyFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoolifyDeployManager");
    private static readonly string LegacyPath = Path.Combine(LegacyFolder, "settings.json");
    private static readonly PortableVault Vault = new(Path.Combine(AppContext.BaseDirectory, "CDM.profile"));
    private static string? sessionPassword;
    private static UserSettings? cached;

    public static bool HasPortableProfile => Vault.Exists;
    public static bool IsConfigured => cached?.IsConfigured == true;

    // Called before the main WPF window is created. Canceling exits rather than
    // discarding or silently resetting encrypted settings.
    public static bool Initialize()
    {
        if (Vault.Exists)
        {
            while (true)
            {
                var dialog = new ProfilePasswordWindow(isCreating: false, isMigrating: false);
                if (dialog.ShowDialog() != true) return false;
                try
                {
                    var data = Vault.Read(dialog.EnteredPassword);
                    cached = Normalize(JsonSerializer.Deserialize<UserSettings>(data)
                        ?? throw new InvalidDataException("Portable profile has no settings."));
                    sessionPassword = dialog.EnteredPassword;
                    return true;
                }
                catch (Exception ex) when (ex is CryptographicException or JsonException or InvalidDataException or FormatException)
                {
                    MessageBox.Show(Loc.T("VaultUnlockFailed"), Loc.T("VaultUnlockTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        if (File.Exists(LegacyPath))
        {
            var prior = LoadLegacy();
            Loc.Apply(prior.Language);
            var dialog = new ProfilePasswordWindow(isCreating: true, isMigrating: true);
            if (dialog.ShowDialog() != true) return false;
            WritePortable(prior, dialog.EnteredPassword);
            return true;
        }
        return true;
    }

    public static UserSettings Load()
    {
        if (cached is not null) return cached.Copy();
        return Vault.Exists
            ? throw new InvalidOperationException("The portable profile must be unlocked first.")
            : LoadLegacy();
    }

    public static void Save(UserSettings settings)
    {
        if (sessionPassword is null)
        {
            if (Vault.Exists) throw new InvalidOperationException("The portable profile is locked.");
            var dialog = new ProfilePasswordWindow(isCreating: true, isMigrating: File.Exists(LegacyPath));
            if (Application.Current?.MainWindow is { IsVisible: true } owner) dialog.Owner = owner;
            if (dialog.ShowDialog() != true) throw new OperationCanceledException("Portable profile creation was canceled.");
            WritePortable(settings, dialog.EnteredPassword);
            return;
        }
        WritePortable(settings, sessionPassword);
    }

    private static void WritePortable(UserSettings value, string password)
    {
        var normalized = Normalize(value.Copy());
        var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
        Vault.Write(password, json);

        // Verify the round trip before discarding the previous DPAPI-backed file.
        _ = JsonSerializer.Deserialize<UserSettings>(Vault.Read(password))
            ?? throw new InvalidDataException("Portable profile verification failed.");
        cached = normalized;
        sessionPassword = password;
        if (File.Exists(LegacyPath)) File.Delete(LegacyPath);
    }

    private static UserSettings Normalize(UserSettings value)
    {
        value.Overrides = new Dictionary<string, DeploymentGroup>(
            value.Overrides ?? new(), StringComparer.OrdinalIgnoreCase);
        value.CardOrders = (value.CardOrders ?? new())
            .ToDictionary(pair => pair.Key, pair => pair.Value ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
        value.Language ??= "system";
        value.ReadToken ??= "";
        value.DeployToken ??= "";
        value.ServerUrl ??= "";
        value.SshHost ??= "";
        value.SshRemoteHost ??= "127.0.0.1";
        return value;
    }

    // One-time migration from earlier WinForms/WPF releases. Tokens were
    // encrypted with Windows DPAPI CurrentUser in %LOCALAPPDATA%.
    private static UserSettings LoadLegacy()
    {
        if (!File.Exists(LegacyPath)) return new();
        var saved = JsonSerializer.Deserialize<LegacySettings>(File.ReadAllText(LegacyPath)) ?? new();
        return Normalize(new UserSettings
        {
            ServerUrl = saved.ServerUrl ?? "",
            Language = saved.Language ?? "system",
            ReadToken = UnprotectLegacy(saved.ReadTokenProtected),
            DeployToken = UnprotectLegacy(saved.DeployTokenProtected),
            Overrides = new(saved.Overrides ?? new(), StringComparer.OrdinalIgnoreCase),
            CardOrders = new(saved.CardOrders ?? new(), StringComparer.OrdinalIgnoreCase),
            ConnectionMode = saved.ConnectionMode == ConnectionMode.Https &&
                saved.ServerUrl?.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase) == true
                    ? ConnectionMode.ExternalLocalTunnel : saved.ConnectionMode,
            SshHost = saved.SshHost ?? "",
            SshLocalPort = saved.SshLocalPort is > 0 and < 65536 ? saved.SshLocalPort : 18000,
            SshRemoteHost = saved.SshRemoteHost ?? "127.0.0.1",
            SshRemotePort = saved.SshRemotePort is > 0 and < 65536 ? saved.SshRemotePort : 8000
        });
    }

    private static string UnprotectLegacy(string? value) =>
        string.IsNullOrEmpty(value) ? "" : Encoding.UTF8.GetString(
            ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));

    private sealed class LegacySettings
    {
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
