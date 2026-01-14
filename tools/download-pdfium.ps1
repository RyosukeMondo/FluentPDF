<#
.SYNOPSIS
    Downloads pre-built PDFium binaries for Windows from bblanchon/pdfium-binaries.
#>
param(
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"

$url = "https://github.com/bblanchon/pdfium-binaries/releases/latest/download/pdfium-win-$Architecture.tgz"
$tgzPath = "$env:TEMP\pdfium-win-$Architecture.tgz"
$tarPath = "$env:TEMP\pdfium-win-$Architecture.tar"
$extractPath = "$env:TEMP\pdfium-extract"

Write-Host "Downloading PDFium ($Architecture) from: $url"
Invoke-WebRequest -Uri $url -OutFile $tgzPath

Write-Host "File downloaded: $([math]::Round((Get-Item $tgzPath).Length / 1MB, 2)) MB"

Write-Host "Extracting .tgz (gzip -> tar)..."
if (Test-Path $extractPath) {
    Remove-Item -Path $extractPath -Recurse -Force
}
New-Item -Path $extractPath -ItemType Directory -Force | Out-Null

# Use tar command (available in Windows 10+)
Push-Location $extractPath
tar -xzf $tgzPath
Pop-Location

Write-Host "Contents:"
Get-ChildItem -Path $extractPath -Recurse -Filter "*.dll" | ForEach-Object {
    Write-Host "  $($_.FullName)"
}

# Find pdfium.dll
$pdfiumDll = Get-ChildItem -Path $extractPath -Recurse -Filter "pdfium.dll" | Select-Object -First 1
if ($pdfiumDll) {
    Write-Host "`nFound pdfium.dll at: $($pdfiumDll.FullName)"
    Write-Host "Size: $([math]::Round($pdfiumDll.Length / 1MB, 2)) MB"

    # Copy to libs directory
    $rootPath = Split-Path $PSScriptRoot -Parent
    $libsPath = Join-Path (Join-Path (Join-Path $rootPath "libs") $Architecture) "bin"

    if (-not (Test-Path $libsPath)) {
        New-Item -Path $libsPath -ItemType Directory -Force | Out-Null
    }

    Copy-Item -Path $pdfiumDll.FullName -Destination $libsPath -Force
    Write-Host "Copied to: $libsPath\pdfium.dll"

    # Also copy to the App bin directory for immediate use
    $appBinPath = Join-Path $rootPath "src\FluentPDF.App\bin\$Architecture\Debug\net8.0-windows10.0.19041.0\win-$Architecture"
    if (Test-Path $appBinPath) {
        Copy-Item -Path $pdfiumDll.FullName -Destination $appBinPath -Force
        Write-Host "Also copied to: $appBinPath\pdfium.dll"
    }

    Write-Host "`nPDFium setup complete!"
}
else {
    Write-Error "pdfium.dll not found in extracted archive!"
}
