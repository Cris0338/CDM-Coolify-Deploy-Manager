# CDM — UI bug handoff, 2026-10-31

This file preserves the screenshot findings and the next implementation goals. **No UI fixes in this handoff commit.** Do not mistake a screenshot of an older personalized binary for the current source.

## Project and branches

- Independent personal project: **CDM — Coolify Deploy Manager**, by **Cris0338**; **not** a Chataide.fr application.
- Repository: `Cris0338/CDM-Coolify-Deploy-Manager`.
- Main development branch: `feature/embedded-clone-20261031`.
- Public default `main` was previously fast-forwarded to checkpoint `c9514ec55c96b35734cf23670d0ea5c4dee56f48` (source and no-sale license). The donation/localization updates are **newer on the feature branch**, not yet synchronized with main.
- At handoff, no GitHub Release exists, and no tag or binary asset has been published (checked via GitHub).
- `CDM.exe` is the blank, generic, self-contained Windows 10/11 x64 executable; `CDM-<user>-<server>.exe` is the personalized executable embedding credentials and UI state. **Never commit or publish the personalized EXE or its backups.**
- Active license: `CDM Free Redistribution License v1.0`, free use/share, **no resale**. It is source-available, not OSI-approved. Historical MIT references in previous releases remain historical.
- Donations: the confirmed user PayPal.Me page is **https://www.paypal.com/paypalme/Cris0338/**, no preselected amount. `.github/FUNDING.yml` points to it.
- Working directory on the owner's Windows PC: `C:\Users\Cris0338\Downloads\CDM-Coolify-Deploy-Manager`.
- .NET 10 WPF; local `BUILD_PORTABLE.ps1`, `PACKAGE_RELEASE.ps1`; **no GitHub Actions**.

## Four screenshots supplied by the user on 31 October 2026

The images were attached to the conversation, NOT committed to GitHub. Their appearance is described precisely below so future chats need not guess.

### 1) Context menu — `image(20261031-161912).png`

Observed: right-click menu contains `Blocca posizione` and `Blocca deploy`; the left checkmark/gutter area appears as a **large white rectangle**, visually mismatched against the dark menu. Neither label is the problem. The current source uses two `MenuItem IsCheckable="True"` inside a `ContextMenu` in `src/CoolifyDeployManager/MainWindow.xaml`. **Action:** implement proper dark WPF ContextMenu/MenuItem styling, including selected/unselected checkmarks, hover/focus, spacing and contrast, rather than inheriting bright system checkmark gutter. Ensure lock/unlock behavior still works and never silently triggers deployment.

### 2) Header/status layout — `image(20261031-161916).png`

Observed: topbar has blurry, low-resolution logo; `Coolify Deploy Manager`, version badge `WPF 2.2.0`, green badge `Portable · Configurato`, then a subtitle `Distribuzione applicazioni · Dashboard Windows`; green `Utente: … · VPS: …` is rendered **under the subtitle near the left**, not under the green configured badge. **Desired:** align username/VPS below the configured status badge in the corresponding header column without overlapping when the window is resized or text is translated. Avoid hardcoding host-specific user/VPS info in documentation or shared source.

Current header source: `src/CoolifyDeployManager/MainWindow.xaml`, `ProfileBadge` and `ProfileIdentity` elements; current `ProfileIdentity` uses a left margin 47px in the title StackPanel. Reconsider the layout grid/columns instead of nudging margins.

### 3) Blurry logo crop — `image(20261031-162141).png`

Observed: close-up of the topbar icon visibly blurred. Current `MainWindow.xaml` displays `Assets/CDMOfficial.ico` at 31×31 and `AboutWindow.xaml` at 53×53. The **approved** graphic assets are `Assets/CDMLogo.png` (high-resolution original) and `Assets/CDMOfficial.ico` (multi-size Windows executable/window icon), embedded at build by SHA256-checked `CDM_Logo_Assets.zip`. Consider showing the original PNG, or a suitable locally generated crisp preview from the approved image, inside WPF while retaining ICO for executable/window resource. Test DPI and transparent edges; DO NOT replace with an unrelated icon or introduce new visual branding. The build requires the approved ZIP locally under Windows Downloads.

