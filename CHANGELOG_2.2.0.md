# CDM 2.2.0 — Personal executable clones (development preview)

- Replaced password-protected `CDM.profile` with a portable profile embedded inside each personal EXE.
- Initial `CDM.exe` template is unchanged; guided setup creates `CDM-<user>-<server>.exe`, starts it and closes the original.
- One Coolify API token using READ and DEPLOY together; read-only API verification during setup.
- HTTPS with certificate validation, or owned SSH tunnel with dedicated ED25519 identity and pinned host key.
- SSH onboarding supports one-click public-key authorization through an existing SSH alias (when available), and browser login through the local tunnel.
- Clone updates are staged and atomically applied after shutdown by a detached PowerShell helper.
- No passwords are requested for portable profiles and no permanent `settings.json` / `CDM.profile` is created.
- Deleted GitHub Actions workflow. Builds and tests are local only.
- Added executable profile integrity and round-trip offline tests.

**Limitations:** single-file clone startup and SSH wizard have not been compiled or manually validated on Windows in this branch. SSH mode requires Windows OpenSSH Client. Credentials inside an unprotected EXE can be extracted by anyone who obtains the file. Full 2.2.0 setup localization remains to be completed.
