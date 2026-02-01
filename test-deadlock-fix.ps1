#!/usr/bin/env pwsh
# Test if deadlock fix works

$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
$testPdf = "C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\100_Apps_AI_Factory.pdf"

Write-Host "Starting FluentPDF to test deadlock fix..." -ForegroundColor Yellow
Write-Host "Test PDF: $testPdf" -ForegroundColor Cyan
Write-Host ""

# Start app (will show window)
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $appPath
$psi.Arguments = "`"$testPdf`""  # Open the PDF file immediately
$psi.UseShellExecute = $false

$process = Start-Process -FilePath $appPath -ArgumentList "`"$testPdf`"" -PassThru

Write-Host "App started (PID: $($process.Id))" -ForegroundColor Green
Write-Host ""
Write-Host "Testing for 15 seconds..." -ForegroundColor Yellow
Write-Host "  - If app opens PDF and shows main page, deadlock is FIXED ✅" -ForegroundColor Green
Write-Host "  - If app hangs after 'Adding to recent files...', deadlock still exists ❌" -ForegroundColor Red
Write-Host ""

# Wait 15 seconds
Start-Sleep -Seconds 15

# Check if still running and responsive
if (!$process.HasExited) {
    Write-Host "App is still running after 15 seconds" -ForegroundColor Green
    Write-Host ""
    Write-Host "Please check the app window:" -ForegroundColor Yellow
    Write-Host "  1. Is the PDF rendered?" -ForegroundColor Cyan
    Write-Host "  2. Can you click buttons/menus?" -ForegroundColor Cyan
    Write-Host "  3. Check logs at: C:\Users\ryosu\AppData\Local\Temp\FluentPDF\logs\" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Press any key to kill the app and check logs..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

    $process.Kill()
    Start-Sleep -Seconds 2
} else {
    Write-Host "❌ App exited unexpectedly (exit code: $($process.ExitCode))" -ForegroundColor Red
}

# Find latest log file
$logDir = "C:\Users\ryosu\AppData\Local\Temp\FluentPDF\logs"
$latestLog = Get-ChildItem $logDir -Filter "log-*.json" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestLog) {
    Write-Host ""
    Write-Host "Checking latest log for deadlock indicators..." -ForegroundColor Yellow
    Write-Host "Log file: $($latestLog.FullName)" -ForegroundColor Cyan

    $logContent = Get-Content $latestLog.FullName -Raw

    # Check for hung operation warnings
    if ($logContent -match "HUNG OPERATION DETECTED") {
        Write-Host ""
        Write-Host "❌ DEADLOCK STILL EXISTS - Found hung operation warnings" -ForegroundColor Red
    } elseif ($logContent -match "\[8/8\] SUCCESS! File opened in new tab") {
        Write-Host ""
        Write-Host "✅ DEADLOCK FIXED - File opened successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "⚠️  Could not determine status from logs" -ForegroundColor Yellow
    }

    # Show last few log entries
    Write-Host ""
    Write-Host "Last 10 log entries:" -ForegroundColor Cyan
    $logLines = (Get-Content $latestLog.FullName | Select-Object -Last 10)
    foreach ($line in $logLines) {
        try {
            $obj = $line | ConvertFrom-Json
            Write-Host "  [$($obj.'@t')] $($obj.'@m')" -ForegroundColor Gray
        } catch {
            # Skip invalid JSON lines
        }
    }
}

Write-Host ""
Write-Host "Test complete." -ForegroundColor Green
