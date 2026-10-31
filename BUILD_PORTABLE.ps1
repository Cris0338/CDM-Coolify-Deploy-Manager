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

Write-Host '== Classification tests (no deployments) =='
& $dotnet run --project $classifierTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Classification tests failed (exit $LASTEXITCODE)." }

Write-Host '== Localization tests =='
& $dotnet run --project $localeTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Localization tests failed (exit $LASTEXITCODE)." }

Write-Host '== Embedded executable profile tests =='
& $dotnet run --project $profileTests -c Release
if ($LASTEXITCODE -ne 0) { throw "Executable profile tests failed (exit $LASTEXITCODE)." }

Write-Host '== Publish portable WPF Windows x64 executable =='
& $dotnet publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' '-p:DebugType=None' '-p:DebugSymbols=false' -o $destination
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$published = Join-Path $destination 'CoolifyDeployManager.exe'
$exe = Join-Path $destination 'CDM.exe'
if (-not (Test-Path $published)) { throw "Executable not found: $published" }
Move-Item -LiteralPath $published -Destination $exe -Force
$extra = @(Get-ChildItem $destination -File | Where-Object Name -ne 'CDM.exe')
if ($extra.Count -gt 0) { Write-Warning "Additional files in portable/: $($extra.Name -join ', ')" }
Write-Host '== Smoke test the published single-file EXE and a personalized clone =='
$smoke = Join-Path ([IO.Path]::GetTempPath()) ('cdm-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $smoke | Out-Null
try {
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
}
finally { Remove-Item -LiteralPath $smoke -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host '== Published executable =='
Get-Item $exe | Select-Object FullName, @{N='MB';E={[math]::Round($_.Length / 1MB, 2)}} | Format-List
Get-FileHash $exe -Algorithm SHA256 | Select-Object Hash, Path | Format-List
Write-Host 'Build completed. Test the UI, SSH connection and a staging deployment before publishing a release.'
