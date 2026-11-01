# CDM — Roadmap and acceptance tracker

Updated 2026-10-31. Author: Cris0338. Project independent of Chataide.fr.

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
- [ ] Confirm manually that card reorder and local settings changes do not append terminal lines while remote deploy messages still appear.
- [ ] Rebuild and run new **real WPF retemplate** smoke after frozen-transform fix. Two compilation blockers found and corrected: malformed `SetLiveDeploymentSpinner` signature (83 cascading errors, `bb8e1e6`) and CLI test variable collision `CS0136` (`146d7b3`). Both fixes await a fresh Windows build and actual WPF smoke PASS.
- [x] Build the new development version on Windows and pass all smoke tests (2026-11-01, .NET 10 WPF win-x64, executable SHA256 `E363C98E6927B07C09E43F0E629EA58C3438349255FC9B88553DC4ADF905123C`).
- [x] Confirm spinner becomes visible during a real Coolify deploy (2026-11-01, owner-observed). Spinner termination at server terminal status still unverified.
- [ ] **WPF crash diagnosed and candidate fixed; Windows retest PENDING:** the 2026-11-01 crash was `XamlParseException` from the card spinner's frozen shared storyboard `RotateTransform`. Per-card runtime transform + actual re-template smoke added. Run Windows build and real drag test before clearing this item; order persistence still unvalidated.
- [x] Identify SSH port 18000 listener: `ssh.exe` PID 33596 / ParentPID 10628, matching crashed CDM PID `0x2984`. Safe one-off cleanup requires exact process verification; automatic orphan prevention not implemented.
- [x] Confirm port 18000 recovered after targeted orphan cleanup and restart of stable personal CDM 2.2.1; user confirmed SSH/Coolify works (2026-11-01).
- [ ] Evaluate behavior after Stop Queue or API disconnect (remote deployment may continue).
- [ ] Explicitly approve separate v2.2.2 release only after acceptance.

**Branch:** `feature/live-deploy-card-status-20261101`. No merge, no tag and no change to released v2.2.1; keep previous stable personal clone. Detailed reasoning and tests: `docs/DIARIO_TECNICO_2026-11-01_DEPLOY_UI.md`.
