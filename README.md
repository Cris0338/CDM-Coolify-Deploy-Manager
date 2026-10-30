# CDM — Coolify Deploy Manager (WPF 2.1.0)

**Created by Cris0338** · Copyright © 2026 Cris0338 · [MIT License](LICENSE)

CDM is an **independent, unofficial** desktop application for managing Coolify projects and deployments on Windows 10/11 (x64). It is not affiliated with Coolify.

## Features

- Discover Coolify projects, environments and applications via the Coolify REST API.
- Group applications by project; distinguish staging, production, and ambiguous resources using application name, Git branch and environment.
- Show separate running/health status, latest deployment time, deployment result and commit.
- Drag to reorder cards, with per-project persistent ordering.
- Individual deployments with confirmation; sequential selected-staging queue, including a stop-waiting action (this **does not cancel server-side deployments already started**).
- Built-in API setup guide and activity log.
- Dark WPF interface with custom window chrome and settings dialogs.
- **English, Français, Italiano**, automatic detection based on Windows UI language, manual override in Settings.
- API access via verified **HTTPS**, automatic **OpenSSH tunnel**, or an already-open localhost tunnel.
- Two separately scoped tokens (`read` and `deploy`) encrypted at rest with Windows DPAPI for the current Windows user.

## Windows build (.NET 10 SDK)

Extract this archive into its own folder, then run in PowerShell **from the folder containing `COMPILA_PORTABLE.ps1`**:

```powershell
powershell -ExecutionPolicy Bypass -File .\COMPILA_PORTABLE.ps1
```

The script executes 8 classification tests and publishes `portable\CoolifyDeployManager.exe` as a Windows x64, self-contained, single-file executable. It also prints the SHA-256 hash. Do not commit build output or personal settings.

## Connection modes

**Public HTTPS:** Enter the Coolify dashboard hostname with a trusted TLS certificate. Connecting directly to a raw VPS IP often fails certificate verification, or reaches the reverse proxy rather than Coolify. CDM does not bypass TLS validation.

**Automatic SSH:** Configure working key-based OpenSSH authentication in Windows (for example, `ssh my-vps`). In CDM Settings choose **Automatic SSH tunnel** and enter the alias, local port and Coolify's remote address/port. For common installations: local port `18000`, remote host `127.0.0.1`, remote port `8000`. CDM runs the system's `ssh.exe` invisibly, validates the host key and checks the Coolify `/api/health` endpoint before loading the inventory. **The EXE includes the .NET runtime, but not `ssh.exe`**; OpenSSH Client must be available if you choose this mode. No interactive password prompt is supported.

**External localhost tunnel:** Choose this only when you are already managing the tunnel yourself. If the tunnel closes, CDM will report that the local port is unavailable. To switch to automatic SSH, close your external tunnel first so the program can bind the local port.

## Token storage and portability

Settings live under `%LOCALAPPDATA%\CoolifyDeployManager\settings.json` and survive application restarts and updates. Tokens are protected with Windows DPAPI (`CurrentUser`), so they are **not** automatically transferable to a different Windows account or machine. The executable itself is portable.

## Development and release status

This project has passed **static checks only** for WPF 2.1.0; a full Windows compilation and functional testing are still required before a public release. In particular verify: settings contrast and dropdowns, window chrome, SSH startup/reconnect, token migration from 2.0.3, staging/production classification, and a single staging deployment. A completed Coolify deployment does not by itself prove application health.

Licensed under MIT; see [LICENSE](LICENSE) and [AUTHORS.md](AUTHORS.md).
