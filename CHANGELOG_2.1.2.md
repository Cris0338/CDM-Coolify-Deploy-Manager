# CDM WPF 2.1.2 — Portable encrypted profile (preview)

> **Historical record only.** This version may describe retired settings, encryption, build or MIT licensing. For CDM 2.2 use [README.md](README.md), [CDM_SETUP_GUIDE.md](CDM_SETUP_GUIDE.md) and the current [LICENSE](LICENSE).

- Stores encrypted configuration and Coolify API tokens in `CDM.profile` beside the executable.
- Uses user-chosen password (minimum 12 characters), PBKDF2-HMAC-SHA256 (600,000 iterations), and authenticated AES-256-GCM encryption.
- Prompts to create/unlock the portable profile, with an integrated dark WPF dialog.
- Migrates existing DPAPI-protected Windows settings without re-entering tokens, deleting the old settings JSON only after the portable profile is written and verified.
- Displays the portable/configured state in the window title and dashboard.
- Maintains the EXE unchanged, so portable profiles can be moved independently and are excluded from Git.
- Adds offline encryption, decryption, wrong-password and tamper-detection tests.

**Preview:** The new portable settings flow requires Windows UI acceptance testing before merging or publishing a binary release. The native Windows OpenSSH client, SSH keys and host configuration are not bundled in the EXE.
