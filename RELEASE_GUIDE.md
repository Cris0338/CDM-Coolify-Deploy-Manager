# CDM — Manual GitHub Release (development workflow)

**v2.2.1 published as a public pre-release on 31 October 2026:** [official release page](https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/tag/v2.2.1). The following manual steps are preserved as a reference for future releases; DO NOT create v2.2.1 again. No GitHub Actions or automatic publishing.

## Recommended downloads

- **CDM.exe** — the generic, portable Windows 10/11 x64 executable; opens first-run setup and contains no credentials.
- **CDM-2.2.1-win-x64.zip** — generic CDM.exe, English/Italian documentation, 14-language SUPPORT.md, installation note and active LICENSE.
- **CDM-2.2.1-SHA256.txt** — integrity hashes of both assets.

**NEVER upload personal CDM-username-server.exe clones, backup .bak files, SSH keys or API tokens.**

## One-command local packaging

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\PACKAGE_RELEASE.ps1
```

The script performs the full local build and smoke tests, checks that the binary does not contain a personalized-profile trailer, embeds the 14-language SUPPORT.md document in the ZIP, and writes the three approved assets to the Git-ignored **release-artifacts/** folder. It does not tag, upload, merge, create a release, or deploy to Coolify.

## Before publishing

For this **v2.2.1 pre-release**, the owner explicitly accepted the current Windows and live-SSH validation, deferring an intentional staging deployment and a second-PC portability test to normal future use. Label it **pre-release**, not a fully certified stable release. Do not imply these outstanding checks passed. For later stable releases, complete [CHECKLIST_TEST_WPF.md](CHECKLIST_TEST_WPF.md), including a deliberate staging deployment.

**Verified v2.2.1 publication:** the tag targets source commit `3ad03bb74cfbfbd84f19a04c677892b1c693662f`; published generic EXE embeds that same commit, built `2026-10-31T22:20:52Z` with SHA256 `4B50975A82F6F542388B033DDB20B266C9A0F430FAF39BDC2423C56628E3915B`. GitHub confirmed all three assets uploaded. Subsequent commits are documentation only; do not retag the release to the latest documentation head. The earlier `56ec905c0b68` build was a separate successful test, not the distributed executable. Release tag, EXE binary, and license must agree.

## Historical procedure for a future new release (do not re-create v2.2.1)

1. Navigate to https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases.
2. Choose **Draft a new release** and create a tag such as **v2.2.1** targeting the verified main commit.
3. Set the release title, for example **CDM 2.2.1 — Windows portable**.
4. Write the release notes: what changed, Windows 10/11 x64 requirement, unofficial Coolify connection, free/no-resale license and optional OpenSSH Client for SSH connections. Include voluntary-support text in English, French and Italian with the verified https://www.paypal.com/paypalme/Cris0338/ link (no amount specified). Complete optional support text is available in [SUPPORT.md](SUPPORT.md), covering all 14 UI languages. Use [RELEASE_NOTES_2.2.1.md](RELEASE_NOTES_2.2.1.md) as the release description template.
5. Upload **ONLY** the three files from release-artifacts. Do not upload the portable folder itself.
6. Use **Save draft** to check the files and release details. For 2.2.1, explicitly **mark as pre-release**, since real staging deployment and cross-PC validation remain pending. Then choose **Publish release** when the three generic assets are present and checked.

GitHub's automatically generated Source code (zip) is NOT the Windows program; use the separately uploaded assets.

## Historical GitHub CLI creation example (v2.2.1 already exists; do not execute)

The following command documents the original publishing procedure. **Do not run it again for v2.2.1**, which already exists:

```powershell
$sha = (git rev-parse HEAD).Trim()
gh release create v2.2.1 .\release-artifacts\CDM.exe .\release-artifacts\CDM-2.2.1-win-x64.zip .\release-artifacts\CDM-2.2.1-SHA256.txt --repo Cris0338/CDM-Coolify-Deploy-Manager --target $sha --title "CDM 2.2.1 - Windows portable (pre-release)" --notes-file .\RELEASE_NOTES_2.2.1.md --prerelease
```

Never use a wildcard such as `portable\*` or `release-artifacts\*` for uploads. Do not run this command twice without checking whether GitHub already created the tag/release. If `gh` is unavailable, use the GitHub web UI steps above.

## Published pre-release links

- Landing page: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/latest
- Latest stable single EXE: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/latest/download/CDM.exe
- Specific ZIP: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases/download/v2.2.1/CDM-2.2.1-win-x64.zip

The `/releases/latest` links only point to the latest **stable** release and will not resolve to a pre-release until a stable release exists; use the direct `/releases/tag/v2.2.1` page for the pre-release. Unsigned Windows EXEs may trigger a Windows SmartScreen reputation prompt. Future optional code signing can improve trust, but no certificate is configured now.

## Later improvements

Additional usability changes have been deferred by the author. Log concrete requests when decided rather than marking them implemented. No new features should be mixed into a tagged, verified version after the release package is built.
