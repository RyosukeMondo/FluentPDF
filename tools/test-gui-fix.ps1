#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick test for "Try Again" button GUI fix
.DESCRIPTION
    Tests whether the GUI properly displays PDFs without showing "Try Again" button
.EXAMPLE
    .\test-gui-fix.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FLUENTPDF GUI FIX TEST" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Find project root
$projectRoot = Split-Path -Parent $PSScriptRoot

# Find executable
$exePath = "$projectRoot\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
if (-not (Test-Path $exePath)) {
    $exePath = "$projectRoot\src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "❌ ERROR: FluentPDF.Avalonia.exe not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please build the project first:" -ForegroundColor Yellow
    Write-Host "  cd src/FluentPDF.Avalonia" -ForegroundColor Yellow
    Write-Host "  dotnet build --configuration Release" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "📂 Found executable:" -ForegroundColor Green
Write-Host "   $exePath" -ForegroundColor Gray
Write-Host ""

Write-Host "🚀 Starting FluentPDF..." -ForegroundColor Cyan
Write-Host ""

# Start the application
Start-Process -FilePath $exePath

Write-Host "✅ Application started!" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  MANUAL TESTING STEPS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "1️⃣  OPEN A PDF FILE" -ForegroundColor Yellow
Write-Host "    • Press Ctrl+O" -ForegroundColor Gray
Write-Host "    • Or click the 'Open' button" -ForegroundColor Gray
Write-Host "    • Select any valid PDF file" -ForegroundColor Gray
Write-Host ""

Write-Host "2️⃣  VERIFY PDF DISPLAYS CORRECTLY" -ForegroundColor Yellow
Write-Host "    ✅ Expected: PDF content is visible" -ForegroundColor Green
Write-Host "    ✅ Expected: Page renders properly" -ForegroundColor Green
Write-Host "    ✅ Expected: Navigation works" -ForegroundColor Green
Write-Host "    ✅ Expected: NO 'Try Again' button" -ForegroundColor Green
Write-Host ""
Write-Host "    ❌ If you see 'Try Again': The fix didn't work" -ForegroundColor Red
Write-Host ""

Write-Host "3️⃣  TEST ERROR HANDLING (OPTIONAL)" -ForegroundColor Yellow
Write-Host "    • Try opening a non-PDF file" -ForegroundColor Gray
Write-Host "    • Try opening a corrupted PDF" -ForegroundColor Gray
Write-Host ""
Write-Host "    ✅ Expected: Error overlay appears" -ForegroundColor Green
Write-Host "    ✅ Expected: 'Try Again' button is visible" -ForegroundColor Green
Write-Host "    ✅ Expected: Error message is displayed" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  KNOWN GOOD TEST FILES" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# List some test files if they exist
$testPdfPath = "C:\Users\ryosu\Downloads\100_Apps_AI_Factory.pdf"
if (Test-Path $testPdfPath) {
    Write-Host "✅ Found test PDF:" -ForegroundColor Green
    Write-Host "   $testPdfPath" -ForegroundColor Gray
    Write-Host ""
}

$testDir = "$projectRoot\tests\Fixtures"
if (Test-Path $testDir) {
    $pdfFiles = Get-ChildItem -Path $testDir -Filter "*.pdf" -ErrorAction SilentlyContinue
    if ($pdfFiles.Count -gt 0) {
        Write-Host "✅ Found $($pdfFiles.Count) test PDF(s) in tests/Fixtures:" -ForegroundColor Green
        foreach ($pdf in $pdfFiles | Select-Object -First 3) {
            Write-Host "   • $($pdf.Name)" -ForegroundColor Gray
        }
        Write-Host ""
    }
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  WHAT WAS FIXED" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔧 Root Cause:" -ForegroundColor Yellow
Write-Host "   • XAML was binding to HasError and ErrorMessage properties" -ForegroundColor Gray
Write-Host "   • These properties were MISSING from PdfViewerViewModel" -ForegroundColor Gray
Write-Host "   • Failed bindings caused error overlay to show incorrectly" -ForegroundColor Gray
Write-Host ""

Write-Host "✅ Fix Applied:" -ForegroundColor Yellow
Write-Host "   • Added HasError property to ViewModel" -ForegroundColor Gray
Write-Host "   • Added ErrorMessage property to ViewModel" -ForegroundColor Gray
Write-Host "   • Updated error handling to set these properties correctly" -ForegroundColor Gray
Write-Host "   • Error overlay now only shows when HasError = true" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📖 For detailed information, see:" -ForegroundColor Cyan
Write-Host "   GUI_FIX_SUMMARY.md" -ForegroundColor Gray
Write-Host ""
Write-Host "Happy testing! 🎉" -ForegroundColor Green
Write-Host ""
