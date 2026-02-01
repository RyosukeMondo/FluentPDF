# FluentPDF Launcher
# Double-click this file to launch FluentPDF

$exePath = Join-Path $PSScriptRoot "src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe"

if (Test-Path $exePath) {
    Write-Host "Launching FluentPDF..." -ForegroundColor Cyan
    Start-Process $exePath
} else {
    Write-Host "Error: FluentPDF.Avalonia.exe not found!" -ForegroundColor Red
    Write-Host "Path: $exePath" -ForegroundColor Yellow
    Write-Host "`nPlease build the application first:" -ForegroundColor Yellow
    Write-Host "  dotnet build src\FluentPDF.Avalonia" -ForegroundColor White
    pause
}
