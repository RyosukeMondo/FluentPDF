#!/usr/bin/env pwsh
# Automatically diagnose where the app hangs

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Automatic Hang Diagnosis" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kill any existing instances
Write-Host "[1/5] Cleaning up old processes..." -ForegroundColor Yellow
Get-Process FluentPDF.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Delete old log files
Write-Host "[2/5] Removing old debug logs..." -ForegroundColor Yellow
Remove-Item C:\Users\ryosu\Desktop\FluentPDF-Hang-Debug-*.txt -ErrorAction SilentlyContinue

# Start the app
$appPath = "C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe"
Write-Host "[3/5] Starting app..." -ForegroundColor Yellow

$process = Start-Process -FilePath $appPath -PassThru -WindowStyle Normal

Write-Host "    Process ID: $($process.Id)" -ForegroundColor Gray
Write-Host "    Waiting 10 seconds for app to initialize or hang..." -ForegroundColor Yellow

# Wait for app to start or hang
Start-Sleep -Seconds 10

# Kill it
Write-Host "[4/5] Terminating app..." -ForegroundColor Yellow
$process | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Find and read the log file
Write-Host "[5/5] Analyzing hang location..." -ForegroundColor Yellow
Write-Host ""

$logFile = Get-ChildItem C:\Users\ryosu\Desktop\FluentPDF-Hang-Debug-*.txt -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($logFile) {
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  HANG DIAGNOSTIC LOG" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""

    $content = Get-Content $logFile.FullName
    $content | ForEach-Object { Write-Host $_ -ForegroundColor White }

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  ANALYSIS" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""

    $lastLine = $content | Select-Object -Last 1
    Write-Host "Last message before hang:" -ForegroundColor Yellow
    Write-Host "  $lastLine" -ForegroundColor Cyan
    Write-Host ""

    if ($lastLine -match "\[(\d+\.?\d*)/7\]") {
        $step = $matches[1]
        Write-Host "Hung at step: $step" -ForegroundColor Red

        switch ($step) {
            "1" { Write-Host "  → Hung before PDFium initialization" -ForegroundColor Red }
            "2" { Write-Host "  → Hung during PDFium initialization" -ForegroundColor Red }
            "3" { Write-Host "  → Hung after PDFium init, before DI container" -ForegroundColor Red }
            "4" { Write-Host "  → Hung getting MainViewModel from DI" -ForegroundColor Red }
            "4.5" { Write-Host "  → Hung after getting MainViewModel, before MainWindow creation" -ForegroundColor Red }
            "5" { Write-Host "  → Hung during MainWindow constructor" -ForegroundColor Red }
            "5.5" { Write-Host "  → Hung after MainWindow created, before setting desktop.MainWindow" -ForegroundColor Red }
            "6" { Write-Host "  → Hung setting desktop.MainWindow" -ForegroundColor Red }
            "6.5" { Write-Host "  → Hung after setting desktop.MainWindow, before calling base" -ForegroundColor Red }
            "7" { Write-Host "  → Hung calling base.OnFrameworkInitializationCompleted()" -ForegroundColor Red }
            "7.5" { Write-Host "  → Hung after base returned (should not happen)" -ForegroundColor Red }
        }
    }

    Write-Host ""
    Write-Host "Log saved at: $($logFile.FullName)" -ForegroundColor Gray

} else {
    Write-Host "ERROR: No log file found!" -ForegroundColor Red
    Write-Host "This means the app crashed before creating the log." -ForegroundColor Red
}

Write-Host ""
Write-Host "Diagnosis complete!" -ForegroundColor Green
