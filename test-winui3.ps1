# Test WinUI 3 FluentPDF Application
# This script runs comprehensive tests on the FluentPDF WinUI 3 app

$appPath = "src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
$fixturesPath = "tests\Fixtures"

Write-Host "=== FluentPDF WinUI 3 Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Check if app exists
if (-not (Test-Path $appPath)) {
    Write-Host "ERROR: App not found at: $appPath" -ForegroundColor Red
    Write-Host "Please build the app first with: dotnet build src/FluentPDF.App -p:Platform=x64" -ForegroundColor Yellow
    exit 1
}

# Test 1: System Diagnostics
Write-Host "[1/4] Running system diagnostics..." -ForegroundColor Yellow
& $appPath --diagnostics
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ PASS: System diagnostics successful" -ForegroundColor Green
} else {
    Write-Host "✗ FAIL: System diagnostics failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Render bookmarked.pdf
Write-Host "[2/4] Testing PDF rendering (bookmarked.pdf)..." -ForegroundColor Yellow
$testPdf = "$fixturesPath\bookmarked.pdf"
& $appPath --test-render $testPdf
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ PASS: PDF rendered successfully" -ForegroundColor Green
} else {
    Write-Host "✗ FAIL: PDF rendering failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Render complex-layout.pdf
Write-Host "[3/4] Testing complex PDF (complex-layout.pdf)..." -ForegroundColor Yellow
$testPdf2 = "$fixturesPath\complex-layout.pdf"
& $appPath --test-render $testPdf2
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ PASS: Complex PDF rendered successfully" -ForegroundColor Green
} else {
    Write-Host "✗ FAIL: Complex PDF rendering failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Launch GUI
Write-Host "[4/4] Launching GUI for manual testing..." -ForegroundColor Yellow
Write-Host "The app will open in GUI mode with bookmarked.pdf" -ForegroundColor Cyan
Write-Host "Please verify:" -ForegroundColor Yellow
Write-Host "  - App launches without crash" -ForegroundColor White
Write-Host "  - PDF displays correctly" -ForegroundColor White
Write-Host "  - Layout matches React prototype (3 columns: Thumbnails | Bookmarks | Viewer)" -ForegroundColor White
Write-Host "  - All toolbars are visible" -ForegroundColor White
Write-Host ""
Write-Host "Press Enter to launch GUI..." -ForegroundColor Green
Read-Host

Start-Process -FilePath $appPath -ArgumentList "--open-file `"$fixturesPath\bookmarked.pdf`""

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host "GUI is now running. Close the app window when done testing." -ForegroundColor Yellow
