# CDM 2.2.0 — Embedded single-file profiles (development branch)

## What changed

- Replaced historical `settings.json` / `CDM.profile` persistence with per-user-server **single-file EXE embedded settings**; the generic template remains blank on every launch and never imports legacy settings.
- Added a verified creation flow for `CDM-<WindowsUser>-<ServerLabel>.exe`. A new clone connects without an external configuration file or an existing Windows SSH alias.
- Uses one Coolify API token with READ+DEPLOY rights; the setup API test is read-only.
- Supports trusted HTTPS, private LAN/VPN HTTP and an **owned SSH tunnel** for remote HTTP-only Coolify.
- Creates a dedicated ED25519 identity; offers explicit alias yes/no authorization, a copyable VPS command and SSH server fingerprint confirmation.
- Uses isolated Windows `ssh.exe` for host-key scanning, explicit OpenSSH error messages, and strict temporary key-file ACLs.
- Includes a searchable project picker, staging selection checkboxes, separate selected/all batch buttons, persistent lock position/deploy flags, and animated within-section card reordering.
- Added official CDM logo, icon, transparent three-second splash and electron/particle animation.
- Supports an **explicit clone upgrade** from a newer generic EXE, with timestamped backup, verification and bounded Windows sharing-lock retries.
- Keeps localization catalogs and tests embedded; local Windows build script checks approved artwork hashes.
- Removed GitHub Actions workflows. Testing and publishing are manual Windows PowerShell operations only.
- Switched new branch versions to **CDM Free Redistribution License v1.0**: free use and sharing, no sale; earlier MIT grants are not retroactively revoked.

## Windows verification on 31 October 2026

Confirmed: source build, full en/fr/it key catalogs, generic blank behavior, personalized clone connection, SSH forwarding, Coolify API inventory, and offline disposable-clone upgrade smoke test. Approved logo/icon and animation rendered.

Remaining: a manually initiated staging deployment, acceptance of all newly added UI controls, cross-device portability and final release review.

**This is a development snapshot, not a published public binary release.**

Historical changes under versions 2.0–2.1 remain documented separately and are not the current instructions.
