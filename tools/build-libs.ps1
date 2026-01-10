<#
.SYNOPSIS
    Builds PDFium and QPDF native libraries using vcpkg.

.DESCRIPTION
    This script automates the process of building native PDF libraries (PDFium and QPDF)
    using Microsoft's vcpkg package manager. It handles vcpkg bootstrap, library installation,
    and copying of built artifacts to the libs/ directory.

.PARAMETER Triplet
    Target architecture triplet. Default: x64-windows
    Supported values: x64-windows, arm64-windows

.PARAMETER Clean
    Force rebuild by removing existing vcpkg installation and libraries.

.PARAMETER UseCache
    Enable vcpkg binary caching for faster builds.

.EXAMPLE
    .\build-libs.ps1
    Builds PDFium and QPDF for x64 Windows.

.EXAMPLE
    .\build-libs.ps1 -Triplet arm64-windows
    Builds for ARM64 Windows.

.EXAMPLE
    .\build-libs.ps1 -Clean -UseCache
    Force rebuild with binary caching enabled.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("x64-windows", "arm64-windows")]
    [string]$Triplet = "x64-windows",

    [Parameter(Mandatory = $false)]
    [switch]$Clean,

    [Parameter(Mandatory = $false)]
    [switch]$UseCache
)

# Error handling
$ErrorActionPreference = "Stop"

# Paths
$RootPath = Split-Path $PSScriptRoot -Parent
$VcpkgPath = Join-Path $PSScriptRoot "vcpkg"
$VcpkgExe = Join-Path $VcpkgPath "vcpkg.exe"
$VcpkgBootstrap = Join-Path $VcpkgPath "bootstrap-vcpkg.bat"
$ArchName = $Triplet -replace '-windows', ''
$LibsPath = Join-Path $RootPath "libs" $ArchName
$InstalledPath = Join-Path $VcpkgPath "installed" $Triplet