### 4) About window — `image(20261031-162219).png`

Observed: in the **photographed older executable**, title `Informazioni — Coolify Deploy Manager`; logo blurry; subtitle `WPF · Versione 2.1.1` (outdated); a license attribution string is clipped at the edge; upper-right red close button looks like `›` (a chevron) instead of a proper X. There is **no donation panel in that photo**.

**Important version chronology:** in *current source on feature branch*, `Localization/it.json` already has `AboutVersion: "WPF · Versione 2.2.0"`, and `AboutWindow.xaml` already includes a new PayPal.Me support section. Those changes came AFTER the screenshot. Do not claim they are broken until the new generic build and personalized clone are updated and tested. **Action:** verify the current built version before modifying version strings; fix close control with a reliable vector `Path` X (consistent with existing settings window), wrap/scroll or abbreviate license text to avoid truncation, improve icon sharpness and test full About layout including the new donation panel.

## Additional known/pending user requests

- Card dragging/reordering still needs real WPF acceptance testing. User previously reported drag/drop did not work in the personalized UI. Current code supports section-local reorder and an empty "drop to end" region; persistent arbitrary empty slots are **not implemented**. Check before marking solved.
- Cross-modal dark theme controls, right-click menus and red close buttons should remain coherent and visible.
- Personalized credentials, position locks and deployment blocks must survive closing/relaunching after changes.
- Support message should be multilingual across all **14 UI languages** (en/fr/it + es/de/pt-BR/nl/pl/tr/ru/uk/ja/ko/zh-CN). Code and resource strings exist on feature branch, not yet verified in Windows build.
- Do not automatically deploy any Coolify app. First real staging deployment and cross-machine portability have **not** been accepted.
- Further improvements are deferred by the user; do not invent features or tag a public release as fully tested.

## Current source and packaging status

- Known checkpoint package prior to donation changes: `release-artifacts/CDM.exe`, `CDM-2.2.0-win-x64.zip`, `CDM-2.2.0-SHA256.txt`. Previous reported generic EXE SHA256 `9B1115C08BDCAAF19500ACDE2FAA9E357F91B03CC9A13AEB7F1F1791721AA8A4` is **obsolete** as soon as new source is built.
- `PACKAGE_RELEASE.ps1` has been changed on feature branch to include `SUPPORT.md` in the ZIP. Re-run after UI improvements and successful testing; keep version/tag/build alignment.
- `README.md`, `README_IT.md`, `SUPPORT.md`, `RELEASE_NOTES_2.2.0.md`, `RELEASE_GUIDE.md`, `CHECKLIST_TEST_WPF.md` are current planning docs on feature branch.
- First priority: correct WPF UI, run `git pull --ff-only`, then local Windows `BUILD_PORTABLE.ps1`, then explicit clone upgrade (after closing clone), then screenshots/validation. Never run GitHub Actions.
- Keep working on feature branch. Synchronize `main` and create a pre-release/tag **only after UI/tests and user approval**. No release currently exists.

## Recommended order for next chat

1. Inspect the committed files and current branch HEAD. Compare screenshot chronology to current code.
2. Fix dark ContextMenu + MenuItem/checkmarks in one coherent style.
3. Fix header alignment using a layout Grid; make the logo sharp without changing approved branding.
4. Fix About modal X button, text wrapping, consistent current version and donation section layout. Review other modal close buttons.
5. Test card drag/drop, current UI localization, and persistence without triggering deployment.
6. Ask the user for **one PowerShell command at a time**, using connected GitHub source edits, direct command, commit hash and local build output. Update technical docs and checklist based on actual tests only.
7. Rebuild and repackage only the blank generic EXE; no personal profile, no keys, no tokens, no GitHub Actions.
