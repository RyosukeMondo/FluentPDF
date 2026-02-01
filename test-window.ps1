#!/usr/bin/env pwsh
# Quick test to see error output

$ErrorActionPreference = "Continue"

cd "$PSScriptRoot/src/FluentPDF.Avalonia"

Write-Host "Testing FluentPDF.Avalonia launch..." -ForegroundColor Cyan
Write-Host ""

# Run with full error output
dotnet run --no-build 2>&1 | Tee-Object -FilePath "launch-errors.log"

Write-Host ""
Write-Host "Error log saved to: src/FluentPDF.Avalonia/launch-errors.log" -ForegroundColor Yellow
