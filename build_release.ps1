# Construieste versiunea de trimis: un singur LastStopNight.exe (include .NET si povestile).
# Rulare:  powershell -ExecutionPolicy Bypass -File build_release.ps1
# Rezultat: dist\LastStopNight.exe si dist\LastStopNight.zip
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '1/3 Verific poveștile (StoryCheck)...'
dotnet run --project tools/StoryCheck -c Release
if ($LASTEXITCODE -ne 0) { throw 'StoryCheck a găsit probleme. Nu public.' }

Write-Host '2/3 Public exe-ul (self-contained, un singur fișier)...'
if (Test-Path dist) { Remove-Item dist -Recurse -Force }
dotnet publish LastStopNight -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -o dist
if ($LASTEXITCODE -ne 0) { throw 'Publicarea a eșuat.' }

Write-Host '3/3 Arhivez...'
Compress-Archive -Path dist\LastStopNight.exe -DestinationPath dist\LastStopNight.zip -Force
$mb = [math]::Round((Get-Item dist\LastStopNight.exe).Length / 1MB, 1)
Write-Host "Gata: dist\LastStopNight.exe ($mb MB). Trimite-l prietenului tău (sau zip-ul)."
