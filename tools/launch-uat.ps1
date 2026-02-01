# FluentPDF UAT Launch Script
# User Acceptance Testing Helper

param(
    [switch]$BuildFirst,
    [switch]$ApiServer,
    [switch]$RunTests,
    [string]$TestPdf = "tests\Fixtures\sample.pdf"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "FluentPDF UAT Launch Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Detect executable path
$exePath = "src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Executable not found. Checking Release build..." -ForegroundColor Yellow
    $exePath = "src\FluentPDF.App\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: FluentPDF.App.exe not found!" -ForegroundColor Red
    Write-Host "Build the project first with:" -ForegroundColor Yellow
    Write-Host "  dotnet build src\FluentPDF.App -p:Platform=x64" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found executable: $exePath" -ForegroundColor Green
Write-Host ""

# Build if requested
if ($BuildFirst) {
    Write-Host "Building FluentPDF.App..." -ForegroundColor Cyan
    dotnet build src\FluentPDF.App -p:Platform=x64
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host ""
}

# Run autonomous tests if requested
if ($RunTests) {
    Write-Host "Running autonomous tests..." -ForegroundColor Cyan
    Write-Host ""

    # Start API server in background
    Write-Host "Starting API server..." -ForegroundColor Yellow
    $apiJob = Start-Job -ScriptBlock {
        param($exePath)
        & $exePath --api-server --port 5000
    } -ArgumentList $exePath

    Start-Sleep -Seconds 3

    # Check API health
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -TimeoutSec 5
        Write-Host "API Server Status: $($response.status)" -ForegroundColor Green
        Write-Host "Version: $($response.version)" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host "API server failed to start!" -ForegroundColor Red
        Stop-Job $apiJob
        Remove-Job $apiJob
        exit 1
    }

    # Run tests
    Write-Host "Running test suite..." -ForegroundColor Cyan
    & pwsh tools\run-autonomous-tests.ps1

    # Stop API server
    Write-Host ""
    Write-Host "Stopping API server..." -ForegroundColor Yellow
    Stop-Job $apiJob
    Remove-Job $apiJob

    Write-Host "Tests complete!" -ForegroundColor Green
    exit 0
}

# Launch modes
if ($ApiServer) {
    Write-Host "Launching FluentPDF in API Server mode..." -ForegroundColor Cyan
    Write-Host "API Endpoints will be available at: http://localhost:5000" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
    Write-Host ""

    & $exePath --api-server --port 5000
}
else {
    Write-Host "Launching FluentPDF UI..." -ForegroundColor Cyan
    Write-Host ""

    if (Test-Path $TestPdf) {
        Write-Host "Opening test PDF: $TestPdf" -ForegroundColor Green
        & $exePath $TestPdf
    }
    else {
        Write-Host "Test PDF not found: $TestPdf" -ForegroundColor Yellow
        Write-Host "Launching without document..." -ForegroundColor Yellow
        & $exePath
    }
}
