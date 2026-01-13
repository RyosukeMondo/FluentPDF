# FluentPDF Smoke Test Script
# Launches the app and verifies it starts successfully

param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

# Paths
$exePath = Join-Path $rootDir "src\FluentPDF.App\bin\x64\$Configuration\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
$pdfiumPath = Join-Path $rootDir "libs\x64\bin\pdfium.dll"
$debugLogPath = Join-Path $env:LOCALAPPDATA "FluentPDF_Debug.log"

Write-Host "=== FluentPDF Smoke Test ===" -ForegroundColor Cyan
Write-Host ""

# Check exe exists
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: FluentPDF.App.exe not found at: $exePath" -ForegroundColor Red
    Write-Host "Run: dotnet build src\FluentPDF.App -c $Configuration -p:Platform=x64" -ForegroundColor Yellow
    exit 1
}
Write-Host "[OK] FluentPDF.App.exe found" -ForegroundColor Green

# Check pdfium.dll in libs
if (-not (Test-Path $pdfiumPath)) {
    Write-Host "ERROR: pdfium.dll not found in libs\x64\bin\" -ForegroundColor Red
    Write-Host "Run: .\tools\download-pdfium.ps1" -ForegroundColor Yellow
    exit 1
}
Write-Host "[OK] pdfium.dll found in libs" -ForegroundColor Green

# Copy pdfium.dll to app output if needed
$appPdfiumPath = Join-Path (Split-Path $exePath) "pdfium.dll"
if (-not (Test-Path $appPdfiumPath)) {
    Write-Host "Copying pdfium.dll to app output..." -ForegroundColor Yellow
    Copy-Item $pdfiumPath $appPdfiumPath -Force
}
Write-Host "[OK] pdfium.dll in app directory" -ForegroundColor Green

# Delete old debug log
if (Test-Path $debugLogPath) {
    Remove-Item $debugLogPath -Force
}

Write-Host ""
Write-Host "Launching FluentPDF.App.exe..." -ForegroundColor Cyan

# Launch app
$process = Start-Process -FilePath $exePath -PassThru

# Wait for app to initialize (up to 15 seconds)
$timeout = 15
$elapsed = 0
$success = $false

Write-Host "Waiting for app to initialize..."

while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++

    # Check if app has exited
    if ($process.HasExited) {
        Write-Host "ERROR: App exited unexpectedly with code $($process.ExitCode)" -ForegroundColor Red
        break
    }

    # Check debug log for success
    if (Test-Path $debugLogPath) {
        $log = Get-Content $debugLogPath -Raw
        if ($log -match "Window activated") {
            Write-Host "[OK] Window activated successfully" -ForegroundColor Green
            $success = $true
            break
        }
    }

    Write-Host "  ... waiting ($elapsed/$timeout seconds)"
}

Write-Host ""
Write-Host "=== Debug Log ===" -ForegroundColor Cyan
if (Test-Path $debugLogPath) {
    Get-Content $debugLogPath
} else {
    Write-Host "(No debug log created)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Result ===" -ForegroundColor Cyan

# Close the app
if (-not $process.HasExited) {
    Write-Host "Closing app..."
    $process.CloseMainWindow() | Out-Null
    Start-Sleep -Seconds 2
    if (-not $process.HasExited) {
        $process.Kill()
    }
}

if ($success) {
    Write-Host "SMOKE TEST PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "SMOKE TEST FAILED" -ForegroundColor Red
    exit 1
}
