#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs FluentPDF API integration tests with automated server lifecycle management.

.DESCRIPTION
    This script:
    1. Starts FluentPDF.App with --api-server --headless
    2. Waits for the API server to become healthy
    3. Runs all API integration tests
    4. Stops the API server gracefully
    5. Reports results with appropriate exit code

.PARAMETER Port
    The port for the API server (default: 5000)

.PARAMETER Timeout
    Timeout in seconds to wait for server startup (default: 30)

.PARAMETER KeepServerRunning
    If specified, does not stop the server after tests complete

.EXAMPLE
    .\run-api-tests.ps1
    Runs tests with default settings

.EXAMPLE
    .\run-api-tests.ps1 -Port 8080
    Runs tests on custom port

.EXAMPLE
    .\run-api-tests.ps1 -KeepServerRunning
    Runs tests and leaves server running for manual inspection
#>

param(
    [int]$Port = 5000,
    [int]$Timeout = 30,
    [switch]$KeepServerRunning
)

$ErrorActionPreference = "Stop"
$OriginalLocation = Get-Location

# ANSI color codes
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-Info { param([string]$Message) Write-Host "${Blue}[INFO]${Reset} $Message" }
function Write-Success { param([string]$Message) Write-Host "${Green}[SUCCESS]${Reset} $Message" }
function Write-Warning { param([string]$Message) Write-Host "${Yellow}[WARNING]${Reset} $Message" }
function Write-Error { param([string]$Message) Write-Host "${Red}[ERROR]${Reset} $Message" }

try {
    # Navigate to repository root
    $RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
    Set-Location $RepoRoot
    Write-Info "Repository root: $RepoRoot"

    # Find FluentPDF.App executable
    $AppPath = Get-ChildItem -Path "src/FluentPDF.App/bin" -Filter "FluentPDF.App.exe" -Recurse | Select-Object -First 1
    if (-not $AppPath) {
        Write-Error "FluentPDF.App.exe not found. Build the project first:"
        Write-Host "  dotnet build src/FluentPDF.App -p:Platform=x64"
        exit 1
    }
    Write-Info "Found app at: $($AppPath.FullName)"

    # Start API server
    Write-Info "Starting API server on port $Port..."
    $ServerProcess = Start-Process -FilePath $AppPath.FullName `
        -ArgumentList "--api-server", "--headless", "--port", $Port `
        -PassThru `
        -NoNewWindow `
        -RedirectStandardOutput "$env:TEMP\fluentpdf-api-server-stdout.log" `
        -RedirectStandardError "$env:TEMP\fluentpdf-api-server-stderr.log"

    if (-not $ServerProcess) {
        Write-Error "Failed to start API server"
        exit 1
    }
    Write-Info "API server process started (PID: $($ServerProcess.Id))"

    # Wait for server to become healthy
    Write-Info "Waiting for API server to become healthy (timeout: ${Timeout}s)..."
    $BaseUrl = "http://localhost:$Port"
    $HealthUrl = "$BaseUrl/api/health"
    $StartTime = Get-Date
    $Healthy = $false

    while (((Get-Date) - $StartTime).TotalSeconds -lt $Timeout) {
        try {
            $Response = Invoke-WebRequest -Uri $HealthUrl -Method GET -TimeoutSec 2 -UseBasicParsing
            if ($Response.StatusCode -eq 200) {
                $Healthy = $true
                Write-Success "API server is healthy!"
                break
            }
        }
        catch {
            # Server not ready yet, continue waiting
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not $Healthy) {
        Write-Error "API server did not become healthy within ${Timeout}s"
        Write-Info "Server stdout: $env:TEMP\fluentpdf-api-server-stdout.log"
        Write-Info "Server stderr: $env:TEMP\fluentpdf-api-server-stderr.log"
        if ($ServerProcess -and -not $ServerProcess.HasExited) {
            Stop-Process -Id $ServerProcess.Id -Force
        }
        exit 1
    }

    # Set environment variable for tests
    $env:TEST_API_BASE_URL = $BaseUrl
    Write-Info "Set TEST_API_BASE_URL=$BaseUrl"

    # Run tests (remove skip filter to run all tests)
    Write-Info "Running API integration tests..."
    $TestPath = "tests/FluentPDF.Api.Tests"

    # Temporarily unskip tests by using a filter that includes them
    $TestResult = dotnet test $TestPath `
        --logger "console;verbosity=normal" `
        --logger "trx;LogFileName=api-tests.trx" `
        --filter "FullyQualifiedName~ApiIntegrationTests|FullyQualifiedName~ErrorHandlingTests" `
        -- `
        TestRunParameters.Parameter(name=\"Skip\", value=\"false\")

    $TestExitCode = $LASTEXITCODE

    if ($TestExitCode -eq 0) {
        Write-Success "All tests passed!"
    }
    else {
        Write-Error "Tests failed with exit code $TestExitCode"
    }

    # Display test results
    $TrxFile = Get-ChildItem -Path $TestPath -Filter "api-tests.trx" -Recurse | Select-Object -First 1
    if ($TrxFile) {
        Write-Info "Test results: $($TrxFile.FullName)"
    }

    # Stop server unless requested to keep running
    if (-not $KeepServerRunning) {
        Write-Info "Stopping API server..."
        if ($ServerProcess -and -not $ServerProcess.HasExited) {
            Stop-Process -Id $ServerProcess.Id -Force
            Write-Info "API server stopped"
        }
        else {
            Write-Warning "API server process already exited"
        }
    }
    else {
        Write-Info "API server is still running on port $Port (PID: $($ServerProcess.Id))"
        Write-Info "Stop it manually: Stop-Process -Id $($ServerProcess.Id)"
    }

    # Exit with test result code
    exit $TestExitCode
}
catch {
    Write-Error "Unhandled error: $_"
    Write-Error $_.ScriptStackTrace

    # Attempt cleanup
    if ($ServerProcess -and -not $ServerProcess.HasExited) {
        Write-Info "Cleaning up: stopping API server..."
        Stop-Process -Id $ServerProcess.Id -Force -ErrorAction SilentlyContinue
    }

    exit 1
}
finally {
    Set-Location $OriginalLocation
}
