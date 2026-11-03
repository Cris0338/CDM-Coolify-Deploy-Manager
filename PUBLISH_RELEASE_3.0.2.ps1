# Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
# Local Windows-only release staging/publishing; NEVER uses GitHub Actions.
# Without -Publish: builds, tests and stages the 3 verified generic-only assets.
# With -Publish: explicitly uploads the 3 approved assets as a public GitHub release.
param([switch]$Publish)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    $repo = 'Cris0338/CDM-Coolify-Deploy-Manager'
    $tag = 'v3.0.2'
    $project = Join-Path $root 'src\CoolifyDeployManager\CoolifyDeployManager.csproj'
    if ((Get-Content -LiteralPath $project -Raw) -notmatch '<Version>3\.0\.2</Version>') {
        throw 'This release script requires CDM project version 3.0.2.'
    }
    $branch = (& git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'main') {
        throw 'Use the public main branch (git switch main; git pull --ff-only) before staging v3.0.2.'
    }
    $changes = @(& git status --porcelain)
    if ($LASTEXITCODE -ne 0 -or $changes.Count -ne 0) {
        throw 'Git working tree must be clean before building an official release.'
    }
    $remote = (& git remote get-url origin).Trim()
    if ($LASTEXITCODE -ne 0 -or $remote -notmatch 'github\.com[:/]Cris0338/CDM-Coolify-Deploy-Manager(?:\.git)?$') {
        throw "Unexpected origin: $remote"
    }
    $sha = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $sha -notmatch '^[0-9a-f]{40}$') {
        throw 'Could not identify the exact release source commit.'
    }

    # Never tag a stale local commit or overwrite an existing public release.
    $remoteMain = (& git ls-remote origin refs/heads/main)
    if ($LASTEXITCODE -ne 0 -or -not $remoteMain) {
        throw 'Could not verify the public main branch on GitHub.'
    }
    $remoteSha = ($remoteMain -split '\s+')[0]
    if ($remoteSha -ne $sha) {
        throw 'Your local main does not match GitHub main. Run git pull --ff-only before publishing.'
    }
    $existingTag = @(& git ls-remote --tags origin "refs/tags/$tag")
    if ($LASTEXITCODE -ne 0) { throw 'Unable to verify existing remote release tag.' }
    if ($existingTag.Count -gt 0) {
        throw "Remote tag $tag already exists. Refusing to overwrite a published release."
    }
    if ($Publish) {
        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            throw 'GitHub CLI gh is required for -Publish. Install it or upload manually from GitHub Releases.'
        }
        & gh auth status *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'Authenticate the GitHub CLI first with gh auth login.'
        }
        # Do NOT call 'gh release view' here to test for absence:
        # on Windows PowerShell 5.1, 'release not found' emitted to stderr is
        # promoted to a terminating NativeCommandError because
        # ErrorActionPreference is Stop. The remote refs/tags guard above
        # already proves v3.0.2 is not yet tagged; gh release create
        # independently rejects duplicate tags/releases at publish time.
    }

    # This script rebuilds a FRESH generic with the CURRENT main SHA,
    # runs Windows smoke tests, and checks for a CDMPF01! personal trailer.
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'PACKAGE_RELEASE.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Release packaging failed. Nothing has been uploaded.' }

    $dir = Join-Path $root 'release-artifacts'
    $names = @('CDM-v3.0.2.exe','CDM-3.0.2-win-x64.zip','CDM-3.0.2-SHA256.txt')
    $assets = @($names | ForEach-Object { Join-Path $dir $_ })
    foreach ($asset in $assets) {
        if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) {
            throw "Required release asset missing: $asset"
        }
    }

    # Verify that the manifest matches these exact two files.
    $manifest = Get-Content -LiteralPath $assets[2] -Raw
    foreach ($asset in @($assets[0],$assets[1])) {
        $hash = (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash
        $name = [IO.Path]::GetFileName($asset)
        if (-not $manifest.Contains("$hash  $name")) {
            throw "Release SHA256 manifest mismatch: $name"
        }
    }

    # Inspect the ZIP member names; refuse other executables, secrets and backups.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($assets[1])
    try {
        $expected = @('CDM-v3.0.2.exe','LICENSE','README.md','README_IT.md',
            'SUPPORT.md','INSTALL.txt')
        $actual = @($zip.Entries | Where-Object { $_.Name } |
            ForEach-Object { $_.FullName })
        if ((($actual | Sort-Object) -join '|') -ne (($expected | Sort-Object) -join '|')) {
            throw "ZIP includes unexpected or missing files: $($actual -join ', ')"
        }
    }
    finally { $zip.Dispose() }

    Write-Host ''
    Write-Host "PASS: release assets are generic-only, SHA256 checked, source Git $sha."
    Write-Host 'Staged assets:' -ForegroundColor Green
    $assets | ForEach-Object { Write-Host "  $_" }

    if (-not $Publish) {
        Write-Host 'STAGED LOCALLY - NOT YET PUBLIC. To publish:'
        Write-Host '  powershell -NoProfile -ExecutionPolicy Bypass -File .\PUBLISH_RELEASE_3.0.2.ps1 -Publish'
        return
    }

    # Explicit publish: no wildcard, only the three vetted, credential-free files.
    $notes = Join-Path $root 'RELEASE_NOTES_3.0.2.md'
    if (-not (Test-Path -LiteralPath $notes)) { throw 'Release notes are missing.' }
    & gh release create $tag $assets[0] $assets[1] $assets[2] `
        --repo $repo --target $sha `
        --title 'CDM 3.0.2 - Windows portable' `
        --notes-file $notes --latest
    if ($LASTEXITCODE -ne 0) {
        throw 'GitHub release upload/publish failed. Inspect GitHub before retrying; a partial draft/release may exist.'
    }

    $releaseData = & gh release view $tag --repo $repo --json url,isDraft,isPrerelease,assets
    if ($LASTEXITCODE -ne 0) { throw 'Release created but could not verify its details.' }
    $release = $releaseData | ConvertFrom-Json
    if ($release.isDraft -or $release.isPrerelease) {
        throw 'Release exists, but is not publicly published as stable.'
    }
    $onlineNames = @($release.assets | ForEach-Object { $_.name })
    if ((($onlineNames | Sort-Object) -join '|') -ne (($names | Sort-Object) -join '|')) {
        throw "Published release asset list does not match the approved list: $($onlineNames -join ', ')"
    }
    Write-Host "PUBLISHED AND VERIFIED: $($release.url)" -ForegroundColor Green
}
finally { Pop-Location }
