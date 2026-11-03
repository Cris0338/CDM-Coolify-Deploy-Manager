// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Refresh an existing restricted v3 personal clone using a new generic template.
// Upgrade 3.0.0/3.0.1 -> 3.0.2 keeps the profile and uses a versioned destination.
// No v2 migration, remote SSH operation, or credential logging.
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CoolifyDeployManager;

internal static class V3CloneRefresher
{
    private const string CurrentVersion = "3.0.2";

    // Filename alone NEVER grants migration eligibility. Require matching
    // Windows file-version metadata and a verified embedded v3 profile.
    internal static bool EligiblePersonalVersion(string filename, int major, int minor, int build) =>
        major == 3 && minor == 0 && build >= 0 && build <= 2 &&
        filename.StartsWith("CDM-", StringComparison.OrdinalIgnoreCase) &&
        filename.EndsWith("-v3.0." + build + ".exe", StringComparison.OrdinalIgnoreCase);

    internal static void VerifyUpgradePolicyForSmoke()
    {
        const string oldName = "CDM-Test-coolify-v3.0.0.exe";
        const string previousName = "CDM-Test-coolify-v3.0.1.exe";
        const string currentName = "CDM-Test-coolify-v3.0.2.exe";
        if (!EligiblePersonalVersion(oldName, 3, 0, 0) ||
            !EligiblePersonalVersion(previousName, 3, 0, 1) ||
            !EligiblePersonalVersion(currentName, 3, 0, 2) ||
            EligiblePersonalVersion(oldName, 3, 0, 1) ||
            EligiblePersonalVersion(previousName, 3, 0, 2) ||
            EligiblePersonalVersion(currentName, 3, 0, 1) ||
            EligiblePersonalVersion("CDM-Test-coolify-v2.2.3.exe", 2, 2, 3) ||
            EligiblePersonalVersion("CDM-Test-coolify-v3.1.0.exe", 3, 1, 0) ||
            EligiblePersonalVersion("CDM-Test-coolify-v3.0.3.exe", 3, 0, 3) ||
            VersionedExecutableName.ForClone(
                Path.Combine(Path.GetTempPath(), oldName), CurrentVersion) !=
                Path.Combine(Path.GetTempPath(), currentName) ||
            VersionedExecutableName.ForClone(
                Path.Combine(Path.GetTempPath(), previousName), CurrentVersion) !=
                Path.Combine(Path.GetTempPath(), currentName))
            throw new InvalidDataException("CDM v3.0.2 clone upgrade policy is invalid.");
    }
    // Returns the actual resulting path, because a patch-version upgrade
    // changes the filename but an intra-version refresh does not.
    internal static string Refresh(string personalExecutable)
    {
        var template = Path.GetFullPath(ExecutableProfile.OwnPath);
        if (!Path.GetFileName(template).Equals(
                VersionedExecutableName.Generic(BuildInfo.Version),
                StringComparison.OrdinalIgnoreCase) ||
            BuildInfo.Version != CurrentVersion ||
            ExecutableProfile.TryReadFile(template, out _))
            throw new InvalidOperationException(
                "Refresh must run from the NEW empty CDM-v3.0.2.exe template.");

        var target = Path.GetFullPath(personalExecutable);
        if (string.Equals(target, template, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(target))
            throw new FileNotFoundException(
                "Select an existing personalized CDM v3.0.0/v3.0.1/v3.0.2 executable.", target);

        var info = FileVersionInfo.GetVersionInfo(target);
        if (!EligiblePersonalVersion(Path.GetFileName(target),
                info.FileMajorPart, info.FileMinorPart, info.FileBuildPart))
            throw new InvalidDataException(
                "The original EXE must be a genuine CDM v3.0.0/v3.0.1/v3.0.2 binary; v2 migration is forbidden.");

        if (!ExecutableProfile.TryReadFile(target, out var saved) || saved?.IsConfigured != true)
            throw new InvalidDataException("An existing valid v3 personal profile is required.");
        if (saved.ConnectionMode == ConnectionMode.ManagedSshTunnel &&
            (!saved.RestrictedSshProvisioned ||
             !saved.SshUsername.EndsWith("-cdm", StringComparison.Ordinal) ||
             string.IsNullOrWhiteSpace(saved.SshPrivateKey) ||
             string.IsNullOrWhiteSpace(saved.SshKnownHosts)))
            throw new InvalidDataException(
                "This profile does not contain a verified v3 restricted SSH identity. Do not import administrator keys.");

        var upgradedPath = VersionedExecutableName.ForClone(target, CurrentVersion);
        bool upgrading = !string.Equals(target, upgradedPath, StringComparison.OrdinalIgnoreCase);
        if (upgrading && File.Exists(upgradedPath))
            throw new IOException("CDM v3.0.2 personal EXE already exists. Refusing to overwrite another profile.");

        var pending = Path.Combine(Path.GetDirectoryName(target)!,
            "." + Path.GetFileName(target) + ".pending");
        var newPending = Path.Combine(Path.GetDirectoryName(upgradedPath)!,
            "." + Path.GetFileName(upgradedPath) + ".pending");
        if (File.Exists(pending) || (upgrading && File.Exists(newPending)))
            throw new IOException("A personal clone has a pending profile save. Close it and resolve that save first.");
        using (var exclusive = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            if (exclusive.Length == 0)
                throw new InvalidDataException("Personal executable is empty.");
        }

        var staging = target + "." + Guid.NewGuid().ToString("N") + ".refresh.exe";
        var backup = target + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".pre-refresh.bak";
        bool replaced = false;
        try
        {
            ExecutableProfile.Build(template, staging, saved);
            if (!ExecutableProfile.TryReadFile(staging, out var staged) || staged is null ||
                JsonSerializer.Serialize(saved) != JsonSerializer.Serialize(staged))
                throw new InvalidDataException("Refreshed executable failed to preserve the full v3 profile.");
            var stagedVersion = FileVersionInfo.GetVersionInfo(staging);
            if (stagedVersion.FileMajorPart != 3 || stagedVersion.FileMinorPart != 0 ||
                stagedVersion.FileBuildPart != 2)
                throw new InvalidDataException("Refreshed executable does not contain version 3.0.2.");

            if (upgrading)
            {
                bool moved = false;
                bool startupRetargeted = false;
                try
                {
                    // No silent overwrite: another v3.0.2 clone may have appeared.
                    File.Move(staging, upgradedPath);
                    moved = true;
                    if (!ExecutableProfile.TryReadFile(upgradedPath, out var installed) ||
                        installed is null ||
                        JsonSerializer.Serialize(saved) != JsonSerializer.Serialize(installed))
                        throw new InvalidDataException("Upgraded personal EXE failed post-install profile verification.");

                    // Move ONLY a CDM startup value pointing at the exact old EXE.
                    // All unrelated Windows Run registrations remain untouched.
                    startupRetargeted = WindowsStartup.RetargetIfRegistered(target, upgradedPath);
                    // Keep no second credential-bearing executable after success.
                    File.Delete(target);
                    return upgradedPath;
                }
                catch
                {
                    if (startupRetargeted)
                    {
                        try { WindowsStartup.RetargetIfRegistered(upgradedPath, target); }
                        catch (Exception ex)
                        {
                            // Preserve both versions if the registry cannot be
                            // restored; deleting the new path would break autostart.
                            throw new IOException(
                                "CDM refresh failed and Windows autostart rollback also failed. Both EXEs were retained; inspect the startup registration.", ex);
                        }
                    }
                    if (moved && File.Exists(upgradedPath)) File.Delete(upgradedPath);
                    throw;
                }
            }

            // Same-version refresh preserves the EXE path and registry entry.
            File.Replace(staging, target, backup);
            replaced = true;
            if (!ExecutableProfile.TryReadFile(target, out var refreshed) || refreshed is null ||
                JsonSerializer.Serialize(saved) != JsonSerializer.Serialize(refreshed))
                throw new InvalidDataException("Installed executable failed post-replacement profile verification.");
            File.Delete(backup);
            return target;
        }
        catch
        {
            if (replaced && File.Exists(backup))
            {
                try { File.Copy(backup, target, overwrite: true); }
                catch (IOException) { /* Retain backup for explicit manual recovery. */ }
            }
            throw;
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }
}
