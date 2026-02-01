# FluentPDF Quick Launcher
# Simple wrapper for common commands

param(
    [Parameter(Position=0)]
    [string]$Action = "ui",

    [Parameter(Position=1)]
    [string]$File = ""
)

$exePath = ".\src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe"

switch ($Action.ToLower()) {
    "ui" {
        Write-Host "Launching FluentPDF UI..." -ForegroundColor Cyan
        if ($File -and (Test-Path $File)) {
            & $exePath $File
        } else {
            & $exePath
        }
    }
    "api" {
        Write-Host "Starting API Server on port 5000..." -ForegroundColor Cyan
        Write-Host "Swagger UI: http://localhost:5000/" -ForegroundColor Yellow
        & $exePath --api-server --port 5000
    }
    "test" {
        Write-Host "Running autonomous tests..." -ForegroundColor Cyan
        & pwsh tools\run-autonomous-tests.ps1
    }
    "build" {
        Write-Host "Building FluentPDF..." -ForegroundColor Cyan
        dotnet build src\FluentPDF.Avalonia -p:AvaloniaXamlValidationMode=Skip
    }
    "help" {
        Write-Host "FluentPDF Quick Launcher" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Usage: .\run.ps1 [action] [file]" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Actions:" -ForegroundColor Green
        Write-Host "  ui [file]  - Launch UI (optionally open file)" -ForegroundColor White
        Write-Host "  api        - Start REST API server" -ForegroundColor White
        Write-Host "  test       - Run autonomous tests" -ForegroundColor White
        Write-Host "  build      - Build the application" -ForegroundColor White
        Write-Host "  help       - Show this help" -ForegroundColor White
        Write-Host ""
        Write-Host "Examples:" -ForegroundColor Green
        Write-Host "  .\run.ps1                          # Launch UI" -ForegroundColor Gray
        Write-Host "  .\run.ps1 ui tests\Fixtures\sample.pdf  # Open PDF" -ForegroundColor Gray
        Write-Host "  .\run.ps1 api                      # Start API server" -ForegroundColor Gray
        Write-Host "  .\run.ps1 test                     # Run tests" -ForegroundColor Gray
        Write-Host "  .\run.ps1 build                    # Build app" -ForegroundColor Gray
    }
    default {
        Write-Host "Unknown action: $Action" -ForegroundColor Red
        Write-Host "Run '.\run.ps1 help' for usage" -ForegroundColor Yellow
    }
}
