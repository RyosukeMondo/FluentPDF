#!/usr/bin/env pwsh
# test-autonomous-hang-detection.ps1 - Fully autonomous hang detection test

param(
    [string]$PdfPath = "C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\sample-with-text.pdf",
    [int]$Port = 5000,
    [int]$StartupTimeout = 30,
    [int]$LoadTimeout = 60
)

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:$Port"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  AUTONOMOUS HANG DETECTION TEST" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill any existing instances
Write-Host "[1/8] Killing existing FluentPDF instances..." -ForegroundColor Yellow
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Write-Host "      Done." -ForegroundColor Green
Write-Host ""

# Step 2: Check if test PDF exists
Write-Host "[2/8] Checking if test PDF exists..." -ForegroundColor Yellow
if (!(Test-Path $PdfPath)) {
    Write-Host "      ERROR: Test PDF not found at: $PdfPath" -ForegroundColor Red
    Write-Host "      Please specify a valid PDF path with -PdfPath parameter" -ForegroundColor Yellow
    exit 1
}
Write-Host "      Found: $PdfPath" -ForegroundColor Green
Write-Host ""

# Step 3: Start app with API server
Write-Host "[3/8] Starting FluentPDF with API server (port $Port)..." -ForegroundColor Yellow
$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
$process = Start-Process -FilePath $appPath -ArgumentList "--api-server", "--port", $Port -PassThru -WindowStyle Normal
Write-Host "      Process ID: $($process.Id)" -ForegroundColor Green
Write-Host ""

# Step 4: Wait for app to start
Write-Host "[4/8] Waiting for app to start (timeout: ${StartupTimeout}s)..." -ForegroundColor Yellow
$startTime = Get-Date
$started = $false

while (((Get-Date) - $startTime).TotalSeconds -lt $StartupTimeout) {
    try {
        $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        if ($health.status -eq "healthy") {
            $started = $true
            Write-Host "      App started successfully!" -ForegroundColor Green
            Write-Host "      Version: $($health.version)" -ForegroundColor Gray
            Write-Host "      PDFium loaded: $($health.pdfiumLoaded)" -ForegroundColor Gray
            break
        }
    }
    catch {
        # Still starting up
    }
    Start-Sleep -Seconds 1
}

