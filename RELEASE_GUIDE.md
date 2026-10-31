# CDM — Manual GitHub Release (development workflow)

**No public release is currently published. No GitHub Actions or automatic publishing.**

## Recommended downloads

- **CDM.exe** — the generic, portable Windows 10/11 x64 executable; opens first-run setup and contains no credentials.
- **CDM-2.2.0-win-x64.zip** — generic CDM.exe, English/Italian documentation, 14-language SUPPORT.md, installation note and active LICENSE.
- **CDM-2.2.0-SHA256.txt** — integrity hashes of both assets.

**NEVER upload personal CDM-username-server.exe clones, backup .bak files, SSH keys or API tokens.**

## One-command local packaging

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\PACKAGE_RELEASE.ps1
```

The script performs the full local build and smoke tests, checks that the binary does not contain a personalized-profile trailer, embeds the 14-language SUPPORT.md document in the ZIP, and writes the three approved assets to the Git-ignored **release-artifacts/** folder. It does not tag, upload, merge, create a release, or deploy to Coolify.

## Before publishing

Complete [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md), including GUI verification, testing the manual SSH authorization flow, one deliberate staging deployment, and verifying real personal clone updates. A successful build alone is not sufficient.

**Important:** `main` was fast-forwarded to the current CDM free-redistribution/no-resale license on 31 October 2026. Before tagging, verify that the release's target commit is the **same reviewed and built commit**, including any later PayPal.Me/localization changes. Release tag, EXE binary, and license must agree.

## Publishing via GitHub web interface (manual)

1. Navigate to https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases.
2. Choose **Draft a new release** and create a tag such as **v2.2.0** targeting the verified main commit.
3. Set the release title, for example **CDM 2.2.0 — Windows portable**.
4. Write the release notes: what changed, Windows 10/11 x64 requirement, unofficial Coolify connection, free/no-resale license and optional OpenSSH Client for SSH connections. Include voluntary-support text in English, French and Italian with the verified https://www.paypal.com/paypalme/Cris0338/ link (no amount specified). Complete optional support text is available in [SUPPORT.md](SUPPORT.md), covering all 14 UI languages. Use [RELEASE_NOTES_2.2.0.md](RELEASE_NOTES_2.2.0.md) as the release description template.
5. Upload **ONLY** the three files from release-artifacts. Do not upload the portable folder itself.
6. Use **Save draft** to check the files and release details. When testing is accepted, choose **Publish release**. Mark as **pre-release** if it is not yet stable.

GitHub's automatically generated Source code (zip) is NOT the Windows program; use the separately uploaded assets.

## Easy links after publication

- Landing page: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/latest
- Latest stable single EXE: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/latest/download/CDM.exe
- Specific ZIP: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/download/v2.2.0/CDM-2.2.0-win-x64.zip

The latest links only work after a stable release has been published. Unsigned Windows EXEs may trigger a Windows SmartScreen reputation prompt. Future optional code signing can improve trust, but no certificate is configured now.

## Later improvements

Additional usability changes have been deferred by the author. Log concrete requests when decided rather than marking them implemented. No new features should be mixed into a tagged, verified version after the release package is built.
