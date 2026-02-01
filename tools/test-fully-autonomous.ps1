#!/usr/bin/env pwsh
# test-fully-autonomous.ps1 - 100% autonomous testing with real-time log monitoring

param(
    [string]$PdfPath = "C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\sample-with-text.pdf",
    [int]$Port = 5000
)

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:$Port"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  FULLY AUTONOMOUS TEST" -ForegroundColor Cyan
Write-Host "  Real-time log monitoring via REST API" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill existing instances
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Start app
$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
Write-Host "[1] Starting app with API server..." -ForegroundColor Yellow
Write-Host "    NOTE: Window will be visible - this is required for Avalonia UI thread" -ForegroundColor Gray
$process = Start-Process -FilePath $appPath `
    -ArgumentList "--api-server", "--port", $Port `
    -PassThru

Write-Host "    Process ID: $($process.Id)" -ForegroundColor Green
Write-Host ""

# Wait for startup
Write-Host "[2] Waiting for API to be ready..." -ForegroundColor Yellow
$started = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -TimeoutSec 1 -ErrorAction Stop
        if ($health.status -eq "healthy") {
            $started = $true
            Write-Host "    ✅ API ready!" -ForegroundColor Green
            break
        }
    } catch { }
    Start-Sleep -Seconds 1
}

