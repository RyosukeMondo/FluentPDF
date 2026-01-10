#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads and installs PDF validation tools (VeraPDF, JHOVE, QPDF).

.DESCRIPTION
    This script automates the installation of PDF validation tools:
    - VeraPDF: PDF/A compliance validation
    - JHOVE: PDF format characterization
    - QPDF: PDF structural validation

    The script is idempotent and can be safely run multiple times.
    It checks for existing installations before downloading.

.PARAMETER SkipVeraPDF
    Skip VeraPDF installation

.PARAMETER SkipJHOVE
    Skip JHOVE installation

.PARAMETER SkipQPDF
    Skip QPDF installation

.PARAMETER Force
    Force reinstallation even if tools are already present

.EXAMPLE
    .\install-tools.ps1
    Install all validation tools

.EXAMPLE
    .\install-tools.ps1 -SkipJHOVE
    Install only VeraPDF and QPDF

.NOTES
    Requirements:
    - PowerShell 7.0+ (cross-platform)
    - Java Runtime Environment (for JHOVE)
    - Internet connection for downloads
#>

[CmdletBinding()]
param(
    [switch]$SkipVeraPDF,
    [switch]$SkipJHOVE,
    [switch]$SkipQPDF,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Tool versions (updated periodically)
$VeraPdfVersion = "1.26.1"
$JhoveVersion = "1.30.1"
$QpdfVersion = "11.9.1"

# Installation directories
$ScriptDir = $PSScriptRoot
$ToolsDir = $ScriptDir
$VeraPdfDir = Join-Path $ToolsDir "verapdf"
$JhoveDir = Join-Path $ToolsDir "jhove"
$QpdfDir = Join-Path $ToolsDir "qpdf"

# Temporary download directory
$TempDir = Join-Path $ScriptDir "temp"

function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Test-Java {
    <#
    .SYNOPSIS
        Check if Java is installed and accessible
    #>
    try {
        $javaVersion = java -version 2>&1 | Select-Object -First 1
        if ($javaVersion -match "version") {
            Write-Success "Java found: $javaVersion"
            return $true
        }
    }
    catch {
        Write-ErrorMsg "Java not found. JHOVE requires Java Runtime Environment."
        Write-ErrorMsg "Please install Java: https://adoptium.net/"
        return $false
    }
}

function Install-VeraPDF {
    <#
    .SYNOPSIS
        Download and install VeraPDF
    #>
    Write-Status "Installing VeraPDF v$VeraPdfVersion..."

    # Check if already installed
    $veraPdfExe = if ($IsWindows) {
        Join-Path $VeraPdfDir "verapdf.bat"
    } else {
        Join-Path $VeraPdfDir "verapdf"
    }

    if ((Test-Path $veraPdfExe) -and -not $Force) {
        Write-Status "VeraPDF already installed at: $VeraPdfDir"
        try {
            $version = & $veraPdfExe --version 2>&1 | Select-Object -First 1
            Write-Success "Current version: $version"
            return $true
        }
        catch {
            Write-Warning "VeraPDF executable found but not working. Reinstalling..."
        }
    }

    # Determine download URL based on OS
    $downloadUrl = if ($IsWindows) {
        "https://downloads.verapdf.org/rel/verapdf-installer-$VeraPdfVersion.zip"
    } elseif ($IsMacOS) {
        "https://downloads.verapdf.org/rel/verapdf-greenfield-$VeraPdfVersion-installer.zip"
    } else {
        "https://downloads.verapdf.org/rel/verapdf-greenfield-$VeraPdfVersion-installer.zip"
    }

    $zipFile = Join-Path $TempDir "verapdf.zip"
    $extractDir = Join-Path $TempDir "verapdf-extract"

    try {
        # Download
        Write-Status "Downloading from: $downloadUrl"
        New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile -UseBasicParsing

        # Extract
        Write-Status "Extracting VeraPDF..."
        New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
        Expand-Archive -Path $zipFile -DestinationPath $extractDir -Force

        # Find the verapdf directory in extracted files
        $veraPdfExtracted = Get-ChildItem -Path $extractDir -Recurse -Directory -Filter "verapdf*" | Select-Object -First 1

        if (-not $veraPdfExtracted) {
            # Sometimes it extracts directly
            $veraPdfExtracted = $extractDir
        }

        # Move to installation directory
        if (Test-Path $VeraPdfDir) {
            Remove-Item -Path $VeraPdfDir -Recurse -Force
        }
        Move-Item -Path $veraPdfExtracted.FullName -Destination $VeraPdfDir -Force

        # Make executable on Unix
        if (-not $IsWindows) {
            chmod +x "$VeraPdfDir/verapdf"
        }

        # Verify installation
        $version = & $veraPdfExe --version 2>&1 | Select-Object -First 1
        Write-Success "VeraPDF v$version installed successfully at: $VeraPdfDir"
        return $true
    }
    catch {
        Write-ErrorMsg "Failed to install VeraPDF: $_"
        return $false
    }
    finally {
        # Cleanup
        if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
        if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    }
}

function Install-JHOVE {
    <#
    .SYNOPSIS
        Download and install JHOVE
    #>
    Write-Status "Installing JHOVE v$JhoveVersion..."

    # Check Java first
    if (-not (Test-Java)) {
        return $false
    }

    # Check if already installed
    $jhoveJar = Join-Path $JhoveDir "jhove.jar"
    if ((Test-Path $jhoveJar) -and -not $Force) {
        Write-Status "JHOVE already installed at: $JhoveDir"
        try {
            $version = java -jar $jhoveJar -v 2>&1 | Select-Object -First 1
            Write-Success "Current version: $version"
            return $true
        }
        catch {
            Write-Warning "JHOVE jar found but not working. Reinstalling..."
        }
    }

    $downloadUrl = "https://github.com/openpreserve/jhove/releases/download/v$JhoveVersion/jhove-$JhoveVersion.jar"
    $jarFile = Join-Path $TempDir "jhove.jar"

    try {
        # Download
        Write-Status "Downloading from: $downloadUrl"
        New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
        Invoke-WebRequest -Uri $downloadUrl -OutFile $jarFile -UseBasicParsing

        # Create installation directory
        New-Item -ItemType Directory -Force -Path $JhoveDir | Out-Null

        # Move jar to installation directory
        $destJar = Join-Path $JhoveDir "jhove.jar"
        Move-Item -Path $jarFile -Destination $destJar -Force

        # Verify installation
        $version = java -jar $destJar -v 2>&1 | Select-Object -First 1
        Write-Success "JHOVE $version installed successfully at: $JhoveDir"
        return $true
    }
    catch {
        Write-ErrorMsg "Failed to install JHOVE: $_"
        return $false
    }
    finally {
        # Cleanup
        if (Test-Path $jarFile) { Remove-Item $jarFile -Force }
    }
}

function Install-QPDF {
    <#
    .SYNOPSIS
        Install QPDF (platform-specific)
    #>
    Write-Status "Installing QPDF v$QpdfVersion..."

    # Check if already installed in system PATH
    $qpdfCmd = Get-Command qpdf -ErrorAction SilentlyContinue
    if ($qpdfCmd -and -not $Force) {
        try {
            $version = qpdf --version 2>&1 | Select-Object -First 1
            Write-Success "QPDF already installed in system PATH: $version"
            return $true
        }
        catch {
            # Continue with local installation
        }
    }

    # Check local installation
    $qpdfExe = if ($IsWindows) {
        Join-Path $QpdfDir "bin" "qpdf.exe"
    } else {
        Join-Path $QpdfDir "bin" "qpdf"
    }

    if ((Test-Path $qpdfExe) -and -not $Force) {
        Write-Status "QPDF already installed at: $QpdfDir"
        try {
            $version = & $qpdfExe --version 2>&1 | Select-Object -First 1
            Write-Success "Current version: $version"
            return $true
        }
        catch {
            Write-Warning "QPDF executable found but not working. Reinstalling..."
        }
    }

    if ($IsWindows) {
        # Download Windows binary
        $downloadUrl = "https://github.com/qpdf/qpdf/releases/download/v$QpdfVersion/qpdf-$QpdfVersion-bin-mingw64.zip"
        $zipFile = Join-Path $TempDir "qpdf.zip"
        $extractDir = Join-Path $TempDir "qpdf-extract"

        try {
            Write-Status "Downloading from: $downloadUrl"
            New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
            Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile -UseBasicParsing

            Write-Status "Extracting QPDF..."
            New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
            Expand-Archive -Path $zipFile -DestinationPath $extractDir -Force

            # Find extracted directory
            $qpdfExtracted = Get-ChildItem -Path $extractDir -Directory | Select-Object -First 1

            # Move to installation directory
            if (Test-Path $QpdfDir) {
                Remove-Item -Path $QpdfDir -Recurse -Force
            }
            Move-Item -Path $qpdfExtracted.FullName -Destination $QpdfDir -Force

            # Verify installation
            $version = & $qpdfExe --version 2>&1 | Select-Object -First 1
            Write-Success "QPDF $version installed successfully at: $QpdfDir"
            return $true
        }
        catch {
            Write-ErrorMsg "Failed to install QPDF: $_"
            return $false
        }
        finally {
            if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
            if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
        }
    }
    else {
        # On Linux/macOS, recommend system package manager
        Write-Status "On Linux/macOS, it's recommended to install QPDF via package manager:"
        Write-Status "  Ubuntu/Debian: sudo apt-get install qpdf"
        Write-Status "  macOS: brew install qpdf"
        Write-Status "  Fedora: sudo dnf install qpdf"

        # Check if it's available in PATH
        $qpdfCmd = Get-Command qpdf -ErrorAction SilentlyContinue
        if ($qpdfCmd) {
            $version = qpdf --version 2>&1 | Select-Object -First 1
            Write-Success "QPDF found in system PATH: $version"
            return $true
        }
        else {
            Write-Warning "QPDF not found. Please install via package manager."
            return $false
        }
    }
}

function Show-InstallationSummary {
    param(
        [hashtable]$Results
    )

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Installation Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    foreach ($tool in $Results.Keys) {
        $status = if ($Results[$tool]) { "SUCCESS" } else { "FAILED" }
        $color = if ($Results[$tool]) { "Green" } else { "Red" }
        Write-Host "$tool : $status" -ForegroundColor $color
    }

    Write-Host "`nInstallation directories:" -ForegroundColor Cyan
    if ($Results["VeraPDF"]) {
        Write-Host "  VeraPDF: $VeraPdfDir"
    }
    if ($Results["JHOVE"]) {
        Write-Host "  JHOVE  : $JhoveDir"
    }
    if ($Results["QPDF"]) {
        $qpdfCmd = Get-Command qpdf -ErrorAction SilentlyContinue
        if ($qpdfCmd) {
            Write-Host "  QPDF   : System PATH"
        } else {
            Write-Host "  QPDF   : $QpdfDir"
        }
    }
    Write-Host "========================================`n" -ForegroundColor Cyan
}

# Main installation process
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "PDF Validation Tools Installer" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$installResults = @{}

# Install VeraPDF
if (-not $SkipVeraPDF) {
    $installResults["VeraPDF"] = Install-VeraPDF
}

# Install JHOVE
if (-not $SkipJHOVE) {
    $installResults["JHOVE"] = Install-JHOVE
}

# Install QPDF
if (-not $SkipQPDF) {
    $installResults["QPDF"] = Install-QPDF
}

# Show summary
Show-InstallationSummary -Results $installResults

# Clean up temp directory
if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Exit with error if any installation failed
$failedTools = $installResults.Keys | Where-Object { -not $installResults[$_] }
if ($failedTools.Count -gt 0) {
    Write-ErrorMsg "Some tools failed to install: $($failedTools -join ', ')"
    exit 1
}

Write-Success "All tools installed successfully!"
exit 0
