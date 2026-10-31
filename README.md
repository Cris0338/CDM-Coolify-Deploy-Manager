# CDM — Coolify Deploy Manager

**Created by [Cris0338](https://github.com/Cris0338)** · © 2026 Cris0338 · [MIT License](LICENSE)

CDM is an **independent, unofficial** Windows 10/11 (x64) desktop tool for managing [Coolify](https://coolify.io/) deployments. It is not affiliated with Coolify.

## Dashboard

- Project and environment discovery through Coolify REST API.
- Staging, production and unclassified application cards, status, branches and recent deploys.
- Sequential staging deployments, confirmations and activity log.
- Integrated connection setup and localized dashboard (English, French and Italian complete for the legacy UI; other languages are previews).

## Your own portable executable (2.2.0 development preview)

The distributed **`CDM.exe` is an unconfigured template**. On its first launch, it opens the guided connection setup. After verifying the connection and the token, it creates and launches:

`CDM-<WindowsUser>-<ServerLabel>.exe`

The configured clone contains the server address, one READ+DEPLOY token, selected language, application layout and — for SSH mode — a dedicated private SSH key plus the pinned server host key. It needs **no profile password, user configuration file or prior SSH alias** on the next Windows computer. You can copy just the clone EXE.

If settings or card order changes, CDM stages a new executable and atomically replaces the previous clone after it closes using a short-lived hidden PowerShell helper. The original `CDM.exe` is never modified.

**Important security property:** these are *portable credentials*, not password-protected secrets. Anyone with the personalized EXE can extract the API token and embedded SSH key. Never distribute your personal clone or commit it to a repository. Revoke the token and SSH key if it is exposed. Appending embedded settings invalidates any pre-existing Authenticode signature; this project does not sign personalized clones.

## Guided setup

1. Pick a server label (e.g., `contabo`) and **HTTPS** or **managed SSH**.
2. **HTTPS:** enter the trusted Coolify URL and open the dashboard in your browser.
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
