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
- [ ] Publish **v2.2.1 as GitHub pre-release**, with generic EXE, ZIP and SHA256; verify exact tag and attached assets after publication.

**Tested binary source revision:** `56ec905c0b6894898a78fc9a92eba93b7925593a`. No personal clone, key or API token belongs in GitHub. Development changes after this revision are documentation-only.

## Phase 2 — Future field validation, no automatic changes

- [ ] Perform a deliberately confirmed real staging deploy in normal use; record outcome.
- [ ] Exercise actual SSH reconnect on spontaneous disconnect, if it recurs; inspect exit code/stderr.
- [ ] Verify portability on a second Windows computer/account.
- [ ] Review remaining static-audit findings before a future fully stable release: remote HTTP hostname classification, SSH configuration change detection, optional clearing of unused private credentials, remaining UI/state consistency concerns.
- [ ] Version increment and release only for an actual new software change; avoid modifying frozen 2.2.1 code speculatively.

**Maintenance rule:** log each engineering step and test with branch, commits, files, result and next step in the technical diary. No GitHub Actions, automated deployments, or uploading credential-bearing personal EXEs.
