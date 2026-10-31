# CDM 2.2.1 - Post-package test and cleanup (2026-10-31)

The owner reported successful local Windows packaging of the generic version 2.2.1, including all build and offline checks. Exact source revision embedded in the resulting executable: `3ad03bb74cfbfbd84f19a04c677892b1c693662f`. Generic executable SHA256: `4B50975A82F6F542388B033DDB20B266C9A0F430FAF39BDC2423C56628E3915B`.

Local generic package files were created: `CDM.exe` (67689213 bytes), `CDM-2.2.1-win-x64.zip` (61812620 bytes) and `CDM-2.2.1-SHA256.txt` (164 bytes). None were uploaded to GitHub at this point.

After packaging, PowerShell reported deleting eight obsolete local personal executable backup files and preserving the newest backup by exclusion. No pending files were listed in the preceding inventory. A separate post-deletion inventory has not yet been performed. These private files were never committed to GitHub.

**Release provenance:** any v2.2.1 tag accompanying these exact compiled assets must point to the source revision embedded in the binary above, even when later documentation commits advance main. No further code changes are planned. The next step is public pre-release publication of only these three generic files; the author will field-test real staging deployments later.

## Repository landing page artwork

The original splash emblem was found on GitHub at `docs/images/CDMLogo.png`. Commit `8586dc86a6ee17d1da9001e460b0d88b076c9c5d` updated the top of `README.md` to display this image centered at 320px width with the descriptive alt text `Emblème CDM néon avec orbite cosmique`. The executable, release tag and downloaded assets were not changed. Only repository presentation was updated.
