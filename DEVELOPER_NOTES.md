# CDM developer notes (2.2.0)

CDM uses .NET 10 Windows Presentation Foundation (WPF). All source code and technical docs are written in English.

## Layout

- `MainWindow.xaml(.cs)` — project dashboard, staging queue, cards, current state.
- `SettingsWindow.xaml(.cs)` — single-token onboarding, HTTPS, SSH key setup, in-browser login.
- `Services/CoolifyApi.cs` — REST API requests and deployment status polling.
- `Services/SshTunnel.cs` — owned Windows OpenSSH process; ephemeral key and known_hosts files; host pinning; localhost-only forward.
- `Services/ExecutableProfile.cs` — binary trailer reader/writer on a published PE single-file EXE, SHA256 integrity check, background self-update after process exit.
- `Services/SettingsStore.cs` — legacy Windows DPAPI settings read/migration and new in-EXE profile saves. Never writes new AppData configuration.
- `Models.cs`, `Loc.cs`, `Localization/*.json` — models, classifier, embedded translations.
- `tests/ExecutableProfileTests`, `tests/ClassifierTests`, `tests/LocalizationTests` — offline checks.

## Portable profile and security

Binary trailer: `[UTF-8 JSON settings][SHA256 (32)][Int64 little-endian payload length][CDMPF01!]`. Profile limit 1 MiB. SHA256 detects corruption but does not protect against a malicious editor or hide credentials. The own EXE is copied without existing trailer, then a new trailer is appended, keeping the template clean.

The run-time settings clone is rebuilt to a hidden pending file; a hidden PowerShell helper waits for the process to exit and atomically replaces the clone. A configured clone can be copied to another Windows PC.

The SSH tunnel uses `BatchMode=yes`, `StrictHostKeyChecking=yes`, `ExitOnForwardFailure=yes`, an explicit localhost bind, a private key and pinned host key materialized only for the tunnel lifetime. Never forward a token over plain public HTTP.

## Release rules

**No GitHub Actions in any project.** All builds and tests are local, using `BUILD_PORTABLE.ps1`. Do not publish source or personal clone files containing credentials. Validate a published EXE on Windows before tagging or merging a release; no Windows SDK is available in the authoring environment for this branch.
