#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Autonomous Test Orchestration Script for FluentPDF

.DESCRIPTION
    Orchestrates comprehensive test suite including:
    - CLI verification commands (merge, split, optimize, watermark, annotations, forms, conversion)
    - REST API verification tests (health, document, render, verify endpoints)
    - Theme and annotation tests
    - Consolidated JSON reporting

.PARAMETER Port
    API server port (default: 5000)

.PARAMETER TimeoutSeconds
    Maximum wait time for API server startup (default: 30)

.PARAMETER OutputDir
    Directory for test reports (default: tests/reports)

.PARAMETER Verbose
    Enable detailed logging

.PARAMETER NoCleanup
    Keep test artifacts and server running

.EXAMPLE
    pwsh tools/run-autonomous-tests.ps1
    pwsh tools/run-autonomous-tests.ps1 -Port 8080 -Verbose

.EXIT CODES
    0 = All tests passed
    1 = One or more tests failed
    2 = Setup/infrastructure failure
#>

param(
    [int]$Port = 5000,
    [int]$TimeoutSeconds = 30,
    [string]$OutputDir = "tests/reports",
    [switch]$Verbose,
    [switch]$NoCleanup
)

$ErrorActionPreference = "Stop"
$VerbosePreference = if ($Verbose) { "Continue" } else { "SilentlyContinue" }

# ============================================================================
# CONSTANTS
# ============================================================================

$BaseUrl = "http://localhost:$Port"
$TestStartTime = Get-Date
$TestResults = @{
    cliTests = @{}
    apiTests = @{}
    totalPassed = 0
    totalFailed = 0
    failures = @()
    skipped = @()
}

# ============================================================================
# LOGGING UTILITIES
# ============================================================================

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO", "SUCCESS", "ERROR", "WARN", "DEBUG")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $color = @{
        "INFO"    = "Cyan"
        "SUCCESS" = "Green"
        "ERROR"   = "Red"
        "WARN"    = "Yellow"
        "DEBUG"   = "Gray"
    }

    $output = "[$timestamp] [$Level] $Message"
    Write-Host $output -ForegroundColor $color[$Level]

    # Also write to log file
    $logFile = Join-Path $OutputDir "orchestration.log"
    Add-Content -Path $logFile -Value $output -ErrorAction SilentlyContinue
}

function Write-TestResult {
    param(
        [string]$TestName,
        [string]$Category,
        [bool]$Passed,
        [string]$Message = "",
        [hashtable]$Details = @{}
    )

    $result = @{
        name = $TestName
        category = $Category
        passed = $Passed
        message = $Message
        timestamp = Get-Date -Format "o"
        details = $Details
    }

    if ($Passed) {
        $TestResults.totalPassed++
        Write-Log "$Category > $TestName: PASSED" "SUCCESS"
    } else {
        $TestResults.totalFailed++
        $TestResults.failures += @{
            name = $TestName
            category = $Category
            message = $Message
            details = $Details
        }
        Write-Log "$Category > $TestName: FAILED - $Message" "ERROR"
    }

    if ($Category -eq "CLI") {
        $TestResults.cliTests[$TestName] = $result
    } else {
        $TestResults.apiTests[$TestName] = $result
    }
}

# ============================================================================
# INFRASTRUCTURE MANAGEMENT
# ============================================================================

