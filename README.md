# CDM — Coolify Deploy Manager

**Created by [Cris0338](https://github.com/Cris0338)** · © 2026 Cris0338 · [MIT License](LICENSE)

CDM is an **independent, unofficial** desktop application for managing [Coolify](https://coolify.io/) applications and deployments on Windows 10/11 (x64). It is not affiliated with or endorsed by Coolify.

## Features

- Automatically discover Coolify projects, environments, applications and deployment history through the REST API.
- Navigate by project; distinguish staging, production and unclassified applications using names, branches and environments.
- View running/health indicators, latest deployment time, deployment result and commit.
- Reorder application cards; preserve ordering per project.
- Start individual deployments or a sequential staging queue. **Stopping the local queue does not cancel deployments already started on Coolify.**
- Dark WPF dashboard, custom title bar, activity log, API connection guide and settings.
- Connect with verified public HTTPS, an automatically managed Windows OpenSSH tunnel, or an already-running localhost tunnel.
- Store read and deploy API tokens encrypted with Windows DPAPI for the current user.
- Localized interface with automatic Windows language detection and manual language selection.

## Languages

CDM currently offers **14 interface languages**:

| Translation status | Languages |
| --- | --- |
| Complete catalogs | English, Français, Italiano |
| Community preview (partial; missing strings fall back to English) | Español, Deutsch, Português (Brasil), Nederlands, Polski, Türkçe, Русский, Українська, 日本語, 한국어, 简体中文 |

English is the source language for all code, technical messages, documentation and translation identifiers.

Translations live in `src/CoolifyDeployManager/Localization/*.json` and are embedded in the portable EXE. See [LOCALIZATION.md](LOCALIZATION.md) to contribute translations or improve existing ones. Preview translations have not yet undergone native-speaker review.

## Build from source (Windows)

Requires the **.NET 10 SDK x64**. Visual Studio is not required.

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

The build script runs classification and localization checks, then publishes `portable\CoolifyDeployManager.exe` as a self-contained single-file Windows x64 executable. See [BUILDING.md](BUILDING.md).

## Connecting to Coolify

**Public HTTPS:** Use the Coolify dashboard hostname with a valid trusted TLS certificate. CDM does not disable certificate validation.

**Automatic SSH tunnel:** Configure key-based Windows OpenSSH (for example, `ssh my-server`), then choose *Automatic SSH tunnel* in Settings and enter the SSH alias, local port and remote Coolify host/port. Windows OpenSSH Client is required and is not included inside the EXE. Host keys are verified.

**External tunnel:** Choose this only when you already manage an SSH tunnel yourself.

In Coolify, enable API access and create separate `read` and `deploy` tokens with permissions for the appropriate team. Do not share these tokens.

## Token protection

Settings are stored in `%LOCALAPPDATA%\CoolifyDeployManager\settings.json`. Tokens are encrypted with Windows DPAPI (`CurrentUser`), remain available across app restarts and upgrades, and do **not** travel with the portable EXE to a different Windows account or machine.

## Project status

Version **2.1.1 (localization preview)** is in development. WPF 2.1.0 compiled successfully on Windows 10 x64 with .NET 10 SDK 10.0.403 and passed all eight existing classification tests. The localization refactor and new translations require the automated Windows build and UI acceptance checks before a packaged GitHub Release.

**CDM currently stores one Coolify server profile at a time.** Multi-server profiles are planned, not implemented.

See [CHANGELOG_2.1.1.md](CHANGELOG_2.1.1.md), [CONTRIBUTING.md](CONTRIBUTING.md) and [LICENSE](LICENSE).
