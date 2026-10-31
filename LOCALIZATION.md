# CDM — localization

English is the canonical language for source comments, technical documentation and localization keys. The UI includes **14 embedded languages**:

| Complete catalogs | Preview catalogs (English fallback for missing keys) |
| --- | --- |
| English (`en`), Français (`fr`), Italiano (`it`) | Español (`es`), Deutsch (`de`), Português-BR (`pt-BR`), Nederlands (`nl`), Polski (`pl`), Türkçe (`tr`), Русский (`ru`), Українська (`uk`), 日本語 (`ja`), 한국어 (`ko`), 简体中文 (`zh-CN`) |

On the 31 October 2026 Windows build, en/fr/it had **221/221 localization keys**; preview catalogs had **53/221** and fell back to English. The exact number can change as keys are added.

To contribute:

1. Open `src/CoolifyDeployManager/Localization/<code>.json` and compare it with `en.json`.
2. Preserve parameter placeholders such as `{0}`, `{1}`, `{2}`. Keep technical meanings precise.
3. Translate SSH alias question, authorization branches, fingerprint verification and deployment confirmations carefully.
4. Keep JSON valid UTF-8 and never include personal tokens, keys, server names or local paths.
5. Run the local localization tests and `BUILD_PORTABLE.ps1`. No GitHub Actions.
6. Submit a pull request with a native-speaker review when possible.

A source translation contribution must respect the project's [CDM Free Redistribution License v1.0](LICENSE). Preview catalogs use English fallback intentionally.
