# CDM 2.2.1 — build-identifiable maintenance update

**Pre-release candidate accepted by author (2026-10-31).** Code checkpoint compiled on Windows: `56ec905c0b6894898a78fc9a92eba93b7925593a`. GitHub release/tag and downloadable assets are distinct publication steps; see `RELEASE_GUIDE.md`.

- The **About / Informazioni / À propos** window now displays the actual compiled application version (2.2.1), Git source commit and UTC compilation timestamp; a Copy button puts these details on the clipboard for diagnostics.
- The main window title and initial log obtain the version from the built assembly, rather than displaying a stale hardcoded 2.2.0.
- `BUILD_PORTABLE.ps1` automatically supplies the source commit and UTC timestamp to MSBuild assembly metadata. The portable generic executable and any personalized clone built from it expose the same nonsecret build identity.
- Offline smoke validates the metadata **inside the published EXE** and again in a disposable personalized clone after explicit upgrade.
- All of the earlier fixes to the portable profile's `.pending` replacement and managed SSH reconnection remain in this branch; this update does **not** claim to have tested a remote SSH failure or live staging deployment.

### Verification

**Author's Windows result:** all portable build and offline smoke tests passed for CDM 2.2.1; the generic binary built at UTC 2026-10-31T21:33:56Z had SHA256 `3037F118C4529256910ECF84A6B29B0AB8F9A7F072C8AB4A77100E81CE6F953B`. After upgrading the actual personal clone, the owner confirmed Coolify connectivity and **more than 14 minutes of uninterrupted managed SSH operation**, with no observed disconnection. No real staging deploy was initiated; it will be validated in normal usage. The personal executable and backups remain local and MUST NOT be published.

Build identity is part of the compiled assembly (not the personal profile trailer), so ordinary embedded-settings changes do not falsify the version. Direct IDE builds without the build script intentionally report `local / unspecified` provenance.
