#!/usr/bin/env pwsh
# Autonomous PDF Rendering Test - Tests PDF loading and rendering via REST API

param(
    [string]$PdfPath = "",
    [int]$Port = 5000,
    [string]$OutputDir = "test-results",
    [switch]$StartServer,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Colors
$colors = @{
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Success = "Green"
    Debug = "Gray"
}

# Test results
$script:testsPassed = 0
$script:testsFailed = 0
$script:testsSkipped = 0

function Write-TestHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info
    Write-Host "  $Message" -ForegroundColor $colors.Info
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info
}

function Write-TestStep {
    param([string]$Message)
    Write-Host "  ▸ $Message" -ForegroundColor $colors.Info
}

function Write-TestPass {
    param([string]$Message)
    $script:testsPassed++
    Write-Host "  ✅ PASS: $Message" -ForegroundColor $colors.Success
}

function Write-TestFail {
    param([string]$Message, [string]$Details = "")
    $script:testsFailed++
    Write-Host "  ❌ FAIL: $Message" -ForegroundColor $colors.Error
    if ($Details) {
        Write-Host "     Details: $Details" -ForegroundColor $colors.Debug
    }
}

function Write-TestSkip {
    param([string]$Message)
    $script:testsSkipped++
    Write-Host "  ⚠️  SKIP: $Message" -ForegroundColor $colors.Warning
}

function Start-FluentPdfServer {
    param([int]$Port, [string]$ProjectPath)

    Write-TestStep "Starting FluentPDF API server on port $Port..."

    # Try Release first, then Debug
    $exePath = Join-Path $ProjectPath "src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"

    if (-not (Test-Path $exePath)) {
        $exePath = Join-Path $ProjectPath "src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe"
    }

    if (-not (Test-Path $exePath)) {
        Write-TestFail "Executable not found" "Tried Release and Debug paths. Please build first: dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Release"
        return $null
    }

    Write-Host "     Using executable: $exePath" -ForegroundColor $colors.Debug

    $process = Start-Process -FilePath $exePath `
        -ArgumentList "--api-server", "--port", $Port, "--headless" `
        -PassThru `
        -WindowStyle Hidden

    # Wait for server to start
    Write-TestStep "Waiting for server to initialize..."
    Start-Sleep -Seconds 5

    # Verify server is running
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -Method Get -TimeoutSec 10
        Write-TestPass "Server started successfully (PID: $($process.Id))"
        return $process
    }
    catch {
        Write-TestFail "Server failed to start" $_.Exception.Message
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        return $null
    }
}

function Test-HealthEndpoint {
    param([int]$Port)

    Write-TestStep "Testing /api/health endpoint..."

    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -Method Get

        if ($response.status -eq "healthy") {
            Write-TestPass "Health check passed (Status: $($response.status), PDFium: $($response.pdfiumInitialized))"
            return $true
        }
        else {
            Write-TestFail "Health check failed" "Status: $($response.status)"
            return $false
        }
    }
    catch {
        Write-TestFail "Health endpoint error" $_.Exception.Message
        return $false
    }
}

function Test-OpenFileInGui {
    param([int]$Port, [string]$FilePath)

    Write-TestStep "Testing /api/gui/action/open-file..."

    if (-not (Test-Path $FilePath)) {
        Write-TestFail "PDF file not found" $FilePath
        return $false
    }

    try {
        $body = @{
            filePath = $FilePath
        } | ConvertTo-Json

        $response = Invoke-RestMethod `
            -Uri "http://localhost:$Port/api/gui/action/open-file" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 60

        if ($response.success -and $response.documentLoaded) {
            Write-TestPass "File opened in GUI (Pages: $($response.pageCount))"
            return $true
        }
        else {
            Write-TestFail "File open failed" "Success: $($response.success), DocumentLoaded: $($response.documentLoaded)"
            return $false
        }
    }
    catch {
        Write-TestFail "Open file API error" $_.Exception.Message
        return $false
    }
}

