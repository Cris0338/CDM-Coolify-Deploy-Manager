// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoolifyDeployManager;

internal static class SettingsStore
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoolifyDeployManager");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    // Compatible with previous WinForms settings: API tokens use DPAPI CurrentUser.
    public static UserSettings Load()
    {
        if (!File.Exists(FilePath)) return new();
        var saved = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(FilePath)) ?? new();
        return new UserSettings
        {
            ServerUrl = saved.ServerUrl ?? "",
            Language = saved.Language ?? "system",
            ReadToken = Decrypt(saved.ReadTokenProtected),
            DeployToken = Decrypt(saved.DeployTokenProtected),
            Overrides = new(saved.Overrides ?? new(), StringComparer.OrdinalIgnoreCase),
            CardOrders = new(saved.CardOrders ?? new(), StringComparer.OrdinalIgnoreCase),
            ConnectionMode = saved.ConnectionMode == ConnectionMode.Https &&
                saved.ServerUrl?.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase) == true
                    ? ConnectionMode.ExternalLocalTunnel : saved.ConnectionMode,
            SshHost = saved.SshHost ?? "",
            SshLocalPort = saved.SshLocalPort is > 0 and < 65536 ? saved.SshLocalPort : 18000,
            SshRemoteHost = saved.SshRemoteHost ?? "127.0.0.1",
            SshRemotePort = saved.SshRemotePort is > 0 and < 65536 ? saved.SshRemotePort : 8000
        };
    }
    public static void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var value = new StoredSettings
        {
            ServerUrl = settings.ServerUrl.Trim(),
            Language = settings.Language,
            ReadTokenProtected = Encrypt(settings.ReadToken),
            DeployTokenProtected = Encrypt(settings.DeployToken),
            Overrides = settings.Overrides,
            CardOrders = settings.CardOrders,
            ConnectionMode = settings.ConnectionMode,
            SshHost = settings.SshHost,
            SshLocalPort = settings.SshLocalPort,
            SshRemoteHost = settings.SshRemoteHost,
            SshRemotePort = settings.SshRemotePort
        };
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.Move(temp, FilePath, true);
    }
    private static string Encrypt(string value) => Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    private static string Decrypt(string? encrypted) => string.IsNullOrEmpty(encrypted) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser));

    private sealed class StoredSettings
    {
        public string? ServerUrl { get; set; } = "";
        public string? Language { get; set; } = "system";
        public string? ReadTokenProtected { get; set; } = "";
        public string? DeployTokenProtected { get; set; } = "";
        public Dictionary<string, DeploymentGroup>? Overrides { get; set; } = new();
        public Dictionary<string, List<string>>? CardOrders { get; set; } = new();
        public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Https;
        public string? SshHost { get; set; } = "";
        public int SshLocalPort { get; set; } = 18000;
        public string? SshRemoteHost { get; set; } = "127.0.0.1";
        public int SshRemotePort { get; set; } = 8000;
    }
}
