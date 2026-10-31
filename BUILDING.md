# Building CDM from source

## Requirements

- Windows 10 or 11, x64
- Microsoft .NET 10 SDK x64 (`dotnet --list-sdks` should show version 10.x)
- PowerShell (bundled with Windows)
- Windows OpenSSH Client only if using SSH connection modes

No Visual Studio installation is required.

## Build

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

The script runs the classification tests, validates localization JSON and placeholders, and publishes a Windows x64 self-contained single-file executable at:

`portable\CoolifyDeployManager.exe`

The executable contains the .NET runtime but does not bundle `ssh.exe`.

## First connection

In Settings, choose one of the three connection modes:

1. **Public HTTPS** — trusted HTTPS domain for the Coolify dashboard.
2. **Automatic SSH tunnel** — previously trusted OpenSSH host alias, local port and Coolify remote host/port.
3. **Existing local tunnel** — only if an SSH tunnel has already been established externally.

In Coolify, enable API access and create a `read` token and a `deploy` token for the right team.

The first time you save settings, CDM prompts for a portable-profile password (12 characters minimum) and writes encrypted `CDM.profile` next to the EXE. Existing DPAPI-protected Windows settings are migrated at first launch after upgrading and the previous settings JSON is deleted after verification.

Move the EXE and `CDM.profile` together for portable use, then unlock the profile with its password. The EXE is never rewritten with secrets. An SSH connection also requires SSH keys/configuration on the destination computer.

## Release validation

A successful build is not enough for a public binary release. Follow [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md), test a staging deployment and independently verify application health. Do not upload user-specific settings or secrets.