function Test-VerifyDocumentLoaded {
    param([int]$Port, [string]$ExpectedPath)

    Write-TestStep "Testing /api/gui/verify/document-loaded..."

    try {
        $uri = "http://localhost:$Port/api/gui/verify/document-loaded"
        if ($ExpectedPath) {
            $uri += "?expectedPath=" + [System.Web.HttpUtility]::UrlEncode($ExpectedPath)
        }

        $response = Invoke-RestMethod -Uri $uri -Method Get

        if ($response.isLoaded) {
            Write-TestPass "Document verified loaded (Pages: $($response.totalPages), CurrentPage: $($response.currentPage))"
            return $true
        }
        else {
            Write-TestFail "Document not loaded" "Reason: $($response.reason)"
            return $false
        }
    }
    catch {
        Write-TestFail "Verify document API error" $_.Exception.Message
        return $false
    }
}

function Test-VerifyPageRendered {
    param([int]$Port, [int]$PageNumber = 0)

    Write-TestStep "Testing /api/gui/verify/page-rendered..."

    try {
        $uri = "http://localhost:$Port/api/gui/verify/page-rendered"
        if ($PageNumber -gt 0) {
            $uri += "?pageNumber=$PageNumber"
        }

        $response = Invoke-RestMethod -Uri $uri -Method Get

        if ($response.isRendered) {
            Write-TestPass "Page rendered successfully (Page: $($response.currentPage), Size: $($response.imageWidth)x$($response.imageHeight))"
            return $true
        }
        else {
            Write-TestFail "Page not rendered" "HasImage: $($response.hasImage), CurrentPage: $($response.currentPage), Status: $($response.statusMessage)"
            return $false
        }
    }
    catch {
        Write-TestFail "Verify page API error" $_.Exception.Message
        return $false
    }
}

function Test-GuiState {
    param([int]$Port)

    Write-TestStep "Testing /api/gui/state..."

    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/state" -Method Get

        Write-Host "     TabCount: $($response.tabCount)" -ForegroundColor $colors.Debug
        Write-Host "     HasActiveTabs: $($response.hasActiveTabs)" -ForegroundColor $colors.Debug

        if ($response.activeTab) {
            Write-Host "     ActiveTab: $($response.activeTab.fileName)" -ForegroundColor $colors.Debug
        }

        Write-TestPass "GUI state retrieved successfully"
        return $true
    }
    catch {
        Write-TestFail "GUI state API error" $_.Exception.Message
        return $false
    }
}

function Test-ViewerState {
    param([int]$Port)

    Write-TestStep "Testing /api/gui/viewer/state..."

    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/gui/viewer/state" -Method Get

        Write-Host "     HasDocument: $($response.hasDocument)" -ForegroundColor $colors.Debug
        Write-Host "     IsLoading: $($response.isLoading)" -ForegroundColor $colors.Debug
        Write-Host "     CurrentPage: $($response.currentPage) / $($response.totalPages)" -ForegroundColor $colors.Debug
        Write-Host "     ZoomLevel: $($response.zoomLevel)" -ForegroundColor $colors.Debug
        Write-Host "     ImageSize: $($response.imageWidth)x$($response.imageHeight)" -ForegroundColor $colors.Debug
        Write-Host "     HasRenderedImage: $($response.hasRenderedImage)" -ForegroundColor $colors.Debug

        if ($response.hasDocument -and $response.hasRenderedImage) {
            Write-TestPass "Viewer state verified (Document loaded and page rendered)"
            return $true
        }
        else {
            Write-TestFail "Viewer state incomplete" "HasDocument: $($response.hasDocument), HasImage: $($response.hasRenderedImage)"
            return $false
        }
    }
    catch {
        Write-TestFail "Viewer state API error" $_.Exception.Message
        return $false
    }
}

function Save-TestResults {
    param([int]$Port, [string]$OutputDir)

    Write-TestStep "Saving test results..."

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $resultFile = Join-Path $OutputDir "test-results-$timestamp.json"

    $results = @{
        Timestamp = Get-Date -Format "o"
        Port = $Port
        TestsPassed = $script:testsPassed
        TestsFailed = $script:testsFailed
        TestsSkipped = $script:testsSkipped
        TotalTests = $script:testsPassed + $script:testsFailed + $script:testsSkipped
        SuccessRate = if (($script:testsPassed + $script:testsFailed) -gt 0) {
            [math]::Round(($script:testsPassed / ($script:testsPassed + $script:testsFailed)) * 100, 2)
        } else { 0 }
    }

    $results | ConvertTo-Json | Set-Content -Path $resultFile
    Write-TestPass "Test results saved to $resultFile"
}

