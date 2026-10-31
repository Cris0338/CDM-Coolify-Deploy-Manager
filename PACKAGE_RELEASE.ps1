# Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
# LOCAL ONLY: build/test and package generic CDM.exe. NEVER uploads or publishes.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\CoolifyDeployManager\CoolifyDeployManager.csproj'
$version = [regex]::Match((Get-Content -LiteralPath $project -Raw),'<Version>(\d+\.\d+\.\d+)</Version>').Groups[1].Value
if (-not $version) { throw 'Cannot determine CDM version from the project file.' }

Write-Host "== Package CDM $version (local build; no upload) =="
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'BUILD_PORTABLE.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Build or offline tests failed; refusing to package.' }

$exe = Join-Path $root 'portable\CDM.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Generic CDM.exe not found.' }
# Refuse to package an executable containing the embedded personal-profile trailer.
$stream = [IO.File]::Open($exe,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
try {
    if ($stream.Length -lt 8) { throw 'Executable too short.' }
    [void]$stream.Seek(-8,[IO.SeekOrigin]::End)
    $magic = New-Object byte[] 8
    if ($stream.Read($magic,0,8) -ne 8) { throw 'Cannot inspect EXE.' }
    if ([Text.Encoding]::ASCII.GetString($magic) -eq 'CDMPF01!') {
        throw 'Refusing to release a personalized executable containing credentials.'
    }
}
finally { $stream.Dispose() }

$out = Join-Path $root 'release-artifacts'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$stage = Join-Path ([IO.Path]::GetTempPath()) ('cdm-package-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$zip = Join-Path $out "CDM-$version-win-x64.zip"
$direct = Join-Path $out 'CDM.exe'
$hashFile = Join-Path $out "CDM-$version-SHA256.txt"
try {
    Copy-Item -LiteralPath $exe -Destination (Join-Path $stage 'CDM.exe') -ErrorAction Stop
    foreach ($name in @('LICENSE','README.md','README_IT.md')) {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination $stage -ErrorAction Stop
    }
    $install = @'
CDM - Coolify Deploy Manager
Created by Cris0338 | Free use and redistribution | NO RESALE
Official license: LICENSE
Windows 10/11 x64: run CDM.exe. No installation or .NET SDK is needed.
This generic EXE has no server configuration or credentials.
Never share your personalized CDM-user-server.exe: it embeds API/SSH credentials.
Windows OpenSSH Client is required only for managed SSH connections.
Read README_IT.md for Italian instructions or README.md for English.
Official downloads: https://github.com/Cris0338/CDM-Coolify-Deploy-Manager/releases
'@
    [IO.File]::WriteAllText((Join-Path $stage 'INSTALL.txt'),$install,[Text.UTF8Encoding]::new($false))
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal -Force
    Copy-Item -LiteralPath $exe -Destination $direct -Force -ErrorAction Stop
    $originalHash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
    $copyHash = (Get-FileHash -LiteralPath $direct -Algorithm SHA256).Hash
    if ($originalHash -ne $copyHash) { throw 'Standalone EXE does not match the verified build.' }
    $zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    $hashContent = ($copyHash + '  CDM.exe' + [Environment]::NewLine +
                    $zipHash + "  CDM-$version-win-x64.zip" + [Environment]::NewLine)
    [IO.File]::WriteAllText($hashFile,$hashContent,[Text.UTF8Encoding]::new($false))
}
finally { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
Write-Host '== Ready for MANUAL GitHub Release upload (NOT published) =='
Get-Item -LiteralPath $direct,$zip,$hashFile | Select-Object Name,Length,FullName | Format-Table -AutoSize
Write-Host 'Do NOT upload any personalized EXE, .bak, SSH key or token.'
Write-Host 'See RELEASE_GUIDE.md and complete the manual release checklist first.'
