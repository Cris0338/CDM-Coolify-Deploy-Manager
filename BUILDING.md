# Building CDM locally

1. Windows 10/11 x64, .NET 10 SDK x64, PowerShell.
2. Clone the repository and check out `feature/embedded-clone-20261031`.
3. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

The script runs three offline test projects (classification, localization, executable profile), then publishes the Windows x64 self-contained single-file WPF application as **`portable\CDM.exe`**.

No GitHub Actions, cloud build or automatic workflow is involved.

## Windows acceptance criteria before release

- Fresh generic template ignores all older user settings and opens the blank setup on startup, with no password dialog and no automatic API requests.
- HTTPS with a trusted URL **or LAN/VPN HTTP by private IP** and a READ+DEPLOY token gives the dashboard (no domain required).
- SSH setup generates a key, checks a pinned host fingerprint and successfully creates its own tunnel.
- Browser-based Coolify login works through localhost on the managed SSH tunnel.
- A configured clone `CDM-user-server.exe` opens on a different Windows account/computer *without* an SSH alias or user settings file.
- Opening the clone does not require a profile password.
- Modifying settings and card order survives restart, after the prior process has exited.
- Tests pass and the *published .NET single-file EXE* still starts after its profile is appended.
- Read-only token tests never deploy anything; explicitly confirm one staging deployment before release.

The self-contained .NET executable does not bundle Windows OpenSSH Client. It must be available on a machine using SSH mode. No guarantee is made that every Windows installation has it.

Do not publish personalized EXEs: they contain recoverable tokens/SSH private keys. The generic template contains no credentials. Executable profile integrity is checksum-protected but not encrypted or signed.

The program may create temporary key material while an SSH tunnel is running, and a temporary pending EXE while saving changes. Application settings are not permanently written to AppData.
