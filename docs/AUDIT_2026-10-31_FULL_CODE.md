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

## 2026-10-31 — Root cause of the Windows deferred-save exception found

The owner reran the full Windows script after the lifecycle redesign. Publish PASS (SHA256 `FD60F67E27595F814E7A938F7FC0EDE1787D7E2E10BC816EFC09330649EA2951`), generic launch PASS, status model PASS, disposable clone launch PASS, and explicit clone upgrade PASS. The staged clone reached exit, after which the updater reported:

```
CDM update helper failed: MethodInvocationException -
Exception calling "Replace" with "3" arguments:
"The path is not of a legal form."
```

**Identified incompatible call:** `[IO.File]::Replace($pending,$target,$null)` in a Windows PowerShell 5.1 child process. PowerShell's argument binder can coerce `$null` to the empty string for an argument whose .NET type is `string`. The `File.Replace` API then receives an invalid empty backup path, producing exactly this exception. This was not a lock timeout; retry changes could never fix this deterministic argument failure. References: [PowerShell issue #4616](https://github.com/PowerShell/PowerShell/issues/4616), [Microsoft File.Replace documentation](https://learn.microsoft.com/dotnet/api/system.io.file.replace).

**Targeted correction — not yet Windows-verified:** `ExecutableProfile.ScheduleReplaceOnExit` now passes a real full backup path `<personal-clone>.before-save.bak` to `File.Replace`. The same path is overwritten with the latest previously installed clone at each successful save, avoiding an unbounded collection of ordinary save backups. Both the backup and current clone contain real credentials; keep them private. The build includes a small isolated Windows PowerShell `File.Replace` compatibility proof using dummy `.txt` files **before** publishing and, in the end-to-end disposable-clone smoke, an SHA256 comparison proving the last-good backup is byte-identical to the pre-save personal clone. No network, real key, or production operation involved.

This fixes a specific, source-grounded cause; **do not mark the close-time save fixed until the owner's Windows script prints all PASS lines**.

## 2026-10-31 — Owner Windows acceptance: deferred save regression PASSED

Owner's full local Windows `BUILD_PORTABLE.ps1` completed with `Build completed` and no failure. Generic EXE SHA256: `76F77AA16D001AFEED984C62C463CA605C20684FD86BFD518A442FF40FF98D79`, size approximately 64.55 MiB. Relevant PASS results:

- Windows PowerShell `File.Replace` dummy-file compatibility with real backup path.
- Generic `CDM.exe` contains no profile; WPF in-memory unhealthy→healthy update.
- Disposable personalized clone starts and keeps embedded credentials after explicit upgrade.
- Disposable clone stages profile changes and exits.
- Close-time helper creates an SHA256-identical last-good backup of the original disposable EXE.
- Close-time `.pending` is applied, retaining API token, card ordering and locked positions.

**BLOCKER RESOLVED BY WINDOWS EVIDENCE:** Previous deferred-save failure due to `File.Replace($pending,$target,$null)` in Windows PowerShell, plus premature helper scheduling during the session, has now passed an end-to-end synthetic Windows test after architectural correction and valid backup path. The personal `CDM-Cris0338-coolify.exe` **has not been proven upgraded or validated** by this smoke; its explicit upgrade and manual UI/API/SSH acceptance are separate next steps. Existing HIGH/MEDIUM findings in the audit remain open; do not imply release security acceptance.

## 2026-10-31 — Managed SSH tunnel unexpectedly stops once after launch

Owner confirms the updated personalized clone launched, connected to Coolify and worked. Approximately one minute later, UI displayed `Tunnel SSH non attivo`; pressing Refresh re-established `SSH automatico connesso`. The disconnection seemed to occur only once.

Code inspection found a deterministic reason for the **time of the warning**, not yet for the underlying SSH process exit: `MainWindow` uses a 60-second `DispatcherTimer`, `CheckTunnelState()` only checked `!ssh.IsRunning` and changed the badge to disconnected without recovering; manually pressing Refresh invokes `EnsureRunningAsync` to start a new ssh.exe. `SshTunnel` previously retained SSH stderr only during initial startup, losing useful diagnostics for later exits. No proof yet of why ssh.exe exits (remote SSH close, local OpenSSH error, process crash, etc.).

Fix committed on the feature branch (needs Windows compile, smoke and real runtime observation):
- `SshTunnel.DescribeStoppedProcess` records the terminated process's exit code plus a bounded stderr excerpt; the stderr-reading task now stays in the object after startup and is cleared with `Stop`.
- The timer asynchronously calls `CheckTunnelStateAsync`, which automatically invokes the existing secure managed-tunnel startup when a stopped ssh.exe is observed. It attempts recovery once per tick and safely logs failures for diagnosis. It does **not** trigger any deployments.
- A `SemaphoreSlim` serializes manual Refresh and automatic reconnection, avoiding attempts to use/replace the same local forwarded port concurrently.
- Close cancels a pending recovery; opening the settings dialog is disabled during one. Normal active tunnel, non-SSH modes, ongoing deployment, missing configuration and app shutdown all prevent automatic reconnection.
- New no-network `--verify-tunnel-recovery-policy` WPF smoke in `BUILD_PORTABLE.ps1` checks these eligibility guards.

**Status:** Source checked on GitHub; WPF build and user-observed SSH reconnect **NOT YET VERIFIED**. The actual reason for the original ssh.exe exit remains unknown until the owner's log records the exit code/stderr.

### Regression provenance check: cards vs. managed SSH

The owner reported that the first-minute stopped-tunnel symptom was absent in the initial working version and only appeared after changes to the cards. Compared `MainWindow.xaml.cs` and `SshTunnel.cs` on the history before the UI card changes (`ba9c8b776`), early card implementation (`3c6e5c9b9`), the card drag refactor (`180f97f94`) and the newer post-deploy status changes (`6572f7264`). The one-minute `DispatcherTimer`, `CheckTunnelState()` and startup `RefreshAsync()` sequence already existed **before** the card edits. The SSH tunnel service blob was identical at `ba9c8b776` and `3c6e5c9b9` (`44b400bb17ef27f0ac361e99f00265460ba24ed9`); later card drag work only coincided with a small other change, and `SshTunnel.cs` remained identical at `180f97f94` and `6572f7264` (`b67a64c379eedc50cc185e95e63cf6c443dfcc66`). This **does not disprove an indirect UI-related regression**, but shows no direct introduction of a 60-second SSH timeout in card code. Preserve the owner-reported chronology as a test clue; rely on newly added OpenSSH exit code/stderr plus reproducible runtime evidence to determine whether the tunnel was closed by SSH, the local process or an interaction in CDM. No further speculative SSH patches added in this historical review.
