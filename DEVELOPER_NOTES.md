# CDM developer notes

CDM is built on .NET 10 and Windows Presentation Foundation (WPF). C# and the repository documentation use English as the source language.

## Source layout

- `src/CoolifyDeployManager/App.xaml` — application-wide dark theme, styles and brushes.
- `MainWindow.xaml(.cs)` — dashboard, projects, card layout, filters, deployment queue and activity log.
- `SettingsWindow.xaml(.cs)` — HTTPS and SSH connection modes, token entry, language selection.
- `Services/CoolifyApi.cs` — Coolify API client, discovery, deployment and status polling.
- `Services/SshTunnel.cs` — managed Windows OpenSSH process with host-key verification.
- `Services/SettingsStore.cs` — user settings and Windows DPAPI-protected API tokens.
- `Models.cs` — application models, staging/production classifier and user settings.
- `Loc.cs` — localization lookup with English fallback and Windows culture detection.
- `Localization/*.json` — embedded locale catalogs; English is the canonical set.
- `tests/ClassifierTests` — offline classification checks.
- `tests/LocalizationTests` — locale schema and formatting-placeholder validation.

## SSH behavior

The managed SSH mode launches the Windows `ssh.exe` client with `BatchMode=yes`, `StrictHostKeyChecking=yes`, `ExitOnForwardFailure=yes`, an explicit `127.0.0.1` local bind and `-N -T`. The application owns and terminates only the process it started; it does not adopt arbitrary existing local listeners. API tokens are never passed as SSH command-line arguments.

## Releasing

Use `BUILD_PORTABLE.ps1`, verify the Windows CI build, and follow the manual acceptance checklist before publishing a binary release. CDM is an independent, unofficial Coolify client.

Contributions: [CONTRIBUTING.md](CONTRIBUTING.md).
