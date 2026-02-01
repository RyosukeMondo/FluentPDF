# FluentPDF Avalonia API - Quick Start Guide

## Overview

The FluentPDF Avalonia REST API enables autonomous PDF testing without UI interaction.

## Installation

### Prerequisites
- .NET 8.0 SDK
- FluentPDF Avalonia built

### Build
```bash
dotnet build src/FluentPDF.Avalonia
```

## Starting the Server

### Option 1: With UI (Default)
```bash
cd src/FluentPDF.Avalonia/bin/Debug/net8.0
./FluentPDF.Avalonia.exe --api-server
```

Server runs at `http://localhost:5000` with full UI available.

### Option 2: Headless (No UI)
```bash
./FluentPDF.Avalonia.exe --api-server --headless
```

Server runs at `http://localhost:5000` without creating a window. Perfect for CI/CD.

### Option 3: Custom Port
```bash
./FluentPDF.Avalonia.exe --api-server --port 8080 --headless
```

Server runs at `http://localhost:8080` in headless mode.

## Quick Test (PowerShell)

```powershell
# Test health endpoint
Invoke-RestMethod -Uri "http://localhost:5000/api/health"

# Expected output:
# status        : healthy
# version       : 1.0.0
# pdfiumLoaded  : True
# activeSessions: 0
# timestamp     : 2025-01-28T10:00:00Z
```

## Basic Usage Example

### Step 1: Load a PDF
```powershell
$loadRequest = @{
    path = "C:/path/to/document.pdf"
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "http://localhost:5000/api/document/load" `
    -Method Post `
    -Body $loadRequest `
    -ContentType "application/json"

$documentId = $response.documentId
Write-Host "Loaded document: $documentId"
Write-Host "Page count: $($response.pageCount)"
```

### Step 2: Render a Page
```powershell
# Render page 0 to PNG
Invoke-WebRequest `
    -Uri "http://localhost:5000/api/render/$documentId/0" `
    -OutFile "page0.png"

Write-Host "Page rendered to page0.png"
```

### Step 3: Verify Rendering
```powershell
$verifyRequest = @{
    documentId = $documentId
    pageIndex = 0
} | ConvertTo-Json

$result = Invoke-RestMethod `
    -Uri "http://localhost:5000/api/verify/render" `
    -Method Post `
    -Body $verifyRequest `
    -ContentType "application/json"

Write-Host "Hash: $($result.hash)"
Write-Host "Match: $($result.match)"
```

### Step 4: Close Document
```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5000/api/document/$documentId" `
    -Method Delete

Write-Host "Document closed"
```

## Automated Testing

Run the comprehensive test script:

```powershell
# Test with sample PDF
.\tools\test-avalonia-api.ps1

# Test with custom PDF
.\tools\test-avalonia-api.ps1 -PdfPath "path/to/test.pdf"

# Test on custom port
.\tools\test-avalonia-api.ps1 -Port 8080
```

The script tests:
1. Health check
2. Document loading
3. Document info
4. Page rendering
5. Hash generation
6. Hash verification
7. Batch verification
8. Document closing

## Common Use Cases

### Use Case 1: Regression Testing
```powershell
# Generate baseline hashes
$baselineHashes = @{}
for ($i = 0; $i -lt $pageCount; $i++) {
    $verify = Invoke-RestMethod `
        -Uri "http://localhost:5000/api/verify/render" `
        -Method Post `
        -Body (@{ documentId = $docId; pageIndex = $i } | ConvertTo-Json) `
        -ContentType "application/json"
    $baselineHashes[$i] = $verify.hash
}

# Later: Verify against baselines
$batchRequest = @{
    documentId = $newDocId
    baselines = $baselineHashes
} | ConvertTo-Json

$result = Invoke-RestMethod `
    -Uri "http://localhost:5000/api/verify/batch" `
    -Method Post `
    -Body $batchRequest `
    -ContentType "application/json"

if ($result.allMatch) {
    Write-Host "✅ All pages match baseline"
} else {
    Write-Host "❌ $($result.failures.Count) pages failed"
}
```

### Use Case 2: CI/CD Integration
```yaml
# GitHub Actions example
- name: Start API Server
  run: |
    ./FluentPDF.Avalonia.exe --api-server --headless &
    sleep 3

