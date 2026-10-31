# CDM 2.2 — current developer notes

CDM is a Windows x64 .NET 10 WPF application. Source files and technical docs are authored in English. Build/test locally with `BUILD_PORTABLE.ps1` **only**; GitHub Actions are not used or allowed.

## Modules

- `App.xaml.cs` — starts generic or personal EXE, offline smoke entry points, explicit clone upgrade, and approximately three-second logo splash.
- `MainWindow.xaml(.cs)` — inventory, searchable project picker, staging checkboxes/batch actions, card drag/drop and persistent position/deploy locks.
- `SettingsWindow.xaml(.cs)` — connection wizard, explicit alias yes/no choice, ED25519 provisioning, independently pinned SSH host key, single token.
- `Services/CoolifyApi.cs` — read-only inventory and explicit deployment/monitoring endpoints.
- `Services/SshTunnel.cs` — owned `ssh.exe` process, temporary key/known_hosts files with restricted Windows ACLs and localhost port forwarding.
- `Services/SettingsStore.cs` — blank template vs embedded personalized settings, cloning/saving; no legacy settings lookup.
- `Services/ExecutableProfile.cs` — trailer parsing/writing, personalized EXE creation and update helper.
- `Models.cs`, `ViewModels/AppCardView.cs` — server/app model, grouping, UI state, deploy/position locks.
- `Loc.cs` and `Localization/*.json` — 14 languages, en/fr/it complete, other languages with English fallback.
- `SplashWindow.xaml(.cs)`, `Assets/CDMLogo.png`, `Assets/CDMOfficial.ico` — transparent official splash and particle animation, embedded logo/icon.
- `tests/*` — classification, translation, profile and publish-time disposable-clone tests.

## Persistent state: embedded EXE only

`SettingsStore.Initialize()` reads the trailing profile of the **running executable** if present; otherwise constructs new empty settings. It does **not** load old files from AppData, current directory, registry, `CDM.profile` or `settings.json`.

For a personalized executable, the trailer layout is:

```text
[UTF-8 JSON user settings][SHA256 checksum (32 bytes)][Int64 payload length][CDMPF01!]
```

Max profile payload: 1 MiB. This checksum only detects accidental modification; it is **not encryption, digital signing or protection against credential extraction**. Settings include one READ+DEPLOY token, server/SSH data, preferred language, app classification overrides, card ordering, locked positions and blocked deployments. The unconfigured generic EXE must never inherit or load a personal profile.

On edits, CDM creates a pending EXE with the updated trailer; the helper attempts replacement after the running clone closes. The separate explicit upgrade command migrates the old clone's embedded profile into a newer template and creates a timestamped backup. Windows lock retries are bounded for the explicit upgrade. Verify any self-update/upgrade behavior on Windows before release.

**Temporary files are not permanent configuration:** key material and `known_hosts` are written only into a temporary directory during SSH operations and cleaned up when possible. There may also be temporary publish, smoke-test and pending EXE files.

## Network and operation

- HTTPS mode validates the certificate; LAN/private VPN HTTP endpoints are permitted for private environments.
- Public-IP HTTP tokens are not sent directly. Use an owned SSH port forward: localhost `127.0.0.1` to Coolify on the VPS.
- Managed SSH uses a dedicated ED25519 key, verified pinned host key, Windows `ssh.exe`, and temporary private-key ACL reduction through `icacls.exe`. Clones must not depend on the original PC's SSH alias.
- Setup asks explicitly whether a working SSH alias exists; otherwise displays a copyable command for the VPS console.
- Setup validates the token with a read-only API call. No deployments occur in the wizard.
- Single-card and staging batch deploys require explicit confirmations. Unknown classification cannot deploy. Production stays per-card with explicit confirmation.
- Deployment locks skip or disable actions; cancelling local monitoring is not equivalent to cancelling the Coolify server task.
- Card reorder uses section-local order. Drag any non-interactive part of a card; drop before/after another card, including the last, or onto unused WrapPanel space to append. No drag grip or dedicated end-of-section bar remains. Arbitrary persistent empty slots are not implemented.

## Assets, localization, builds

The approved `CDM_Logo_Assets.zip` is expected under Windows Downloads. `BUILD_PORTABLE.ps1` checks all PNG/ICO hashes and embeds the official assets in the published single-file executable. Without the files, the build fails rather than using a legacy icon. Explorer may cache an older icon for an existing filename.

