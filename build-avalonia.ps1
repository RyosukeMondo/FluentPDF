# FluentPDF Avalonia - Windows Build Script
# PowerShell version for Windows users

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $false,
    [switch]$SingleFile = $false,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

Write-Host "FluentPDF Avalonia Build Script" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host ""

# Configuration
$ProjectPath = "src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj"
$OutputDir = "artifacts\$Runtime"

# Clean previous builds
if (Test-Path $OutputDir) {
    Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Restore dependencies
Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath

# Build arguments
$buildArgs = @(
    "build", $ProjectPath,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--output", $OutputDir
)

if ($SelfContained) {
    $buildArgs += "--self-contained"
}

if ($SingleFile) {
    $buildArgs += "-p:PublishSingleFile=true"
    $buildArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

# Build
Write-Host "`nBuilding..." -ForegroundColor Yellow
& dotnet $buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Run tests
if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow

    dotnet test tests\FluentPDF.Core.Tests\FluentPDF.Core.Tests.csproj --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Core tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    dotnet test tests\FluentPDF.Rendering.Tests\FluentPDF.Rendering.Tests.csproj --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Rendering tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow

# Display build info
$exePath = Join-Path $OutputDir "FluentPDF.Avalonia.exe"
if (Test-Path $exePath) {
    Write-Host "`nExecutable:" -ForegroundColor Green
    Get-Item $exePath | Format-Table Name, Length, LastWriteTime -AutoSize

    Write-Host "`nTo run the application:" -ForegroundColor Yellow
    Write-Host "  cd $OutputDir" -ForegroundColor Cyan
    Write-Host "  .\FluentPDF.Avalonia.exe" -ForegroundColor Cyan
} else {
    Write-Host "`nWarning: Executable not found at $exePath" -ForegroundColor Red
}

# Display size info
$totalSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host "`nTotal output size: $totalSizeMB MB" -ForegroundColor Yellow
