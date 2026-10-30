$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\CoolifyDeployManager\CoolifyDeployManager.csproj'
$tests = Join-Path $root 'tests\ClassifierTests\ClassifierTests.csproj'
$destination = Join-Path $root 'portable'
# Script di pubblicazione portable: Coolify Deploy Manager 2.1.0 · © Cris0338
# I sorgenti C# e i test sono nella stessa cartella sotto src/ e tests/.

$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (!(Test-Path $dotnet)) { throw '.NET 10 SDK x64 non trovato in Program Files.' }
Write-Host '== Test classificazione (nessun deploy) =='
& $dotnet run --project $tests --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Test falliti (exit $LASTEXITCODE)" }
Write-Host '== Build WPF Windows x64 portable single-file =='
& $dotnet publish $project -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' '-p:DebugType=None' '-p:DebugSymbols=false' -o $destination
if ($LASTEXITCODE -ne 0) { throw "Compilazione fallita (exit $LASTEXITCODE)" }
$exe = Join-Path $destination 'CoolifyDeployManager.exe'
if (!(Test-Path $exe)) { throw "EXE non creato: $exe" }
$other = @(Get-ChildItem $destination -File | Where-Object Name -ne 'CoolifyDeployManager.exe')
if ($other.Count -gt 0) { Write-Warning "Ci sono altri file in portable: $($other.Name -join ', ')" }
Write-Host '== Risultato =='
Get-Item $exe | Select-Object FullName,@{N='MB';E={[math]::Round($_.Length/1MB,2)}} | Format-List
Get-FileHash $exe -Algorithm SHA256 | Select-Object Hash,Path | Format-List
Write-Host 'EXE generato. Testare connessione e staging prima della distribuzione.'
