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
