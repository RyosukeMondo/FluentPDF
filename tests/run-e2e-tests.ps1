#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds FluentPDF.App and runs E2E test suite

.DESCRIPTION
    This script automates the E2E test execution by:
    1. Building FluentPDF.App in Release mode
    2. Running the E2E test suite with xUnit
    3. Collecting and outputting test results
    4. Handling timeouts and failures gracefully

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER Platform
    Build platform (default: x64)

.PARAMETER TestTimeout
    Test timeout in minutes (default: 10)

.PARAMETER SkipBuild
    Skip building the app and run tests only

.EXAMPLE
    .\run-e2e-tests.ps1
    .\run-e2e-tests.ps1 -Configuration Debug
    .\run-e2e-tests.ps1 -SkipBuild
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64',

    [Parameter()]
    [int]$TestTimeout = 10,

    [Parameter()]
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Script constants
$ScriptRoot = Split-Path -Parent $PSCommandPath
$RepoRoot = Split-Path -Parent $ScriptRoot
$AppProject = Join-Path $RepoRoot "src\FluentPDF.App\FluentPDF.App.csproj"
$E2ETestProject = Join-Path $ScriptRoot "FluentPDF.E2E.Tests\FluentPDF.E2E.Tests.csproj"
$TestResultsDir = Join-Path $ScriptRoot "TestResults"

# Color output helpers
function Write-ColorOutput {
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        [ConsoleColor]$Color = [ConsoleColor]::White
    )
    $previousColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $Color
    Write-Output $Message
    $Host.UI.RawUI.ForegroundColor = $previousColor
}

function Write-Success { param([string]$Message) Write-ColorOutput $Message -Color Green }
function Write-Error { param([string]$Message) Write-ColorOutput $Message -Color Red }
function Write-Warning { param([string]$Message) Write-ColorOutput $Message -Color Yellow }
function Write-Info { param([string]$Message) Write-ColorOutput $Message -Color Cyan }

# Clean up old test results
function Clear-TestResults {
    Write-Info "`n=== Cleaning up old test results ==="
    if (Test-Path $TestResultsDir) {
        Remove-Item -Path $TestResultsDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "Removed old test results"
    }
    New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
}

# Build the application
function Build-Application {
    Write-Info "`n=== Building FluentPDF.App ($Configuration|$Platform) ==="

    if (-not (Test-Path $AppProject)) {
        Write-Error "ERROR: App project not found at: $AppProject"
        exit 1
    }

    try {
        $buildArgs = @(
            'build',
            $AppProject,
            '-c', $Configuration,
            '-p:Platform=' + $Platform,
            '--nologo',
            '-v', 'minimal'
        )

        Write-Info "Running: dotnet $($buildArgs -join ' ')"
        $buildOutput = & dotnet @buildArgs 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Error "`nBuild failed with exit code: $LASTEXITCODE"
            Write-Output $buildOutput
            exit $LASTEXITCODE
        }

        Write-Success "Build completed successfully"

        # Verify executable exists
        $exePath = Join-Path $RepoRoot "src\FluentPDF.App\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\win-$($Platform.ToLower())\FluentPDF.App.exe"
        if (Test-Path $exePath) {
            Write-Success "Executable found: $exePath"
        } else {
            Write-Warning "Warning: Executable not found at expected location: $exePath"
        }
    }
    catch {
        Write-Error "`nBuild exception: $_"
        exit 1
    }
}