Windows build/test: .NET 10 SDK, local PowerShell, no GitHub Actions, no remote deployments. Published generic `portable/CDM.exe` is credentials-free; personal clones and timestamped `.bak` files are **never release assets**.

## License

The active development branch uses [CDM Free Redistribution License v1.0](LICENSE). Source-available, free-to-use/share, **not for resale**. This is **not an OSI-approved open-source license**. Historical changelogs may mention MIT because earlier commits were MIT-licensed; those references are records, not the current terms.

## Maintenance rule — no stacked patches

**Owner requirement (2026-10-31):** Never accumulate successive workaround layers that override previous fixes. When correcting a component, **rewrite the affected complete logical unit** (XAML template, layout or C# handler set) into one coherent current implementation. Remove obsolete handlers, duplicate event routing and obsolete styling rather than stacking adapters or compensating patches. Preserve this rule in future sessions. Save all edits directly to the development branch on GitHub, never require the owner to apply hand-written source patches in PowerShell. The owner performs only pull/build/Windows GUI acceptance. No GitHub Actions, deployment, automatic release, or unrequested merge.

**Current card DnD contract:** drag from non-interactive card body/grip, cursor-following preview snapshot (WPF adorner), insert before/after target based on its horizontal midpoint, append in empty WrapPanel space (or insert after the last card using its right half); there is no extra end-target bar. Cards stay inside their staging/production/unknown groups; position locks cannot shift. A single `CommitCardMove` handles persistence and rollback on save error. The legacy grip-specific drag handlers and duplicated per-target save blocks have been removed. The translucent preview must never accept hit tests.

## Window chrome and resize (2026-10-31)

The full MainWindow titlebar border handles `PreviewMouseLeftButtonDown`, skipping any ButtonBase/interactive descendants. No `SizeAll` cursor, and dragging works across empty titlebar regions and labels. Minimize and maximize are vector Path buttons; the maximize glyph changes when maximized. Native `WM_GETMINMAXINFO` processing uses `MonitorFromWindow` and `GetMonitorInfo` to constrain maximize bounds to the current monitor's **work area** (taskbar respected, including on multi-monitor setups). No taskbar state changes.

The dashboard ScrollViewer and activity-log TextBox use the same 16px right inset. The log row is a resizable Grid track with its resize GridSplitter across the status-bar top edge; toggle hide/show saves and restores its previous row height, collapsing the actual track rather than leaving unused space. Drag-reorder and log resizing never initiate deployments.

These third/fourth iteration fixes are on the feature branch only. A successful static handler/XAML check does not substitute for the next Windows build and real UI trials. Do not release until manually accepted.

## Pending save retries and post-deploy health — 31 October 2026

**Observed by owner:** real clone had a newer `.pending` than the main EXE even after CDM processes were closed. This is a genuine unresolved save on that older EXE, NOT evidence that settings were automatically persisted. Never delete an unverified pending file, overwrite the clone from an older profile or commit personal EXEs/backups/credentials to GitHub.

The old `ExecutableProfile.ScheduleReplaceOnExit` launched a hidden PowerShell helper with one `File.Replace` attempt. If Windows temporarily locked the EXE, the pending update remained with no visible diagnostic. The entire helper method was replaced: validate the staged embedded-profile checksum/required settings, wait for the originating process to exit, retry transient lock/permission failures for a bounded 48 seconds (160 × 300ms), leave the pending EXE untouched if unsuccessful, and write a local `.updater-error.txt` containing only the failure description. This is a best-effort local diagnostic, not user configuration. On success, remove the error file. No real API token or SSH key is passed to the helper command line.

The build's disposable-clone smoke flow now performs an actual `SettingsStore.Save` with artificial token and card order, waits for automatic pending replacement and verifies the new EXE's embedded data. The exact real personal pending file must still be recovered/validated separately before installing the new generic build; the corrected updater cannot retroactively replace pending left by the older running binary.

**Health:** `deployments/{uuid}` returning `finished` means deployment job completed; application health is a separate runtime status such as `running:unhealthy` or `running:healthy`. `CoolifyApi.GetApplicationStatusAsync` reads `GET applications/{uuid}` with the READ token (official docs: https://coolify.io/docs/api/endpoints/applications/get-application-by-uuid). After the existing first inventory refresh, a bounded async monitor polls only finished applications every five seconds for up to 90 seconds, updating the existing `AppCardView` via `INotifyPropertyChanged` without extra deployments or overwriting deployment history. It stops on healthy, on next deploy, or on window close; unconfirmed health is never labeled healthy without API evidence. A generic-EXE offline smoke asserts unhealthy→healthy while preserving `finished` history.

## Focused security review — 2026-10-31

Scope: static inspection of `CoolifyApi.cs`, `SshTunnel.cs`, `SettingsWindow.xaml.cs`, `ExecutableProfile.cs`, `SettingsStore.cs`, `App.xaml.cs`, the WPF project's `.csproj`, local build tests and `.gitignore`. This is NOT a penetration test, dependency/CVE assessment, secret-scanning result, or a guarantee of secure production operation.

**Positive design measures**

- The generic `CDM.exe` does not load a personal profile or auto-connect; personalized profiles are deliberately separate.
- Coolify HTTPS uses normal .NET certificate validation; public remote HTTP is rejected by `CoolifyApi.NormalizeUrl`; direct HTTP is allowed for loopback, private LAN and VPN endpoints (LAN HTTP is still unencrypted on that segment).
- Managed OpenSSH uses `StrictHostKeyChecking=yes`, dedicated `known_hosts`, user-confirmed fingerprint, `-F none`, and a local-only `127.0.0.1` listener. Temporary SSH identity files are restricted to the Windows user via `icacls`, with best-effort cleanup when tunnel exits.
- Bearer API credentials are sent in the Authorization header, not inserted into the URL. The deployment request is sent only on the explicit, confirmed deployment path. The health sync reads application status with the read token and performs no new deployments.
- The source-only Git repository excludes `portable/`, generated binaries, personal settings, temporary executable updates and backup artifacts. Build smoke uses synthetic credentials, never an actual personal clone.
- Project's WPF `.csproj` shows no external NuGet `PackageReference`; SDK/framework dependencies still require their own lifecycle and security updates.

**Important remaining risks — do not conceal from users**

1. **HIGH — local credential disclosure from personalized binary:** the personal EXE trailer is unencrypted UTF-8 JSON containing a full Coolify bearer token and SSH private key. Its `SHA256` is an *unkeyed checksum* to detect corruption, not an authenticity check; anyone who can read and rewrite the EXE can recompute it. Treat `CDM-<user>-<server>.exe`, all `.pending` files and all `.bak` copies as secrets. `.gitignore` protects against ordinary accidental add, NOT exfiltration, malware, prior Git history or cloud-backup sharing. A genuinely encrypted-at-rest design needs a deliberate choice between Windows DPAPI (machine/user bound) and portable passphrase encryption, and associated backup and recovery rules.
2. **MEDIUM/HIGH — distribution authenticity:** current EXE is not Authenticode signed; a SHA256 file helps only if the expected digest itself comes from a trusted source. Do not mistake executable-trailer hashing for publisher authentication. Sign public releases and verify provenance before distributing.
3. **MEDIUM — HTTP inside private LAN:** app allows direct HTTP to private addresses; a hostile device on that LAN can inspect Bearer tokens. Prefer HTTPS or the verified SSH tunnel where local networks are not fully trusted. Existing SSH forwarding terminates at the configured remote HTTP service; its reachability and network exposure remain outside this app.
4. **MEDIUM — privileges:** a single user-configured token is used for READ and DEPLOY when separate values are not supplied. Its Coolify permissions determine the blast radius; the UI cannot enforce server-side least privilege. Dedicated server credentials scoped to intended resources are preferable.
5. **LOW/MEDIUM — residual temporary files:** a Windows lock/crash can leave temporary SSH-key material or a credential-bearing `.pending`/backup on disk; cleanup cannot be guaranteed on an abrupt exit. Existing close-time updater now retries and records a local failure message, but Windows smoke acceptance is pending.

**Build status at this checkpoint:** user-reported Windows build error `CS0246 InvalidDataException` at `CoolifyApi.cs` line 223; resolved by `using System.IO;` at commit `0d69a6f`. No Windows build had yet confirmed the fix or the new disposable-clone save/health tests. No credentials were read or transmitted during this static review.

**Release gate:** do not publish a personalized clone or debug backup; require owner-accepted Windows build + offline smoke + security strategy for encrypting personal credentials, plus public-binary authenticity check before claiming release-grade security.
