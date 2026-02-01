#!/usr/bin/env pwsh
# test-direct-crash.ps1 - Direct test to capture crash exception

$ErrorActionPreference = "Continue"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DIRECT CRASH TEST" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill existing
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Start app
$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
Write-Host "Starting app..." -ForegroundColor Yellow

# Capture console output
$process = Start-Process -FilePath $appPath `
    -ArgumentList "--api-server", "--port", "5000" `
    -RedirectStandardOutput "app-stdout.log" `
    -RedirectStandardError "app-stderr.log" `
    -PassThru `
    -WindowStyle Hidden

Write-Host "Process ID: $($process.Id)" -ForegroundColor Green
Write-Host ""

# Wait for startup
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
$started = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -TimeoutSec 1 -ErrorAction Stop
        if ($health.status -eq "healthy") {
            $started = $true
            Write-Host "API ready!" -ForegroundColor Green
            break
        }
    } catch { }
    Start-Sleep -Seconds 1
}

if (!$started) {
    Write-Host "FAILED to start" -ForegroundColor Red
    Get-Content "app-stderr.log" -ErrorAction SilentlyContinue
    exit 1
}

Write-Host ""
Write-Host "Triggering file open (this should crash)..." -ForegroundColor Yellow

try {
    $request = @{ filePath = "C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\sample-with-text.pdf" } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/gui/action/open-file" `
        -Method Post `
        -Body $request `
        -ContentType "application/json" `
        -TimeoutSec 10 `
        -ErrorAction Stop

    Write-Host "Response: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Body: $($response.Content)" -ForegroundColor White
}
catch {
    Write-Host "Request failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Wait to see if process crashes
Write-Host ""
Write-Host "Waiting 5 seconds to detect crash..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

if ($process.HasExited) {
    Write-Host ""
    Write-Host "❌ PROCESS CRASHED!" -ForegroundColor Red
    Write-Host "Exit code: $($process.ExitCode)" -ForegroundColor Red
    Write-Host ""

    Write-Host "STDOUT:" -ForegroundColor Cyan
    Get-Content "app-stdout.log" -ErrorAction SilentlyContinue | Select-Object -Last 50
    Write-Host ""

    Write-Host "STDERR:" -ForegroundColor Cyan
    Get-Content "app-stderr.log" -ErrorAction SilentlyContinue | Select-Object -Last 50
} else {
    Write-Host "✅ Process still running" -ForegroundColor Green

    # Get logs from API
    try {
        $logs = Invoke-RestMethod -Uri "http://localhost:5000/api/logs/errors" -TimeoutSec 5
        Write-Host ""
        Write-Host "Error logs from API:" -ForegroundColor Cyan
        $logs | ForEach-Object {
            Write-Host "  [$($_.timestamp)] [$($_.level)] $($_.message)" -ForegroundColor Red
        }
    } catch {
        Write-Host "Could not get logs" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Stopping process..." -ForegroundColor Yellow
    $process | Stop-Process -Force
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
