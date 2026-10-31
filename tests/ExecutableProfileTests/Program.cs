// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using CoolifyDeployManager;

static void Check(bool valid, string explanation)
{
    if (!valid) throw new Exception("FAIL: " + explanation);
    Console.WriteLine("PASS: " + explanation);
}
var folder = Directory.CreateTempSubdirectory("cdm-profile-tests-").FullName;
try
{
    var template = Path.Combine(folder, "template.exe");
    var first = Path.Combine(folder, "clone.exe");
    var second = Path.Combine(folder, "updated.exe");
    var binary = new byte[8192];
    Random.Shared.NextBytes(binary);
    File.WriteAllBytes(template, binary);
    Check(!ExecutableProfile.TryReadFile(template, out _), "Generic executable has no profile");

    var settings = new UserSettings {
        ServerLabel = "contabo", ServerUrl = "https://coolify.example.com",
        ReadToken = "secret-read-and-deploy-token", DeployToken = "secret-read-and-deploy-token",
        SshPrivateKey = "test-SSH-private-key", SshKnownHosts = "test-host-key",
        Overrides = new() { ["app123"] = DeploymentGroup.Staging }
    };
    ExecutableProfile.Build(template, first, settings);
    Check(ExecutableProfile.TryReadFile(first, out var loaded), "Clone contains a readable profile");
    Check(loaded!.ReadToken == settings.ReadToken && loaded.SshPrivateKey == settings.SshPrivateKey,
        "Credentials survive serialization without Windows DPAPI");
    Check(loaded.Overrides["app123"] == DeploymentGroup.Staging, "User card state survives");
    var firstBytes = File.ReadAllBytes(first);
    Check(firstBytes.AsSpan(0, binary.Length).SequenceEqual(binary), "Base executable bytes are unchanged");

    loaded.ServerLabel = "coolify";
    ExecutableProfile.Build(first, second, loaded);
    Check(ExecutableProfile.TryReadFile(second, out var updated) && updated!.ServerLabel == "coolify",
        "Clone can be rebuilt from another clone");
    Check(new FileInfo(first).Length == new FileInfo(second).Length,
        "Reconfiguring a clone replaces the embedded profile rather than nesting it");
    var changed = File.ReadAllBytes(second);
    changed[changed.Length - 60] ^= 1;
    File.WriteAllBytes(second, changed);
    bool rejected = false;
    try { _ = ExecutableProfile.TryReadFile(second, out _); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Modified embedded profile is detected");
}
finally { Directory.Delete(folder, recursive: true); }
Console.WriteLine("Offline executable-profile tests passed.");

Console.WriteLine("== Coolify connection URLs (offline, no API requests) ==");
foreach (var url in new[] {
    "http://127.0.0.1:8000", "http://192.168.1.50:8000",
    "http://10.0.0.42:8000", "http://172.16.0.3:8000",
    "http://100.64.0.10:8000", "http://[fd12::34]:8000",
    "http://nas.local:8000", "http://homeserver:8000",
    "https://coolify.example.com", "https://169.58.7.207:8000"
})
    Check(CoolifyApi.NormalizeUrl(url) == url, "Allowed direct URL: " + url);

foreach (var url in new[] {
    "http://169.58.7.207:8000", "http://coolify.example.com",
    "http://192.168.1.50:8000?secret=yes",
    "http://username:password@192.168.1.50:8000"
})
{
    bool rejected = false;
    try { _ = CoolifyApi.NormalizeUrl(url); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected, "Remote public HTTP or unsafe URL rejected: " + url);
}

