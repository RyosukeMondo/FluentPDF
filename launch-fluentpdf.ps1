# FluentPDF Avalonia Launcher
# Builds and launches FluentPDF with XAML warnings allowed

Write-Host "Building FluentPDF.Avalonia..." -ForegroundColor Cyan
Push-Location "src/FluentPDF.Avalonia"

$buildResult = dotnet build -p:TreatWarningsAsErrors=false 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    Pop-Location
    exit 1
}

Write-Host "Build succeeded. Launching app..." -ForegroundColor Green
dotnet run --no-build -p:TreatWarningsAsErrors=false

Pop-Location