# Libraries to install
$Libraries = @("pdfium", "qpdf")

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

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Main script
try {
    Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║          FluentPDF Native Libraries Build Script              ║
║                   PDFium + QPDF via vcpkg                      ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

    Write-Host "Target Triplet: $Triplet"
    Write-Host "Output Directory: $LibsPath"
    Write-Host "Clean Build: $Clean"
    Write-Host "Use Cache: $UseCache"

    # Step 1: Clean if requested
    if ($Clean) {
        Write-Step "Cleaning existing vcpkg installation and libraries..."

        if (Test-Path $VcpkgPath) {
            Remove-Item -Path $VcpkgPath -Recurse -Force
            Write-Success "Removed vcpkg installation"
        }

        if (Test-Path $LibsPath) {
            Remove-Item -Path $LibsPath -Recurse -Force
            Write-Success "Removed existing libraries"
        }
    }

    # Step 2: Clone vcpkg if not exists
    if (-not (Test-Path $VcpkgPath)) {
        Write-Step "Cloning vcpkg from GitHub..."

        Push-Location $PSScriptRoot
        try {
            git clone https://github.com/microsoft/vcpkg.git vcpkg
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to clone vcpkg repository"
            }
            Write-Success "vcpkg cloned successfully"
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Success "vcpkg already exists at $VcpkgPath"
    }

    # Step 3: Bootstrap vcpkg if vcpkg.exe doesn't exist
    if (-not (Test-Path $VcpkgExe)) {
        Write-Step "Bootstrapping vcpkg..."

        if (-not (Test-Path $VcpkgBootstrap)) {
            throw "vcpkg bootstrap script not found at $VcpkgBootstrap"
        }

        Push-Location $VcpkgPath
        try {
            & cmd /c "bootstrap-vcpkg.bat"
            if ($LASTEXITCODE -ne 0) {
                throw "vcpkg bootstrap failed"
            }
            Write-Success "vcpkg bootstrapped successfully"
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Success "vcpkg.exe already exists"
    }

    # Step 4: Configure binary caching if requested
    if ($UseCache) {
        Write-Step "Configuring vcpkg binary caching..."

        $CachePath = Join-Path $env:LOCALAPPDATA "vcpkg" "cache"
        if (-not (Test-Path $CachePath)) {
            New-Item -Path $CachePath -ItemType Directory -Force | Out-Null
        }

        $env:VCPKG_BINARY_SOURCES = "clear;files,$CachePath,readwrite"
        Write-Success "Binary caching enabled: $CachePath"
    }

    # Step 5: Install libraries
    Write-Step "Installing libraries: $($Libraries -join ', ')"
    Write-Warning "This may take 30-60 minutes on first run (depending on hardware)"

    foreach ($Library in $Libraries) {
        $Package = "${Library}:${Triplet}"
        Write-Host "`nInstalling $Package..." -ForegroundColor Yellow

        Push-Location $VcpkgPath
        try {
            & .\vcpkg.exe install $Package
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to install $Package"
            }
            Write-Success "$Package installed successfully"
        }
        finally {
            Pop-Location
        }
    }

    # Step 6: Create output directories
    Write-Step "Creating output directories..."

    $BinPath = Join-Path $LibsPath "bin"
    $IncludePath = Join-Path $LibsPath "include"

    if (-not (Test-Path $BinPath)) {
        New-Item -Path $BinPath -ItemType Directory -Force | Out-Null
        Write-Success "Created $BinPath"
    }

    if (-not (Test-Path $IncludePath)) {
        New-Item -Path $IncludePath -ItemType Directory -Force | Out-Null
        Write-Success "Created $IncludePath"
    }

    # Step 7: Copy DLLs
    Write-Step "Copying DLLs to libs/$ArchName/bin/..."

    $SourceBinPath = Join-Path $InstalledPath "bin"
    if (Test-Path $SourceBinPath) {
        $DllFiles = Get-ChildItem -Path $SourceBinPath -Filter "*.dll" -File

        if ($DllFiles.Count -eq 0) {
            Write-Warning "No DLL files found in $SourceBinPath"
        }
        else {
            foreach ($Dll in $DllFiles) {
                Copy-Item -Path $Dll.FullName -Destination $BinPath -Force
                Write-Host "  Copied: $($Dll.Name)" -ForegroundColor Gray
            }
            Write-Success "Copied $($DllFiles.Count) DLL file(s)"
        }
    }
    else {
        Write-Warning "Source bin path not found: $SourceBinPath"
    }

    # Step 8: Copy include headers
    Write-Step "Copying include headers to libs/$ArchName/include/..."

    $SourceIncludePath = Join-Path $InstalledPath "include"
    if (Test-Path $SourceIncludePath) {
        # Copy PDFium headers
        $PdfiumInclude = Join-Path $SourceIncludePath "pdfium"
        if (Test-Path $PdfiumInclude) {
            $PdfiumDest = Join-Path $IncludePath "pdfium"
            Copy-Item -Path $PdfiumInclude -Destination $PdfiumDest -Recurse -Force
            Write-Success "Copied PDFium headers"
        }

        # Copy QPDF headers
        $QpdfInclude = Join-Path $SourceIncludePath "qpdf"
        if (Test-Path $QpdfInclude) {
            $QpdfDest = Join-Path $IncludePath "qpdf"
            Copy-Item -Path $QpdfInclude -Destination $QpdfDest -Recurse -Force
            Write-Success "Copied QPDF headers"
        }

        # Copy any top-level headers
        $HeaderFiles = Get-ChildItem -Path $SourceIncludePath -Filter "*.h" -File
        foreach ($Header in $HeaderFiles) {
            Copy-Item -Path $Header.FullName -Destination $IncludePath -Force
        }

        if ($HeaderFiles.Count -gt 0) {
            Write-Success "Copied $($HeaderFiles.Count) additional header file(s)"
        }
    }
    else {
        Write-Warning "Source include path not found: $SourceIncludePath"
    }

    # Step 9: Display summary
    Write-Host "`n" -NoNewline
    Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║                       BUILD COMPLETE                          ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

    Write-Success "Built PDFium and QPDF for $Triplet"
    Write-Success "DLLs copied to: $BinPath"
    Write-Success "Headers copied to: $IncludePath"

    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "  1. Add DLLs as Content items in FluentPDF.App.csproj:"
    Write-Host "     <Content Include=`"..\libs\$ArchName\bin\*.dll`">" -ForegroundColor Gray
    Write-Host "       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>" -ForegroundColor Gray
    Write-Host "     </Content>" -ForegroundColor Gray
    Write-Host "  2. Implement P/Invoke wrappers in FluentPDF.Rendering project"
    Write-Host "  3. Reference headers in libs/$ArchName/include/ for P/Invoke signatures"

    # List built DLLs
    if (Test-Path $BinPath) {
        $BuiltDlls = Get-ChildItem -Path $BinPath -Filter "*.dll" -File
        if ($BuiltDlls.Count -gt 0) {
            Write-Host "`nBuilt Libraries ($($BuiltDlls.Count)):" -ForegroundColor Cyan
            foreach ($Dll in $BuiltDlls) {
                $Size = [math]::Round($Dll.Length / 1MB, 2)
                Write-Host "  • $($Dll.Name) ($Size MB)" -ForegroundColor Gray
            }
        }
    }

    Write-Host ""
    exit 0
}
catch {
    Write-Host "`n" -NoNewline
    Write-Error "BUILD FAILED: $_"
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
