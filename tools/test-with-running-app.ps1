#!/usr/bin/env pwsh
# Test PDF Rendering with Already-Running App
# This script tests against a FluentPDF instance that's already running with API server

param(
    [string]$PdfPath = "C:\Users\ryosu\Downloads\100_Apps_AI_Factory.pdf",
    [int]$Port = 5000
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  FLUENTPDF API TEST (Against Running App)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script tests the API of an ALREADY-RUNNING FluentPDF instance." -ForegroundColor Yellow
Write-Host ""
Write-Host "To start the app with API server:" -ForegroundColor Gray
Write-Host "  FluentPDF.Avalonia.exe --api-server --port $Port" -ForegroundColor Gray
Write-Host ""

# Test 1: Health Check
Write-Host "► TEST 1: Health Check" -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -Method Get -ErrorAction Stop
    Write-Host "  ✅ PASS: Server is healthy" -ForegroundColor Green
    Write-Host "     Status: $($health.status)" -ForegroundColor Gray
    Write-Host "     PDFium: $($health.pdfiumInitialized)" -ForegroundColor Gray
    Write-Host "     Version: $($health.version)" -ForegroundColor Gray
}
catch {
    Write-Host "  ❌ FAIL: Cannot connect to server" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure FluentPDF is running with:" -ForegroundColor Yellow
    Write-Host "  FluentPDF.Avalonia.exe --api-server --port $Port" -ForegroundColor Yellow
    exit 1
}

# Test 2: GUI State
Write-Host ""
Write-Host "► TEST 2: Initial GUI State" -ForegroundColor Cyan
try {
    $state = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/state" -Method Get
    Write-Host "  ✅ PASS: GUI state retrieved" -ForegroundColor Green
    Write-Host "     Tabs: $($state.tabCount)" -ForegroundColor Gray
    Write-Host "     Active: $($state.hasActiveTabs)" -ForegroundColor Gray
}
catch {
    Write-Host "  ❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Open PDF
if (Test-Path $PdfPath) {
    Write-Host ""
    Write-Host "► TEST 3: Open PDF File" -ForegroundColor Cyan
    Write-Host "     File: $PdfPath" -ForegroundColor Gray

    try {
        $body = @{
            filePath = $PdfPath
        } | ConvertTo-Json

        $result = Invoke-RestMethod `
            -Uri "http://localhost:$Port/api/gui/action/open-file" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 60

        if ($result.success) {
            Write-Host "  ✅ PASS: File opened successfully" -ForegroundColor Green
            Write-Host "     Pages: $($result.pageCount)" -ForegroundColor Gray
            Write-Host "     Tab Created: $($result.tabCreated)" -ForegroundColor Gray
            Write-Host "     Doc Loaded: $($result.documentLoaded)" -ForegroundColor Gray

            # Test 4: Verify Document Loaded
            Write-Host ""
            Write-Host "► TEST 4: Verify Document Loaded" -ForegroundColor Cyan
            Start-Sleep -Seconds 2
            $verify = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/verify/document-loaded"

            if ($verify.isLoaded) {
                Write-Host "  ✅ PASS: Document verified loaded" -ForegroundColor Green
                Write-Host "     Pages: $($verify.totalPages)" -ForegroundColor Gray
                Write-Host "     Current Page: $($verify.currentPage)" -ForegroundColor Gray
            }
            else {
                Write-Host "  ❌ FAIL: Document not loaded" -ForegroundColor Red
                Write-Host "     Reason: $($verify.reason)" -ForegroundColor Red
            }

            # Test 5: Verify Page Rendered
            Write-Host ""
            Write-Host "► TEST 5: Verify Page Rendered" -ForegroundColor Cyan
            Start-Sleep -Seconds 1
            $rendered = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/verify/page-rendered"

            if ($rendered.isRendered) {
                Write-Host "  ✅ PASS: Page rendered successfully" -ForegroundColor Green
                Write-Host "     Page: $($rendered.currentPage)" -ForegroundColor Gray
                Write-Host "     Size: $($rendered.imageWidth)x$($rendered.imageHeight)" -ForegroundColor Gray
            }
            else {
                Write-Host "  ❌ FAIL: Page not rendered" -ForegroundColor Red
                Write-Host "     Has Image: $($rendered.hasImage)" -ForegroundColor Red
                Write-Host "     Status: $($rendered.statusMessage)" -ForegroundColor Red
            }

            # Test 6: Viewer State
            Write-Host ""
            Write-Host "► TEST 6: Viewer State" -ForegroundColor Cyan
            $viewer = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/viewer/state"

            Write-Host "  ✅ Retrieved viewer state" -ForegroundColor Green
            Write-Host "     Has Document: $($viewer.hasDocument)" -ForegroundColor Gray
            Write-Host "     Is Loading: $($viewer.isLoading)" -ForegroundColor Gray
            Write-Host "     Current Page: $($viewer.currentPage) / $($viewer.totalPages)" -ForegroundColor Gray
            Write-Host "     Has Rendered Image: $($viewer.hasRenderedImage)" -ForegroundColor Gray
            Write-Host "     Image Size: $($viewer.imageWidth)x$($viewer.imageHeight)" -ForegroundColor Gray
            Write-Host "     View Mode: $($viewer.viewMode)" -ForegroundColor Gray

            if ($viewer.hasDocument -and $viewer.hasRenderedImage -and -not $viewer.isLoading) {
                Write-Host ""
                Write-Host "  🎉 SUCCESS: PDF loaded and rendered!" -ForegroundColor Green -BackgroundColor DarkGreen
            }
            else {
                Write-Host ""
                Write-Host "  ⚠️  WARNING: PDF loaded but rendering incomplete" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  ❌ FAIL: File open failed" -ForegroundColor Red
            Write-Host "     Message: $($result.message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  ❌ FAIL: API error" -ForegroundColor Red
        Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
else {
    Write-Host ""
    Write-Host "⚠️  SKIP: PDF file not found: $PdfPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
