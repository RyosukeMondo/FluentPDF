#!/usr/bin/env pwsh
# Minimal test to find where it hangs

Write-Host "Killing any running instances..." -ForegroundColor Yellow
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Starting app with console output..." -ForegroundColor Cyan
Write-Host "Watch for console messages to see where it hangs" -ForegroundColor Yellow
Write-Host ""

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

# Start process and wait for it to show output or hang
$process = Start-Process -FilePath $appPath -PassThru -Wait:$false

Write-Host "Process started with PID: $($process.Id)" -ForegroundColor Green
Write-Host "Waiting 10 seconds to see if window appears..." -ForegroundColor Yellow

Start-Sleep -Seconds 10

if (!$process.HasExited) {
    Write-Host ""
    Write-Host "App is still running after 10 seconds" -ForegroundColor Yellow
    Write-Host "If window didn't appear, it's hung during initialization" -ForegroundColor Red
    Write-Host "Killing process..." -ForegroundColor Yellow
    $process | Stop-Process -Force
}
else {
    Write-Host ""
    Write-Host "App exited with code: $($process.ExitCode)" -ForegroundColor Red
}
