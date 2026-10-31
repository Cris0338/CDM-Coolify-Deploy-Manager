# Contributing to CDM

Thanks for helping make Coolify Deploy Manager accessible to more people.

CDM is an independent, MIT-licensed Windows application created by **Cris0338**. Please keep source code, commit messages, tests and documentation in **English**, even when contributing another UI language.

## Development workflow

1. Fork the repository and create a descriptive branch.
2. Make focused changes without introducing server-specific credentials, IP addresses, token values or personal configuration.
3. Run the offline tests and the portable build using `BUILD_PORTABLE.ps1` on Windows.
4. Open a pull request describing behavior, tests and any remaining limitations.
5. For connection or deployment changes, clearly distinguish verified behavior from assumptions.

## Translations

Read [LOCALIZATION.md](LOCALIZATION.md). Each translation is stored in a UTF-8 JSON file. English is authoritative; preserve placeholders and safety-related wording. Native-speaker review is particularly welcome for Preview locales.

## Security and releases

Never include `settings.json`, `CDM.profile`, API tokens, private SSH keys, passwords, local build output or server-specific files. Avoid turning off TLS certificate verification or SSH host-key checks. Release EXEs must be built and tested from reviewed source.

## Project limitations

CDM currently manages one saved Coolify server profile. Multi-server support is planned. A finished Coolify deployment is not proof of application health.
