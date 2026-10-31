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
