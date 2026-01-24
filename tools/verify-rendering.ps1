#!/usr/bin/env pwsh
# Autonomous PDF Rendering Verification Script
# Starts the API server, loads a test PDF, verifies rendering, and reports results.
# Exit code 0 on success, 1 on failure.

param(
    [string]$PdfPath = "tests/Fixtures/sample-with-text.pdf",
    [int]$Port = 5000,
    [int]$TimeoutSeconds = 30,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:$Port"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = switch ($Level) {
        "INFO"    { "Cyan" }
        "SUCCESS" { "Green" }
        "ERROR"   { "Red" }
        "WARN"    { "Yellow" }
        default   { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Wait-ForServer {
    param([int]$MaxWaitSeconds = 30)
    $start = Get-Date
    while ((Get-Date) - $start -lt [TimeSpan]::FromSeconds($MaxWaitSeconds)) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.status -eq "healthy") {
                return $true
            }
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
}

function Stop-ApiServer {
    param($Process)
    if ($Process -and !$Process.HasExited) {
        Write-Log "Stopping API server..." "INFO"
        try {
            $Process.Kill()
            $Process.WaitForExit(5000)
        } catch {
            Write-Log "Failed to stop server gracefully" "WARN"
        }
    }
}

# Main script
Write-Log "FluentPDF Autonomous Rendering Verification" "INFO"
Write-Log "============================================" "INFO"

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$appExe = Join-Path $repoRoot "src/FluentPDF.App/bin/x64/Debug/net9.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
$testPdf = Join-Path $repoRoot $PdfPath

# Validate prerequisites
if (!(Test-Path $appExe)) {
    Write-Log "FluentPDF.App.exe not found at: $appExe" "ERROR"
    Write-Log "Please build the project first: dotnet build src/FluentPDF.App -p:Platform=x64" "ERROR"
    exit 1
}

if (!(Test-Path $testPdf)) {
    Write-Log "Test PDF not found at: $testPdf" "ERROR"
    exit 1
}

$serverProcess = $null
$exitCode = 0

try {
    # Start API server
    Write-Log "Starting API server on port $Port..." "INFO"
    $serverProcess = Start-Process -FilePath $appExe -ArgumentList "--api-server", "--port", $Port, "--headless", "--console" -PassThru -NoNewWindow

    # Wait for server to be ready
    Write-Log "Waiting for server to be ready..." "INFO"
    if (!(Wait-ForServer -MaxWaitSeconds $TimeoutSeconds)) {
        Write-Log "Server failed to start within $TimeoutSeconds seconds" "ERROR"
        exit 1
    }
    Write-Log "Server is ready" "SUCCESS"

    # Health check
    Write-Log "Checking health..." "INFO"
    $health = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get
    Write-Log "Health: $($health.status), Version: $($health.version), PDFium: $($health.pdfiumLoaded)" "INFO"

    # Load document
    Write-Log "Loading PDF: $testPdf" "INFO"
    $loadBody = @{ path = $testPdf } | ConvertTo-Json
    $loadResponse = Invoke-RestMethod -Uri "$BaseUrl/api/document/load" -Method Post -Body $loadBody -ContentType "application/json"
    $documentId = $loadResponse.documentId
    $pageCount = $loadResponse.pageCount
    Write-Log "Document loaded: ID=$documentId, Pages=$pageCount" "SUCCESS"

    # Render and verify each page
    $allPassed = $true
    for ($i = 0; $i -lt $pageCount; $i++) {
        Write-Log "Rendering page $($i + 1)/$pageCount..." "INFO"

        try {
            $renderResponse = Invoke-WebRequest -Uri "$BaseUrl/api/render/$documentId/$i" -Method Get -TimeoutSec 30
            $renderTimeMs = $renderResponse.Headers["X-Render-Time-Ms"]
            $contentLength = $renderResponse.Headers["Content-Length"]

            if ($renderResponse.StatusCode -eq 200 -and $renderResponse.Content.Length -gt 0) {
                Write-Log "Page $($i + 1): SUCCESS (${renderTimeMs}ms, $contentLength bytes)" "SUCCESS"
            } else {
                Write-Log "Page $($i + 1): FAILED (empty response)" "ERROR"
                $allPassed = $false
            }
        } catch {
            Write-Log "Page $($i + 1): FAILED ($_)" "ERROR"
            $allPassed = $false
        }
    }

    # Verify endpoint test
    Write-Log "Testing verification endpoint..." "INFO"
    $verifyBody = @{
        documentId = $documentId
        pageIndex = 0
    } | ConvertTo-Json
    $verifyResponse = Invoke-RestMethod -Uri "$BaseUrl/api/verify/render" -Method Post -Body $verifyBody -ContentType "application/json"
    Write-Log "Verify: Hash=$($verifyResponse.hash.Substring(0, 16))..." "INFO"

    # Close document
    Write-Log "Closing document..." "INFO"
    Invoke-RestMethod -Uri "$BaseUrl/api/document/$documentId" -Method Delete | Out-Null
    Write-Log "Document closed" "SUCCESS"

    # Final result
    if ($allPassed) {
        Write-Log "============================================" "INFO"
        Write-Log "ALL TESTS PASSED" "SUCCESS"
        $exitCode = 0
    } else {
        Write-Log "============================================" "INFO"
        Write-Log "SOME TESTS FAILED" "ERROR"
        $exitCode = 1
    }

} catch {
    Write-Log "Verification failed: $_" "ERROR"
    $exitCode = 1
} finally {
    Stop-ApiServer -Process $serverProcess
}

exit $exitCode
