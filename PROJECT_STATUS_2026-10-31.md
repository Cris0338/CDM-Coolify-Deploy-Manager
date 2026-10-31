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
- New **CDM Free Redistribution License v1.0** on feature branch: free use and sharing, no resale, voluntary donations. Main was synchronized with the no-resale license on 31 October 2026 (before later support changes).
- Updated documentation in README.md, README_IT.md, BUILDING.md, CDM_SETUP_GUIDE.md, DEVELOPER_NOTES.md and checklist.

## Pending before public release

- Review UI controls and transparent splash on the final personal clone.
- Verify manual no-alias SSH flow and personal clone update settings/locks.
- Test portable clone on a different machine/account.
- Approve ONE real staging deployment and verify its results.
- Main already carries the no-resale license; sync the final tested support/localization changes to main before tagging.
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

The script finished with `Ready for MANUAL GitHub Release upload (NOT published)`. This confirms local asset generation, **not** an actual GitHub Release. No binaries, credentials or personal clones were added to the repository. The ZIP contents have not been independently inspected. Remaining UI acceptance testing and syncing the final release candidate to main are still pending.

## Optional donations and multilingual support — 31 October 2026

The author confirmed https://www.paypal.com/paypalme/Cris0338/ as the voluntary support link, with no amount prefilled. CDM now has a clickable support panel in About, including translated title, explanatory description and button for **all 14 UI languages**. The GitHub Sponsor button is configured in `.github/FUNDING.yml`; README.md and README_IT.md mention optional support. `SUPPORT.md` offers all 14 language versions and the local release ZIP will include it. This source update is not yet compiled/tested or published in a binary release; the prior prepared ZIP remains outdated and must be rebuilt.

## Next-session UI handoff — 31 October 2026

The owner supplied four cropped WPF screenshots documenting an incorrectly white right-click context-menu check gutter, blurry logo in the header and About, misaligned username/VPS line, and an older About modal with chevron-like red close button, clipped license and stale version text. These screenshots show an older binary, so they do not establish whether the subsequently committed PayPal.Me panel and `2.2.0` version are broken. **All technical findings and recommended next steps are recorded in [docs/HANDOFF_2026-10-31_UI.md](docs/HANDOFF_2026-10-31_UI.md).** A ready-to-copy new-chat instruction is saved in [docs/NEXT_CHAT_PROMPT_2026-10-31.md](docs/NEXT_CHAT_PROMPT_2026-10-31.md). No UI changes or releases are performed by this handoff.

## UI implementation after the handoff — 31 October 2026

The five reported UI areas now have **committed source fixes** on the feature branch: card right-click menu dark check gutter (`5af5f67`), status/identity placement and Main logo (`c1aa48e`), card drag/drop targets (`1542ee0`), About logo/layout/vector X (`d832fd3`), and Guide vector X (`01e59ae`). Full technical detail: [docs/UI_FIXES_2026-10-31.md](docs/UI_FIXES_2026-10-31.md).

**Validation scope:** static tag-balance and handler-reference checks only; the local Windows executable has **not** been rebuilt or GUI-tested after these changes. Every pending UI acceptance checkbox remains open. Do not use the obsolete release package or synchronize/tag/publish until Windows build, manual UI testing and the owner's approval.

## Follow-up after live Windows UI screenshots — 31 October 2026

Windows build and offline clone tests passed on the **first** UI patch (generic EXE SHA256 `87888E0E21A918021FD006F478A4AFE9FC318AB884C3C2397ADAE1CBD29BE2DA`); the owner upgraded the actual personal clone and **confirmed vector close X works**. New screenshots revealed the unchecked context-menu marks still invisible, status texts visually mismatched, header/About logos with purple tiles, About initial height slightly short, and the drag/drop still not operating. The 2.2.0 version appears correctly in About.

New fixes are now committed only on `feature/embedded-clone-20261031`: menu squares/status badges/header logo (`5804b24`), correct bubbling/preview drop routing (`5ceb84e`), capture-based card drag (`22e4a4c`, `7f65958`), About larger/transparent logo (`39738a8`). Full details [docs/UI_FIXES_2026-10-31.md](docs/UI_FIXES_2026-10-31.md). **No second build or visual acceptance yet**. Local package and checksum must be regenerated after the new build. No automatic deployment/release.

## Current topbar + drag/drop redesign — 31 October 2026

The owner accepted the second iteration's About window, disappearance of the Main/Info purple logo backgrounds, and the status badge styling. The drag mechanism was still unintuitive and could not insert after the final card. User's explicit engineering rule: **do not stack patch upon patch; replace the relevant implementation cleanly**.

Third-iteration source rewrites on the feature branch: complete titlebar and card XAML templates (`4d63083`), consolidated C# drag plan/drop/save (`180f97f`), fully hit-testable transparent section/end drop zones (`387e44f`), cursor-following WPF adorner preview (`b51405c`, `95b84e7`). Main header PNG is now 88px; WPF version is plain purple text above green badge/identity. Cards drop before/after target midpoint or append in blank/end zone. No automatic deployment, merge or release. **Windows build and owner acceptance of this revision are still pending.** The perpetual maintenance rule is now saved in `DEVELOPER_NOTES.md`.

## Third-iteration build feedback — 31 October 2026

Windows .NET 10 compilation and classifier, localization and embedded-profile tests passed. The build stopped before executable smoke testing when Windows denied removing duplicate `portable/ CoolifyDeployManager.exe`. The owner verified that `portable/CDM.exe` and `portable/CoolifyDeployManager.exe` were same size (67,669,245 bytes) and exactly matched SHA256 `2D58DCCD86082702AFCCFA6056BDA5DA4423B1BC6CF03981A12DDC87F1559B41`. The verified main generic EXE was therefore copied correctly, but third-iteration executable smoke tests **did not yet run**.

`BUILD_PORTABLE.ps1` publication/cleanup logical unit was rewritten in commit `c0d00b7`: copying and SHA256 equality remain mandatory, while failed deletion of the redundant SDK-named binary becomes a warning and smoke tests continue. **Run script again from Windows after pulling**; no personal clone/release/deployment is touched automatically.
