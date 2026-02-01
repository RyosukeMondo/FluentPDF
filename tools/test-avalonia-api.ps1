#!/usr/bin/env pwsh
# Test script for FluentPDF Avalonia REST API

param(
    [string]$PdfPath = "tests/Fixtures/sample.pdf",
    [int]$Port = 5000,
    [string]$ExePath = "src/FluentPDF.Avalonia/bin/Debug/net8.0/FluentPDF.Avalonia.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== FluentPDF Avalonia API Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if executable exists
if (-not (Test-Path $ExePath)) {
    Write-Host "ERROR: Executable not found at: $ExePath" -ForegroundColor Red
    Write-Host "Please build the project first: dotnet build src/FluentPDF.Avalonia" -ForegroundColor Yellow
    exit 1
}

# Check if PDF exists
if (-not (Test-Path $PdfPath)) {
    Write-Host "ERROR: PDF file not found at: $PdfPath" -ForegroundColor Red
    exit 1
}

$PdfPath = Resolve-Path $PdfPath

# Start API server in background
Write-Host "Starting API server on port $Port..." -ForegroundColor Green
$serverProcess = Start-Process -FilePath $ExePath -ArgumentList "--api-server --port $Port --headless" -PassThru -NoNewWindow

# Wait for server to start
Write-Host "Waiting for server to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$baseUrl = "http://localhost:$Port"

try {
    # Test 1: Health check
    Write-Host ""
    Write-Host "Test 1: Health Check" -ForegroundColor Cyan
    $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get
    Write-Host "  Status: $($health.status)" -ForegroundColor Green
    Write-Host "  Version: $($health.version)" -ForegroundColor Green
    Write-Host "  PDFium Loaded: $($health.pdfiumLoaded)" -ForegroundColor Green
    Write-Host "  Active Sessions: $($health.activeSessions)" -ForegroundColor Green

    if ($health.status -ne "healthy") {
        throw "Health check failed: $($health.status)"
    }

    # Test 2: Load document
    Write-Host ""
    Write-Host "Test 2: Load Document" -ForegroundColor Cyan
    $loadRequest = @{
        path = $PdfPath.ToString()
    } | ConvertTo-Json

    $loadResponse = Invoke-RestMethod -Uri "$baseUrl/api/document/load" -Method Post -Body $loadRequest -ContentType "application/json"
    Write-Host "  Document ID: $($loadResponse.documentId)" -ForegroundColor Green
    Write-Host "  Page Count: $($loadResponse.pageCount)" -ForegroundColor Green
    Write-Host "  Width: $($loadResponse.metadata.width)" -ForegroundColor Green
    Write-Host "  Height: $($loadResponse.metadata.height)" -ForegroundColor Green

    $documentId = $loadResponse.documentId

    # Test 3: Get document info
    Write-Host ""
    Write-Host "Test 3: Get Document Info" -ForegroundColor Cyan
    $docInfo = Invoke-RestMethod -Uri "$baseUrl/api/document/$documentId" -Method Get
    Write-Host "  Page Count: $($docInfo.pageCount)" -ForegroundColor Green

    # Test 4: Render page
    Write-Host ""
    Write-Host "Test 4: Render Page 0" -ForegroundColor Cyan
    $outputFile = "test-page-0.png"
    Invoke-WebRequest -Uri "$baseUrl/api/render/$documentId/0" -OutFile $outputFile
    $fileInfo = Get-Item $outputFile
    Write-Host "  Rendered to: $outputFile" -ForegroundColor Green
    Write-Host "  File size: $($fileInfo.Length) bytes" -ForegroundColor Green

    # Test 5: Verify rendering
    Write-Host ""
    Write-Host "Test 5: Verify Rendering (no baseline)" -ForegroundColor Cyan
    $verifyRequest = @{
        documentId = $documentId
        pageIndex = 0
    } | ConvertTo-Json

    $verifyResponse = Invoke-RestMethod -Uri "$baseUrl/api/verify/render" -Method Post -Body $verifyRequest -ContentType "application/json"
    Write-Host "  Match: $($verifyResponse.match)" -ForegroundColor Green
    Write-Host "  Hash: $($verifyResponse.hash)" -ForegroundColor Green

    $baselineHash = $verifyResponse.hash

    # Test 6: Verify with baseline (should match)
    Write-Host ""
    Write-Host "Test 6: Verify with Baseline (should match)" -ForegroundColor Cyan
    $verifyRequest2 = @{
        documentId = $documentId
        pageIndex = 0
        baselineHash = $baselineHash
    } | ConvertTo-Json

    $verifyResponse2 = Invoke-RestMethod -Uri "$baseUrl/api/verify/render" -Method Post -Body $verifyRequest2 -ContentType "application/json"
    Write-Host "  Match: $($verifyResponse2.match)" -ForegroundColor Green
    Write-Host "  Hash: $($verifyResponse2.hash)" -ForegroundColor Green

    if (-not $verifyResponse2.match) {
        throw "Verification failed: hashes don't match"
    }

    # Test 7: Batch verify
    Write-Host ""
    Write-Host "Test 7: Batch Verify" -ForegroundColor Cyan
    $batchRequest = @{
        documentId = $documentId
        baselines = @{
            "0" = $baselineHash
            "1" = $baselineHash
        }
    } | ConvertTo-Json

    $batchResponse = Invoke-RestMethod -Uri "$baseUrl/api/verify/batch" -Method Post -Body $batchRequest -ContentType "application/json"
    Write-Host "  All Match: $($batchResponse.allMatch)" -ForegroundColor Green
    Write-Host "  Results Count: $($batchResponse.results.Count)" -ForegroundColor Green
    Write-Host "  Failures: $($batchResponse.failures.Count)" -ForegroundColor Green

    # Test 8: Close document
    Write-Host ""
    Write-Host "Test 8: Close Document" -ForegroundColor Cyan
    $closeResponse = Invoke-RestMethod -Uri "$baseUrl/api/document/$documentId" -Method Delete
    Write-Host "  Closed: $($closeResponse.closed)" -ForegroundColor Green

    # Success
    Write-Host ""
    Write-Host "=== ALL TESTS PASSED ===" -ForegroundColor Green
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
} finally {
    # Stop server
    Write-Host "Stopping API server..." -ForegroundColor Yellow
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Server stopped" -ForegroundColor Green
}
