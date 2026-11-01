# CDM — Roadmap and acceptance tracker

Updated 2026-11-01. Author: Cris0338. Project independent of Chataide.fr.

## Phase 1 — CDM 2.2.1 Windows pre-release

- [x] Windows .NET 10 single-file generic EXE builds.
- [x] Generic EXE starts blank without reading old credentials.
- [x] Personal EXE creation, upgrade, recoverable prior-version backup and on-exit profile saving verified with disposable synthetic tests.
- [x] Card UI and status/deploy history model tested offline; owner previously accepted working card drag/drop.
- [x] Managed SSH reconnect eligibility tested offline, and diagnostic exit detail added.
- [x] Actual owner personal clone connects to Coolify; SSH observed stable for more than 14 minutes.
- [x] Version, Git source revision and UTC build timestamp shown in About and testable in generic/personalized EXEs.
- [x] Release notes, license and generic-asset packaging safeguards prepared.
- [x] Publish **v2.2.1 as GitHub pre-release** on 2026-10-31, with exactly three approved generic assets (EXE, ZIP and SHA256); verified the tag and each uploaded asset on GitHub.

**Distributed binary source revision:** `3ad03bb74cfbfbd84f19a04c677892b1c693662f` (tag `v2.2.1`); SHA256 `4B50975A82F6F542388B033DDB20B266C9A0F430FAF39BDC2423C56628E3915B`. The earlier `56ec905c0b68` build was separately tested, and the final release packaging independently passed all smoke tests. No personal clone, key or API token belongs in GitHub. Later commits document the release without modifying its executable code.

## Phase 2 — Field validation deferred to future ordinary usage (CDM development closed)

- [ ] Perform a deliberately confirmed real staging deploy in normal use; record outcome.
- [ ] Exercise actual SSH reconnect on spontaneous disconnect, if it recurs; inspect exit code/stderr.
- [ ] Verify portability on a second Windows computer/account.
- [ ] Review remaining static-audit findings before a future fully stable release: remote HTTP hostname classification, SSH configuration change detection, optional clearing of unused private credentials, remaining UI/state consistency concerns.
- [ ] Version increment and release only for an actual new software change; avoid modifying frozen 2.2.1 code speculatively.

**Maintenance rule:** log each engineering step and test with branch, commits, files, result and next step in the technical diary. No GitHub Actions, automated deployments, or uploading credential-bearing personal EXEs.

## Phase 3 — CDM 2.2.2 development branch (2026-11-01)

- [x] Investigate actual Coolify deployment endpoint and confirm four-second server-status polling already exists.
- [x] Add a per-card spinner gated by real `queued/in_progress` deployment states (no invented progress).
- [x] Decouple card reordering from `busy` while retaining inventory rebuild/filter/position-lock guards.
- [x] Add offline WPF entrypoint to test state transitions, property notifications and drag policy.
- [x] Restrict terminal log to incoming/outgoing Coolify/SSH activity and related errors; move local blocked-move and skipped-deploy feedback to status bar.
- [x] Add build-time guard against logging nine internal UI/queue events.
- [x] Real-world verification: card reorder does not append terminal entries; Coolify API and SSH communication still appears (2026-11-01, staging deploy).
- [ ] Verify separately that other local settings changes do not append terminal entries.
- [x] Rebuild and pass **real WPF retemplate** smoke after frozen-transform fix (2026-11-01, source `48c87cf6df3d`); earlier syntax and scope errors corrected in `bb8e1e6` / `146d7b3`. Owner confirmed `PASS: real WPF card retemplate during live spinner does not crash or freeze transforms.`
- [x] Build the new development version on Windows and pass all smoke tests; **latest passing 2026-11-01 WPF-fix build**: source `48c87cf6df3d`, .NET 10 win-x64, SHA256 `12F5B9F44EF25E749BA40B8C56D13BE10B7326037EE23A11515AB56045AB5DEF` (supersedes earlier 2.2.2 candidate).
- [x] Confirm per-card spinner animates during real Coolify deployment and stops on actual `finished` server response (2026-11-01, owner-observed).
- [x] **Real card drag during deploy PASS (2026-11-01)**, no crash WPF after correcting frozen `RotateTransform`; WPF re-template smoke also PASS. Saved order persistence across restart still pending.
- [x] Identify SSH port 18000 listener: `ssh.exe` PID 33596 / ParentPID 10628, matching crashed CDM PID `0x2984`. Safe one-off cleanup requires exact process verification; automatic orphan prevention not implemented.
- [x] Confirm port 18000 recovered after targeted orphan cleanup and restart of stable personal CDM 2.2.1; user confirmed SSH/Coolify works (2026-11-01).
- [x] Identify leftover `.CDM-Test-2.2.2.exe.pending` (11:00:42, same timestamp as crash; full staged clone per SettingsStore); no CDM process reported, original test clone remains unchanged. Preserve pending and prepare separate recovered test clone without touching stable personal 2.2.1 or SSH tunnel.
- [x] Recover separate clone from crash-time pending and upgrade to passing 2.2.2 build; owner confirmed PASS and used it for successful real-world staging deployment.
- [x] Reordered cards persist after clean exit and relaunch; owner confirmed **«sì... è ok»** on 2026-11-01.
- [ ] Evaluate behavior after Stop Queue or API disconnect (remote deployment may continue).
- [ ] Explicitly approve separate v2.2.2 release only after acceptance.

