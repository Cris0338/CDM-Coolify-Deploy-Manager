# CDM — local Windows build

## Requirements

- Windows 10/11 x64 with PowerShell 5.1 or later
- .NET 10 SDK x64 at `%ProgramFiles%\dotnet\dotnet.exe`
- Checked-out branch `feature/embedded-clone-20261031`
- Approved `CDM_Logo_Assets.zip` saved in **Windows Downloads** (do not extract manually)
- Windows OpenSSH Client only if you plan to use the managed SSH connection

## Build and offline test

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1
```

The script checks the SHA256 of the artwork ZIP and both its PNG/ICO files, copies them to the ignored `Assets` directory, and runs the classification, localization, executable-profile and URL validation tests. It then builds the self-contained single-file Windows executable `portable\CDM.exe`. Final smoke tests verify that the generic template is blank and that a **disposable** personalized clone can start and be upgraded with its profile intact.

The generic template is replaced on each successful build. **The real personalized clones alongside it are not modified or reconfigured.** Close `portable\CDM.exe` before compiling; Windows locks running executables. Other files in `portable\` are normal and may include private personal clones and their credential-bearing backups.

There is **no GitHub Actions workflow, cloud build, or automatic deployment**.

## Upgrade a real personal clone (explicit)

First close it, then:

```powershell
$clone = (Resolve-Path '.\portable\CDM-YourUser-YourServer.exe').Path
Start-Process -FilePath '.\portable\CDM.exe' -ArgumentList ('--upgrade-clone "{0}"' -f $clone) -Wait
```

The new generic EXE reads the explicitly named old clone and writes an updated clone with its embedded settings, after creating a timestamped backup. A clone's token/SSH private key exists in both the clone and backup. If Windows keeps the old EXE locked, the upgrader retries sharing violations for a bounded interval and then reports the failure.

Do not publish or commit the personal clone. A released `CDM.exe` template contains no server credentials.

## Windows acceptance before publishing

See [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md). Passing offline tests does not prove a real staging deploy or a cross-PC SSH session. Deployments must be tested only when deliberately triggered.

## Historical storage behavior

Previous releases may have used `settings.json`, DPAPI or `CDM.profile`. **CDM 2.2 does not use, read, import, migrate or overwrite them.** It stores ongoing settings only inside each personalized EXE. Temporary SSH files and staged EXE replacements are distinct from permanent configuration.

## Licensing

This development branch uses the [CDM Free Redistribution License v1.0](LICENSE), not MIT. Use, modification and free sharing are allowed; sale or paid distribution is restricted. No GitHub Actions.

## Updated offline smoke checks (2026-10-31)

In addition to the generic blank template and clone/upgrade tests, `BUILD_PORTABLE.ps1` now invokes two isolated, offline regression checks: a WPF card transitions from `running:unhealthy` to `running:healthy` without changing its finished deployment history; and a disposable clone modifies its artificial embedded profile, exits, automatically replaces its `.pending` EXE and is relaunched to verify the synthetic token, saved card ordering and locked positions. These tests use no real VPS, tokens, personal clones or deployment requests. A stuck pending helper causes the smoke test to FAIL rather than silently claiming completion.
