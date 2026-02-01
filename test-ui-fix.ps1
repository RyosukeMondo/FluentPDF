#!/usr/bin/env pwsh
# Quick test for UI freeze fix

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Testing UI Freeze Fix" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill any running instances
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Starting FluentPDF..." -ForegroundColor Yellow
Write-Host "Expected: Window should appear and be IMMEDIATELY responsive" -ForegroundColor Green
Write-Host ""
Write-Host "Test checklist:" -ForegroundColor Cyan
Write-Host "  [1] Window appears within 2-3 seconds" -ForegroundColor White
Write-Host "  [2] No loading cursor (spinning wheel)" -ForegroundColor White
Write-Host "  [3] Can click File menu" -ForegroundColor White
Write-Host "  [4] Can hover over buttons" -ForegroundColor White
Write-Host "  [5] Debug console at bottom shows logs" -ForegroundColor White
Write-Host ""

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

if (!(Test-Path $appPath)) {
    Write-Host "ERROR: App not found at $appPath" -ForegroundColor Red
    exit 1
}

Write-Host "Launching: $appPath" -ForegroundColor Gray
Write-Host ""

# Launch and wait for user to test
& $appPath

Write-Host ""
Write-Host "App closed." -ForegroundColor Green
