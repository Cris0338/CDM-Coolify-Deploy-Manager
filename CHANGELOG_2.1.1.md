# CDM WPF 2.1.1 — Internationalization preview

> **Historical record only.** This version may describe retired settings, encryption, build or MIT licensing. For CDM 2.2 use [README.md](README.md), [CDM_SETUP_GUIDE.md](CDM_SETUP_GUIDE.md) and the current [LICENSE](LICENSE).

- Extracted English, French and Italian UI strings into separate embedded JSON catalogs.
- Added 11 **preview** languages: Spanish, German, Brazilian Portuguese, Dutch, Polish, Turkish, Russian, Ukrainian, Japanese, Korean and Simplified Chinese.
- Preserved English as the safe fallback for untranslated strings.
- Converted technical comments, user-visible nonlocalized error text, build scripts and documentation to English.
- Added localization validation for JSON catalogs, translation keys and message placeholders.
- Renamed the build script to `BUILD_PORTABLE.ps1`.
- Added contribution instructions and a Windows CI workflow.

**Release status:** Localization changes must pass the Windows build and UI test before merging into the public default branch or publishing a binary release.
