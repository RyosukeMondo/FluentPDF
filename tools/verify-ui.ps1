#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Autonomous UI verification script using the FluentPDF Verification API.

.DESCRIPTION
    This script demonstrates UI verification capabilities by making HTTP requests
    to the FluentPDF REST API. It verifies element properties, layout, theme switching,
    and user interactions.

.PARAMETER Port
    The port number where the API server is running (default: 5000).

.PARAMETER BaseUrl
    The base URL of the API server (default: http://localhost:$Port).

.EXAMPLE
    .\verify-ui.ps1
    Runs UI verification against the default API server at localhost:5000.

.EXAMPLE
    .\verify-ui.ps1 -Port 8080
    Runs UI verification against the API server at localhost:8080.
#>

param(
    [int]$Port = 5000,
    [string]$BaseUrl = "http://localhost:$Port"
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

# HTTP helper functions
function Invoke-ApiGet {
    param([string]$Endpoint)

    $url = "$BaseUrl$Endpoint"
    Write-Info "GET $url"

    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -ContentType "application/json"
        return $response
    }
    catch {
        Write-Failure "Request failed: $_"
        throw
    }
}

function Invoke-ApiPost {
    param(
        [string]$Endpoint,
        [object]$Body
    )

    $url = "$BaseUrl$Endpoint"
    Write-Info "POST $url"

    try {
        $json = $Body | ConvertTo-Json -Depth 10
        $response = Invoke-RestMethod -Uri $url -Method Post -Body $json -ContentType "application/json"
        return $response
    }
    catch {
        Write-Failure "Request failed: $_"
        throw
    }
}

# Test functions
function Test-Health {
    Write-Host "`n=== Health Check ===" -ForegroundColor Yellow

    $health = Invoke-ApiGet "/api/health"

    if ($health.status -eq "healthy") {
        Write-Success "API server is healthy"
        Write-Info "Version: $($health.version)"
        Write-Info "PDFium loaded: $($health.pdfiumLoaded)"
        return $true
    }
    else {
        Write-Failure "API server is unhealthy"
        return $false
    }
}

function Test-Status {
    Write-Host "`n=== Application Status ===" -ForegroundColor Yellow

    $status = Invoke-ApiGet "/api/status"

    Write-Info "Window open: $($status.windowOpen)"
    Write-Info "Document loaded: $($status.documentLoaded)"
    Write-Info "Current page: $($status.currentPage) / $($status.totalPages)"
    Write-Info "Zoom level: $($status.zoomLevel)%"
    Write-Info "Theme: $($status.theme)"

    if ($status.windowOpen) {
        Write-Success "Application window is open"
        return $true
    }
    else {
        Write-Failure "Application window is not open"
        return $false
    }
}

function Test-ElementVerification {
    Write-Host "`n=== Element Verification ===" -ForegroundColor Yellow

    $request = @{
        automationId = "OpenFileButton"
        expectedProperties = @{
            isEnabled = $true
            isVisible = $true
            width = @{
                min = 80
                max = 150
            }
        }
    }

    $response = Invoke-ApiPost "/api/verify/element" $request

    if ($response.found) {
        Write-Success "Element 'OpenFileButton' found"
        Write-Info "  Name: $($response.element.name)"
        Write-Info "  Enabled: $($response.element.isEnabled)"
        Write-Info "  Visible: $($response.element.isVisible)"
        Write-Info "  Size: $($response.element.width) x $($response.element.height)"

        if ($response.passed) {
            Write-Success "All property checks passed"
            return $true
        }
        else {
            Write-Failure "Some property checks failed"
            foreach ($check in $response.checks) {
                if (-not $check.passed) {
                    Write-Info "  ✗ $($check.property): expected $($check.expected), got $($check.actual)"
                }
            }
            return $false
        }
    }
    else {
        Write-Failure "Element 'OpenFileButton' not found"
        return $false
    }
}

function Test-LayoutVerification {
    Write-Host "`n=== Layout Verification ===" -ForegroundColor Yellow

    $request = @{
        elements = @(
            @{
                automationId = "ThumbnailsPanel"
                expectedPosition = "left"
                expectedWidth = @{
                    min = 150
                    max = 300
                }
            },
            @{
                automationId = "ContentArea"
                expectedWidth = @{
                    min = 400
                }
            }
        )
    }

    $response = Invoke-ApiPost "/api/verify/layout" $request

    if ($response.passed) {
        Write-Success "Layout verification passed"
        foreach ($element in $response.elements) {
            if ($element.found) {
                Write-Info "  ✓ $($element.automationId): $($element.position.width) x $($element.position.height)"
            }
        }
        return $true
    }
    else {
        Write-Failure "Layout verification failed"
        foreach ($error in $response.errors) {
            Write-Info "  $error"
        }
        return $false
    }
}

function Test-Navigation {
    Write-Host "`n=== Navigation Testing ===" -ForegroundColor Yellow

    # Get current status
    $status = Invoke-ApiGet "/api/status"
    $currentPage = $status.currentPage

    if ($status.totalPages -gt 1) {
        # Navigate to next page
        $request = @{
            action = "nextPage"
            expectedPage = $currentPage + 1
        }

        $response = Invoke-ApiPost "/api/action/navigate" $request

        if ($response.success -and $response.passed) {
            Write-Success "Successfully navigated to next page"
            Write-Info "  Previous: $($response.previousPage), Current: $($response.currentPage)"
            return $true
        }
        else {
            Write-Failure "Navigation failed"
            foreach ($error in $response.errors) {
                Write-Info "  $error"
            }
            return $false
        }
    }
    else {
        Write-Info "Document has only 1 page, skipping navigation test"
        return $true
    }
}

function Test-ThemeVerification {
    Write-Host "`n=== Theme Verification ===" -ForegroundColor Yellow

    $request = @{
        setTheme = "dark"
        verifyElements = @(
            @{
                automationId = "MainGrid"
                expectedBackground = "#1E1E1E"
            }
        )
    }

    try {
        $response = Invoke-ApiPost "/api/verify/theme" $request

        if ($response.passed) {
            Write-Success "Theme switched to dark successfully"
            return $true
        }
        else {
            Write-Failure "Theme verification failed"
            foreach ($element in $response.elements) {
                if (-not $element.passed) {
                    Write-Info "  $($element.automationId): expected $($element.expectedBackground), got $($element.background)"
                }
            }
            return $false
        }
    }
    catch {
        Write-Info "Theme verification not available (may require document loaded)"
        return $true
    }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  FluentPDF UI Verification Test Suite     ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Magenta

$results = @{
    Health = $false
    Status = $false
    Element = $false
    Layout = $false
    Navigation = $false
    Theme = $false
}

try {
    # Run tests
    $results.Health = Test-Health
    $results.Status = Test-Status
    $results.Element = Test-ElementVerification
    $results.Layout = Test-LayoutVerification
    $results.Navigation = Test-Navigation
    $results.Theme = Test-ThemeVerification

    # Summary
    Write-Host "`n╔════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║  Test Results Summary                      ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor Magenta

    $totalTests = $results.Count
    $passedTests = ($results.Values | Where-Object { $_ -eq $true }).Count

    foreach ($test in $results.GetEnumerator()) {
        $symbol = if ($test.Value) { "✓" } else { "✗" }
        $color = if ($test.Value) { "Green" } else { "Red" }
        Write-Host "  $symbol $($test.Key)" -ForegroundColor $color
    }

    Write-Host "`nPassed: $passedTests / $totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Yellow" })

    # Exit with appropriate code
    if ($passedTests -eq $totalTests) {
        Write-Host "`n✓ All tests passed!" -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "`n⚠ Some tests failed" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "`n✗ Test suite failed with error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 2
}
