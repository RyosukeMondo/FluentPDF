<#
.SYNOPSIS
    Downloads pre-built QPDF binaries for Windows from qpdf/qpdf.
#>
param(
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"

# Version to download
$version = "12.3.0"
$archSuffix = if ($Architecture -eq "x64") { "msvc64" } else { "msvc32" }
$url = "https://github.com/qpdf/qpdf/releases/download/v$version/qpdf-$version-$archSuffix.zip"
$zipPath = "$env:TEMP\qpdf-win-$Architecture.zip"
$extractPath = "$env:TEMP\qpdf-extract"

Write-Host "Downloading QPDF ($Architecture) from: $url"
Invoke-WebRequest -Uri $url -OutFile $zipPath

Write-Host "File downloaded: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB"

if (Test-Path $extractPath) {
    Remove-Item -Path $extractPath -Recurse -Force
}
New-Item -Path $extractPath -ItemType Directory -Force | Out-Null

Write-Host "Extracting .zip..."
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

# Find qpdf.dll
$qpdfDll = Get-ChildItem -Path $extractPath -Recurse -Filter "qpdf*.dll" | Where-Object { $_.Name -match "^qpdf\d+\.dll$" -or $_.Name -eq "qpdf.dll" } | Select-Object -First 1
if ($qpdfDll) {
    Write-Host "`nFound QPDF DLL at: $($qpdfDll.FullName)"
    
    # In recent versions it might be qpdf29.dll or similar, but the app expects qpdf.dll
    # We will copy it as qpdf.dll
    
    $rootPath = Split-Path $PSScriptRoot -Parent
    $libsPath = Join-Path (Join-Path (Join-Path $rootPath "libs") $Architecture) "bin"

    if (-not (Test-Path $libsPath)) {
        New-Item -Path $libsPath -ItemType Directory -Force | Out-Null
    }

    Copy-Item -Path $qpdfDll.FullName -Destination (Join-Path $libsPath "qpdf.dll") -Force
    Write-Host "Copied to: $libsPath\qpdf.dll"

    # Also copy to the App bin directory for immediate use
    $appBinPath = Join-Path $rootPath "src\FluentPDF.App\bin\$Architecture\Debug\net8.0-windows10.0.19041.0\win-$Architecture"
    if (Test-Path $appBinPath) {
        Copy-Item -Path $qpdfDll.FullName -Destination (Join-Path $appBinPath "qpdf.dll") -Force
        Write-Host "Also copied to: $appBinPath\qpdf.dll"
    }

    Write-Host "`nQPDF setup complete!"
}
else {
    Write-Error "qpdf dll not found in extracted archive!"
}
