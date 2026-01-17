# Simple test with ASCII filename
$appPath = "src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
$logFile = "$env:LOCALAPPDATA\FluentPDF_Debug.log"

Get-Process FluentPDF.App -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

if (Test-Path $logFile) { Clear-Content $logFile }

Write-Host "Testing with ASCII filename: C:\Users\ryosu\Downloads\test.pdf"
Start-Process -FilePath $appPath -ArgumentList '--open-file "C:\Users\ryosu\Downloads\test.pdf" --console'

Start-Sleep -Seconds 8

Write-Host "`n=== Log Contents ===" -ForegroundColor Cyan
Get-Content $logFile | Select-Object -Last 30

Get-Process FluentPDF.App -ErrorAction SilentlyContinue | Stop-Process -Force