function Initialize-TestEnvironment {
    Write-Log "Initializing test environment..." "INFO"

    # Create output directory
    if (!(Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        Write-Log "Created output directory: $OutputDir" "DEBUG"
    }

    # Verify project structure
    $appExe = Get-AppExecutablePath
    if (!(Test-Path $appExe)) {
        Write-Log "FluentPDF.App.exe not found at: $appExe" "ERROR"
        Write-Log "Build the project first: dotnet build src/FluentPDF.App -p:Platform=x64" "ERROR"
        return $false
    }

    # Get test fixtures
    $fixtures = Get-TestFixtures
    if ($fixtures.Count -eq 0) {
        Write-Log "No test fixtures found in tests/Fixtures/" "WARN"
        return $false
    }

    Write-Log "Test environment initialized successfully" "SUCCESS"
    return $true
}

function Get-AppExecutablePath {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
    Join-Path $repoRoot "src/FluentPDF.App/bin/x64/Debug/net9.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
}

function Get-TestFixtures {
    $fixturesDir = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "tests/Fixtures"
    Get-ChildItem -Path $fixturesDir -Filter "*.pdf" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
}

function Start-ApiServer {
    Write-Log "Starting API server on port $Port..." "INFO"

    $appExe = Get-AppExecutablePath

    try {
        $script:ServerProcess = Start-Process -FilePath $appExe `
            -ArgumentList "--api-server", "--port", $Port, "--headless", "--console" `
            -PassThru -NoNewWindow -ErrorAction Stop

        Write-Log "API server process started (PID: $($script:ServerProcess.Id))" "DEBUG"

        # Wait for server to be ready
        if (!(Wait-ForServerReady -MaxWaitSeconds $TimeoutSeconds)) {
            Write-Log "Server failed to start within $TimeoutSeconds seconds" "ERROR"
            Stop-ApiServer
            return $false
        }

        Write-Log "API server is ready" "SUCCESS"
        return $true
    } catch {
        Write-Log "Failed to start API server: $_" "ERROR"
        return $false
    }
}

function Wait-ForServerReady {
    param([int]$MaxWaitSeconds = 30)

    $start = Get-Date
    $attempts = 0

    while ((Get-Date) - $start -lt [TimeSpan]::FromSeconds($MaxWaitSeconds)) {
        $attempts++
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.status -eq "healthy") {
                Write-Log "Server ready after $attempts attempts" "DEBUG"
                return $true
            }
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Write-Log "Server health check timeout after $attempts attempts" "ERROR"
    return $false
}

function Stop-ApiServer {
    if ($script:ServerProcess -and !$script:ServerProcess.HasExited) {
        Write-Log "Stopping API server (PID: $($script:ServerProcess.Id))..." "INFO"
        try {
            $script:ServerProcess.Kill()
            $script:ServerProcess.WaitForExit(5000)
            Write-Log "API server stopped gracefully" "SUCCESS"
        } catch {
            Write-Log "Failed to stop server gracefully: $_" "WARN"
        }
    }
}

# ============================================================================
# CLI TEST COMMANDS
# ============================================================================

function Invoke-CliCommand {
    param(
        [string]$CommandName,
        [string[]]$Arguments,
        [string]$Description
    )

    Write-Log "Running CLI test: $CommandName" "INFO"

    $appExe = Get-AppExecutablePath
    $outputFile = Join-Path $OutputDir "cli-$CommandName.json"

    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        # Run command and capture output
        $process = Start-Process -FilePath $appExe -ArgumentList $Arguments `
            -PassThru -NoNewWindow -RedirectStandardOutput $outputFile -Wait

        $stopwatch.Stop()
        $exitCode = $process.ExitCode

        $details = @{
            command = "$appExe $($Arguments -join ' ')"
            exitCode = $exitCode
            elapsedMs = $stopwatch.ElapsedMilliseconds
            outputFile = $outputFile
        }

        if ($exitCode -eq 0) {
            Write-TestResult -TestName $CommandName -Category "CLI" -Passed $true `
                -Message "Command executed successfully" -Details $details
            return $true
        } else {
            Write-TestResult -TestName $CommandName -Category "CLI" -Passed $false `
                -Message "Command failed with exit code $exitCode" -Details $details
            return $false
        }
    } catch {
        Write-TestResult -TestName $CommandName -Category "CLI" -Passed $false `
            -Message "Exception: $_" -Details @{ error = $_.Exception.Message }
        return $false
    }
}