# Main execution
try {
    Write-TestHeader "FLUENTPDF AUTONOMOUS RENDERING TEST"
    Write-Host "Port: $Port" -ForegroundColor $colors.Debug
    Write-Host "PDF: $PdfPath" -ForegroundColor $colors.Debug
    Write-Host "Output: $OutputDir" -ForegroundColor $colors.Debug
    Write-Host ""

    # Find project root (tools is at project-root/tools, so go up one level)
    $projectRoot = Split-Path -Parent $PSScriptRoot

    # Start server if requested
    $serverProcess = $null
    if ($StartServer) {
        Write-TestHeader "STEP 1: Starting Server"
        $serverProcess = Start-FluentPdfServer -Port $Port -ProjectPath $projectRoot

        if (-not $serverProcess) {
            Write-Host ""
            Write-Host "❌ FATAL: Failed to start server" -ForegroundColor $colors.Error
            exit 1
        }
    }

    # Test 1: Health Check
    Write-TestHeader "STEP 2: Health Check"
    $healthOk = Test-HealthEndpoint -Port $Port

    if (-not $healthOk) {
        Write-Host ""
        Write-Host "❌ FATAL: Health check failed - server not responding" -ForegroundColor $colors.Error
        exit 1
    }

    # Test 2: GUI State (empty)
    Write-TestHeader "STEP 3: Initial GUI State"
    Test-GuiState -Port $Port

    # Test 3: Open PDF
    if ($PdfPath) {
        Write-TestHeader "STEP 4: Open PDF File"
        $openOk = Test-OpenFileInGui -Port $Port -FilePath $PdfPath

        # Test 4: Verify Document Loaded
        if ($openOk) {
            Write-TestHeader "STEP 5: Verify Document Loaded"
            Test-VerifyDocumentLoaded -Port $Port -ExpectedPath $PdfPath

            # Test 5: Verify Page Rendered
            Write-TestHeader "STEP 6: Verify Page Rendered"
            Test-VerifyPageRendered -Port $Port

            # Test 6: GUI State (with document)
            Write-TestHeader "STEP 7: GUI State After Load"
            Test-GuiState -Port $Port

            # Test 7: Viewer State
            Write-TestHeader "STEP 8: Viewer State"
            Test-ViewerState -Port $Port
        }
    }
    else {
        Write-TestSkip "PDF path not provided - skipping file tests"
    }

    # Save results
    Write-TestHeader "STEP 9: Save Results"
    Save-TestResults -Port $Port -OutputDir $OutputDir

    # Summary
    Write-TestHeader "TEST SUMMARY"
    Write-Host ""
    Write-Host "  Total Tests: $($script:testsPassed + $script:testsFailed + $script:testsSkipped)" -ForegroundColor $colors.Info
    Write-Host "  ✅ Passed: $script:testsPassed" -ForegroundColor $colors.Success
    Write-Host "  ❌ Failed: $script:testsFailed" -ForegroundColor $colors.Error
    Write-Host "  ⚠️  Skipped: $script:testsSkipped" -ForegroundColor $colors.Warning

    if ($script:testsFailed -gt 0) {
        Write-Host ""
        Write-Host "  ❌ OVERALL: FAILED" -ForegroundColor $colors.Error
        $exitCode = 1
    }
    else {
        Write-Host ""
        Write-Host "  ✅ OVERALL: PASSED" -ForegroundColor $colors.Success
        $exitCode = 0
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info

}
finally {
    # Cleanup: Stop server if we started it
    if ($StartServer -and $serverProcess -and -not $serverProcess.HasExited) {
        Write-Host ""
        Write-Host "Stopping server (PID: $($serverProcess.Id))..." -ForegroundColor $colors.Debug
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
        Write-Host "Server stopped" -ForegroundColor $colors.Success
    }
}

exit $exitCode
