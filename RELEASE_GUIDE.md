# CDM 3.0.2 — Manual release checklist

**STATUS: NOT PUBLISHED.**

1. Finish privacy review of source text, embedded binary and images, then manual UI validation.
2. Preserve private backup of the old repository; do not upload it. Recreate the GitHub repository with clean source files and a single new root commit. Never attach historical `.git`, bundles, snapshots or personal EXEs.
3. Confirm the new `main` commit is synchronized with `origin/main`; rebuild the release from that new commit (never reuse an EXE built against a temporary commit).
4. Run `PUBLISH_RELEASE_3.0.2.ps1` **without** `-Publish` for build, offline tests and packaging.
5. Inspect the package whitelist: `CDM-v3.0.2.exe`, `CDM-3.0.2-win-x64.zip`, `CDM-3.0.2-SHA256.txt`. Verify zip members and hashes. Check the EXE has NO embedded personal profile.
6. Only with the author's explicit authorization, use `PUBLISH_RELEASE_3.0.2.ps1 -Publish` to create the stable release from the exact public source commit. No GitHub Actions.
7. Verify public release tag, three assets and SHA256. Update README status and references in a separate subsequent commit/release if needed; never falsely report publication before it happens.

**Important:** GitHub deletion does not erase cached clones, mirrors, search indexes or external copies of the old repository. Keep historical backup private. Confirm the recreated repository does not automatically reconnect old branches/tags or old pull-request history.
