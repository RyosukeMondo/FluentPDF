# FluentPDF Autonomous Testing Guide

**Complete guide for autonomous PDF rendering verification**

---

## Quick Start

### Option 1: One-Command Test (Easiest)
```powershell
pwsh tools/quick-test-render.ps1 -UseTestFixture
```

### Option 2: Test with Your Own PDF
```powershell
pwsh tools/quick-test-render.ps1 -PdfPath "C:\path\to\your.pdf"
```

### Option 3: Full Control
```powershell
pwsh tools/test-pdf-render-autonomous.ps1 `
    -PdfPath "C:\test.pdf" `
    -StartServer `
    -Port 5000 `
    -OutputDir "test-results"
```

---

## CLI Options

### Running the App

```powershell
# Start with GUI
FluentPDF.Avalonia.exe

# Start API server with GUI
FluentPDF.Avalonia.exe --api-server

# Start API server headless (no GUI)
FluentPDF.Avalonia.exe --api-server --headless --port 5000

# Enable verbose logging
FluentPDF.Avalonia.exe --api-server --verbose
```

### Available Flags

| Flag | Description | Example |
|------|-------------|---------|
| `--api-server` | Start REST API server | `--api-server` |
| `--port <num>` | Set API port (default: 5000) | `--port 8080` |
| `--headless` | Run without UI (server only) | `--headless` |
| `--verbose` | Enable verbose logging | `--verbose` |
| `--test-render <path>` | Test PDF rendering | `--test-render "file.pdf"` |
| `--test-load <path>` | Test PDF loading | `--test-load "file.pdf"` |
| `--output <dir>` | Output directory for test results | `--output "results"` |
| `--test-mode` | Run in test mode (exit after test) | `--test-mode` |

---

## REST API Endpoints

### Health Check
```bash
curl http://localhost:5000/api/health
```

**Response:**
```json
{
  "status": "healthy",
  "pdfiumInitialized": true,
  "version": "1.0.0",
  "timestamp": "2026-01-29T12:00:00Z"
}
```

### Open File in GUI
```bash
curl -X POST http://localhost:5000/api/gui/action/open-file \
  -H "Content-Type: application/json" \
  -d '{"filePath":"C:/test.pdf"}'
```

**Response:**
```json
{
  "success": true,
  "filePath": "C:/test.pdf",
  "tabCreated": true,
  "documentLoaded": true,
  "pageCount": 10,
  "message": "Document loaded in GUI"
}
```

### Verify Document Loaded
```bash
curl "http://localhost:5000/api/gui/verify/document-loaded?expectedPath=C:/test.pdf"
```

**Response:**
```json
{
  "isLoaded": true,
  "hasDocument": true,
  "isLoading": false,
  "currentPath": "C:/test.pdf",
  "expectedPath": "C:/test.pdf",
  "pathMatches": true,
  "currentPage": 1,
  "totalPages": 10,
  "hasRenderedImage": true
}
```

### Verify Page Rendered
```bash
curl "http://localhost:5000/api/gui/verify/page-rendered?pageNumber=1"
```

**Response:**
```json
{
  "isRendered": true,
  "hasImage": true,
  "currentPage": 1,
  "expectedPage": 1,
  "pageMatches": true,
  "imageWidth": 612,
  "imageHeight": 792,
  "isLoading": false,
  "statusMessage": "Ready"
}
```

### Get GUI State
```bash
curl http://localhost:5000/api/gui/state
```

**Response:**
```json
{
  "tabCount": 1,
  "hasActiveTabs": true,
  "activeTab": {
    "filePath": "C:/test.pdf",
    "fileName": "test.pdf",
    "hasUnsavedChanges": false
  },
  "tabs": [
    {
      "filePath": "C:/test.pdf",
      "fileName": "test.pdf",
      "hasUnsavedChanges": false
    }
  ]
}
```

### Get Viewer State
```bash
curl http://localhost:5000/api/gui/viewer/state
```

**Response:**
```json
{
  "hasDocument": true,
  "isLoading": false,
  "currentPage": 1,
  "totalPages": 10,
  "zoomLevel": 1.0,
  "statusMessage": "Ready",
  "filePath": "C:/test.pdf",
  "hasRenderedImage": true,
  "imageWidth": 612,
  "imageHeight": 792,
  "viewMode": "SinglePage"
}
```

---

## Autonomous Test Scripts

### 1. Quick Test (Auto-Find PDF)
```powershell
pwsh tools/quick-test-render.ps1
```

**Features:**
- Automatically finds test PDF
- Starts server automatically
- Runs all verification tests
- Stops server when done
- Generates test results

### 2. Full Autonomous Test
```powershell
pwsh tools/test-pdf-render-autonomous.ps1 `
    -PdfPath "C:\test.pdf" `
    -StartServer `
    -Port 5000 `
    -OutputDir "test-results"
```

**Test Steps:**
1. ✅ Start API server (if `-StartServer` specified)
2. ✅ Health check
3. ✅ Verify initial GUI state (empty)
4. ✅ Open PDF file
5. ✅ Verify document loaded
6. ✅ Verify page rendered
7. ✅ Check GUI state (with document)
8. ✅ Validate viewer state
9. ✅ Save test results to JSON

