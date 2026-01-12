<#
.SYNOPSIS
    Creates MSIX packages for FluentPDF.App from build output.

.DESCRIPTION
    This script packages the FluentPDF.App build output into MSIX format using makeappx.exe
    from the Windows SDK. It supports both x64 and ARM64 platforms and handles the
    packaging process required for Microsoft Store submission.

.PARAMETER Platform
    Target platform architecture. Default: x64
    Supported values: x64, ARM64

.PARAMETER Configuration
    Build configuration. Default: Release
    Supported values: Debug, Release

.PARAMETER Version
    Package version in format Major.Minor.Build (e.g., 1.0.0)
    If not specified, uses version from Package.appxmanifest.

.PARAMETER OutputPath
    Output directory for the MSIX package.
    Default: artifacts/msix

.EXAMPLE
    .\package-msix.ps1
    Creates x64 Release MSIX package.

.EXAMPLE
    .\package-msix.ps1 -Platform ARM64 -Version 1.2.3
    Creates ARM64 Release MSIX package with version 1.2.3.

.EXAMPLE
    .\package-msix.ps1 -Platform x64 -Configuration Debug -OutputPath C:\output
    Creates x64 Debug MSIX package in custom output path.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("x64", "ARM64")]
    [string]$Platform = "x64",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

# Error handling
$ErrorActionPreference = "Stop"

# Paths
$RootPath = Split-Path $PSScriptRoot -Parent
$AppProjectPath = Join-Path $RootPath "src" "FluentPDF.App"
$BuildOutputPath = Join-Path $AppProjectPath "bin" $Platform $Configuration

# Find the net8.0-windows* folder
$TargetFrameworkFolders = Get-ChildItem -Path $BuildOutputPath -Directory -Filter "net8.0-windows*" | Sort-Object -Descending
if ($TargetFrameworkFolders.Count -eq 0) {
    throw "No build output found at $BuildOutputPath. Please build the project first."
}
$AppxPath = Join-Path $TargetFrameworkFolders[0].FullName "win-$($Platform.ToLower())"

# Default output path
if (-not $OutputPath) {
    $OutputPath = Join-Path $RootPath "artifacts" "msix"
}

# Manifest path
$ManifestPath = Join-Path $AppProjectPath "Package.appxmanifest"

# Helper functions
function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Find-MakeAppx {
    # Search for makeappx.exe in Windows SDK
    $WindowsKitsPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"

    if (-not (Test-Path $WindowsKitsPath)) {
        throw "Windows SDK not found at $WindowsKitsPath. Please install Windows SDK."
    }

    # Find the latest SDK version
    $SdkVersions = Get-ChildItem -Path $WindowsKitsPath -Directory |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object -Descending

    foreach ($SdkVersion in $SdkVersions) {
        $MakeAppxPath = Join-Path $SdkVersion.FullName "x64" "makeappx.exe"
        if (Test-Path $MakeAppxPath) {
            return $MakeAppxPath
        }
    }

    throw "makeappx.exe not found in Windows SDK. Please install Windows SDK."
}

function Update-ManifestVersion {
    param(
        [string]$ManifestPath,
        [string]$NewVersion
    )

    if (-not $NewVersion) {
        return
    }

    # Read manifest
    [xml]$Manifest = Get-Content $ManifestPath

    # Parse version (e.g., "1.2.3" or "v1.2.3")
    $VersionString = $NewVersion -replace '^v', ''
    $VersionParts = $VersionString -split '\.'

    if ($VersionParts.Count -ne 3) {
        throw "Version must be in format Major.Minor.Build (e.g., 1.0.0)"
    }

    # Update Identity element
    $Manifest.Package.Identity.Version = "$VersionString.0"

    # Save manifest
    $Manifest.Save($ManifestPath)
    Write-Success "Updated manifest version to $VersionString.0"
}

# Main script
try {
    Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║              FluentPDF MSIX Packaging Script                  ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

    Write-Host "Platform: $Platform"
    Write-Host "Configuration: $Configuration"
    Write-Host "Output Path: $OutputPath"

    # Step 1: Verify build output exists
    Write-Step "Verifying build output..."

    if (-not (Test-Path $AppxPath)) {
        throw "Build output not found at $AppxPath. Please build the project first with: msbuild /p:Platform=$Platform /p:Configuration=$Configuration"
    }

    Write-Success "Build output verified at $AppxPath"

    # Step 2: Find makeappx.exe
    Write-Step "Locating Windows SDK tools..."

    $MakeAppxExe = Find-MakeAppx
    Write-Success "Found makeappx.exe at $MakeAppxExe"

    # Step 3: Update manifest version if specified
    if ($Version) {
        Write-Step "Updating manifest version to $Version..."
        Update-ManifestVersion -ManifestPath $ManifestPath -NewVersion $Version
    }

    # Step 4: Create output directory
    if (-not (Test-Path $OutputPath)) {
        New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
        Write-Success "Created output directory: $OutputPath"
    }

    # Step 5: Package MSIX
    Write-Step "Creating MSIX package..."

    $PackageName = "FluentPDF_$($Platform)_$Configuration.msix"
    $MsixOutputPath = Join-Path $OutputPath $PackageName

    # Remove existing package if it exists
    if (Test-Path $MsixOutputPath) {
        Remove-Item $MsixOutputPath -Force
        Write-Warning "Removed existing package: $PackageName"
    }

    # Run makeappx.exe
    $MakeAppxArgs = @(
        "pack",
        "/d", $AppxPath,
        "/p", $MsixOutputPath,
        "/nv"  # No version validation (version is in manifest)
    )

    Write-Host "Running: makeappx.exe $($MakeAppxArgs -join ' ')" -ForegroundColor Gray

    & $MakeAppxExe @MakeAppxArgs 2>&1 | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Gray
    }

    if ($LASTEXITCODE -ne 0) {
        throw "makeappx.exe failed with exit code $LASTEXITCODE"
    }

    # Step 6: Verify package was created
    if (-not (Test-Path $MsixOutputPath)) {
        throw "MSIX package was not created at $MsixOutputPath"
    }

    $PackageSize = [math]::Round((Get-Item $MsixOutputPath).Length / 1MB, 2)
    Write-Success "MSIX package created successfully ($PackageSize MB)"

    # Step 7: Display summary
    Write-Host "`n" -NoNewline
    Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║                   PACKAGING COMPLETE                          ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

    Write-Host "`nPackage Details:" -ForegroundColor Cyan
    Write-Host "  Name: $PackageName" -ForegroundColor Gray
    Write-Host "  Path: $MsixOutputPath" -ForegroundColor Gray
    Write-Host "  Size: $PackageSize MB" -ForegroundColor Gray
    Write-Host "  Platform: $Platform" -ForegroundColor Gray
    Write-Host "  Configuration: $Configuration" -ForegroundColor Gray

    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "  1. Sign the package with signtool.exe"
    Write-Host "  2. Test installation on a Windows device"
    Write-Host "  3. Submit to Microsoft Store Partner Center"

    Write-Host ""
    exit 0
}
catch {
    Write-Host "`n" -NoNewline
    Write-ErrorMessage "PACKAGING FAILED: $_"
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
