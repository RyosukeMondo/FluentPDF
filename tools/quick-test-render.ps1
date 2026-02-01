#!/usr/bin/env pwsh
# Quick PDF Rendering Test - One-command testing

param(
    [string]$PdfPath = "",
    [switch]$UseTestFixture
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FLUENTPDF QUICK RENDER TEST" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Find project root (tools is at project-root/tools, so go up one level)
$projectRoot = Split-Path -Parent $PSScriptRoot

# Find test PDF
if (-not $PdfPath) {
    if ($UseTestFixture) {
        $PdfPath = Join-Path $projectRoot "tests\Fixtures\sample-with-text.pdf"
        if (-not (Test-Path $PdfPath)) {
            Write-Host "❌ Test fixture not found: $PdfPath" -ForegroundColor Red
            Write-Host ""
            Write-Host "Looking for any PDF in Downloads..." -ForegroundColor Yellow
            $PdfPath = ""
        }
        else {
            Write-Host "✅ Using test fixture: $PdfPath" -ForegroundColor Green
        }
    }

    if (-not $PdfPath) {
        # Try to find a PDF in Downloads
        $downloadsPath = [Environment]::GetFolderPath('UserProfile') + "\Downloads"
        $pdfs = Get-ChildItem $downloadsPath -Filter "*.pdf" -ErrorAction SilentlyContinue | Select-Object -First 1

        if ($pdfs) {
            $PdfPath = $pdfs.FullName
            Write-Host "✅ Found PDF: $PdfPath" -ForegroundColor Green
        }
        else {
            Write-Host "❌ No PDF found. Please provide a path:" -ForegroundColor Red
            Write-Host ""
            Write-Host "Usage: pwsh tools/quick-test-render.ps1 -PdfPath 'C:\path\to\test.pdf'" -ForegroundColor Yellow
            Write-Host "   or: pwsh tools/quick-test-render.ps1 -UseTestFixture" -ForegroundColor Yellow
            exit 1
        }
    }
}

Write-Host ""

# Run the full autonomous test
& "$PSScriptRoot\test-pdf-render-autonomous.ps1" `
    -PdfPath $PdfPath `
    -StartServer `
    -Verbose

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  TEST COMPLETE" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Check test-results/ directory for detailed output" -ForegroundColor Gray
Write-Host ""