**Branch:** `feature/live-deploy-card-status-20261101`. No merge, no tag and no change to released v2.2.1; keep previous stable personal clone. Detailed reasoning and tests: `docs/DIARIO_TECNICO_2026-11-01_DEPLOY_UI.md`.

## Phase 4 — Windows startup to notification area (requested 2026-11-01)

- [x] Code: explicit off-by-default **Start with Windows, minimized to the notification area**; HKCU Run key holds only selected clone EXE path and `--startup-tray`, no credentials. Windows end-to-end test pending.
- [x] Code: official tray icon, restore from double-click/context menu and explicit Exit; ordinary Close still shuts down app/tunnel. Windows visual acceptance pending.
- [x] Code: `--startup-tray` bypasses splash/taskbar, begins regular SSH/inventory in background; manual launch unchanged. Windows sign-in acceptance pending.
- [x] Code: named mutex + restore event scoped by hash of clone EXE path; second launch should reopen same window, not compete for SSH port. Offline smoke and Windows manual acceptance pending.
- [x] IT/FR/EN locale strings for Windows-start checkbox, explanation, menu Open/Exit and minimize tooltip (232/232 each); others use English fallback.
- [x] **Windows build and full offline smoke PASS** (2026-11-01): 2.2.2 source `8585427dff9e`, SHA256 generic `E4F3D9989185694A17C97344C6E014A45CCFE784812586D71C05FB181B41E6E4`, successful `--verify-tray-ui` (hide/restore, named-instance event, autostart command; no registry modifications).
- [x] Upgrade only disposable `CDM-Test-2.2.2-Recovered.exe` using the SHA256-verified Windows tray build; owner confirmed PASS (2026-11-01); original personalized clone preserved.
- [x] Manually test notification icon, minimize to tray and double-click to restore actual WPF window on recovered clone: owner confirmed 👍 (2026-11-01).
- [ ] Verify tray context menu Open/Exit, normal Close, same-clone double-launch and that no competing SSH listener appears on port 18000.
- [x] Enable checkbox on test clone and verify the actual HKCU Run value points to `CDM-Test-2.2.2-Recovered.exe --startup-tray` (owner showed actual registration, 2026-11-01).
- [x] Simulate `--startup-tray` from closed test clone: no splash/window, tray icon shown, and double-click opens connected CDM (owner confirmed 2026-11-01). **Defect found:** restored window stays behind other Windows windows.
- [x] Code patch: restore from tray and second-instance wake now explicitly raises WPF window in foreground using topmost z-order transition, WPF activation and Win32 SetForegroundWindow. **Windows manual confirmation pending**.
- [x] Code patch: persist last normal window rectangle and normal/maximized mode inside personalized EXE profile on clean Close; restore on next launch and after tray. Validate virtual desktop bounds and do not persist transient minimized state. **Windows manual confirmation pending**.
- [x] Offline WPF / JSON / real staged embedded profile smoke authored: `--verify-window-placement` and existing upgraded synthetic clone close-time save. No real network, registry or user settings written by tests.
- [x] **Windows compile and smoke PASS** for foreground/geometry patch: source `1123f2d2d95f` built 2026-11-01T12:15:28Z, SHA256 generic `9A8612A7941A96AA02535A5832441F12436E5DCBA059CE7A1F1685FB7B43E0A1`; new WPF geometry/normal/maximized/JSON/pending-clone checks and prior smoke PASS.
- [ ] Upgrade only recovered test clone from this hash-verified template after clean Exit and no remaining `.pending`; manually verify foreground focus, persisted normal bounds, maximized state and no competing SSH tunnel. Preserve personal 2.2.1.
- [ ] Disable checkbox and verify HKCU Run entry removal, then optionally re-enable; actual sign-in auto-launch still unverified. No public release until accepted.

**Versioning:** continue the unpublished 2.2.2 feature branch, with no release or main changes pending validation.
