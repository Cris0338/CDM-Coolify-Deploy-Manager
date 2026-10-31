# 2026-10-31 — CDM 2.2 development checkpoint

Repository: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager  
Working branch: **feature/embedded-clone-20261031**  
Version: **2.2.0**, unreleased. **No GitHub Actions. No automatic deploy.**

## Verified by owner on Windows

- Local .NET 10 WPF win-x64 publish of the self-contained, single-file generic EXE (~64.52 MB).
- Classification, localization, executable-profile, generic-template startup, and disposable personalized-clone upgrade smoke tests all PASS.
- Actual personal clone reads the embedded profile and connects to Coolify using its own SSH tunnel and one READ+DEPLOY token.
- Coolify inventory displayed 30 applications.
- Official animated CDM splash and embedded multiresolution EXE icon tested; Explorer icon cache required refresh.
- The most recent build after new docs/license changes passed; the GUI changes and staging deployment are not yet fully acceptance-tested.

## Current design and documentation

- Generic CDM.exe is ALWAYS unconfigured. Real personal clones store settings, card state, token, and SSH identity **inside their own EXE**.
- No persistent LocalAppData/settings.json/CDM.profile. Historical storage references in previous changelogs are not current instructions.
- Managed SSH setup now asks first whether the PC has an existing alias; only the chosen authorization path is shown.
- Searchable project picker, staging selection boxes, separate batch-deploy actions, card locking and drag-and-drop.
- Splash without rectangle; logo and icon use approved assets from Windows Downloads.
- New **CDM Free Redistribution License v1.0** on feature branch: free use and sharing, no resale, voluntary donations. Main branch remains MIT pending acceptance.
- Updated documentation in README.md, README_IT.md, BUILDING.md, CDM_SETUP_GUIDE.md, DEVELOPER_NOTES.md and checklist.

## Pending before public release

- Review UI controls and transparent splash on the final personal clone.
- Verify manual no-alias SSH flow and personal clone update settings/locks.
- Test portable clone on a different machine/account.
- Approve ONE real staging deployment and verify its results.
- Merge tested feature branch to main to align default source license before tagging.
- [DONE] Prepare generic downloadable EXE/ZIP/checksum using local PACKAGE_RELEASE.ps1 (owner confirmed local packaging output; ZIP content inspection pending).
- Publish a manual GitHub Release only after owner explicitly approves it.
- Improvements the owner has mentioned but not specified yet: defer to a future session.

No real deployment, merge, tag, release or GitHub Action was performed when saving this checkpoint.

## Local release-package verification — 2026-10-31

Owner ran `PACKAGE_RELEASE.ps1` on Windows successfully. The script reported:

| Output in local `release-artifacts/` | Size (bytes) |
| --- | ---: |
| `CDM.exe` | 67,658,493 |
| `CDM-2.2.0-win-x64.zip` | 61,795,842 |
| `CDM-2.2.0-SHA256.txt` | 164 |

SHA256 of source generic `portable/CDM.exe`: `9B1115C08BDCAAF19500ACDE2FAA9E357F91B03CC9A13AEB7F1F1791721AA8A4`.

The script finished with `Ready for MANUAL GitHub Release upload (NOT published)`. This confirms local asset generation, **not** an actual GitHub Release. No binaries, credentials or personal clones were added to the repository. The ZIP contents have not been independently inspected. Remaining acceptance testing and the main-branch license alignment are still pending.
