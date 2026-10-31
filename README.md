# CDM — Coolify Deploy Manager

**[Guida rapida in italiano / Guida italiana](README_IT.md)**

**Created by [Cris0338](https://github.com/Cris0338)** · © 2026 Cris0338 · [CDM Free Redistribution License v1.0](LICENSE)  
**Free to use, including at work · Free to share · Not for resale · Voluntary support welcome**

CDM is an independent, unofficial **Windows 10/11 x64** application for managing [Coolify](https://coolify.io/) through its API. It is not affiliated with or endorsed by Coolify.

## What CDM does

- Discovers projects, applications, branches, deployment histories, and health states.
- Separates staging, production, and unclassified applications without deploying anything automatically.
- Filters projects with a searchable dropdown; filters applications by name/status.
- Provides per-card staging deploy, **Deploy selected staging** using checkboxes, and **Deploy ALL staging** in the current project. Each operation needs explicit confirmation; blocked deployments are skipped.
- Provides a card context menu for persistent **Lock position** and **Block deployment** options.
- Reorders cards within their own section with animated transitions; the blank drop target sends a card to the end of that section. **Arbitrary persistent empty grid cells are not implemented yet.**
- Offers English, French and Italian complete translations, plus 11 languages with English fallback.
- Opens with the official CDM logo, animated electron orbit, particle burst and transparent splash (approximately three seconds).

## Two different executables — the key concept

| File | What it contains | Startup |
| --- | --- | --- |
| `CDM.exe` | Generic application, **no server configuration or credentials** | Always opens a blank setup wizard |
| `CDM-<WindowsUser>-<ServerLabel>.exe` | Personalized clone with server, READ+DEPLOY token, language, SSH key/host fingerprint if SSH is enabled, and layout settings | Connects with its embedded profile automatically |

**There is no persistent `settings.json`, `CDM.profile`, password vault, or DPAPI configuration.** CDM does **not** read or migrate settings from earlier releases or from Windows AppData. The personalized clone's profile is appended to the EXE itself. Its settings and UI locks are re-embedded when saved. The generic template stays empty on every launch.

**The profile is portable, NOT encrypted.** Anyone who obtains your personalized EXE (or a `.bak` copy) can extract its API token and private SSH key. Keep personal clones and their backups private. Distribute **only the generic `CDM.exe`**.

## Configure a new clone

1. Launch generic `CDM.exe`; choose a nickname, language, and connection mode.
2. For a Coolify instance with trusted HTTPS, enter its URL. For a Coolify on a **private LAN/VPN**, an HTTP private IP or local hostname can be used without owning a domain.
3. For a public IP exposing Coolify only via HTTP, choose **automatic SSH tunnel**: VPS address, SSH username/port and Coolify remote address/port.
4. Click **Generate dedicated SSH key**. The wizard asks **Do you already have a working SSH alias on this PC?**
   - **Yes:** enter the existing alias and click **Authorize using SSH alias**.
   - **No:** copy the displayed one-line command and execute it in the VPS console as the intended SSH user.
5. **Verify server fingerprint** by comparing with the VPS independently; the fingerprint and verification command have **Copy** buttons.
6. Create **one Coolify API token with READ + DEPLOY** (not Root, Write or Read Sensitive Data). The wizard's API check only reads data; it does not deploy.
7. Click **Verify and create/save**. After a successful check, the blank template creates and launches your personal clone.

The clone does **not** require your former SSH alias on a new Windows computer, but managed SSH mode requires Windows OpenSSH Client installed (`ssh.exe`). No domain, public TLS certificate, external config file or password is required for SSH tunnel mode.

For connection troubleshooting, see [CDM_SETUP_GUIDE.md](CDM_SETUP_GUIDE.md).

## Update an existing personalized clone

Building a new `CDM.exe` **does not automatically upgrade** an existing personalized EXE. Close your clone first, then from the repository root run:

```powershell
$clone = (Resolve-Path '.\portable\CDM-YourUser-YourServer.exe').Path
Start-Process -FilePath '.\portable\CDM.exe' -ArgumentList ('--upgrade-clone "{0}"' -f $clone) -Wait
```

Replace the filename with your actual clone name. The upgrade copies the embedded profile into the new EXE, verifies it and creates a timestamped `.bak` copy before replacement. It has a bounded retry for Windows file-sharing locks; the file must not still be running. **The backup also contains your credentials.** This functionality has passed the disposable-clone offline smoke test; independently verify real-clone updates before release.

## Build on Windows (no cloud CI)

Required: Windows x64, .NET 10 SDK x64, PowerShell, and the separately supplied **`CDM_Logo_Assets.zip`** in your Windows Downloads directory. The script verifies the approved splash PNG/ICO hashes; missing assets are now an error rather than silently reverting to the old icon.

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

The script runs local classification, localization, embedded-profile, template-startup and disposable-clone-upgrade tests; then publishes `portable\CDM.exe` as a self-contained single-file Windows executable. **No GitHub Actions** are used or permitted for this project.

See [BUILDING.md](BUILDING.md), [DEVELOPER_NOTES.md](DEVELOPER_NOTES.md), and [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md).

## Validation status (31 October 2026)

**Verified on Windows:** portable build, generic clean startup, serialized profile tests, personalized-clone startup and independent SSH tunnel/API connection; one live Coolify inventory of 30 applications was displayed. The logo/splash and EXE icon were rendered; Windows Explorer may cache old icons. The disposable clone upgrade smoke test has also passed.

**Not yet certified for public release:** a real staging deployment and its monitoring, full usability review of all new card controls, and complete cross-machine portability testing. A successful build does not establish these behaviors.

## Release and license

The source is visible and CDM may be used professionally, modified and shared **free of charge**. **Selling copies, paywalling downloads or distributing paid bundles containing CDM is prohibited** without separate authorization. Truly voluntary contributions to the creator are welcome. See the actual terms in [LICENSE](LICENSE). This custom license is **not an OSI open-source license**.

The repository previously contained MIT-licensed historical versions. A change in licensing does not cancel any valid grants under those earlier versions. Old changelogs remain archival records and are not instructions for current configuration. Latest active documentation is linked above.

No personalized clones, API tokens, private SSH keys, local test output or backup copies belong in a public Git repository.

## Future GitHub Release downloads

The official single-file generic CDM.exe will be downloadable from [GitHub Releases](https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases) when it is published; currently no release is claimed. `PACKAGE_RELEASE.ps1` locally prepares the generic EXE, documentation ZIP and SHA256 file without uploading them. See [RELEASE_GUIDE.md](RELEASE_GUIDE.md) and the [31 October checkpoint](PROJECT_STATUS_2026-10-31.md).

## Voluntary support — buy Cris0338 a beer 🍺

CDM remains **completely free**. If you enjoy it and would like to support its development, you can choose any amount — absolutely optional:

**[🍺 Buy Cris0338 a beer via PayPal.Me](https://www.paypal.com/paypalme/Cris0338/)**

The donation message is translated in CDM's About window for every supported interface language. GitHub's Sponsor button links to the same PayPal.Me page. No payment is required to download, use or share CDM.
