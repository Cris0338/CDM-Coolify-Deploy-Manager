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