function Run-CliTests {
    Write-Log "============================================" "INFO"
    Write-Log "PHASE 1: CLI VERIFICATION TESTS" "INFO"
    Write-Log "============================================" "INFO"

    $testFixtures = Get-TestFixtures
    if ($testFixtures.Count -eq 0) {
        Write-Log "No test fixtures available, skipping CLI tests" "WARN"
        $TestResults.skipped += "CLI tests (no fixtures)"
        return
    }

    $testPdf = $testFixtures[0]  # Use first fixture for basic tests

    # Test 1: Diagnostic command
    Invoke-CliCommand -CommandName "diagnostics" `
        -Arguments "--diagnostics" `
        -Description "System diagnostics"

    # Test 2: Test render command
    Invoke-CliCommand -CommandName "test-render" `
        -Arguments "--test-render", $testPdf `
        -Description "Test PDF rendering"

    # Test 3: Document editing operations
    Invoke-CliCommand -CommandName "test-merge" `
        -Arguments "--test-merge", $testPdf `
        -Description "Test PDF merge (F2.1.1)"

    Invoke-CliCommand -CommandName "test-split" `
        -Arguments "--test-split", $testPdf, "--split-ranges", "1-3,5" `
        -Description "Test PDF split (F2.1.2)"

    Invoke-CliCommand -CommandName "test-forms" `
        -Arguments "--test-forms", (Join-Path $TestDataDir "sample-form.pdf") `
        -Description "Test form field filling (F4.1.1-F4.1.3)"

    Invoke-CliCommand -CommandName "test-annotations-cmd" `
        -Arguments "--test-annotations-cmd", $testPdf `
        -Description "Test annotation creation (F3.1.1-F3.2.5)"

    Invoke-CliCommand -CommandName "test-watermark" `
        -Arguments "--test-watermark", $testPdf, "--watermark-text", "CONFIDENTIAL", "--watermark-opacity", "0.5" `
        -Description "Test text watermark (F5.2.1-F5.2.5)"
}

# ============================================================================
# REST API TEST HELPERS
# ============================================================================

