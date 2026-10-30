# Localization guide

English is the canonical language for CDM source code, comments, documentation, logs and translation keys. The application interface supports localization.

## Supported languages

| Code | Name | Status |
| --- | --- | --- |
| `en` | English | Complete |
| `fr` | Français | Complete |
| `it` | Italiano | Complete |
| `es` | Español | Preview |
| `de` | Deutsch | Preview |
| `pt-BR` | Português (Brasil) | Preview |
| `nl` | Nederlands | Preview |
| `pl` | Polski | Preview |
| `tr` | Türkçe | Preview |
| `ru` | Русский | Preview |
| `uk` | Українська | Preview |
| `ja` | 日本語 | Preview |
| `ko` | 한국어 | Preview |
| `zh-CN` | 简体中文 | Preview |

All languages are embedded into the EXE at build time. The first three catalogs have complete coverage; preview catalogs initially cover 53 important interface strings. Missing keys use English without showing raw identifiers.

## Improve an existing translation

1. Open `src/CoolifyDeployManager/Localization/<code>.json`.
2. Use `en.json` as the canonical key list; add missing keys as needed.
3. Preserve parameter placeholders such as `{0}`, `{1}` and `{2}` exactly. Do not change their quantity or numbers.
4. Preserve semantic distinctions: *running* vs *healthy*, *staging* vs *production*, *cancel local queue* vs *cancel deployment*.
5. Keep JSON valid UTF-8. Do not include credentials, server names or user-specific settings.
6. Run `BUILD_PORTABLE.ps1` on Windows or the localization tests separately, then open a GitHub pull request.

To add a language, create `<code>.json` and register its code/native name in `Loc.Languages`. Unsupported system languages fall back to English. `pt` resolves to `pt-BR` and `zh` to `zh-CN`.

Community translations should be reviewed by a fluent speaker before removing the Preview label. Fallback English is preferable to incorrect or unsafe instructions.
