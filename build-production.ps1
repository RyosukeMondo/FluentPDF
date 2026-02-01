# FluentPDF Avalonia - Production Build Script
# Creates optimized, self-contained production builds

param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [switch]$CreateZip = $true,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Green
Write-Host "FluentPDF Avalonia Production Build" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Paths
$ProjectPath = "src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj"
$OutputDir = "artifacts\$Runtime"
$ReleaseDir = "releases\v$Version\$Runtime"

# Create release directory
if (-not (Test-Path $ReleaseDir)) {
    Write-Host "Creating release directory: $ReleaseDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null
}

# Clean previous builds
if (Test-Path $OutputDir) {
    Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Run tests first
if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow
    Write-Host "============================" -ForegroundColor Cyan

    dotnet test tests\FluentPDF.Core.Tests\FluentPDF.Core.Tests.csproj --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Core tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    dotnet test tests\FluentPDF.Rendering.Tests\FluentPDF.Rendering.Tests.csproj --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Rendering tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "All tests passed!" -ForegroundColor Green
}

# Restore dependencies
Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath

# Build with optimizations
Write-Host "`nBuilding production executable..." -ForegroundColor Yellow
Write-Host "This may take several minutes..." -ForegroundColor Cyan
Write-Host ""

$buildStartTime = Get-Date

dotnet publish $ProjectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $OutputDir `
    /p:PublishSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:Version=$Version `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

$buildEndTime = Get-Date
$buildDuration = ($buildEndTime - $buildStartTime).TotalSeconds

# Verify executable exists
$exePath = Join-Path $OutputDir "FluentPDF.Avalonia.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "`nError: Executable not found at $exePath" -ForegroundColor Red
    exit 1
}

# Display build info
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

$exeInfo = Get-Item $exePath
$exeSizeMB = [math]::Round($exeInfo.Length / 1MB, 2)

Write-Host "Executable Information:" -ForegroundColor Yellow
Write-Host "  Path:         $exePath" -ForegroundColor Cyan
Write-Host "  Size:         $exeSizeMB MB" -ForegroundColor Cyan
Write-Host "  Build Time:   $([math]::Round($buildDuration, 1)) seconds" -ForegroundColor Cyan
Write-Host ""

# Generate SHA256 checksum
Write-Host "Generating SHA256 checksum..." -ForegroundColor Yellow
$hash = Get-FileHash -Path $exePath -Algorithm SHA256
$hashFile = Join-Path $OutputDir "FluentPDF.Avalonia.exe.sha256"
"$($hash.Hash)  FluentPDF.Avalonia.exe" | Out-File -FilePath $hashFile -Encoding ASCII

Write-Host "  SHA256: $($hash.Hash)" -ForegroundColor Cyan
Write-Host ""

# Create ZIP package
if ($CreateZip) {
    Write-Host "Creating ZIP package..." -ForegroundColor Yellow
    $zipName = "FluentPDF-$Runtime-v$Version.zip"
    $zipPath = Join-Path $ReleaseDir $zipName

    # Remove old ZIP if exists
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    # Compress
    Compress-Archive -Path "$OutputDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

    $zipInfo = Get-Item $zipPath
    $zipSizeMB = [math]::Round($zipInfo.Length / 1MB, 2)

    Write-Host "  Package: $zipName" -ForegroundColor Cyan
    Write-Host "  Size:    $zipSizeMB MB" -ForegroundColor Cyan
    Write-Host "  Path:    $zipPath" -ForegroundColor Cyan
    Write-Host ""

    # Generate ZIP checksum
    $zipHash = Get-FileHash -Path $zipPath -Algorithm SHA256
    $zipHashFile = "$zipPath.sha256"
    "$($zipHash.Hash)  $zipName" | Out-File -FilePath $zipHashFile -Encoding ASCII
}

# Copy executable to release directory
Write-Host "Copying artifacts to release directory..." -ForegroundColor Yellow
Copy-Item -Path $exePath -Destination $ReleaseDir -Force
Copy-Item -Path $hashFile -Destination $ReleaseDir -Force

# Display total size
$totalSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "Total output size: $totalSizeMB MB" -ForegroundColor Yellow
Write-Host ""

# Display next steps
Write-Host "================================================" -ForegroundColor Green
Write-Host "NEXT STEPS" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Test the executable:" -ForegroundColor Yellow
Write-Host "  cd $OutputDir" -ForegroundColor Cyan
Write-Host "  .\FluentPDF.Avalonia.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "Release artifacts:" -ForegroundColor Yellow
Write-Host "  $ReleaseDir" -ForegroundColor Cyan
Write-Host ""

# Create verification report
$reportPath = Join-Path $ReleaseDir "BUILD_REPORT.txt"
$report = @"
FluentPDF Avalonia - Production Build Report
=============================================

Build Information
-----------------
Version:        $Version
Runtime:        $Runtime
Configuration:  Release
Build Date:     $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Build Duration: $([math]::Round($buildDuration, 1)) seconds

Executable
----------
Path:   $exePath
Size:   $exeSizeMB MB
SHA256: $($hash.Hash)

Package
-------
"@

if ($CreateZip) {
    $report += @"
ZIP:    $zipName
Size:   $zipSizeMB MB
SHA256: $($zipHash.Hash)

"@
}

$report += @"

Build Settings
--------------
- PublishSingleFile:                    true
- PublishTrimmed:                       false (disabled due to reflection)
- EnableCompressionInSingleFile:        true
- IncludeNativeLibrariesForSelfExtract: true
- SelfContained:                        true

Features
--------
- Cross-platform PDF viewer
- File dialog support
- Theme system (dark/light modes)
- REST API support (optional)

System Requirements
-------------------
- Windows 10/11 (x64)
- No .NET runtime required (self-contained)
- ~$exeSizeMB MB disk space

Verification Checklist
----------------------
[ ] Executable runs without errors
[ ] Can open file dialog
[ ] Can load and view PDF files
[ ] Theme switching works
[ ] No missing dependencies
[ ] No console window appears

Distribution
------------
Files to distribute:
1. FluentPDF.Avalonia.exe
2. FluentPDF.Avalonia.exe.sha256
3. $zipName (optional)
4. $zipName.sha256 (optional)

"@

$report | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "Build report saved: $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "Production build completed successfully!" -ForegroundColor Green
