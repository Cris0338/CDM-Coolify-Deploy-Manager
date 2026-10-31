# CDM — Coolify Deploy Manager

**Created by [Cris0338](https://github.com/Cris0338)** · © 2026 Cris0338 · [MIT License](LICENSE)

CDM is an **independent, unofficial** Windows 10/11 (x64) desktop tool for managing [Coolify](https://coolify.io/) deployments. It is not affiliated with Coolify.

## Dashboard

- Project and environment discovery through Coolify REST API.
- Staging, production and unclassified application cards, status, branches and recent deploys.
- Sequential staging deployments, confirmations and activity log.
- Integrated connection setup and localized dashboard (English, French and Italian complete for the legacy UI; other languages are previews).

## Your own portable executable (2.2.0 development preview)

The distributed **`CDM.exe` is an unconfigured template**. On every launch, it ignores all older user configuration files and opens a blank guided connection setup; it never connects automatically. After verifying the connection and the token, it creates and launches:

`CDM-<WindowsUser>-<ServerLabel>.exe`

The configured clone contains the server address, one READ+DEPLOY token, selected language, application layout and — for SSH mode — a dedicated private SSH key plus the pinned server host key. It needs **no profile password, user configuration file or prior SSH alias** on the next Windows computer. You can copy just the clone EXE.

If settings or card order changes, CDM stages a new executable and atomically replaces the previous clone after it closes using a short-lived hidden PowerShell helper. The original `CDM.exe` is never modified.

**Important security property:** these are *portable credentials*, not password-protected secrets. Anyone with the personalized EXE can extract the API token and embedded SSH key. Never distribute your personal clone or commit it to a repository. Revoke the token and SSH key if it is exposed. Appending embedded settings invalidates any pre-existing Authenticode signature; this project does not sign personalized clones.

## Guided setup

1. Pick a server label (e.g., `contabo`) and **direct connection** (HTTPS or local LAN/VPN HTTP), or **managed SSH**.
2. **Direct connection:** enter a trusted HTTPS Coolify URL, or a private LAN/VPN address such as `http://192.168.1.50:8000`. No public domain or DNS setup is required. Open the dashboard in your browser.
3. **Managed SSH:** enter VPS host/IP, SSH user and port. Generate a dedicated ED25519 SSH key. Authorize the *public* key using an existing SSH alias if you have one; otherwise add it to the VPS user's `~/.ssh/authorized_keys`. Scan and compare the server's SSH host key fingerprint to a trusted fingerprint.
4. For managed SSH, clicking **Open Coolify in browser** opens the panel through CDM's own localhost tunnel, not over public HTTP.
5. In Coolify **Keys & Tokens → API Tokens**, generate **one token with READ and DEPLOY only**. Do not select Root, Write or Read Sensitive Data.
6. Paste the token into CDM. The setup performs a read-only API request to `/api/v1/version` and checks the connection. It never starts a deployment to test permissions.
7. CDM creates and launches the personal clone, closing the original window.

**No OAuth API grant is assumed.** Coolify's OAuth login providers do not expose a documented native-app authorization-code token flow for CDM. Browser login plus token copy is the supported path.

**Windows OpenSSH Client** (`ssh.exe`, `ssh-keygen.exe`, `ssh-keyscan.exe`) is used for SSH mode. These Windows executables are *not* bundled into CDM. Public HTTPS mode requires no SSH client. The SSH key's first authorization still requires someone with access to the VPS; CDM cannot invent access rights.

## Build locally — no GitHub Actions

Requires **.NET 10 SDK x64** on Windows; Visual Studio is optional.

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

Runs classification, localization and embedded-profile tests locally, then publishes a self-contained `portable\CDM.exe`. This is the only supported build workflow; there are no GitHub Actions in this development branch.

See [BUILDING.md](BUILDING.md), [CDM_SETUP_GUIDE.md](CDM_SETUP_GUIDE.md), [CHANGELOG_2.2.0.md](CHANGELOG_2.2.0.md), and [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md).

## Project status

**Version 2.2.0 is a source-code development preview, not a verified Windows build.** The source has not yet passed Windows compilation or end-to-end single-file clone startup testing. In particular, the .NET single-file host's behavior with appended profile bytes must be verified on a published EXE before release. No public binary release is claimed.

CDM manages one Coolify server per clone. To manage several servers, create separate personal clones.

## Languages

The dashboard includes 14 locale catalogs. The new 2.2.0 guided setup still needs complete localization and native-speaker review. See [LOCALIZATION.md](LOCALIZATION.md).

### Domainless and offline-friendly installations

CDM can administer a Coolify instance on a private LAN, e.g. `http://192.168.1.50:8000`, without a domain, public DNS, Let's Encrypt or public Internet exposure. A Docker application managed by Coolify can use Docker-internal networking or explicit host port mappings rather than a public domain. For HTTP over a public server IP, choose the managed SSH tunnel rather than transmitting the bearer token in cleartext. Private VPN address ranges are also accepted for direct HTTP access, when the VPN itself provides a secure network path.

## Desktop UI improvements (feature branch, 31 October 2026)

- Searchable project dropdown (replaces redundant project tabs). Search applications separately.
- Staging cards have selection checkboxes. **Deploy selected staging** uses only checked applications; **Deploy ALL staging** uses all non-blocked staging applications in the current project filter. Both require explicit confirmation before any API deployment. Neither runs automatically.
- Right-click a card to lock its position and/or block deployment. Both flags are saved inside the personalized EXE; locked deployments are omitted from both batch actions and explicitly skipped if already queued.
- Drag/drop features an animated card transition and an additional blank drop target in each section for moving cards to the end. Free persistent empty cells and positional grid occupancy are **not** yet implemented.
- Wizard exposes a copyable manual `authorized_keys` command when the user has no preexisting SSH alias.
- The wizard's title-bar close button uses vector geometry. The dashboard shows the configured SSH username and host under the title.
- New Windows icon (neon rocket in multiple sizes) and animated splash. A 3-second splash is displayed on normal app startup, but never during local smoke-test commands.
- The original generic `CDM.exe` stays credential-free; existing personalized clones are not automatically upgraded by compiling a new generic template. Preserve your working clone until its update path is tested.

No GitHub Actions. Run `BUILD_PORTABLE.ps1` locally; don't publish a release or run a real staging deployment until the Windows UI and safety checks pass.

### Official 3-second CDM opening animation

The startup shows the **approved transparent CDM logo**, while a luminous electron makes a depth-aware orbit behind/over the letters. At ~2.25 s the particle collision radiates outward; the splash fades at ~2.64 s and reveals the real dashboard by ~3 s. No network request or deploy is triggered by the animation. Local self-test/clone-upgrade commands bypass the splash.

The two approved binary assets are delivered in **`CDM_Logo_Assets.zip`** (the exact logo PNG and an ICO derived from it). Download this archive to your Windows **Downloads** folder before building. `BUILD_PORTABLE.ps1` verifies the SHA256 of the ZIP and each extracted image, installs them under `src/CoolifyDeployManager/Assets`, and embeds them in `CDM.exe`. The logo and key art thus travel inside the one-file personalized clone; the archive is only needed during building. Builds without the ZIP use the existing vector logo fallback. The binary files are Git-ignored so that source-only branches remain small; build them into the clone on the PC with the ZIP.

The launch sequence does not change any SSH keys, tokens, Coolify services or deployment settings.
