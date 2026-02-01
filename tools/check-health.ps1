#!/usr/bin/env pwsh
# check-health.ps1 - Autonomous health check via REST API

param(
    [string]$Port = "5000",
    [switch]$Watch,
    [int]$Interval = 5
)

$baseUrl = "http://localhost:$Port"

function Get-HealthStatus {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Get-WatchdogStatus {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/health/watchdog" -Method Get -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Get-HungOperations {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/health/hung" -Method Get -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Show-HealthReport {
    Clear-Host
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  FluentPDF Health Check" -ForegroundColor Cyan
    Write-Host "  Time: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""

    # Overall health
    $health = Get-HealthStatus
    if ($health.Success) {
        $status = $health.Data.status
        if ($status -eq "OK") {
            Write-Host "[✓] Status: " -NoNewline -ForegroundColor Green
            Write-Host $status -ForegroundColor White
        } else {
            Write-Host "[!] Status: " -NoNewline -ForegroundColor Yellow
            Write-Host $status -ForegroundColor White
        }
        Write-Host "    Timestamp: $($health.Data.timestamp)" -ForegroundColor Gray
    } else {
        Write-Host "[✗] Status: " -NoNewline -ForegroundColor Red
        Write-Host "UNREACHABLE" -ForegroundColor Red
        Write-Host "    Error: $($health.Error)" -ForegroundColor Red
        return
    }

    Write-Host ""

    # Watchdog status
    $watchdog = Get-WatchdogStatus
    if ($watchdog.Success) {
        $data = $watchdog.Data
        Write-Host "[WATCHDOG]" -ForegroundColor Cyan
        Write-Host "  Active Operations: " -NoNewline
        Write-Host $data.totalActive -ForegroundColor White
        Write-Host "  Hung Operations: " -NoNewline
        if ($data.totalHung -eq 0) {
            Write-Host $data.totalHung -ForegroundColor Green
        } else {
            Write-Host $data.totalHung -ForegroundColor Red
        }
        Write-Host "  Healthy: " -NoNewline
        if ($data.isHealthy) {
            Write-Host "YES" -ForegroundColor Green
        } else {
            Write-Host "NO" -ForegroundColor Red
        }

        Write-Host ""

        # Show active operations
        if ($data.operations.Count -gt 0) {
            Write-Host "[ACTIVE OPERATIONS]" -ForegroundColor Yellow
            foreach ($op in $data.operations) {
                $elapsed = [math]::Round($op.elapsedMs, 0)
                $status = if ($op.isHung) { "HUNG" } else { "RUNNING" }
                $color = if ($op.isHung) { "Red" } else { "Green" }

                Write-Host "  • " -NoNewline
                Write-Host $op.operationName -NoNewline -ForegroundColor White
                Write-Host " [$status]" -NoNewline -ForegroundColor $color
                Write-Host " - ${elapsed}ms" -ForegroundColor Gray

                if ($op.isHung) {
                    $hungFor = [math]::Round($op.hungFor, 0)
                    Write-Host "    ⚠️ HUNG FOR: ${hungFor}ms (timeout: $($op.timeoutMs)ms)" -ForegroundColor Red
                }
            }
            Write-Host ""
        }
    }

    # Show hung operations
    $hung = Get-HungOperations
    if ($hung.Success -and $hung.Data.hungOperations.Count -gt 0) {
        Write-Host "[⚠️ HUNG OPERATIONS DETECTED]" -ForegroundColor Red
        foreach ($op in $hung.Data.hungOperations) {
            $elapsed = [math]::Round($op.elapsedMs, 0)
            $hungFor = [math]::Round($op.hungForMs, 0)

            Write-Host "  • " -NoNewline -ForegroundColor Red
            Write-Host $op.operationName -ForegroundColor White
            Write-Host "    Severity: " -NoNewline -ForegroundColor Red
            Write-Host $op.severity -ForegroundColor $(if ($op.severity -eq "CRITICAL") { "Red" } else { "Yellow" })
            Write-Host "    Running for: ${elapsed}ms (timeout: $($op.timeoutMs)ms)" -ForegroundColor Gray
            Write-Host "    Hung for: ${hungFor}ms" -ForegroundColor Red
        }
        Write-Host ""
    }

    Write-Host "============================================" -ForegroundColor Cyan
}

# Main execution
if ($Watch) {
    Write-Host "Watching health status every ${Interval} seconds..." -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""

    while ($true) {
        Show-HealthReport
        Start-Sleep -Seconds $Interval
    }
} else {
    Show-HealthReport
}
