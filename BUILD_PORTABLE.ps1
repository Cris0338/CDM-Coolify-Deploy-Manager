# Copyright (c) 2026 Cris0338. Licensed under the MIT License.
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
    Write-Warning 'Approved CDM logo not installed. Download CDM_Logo_Assets.zip into Downloads to embed the official artwork (current build uses a fallback).'
}
else {
    Write-Host 'PASS: official CDM transparent logo and icon available for embedding.'
}

Write-Host '== Classification tests (no deployments) =='
& $dotnet run --project $classifierTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Classification tests failed (exit $LASTEXITCODE)." }

Write-Host '== Localization tests =='
& $dotnet run --project $localeTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Localization tests failed (exit $LASTEXITCODE)." }

Write-Host '== Embedded executable profile tests =='
& $dotnet run --project $profileTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Executable profile tests failed (exit $LASTEXITCODE)." }

# Avoid rebuilding while the published template is running and its file is locked.
$exe = Join-Path $destination 'CDM.exe'
$running = @(Get-Process -Name CDM -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
if ($running.Count -gt 0) {
    throw "Close the running portable\CDM.exe before rebuilding (PID: $($running.Id -join ', '))."
}

Write-Host '== Publish portable WPF Windows x64 executable =='
& $dotnet publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' '-p:DebugType=None' '-p:DebugSymbols=false' -o $destination
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$published = Join-Path $destination 'CoolifyDeployManager.exe'
if (-not (Test-Path $published)) { throw "Executable not found: $published" }
# PowerShell 5.1 does not consistently overwrite EXEs via Move-Item -Force,
# and File.Replace can reject the path. Copy-Item works for a closed executable.
try {
    Copy-Item -LiteralPath $published -Destination $exe -Force -ErrorAction Stop
    $sourceHash = (Get-FileHash -LiteralPath $published -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) {
        throw "The copied CDM.exe does not match the freshly published executable."
    }
    Remove-Item -LiteralPath $published -Force -ErrorAction Stop
}
catch {
    throw "Cannot update portable\CDM.exe. Close CDM if it is running, then rerun BUILD_PORTABLE.ps1. Details: $($_.Exception.Message)"
}
$extra = @(Get-ChildItem $destination -File | Where-Object Name -ne 'CDM.exe')
if ($extra.Count -gt 0) { Write-Warning "Additional files in portable/: $($extra.Name -join ', ')" }
Write-Host '== Smoke test the published single-file EXE and a personalized clone =='
$smoke = Join-Path ([IO.Path]::GetTempPath()) ('cdm-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $smoke | Out-Null
try {
    # Regression check: a generic CDM.exe must never read previous-version settings
    # or connect automatically, even when the current Windows user has old data.
    $templateMarker = Join-Path $smoke 'template-clean.txt'
    $templateCheck = Start-Process -FilePath $exe -ArgumentList ('--verify-template-empty "' + $templateMarker + '"') -Wait -PassThru
    if ($templateCheck.ExitCode -ne 0 -or -not (Test-Path $templateMarker) -or (Get-Content $templateMarker -Raw).Trim() -ne 'CDM-TEMPLATE-CLEAN') {
        throw "Generic CDM.exe unexpectedly loaded saved credentials. $((Get-Content "$templateMarker.error" -ErrorAction SilentlyContinue) -join ' ')"
    }
    Write-Host 'PASS: generic CDM.exe starts without credentials, irrespective of older user settings.'
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
}
finally { Remove-Item -LiteralPath $smoke -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host '== Published executable =='
Get-Item $exe | Select-Object FullName, @{N='MB';E={[math]::Round($_.Length / 1MB, 2)}} | Format-List
Get-FileHash $exe -Algorithm SHA256 | Select-Object Hash, Path | Format-List
Write-Host 'Build completed. Test the UI, SSH connection and a staging deployment before publishing a release.'
