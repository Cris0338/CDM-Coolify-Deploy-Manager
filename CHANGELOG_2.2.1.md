# CDM 2.2.1 — build-identifiable maintenance update

**Development branch:** `feature/embedded-clone-20261031`. This is a local build checkpoint, **not a published release**.

- The **About / Informazioni / À propos** window now displays the actual compiled application version (2.2.1), Git source commit and UTC compilation timestamp; a Copy button puts these details on the clipboard for diagnostics.
- The main window title and initial log obtain the version from the built assembly, rather than displaying a stale hardcoded 2.2.0.
- `BUILD_PORTABLE.ps1` automatically supplies the source commit and UTC timestamp to MSBuild assembly metadata. The portable generic executable and any personalized clone built from it expose the same nonsecret build identity.
- Offline smoke validates the metadata **inside the published EXE** and again in a disposable personalized clone after explicit upgrade.
- All of the earlier fixes to the portable profile's `.pending` replacement and managed SSH reconnection remain in this branch; this update does **not** claim to have tested a remote SSH failure or live staging deployment.

### Verification

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\BUILD_PORTABLE.ps1` after a `git pull --ff-only`. The owner must report **all PASS** results before upgrading the actual personalized clone. The connected Coolify, personal SSH key, credentials and backups are not changed by this source-only update.

Build identity is part of the compiled assembly (not the personal profile trailer), so ordinary embedded-settings changes do not falsify the version. Direct IDE builds without the build script intentionally report `local / unspecified` provenance.
