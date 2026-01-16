# PDFium Threading Fix - Integration Test Script
# This script validates all PDF operations work without crashes

Write-Host "=== FluentPDF Integration Test - PDFium Threading Fix ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Testing Objective:" -ForegroundColor Yellow
Write-Host "  Verify all PDF operations work without AccessViolation crashes"
Write-Host "  after removing Task.Run from PDFium service methods"
Write-Host ""

# Test PDFs
$testPdfDir = "C:\Users\ryosu\repos\FluentPDF\tests\Fixtures"
$testFiles = @(
    "$testPdfDir\sample-form.pdf",          # Forms testing
    "$testPdfDir\bookmarked.pdf",           # Bookmarks testing
    "$testPdfDir\multi-page.pdf",           # Navigation testing
    "$testPdfDir\sample-with-text.pdf",     # Text search testing
    "$testPdfDir\images-graphics.pdf",      # Image rendering testing
    "$testPdfDir\complex-layout.pdf"        # Complex rendering testing
)

Write-Host "Test Files Prepared:" -ForegroundColor Green
foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ $(Split-Path -Leaf $file)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $(Split-Path -Leaf $file) - NOT FOUND" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "Manual Testing Checklist:" -ForegroundColor Cyan
Write-Host ""
Write-Host "[ ] 1. LOAD PDF FILES" -ForegroundColor Yellow
Write-Host "    - Open FluentPDF.App"
Write-Host "    - Load: $($testFiles[0])"
Write-Host "    - Verify: PDF loads without crash, pages render correctly"
Write-Host ""

Write-Host "[ ] 2. BOOKMARKS" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[1])"
Write-Host "    - Click on bookmarks panel"
Write-Host "    - Navigate through bookmarks"
Write-Host "    - Verify: Bookmarks load and navigation works without crash"
Write-Host ""

Write-Host "[ ] 3. PAGE NAVIGATION" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[2])"
Write-Host "    - Navigate through pages using next/prev buttons"
Write-Host "    - Jump to specific pages"
Write-Host "    - Verify: All pages load without crash, rendering is correct"
Write-Host ""

Write-Host "[ ] 4. TEXT SEARCH" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[3])"
Write-Host "    - Open search panel"
Write-Host "    - Search for text (e.g., 'sample', 'test')"
Write-Host "    - Navigate through search results"
Write-Host "    - Verify: Search works without crash, highlights correct"
Write-Host ""

Write-Host "[ ] 5. THUMBNAILS" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[4])"
Write-Host "    - Open thumbnails panel"
Write-Host "    - Scroll through thumbnails"
Write-Host "    - Click on thumbnails to navigate"
Write-Host "    - Verify: Thumbnails render without crash, navigation works"
Write-Host ""

Write-Host "[ ] 6. FORM FIELDS" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[0])"
Write-Host "    - Click on form fields"
Write-Host "    - Enter text in text fields"
Write-Host "    - Check checkboxes"
Write-Host "    - Verify: Form fields work without crash, input persists"
Write-Host ""

Write-Host "[ ] 7. ANNOTATIONS" -ForegroundColor Yellow
Write-Host "    - Load any PDF"
Write-Host "    - Open annotations panel"
Write-Host "    - View existing annotations (if any)"
Write-Host "    - Verify: Annotations load without crash"
Write-Host ""

Write-Host "[ ] 8. ZOOM AND RENDERING" -ForegroundColor Yellow
Write-Host "    - Load: $($testFiles[5])"
Write-Host "    - Zoom in and out"
Write-Host "    - Pan around the document"
Write-Host "    - Verify: Rendering works at all zoom levels without crash"
Write-Host ""

Write-Host "[ ] 9. EXTENDED STABILITY TEST" -ForegroundColor Yellow
Write-Host "    - Keep application running for 5+ minutes"
Write-Host "    - Perform mixed operations: load, navigate, search, zoom"
Write-Host "    - Open and close multiple PDFs"
Write-Host "    - Verify: No crashes, no memory leaks, stable operation"
Write-Host ""

Write-Host "=== Testing Instructions ===" -ForegroundColor Cyan
Write-Host "1. Launch FluentPDF.App manually"
Write-Host "2. Work through the checklist above"
Write-Host "3. Mark each item when completed successfully"
Write-Host "4. If any crash occurs, note the operation and error details"
Write-Host "5. Test should run for minimum 5 minutes of active usage"
Write-Host ""
Write-Host "Expected Result: All operations complete without AccessViolation crashes"
Write-Host ""

# Check if app is already running
$appProcess = Get-Process -Name "FluentPDF.App" -ErrorAction SilentlyContinue
if ($appProcess) {
    Write-Host "✓ FluentPDF.App is already running (PID: $($appProcess.Id))" -ForegroundColor Green
} else {
    Write-Host "Starting FluentPDF.App..." -ForegroundColor Yellow
    $appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
    if (Test-Path $appPath) {
        Start-Process $appPath
        Write-Host "✓ FluentPDF.App launched" -ForegroundColor Green
    } else {
        Write-Host "✗ Application not found at: $appPath" -ForegroundColor Red
        Write-Host "Please build the application first with: dotnet build src/FluentPDF.App -p:Platform=x64" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Press any key when testing is complete..." -ForegroundColor Cyan