if (!$started) {
    Write-Host "    ❌ Failed to start" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Get initial logs
Write-Host "[3] Getting initial logs..." -ForegroundColor Yellow
try {
    $initialLogs = Invoke-RestMethod -Uri "$baseUrl/api/logs?count=100" -TimeoutSec 5
    Write-Host "    Found $($initialLogs.count) initial log entries" -ForegroundColor Gray

    Write-Host ""
    Write-Host "    === STARTUP LOGS ===" -ForegroundColor Cyan
    foreach ($log in $initialLogs.logs | Select-Object -Last 10) {
        $color = switch ($log.level) {
            "Error" { "Red" }
            "Warning" { "Yellow" }
            default { "White" }
        }
        Write-Host "    [$($log.timestamp.ToString('HH:mm:ss.fff'))] [$($log.level)] [$($log.source)] $($log.message)" -ForegroundColor $color
    }
}
catch {
    Write-Host "    ⚠️ Could not get initial logs" -ForegroundColor Yellow
}
Write-Host ""

# Trigger file load
Write-Host "[4] Triggering file load: $PdfPath" -ForegroundColor Yellow
$loadRequest = @{ filePath = $PdfPath } | ConvertTo-Json

$loadJob = Start-Job -ScriptBlock {
    param($url, $body)
    try {
        Invoke-RestMethod -Uri $url `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -TimeoutSec 30
    }
    catch {
        @{ error = $_.Exception.Message }
    }
} -ArgumentList "$baseUrl/api/gui/action/open-file", $loadRequest

Write-Host "    File load request sent (running in background)" -ForegroundColor Gray
Write-Host ""

# Monitor logs in real-time
Write-Host "[5] Monitoring logs in REAL-TIME..." -ForegroundColor Yellow
Write-Host "    Watching for file open operation..." -ForegroundColor Gray
Write-Host ""

$lastLogCount = $initialLogs.count
$startTime = Get-Date
$maxWaitSeconds = 30
$foundFileOpen = $false
$lastStep = 0
$hungDetected = $false

while (((Get-Date) - $startTime).TotalSeconds -lt $maxWaitSeconds) {
    try {
        # Get latest logs
        $currentLogs = Invoke-RestMethod -Uri "$baseUrl/api/logs?count=100" -TimeoutSec 2

        # Check for new logs
        if ($currentLogs.count -gt $lastLogCount) {
            $newLogs = $currentLogs.logs | Select-Object -Skip $lastLogCount

            foreach ($log in $newLogs) {
                $timestamp = $log.timestamp.ToString('HH:mm:ss.fff')
                $color = switch ($log.level) {
                    "Error" { "Red" }
                    "Warning" { "Yellow" }
                    "Info" { "Cyan" }
                    default { "White" }
                }

                Write-Host "    [$timestamp] [$($log.level)] [$($log.source)]" -NoNewline -ForegroundColor Gray
                Write-Host " $($log.message)" -ForegroundColor $color

                # Track progress through file open steps
                if ($log.message -match "\[(\d+)/8\]") {
                    $step = [int]$matches[1]
                    if ($step -gt $lastStep) {
                        $lastStep = $step
                        $foundFileOpen = $true
                    }
                }

                # Check for success
                if ($log.message -match "SUCCESS! File opened") {
                    Write-Host ""
                    Write-Host "    ✅ FILE OPENED SUCCESSFULLY!" -ForegroundColor Green
                    break
                }

                # Check for errors
                if ($log.level -eq "Error" -and $log.message -match "CRITICAL") {
                    Write-Host ""
                    Write-Host "    ❌ ERROR DETECTED!" -ForegroundColor Red
                    break
                }
            }

            $lastLogCount = $currentLogs.count
        }

        # Check watchdog for hangs
        $watchdog = Invoke-RestMethod -Uri "$baseUrl/api/health/watchdog" -TimeoutSec 2
        if ($watchdog.totalHung -gt 0) {
            $hungDetected = $true
            Write-Host ""
            Write-Host "    ⚠️ WATCHDOG DETECTED HANG!" -ForegroundColor Red
            foreach ($op in $watchdog.operations | Where-Object { $_.isHung }) {
                Write-Host "    Operation: $($op.operationName)" -ForegroundColor Yellow
                Write-Host "    Elapsed: $([math]::Round($op.elapsedMs))ms / Timeout: $($op.timeoutMs)ms" -ForegroundColor Yellow
            }
            break
        }

        # Check if load job completed
        if ($loadJob.State -eq "Completed") {
            $result = Receive-Job $loadJob
            if ($result.error) {
                Write-Host ""
                Write-Host "    ⚠️ Load request completed with error: $($result.error)" -ForegroundColor Yellow
            } else {
                Write-Host ""
                Write-Host "    Load request completed" -ForegroundColor Gray
                # Check if document was successfully loaded
                if ($result.success -and $result.documentLoaded) {
                    Write-Host "    ✅ Document loaded! Pages: $($result.pageCount)" -ForegroundColor Green
                    $foundFileOpen = $true
                    $lastStep = 8  # Mark as complete since document loaded successfully
                }
            }
            break
        }

    }
    catch {
        Write-Host "    ⚠️ Error querying API: $($_.Exception.Message)" -ForegroundColor Yellow
        break
    }

    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host ""

# Get final logs
Write-Host "[6] Getting final logs..." -ForegroundColor Yellow
try {
    $finalLogs = Invoke-RestMethod -Uri "$baseUrl/api/logs?count=100" -TimeoutSec 5

    # Save to file
    $logFile = "autonomous-full-logs-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $finalLogs | ConvertTo-Json -Depth 10 | Out-File $logFile
    Write-Host "    Saved to: $logFile" -ForegroundColor Green

    # Show error logs
    $errorLogs = $finalLogs.logs | Where-Object { $_.level -eq "Error" }
    if ($errorLogs.Count -gt 0) {
        Write-Host ""
        Write-Host "    === ERROR LOGS ===" -ForegroundColor Red
        foreach ($log in $errorLogs) {
            Write-Host "    [$($log.timestamp.ToString('HH:mm:ss.fff'))] [$($log.source)]" -ForegroundColor Red
            Write-Host "    $($log.message)" -ForegroundColor White
            Write-Host ""
        }
    }
}
catch {
    Write-Host "    Could not get final logs" -ForegroundColor Yellow
}
Write-Host ""

# Final summary
Write-Host "[7] TEST SUMMARY" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($hungDetected) {
    Write-Host "RESULT: ⚠️ HANG DETECTED" -ForegroundColor Yellow
    Write-Host "The operation exceeded timeout threshold." -ForegroundColor Yellow
} elseif ($foundFileOpen) {
    if ($lastStep -eq 8) {
        Write-Host "RESULT: ✅ SUCCESS" -ForegroundColor Green
        Write-Host "File opened successfully! All 8 steps completed." -ForegroundColor Green
    } else {
        Write-Host "RESULT: ⚠️ INCOMPLETE" -ForegroundColor Yellow
        Write-Host "File open started but only reached step $lastStep/8" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "The operation may have hung at step $lastStep" -ForegroundColor Yellow
    }
} else {
    Write-Host "RESULT: ❌ FAILED" -ForegroundColor Red
    Write-Host "File open operation did not start or was not logged." -ForegroundColor Red
}

Write-Host ""
Write-Host "Process still running: $(!$process.HasExited)" -ForegroundColor Gray
Write-Host "Log file: $logFile" -ForegroundColor Gray
Write-Host ""

# Cleanup
Write-Host "Stopping app..." -ForegroundColor Yellow
$process | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Job $loadJob -Force -ErrorAction SilentlyContinue

Write-Host "Done." -ForegroundColor Green
