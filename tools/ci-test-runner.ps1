#!/usr/bin/env pwsh
<#
.SYNOPSIS
    CI/CD-friendly test runner wrapper for FluentPDF autonomous tests

.DESCRIPTION
    Executes the autonomous test suite with CI/CD optimizations:
    - Structured error reporting
    - JUnit XML output for CI/CD systems
    - Performance benchmarking
    - Artifact collection
    - Exit code management

.PARAMETER Port
    API server port (default: 5000)

.PARAMETER Verbose
    Enable detailed logging

.PARAMETER ExportJunit
    Export JUnit XML format for CI/CD integration

.PARAMETER CollectArtifacts
    Copy reports to specified directory

.PARAMETER FailFast
    Exit immediately on first failure

.EXAMPLE
    pwsh tools/ci-test-runner.ps1
    pwsh tools/ci-test-runner.ps1 -ExportJunit -CollectArtifacts ./ci-artifacts
    pwsh tools/ci-test-runner.ps1 -Verbose -FailFast
#>

param(
    [int]$Port = 5000,
    [switch]$Verbose,
    [switch]$ExportJunit,
    [string]$CollectArtifacts,
    [switch]$FailFast
)

$ErrorActionPreference = "Stop"
$scriptsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptsDir

# Run the main test script
$testScriptArgs = @()
$testScriptArgs += "-Port", $Port

if ($Verbose) {
    $testScriptArgs += "-Verbose"
}

Write-Host "Running autonomous tests..." -ForegroundColor Cyan

$testScript = Join-Path $scriptsDir "run-autonomous-tests.ps1"
$testOutput = & $testScript @testScriptArgs
$testExitCode = $LASTEXITCODE

# Parse JSON report if it exists
$jsonReportPath = Join-Path $repoRoot "tests/reports/test-report.json"
$report = $null

if (Test-Path $jsonReportPath) {
    $report = Get-Content $jsonReportPath | ConvertFrom-Json
} else {
    Write-Warning "Test report not found at: $jsonReportPath"
}

# Export JUnit format if requested
if ($ExportJunit -and $report) {
    Write-Host "Generating JUnit XML..." -ForegroundColor Cyan
    $junitPath = Join-Path (Split-Path $jsonReportPath) "test-results.xml"

    $junitXml = @"
<?xml version="1.0" encoding="UTF-8"?>
<testsuites name="FluentPDF Autonomous Tests" tests="$($report.summary.totalTests)" failures="$($report.summary.totalFailed)" skipped="$($report.skipped.Count)" timestamp="$($report.timestamp)">
"@

    # Add CLI tests
    $junitXml += "`n  <testsuite name=`"CLI Tests`" tests=`"$($report.cliTests.Count)`">`n"
    foreach ($test in $report.cliTests.Values) {
        $status = if ($test.passed) { "" } else { "failure" }
        if ($status) {
            $junitXml += "    <testcase classname=`"CLI`" name=`"$($test.name)`" time=`"$([Math]::Round($test.details.elapsedMs / 1000, 2))`"><failure message=`"$($test.message)`"/></testcase>`n"
        } else {
            $junitXml += "    <testcase classname=`"CLI`" name=`"$($test.name)`" time=`"$([Math]::Round($test.details.elapsedMs / 1000, 2))`"/>`n"
        }
    }
    $junitXml += "  </testsuite>`n"

    # Add API tests
    $junitXml += "  <testsuite name=`"API Tests`" tests=`"$($report.apiTests.Count)`">`n"
    foreach ($test in $report.apiTests.Values) {
        $status = if ($test.passed) { "" } else { "failure" }
        if ($status) {
            $junitXml += "    <testcase classname=`"API`" name=`"$($test.name)`" time=`"$([Math]::Round($test.details.elapsedMs / 1000, 2))`"><failure message=`"$($test.message)`"/></testcase>`n"
        } else {
            $junitXml += "    <testcase classname=`"API`" name=`"$($test.name)`" time=`"$([Math]::Round($test.details.elapsedMs / 1000, 2))`"/>`n"
        }
    }
    $junitXml += "  </testsuite>`n"

    $junitXml += "</testsuites>"

    $junitXml | Set-Content -Path $junitPath
    Write-Host "JUnit XML saved: $junitPath" -ForegroundColor Green
}

# Collect artifacts if requested
if ($CollectArtifacts) {
    Write-Host "Collecting test artifacts..." -ForegroundColor Cyan

    if (!(Test-Path $CollectArtifacts)) {
        New-Item -ItemType Directory -Path $CollectArtifacts -Force | Out-Null
    }

    $reportsDir = Join-Path $repoRoot "tests/reports"
    if (Test-Path $reportsDir) {
        Copy-Item -Path "$reportsDir/*" -Destination $CollectArtifacts -Recurse -Force
        Write-Host "Artifacts collected to: $CollectArtifacts" -ForegroundColor Green
    }
}

# Print summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TEST EXECUTION COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($report) {
    Write-Host "Passed:  $($report.summary.totalPassed)" -ForegroundColor Green
    Write-Host "Failed:  $($report.summary.totalFailed)" -ForegroundColor $(if ($report.summary.totalFailed -gt 0) { "Red" } else { "Green" })
    Write-Host "Skipped: $($report.skipped.Count)" -ForegroundColor Yellow
    Write-Host "Success: $($report.summary.successRate)%" -ForegroundColor $(if ($report.summary.successRate -eq 100) { "Green" } else { "Yellow" })
    Write-Host ""

    if ($report.summary.totalFailed -gt 0) {
        Write-Host "Failed tests:" -ForegroundColor Red
        foreach ($failure in $report.failures) {
            Write-Host "  - $($failure.category) > $($failure.name)" -ForegroundColor Red
        }
    }
}

# Output for CI systems
Write-Host ""
Write-Host "Exit code: $testExitCode" -ForegroundColor $(if ($testExitCode -eq 0) { "Green" } else { "Red" })

exit $testExitCode
