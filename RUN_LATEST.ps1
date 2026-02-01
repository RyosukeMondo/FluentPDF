#!/usr/bin/env pwsh
# RUN_LATEST.ps1 - Always runs the latest built version

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  FluentPDF - Running Latest Build" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill any old running instances
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force

# Path to the latest build
$LatestBuild = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

if (!(Test-Path $LatestBuild)) {
    Write-Host "ERROR: Latest build not found!" -ForegroundColor Red
    Write-Host "Please build first: dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Release" -ForegroundColor Yellow
    exit 1
}

Write-Host "Running: $LatestBuild" -ForegroundColor Green
Write-Host ""
Write-Host "Look at the BOTTOM of the window for the Debug Console!" -ForegroundColor Yellow
Write-Host ""

# Launch with API server on port 5000 (optional)
if ($args.Length -gt 0 -and $args[0] -eq "--api") {
    Write-Host "Starting with REST API server on port 5000..." -ForegroundColor Cyan
    & $LatestBuild --api-server --port 5000
} else {
    Write-Host "Starting normally (use --api flag for REST API server)..." -ForegroundColor Cyan
    & $LatestBuild
}
