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

# The visible terminal is reserved for Coolify/SSH traffic and related
# operation diagnostics. UI-only activities must not write terminal lines.
$dashboardSource = Get-Content -LiteralPath (Join-Path $root 'src\CoolifyDeployManager\MainWindow.xaml.cs') -Raw
foreach ($uiOnlyLog in @(
    'AddLog(Loc.T("OrderSaved"))',
    'AddLog(Loc.T("LockedPositionPrevented"))',
    'AddLog(Loc.T("SettingsSaved"))',
    'AddLog(Loc.T("LocalQueueCancelled"))',
    'AddLog(Loc.T("StatusWaiting"))',
    'AddLog(Loc.T("LastStatusLog"))',
    'AddLog(Loc.F("DeploySkippedLocked"',
    'AddLog(BuildInfo.DiagnosticIdentity',
    'AddLog(Loc.F("UnclassifiedExplanation"'
)) {
    if ($dashboardSource.Contains($uiOnlyLog)) {
        throw "Internal UI event leaked into Coolify/SSH terminal: $uiOnlyLog"
    }
}
Write-Host 'PASS: card reordering, local locks and settings events do not pollute the communication terminal.'



# Self-contained source checkout: checked-in sanitized artwork, no private ZIP required.
# Both files originate from the approved CDM artwork; only the PNG caBX was removed.
$assetsFolder = Join-Path $root 'src\CoolifyDeployManager\Assets'
$logoFile = Join-Path $assetsFolder 'CDMLogo.png'
$iconFile = Join-Path $assetsFolder 'CDMOfficial.ico'
$expectedSanitizedLogoHash = '4D2A93308FF7641FB980A17DB0BEE54C0F7DE7F94ECDDBA7D7C520EE808B7A52'
$expectedIconHash = '1EB1E8CD4E078373E29813323BACC69DC4B8A4E9C801E6EDBD32E56440B54CD4'
if (-not (Test-Path -LiteralPath $logoFile -PathType Leaf) -or
    -not (Test-Path -LiteralPath $iconFile -PathType Leaf)) {
    throw 'CDM artwork missing from the source checkout.'
}
if ((Get-FileHash -LiteralPath $logoFile -Algorithm SHA256).Hash -cne $expectedSanitizedLogoHash -or
    (Get-FileHash -LiteralPath $iconFile -Algorithm SHA256).Hash -cne $expectedIconHash) {
    throw 'CDM artwork hash mismatch. Restore the exact approved source assets.'
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
    [IO.File]::Delete($backupFile)
    if ([IO.File]::Exists($backupFile)) {
        throw 'The temporary File.Replace backup was not removed.'
    }
    Write-Host 'PASS: Windows PowerShell File.Replace updated the file and removed its temporary backup.'
}
finally {
    Remove-Item -LiteralPath $replaceProof -Recurse -Force -ErrorAction SilentlyContinue
}

