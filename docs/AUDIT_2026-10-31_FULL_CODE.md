# 2026-10-31 — CDM 2.2 comprehensive source review

Repository: `Cris0338/CDM-Coolify-Deploy-Manager`  
Branch: `feature/embedded-clone-20261031`  
Status: **static source review, NOT successful Windows acceptance and NOT a penetration test.** No production deployment or public release initiated.

## What was reviewed

Tree of the entire development branch (via GitHub commit/tree), all C# modules, WPF XAML views/resources, configuration and localization model, the 3 test programs, `BUILD_PORTABLE.ps1`, `PACKAGE_RELEASE.ps1`, `.gitignore`, and the project metadata. The 14 JSON catalogs are covered by the Windows localization regression result; EN/FR/IT are complete, 11 are explicit English-fallback previews. Existing docs/changelogs checked for intended rules and contradictions. No private binary, token or SSH key was opened or requested. In particular:
- Lifecycle: `App.xaml.cs`, `SettingsStore.cs`, `ExecutableProfile.cs`.
- Data/API: `Models.cs`, `CoolifyApi.cs`, `SshTunnel.cs`, `SettingsWindow.xaml(.cs)`.
- UI and state: `MainWindow.xaml(.cs)`, `ViewModels/AppCardView.cs`, `App.xaml`, splash/about/guide, `Loc.cs`, translation catalogs.
- Builds, packaging and tests: `CoolifyDeployManager.csproj`, `BUILD_PORTABLE.ps1`, `PACKAGE_RELEASE.ps1`, test projects.
The binary-only approved logo/ICO are validated by the Windows hash test, not inspected as source here.

## Verified from the owner's Windows output

- Fresh single-file WPF .NET 10 publish **PASS**.
- Classification **8/8 PASS**; locale catalogs **PASS** (EN/FR/IT 226/226); profile integrity/round-trip **PASS**; allowed/disallowed URL test **PASS**.
- Generic profile-free launch **PASS**, in-memory unhealthy→healthy UI model test **PASS**, disposable clone startup **PASS**, explicit upgrade preserving its embedded test profile **PASS**.
- **BLOCKER:** a saved disposable clone exits and leaves its `.pending` unapplied. Old test's process-tree wait was also a separate issue. We have **not** shown a successful deferred-save end-to-end result on Windows. The health **network polling** and updated WPF UI still need actual acceptance.

## Confirmed structural defect behind the save failure (critical)

Original `SettingsStore.Save` launched `ExecutableProfile.ScheduleReplaceOnExit` at the **first setting/card edit**. Its PowerShell child attempted to wait for process termination or, more recently, retried for a finite window. The owner may keep CDM open for arbitrary time after the first edit. A helper launched this early necessarily can expire **before program exit**, leaving `.pending` behind. Extending the retry count or changing File.Move/File.Replace alone cannot guarantee a save. The synthetic test additionally exposed an unverified helper startup/replacement mechanism; its exact Windows failure mode has not yet been observed from the helper's diagnostics.

**Architectural correction now on feature branch (awaiting Windows validation):**
1. Each successful in-app `Save` atomically stages a complete checksum-verified `.pending` file and updates in-memory state. No updater launches while the app is open.
2. WPF `App.OnExit` schedules the helper **once**, after the UI is closing.
3. Detached PowerShell invocation uses Unicode `-EncodedCommand` to survive spaces/quotes in paths. It retries same-volume `File.Replace` for a bounded period while the exiting executable releases its image.
4. Safe `.updater-started.txt` and `.updater-error.txt` markers distinguish failure classes; no credential bytes in arguments or diagnostics. Failed `.pending` is preserved.
5. The disposable smoke asserts a valid staged file and no prematurely launched updater; it waits for the clone process only, then polls helper completion and checks token, card order and position locks.
6. On smoke failure, the synthetic-only test directory is retained so diagnostics are not deleted; successful runs clean it up.
**Do not declare the fix working until a full `BUILD_PORTABLE.ps1` run prints the final `PASS: close-time .pending applied...` and exits normally.**

## Other concrete findings (not secretly modified in this pass)