function Invoke-ApiTest {
    param(
        [string]$TestName,
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body,
        [int]$ExpectedStatus = 200,
        [scriptblock]$Assertion
    )

    Write-Log "Running API test: $TestName" "INFO"

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $url = "$BaseUrl/api$Endpoint"
        $requestParams = @{
            Uri = $url
            Method = $Method
            TimeoutSec = 10
            ContentType = "application/json"
        }

        if ($Body) {
            $requestParams.Body = $Body | ConvertTo-Json
        }

        $response = Invoke-WebRequest @requestParams -ErrorAction Stop
        $stopwatch.Stop()

        # Try to parse response as JSON
        $responseContent = $null
        try {
            $responseContent = $response.Content | ConvertFrom-Json
        } catch {
            $responseContent = $response.Content
        }

        $statusOk = $response.StatusCode -eq $ExpectedStatus

        # Run custom assertion if provided
        $assertionPassed = $true
        if ($Assertion) {
            try {
                & $Assertion $responseContent
            } catch {
                $assertionPassed = $false
                Write-Log "Assertion failed: $_" "ERROR"
            }
        }

        $passed = $statusOk -and $assertionPassed

        $details = @{
            method = $Method
            endpoint = $Endpoint
            statusCode = $response.StatusCode
            expectedStatus = $ExpectedStatus
            elapsedMs = $stopwatch.ElapsedMilliseconds
            contentLength = $response.Content.Length
        }

        Write-TestResult -TestName $TestName -Category "API" -Passed $passed `
            -Message "Status: $($response.StatusCode), Time: $($stopwatch.ElapsedMilliseconds)ms" `
            -Details $details

        return @{
            passed = $passed
            content = $responseContent
            response = $response
            stopwatch = $stopwatch
        }
    } catch {
        $stopwatch.Stop()

        $details = @{
            method = $Method
            endpoint = $Endpoint
            error = $_.Exception.Message
            expectedStatus = $ExpectedStatus
            elapsedMs = $stopwatch.ElapsedMilliseconds
        }

        Write-TestResult -TestName $TestName -Category "API" -Passed $false `
            -Message "Exception: $($_.Exception.Message)" -Details $details

        return @{
            passed = $false
            error = $_.Exception.Message
            stopwatch = $stopwatch
        }
    }
}

# ============================================================================
# REST API TESTS
# ============================================================================

function Run-ApiTests {
    Write-Log "============================================" "INFO"
    Write-Log "PHASE 2: REST API VERIFICATION TESTS" "INFO"
    Write-Log "============================================" "INFO"

    # Test 1: Health Check
    Write-Log "Testing Health endpoint..." "INFO"
    $healthResult = Invoke-ApiTest -TestName "health-check" -Method GET -Endpoint "/health" `
        -Assertion { param($response)
            if (-not $response.status) { throw "No status in response" }
            if ($response.status -ne "healthy") { throw "Status is not healthy: $($response.status)" }
        }

    if (-not $healthResult.passed) {
        Write-Log "Health check failed, cannot continue with other tests" "ERROR"
        return
    }

    $healthResponse = $healthResult.content
    Write-Log "Health Check Details - Status: $($healthResponse.status), PDFium: $($healthResponse.pdfiumLoaded), Version: $($healthResponse.version)" "DEBUG"

    # Test 2: Load Document
    Write-Log "Testing Document Load endpoint..." "INFO"

    $testFixtures = Get-TestFixtures
    if ($testFixtures.Count -eq 0) {
        Write-Log "No test fixtures available, skipping document tests" "WARN"
        $TestResults.skipped += "Document loading tests"
        return
    }

    $testPdfPath = $testFixtures[0]

    $loadResult = Invoke-ApiTest -TestName "document-load" -Method POST -Endpoint "/document/load" `
        -Body @{ path = $testPdfPath } `
        -Assertion { param($response)
            if (-not $response.documentId) { throw "No documentId in response" }
            if ($response.pageCount -lt 1) { throw "Invalid page count: $($response.pageCount)" }
        }

    if (-not $loadResult.passed) {
        Write-Log "Document loading failed, cannot continue with render/verify tests" "ERROR"
        return
    }

    $documentId = $loadResult.content.documentId
    $pageCount = $loadResult.content.pageCount
    Write-Log "Document Loaded - ID: $documentId, Pages: $pageCount" "DEBUG"

    # Test 3: Get Document Info
    Write-Log "Testing Document Info endpoint..." "INFO"
    Invoke-ApiTest -TestName "document-info" -Method GET -Endpoint "/document/$documentId" `
        -Assertion { param($response)
            if (-not $response.documentId) { throw "No documentId in response" }
            if ($response.pageCount -ne $pageCount) { throw "Page count mismatch" }
        } | Out-Null

    # Test 4: Render Pages
    Write-Log "Testing Render endpoint..." "INFO"

    $rendersPassed = 0
    $rendersTotal = [Math]::Min($pageCount, 3)  # Test first 3 pages max

    for ($i = 0; $i -lt $rendersTotal; $i++) {
        $renderResult = Invoke-ApiTest -TestName "render-page-$i" -Method GET -Endpoint "/render/$documentId/$i" `
            -Assertion { param($response)
                # Response is binary PNG, not JSON
                # Just verify it's not empty
                if ($response.Content.Length -lt 100) {
                    throw "Response too small for valid PNG: $($response.Content.Length) bytes"
                }
            }

        if ($renderResult.passed) { $rendersPassed++ }
    }

    Write-Log "Rendered $rendersPassed/$rendersTotal pages successfully" "INFO"

    # Test 5: Verify Render
    Write-Log "Testing Verify endpoint..." "INFO"

    Invoke-ApiTest -TestName "verify-render" -Method POST -Endpoint "/verify/render" `
        -Body @{ documentId = $documentId; pageIndex = 0 } `
        -Assertion { param($response)
            if (-not $response.hash) { throw "No hash in response" }
            if ($response.hash.Length -lt 10) { throw "Invalid hash: $($response.hash)" }
        } | Out-Null

    # Test 6: Batch Verify
    Write-Log "Testing Batch Verify endpoint..." "INFO"

    $batchPages = @()
    for ($i = 0; $i -lt $rendersTotal; $i++) {
        $batchPages += @{ documentId = $documentId; pageIndex = $i }
    }

    Invoke-ApiTest -TestName "batch-verify" -Method POST -Endpoint "/verify/batch" `
        -Body @{ verifications = $batchPages } `
        -Assertion { param($response)
            if (-not $response.results) { throw "No results in response" }
            if ($response.results.Count -eq 0) { throw "Empty results" }
        } | Out-Null

    # Test 7: Close Document
    Write-Log "Testing Document Close endpoint..." "INFO"

    Invoke-ApiTest -TestName "document-close" -Method DELETE -Endpoint "/document/$documentId" `
        -Assertion { param($response)
            # Response should indicate success
        } | Out-Null
}

# ============================================================================
# THEME & VISUAL TESTS
# ============================================================================

function Run-ThemeTests {
    Write-Log "============================================" "INFO"
    Write-Log "PHASE 3: THEME & VISUAL VERIFICATION" "INFO"
    Write-Log "============================================" "INFO"

    # Note: These tests would verify visual appearance consistency
    # Implementation depends on theme API endpoints

    Write-Log "Theme tests - skipped (pending theme API implementation)" "WARN"
    $TestResults.skipped += "Theme tests"
}

function Run-AnnotationTests {
    Write-Log "============================================" "INFO"
    Write-Log "PHASE 4: ANNOTATION & MARKUP TESTS" "INFO"
    Write-Log "============================================" "INFO"

    # Note: These tests would verify annotation handling
    # Implementation depends on annotation API endpoints

    Write-Log "Annotation tests - skipped (pending annotation API implementation)" "WARN"
    $TestResults.skipped += "Annotation tests"
}

function Run-FormTests {
    Write-Log "============================================" "INFO"
    Write-Log "PHASE 5: FORM FIELD TESTS" "INFO"
    Write-Log "============================================" "INFO"

    # Note: These tests would verify form handling
    # Implementation depends on form API endpoints

    Write-Log "Form field tests - skipped (pending form API implementation)" "WARN"
    $TestResults.skipped += "Form tests"
}

# ============================================================================
# REPORTING
# ============================================================================

function Generate-JsonReport {
    param([string]$OutputPath)

    Write-Log "Generating JSON report..." "INFO"

    $elapsed = (Get-Date) - $TestStartTime

    $report = @{
        timestamp = Get-Date -Format "o"
        duration = @{
            totalSeconds = [int]$elapsed.TotalSeconds
            totalMinutes = [decimal]::Round($elapsed.TotalMinutes, 2)
        }
        summary = @{
            totalTests = $TestResults.totalPassed + $TestResults.totalFailed
            totalPassed = $TestResults.totalPassed
            totalFailed = $TestResults.totalFailed
            successRate = if (($TestResults.totalPassed + $TestResults.totalFailed) -gt 0) {
                [decimal]::Round(100 * $TestResults.totalPassed / ($TestResults.totalPassed + $TestResults.totalFailed), 2)
            } else {
                0
            }
        }
        skipped = $TestResults.skipped
        cliTests = $TestResults.cliTests
        apiTests = $TestResults.apiTests
        failures = $TestResults.failures
    }

    $report | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath
    Write-Log "JSON report saved to: $OutputPath" "SUCCESS"
    return $report
}

function Generate-HtmlReport {
    param(
        [string]$OutputPath,
        [hashtable]$Report
    )

    Write-Log "Generating HTML report..." "INFO"

    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>FluentPDF Test Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            padding: 30px;
        }
        h1 {
            color: #333;
            border-bottom: 3px solid #007acc;
            padding-bottom: 10px;
        }
        h2 {
            color: #555;
            margin-top: 30px;
            border-bottom: 1px solid #ddd;
            padding-bottom: 10px;
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 20px 0;
        }
        .summary-card {
            padding: 20px;
            border-radius: 6px;
            text-align: center;
            font-weight: bold;
        }
        .summary-card.passed {
            background: #e6f7ed;
            color: #27ae60;
            border-left: 4px solid #27ae60;
        }
        .summary-card.failed {
            background: #fadbd8;
            color: #e74c3c;
            border-left: 4px solid #e74c3c;
        }
        .summary-card.skipped {
            background: #fef5e7;
            color: #f39c12;
            border-left: 4px solid #f39c12;
        }
        .summary-card .number {
            font-size: 28px;
            display: block;
            margin: 10px 0;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }
        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }
        th {
            background: #f8f9fa;
            font-weight: 600;
            color: #333;
        }
        tr:hover {
            background: #f5f5f5;
        }
        .badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 3px;
            font-size: 12px;
            font-weight: bold;
        }
        .badge.pass {
            background: #d4edda;
            color: #155724;
        }
        .badge.fail {
            background: #f8d7da;
            color: #721c24;
        }
        .badge.skip {
            background: #fff3cd;
            color: #856404;
        }
        .duration {
            color: #666;
            font-size: 14px;
        }
        .failure-detail {
            background: #fff5f5;
            border-left: 4px solid #e74c3c;
            padding: 15px;
            margin: 10px 0;
            border-radius: 4px;
        }
        .failure-detail h4 {
            margin-top: 0;
            color: #e74c3c;
        }
        .code {
            background: #f4f4f4;
            padding: 8px;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 12px;
            overflow-x: auto;
        }
        .footer {
            text-align: center;
            color: #999;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>FluentPDF Test Report</h1>

        <div class="duration">
            Generated: $($Report.timestamp)
            Duration: $($Report.duration.totalMinutes) minutes
        </div>

        <h2>Summary</h2>
        <div class="summary">
            <div class="summary-card passed">
                <div>Passed</div>
                <span class="number">$($Report.summary.totalPassed)</span>
            </div>
            <div class="summary-card failed">
                <div>Failed</div>
                <span class="number">$($Report.summary.totalFailed)</span>
            </div>
            <div class="summary-card skipped">
                <div>Skipped</div>
                <span class="number">$($Report.skipped.Count)</span>
            </div>
            <div class="summary-card">
                <div>Success Rate</div>
                <span class="number">$($Report.summary.successRate)%</span>
            </div>
        </div>

        <h2>CLI Tests</h2>
        <table>
            <thead>
                <tr>
                    <th>Test Name</th>
                    <th>Status</th>
                    <th>Duration (ms)</th>
                    <th>Exit Code</th>
                </tr>
            </thead>
            <tbody>
"@

    # Add CLI test rows
    foreach ($test in $Report.cliTests.Values) {
        $badge = if ($test.passed) { '<span class="badge pass">PASS</span>' } else { '<span class="badge fail">FAIL</span>' }
        $exitCode = if ($test.details.exitCode -ne $null) { $test.details.exitCode } else { "-" }
        $html += @"
                <tr>
                    <td>$($test.name)</td>
                    <td>$badge</td>
                    <td>$($test.details.elapsedMs)</td>
                    <td>$exitCode</td>
                </tr>
"@
    }

    $html += @"
            </tbody>
        </table>

        <h2>API Tests</h2>
        <table>
            <thead>
                <tr>
                    <th>Test Name</th>
                    <th>Status</th>
                    <th>Duration (ms)</th>
                    <th>Endpoint</th>
                    <th>HTTP Status</th>
                </tr>
            </thead>
            <tbody>
"@

    # Add API test rows
    foreach ($test in $Report.apiTests.Values) {
        $badge = if ($test.passed) { '<span class="badge pass">PASS</span>' } else { '<span class="badge fail">FAIL</span>' }
        $endpoint = $test.details.endpoint
        $status = if ($test.details.statusCode) { $test.details.statusCode } else { "-" }
        $html += @"
                <tr>
                    <td>$($test.name)</td>
                    <td>$badge</td>
                    <td>$($test.details.elapsedMs)</td>
                    <td>$endpoint</td>
                    <td>$status</td>
                </tr>
"@
    }

    $html += "</tbody></table>"

    # Add failure details if any
    if ($Report.failures.Count -gt 0) {
        $html += "<h2>Failure Details</h2>"
        foreach ($failure in $Report.failures) {
            $html += @"
        <div class="failure-detail">
            <h4>$($failure.category) > $($failure.name)</h4>
            <p><strong>Message:</strong> $($failure.message)</p>
            <div class="code">$($failure.details | ConvertTo-Json)</div>
        </div>
"@
        }
    }

    # Add skipped tests
    if ($Report.skipped.Count -gt 0) {
        $html += "<h2>Skipped Tests</h2>"
        $html += "<ul>"
        foreach ($skipped in $Report.skipped) {
            $html += "<li>$skipped</li>"
        }
        $html += "</ul>"
    }

    $html += @"
        <div class="footer">
            <p>FluentPDF Autonomous Test Orchestration</p>
            <p>Generated on $($Report.timestamp)</p>
        </div>
    </div>
</body>
</html>
"@

    $html | Set-Content -Path $OutputPath
    Write-Log "HTML report saved to: $OutputPath" "SUCCESS"
}

function Print-ConsoleSummary {
    param([hashtable]$Report)

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "TEST EXECUTION SUMMARY" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "Passed:  $($Report.summary.totalPassed)" -ForegroundColor Green
    Write-Host "Failed:  $($Report.summary.totalFailed)" -ForegroundColor $(if ($Report.summary.totalFailed -gt 0) { "Red" } else { "Green" })
    Write-Host "Skipped: $($Report.skipped.Count)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Success Rate: $($Report.summary.successRate)%" -ForegroundColor $(if ($Report.summary.successRate -eq 100) { "Green" } else { "Yellow" })
    Write-Host "Duration: $($Report.duration.totalMinutes) minutes"
    Write-Host ""

    if ($Report.failures.Count -gt 0) {
        Write-Host "Failed Tests:" -ForegroundColor Red
        foreach ($failure in $Report.failures) {
            Write-Host "  - $($failure.category) > $($failure.name): $($failure.message)" -ForegroundColor Red
        }
        Write-Host ""
    }

    Write-Host "Reports:" -ForegroundColor Cyan
    Write-Host "  JSON: $(Join-Path $OutputDir 'test-report.json')"
    Write-Host "  HTML: $(Join-Path $OutputDir 'test-report.html')"
    Write-Host "  Log:  $(Join-Path $OutputDir 'orchestration.log')"
    Write-Host ""
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

$script:ServerProcess = $null
$exitCode = 0

try {
    Write-Host ""
    Write-Log "FluentPDF Autonomous Test Orchestration" "INFO"
    Write-Log "========================================" "INFO"
    Write-Log "Start time: $(Get-Date -Format 'o')" "INFO"
    Write-Log "Output directory: $OutputDir" "INFO"
    Write-Log ""

    # Initialize environment
    if (!(Initialize-TestEnvironment)) {
        Write-Log "Environment initialization failed" "ERROR"
        exit 2
    }

    # Start API server
    if (!(Start-ApiServer)) {
        Write-Log "API server failed to start" "ERROR"
        exit 2
    }

    # Run test phases
    Run-CliTests
    Run-ApiTests
    Run-ThemeTests
    Run-AnnotationTests
    Run-FormTests

    # Generate reports
    $jsonReportPath = Join-Path $OutputDir "test-report.json"
    $htmlReportPath = Join-Path $OutputDir "test-report.html"

    $report = Generate-JsonReport -OutputPath $jsonReportPath
    Generate-HtmlReport -OutputPath $htmlReportPath -Report $report

    # Print summary
    Print-ConsoleSummary -Report $report

    # Determine exit code
    if ($report.summary.totalFailed -gt 0) {
        $exitCode = 1
    } else {
        $exitCode = 0
    }

} catch {
    Write-Log "Unhandled exception: $_" "ERROR"
    Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    $exitCode = 2
} finally {
    if (-not $NoCleanup) {
        Stop-ApiServer
    } else {
        Write-Log "API server left running (no cleanup requested)" "WARN"
    }

    Write-Log "Test orchestration completed with exit code: $exitCode" $(if ($exitCode -eq 0) { "SUCCESS" } else { "ERROR" })
    Write-Log "End time: $(Get-Date -Format 'o')" "INFO"
}

exit $exitCode
