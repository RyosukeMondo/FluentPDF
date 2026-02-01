# FluentPDF Launcher
# Double-click this file to launch FluentPDF

$exePath = Join-Path $PSScriptRoot "src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

if (Test-Path $exePath) {
    Write-Host "Launching FluentPDF..." -ForegroundColor Cyan
    Start-Process $exePath
} else {
    Write-Host "Error: FluentPDF.App.exe not found!" -ForegroundColor Red
    Write-Host "Path: $exePath" -ForegroundColor Yellow
    Write-Host "`nPlease build the application first:" -ForegroundColor Yellow
    Write-Host "  dotnet build src\FluentPDF.App -p:Platform=x64" -ForegroundColor White
    pause
}
