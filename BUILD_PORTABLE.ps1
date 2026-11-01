# Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
# Build and test the standalone Windows x64 Coolify Deploy Manager executable.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\CoolifyDeployManager\CoolifyDeployManager.csproj'
$classifierTests = Join-Path $root 'tests\ClassifierTests\ClassifierTests.csproj'
$localeTests = Join-Path $root 'tests\LocalizationTests\LocalizationTests.csproj'
$profileTests = Join-Path $root 'tests\ExecutableProfileTests\ExecutableProfileTests.csproj'
$destination = Join-Path $root 'portable'
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { throw 'The 64-bit .NET 10 SDK was not found under Program Files.' }


# One-time import of the user-approved transparent CDM logo and derived icon.
# Download CDM_Logo_Assets.zip to the regular Windows Downloads folder;
# the ZIP is intentionally kept outside the repository's source-only commits.
$assetsFolder = Join-Path $root 'src\CoolifyDeployManager\Assets'
$logoZip = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads\CDM_Logo_Assets.zip'
$expectedZipHash = 'FC68EBFB2DD2B1FBEB913D95CE663520B57F458BD082D25FFF9C2695631FBAF7'
$expectedLogoHash = '9087F9D3360DADD2D76572799492E439DE06E24ADBA70D81846C98EC2CBD707F'
$expectedIconHash = '1EB1E8CD4E078373E29813323BACC69DC4B8A4E9C801E6EDBD32E56440B54CD4'
$logoFile = Join-Path $assetsFolder 'CDMLogo.png'
$iconFile = Join-Path $assetsFolder 'CDMOfficial.ico'
if (Test-Path -LiteralPath $logoZip) {
    if ((Get-FileHash -LiteralPath $logoZip -Algorithm SHA256).Hash -ne $expectedZipHash) {
        throw 'CDM_Logo_Assets.zip has an unexpected SHA256. Use the approved original ZIP.'
    }
    $stage = Join-Path ([IO.Path]::GetTempPath()) ('cdm-art-' + [guid]::NewGuid().ToString('N'))
    try {
        Expand-Archive -LiteralPath $logoZip -DestinationPath $stage -Force
        foreach ($pair in @(
            @{ File = 'CDMLogo.png'; Hash = $expectedLogoHash },
            @{ File = 'CDMOfficial.ico'; Hash = $expectedIconHash }
        )) {
            $source = Join-Path $stage $pair.File
            if (-not (Test-Path -LiteralPath $source) -or
                (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $pair.Hash) {
                throw "Approved asset '$($pair.File)' missing or changed."
            }
            Copy-Item -LiteralPath $source -Destination (Join-Path $assetsFolder $pair.File) -Force
        }
    }
    finally { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
}
if (-not (Test-Path -LiteralPath $logoFile) -or -not (Test-Path -LiteralPath $iconFile)) {
    throw 'Official CDM artwork is missing. Download CDM_Logo_Assets.zip into Windows Downloads before building.'
}
if ((Get-FileHash -LiteralPath $logoFile -Algorithm SHA256).Hash -ne $expectedLogoHash -or
    (Get-FileHash -LiteralPath $iconFile -Algorithm SHA256).Hash -ne $expectedIconHash) {
    throw 'Official CDM artwork has a different SHA256. Restore the approved assets from CDM_Logo_Assets.zip.'
}
Write-Host 'PASS: official CDM logo AND Windows executable icon verified (SHA256).'

Write-Host '== Classification tests (no deployments) =='
& $dotnet run --project $classifierTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Classification tests failed (exit $LASTEXITCODE)." }

Write-Host '== Localization tests =='
& $dotnet run --project $localeTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Localization tests failed (exit $LASTEXITCODE)." }

Write-Host '== Embedded executable profile tests =='
& $dotnet run --project $profileTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Executable profile tests failed (exit $LASTEXITCODE)." }

# Isolated Windows PowerShell/.NET Framework compatibility check:
# File.Replace must receive a real backup path, not $null (which PowerShell
# can coerce to an invalid empty string). Test ordinary file bytes first,
# before running any executable-based save tests or touching personal clones.
Write-Host '== PowerShell File.Replace compatibility (dummy files only) =='
$replaceProof = Join-Path ([IO.Path]::GetTempPath()) ('CDM replace proof ' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $replaceProof -Force | Out-Null
try {
    $oldFile = Join-Path $replaceProof 'CDM old.txt'
    $newFile = Join-Path $replaceProof 'CDM new.txt'
    $backupFile = Join-Path $replaceProof 'CDM previous.bak'
    [IO.File]::WriteAllText($oldFile, 'ORIGINAL')
    [IO.File]::WriteAllText($newFile, 'UPDATED')
    [IO.File]::Replace($newFile, $oldFile, $backupFile)
    if ([IO.File]::ReadAllText($oldFile) -ne 'UPDATED' -or
        [IO.File]::ReadAllText($backupFile) -ne 'ORIGINAL' -or
        [IO.File]::Exists($newFile)) {
        throw 'File.Replace produced incorrect destination, backup, or staging cleanup.'
    }
    Write-Host 'PASS: Windows PowerShell File.Replace updated a file and preserved the original in a valid backup.'
}
finally {
    Remove-Item -LiteralPath $replaceProof -Recurse -Force -ErrorAction SilentlyContinue
}

# Avoid rebuilding while the published template is running and its file is locked.
$exe = Join-Path $destination 'CDM.exe'
$running = @(Get-Process -Name CDM -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
if ($running.Count -gt 0) {
    throw "Close the running portable\CDM.exe before rebuilding (PID: $($running.Id -join ', '))."
}

# Embed Git revision and UTC compile time into the executable's assembly.
$gitFullSha = (& git -C $root rev-parse --verify HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitFullSha -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'Cannot determine Git commit for CDM build identity.'
}
$gitShortSha = $gitFullSha.Substring(0,12).ToLowerInvariant()
$buildUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
$projectVersion = [regex]::Match((Get-Content -LiteralPath $project -Raw),'<Version>(\d+\.\d+\.\d+)</Version>').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($projectVersion)) { throw 'Missing CDM project version.' }
Write-Host "== Publish CDM $projectVersion (Git $gitShortSha, UTC $buildUtc) portable WPF Windows x64 =="
& $dotnet publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' '-p:DebugType=None' '-p:DebugSymbols=false' "-p:CDMSourceCommit=$gitShortSha" "-p:CDMBuildUtc=$buildUtc" -o $destination
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$published = Join-Path $destination 'CoolifyDeployManager.exe'
if (-not (Test-Path -LiteralPath $published)) { throw "Executable not found: $published" }

# Publish the verified generic executable. Copy and integrity validation are
# mandatory; deleting the duplicate SDK output is best-effort housekeeping.
# Windows Defender/indexers can temporarily lock the duplicate immediately
# after dotnet publish even when the intended CDM.exe has copied correctly.
try {
    Copy-Item -LiteralPath $published -Destination $exe -Force -ErrorAction Stop
    $sourceHash = (Get-FileHash -LiteralPath $published -Algorithm SHA256 -ErrorAction Stop).Hash
    $targetHash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256 -ErrorAction Stop).Hash
    if ($sourceHash -ne $targetHash) {
        throw "The copied CDM.exe does not match the freshly published executable."
    }
    Write-Host "PASS: generic CDM.exe matches freshly published output (SHA256 $targetHash)."
}
catch {
    throw "Cannot update or verify portable\\CDM.exe. Close CDM if it is running, then rerun BUILD_PORTABLE.ps1. Details: $($_.Exception.Message)"
}
try {
    Remove-Item -LiteralPath $published -Force -ErrorAction Stop
}
catch {
    # Nonfatal: the verified CDM.exe is already in place. It is safe to
    # leave this duplicate for later manual cleanup if Windows is locking it.
    Write-Warning "Could not remove duplicate publish output '$published' (Windows file lock or permissions); build and smoke tests will continue. Details: $($_.Exception.Message)"
}
$extra = @(Get-ChildItem $destination -File | Where-Object Name -ne 'CDM.exe')
if ($extra.Count -gt 0) { Write-Warning "Additional files in portable/: $($extra.Name -join ', ')" }
Write-Host '== Smoke test the published single-file EXE and a personalized clone =='
$smoke = Join-Path ([IO.Path]::GetTempPath()) ('cdm-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $smoke | Out-Null
$smokeSucceeded = $false
try {
    # Regression check: a generic CDM.exe must never read previous-version settings
    # or connect automatically, even when the current Windows user has old data.
    $templateMarker = Join-Path $smoke 'template-clean.txt'
    $templateCheck = Start-Process -FilePath $exe -ArgumentList ('--verify-template-empty "' + $templateMarker + '"') -Wait -PassThru
    if ($templateCheck.ExitCode -ne 0 -or -not (Test-Path $templateMarker) -or (Get-Content $templateMarker -Raw).Trim() -ne 'CDM-TEMPLATE-CLEAN') {
        throw "Generic CDM.exe unexpectedly loaded saved credentials. $((Get-Content "$templateMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: generic CDM.exe starts without credentials, irrespective of older user settings.'

    # Compare version, commit and timestamp actually read from the published EXE.
    $buildMarker = Join-Path $smoke 'build-info.txt'
    $buildCheck = Start-Process -FilePath $exe -ArgumentList ('--verify-build-info "' + $buildMarker + '"') -Wait -PassThru
    $expectedBuildInfo = "$projectVersion|$gitShortSha|$buildUtc"
    if ($buildCheck.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $buildMarker) -or
        (Get-Content -LiteralPath $buildMarker -Raw).Trim() -ne $expectedBuildInfo) {
        throw "CDM build identity mismatch (expected $expectedBuildInfo). $((Get-Content "$buildMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host "PASS: CDM $projectVersion Git $gitShortSha UTC $buildUtc embedded in the published executable."

    # UI-only regression: a Coolify-confirmed job animates its own card,
    # terminal/unknown states stop, while card drag remains enabled during
    # network deployment and is blocked during inventory refresh.
    $activityMarker = Join-Path $smoke 'live-deploy-ui.txt'
    $activityCheck = Start-Process -FilePath $exe -ArgumentList ('--verify-live-deploy-ui "' + $activityMarker + '"') -Wait -PassThru
    if ($activityCheck.ExitCode -ne 0 -or -not (Test-Path $activityMarker) -or
        (Get-Content $activityMarker -Raw).Trim() -ne 'CDM-LIVE-DEPLOY-UI-OK') {
        throw "Coolify live deployment card UI smoke failed. $((Get-Content "$activityMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: per-card Coolify-driven spinner state and card drag policy during network deployment.'

    # Status update is independent of the deployment's "finished" event.
    # This offline test exercises WPF view-model property notifications only.
    $healthMarker = Join-Path $smoke 'health-updated.txt'
    $healthTest = Start-Process -FilePath $exe -ArgumentList ('--verify-health-viewmodel "' + $healthMarker + '"') -Wait -PassThru
    if ($healthTest.ExitCode -ne 0 -or -not (Test-Path $healthMarker) -or
        (Get-Content $healthMarker -Raw).Trim() -ne 'CDM-HEALTH-UPDATED') {
        throw "Read-only app health view-model regression failed. $((Get-Content "$healthMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: card health changes from unhealthy to healthy without overwriting finished deployment history.'

    # SSH recovery must trigger for a stopped, configured managed tunnel,
    # but never during a deployment, another recovery, closing, HTTPS
    # or the user's externally managed local tunnel. No real ssh.exe needed.
    $tunnelMarker = Join-Path $smoke 'tunnel-policy.txt'
    $tunnelTest = Start-Process -FilePath $exe -ArgumentList ('--verify-tunnel-recovery-policy "' + $tunnelMarker + '"') -Wait -PassThru
    if ($tunnelTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $tunnelMarker) -or
        (Get-Content -LiteralPath $tunnelMarker -Raw).Trim() -ne 'CDM-TUNNEL-RECOVERY-POLICY-OK') {
        throw "Managed SSH auto-reconnect policy regression failed. $((Get-Content "$tunnelMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: expired managed SSH tunnel will auto-recover; busy, active, closing and other modes are protected.'

    $clone = Join-Path $smoke 'CDM-Smoke-Test.exe'
    $marker = Join-Path $smoke 'clone-started.txt'
    $create = Start-Process -FilePath $exe -ArgumentList ('--emit-self-test-clone "' + $clone + '"') -Wait -PassThru
    if ($create.ExitCode -ne 0 -or -not (Test-Path $clone)) {
        throw "The published template could not generate a test clone. $((Get-Content "$clone.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    $verify = Start-Process -FilePath $clone -ArgumentList ('--verify-self-test-clone "' + $marker + '"') -Wait -PassThru
    if ($verify.ExitCode -ne 0 -or -not (Test-Path $marker) -or (Get-Content $marker -Raw).Trim() -ne 'CDM-CLONE-STARTED') {
        throw "The published personalized EXE failed to start or read its embedded profile. $((Get-Content "$marker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: personalized single-file clone launched and read its embedded profile.'
    # Explicit template-to-clone upgrade must preserve embedded credentials and
    # keep a recoverable backup (no real user profile involved).
    $upgrade = Start-Process -FilePath $exe -ArgumentList ('--upgrade-clone-silent "' + $clone + '"') -Wait -PassThru
    if ($upgrade.ExitCode -ne 0) {
        throw "Personalized clone upgrade failed. $((Get-Content "$clone.upgrade-error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    $verifyAgain = Start-Process -FilePath $clone -ArgumentList ('--verify-self-test-clone "' + $marker + '"') -Wait -PassThru
    if ($verifyAgain.ExitCode -ne 0 -or (Get-Content $marker -Raw).Trim() -ne 'CDM-CLONE-STARTED') {
        throw "Upgraded clone failed to read its preserved embedded profile."
    }
    Write-Host 'PASS: clone upgraded from the new template without losing its embedded profile.'
    # Personal clones must expose the SAME build identity as the template.
    # This is the metadata the About dialog will show on the owner's EXE.
    $cloneBuildMarker = Join-Path $smoke 'clone-build-info.txt'
    $cloneBuildCheck = Start-Process -FilePath $clone -ArgumentList ('--verify-build-info "' + $cloneBuildMarker + '"') -Wait -PassThru
    if ($cloneBuildCheck.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $cloneBuildMarker) -or
        (Get-Content -LiteralPath $cloneBuildMarker -Raw).Trim() -ne $expectedBuildInfo) {
        throw "Personal clone does not contain its original build identity. $((Get-Content "$cloneBuildMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: upgraded personalized clone preserves compiled CDM version, Git commit and UTC build date.'

    # Exercise the real detached save helper on a disposable personal EXE.
    # No network, real SSH keys, tokens or user's own clone are involved.
    $cloneHashBeforeSave = (Get-FileHash -LiteralPath $clone -Algorithm SHA256).Hash
    $savedMarker = Join-Path $smoke 'pending-staged.txt'
    # Start-Process -Wait on Windows waits for the entire descendant process
    # tree, not just the clone. The clone deliberately spawns a detached
    # powershell.exe helper to update its EXE after exit. Waiting for that
    # process tree here can stall the test before it checks the .pending file.
    # .NET Process.WaitForExit waits for only this disposable clone's PID.
    $saveStart = New-Object System.Diagnostics.ProcessStartInfo
    $saveStart.FileName = $clone
    $saveStart.Arguments = '--save-self-test-clone "' + $savedMarker + '"'
    $saveStart.UseShellExecute = $false
    $saveStart.CreateNoWindow = $true
    $saveProcess = [System.Diagnostics.Process]::Start($saveStart)
    if ($null -eq $saveProcess) { throw 'Could not launch disposable clone save smoke test.' }
    try {
        if (-not $saveProcess.WaitForExit(20000)) {
            try { $saveProcess.Kill() } catch { }
            throw 'Disposable clone save did not exit within 20 seconds.'
        }
        if ($saveProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $savedMarker) -or
            (Get-Content -LiteralPath $savedMarker -Raw).Trim() -ne 'CDM-PENDING-STAGED') {
            throw "Disposable clone could not stage an embedded profile change. $((Get-Content "$savedMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
        }
    }
    finally { $saveProcess.Dispose() }
    Write-Host 'PASS: disposable clone staged a profile update and exited; checking detached helper.'
    $pending = Join-Path $smoke '.CDM-Smoke-Test.exe.pending'
    $updaterError = "$pending.updater-error.txt"
    $updaterStarted = "$pending.updater-started.txt"
    $deadline = [DateTime]::UtcNow.AddSeconds(65)
    while ((Test-Path -LiteralPath $pending) -and [DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $updaterError) {
            throw "Detached updater failed. $((Get-Content -LiteralPath $updaterError -ErrorAction SilentlyContinue) -join ' ')"
        }
        Start-Sleep -Milliseconds 250
    }
    if (Test-Path -LiteralPath $pending) {
        $started = Test-Path -LiteralPath $updaterStarted
        $errorDetail = (Get-Content -LiteralPath $updaterError -ErrorAction SilentlyContinue) -join ' '
        if ([string]::IsNullOrWhiteSpace($errorDetail)) {
            $errorDetail = 'No helper error file was produced.'
        }
        throw "Detached updater did not apply disposable profile. Helper started=$started. $errorDetail"
    }
    $previousBackup = "$clone.before-save.bak"
    if (-not (Test-Path -LiteralPath $previousBackup) -or
        (Get-FileHash -LiteralPath $previousBackup -Algorithm SHA256).Hash -ne $cloneHashBeforeSave) {
        throw 'The detached helper did not preserve the previous personalized clone in its last-good backup.'
    }
    Write-Host 'PASS: close-time save preserved the exact previous disposable clone in its last-good backup.'
    $updatedMarker = Join-Path $smoke 'pending-applied.txt'
    $verifySaved = Start-Process -FilePath $clone -ArgumentList ('--verify-saved-self-test-clone "' + $updatedMarker + '"') -Wait -PassThru
    if ($verifySaved.ExitCode -ne 0 -or -not (Test-Path $updatedMarker) -or
        (Get-Content $updatedMarker -Raw).Trim() -ne 'CDM-PENDING-APPLIED') {
        throw "Staged profile was not preserved in the updated executable. $((Get-Content "$updatedMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: close-time .pending applied to disposable clone; token, card order and position locks preserved.'
    $smokeSucceeded = $true
}
finally {
    if ($smokeSucceeded) {
        Remove-Item -LiteralPath $smoke -Recurse -Force -ErrorAction SilentlyContinue
    }
    else {
        # The directory contains only synthetic, explicitly invalid test
        # credentials. Keep it on failure so the helper's started/error
        # markers remain available for diagnosis instead of losing evidence.
        Write-Warning "Disposable smoke evidence retained: $smoke"
    }
}

Write-Host '== Published executable =='
Get-Item $exe | Select-Object FullName, @{N='MB';E={[math]::Round($_.Length / 1MB, 2)}} | Format-List
Get-FileHash $exe -Algorithm SHA256 | Select-Object Hash, Path | Format-List
Write-Host 'Build completed. Test the UI, SSH connection and a staging deployment before publishing a release.'
