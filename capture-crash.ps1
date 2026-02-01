#!/usr/bin/env pwsh
# Capture app crash with Windows Error Reporting

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
$outputLog = "crash-output-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"

Write-Host "Starting app and capturing output to: $outputLog" -ForegroundColor Yellow

# Start process with redirected output
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $appPath
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $false  # Show window

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $psi

# Start capturing output
$outputBuilder = New-Object System.Text.StringBuilder
$errorBuilder = New-Object System.Text.StringBuilder

$outHandler = {
    if ($EventArgs.Data) {
        [void]$Event.MessageData.AppendLine($EventArgs.Data)
    }
}
$errHandler = {
    if ($EventArgs.Data) {
        [void]$Event.MessageData.AppendLine("ERROR: " + $EventArgs.Data)
    }
}

$outEvent = Register-ObjectEvent -InputObject $process -EventName OutputDataReceived -Action $outHandler -MessageData $outputBuilder
$errEvent = Register-ObjectEvent -InputObject $process -EventName ErrorDataReceived -Action $errHandler -MessageData $errorBuilder

try {
    $process.Start() | Out-Null
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()

    Write-Host "Process started. Waiting 10 seconds..." -ForegroundColor Green

    # Wait max 10 seconds
    $process.WaitForExit(10000) | Out-Null

    if (!$process.HasExited) {
        Write-Host "App still running after 10 seconds - killing it" -ForegroundColor Yellow
        $process.Kill()
    } else {
        Write-Host "App exited with code: $($process.ExitCode)" -ForegroundColor $(if ($process.ExitCode -eq 0) { "Green" } else { "Red" })
    }
}
finally {
    # Clean up events
    Unregister-Event -SourceIdentifier $outEvent.Name -ErrorAction SilentlyContinue
    Unregister-Event -SourceIdentifier $errEvent.Name -ErrorAction SilentlyContinue

    # Save output
    $output = $outputBuilder.ToString()
    $errors = $errorBuilder.ToString()

    "$output`n`n=== ERRORS ===`n$errors" | Out-File $outputLog

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  CAPTURED OUTPUT" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host $output
    if ($errors) {
        Write-Host ""
        Write-Host "=== ERRORS ===" -ForegroundColor Red
        Write-Host $errors -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Saved to: $outputLog" -ForegroundColor Green

    # Also check Desktop for any log files
    Write-Host ""
    Write-Host "Checking for log files on Desktop..." -ForegroundColor Yellow
    Get-ChildItem C:\Users\ryosu\Desktop\FluentPDF-*.* -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 3 |
        ForEach-Object {
            Write-Host "  Found: $($_.Name)" -ForegroundColor Cyan
        }
}
