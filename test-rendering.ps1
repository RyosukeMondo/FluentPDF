# Test FluentPDF rendering with PDFiumCore
# This script launches the app, opens a PDF, and checks for successful rendering

$appPath = "src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
$testFile = "C:\Users\ryosu\Downloads\ロボコンコース.pdf"
$logFile = "$env:LOCALAPPDATA\FluentPDF_Debug.log"

Write-Host "Testing FluentPDF with PDFiumCore..." -ForegroundColor Cyan

# Clean up previous instances
Get-Process FluentPDF.App -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Clear old log
if (Test-Path $logFile) {
    Clear-Content $logFile
}

Write-Host "Launching app..." -ForegroundColor Yellow
Start-Process -FilePath $appPath -ArgumentList "--open-file `"$testFile`" --console" -PassThru | Out-Null

# Wait for app to initialize
Start-Sleep -Seconds 5

Write-Host "`nChecking logs..." -ForegroundColor Yellow
if (Test-Path $logFile) {
    $logs = Get-Content $logFile -Raw

    # Check for success indicators
    $pdfiumInit = $logs -match "PDFium initialized successfully"
    $docLoaded = $logs -match "LoadDocumentFromPathAsync START"
    $docSuccess = $logs -match "OpenFileInTabAsync SUCCESS EXIT"
    $noCrash = $logs -notmatch "Fatal error|AccessViolation|crash"

    Write-Host "`n=== Test Results ===" -ForegroundColor Cyan
    Write-Host "PDFium Initialized: $(if($pdfiumInit){'✓ PASS'}else{'✗ FAIL'})" -ForegroundColor $(if($pdfiumInit){'Green'}else{'Red'})
    Write-Host "Document Loaded: $(if($docLoaded){'✓ PASS'}else{'✗ FAIL'})" -ForegroundColor $(if($docLoaded){'Green'}else{'Red'})
    Write-Host "Load Succeeded: $(if($docSuccess){'✓ PASS'}else{'✗ FAIL'})" -ForegroundColor $(if($docSuccess){'Green'}else{'Red'})
    Write-Host "No Crashes: $(if($noCrash){'✓ PASS'}else{'✗ FAIL'})" -ForegroundColor $(if($noCrash){'Green'}else{'Red'})

    if ($pdfiumInit -and $docLoaded -and $docSuccess -and $noCrash) {
        Write-Host "`n✓ ALL TESTS PASSED - PDFiumCore integration successful!" -ForegroundColor Green
        Write-Host "The app should be displaying the PDF now. Please verify visually." -ForegroundColor Yellow
    } else {
        Write-Host "`n✗ SOME TESTS FAILED - Check logs for details" -ForegroundColor Red
    }

    Write-Host "`n=== Recent Log Entries ===" -ForegroundColor Cyan
    Get-Content $logFile | Select-Object -Last 20
} else {
    Write-Host "Log file not found at: $logFile" -ForegroundColor Red
}

Write-Host "`nPress Enter to close the app..." -ForegroundColor Yellow
Read-Host
Get-Process FluentPDF.App -ErrorAction SilentlyContinue | Stop-Process -Force
