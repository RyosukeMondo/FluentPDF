#!/usr/bin/env pwsh
# Launch FluentPDF with full console output

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  FluentPDF Avalonia - Console Debug" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill any existing processes
Write-Host "Killing existing processes..." -ForegroundColor Yellow
Get-Process -Name "FluentPDF.Avalonia" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Navigate to bin directory
Set-Location "$PSScriptRoot\src\FluentPDF.Avalonia\bin\Debug\net8.0"

Write-Host "Current directory: $(Get-Location)" -ForegroundColor Gray
Write-Host ""

# Check if exe exists
if (Test-Path "FluentPDF.Avalonia.exe") {
    Write-Host "✅ FluentPDF.Avalonia.exe found" -ForegroundColor Green
} else {
    Write-Host "❌ FluentPDF.Avalonia.exe NOT found!" -ForegroundColor Red
    exit 1
}

# Check PDFium
if (Test-Path "pdfium.dll") {
    Write-Host "✅ pdfium.dll found" -ForegroundColor Green
} else {
    Write-Host "⚠️  pdfium.dll NOT found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Launching FluentPDF.Avalonia.exe..." -ForegroundColor Cyan
Write-Host "Watch for console output below:" -ForegroundColor Gray
Write-Host "----------------------------------------" -ForegroundColor Gray

# Run the exe and capture all output
try {
    & ".\FluentPDF.Avalonia.exe"
} catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Red
}

Write-Host ""
Write-Host "App closed or crashed" -ForegroundColor Yellow
Read-Host "Press Enter to exit"