# Run E2E tests
function Invoke-E2ETests {
    Write-Info "`n=== Running E2E Test Suite ==="

    if (-not (Test-Path $E2ETestProject)) {
        Write-Error "ERROR: E2E test project not found at: $E2ETestProject"
        exit 1
    }

    try {
        # Clean up old app logs before running tests
        $logPath = Join-Path $env:LOCALAPPDATA "FluentPDF_Debug.log"
        if (Test-Path $logPath) {
            Remove-Item -Path $logPath -Force -ErrorAction SilentlyContinue
            Write-Info "Cleaned up old app log: $logPath"
        }

        $testArgs = @(
            'test',
            $E2ETestProject,
            '-c', $Configuration,
            '-p:Platform=' + $Platform,
            '--no-build',
            '--nologo',
            '-v', 'normal',
            '--logger', "trx;LogFileName=e2e-test-results.trx",
            '--results-directory', $TestResultsDir,
            '--',
            'xUnit.ParallelizeAssembly=false',
            'xUnit.ParallelizeTestCollections=false'
        )

        Write-Info "Running: dotnet $($testArgs -join ' ')"
        Write-Info "Test timeout: $TestTimeout minutes"
        Write-Info "Output directory: $TestResultsDir`n"

        # Start test process with timeout
        $testProcess = Start-Process -FilePath 'dotnet' `
            -ArgumentList $testArgs `
            -NoNewWindow `
            -PassThru `
            -RedirectStandardOutput (Join-Path $TestResultsDir "stdout.log") `
            -RedirectStandardError (Join-Path $TestResultsDir "stderr.log")

        # Wait for process with timeout
        $timeoutMs = $TestTimeout * 60 * 1000
        if (-not $testProcess.WaitForExit($timeoutMs)) {
            Write-Error "`nTests exceeded timeout of $TestTimeout minutes"
            $testProcess.Kill($true)
            Write-Output (Get-Content (Join-Path $TestResultsDir "stdout.log") -Raw)
            Write-Output (Get-Content (Join-Path $TestResultsDir "stderr.log") -Raw)
            exit 124  # Timeout exit code
        }

        $exitCode = $testProcess.ExitCode

        # Output logs
        $stdout = Get-Content (Join-Path $TestResultsDir "stdout.log") -Raw -ErrorAction SilentlyContinue
        $stderr = Get-Content (Join-Path $TestResultsDir "stderr.log") -Raw -ErrorAction SilentlyContinue

        if ($stdout) { Write-Output $stdout }
        if ($stderr) { Write-Output $stderr }

        return $exitCode
    }
    catch {
        Write-Error "`nTest execution exception: $_"
        exit 1
    }
}

# Parse and display test results
function Show-TestResults {
    param([int]$ExitCode)

    Write-Info "`n=== Test Results Summary ==="

    # Find TRX file
    $trxFile = Get-ChildItem -Path $TestResultsDir -Filter "*.trx" -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($trxFile) {
        Write-Info "Results file: $($trxFile.FullName)"

        try {
            [xml]$trxContent = Get-Content $trxFile.FullName
            $summary = $trxContent.TestRun.ResultSummary
            $counters = $summary.Counters

            $total = [int]$counters.total
            $executed = [int]$counters.executed
            $passed = [int]$counters.passed
            $failed = [int]$counters.failed
            $inconclusive = [int]$counters.inconclusive

            Write-Output "`nTotal Tests: $total"
            Write-Output "Executed: $executed"

            if ($passed -gt 0) {
                Write-Success "Passed: $passed"
            }
            if ($failed -gt 0) {
                Write-Error "Failed: $failed"
            }
            if ($inconclusive -gt 0) {
                Write-Warning "Inconclusive: $inconclusive"
            }

            # Show failed test details
            if ($failed -gt 0) {
                Write-Error "`n=== Failed Tests ==="
                $failedTests = $trxContent.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Failed" }
                foreach ($test in $failedTests) {
                    Write-Error "  - $($test.testName)"
                    if ($test.Output.ErrorInfo.Message) {
                        Write-Output "    Message: $($test.Output.ErrorInfo.Message)"
                    }
                    if ($test.Output.ErrorInfo.StackTrace) {
                        Write-Output "    Stack: $($test.Output.ErrorInfo.StackTrace)"
                    }
                }
            }
        }
        catch {
            Write-Warning "Could not parse TRX file: $_"
        }
    }
    else {
        Write-Warning "No TRX results file found in $TestResultsDir"
    }

    # Final status
    Write-Output ""
    if ($ExitCode -eq 0) {
        Write-Success "=== ALL TESTS PASSED ==="
    }
    else {
        Write-Error "=== TESTS FAILED (Exit Code: $ExitCode) ==="
    }

    return $ExitCode
}

# Main execution
try {
    $startTime = Get-Date
    Write-Info "FluentPDF E2E Test Runner"
    Write-Info "Configuration: $Configuration"
    Write-Info "Platform: $Platform"
    Write-Info "Start Time: $startTime"

    Clear-TestResults

    if (-not $SkipBuild) {
        Build-Application
    }
    else {
        Write-Warning "Skipping build (using existing binaries)"
    }

    $testExitCode = Invoke-E2ETests
    $finalExitCode = Show-TestResults -ExitCode $testExitCode

    $endTime = Get-Date
    $duration = $endTime - $startTime
    Write-Info "`nEnd Time: $endTime"
    Write-Info "Total Duration: $($duration.ToString('mm\:ss'))"

    exit $finalExitCode
}
catch {
    Write-Error "`nFatal error: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