# Avoid rebuilding while the published template is running and its file is locked.
$projectVersion = [regex]::Match((Get-Content -LiteralPath $project -Raw),'<Version>(\d+\.\d+\.\d+)</Version>').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($projectVersion)) { throw 'Missing CDM project version.' }
$exeName = "CDM-v$projectVersion.exe"
$exe = Join-Path $destination $exeName
$running = @(Get-Process -Name 'CDM*' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
if ($running.Count -gt 0) {
    throw "Close the running portable\$exeName before rebuilding (PID: $($running.Id -join ', '))."
}

# Embed Git revision and UTC compile time into the executable's assembly.
$gitFullSha = (& git -C $root rev-parse --verify HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitFullSha -notmatch '^[0-9a-fA-F]{40}$') {
    throw 'Cannot determine Git commit for CDM build identity.'
}
$gitShortSha = $gitFullSha.Substring(0,12).ToLowerInvariant()
$buildUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
Write-Host "== Publish CDM $projectVersion (Git $gitShortSha, UTC $buildUtc) portable WPF Windows x64 =="
& $dotnet publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' '-p:DebugType=None' '-p:DebugSymbols=false' "-p:CDMSourceCommit=$gitShortSha" "-p:CDMBuildUtc=$buildUtc" -o $destination
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$published = Join-Path $destination 'CoolifyDeployManager.exe'
if (-not (Test-Path -LiteralPath $published)) { throw "Executable not found: $published" }

# Publish the verified generic executable. Copy and integrity validation are
# mandatory; deleting the duplicate SDK output is best-effort housekeeping.
# Windows Defender/indexers can temporarily lock the duplicate immediately
# after dotnet publish even when the intended versioned CDM has copied correctly.
try {
    Copy-Item -LiteralPath $published -Destination $exe -Force -ErrorAction Stop
    $sourceHash = (Get-FileHash -LiteralPath $published -Algorithm SHA256 -ErrorAction Stop).Hash
    $targetHash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256 -ErrorAction Stop).Hash
    if ($sourceHash -ne $targetHash) {
        throw "The copied $exeName does not match the freshly published executable."
    }
    Write-Host "PASS: generic $exeName matches freshly published output (SHA256 $targetHash)."
}
catch {
    throw "Cannot update or verify portable\\$exeName. Close CDM if it is running, then rerun BUILD_PORTABLE.ps1. Details: $($_.Exception.Message)"
}
try {
    Remove-Item -LiteralPath $published -Force -ErrorAction Stop
}
catch {
    # Nonfatal: the verified versioned generic is already in place. It is safe to
    # leave this duplicate for later manual cleanup if Windows is locking it.
    Write-Warning "Could not remove duplicate publish output '$published' (Windows file lock or permissions); build and smoke tests will continue. Details: $($_.Exception.Message)"
}
$extra = @(Get-ChildItem $destination -File | Where-Object Name -ne $exeName)
if ($extra.Count -gt 0) { Write-Warning "Additional files in portable/: $($extra.Name -join ', ')" }
Write-Host '== Smoke test the published single-file EXE and a personalized clone =='
$smoke = Join-Path ([IO.Path]::GetTempPath()) ('cdm-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $smoke | Out-Null
$smokeSucceeded = $false
try {
    # Version suffix is part of both the distributable EXE and future clones.
    # Verify the actual published filename matches its embedded assembly version.
    $nameMarker = Join-Path $smoke 'versioned-filenames.txt'
    $nameTest = Start-Process -FilePath $exe -ArgumentList ('--verify-versioned-executable-names "' + $nameMarker + '"') -Wait -PassThru
    if ($nameTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $nameMarker) -or
        (Get-Content -LiteralPath $nameMarker -Raw).Trim() -ne 'CDM-VERSIONED-FILENAMES-OK') {
        throw 'Published generic filename does not match embedded version or clone versioning regression.'
    }
    Write-Host 'PASS: generic and personal CDM filenames embed the same software version.'

    # Regression check: a generic versioned CDM.exe must never read previous-version settings
    # or connect automatically, even when the current Windows user has old data.
    $templateMarker = Join-Path $smoke 'template-clean.txt'
    $templateCheck = Start-Process -FilePath $exe -ArgumentList ('--verify-template-empty "' + $templateMarker + '"') -Wait -PassThru
    if ($templateCheck.ExitCode -ne 0 -or -not (Test-Path $templateMarker) -or (Get-Content $templateMarker -Raw).Trim() -ne 'CDM-TEMPLATE-CLEAN') {
        throw "Generic $exeName unexpectedly loaded saved credentials. $((Get-Content "$templateMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host "PASS: generic $exeName starts without credentials, irrespective of older user settings."

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

    # Coolify's existing deployment detail poll includes JSON build logs.
    # Check one-time incremental output, appended progress, legacy text and
    # suppression of hidden commands. No server or credential access.
    $deployLogMarker = Join-Path $smoke 'deployment-live-logs.txt'
    $deployLogTest = Start-Process -FilePath $exe -ArgumentList ('--verify-deployment-log-follower "' + $deployLogMarker + '"') -Wait -PassThru
    if ($deployLogTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $deployLogMarker) -or
        (Get-Content -LiteralPath $deployLogMarker -Raw).Trim() -ne 'CDM-DEPLOYMENT-LOG-FOLLOWER-OK') {
        throw "Coolify deployment log incremental parsing smoke failed. $((Get-Content "$deployLogMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: Coolify deployment logs show new build lines only, excluding hidden commands and duplicates.'

    # Actual WPF templates must survive card reorder while an indicator
    # is active. The earlier view-model-only test missed a fatal frozen
    # Freezable/Storyboard exception in Window.xaml on template recreation.
    $wpfMarker = Join-Path $smoke 'wpf-spinner-retemplate.txt'
    $wpfTest = Start-Process -FilePath $exe -ArgumentList ('--verify-live-spinner-retemplate "' + $wpfMarker + '"') -PassThru
    try {
        if (-not $wpfTest.WaitForExit(30000)) {
            try { $wpfTest.Kill() } catch { }
            throw 'WPF live spinner retemplate regression timed out after 30 seconds.'
        }
        if ($wpfTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $wpfMarker) -or
            (Get-Content -LiteralPath $wpfMarker -Raw).Trim() -ne 'CDM-WPF-SPINNER-RETEMPLATE-OK') {
            throw "WPF live spinner retemplate regression failed. $((Get-Content "$wpfMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
        }
    }
    finally { $wpfTest.Dispose() }
    Write-Host 'PASS: real WPF card retemplate during live spinner does not crash or freeze transforms.'

    # The WinForms tray icon must hide and restore the real WPF dashboard.
    # This test performs NO writes to HKCU Run and makes NO SSH/API calls.
    $trayMarker = Join-Path $smoke 'notification-area-ui.txt'
    $trayTest = Start-Process -FilePath $exe -ArgumentList ('--verify-tray-ui "' + $trayMarker + '"') -PassThru
    try {
        if (-not $trayTest.WaitForExit(30000)) {
            try { $trayTest.Kill() } catch { }
            throw 'Windows tray UI smoke timed out after 30 seconds.'
        }
        if ($trayTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $trayMarker) -or
            (Get-Content -LiteralPath $trayMarker -Raw).Trim() -ne 'CDM-TRAY-UI-OK') {
            throw "Windows tray UI smoke failed. $((Get-Content "$trayMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
        }
    }
    finally { $trayTest.Dispose() }
    Write-Host 'PASS: Windows tray icon hides/restores WPF, validates user autostart command without touching registry.'

    # Real WPF taskbar overlay and Windows notification icon must animate
    # from Coolify-confirmed state, survive minimization and multi-deploy, then
    # return to the exact original icon. This makes no SSH/API/registry calls.
    $shellMarker = Join-Path $smoke 'shell-deploy-indicator.txt'
    $shellTest = Start-Process -FilePath $exe -ArgumentList ('--verify-deploy-shell-indicator "' + $shellMarker + '"') -PassThru
    try {
        if (-not $shellTest.WaitForExit(30000)) {
            try { $shellTest.Kill() } catch { }
            throw 'Windows deploy shell indicator smoke timed out after 30 seconds.'
        }
        if ($shellTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $shellMarker) -or
            (Get-Content -LiteralPath $shellMarker -Raw).Trim() -ne 'CDM-SHELL-INDICATOR-OK') {
            throw "Deploy shell indicator WPF/tray smoke failed. $((Get-Content "$shellMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
        }
    }
    finally { $shellTest.Dispose() }
    Write-Host 'PASS: taskbar and tray deploy rings animate with actual server states, even when minimized.'

    # Verify geometry survives settings JSON and that minimizing into the tray
    # retains the last normal/maximized presentation. No settings are written.
    $placementMarker = Join-Path $smoke 'window-placement.txt'
    $placementTest = Start-Process -FilePath $exe -ArgumentList ('--verify-window-placement "' + $placementMarker + '"') -PassThru
    try {
        if (-not $placementTest.WaitForExit(30000)) {
            try { $placementTest.Kill() } catch { }
            throw 'Windows placement WPF regression timed out after 30 seconds.'
        }
        if ($placementTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $placementMarker) -or
            (Get-Content -LiteralPath $placementMarker -Raw).Trim() -ne 'CDM-WINDOW-PLACEMENT-OK') {
            throw "Window placement/maximize/tray smoke failed. $((Get-Content "$placementMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
        }
    }
    finally { $placementTest.Dispose() }
    Write-Host 'PASS: window geometry JSON and normal/maximized presentation survive minimize-to-tray/restore.'

    # Status update is independent of the deployment's "finished" event.
    # This offline test exercises WPF view-model property notifications only.
    $healthMarker = Join-Path $smoke 'health-updated.txt'
    $healthTest = Start-Process -FilePath $exe -ArgumentList ('--verify-health-viewmodel "' + $healthMarker + '"') -Wait -PassThru
    if ($healthTest.ExitCode -ne 0 -or -not (Test-Path $healthMarker) -or
        (Get-Content $healthMarker -Raw).Trim() -ne 'CDM-HEALTH-UPDATED') {
        throw "Read-only app health view-model regression failed. $((Get-Content "$healthMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: card health changes from unhealthy to healthy without overwriting finished deployment history.'

    # New v3 provisioning policy smoke uses embedded bash resource; no SSH/VPS access.
    $v3Marker = Join-Path $smoke 'v3-ssh-bootstrap.txt'
    $v3Test = Start-Process -FilePath $exe -ArgumentList ('--verify-v3-ssh-bootstrap "' + $v3Marker + '"') -Wait -PassThru
    if ($v3Test.ExitCode -ne 0 -or -not (Test-Path $v3Marker) -or
        (Get-Content $v3Marker -Raw).Trim() -ne 'CDM-V3-SSH-BOOTSTRAP-OK') {
        throw 'CDM v3 restricted SSH bootstrap policy regression.'
    }
    Write-Host 'PASS: v3 SSH bootstrap policy and input validation (offline, no VPS changes).'

    # WPF visual feedback for Copy VPS Command, without touching the clipboard.
    $copyMarker = Join-Path $smoke 'v3-manual-copy-feedback.txt'
    $copyTest = Start-Process -FilePath $exe -ArgumentList ('--verify-manual-copy-feedback "' + $copyMarker + '"') -Wait -PassThru
    if ($copyTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $copyMarker) -or
        (Get-Content -LiteralPath $copyMarker -Raw).Trim() -ne 'CDM-MANUAL-COPY-FEEDBACK-OK') {
        throw "CDM v3 Copy VPS Command animation/label feedback smoke failed. $((Get-Content "$copyMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: Copy VPS Command button animates and shows copied feedback, then resets (WPF smoke).'

    # First-run WPF modal wizard and guide must BOTH accept user input.
    $guideMarker = Join-Path $smoke 'v3-first-run-guide.txt'
    $guideTest = Start-Process -FilePath $exe -ArgumentList ('--verify-first-run-guide "' + $guideMarker + '"') -Wait -PassThru
    if ($guideTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $guideMarker) -or
        (Get-Content -LiteralPath $guideMarker -Raw).Trim() -ne 'CDM-FIRST-RUN-GUIDE-OK') {
        throw "First-run WPF wizard/guide side-by-side smoke failed. $((Get-Content "$guideMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: setup and companion guide are independently interactive in BOTH generic and configured personal CDM (WPF smoke).'

    # Selection is scoped to the personal EXE. No server calls or real credentials.
    $projectMarker = Join-Path $smoke 'project-selection.txt'
    $projectTest = Start-Process -FilePath $exe -ArgumentList ('--verify-project-selection-persistence "' + $projectMarker + '"') -Wait -PassThru
    if ($projectTest.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $projectMarker) -or
        (Get-Content -LiteralPath $projectMarker -Raw).Trim() -ne 'CDM-PROJECT-SELECTION-OK') {
        throw 'Last selected project restoration / deletion fallback / JSON persistence regression.'
    }
    Write-Host 'PASS: last selected project restored by UUID, missing project falls back to All, profile persistence verified.'

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


    # v3 always starts from a NEW identity: no old-EXE import or upgrade.
    $clone = Join-Path $smoke "CDM-Smoke-Test-v$projectVersion.exe"
    $marker = Join-Path $smoke 'clone-started.txt'
    $create = Start-Process -FilePath $exe -ArgumentList ('--emit-self-test-clone "' + $clone + '"') -Wait -PassThru
    if ($create.ExitCode -ne 0 -or -not (Test-Path $clone)) {
        throw "The v3 template did not create a new synthetic personal clone."
    }
    $verify = Start-Process -FilePath $clone -ArgumentList ('--verify-self-test-clone "' + $marker + '"') -Wait -PassThru
    if ($verify.ExitCode -ne 0 -or -not (Test-Path $marker) -or (Get-Content $marker -Raw).Trim() -ne 'CDM-CLONE-STARTED') {
        throw "New personalized v3 clone failed to read its embedded profile."
    }
    Write-Host 'PASS: v3 personal clone created independently from scratch.'
    # Personal v3->v3 refresh changes only the software binary and preserves
    # the existing v3 embedded profile. The previous v2->v3 migration remains forbidden.
    $v3ProfileBefore = Join-Path $smoke 'v3-refresh-before.txt'
    $v3ProfileAfter = Join-Path $smoke 'v3-refresh-after.txt'
    $beforeProfile = Start-Process -FilePath $clone -ArgumentList ('--verify-self-test-clone "' + $v3ProfileBefore + '"') -Wait -PassThru
    if ($beforeProfile.ExitCode -ne 0) { throw 'Disposable personal v3 clone failed before refresh.' }
    $refresh = Start-Process -FilePath $exe -ArgumentList ('--refresh-v3-clone-silent "' + $clone + '"') -Wait -PassThru
    if ($refresh.ExitCode -ne 0) {
        throw "CDM v3 failed to refresh an existing v3 clone: $((Get-Content "$clone.refresh-error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    $afterProfile = Start-Process -FilePath $clone -ArgumentList ('--verify-self-test-clone "' + $v3ProfileAfter + '"') -Wait -PassThru
    if ($afterProfile.ExitCode -ne 0 -or
        (Get-Content $v3ProfileAfter -Raw).Trim() -ne 'CDM-CLONE-STARTED') {
        throw 'Refreshed v3 clone cannot read its original embedded profile.'
    }
    $oldVersionName = Join-Path $smoke 'CDM-Smoke-Test-v2.2.3.exe'
    Copy-Item -LiteralPath $clone -Destination $oldVersionName -Force
    $oldHash = (Get-FileHash -LiteralPath $oldVersionName -Algorithm SHA256).Hash
    $rejected = Start-Process -FilePath $exe -ArgumentList ('--refresh-v3-clone-silent "' + $oldVersionName + '"') -Wait -PassThru
    if ($rejected.ExitCode -eq 0 -or
        (Get-FileHash -LiteralPath $oldVersionName -Algorithm SHA256).Hash -ne $oldHash) {
        throw 'CDM v3 must never import a 2.x personal executable, even when it contains valid test profile.'
    }
    Remove-Item -LiteralPath $oldVersionName -Force
    Remove-Item -LiteralPath "$oldVersionName.refresh-error" -Force -ErrorAction SilentlyContinue
    $refreshBackup = Get-ChildItem -LiteralPath $smoke -Filter '*.pre-refresh.bak' -ErrorAction SilentlyContinue
    if ($refreshBackup) { throw 'Successful v3 refresh retained unnecessary credential-bearing backup.' }
    Write-Host 'PASS: v3 personal EXE refreshed in place with its profile intact; v2 import rejected, no backup left.'

    $cloneHash = (Get-FileHash -LiteralPath $clone -Algorithm SHA256).Hash
    $upgrade = Start-Process -FilePath $exe -ArgumentList ('--upgrade-clone-silent "' + $clone + '"') -Wait -PassThru
    if ($upgrade.ExitCode -eq 0 -or (Get-FileHash -LiteralPath $clone -Algorithm SHA256).Hash -ne $cloneHash) {
        throw 'CDM v3 must reject clone upgrade without touching the original file.'
    }
    Write-Host 'PASS: CDM v3 refuses old clone migration and preserves existing file.'
    $cloneBuildMarker = Join-Path $smoke 'clone-build-info.txt'
    $cloneBuildCheck = Start-Process -FilePath $clone -ArgumentList ('--verify-build-info "' + $cloneBuildMarker + '"') -Wait -PassThru
    if ($cloneBuildCheck.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $cloneBuildMarker) -or
        (Get-Content -LiteralPath $cloneBuildMarker -Raw).Trim() -ne $expectedBuildInfo) {
        throw "Fresh v3 clone does not contain expected version and source identity."
    }
    Write-Host 'PASS: new personal clone preserves v3 version, Git commit and build UTC.'

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
    $pending = Join-Path $smoke ('.' + [IO.Path]::GetFileName($clone) + '.pending')
    $updaterError = "$pending.updater-error.txt"
    $updaterStarted = "$pending.updater-started.txt"
    $previousBackup = "$clone.before-save.bak"
    $deadline = [DateTime]::UtcNow.AddSeconds(65)
    while (((Test-Path -LiteralPath $pending) -or
            (Test-Path -LiteralPath $updaterStarted) -or
            (Test-Path -LiteralPath $previousBackup)) -and
            [DateTime]::UtcNow -lt $deadline) {
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
    if ((Test-Path -LiteralPath $previousBackup) -or
        (Test-Path -LiteralPath $updaterStarted) -or
        (Test-Path -LiteralPath $updaterError)) {
        throw 'Detached updater left a backup or updater marker after successful profile save.'
    }
    $cloneHashAfterSave = (Get-FileHash -LiteralPath $clone -Algorithm SHA256).Hash
    if ($cloneHashAfterSave -eq $cloneHashBeforeSave) {
        throw 'The personal clone executable did not change after profile save.'
    }
    Write-Host 'PASS: close-time profile save applied without retaining a .before-save.bak or updater marker.'
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