**Output:**
```
═══════════════════════════════════════════════════════════
  FLUENTPDF AUTONOMOUS RENDERING TEST
═══════════════════════════════════════════════════════════

STEP 1: Starting Server
  ▸ Starting FluentPDF API server on port 5000...
  ▸ Waiting for server to initialize...
  ✅ PASS: Server started successfully (PID: 12345)

STEP 2: Health Check
  ▸ Testing /api/health endpoint...
  ✅ PASS: Health check passed (Status: healthy, PDFium: True)

STEP 3: Initial GUI State
  ▸ Testing /api/gui/state...
  ✅ PASS: GUI state retrieved successfully

STEP 4: Open PDF File
  ▸ Testing /api/gui/action/open-file...
  ✅ PASS: File opened in GUI (Pages: 10)

STEP 5: Verify Document Loaded
  ▸ Testing /api/gui/verify/document-loaded...
  ✅ PASS: Document verified loaded (Pages: 10, CurrentPage: 1)

STEP 6: Verify Page Rendered
  ▸ Testing /api/gui/verify/page-rendered...
  ✅ PASS: Page rendered successfully (Page: 1, Size: 612x792)

STEP 7: GUI State After Load
  ▸ Testing /api/gui/state...
  ✅ PASS: GUI state retrieved successfully

STEP 8: Viewer State
  ▸ Testing /api/gui/viewer/state...
  ✅ PASS: Viewer state verified (Document loaded and page rendered)

STEP 9: Save Results
  ▸ Saving test results...
  ✅ PASS: Test results saved to test-results/test-results-20260129-120000.json

═══════════════════════════════════════════════════════════
  TEST SUMMARY
═══════════════════════════════════════════════════════════

  Total Tests: 9
  ✅ Passed: 9
  ❌ Failed: 0
  ⚠️  Skipped: 0

  ✅ OVERALL: PASSED

═══════════════════════════════════════════════════════════
```

---

## Testing Scenarios

### Scenario 1: Test Rendering After Bug Fix
```powershell
# 1. Stop any running instances
pwsh -Command "Get-Process FluentPDF.Avalonia | Stop-Process -Force"

# 2. Rebuild
dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Release

# 3. Run autonomous test
pwsh tools/quick-test-render.ps1 -PdfPath "tests\Fixtures\sample-with-text.pdf"
```

### Scenario 2: CI/CD Integration
```powershell
# In GitHub Actions or CI pipeline
pwsh tools/test-pdf-render-autonomous.ps1 `
    -PdfPath "tests/Fixtures/sample.pdf" `
    -StartServer `
    -OutputDir "test-results"

# Check exit code
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!"
    exit 1
}
```

### Scenario 3: Manual API Testing
```powershell
# 1. Start server manually
FluentPDF.Avalonia.exe --api-server --port 5000

# 2. In another terminal, run tests against running server
pwsh tools/test-pdf-render-autonomous.ps1 `
    -PdfPath "C:\test.pdf" `
    -Port 5000
    # Note: No -StartServer flag
```

### Scenario 4: Performance Testing
```powershell
# Test rendering performance with multiple PDFs
$pdfs = Get-ChildItem "tests\Fixtures" -Filter "*.pdf"

foreach ($pdf in $pdfs) {
    Write-Host "Testing: $($pdf.Name)"
    pwsh tools/test-pdf-render-autonomous.ps1 `
        -PdfPath $pdf.FullName `
        -StartServer
}
```

---

## Troubleshooting

### Problem: "Server failed to start"

**Solution:**
```powershell
# Check if port is in use
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue

# Kill any hanging processes
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force

# Try again
pwsh tools/quick-test-render.ps1
```

### Problem: "Document not loaded"

**Diagnose:**
```bash
# Check server logs
curl http://localhost:5000/api/logs/recent

# Check viewer state
curl http://localhost:5000/api/gui/viewer/state
```

### Problem: "Page not rendered"

**Diagnose:**
```bash
# Check if document is loaded
curl http://localhost:5000/api/gui/verify/document-loaded

# Check viewer status
curl http://localhost:5000/api/gui/viewer/state

# Look for IsLoading=true or StatusMessage
```

### Problem: Tests timeout

**Solution:**
```powershell
# Increase timeout in test script (edit test-pdf-render-autonomous.ps1)
# Line ~207: change -TimeoutSec 60 to -TimeoutSec 120

# Or run with verbose logging
pwsh tools/test-pdf-render-autonomous.ps1 -PdfPath "file.pdf" -StartServer -Verbose
```

---

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: PDF Rendering Tests

on: [push, pull_request]

jobs:
  test-rendering:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build
        run: dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Release

      - name: Run Rendering Tests
        run: |
          pwsh tools/test-pdf-render-autonomous.ps1 `
            -PdfPath "tests/Fixtures/sample-with-text.pdf" `
            -StartServer `
            -OutputDir "test-results"

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: test-results/
```

---

## Advanced Usage

### Custom Verification Script
```powershell
# Start server
FluentPDF.Avalonia.exe --api-server --headless --port 5000 &

# Wait for startup
Start-Sleep -Seconds 5

# Your custom tests
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/gui/action/open-file" `
    -Method Post `
    -Body '{"filePath":"C:/test.pdf"}' `
    -ContentType "application/json"

if ($response.success) {
    Write-Host "✅ PDF opened successfully"

    # Verify rendering
    $rendered = Invoke-RestMethod -Uri "http://localhost:5000/api/gui/verify/page-rendered"

    if ($rendered.isRendered) {
        Write-Host "✅ Page rendered: $($rendered.imageWidth)x$($rendered.imageHeight)"
    }
}

# Cleanup
Get-Process FluentPDF.Avalonia | Stop-Process -Force
```

---

## Summary

**All tools are now available for autonomous PDF rendering verification:**

✅ **Enhanced CLI** - Test modes, headless operation, auto-exit
✅ **Comprehensive REST API** - Full GUI state inspection
✅ **Autonomous Test Scripts** - One-command testing
✅ **CI/CD Ready** - Exit codes, JSON reports
✅ **Detailed Diagnostics** - Health checks, state verification

**Run your first test:**
```powershell
pwsh tools/quick-test-render.ps1 -UseTestFixture
```

This will automatically test PDF loading and rendering, then report success or failure!
