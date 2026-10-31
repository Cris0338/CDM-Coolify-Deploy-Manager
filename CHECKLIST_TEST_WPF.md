# CDM 2.2 — Windows release acceptance checklist

Checklist for **generic template + portable personalized clone**. Tests are local and must not silently trigger deployment. Legend: `[x]` observed in the October 2026 Windows tests; `[ ]` still needs explicit acceptance.

## Build, assets, and profile

- [x] Windows .NET 10 x64 portable build with classification, localization and embedded-profile tests.
- [x] SHA256-verified logo PNG + multi-resolution official ICO incorporated; Windows Explorer fresh-name icon test.
- [x] Clean `CDM.exe` never auto-connects and loads no older settings or tokens.
- [x] Personal single-file EXE generated and reopened with its embedded profile, without separate config file.
- [x] Disposable personalized-clone upgrade keeps its embedded profile and creates a backup.
- [ ] Real personalized-clone upgrade after latest changes, including GUI, token, SSH and layout persistence.
- [ ] Full cross-PC/cross-user independent clone portability test.
- [ ] Verify all updated windows and transparent splash with no rectangular background.

## SSH and API

- [x] Generated dedicated ED25519 key and authorized it through existing SSH alias.
- [x] Independently compared the VPS server key fingerprint.
- [x] Resolved Windows OpenSSH key ACL error and connected through owned SSH tunnel.
- [x] Read Coolify API inventory (30 applications) from the connected personal clone.
- [ ] No-alias VPS command copy path tested on a fresh setup.
- [ ] SSH local port already occupied produces clear diagnostics.
- [ ] Invalid TLS certificates and public HTTP refusal produce legible guidance.

## Dashboard and deployment

- [ ] Searchable project menu (without duplicate tab strip) and application filters reviewed in WPF.
- [ ] Per-card selection checkboxes; selected-only staging action and separate deploy-all action verified.
- [ ] Locked position persists; locked deploy cannot be inadvertently included in a batch.
- [ ] Drag/drop in each section, including empty append drop target and animation, reviewed.
- [ ] Custom title bars and setup close button work and display correctly.
- [ ] All 14 UI locales render; en/fr/it fully localized, other locales fall back to English.
- [ ] Deliberately deploy **one staging application**; verify command, monitoring result and independent app health.
- [ ] Production requires explicit per-card confirmation and is never automatically included in staging batches.
- [ ] Cancelling CDM's local queue does not falsely claim to stop the server deployment.

## Release and distribution

- [ ] LICENSE and app About screen show the current CDM Free Redistribution License, not MIT.
- [ ] No personal clone, API token, private key or credentials-bearing `.bak` in release assets or Git commits.
- [ ] Only the **generic** credential-free EXE is publicly distributed.
- [ ] Source and documentation agree with the embedded-EXE model; no obsolete persistent settings workflow.
- [ ] Manual Windows review completed before publishing; **no GitHub Actions**.

## Manual release packaging

- [x] Run `PACKAGE_RELEASE.ps1` locally and verify generic EXE/ZIP/checksum are produced (31 Oct 2026, owner-reported successful output).
- [ ] Confirm ZIP contains the current license and Italian/English documentation, without any personal EXE or .bak.
- [ ] Check binary/source version, license, and release tag all refer to the same verified commit on main.
- [ ] Create GitHub Release as draft, inspect assets, then publish only after explicit acceptance.

## Voluntary support localization

- [ ] All 14 languages display the correct optional support title, explanation and PayPal.Me button in About.
- [ ] PayPal.Me button opens `https://www.paypal.com/paypalme/Cris0338/` without a fixed amount.
- [ ] Repackage after the localized support changes; ensure `SUPPORT.md` is included in the distribution ZIP.

## UI defects reported in screenshots (31 Oct 2026; fix before release)

- [ ] Custom dark `ContextMenu` / `MenuItem`: remove white checkbox/gutter rectangle; verify checked/un-checked states and actions.
- [ ] Make in-app header/About CDM logos crisp at common Windows scaling (retain the approved high-res brand art).
- [ ] Move `ProfileIdentity` under the green `Portable · Configurato` badge, not below the dashboard subtitle; verify resizing.
- [ ] About + other modals: vector X close control, no overflowing/clipped license, appropriate layouts.
- [ ] Verify built About displays 2.2.0 and translated PayPal.Me section: screenshot was from an older binary.
- [ ] Manually validate drag-and-drop cards and persistence; currently not accepted.

See [docs/HANDOFF_2026-10-31_UI.md](docs/HANDOFF_2026-10-31_UI.md) for screenshot-by-screenshot details.

### Source fix checkpoint (31 October 2026; NOT accepted yet)

