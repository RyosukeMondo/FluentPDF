#!/usr/bin/env pwsh
# E2E GUI Testing Script for FluentPDF Avalonia
# Tests file loading, rendering, and GUI state verification

param(
    [string]$AppPath = "src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe",
    [string]$TestPdf = "tests\Fixtures\bookmarked.pdf",
    [int]$Port = 5000,
    [int]$Timeout = 30
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "=== FluentPDF E2E GUI Test ===" -ForegroundColor Cyan
Write-Host ""

# Resolve paths
$RepoRoot = Split-Path $PSScriptRoot -Parent
$AppExe = Join-Path $RepoRoot $AppPath
$PdfPath = Join-Path $RepoRoot $TestPdf

if (!(Test-Path $AppExe)) {
    Write-Host "ERROR: App not found at $AppExe" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $PdfPath)) {
    Write-Host "ERROR: Test PDF not found at $PdfPath" -ForegroundColor Red
    exit 1
}

Write-Host "App: $AppExe" -ForegroundColor Gray
Write-Host "PDF: $PdfPath" -ForegroundColor Gray
Write-Host "API: http://localhost:$Port" -ForegroundColor Gray
Write-Host ""

# Kill any existing instances
Write-Host "[1/8] Cleaning up existing processes..." -ForegroundColor Yellow
Get-Process -Name "FluentPDF.Avalonia" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Launch app with API server
Write-Host "[2/8] Launching app with API server..." -ForegroundColor Yellow
$Process = Start-Process -FilePath $AppExe -ArgumentList "--api-server", "--port", $Port -PassThru -WindowStyle Normal

# Wait for API to be ready
Write-Host "[3/8] Waiting for API server..." -ForegroundColor Yellow
$ApiReady = $false
for ($i = 0; $i -lt $Timeout; $i++) {
    try {
        $Health = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -ErrorAction SilentlyContinue
        if ($Health.status -eq "healthy") {
            Write-Host "  [OK] API server ready (PDFium: $($Health.pdfiumLoaded))" -ForegroundColor Green
            $ApiReady = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 1
    }
}

if (!$ApiReady) {
    Write-Host "  [FAIL] API server failed to start" -ForegroundColor Red
    $Process | Stop-Process -Force
    exit 1
}

# Check initial GUI state
Write-Host "[4/8] Checking initial GUI state..." -ForegroundColor Yellow
try {
    $GuiState = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/state"
    Write-Host "  Tabs: $($GuiState.tabCount), Active: $($GuiState.hasActiveTabs)" -ForegroundColor Gray
} catch {
    Write-Host "  [FAIL] GUI state endpoint failed: $_" -ForegroundColor Red
    $Process | Stop-Process -Force
    exit 1
}

# Test 1: Load document via API
Write-Host "[5/8] Test 1: Loading PDF via REST API..." -ForegroundColor Yellow
try {
    $LoadBody = @{
        path = $PdfPath.Replace('\', '/')
    } | ConvertTo-Json

    $LoadResult = Invoke-RestMethod -Uri "http://localhost:$Port/api/document/load" `
        -Method POST `
        -Body $LoadBody `
        -ContentType "application/json"

    Write-Host "  [OK] Document loaded via API" -ForegroundColor Green
    Write-Host "    Session ID: $($LoadResult.documentId)" -ForegroundColor Gray
    Write-Host "    Pages: $($LoadResult.pageCount)" -ForegroundColor Gray

    # Render first page
    Write-Host "  Rendering page 0..." -ForegroundColor Gray
    $RenderUrl = "http://localhost:$Port/api/render/$($LoadResult.documentId)/0"
    $TempPng = Join-Path $env:TEMP "test-page-0.png"
    Invoke-WebRequest -Uri $RenderUrl -OutFile $TempPng

    if (Test-Path $TempPng) {
        $FileInfo = Get-Item $TempPng
        Write-Host "  [OK] Page rendered ($($FileInfo.Length) bytes)" -ForegroundColor Green
        Remove-Item $TempPng -Force
    }
} catch {
    Write-Host "  [FAIL] API document load failed: $_" -ForegroundColor Red
}

# Test 2: Load document via GUI
Write-Host "[6/8] Test 2: Loading PDF via GUI API..." -ForegroundColor Yellow
try {
    $GuiLoadBody = @{
        filePath = $PdfPath.Replace('\', '/')
    } | ConvertTo-Json

    $GuiLoadResult = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/action/open-file" `
        -Method POST `
        -Body $GuiLoadBody `
        -ContentType "application/json"

    Write-Host "  Tab Created: $($GuiLoadResult.tabCreated)" -ForegroundColor Gray
    Write-Host "  Document Loaded: $($GuiLoadResult.documentLoaded)" -ForegroundColor Gray
    Write-Host "  Page Count: $($GuiLoadResult.pageCount)" -ForegroundColor Gray

    if ($GuiLoadResult.success) {
        Write-Host "  [OK] Document loaded in GUI" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Document NOT loaded in GUI" -ForegroundColor Red
        Write-Host "  Message: $($GuiLoadResult.message)" -ForegroundColor Yellow
    }

    # Wait for rendering
    Start-Sleep -Seconds 2

    # Check viewer state
    Write-Host "  Checking viewer state..." -ForegroundColor Gray
    $ViewerState = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/viewer/state"
    Write-Host "    Has Document: $($ViewerState.hasDocument)" -ForegroundColor Gray
    Write-Host "    Is Loading: $($ViewerState.isLoading)" -ForegroundColor Gray
    Write-Host "    Has Rendered Image: $($ViewerState.hasRenderedImage)" -ForegroundColor Gray
    Write-Host "    Image Size: $($ViewerState.imageWidth)x$($ViewerState.imageHeight)" -ForegroundColor Gray
    Write-Host "    Current Page: $($ViewerState.currentPage)/$($ViewerState.totalPages)" -ForegroundColor Gray

    if ($ViewerState.hasRenderedImage) {
        Write-Host "  [OK] Page rendered in GUI viewer" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Page NOT rendered in GUI" -ForegroundColor Red
    }
} catch {
    Write-Host "  [FAIL] GUI load failed: $_" -ForegroundColor Red
    Write-Host "  Error details: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 3: Verify document state
Write-Host "[7/8] Test 3: Verifying document state..." -ForegroundColor Yellow
try {
    $VerifyUrl = "http://localhost:$Port/api/gui/verify/document-loaded?expectedPath=$($PdfPath.Replace('\', '/'))"
    $VerifyResult = Invoke-RestMethod -Uri $VerifyUrl

    Write-Host "  Is Loaded: $($VerifyResult.isLoaded)" -ForegroundColor Gray
    Write-Host "  Path Matches: $($VerifyResult.pathMatches)" -ForegroundColor Gray
    Write-Host "  Has Rendered Image: $($VerifyResult.hasRenderedImage)" -ForegroundColor Gray

    if ($VerifyResult.isLoaded) {
        Write-Host "  [OK] Document verification passed" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Document verification failed" -ForegroundColor Red
        Write-Host "  Reason: $($VerifyResult.reason)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [FAIL] Verification failed: $_" -ForegroundColor Red
}

# Cleanup
Write-Host "[8/8] Cleaning up..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
$Process | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host ""
Write-Host "=== E2E Test Complete ===" -ForegroundColor Cyan
