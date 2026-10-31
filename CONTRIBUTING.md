# Contributing to CDM

CDM is created and maintained by **Cris0338**. It uses the custom [CDM Free Redistribution License v1.0](LICENSE), not MIT. You can study, modify and share versions of CDM **for free**, but you may not sell copies or provide paid access without the original author's separate authorization.

Please write C# source, technical documentation, comments and commit messages in English. UI translations are welcome in any supported language.

## Workflow

1. Fork the repository or create a feature branch. Keep your work separate from the release branch until tested.
2. Never commit personal EXEs, their `.bak` copies, Coolify tokens, private SSH keys, passwords, host-specific settings, or locally built outputs.
3. Run `BUILD_PORTABLE.ps1` on Windows with the approved logo ZIP installed; verify classification, localization and clone-profile smoke tests.
4. Test modified WPF windows locally. A passing build is not proof that SSH or deployment work end-to-end.
5. Submit a pull request explaining the changes and which tests actually ran. Mark any unverified behavior explicitly.

## Translations

See [LOCALIZATION.md](LOCALIZATION.md). The canonical language is English; preserve substitution placeholders and distinctions between production/staging, running/healthy, and local cancellation/server-side cancellation.

## Security

The generic `CDM.exe` is always blank. A personalized clone and its backups embed **recoverable** token/SSH credentials and must not be distributed. Do not silently disable TLS validation, relax host key pinning, expose tokens in logs or connect directly to public HTTP for bearer-token operations.

## Release rules

**NO GitHub Actions, no CI workflows, no cloud build and no automatic deployment.** Local Windows PowerShell scripts are the only supported build/test path. Do not tag, merge or publish executables before Windows UI acceptance and a deliberately initiated staging deployment test.

Contributors must preserve the attribution and free-redistribution/no-sale terms in modified distributions. License questions and permission for paid distribution must be addressed to the copyright holder, not inferred from the repository's public visibility.
