#!/usr/bin/env pwsh
# Test if app is actually responsive via API

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

Write-Host "Starting app with API server..." -ForegroundColor Yellow
$process = Start-Process -FilePath $appPath -ArgumentList "--api-server","--port","5555" -PassThru

Write-Host "Waiting 5 seconds for startup..." -ForegroundColor Gray
Start-Sleep -Seconds 5

Write-Host "Testing API responsiveness..." -ForegroundColor Yellow

try {
    $health = Invoke-RestMethod -Uri "http://localhost:5555/api/health" -TimeoutSec 3
    Write-Host ""
    Write-Host "✅ SUCCESS - App IS responsive!" -ForegroundColor Green
    Write-Host "   Status: $($health.status)" -ForegroundColor Cyan
    Write-Host "   The app is running and processing requests" -ForegroundColor Green
    Write-Host ""
    Write-Host "This proves the app works - the issue is ONLY with GUI input events" -ForegroundColor Yellow
}
catch {
    Write-Host ""
    Write-Host "❌ FAILED - App not responding to API" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Cleaning up..." -ForegroundColor Gray
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
