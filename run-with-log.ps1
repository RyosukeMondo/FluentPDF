#!/usr/bin/env pwsh
# Runs app and captures ALL output to log file

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = "hang-debug-$timestamp.txt"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Running FluentPDF with Full Logging" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Log file: $logFile" -ForegroundColor Green
Write-Host ""

# Kill any existing instances
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

Write-Host "Starting app and capturing output..." -ForegroundColor Yellow
Write-Host "Press Ctrl+C after 10 seconds if app hangs" -ForegroundColor Yellow
Write-Host ""

# Run app and capture ALL output (stdout + stderr)
try {
    & $appPath 2>&1 | Tee-Object -FilePath $logFile
}
catch {
    Write-Host "App terminated" -ForegroundColor Red
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Log saved to: $logFile" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Last 50 lines of log:" -ForegroundColor Yellow
Get-Content $logFile -Tail 50 | Write-Host

Write-Host ""
Write-Host "Full log available at: $(Resolve-Path $logFile)" -ForegroundColor Green