Patches for the dark context menu, status/identity alignment, high-resolution PNG logo, About/Guide red vector X buttons, wrapped About license/support content, and card body/grip drag/drop have been committed to the development branch. See [docs/UI_FIXES_2026-10-31.md](docs/UI_FIXES_2026-10-31.md). The unchecked items above **must remain unchecked** pending the owner's Windows build, real GUI screenshots, and drag/lock persistence trials. No staging or production deployment was triggered during these source changes.

### Second screenshot follow-up — 31 October 2026

- [x] The owner visually verified the red vector X in the first revised EXE.
- [x] Owner's first-patch Windows build and offline generic/clone smoke tests passed; generic SHA256 `87888E0E21A918021FD006F478A4AFE9FC318AB884C3C2397ADAE1CBD29BE2DA`.
- [ ] Re-test *visible* dark checkboxes (unchecked and checked) in the second-patch context menu, including position/deploy lock persistence.
- [ ] Re-test matching green status/identity badge styles, alignment and clipping at narrow window widths.
- [ ] Re-test header/About PNG at increased sizes, with no extra purple tile.
- [ ] Re-test initial About window height 610 px and localized support/overflow.
- [ ] Re-test card drag from handle and body, onto another unlocked card and section append target; save/reopen order, locked positions, and no accidental deploy.
- [ ] Compile new second-patch source, upgrade personal clone explicitly and check UI before packaging or tagging.

Second-patch source commits and root-cause notes are in [docs/UI_FIXES_2026-10-31.md](docs/UI_FIXES_2026-10-31.md). Keep all unverified items open.

### Third-iteration acceptance: clean titlebar/card drag rewrite (2026-10-31)

- [x] Second-iteration Windows build and disposable clone smoke PASS (SHA256 `356D7724B006CDD0AC20E13B444A162176CF21D85AF7603759F6897319161564`).
- [x] Owner visually accepted About size and logo, unframed Main logo, and green status badges in second-iteration real clone.
- [ ] Build third-iteration source on Windows and explicitly upgrade clone, preserving the latest embedded profile and a timestamped backup.
- [ ] Verify larger ~88px Main logo and plain WPF version aligned **above** both green status lines, without overlapping the window controls.
- [ ] Drag a card and see translucent screenshot following mouse; blue marker on left/right target half.
- [ ] Drop A **before** B; drop A **after the last card**; append via explicit end rectangle; append via blank wrap-panel area.
- [ ] Verify lock restrictions, abort/cancel does not reorder, no dragging from checkbox/deploy button, order persists after close/reopen.
- [ ] Verify production and unknown sections are isolated from staging; no real deployments triggered by drag.

All source changes are committed but GUI functionality remains unaccepted until actual Windows testing.

### Fourth-iteration owner acceptance — window chrome and terminal (2026-10-31)

- [x] Third-iteration real Windows clone: owner confirmed card drag-and-drop works and About/status/logo accepted.
- [ ] Fourth revision Windows compilation and executable smoke tests.
- [ ] Card has no grip; reordering from body remains functional; right half of last card permits append; blank WrapPanel area can append.
- [ ] No dedicated `DropToEnd`/drag-here bar visible in any section.
- [ ] Drag the window from all non-button titlebar areas; cursor does not show a crosshair; buttons still click without dragging.
- [ ] Maximize/restore icon is legible at normal and maximized size and changes glyph appropriately; double-click on titlebar toggles maximization.
- [ ] Maximize respects taskbar work area; no hidden last lines of terminal or off-screen content (also test secondary monitor if available).
- [ ] Dashboard and terminal scrollbars align with a 16px inset and are not clipped by window edges.
- [ ] Splitter at top of status strip can resize log up/down, without breaking dashboard min height; hide/show restores last selected log height.
- [ ] After latest real clone upgrade, inspect whether the settings `.pending` is consumed normally on close; preserve all personal backups.

### Fifth regression: live health and pending save (2026-10-31)

- [x] Owner reported a real deploy job finished, but CDM stayed red UNHEALTHY until manual Refresh (Coolify already green).
- [x] Owner checked no CDM processes active while newer personal `.pending` remained unapplied.
- [ ] Windows build smoke validates health view-model's unhealthy→healthy transition, keeping deployment history unchanged.
- [ ] Disposable clone's actual close-time pending replacement completes and preserved token/card order/locks are verified by the build script.
- [ ] Verify existing real personal `.pending` integrity and recover newer profile with an independent backup **before** upgrading personal EXE.
- [ ] After another confirmed deployment, verify automatic health transition in the UI without pressing Refresh, including timeout when Coolify is not healthy.
- [ ] Close/restart updated personalized EXE after changing card order; pending should be consumed and latest order retained; inspect `.pending.updater-error.txt` if a save fails.

No real server action is part of the pending or health smoke tests; manual real deployment only with owner's explicit confirmation.