| Priority | Area | Finding / consequence | Status |
| --- | --- | --- | --- |
| HIGH | HTTP transport | `CoolifyApi.IsLocalHttpUri` treats **any single-label hostname** (and some local-looking suffixes) as trusted for direct HTTP without resolving its actual IP. A name that resolves outside the intended private LAN could expose a bearer token. The current tests only verify strings, not DNS resolution or the actual socket peer. | **OPEN**, require private-address validation at connection time or route such names through HTTPS/SSH. |
| MEDIUM | SSH identity persistence | `SettingsWindow.Save_Click` copies prior `SshPrivateKey` and `SshKnownHosts` even when switching from managed SSH to HTTPS, retaining credentials which might no longer be needed. | **OPEN**, decide whether switching to HTTPS should clear retired SSH material (with explicit UI notice). |
| MEDIUM | SSH configuration reuse | `SshTunnel.EnsureRunningAsync` calculates `configSignature` from host, username and ports but not key or pinned host key. Within the wizard a rotated key with otherwise unchanged fields can reuse an old authenticated tunnel. | **OPEN**, include a nonsecret hash of authentication material in the signature or force reconnection when it changes. |
| MEDIUM | Upgrade integrity | `App.xaml.cs` compares only API read token and SSH private/host key when verifying upgraded clones. Full preserved user settings, card positions and lock state are not checked after the upgrade. Tests start with simple synthetic state. | **OPEN**, extend semantic verification and upgrade regression with full settings. |
| MEDIUM | Status UI | `MainWindow.OpenSettings` reconstructs cards from the old inventory `app.Status` and copies only previous LatestDeploy/IsSelected, so a health state that was updated by read-only polling can temporarily revert until a fresh inventory read. | **OPEN**, preserve previous observed runtime status or read it again. |
| MEDIUM | SSH temporary files | Abrupt process termination or a Windows file handle can leave the temporary SSH private-key file on disk; cleanup is best effort in `SshTunnel.Stop`. | **OPEN**, security/privacy tradeoff, document or add safe stale-directory cleanup. |
| LOW/MEDIUM | Refresh UX | `RefreshAsync` reconstructs `allCards` and does not carry staging checkbox selection into new card instances; selected staging set is lost on manual/after-deploy refresh. | **OPEN**, only if selections are intended to persist within a session. |
| LOW/MEDIUM | UI responsiveness | Profile `Save` synchronously copies the ~65 MB executable each time a card moves or lock changes, potentially blocking the WPF UI briefly on slow disks. | **OPEN**, optimize only after correctness tests pass. |
| LOW | Modes | `ConnectionMode.ExternalLocalTunnel` remains in the model and service, but the settings wizard only exposes direct mode and managed SSH. A legacy external-tunnel selection cannot be reselected through the current wizard. | **OPEN**, clarify supported mode in UI/docs and remove or expose consistently. |
| LOW | Documentation | Some older README/checklist text still describes a drag grip or legacy behavior, whereas current main-window code drags by the card surface. Old checkpoint claims may predate latest UI work. | **OPEN**, reconcile before release. |
| LOW | Test coverage | Most checks are synthetic unit/offline process tests. No automated hostile-network, real deployment polling, external-token-permission, full WPF visual, or cross-PC portability tests. | **OPEN**, required for release claim. |

## Positive controls observed

- No implicit reuse of old Windows account settings; generic CDM starts without secrets.
- HTTPS uses default .NET certificate verification; remote public IP HTTP literals are refused.
- Managed SSH uses OpenSSH `StrictHostKeyChecking=yes`, an independently verified fingerprint and Windows ACL restriction on the temporary private-key file; it forwards to `127.0.0.1`.
- Explicit user confirmation for deployment, separately read-only health/status endpoints, deployment locks and block rules.
- Source repository ignores personal executables/backups/keys; release package refuses a generic template carrying the profile trailer and requires the offline tests to pass.
- WPF binding `AppCardView.UpdateApplicationStatus` raises property-changed notifications for visible health state, independently of deployment completion.
- `PACKAGE_RELEASE.ps1` does not upload or deploy anything; GitHub Actions are not configured.

## Testing sequence and nonnegotiable constraints

1. Run Windows `git pull --ff-only` and `BUILD_PORTABLE.ps1`. Preserve exact output. **No real personalized clone upgrade before all disposable smoke tests succeed.**
2. Only after PASS: close the personal clone, explicitly upgrade it using generic `CDM.exe --upgrade-clone`, retain its existing timestamped backup and test close-time profile saving.
3. Evaluate real health polling only when the owner explicitly approves an actual staging deployment; no automated rollout.
4. Address remaining HIGH and MEDIUM findings in distinct coherent changes with dedicated regressions, not broad untested edits. Do not introduce a password or SID/DPAPI binding; the owner deliberately chose a truly portable clone with documentation for revoking Coolify API and SSH authorizations if lost.
5. No GitHub Actions, no merge, tag or Release without the owner's approval.

## Audit limitations

Static inspection can reveal control-flow and data-flow risks; it cannot prove Windows kernel file-sharing behavior, PowerShell execution policy under every account, HTTPS peer security, correct Coolify API response structure, or GUI visuals. Any new source changes remain *unverified* until the owner runs the Windows integration tests.

## Additional static consistency checks

Verified all six WPF XAML files against their code-behind: **no missing named event handlers**, **no missing dynamic `L.*` translation resource keys** in the English fallback, and **no mismatched/unclosed XML-like tags** in the simple static tag scan. Checked current `SettingsStore.Save` only stages updates, `App.OnExit` schedules the updater, and `ExecutableProfile.ScheduleReplaceOnExit` uses Unicode-encoded PowerShell with safe status markers. These checks are useful but **do not substitute for a Windows WPF compiler or the close-time smoke test**.