if (!$started) {
    Write-Host "      TIMEOUT: App did not start within ${StartupTimeout}s" -ForegroundColor Red
    Write-Host ""
    Write-Host "App may have crashed during startup. Checking process..." -ForegroundColor Yellow
    if ($process.HasExited) {
        Write-Host "Process has EXITED with code: $($process.ExitCode)" -ForegroundColor Red
    } else {
        Write-Host "Process is still running but API is not responding" -ForegroundColor Red
    }
    $process | Stop-Process -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host ""

# Step 5: Attempt to load PDF via GUI action
Write-Host "[5/8] Attempting to load PDF via GUI..." -ForegroundColor Yellow
Write-Host "      File: $PdfPath" -ForegroundColor Gray

$loadStartTime = Get-Date
$loadSuccess = $false
$hungDetected = $false

try {
    # Trigger file load
    $loadRequest = @{
        filePath = $PdfPath
    } | ConvertTo-Json

    Write-Host "      Sending open-file request..." -ForegroundColor Gray
    $loadResult = Invoke-RestMethod -Uri "$baseUrl/api/gui/action/open-file" `
        -Method Post `
        -Body $loadRequest `
        -ContentType "application/json" `
        -TimeoutSec 5 `
        -ErrorAction Stop

    Write-Host "      Request sent. Result:" -ForegroundColor Gray
    Write-Host "        Success: $($loadResult.success)" -ForegroundColor Gray
    Write-Host "        TabCreated: $($loadResult.tabCreated)" -ForegroundColor Gray
    Write-Host "        DocumentLoaded: $($loadResult.documentLoaded)" -ForegroundColor Gray
    Write-Host "        PageCount: $($loadResult.pageCount)" -ForegroundColor Gray

    if ($loadResult.success) {
        $loadSuccess = $true
    }
}
catch {
    Write-Host "      ERROR during load request: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Step 6: Monitor for hangs using watchdog
Write-Host "[6/8] Monitoring for hangs (timeout: ${LoadTimeout}s)..." -ForegroundColor Yellow
$monitorStart = Get-Date
$maxChecks = [math]::Ceiling($LoadTimeout / 2)
$checkCount = 0

while ($checkCount -lt $maxChecks) {
    Start-Sleep -Seconds 2
    $checkCount++

    try {
        # Check watchdog status
        $watchdog = Invoke-RestMethod -Uri "$baseUrl/api/health/watchdog" -Method Get -TimeoutSec 2 -ErrorAction Stop

        Write-Host "      [Check $checkCount/$maxChecks] Active: $($watchdog.totalActive), Hung: $($watchdog.totalHung)" -ForegroundColor Gray

        if ($watchdog.totalHung -gt 0) {
            $hungDetected = $true
            Write-Host ""
            Write-Host "      ⚠️ HUNG OPERATION DETECTED!" -ForegroundColor Red
            Write-Host ""

            foreach ($op in $watchdog.operations | Where-Object { $_.isHung }) {
                Write-Host "        Operation: $($op.operationName)" -ForegroundColor Yellow
                Write-Host "        Elapsed: $([math]::Round($op.elapsedMs, 0))ms" -ForegroundColor Yellow
                Write-Host "        Timeout: $($op.timeoutMs)ms" -ForegroundColor Yellow
                Write-Host "        Hung for: $([math]::Round($op.hungFor, 0))ms" -ForegroundColor Red
            }
            break
        }

        # If no active operations and we got past initial load, we're done
        if ($watchdog.totalActive -eq 0 -and $checkCount -gt 2) {
            Write-Host "      All operations completed." -ForegroundColor Green
            break
        }
    }
    catch {
        Write-Host "      ERROR querying watchdog: $($_.Exception.Message)" -ForegroundColor Red
        break
    }
}

Write-Host ""

# Step 7: Get detailed logs
Write-Host "[7/8] Retrieving error logs..." -ForegroundColor Yellow
try {
    $errorLogs = Invoke-RestMethod -Uri "$baseUrl/api/logs/errors" -Method Get -TimeoutSec 5 -ErrorAction Stop

    if ($errorLogs.Count -gt 0) {
        Write-Host "      Found $($errorLogs.Count) error log(s):" -ForegroundColor Red
        Write-Host ""
        foreach ($log in $errorLogs | Select-Object -First 10) {
            Write-Host "        [$($log.timestamp)] [$($log.level)] [$($log.source)]" -ForegroundColor Red
            Write-Host "        $($log.message)" -ForegroundColor White
            Write-Host ""
        }
    } else {
        Write-Host "      No errors logged." -ForegroundColor Green
    }
}
catch {
    Write-Host "      Could not retrieve logs: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Step 8: Get last 50 logs for full context
Write-Host "[7.5/8] Retrieving last 50 logs for context..." -ForegroundColor Yellow
try {
    $allLogs = Invoke-RestMethod -Uri "$baseUrl/api/logs?count=50" -Method Get -TimeoutSec 5 -ErrorAction Stop

    # Save to file
    $logFile = "autonomous-test-logs-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $allLogs | ConvertTo-Json -Depth 10 | Out-File $logFile
    Write-Host "      Saved to: $logFile" -ForegroundColor Green
}
catch {
    Write-Host "      Could not retrieve all logs: $($_.Exception.Message)" -ForegroundColor Yellow
}
Write-Host ""

# Step 8: Final summary
Write-Host "[8/8] TEST SUMMARY" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($hungDetected) {
    Write-Host "RESULT: ❌ HANG DETECTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "The application HUNG while loading the PDF file." -ForegroundColor Red
    Write-Host "Operation exceeded timeout threshold." -ForegroundColor Red
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Check logs file: $logFile" -ForegroundColor Yellow
    Write-Host "2. Review error logs above" -ForegroundColor Yellow
    Write-Host "3. Check GUI window - is it frozen?" -ForegroundColor Yellow
    $exitCode = 1
} elseif ($loadSuccess) {
    Write-Host "RESULT: ✅ SUCCESS" -ForegroundColor Green
    Write-Host ""
    Write-Host "PDF loaded successfully without hanging!" -ForegroundColor Green
    Write-Host "All operations completed within timeout." -ForegroundColor Green
    $exitCode = 0
} else {
    Write-Host "RESULT: ⚠️ INCONCLUSIVE" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Load request did not report success, but no hang detected." -ForegroundColor Yellow
    Write-Host "Check logs file: $logFile" -ForegroundColor Yellow
    $exitCode = 2
}

Write-Host ""
Write-Host "Process still running: $(!$process.HasExited)" -ForegroundColor Gray
Write-Host ""
Write-Host "To view GUI debug console, check the app window." -ForegroundColor Cyan
Write-Host "To stop app: Get-Process FluentPDF.Avalonia | Stop-Process" -ForegroundColor Cyan
Write-Host ""

# Don't auto-kill to allow manual inspection
Write-Host "App left running for manual inspection." -ForegroundColor Yellow
Write-Host "Press any key to stop the app and exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Stopping app..." -ForegroundColor Yellow
$process | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Done." -ForegroundColor Green

exit $exitCode
