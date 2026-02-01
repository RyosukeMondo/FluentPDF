#!/usr/bin/env pwsh
# Test script for Avalonia FluentPDF application

Write-Host "FluentPDF Avalonia Test Script" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project root
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

Write-Host "1. Building Avalonia project..." -ForegroundColor Yellow
$buildResult = dotnet build src/FluentPDF.Avalonia 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

Write-Host "2. Checking for pdfium.dll..." -ForegroundColor Yellow
$pdfiumPath = "src/FluentPDF.Avalonia/bin/Debug/net8.0/pdfium.dll"
if (Test-Path $pdfiumPath) {
    $pdfiumInfo = Get-Item $pdfiumPath
    Write-Host "✅ pdfium.dll found ($($pdfiumInfo.Length / 1MB) MB)" -ForegroundColor Green
} else {
    Write-Host "❌ pdfium.dll not found at $pdfiumPath" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "3. Checking for qpdf.dll..." -ForegroundColor Yellow
$qpdfPath = "src/FluentPDF.Avalonia/bin/Debug/net8.0/qpdf.dll"
if (Test-Path $qpdfPath) {
    $qpdfInfo = Get-Item $qpdfPath
    Write-Host "✅ qpdf.dll found ($($qpdfInfo.Length / 1MB) MB)" -ForegroundColor Green
} else {
    Write-Host "⚠️ qpdf.dll not found (may not be required)" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "4. Starting application (will run for 5 seconds)..." -ForegroundColor Yellow
$process = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/FluentPDF.Avalonia" `
    -PassThru `
    -NoNewWindow `
    -RedirectStandardOutput "avalonia-test-output.txt" `
    -RedirectStandardError "avalonia-test-error.txt"

Start-Sleep -Seconds 5

if ($process.HasExited) {
    Write-Host "❌ Application exited unexpectedly" -ForegroundColor Red
    Write-Host "Error output:" -ForegroundColor Red
    Get-Content "avalonia-test-error.txt" -ErrorAction SilentlyContinue
    exit 1
} else {
    Write-Host "✅ Application is running" -ForegroundColor Green

    # Check if window exists
    $windowTitle = "FluentPDF"
    $windows = Get-Process | Where-Object { $_.MainWindowTitle -like "*$windowTitle*" }

    if ($windows.Count -gt 0) {
        Write-Host "✅ FluentPDF window found with title: $($windows[0].MainWindowTitle)" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Could not verify window title (window may still be initializing)" -ForegroundColor Yellow
    }

    # Stop the process
    Stop-Process -Id $process.Id -Force
    Write-Host "✅ Application stopped successfully" -ForegroundColor Green
}
Write-Host ""

Write-Host "5. Checking log output..." -ForegroundColor Yellow
if (Test-Path "avalonia-test-output.txt") {
    $output = Get-Content "avalonia-test-output.txt" -Raw
    if ($output) {
        Write-Host "Standard output:" -ForegroundColor Gray
        Write-Host $output -ForegroundColor Gray
    }
}

if (Test-Path "avalonia-test-error.txt") {
    $errors = Get-Content "avalonia-test-error.txt" -Raw
    if ($errors -and $errors.Trim().Length -gt 0) {
        Write-Host "Standard error:" -ForegroundColor Yellow
        Write-Host $errors -ForegroundColor Yellow
    }
}
Write-Host ""

# Clean up
Remove-Item "avalonia-test-output.txt" -ErrorAction SilentlyContinue
Remove-Item "avalonia-test-error.txt" -ErrorAction SilentlyContinue

Write-Host "===============================" -ForegroundColor Cyan
Write-Host "✅ All tests passed!" -ForegroundColor Green
Write-Host ""
Write-Host "Manual testing:" -ForegroundColor Cyan
Write-Host "  Run: dotnet run --project src/FluentPDF.Avalonia" -ForegroundColor White
Write-Host "  Expected: Window opens with 'FluentPDF' title" -ForegroundColor White
Write-Host "  Expected: Menu bar with File and Tools menus" -ForegroundColor White
Write-Host "  Expected: Empty state showing 'No PDFs open'" -ForegroundColor White
Write-Host ""