- name: Run API Tests
  run: pwsh ./tools/test-avalonia-api.ps1

- name: Generate Baseline Hashes
  run: pwsh ./tools/generate-baselines.ps1
```

### Use Case 3: Performance Testing
```powershell
# Measure rendering time for all pages
$times = @()
for ($i = 0; $i -lt $pageCount; $i++) {
    $start = Get-Date
    Invoke-WebRequest `
        -Uri "http://localhost:5000/api/render/$docId/$i" `
        -OutFile "page$i.png"
    $elapsed = (Get-Date) - $start
    $times += $elapsed.TotalMilliseconds
}

$avgTime = ($times | Measure-Object -Average).Average
Write-Host "Average render time: $avgTime ms"
```

## Troubleshooting

### Server Won't Start

**Problem**: Port already in use
```
ERROR: Failed to start API server: Address already in use
```

**Solution**: Use a different port
```bash
./FluentPDF.Avalonia.exe --api-server --port 8080
```

### Document Load Fails

**Problem**: File not found
```json
{
  "error": "PDF_FILE_NOT_FOUND",
  "message": "File not found: C:/test.pdf"
}
```

**Solution**: Use absolute path and check file exists
```powershell
$pdfPath = Resolve-Path "relative/path/to/file.pdf"
```

### Rendering Fails

**Problem**: Invalid page index
```json
{
  "error": "PAGE_OUT_OF_RANGE",
  "message": "Page index 10 is out of range"
}
```

**Solution**: Check page count first
```powershell
$info = Invoke-RestMethod -Uri "http://localhost:5000/api/document/$docId"
Write-Host "Document has $($info.pageCount) pages (0-based)"
```

### Hash Mismatch

**Problem**: Verification fails
```json
{
  "match": false,
  "hash": "abc123...",
  "ssim": null
}
```

**Solution**: Ensure same DPI/zoom settings
```powershell
# Always use same settings for baseline and verification
$verifyRequest = @{
    documentId = $docId
    pageIndex = 0
    baselineHash = $baseline
    dpi = 96  # Must match baseline DPI
}
```

## API Reference

Full documentation: `src/FluentPDF.Avalonia/Api/README.md`

### Quick Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/health` | GET | Server status |
| `/api/document/load` | POST | Load PDF |
| `/api/document/{id}` | GET | Get info |
| `/api/document/{id}` | DELETE | Close |
| `/api/render` | POST | Render page |
| `/api/render/{id}/{page}` | GET | Render page |
| `/api/verify/render` | POST | Verify hash |
| `/api/verify/batch` | POST | Batch verify |

## Best Practices

### 1. Always Close Documents
```powershell
try {
    # Load and use document
    $doc = Invoke-RestMethod ...
} finally {
    # Always close, even on error
    Invoke-RestMethod -Uri "http://localhost:5000/api/document/$docId" -Method Delete
}
```

### 2. Use Correlation IDs
```powershell
$correlationId = [Guid]::NewGuid().ToString()
$headers = @{ "X-Correlation-Id" = $correlationId }

Invoke-RestMethod -Uri "http://localhost:5000/api/document/load" -Headers $headers
# All logs will include this correlation ID for tracing
```

### 3. Check Health Before Testing
```powershell
$health = Invoke-RestMethod -Uri "http://localhost:5000/api/health"
if ($health.status -ne "healthy") {
    throw "Server not healthy: $($health.status)"
}
```

### 4. Handle Errors Gracefully
```powershell
try {
    $result = Invoke-RestMethod ...
} catch {
    $error = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Error: $($error.error)"
    Write-Host "Message: $($error.message)"
    Write-Host "Correlation ID: $($error.correlationId)"
}
```

## Next Steps

1. Read full API documentation: `src/FluentPDF.Avalonia/Api/README.md`
2. Run test script: `tools/test-avalonia-api.ps1`
3. Integrate into your CI/CD pipeline
4. Create custom verification scripts

## Support

- **Implementation Details**: See `AVALONIA_API_IMPLEMENTATION.md`
- **WinUI 3 API**: See `src/FluentPDF.App/Api/README.md`
- **Issues**: Create GitHub issue with `avalonia-api` label
