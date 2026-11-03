# CDM 3.0.2 — Windows x64 portable

**Release status: candidate validated locally; public GitHub release pending.**

## Included in 3.0.2

- WPF dashboard, Coolify inventory and explicit deployment controls, deployment history/status indicators, tray, window placement and card preferences.
- Independent setup wizard and companion Guide, managed SSH forwarding with dedicated restricted account, token-based Coolify API access.
- Windows startup registration manager and refresh support for existing eligible personal v3.0.0/v3.0.1 executables; v2 migration is refused.
- New clean public source snapshot without previous public Git commit history, old personal VPS examples, or historical release logs.
- Removed PNG `caBX` provenance metadata from embedded and demo artwork while preserving image data.
- Repaired version-specific and anonymized SSH smoke tests in the clean snapshot.

## Validation

Local Windows x64 build, offline WPF/SSH smoke suite, generic-EXE empty-profile verification, binary signature scan, PNG metadata scan and EXE/ZIP SHA256 checks: **PASS**. Manual launch of the generic 3.0.2 EXE: wizard, Guide and About version display **PASS**.

This source snapshot has **not** yet been used for a public release. A fresh build from the eventual single initial Git commit will produce final commit metadata and SHA256 values; temporary test-build hashes must not be cited as official release hashes.

Only the generic EXE, generic ZIP and SHA256 manifest may be published. Never distribute a personalized EXE or private Git-history backup.
