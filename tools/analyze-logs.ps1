#!/usr/bin/env pwsh
# Autonomous Log Analyzer - Diagnose FluentPDF issues

param(
    [string]$LogPath = "",
    [switch]$Latest,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

# Colors
$colors = @{
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Success = "Green"
    Debug = "Gray"
}

function Get-LatestLogFile {
    $logDir = "C:\Users\ryosu\AppData\Local\Temp\FluentPDF\logs"
    $latestLog = Get-ChildItem $logDir -Filter "log-*.json" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    return $latestLog.FullName
}

function Parse-LogEntry {
    param([string]$Line)

    try {
        $entry = $Line | ConvertFrom-Json
        return [PSCustomObject]@{
            Timestamp = [DateTime]::Parse($entry.'@t').ToLocalTime()
            Level = $entry.'@l' ?? "Information"
            Message = $entry.'@mt'
            Source = $entry.SourceContext -replace 'FluentPDF\.(Avalonia|Rendering)\.', ''
            Exception = $entry.'@x'
            Raw = $entry
        }
    } catch {
        return $null
    }
}

function Get-ErrorSummary {
    param([array]$Entries)

    $errors = @{}
    $warnings = @{}

    foreach ($entry in $Entries) {
        if ($entry.Level -eq "Error") {
            $key = $entry.Message
            if (!$errors.ContainsKey($key)) {
                $errors[$key] = @{
                    Count = 0
                    FirstSeen = $entry.Timestamp
                    LastSeen = $entry.Timestamp
                    Source = $entry.Source
                }
            }
            $errors[$key].Count++
            $errors[$key].LastSeen = $entry.Timestamp
        }
        elseif ($entry.Level -eq "Warning") {
            $key = $entry.Message
            if (!$warnings.ContainsKey($key)) {
                $warnings[$key] = @{
                    Count = 0
                    FirstSeen = $entry.Timestamp
                    LastSeen = $entry.Timestamp
                    Source = $entry.Source
                }
            }
            $warnings[$key].Count++
            $warnings[$key].LastSeen = $entry.Timestamp
        }
    }

    return @{
        Errors = $errors
        Warnings = $warnings
    }
}

function Show-DiagnosticReport {
    param([string]$LogFile)

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info
    Write-Host "  FLUENTPDF AUTONOMOUS DIAGNOSTIC REPORT" -ForegroundColor $colors.Info
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info
    Write-Host "Log File: $LogFile" -ForegroundColor $colors.Debug
    Write-Host "Analyzed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor $colors.Debug
    Write-Host ""

    # Parse all log entries
    $lines = Get-Content $LogFile
    $entries = $lines | ForEach-Object { Parse-LogEntry $_ } | Where-Object { $_ -ne $null }

    Write-Host "Total Log Entries: $($entries.Count)" -ForegroundColor $colors.Info
    Write-Host "Time Range: $($entries[0].Timestamp.ToString('HH:mm:ss')) - $($entries[-1].Timestamp.ToString('HH:mm:ss'))" -ForegroundColor $colors.Info
    Write-Host ""

    # Get error summary
    $summary = Get-ErrorSummary -Entries $entries

    # Show errors
    if ($summary.Errors.Count -gt 0) {
        Write-Host "═══ ERRORS ($($summary.Errors.Count) unique) ═══" -ForegroundColor $colors.Error
        Write-Host ""
        foreach ($error in $summary.Errors.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending) {
            Write-Host "  ❌ $($error.Key)" -ForegroundColor $colors.Error
            Write-Host "     Count: $($error.Value.Count)" -ForegroundColor $colors.Debug
            Write-Host "     Source: $($error.Value.Source)" -ForegroundColor $colors.Debug
            Write-Host "     First: $($error.Value.FirstSeen.ToString('HH:mm:ss'))" -ForegroundColor $colors.Debug
            Write-Host "     Last: $($error.Value.LastSeen.ToString('HH:mm:ss'))" -ForegroundColor $colors.Debug
            Write-Host ""
        }
    }

    # Show warnings
    if ($summary.Warnings.Count -gt 0) {
        Write-Host "═══ WARNINGS ($($summary.Warnings.Count) unique) ═══" -ForegroundColor $colors.Warning
        Write-Host ""
        foreach ($warning in $summary.Warnings.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending) {
            $msg = $warning.Key
            $count = $warning.Value.Count
            $source = $warning.Value.Source

            Write-Host "  ⚠️  $msg" -ForegroundColor $colors.Warning
            Write-Host "     Count: $count" -ForegroundColor $colors.Debug
            Write-Host "     Source: $source" -ForegroundColor $colors.Debug
            Write-Host "     First: $($warning.Value.FirstSeen.ToString('HH:mm:ss'))" -ForegroundColor $colors.Debug
            Write-Host "     Last: $($warning.Value.LastSeen.ToString('HH:mm:ss'))" -ForegroundColor $colors.Debug

            # Provide diagnostic suggestions
            if ($msg -match "Failed to initialize form environment") {
                Write-Host "     💡 DIAGNOSIS: PDFium form environment initialization failing" -ForegroundColor $colors.Info
                Write-Host "        This is NON-CRITICAL - PDFs without forms load normally" -ForegroundColor $colors.Info
                Write-Host "        Forms functionality may be limited in loaded PDFs" -ForegroundColor $colors.Info
            }
            elseif ($msg -match "HUNG OPERATION DETECTED") {
                Write-Host "     💡 DIAGNOSIS: Operation exceeded timeout threshold" -ForegroundColor $colors.Info
                Write-Host "        Check for deadlocks or blocking I/O on UI thread" -ForegroundColor $colors.Info
            }

            Write-Host ""
        }
    }

    # Show diagnostic recommendations
    Write-Host "═══ DIAGNOSTIC RECOMMENDATIONS ═══" -ForegroundColor $colors.Success
    Write-Host ""

    $recommendations = @()

    if ($summary.Warnings.Keys -match "Failed to initialize form environment") {
        $recommendations += @"
  1. Form Environment Initialization Warning
     - Status: NON-CRITICAL (PDFs load successfully)
     - Impact: Form fill features may not work
     - Action: Check PDFium form callbacks in PdfFormService.cs:62
     - Temporary: Can be safely ignored for viewing PDFs
"@
    }

    if ($summary.Errors.Count -eq 0 -and $summary.Warnings.Count -le 1) {
        Write-Host "  ✅ Application is running normally" -ForegroundColor $colors.Success
        Write-Host "     No critical issues detected" -ForegroundColor $colors.Success
    }
    elseif ($recommendations.Count -gt 0) {
        foreach ($rec in $recommendations) {
            Write-Host $rec -ForegroundColor $colors.Info
        }
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $colors.Info
    Write-Host ""
}

# Main execution
if ($Latest -or [string]::IsNullOrEmpty($LogPath)) {
    $LogPath = Get-LatestLogFile
    Write-Host "Using latest log file: $LogPath" -ForegroundColor $colors.Info
}

if (-not (Test-Path $LogPath)) {
    Write-Host "❌ Log file not found: $LogPath" -ForegroundColor $colors.Error
    exit 1
}

if ($Watch) {
    Write-Host "Watching log file for changes (Ctrl+C to stop)..." -ForegroundColor $colors.Info

    $lastSize = 0
    while ($true) {
        $currentSize = (Get-Item $LogPath).Length
        if ($currentSize -ne $lastSize) {
            Clear-Host
            Show-DiagnosticReport -LogFile $LogPath
            $lastSize = $currentSize
        }
        Start-Sleep -Seconds 2
    }
}
else {
    Show-DiagnosticReport -LogFile $LogPath
}
