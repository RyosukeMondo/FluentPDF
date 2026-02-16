<#
.SYNOPSIS
    Downloads PDFium pre-built binaries and builds QPDF via vcpkg.

.DESCRIPTION
    PDFium is not available as a standard vcpkg port. This script downloads
    pre-built PDFium binaries from the pdfium-binaries GitHub releases and
    installs QPDF via vcpkg.

.PARAMETER Triplet
    Target architecture triplet. Default: x64-windows
    Supported values: x64-windows, arm64-windows

.PARAMETER Clean
    Force rebuild by removing existing vcpkg installation and libraries.

.PARAMETER UseCache
    Enable vcpkg binary caching for faster builds.

.EXAMPLE
    .\build-libs.ps1
    Downloads PDFium and builds QPDF for x64 Windows.

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

$ErrorActionPreference = "Stop"

# Paths
$RootPath = Split-Path $PSScriptRoot -Parent
$VcpkgPath = Join-Path $PSScriptRoot "vcpkg"
$VcpkgExe = Join-Path $VcpkgPath "vcpkg.exe"
$VcpkgBootstrap = Join-Path $VcpkgPath "bootstrap-vcpkg.bat"
$ArchName = $Triplet -replace '-windows', ''
$LibsPath = Join-Path $RootPath "libs" $ArchName
$InstalledPath = Join-Path $VcpkgPath "installed" $Triplet

# PDFium configuration - downloaded from bblanchon/pdfium-binaries
# Using 'latest' to always get the most recent stable build
if ($ArchName -eq "x64") {
    $PdfiumArchive = "pdfium-win-x64.tgz"
} else {
    $PdfiumArchive = "pdfium-win-arm64.tgz"
}
$PdfiumUrl = "https://github.com/bblanchon/pdfium-binaries/releases/latest/download/$PdfiumArchive"
$PdfiumTempDir = Join-Path $PSScriptRoot "pdfium-temp"

# vcpkg libraries (only QPDF, pdfium is downloaded separately)
$VcpkgLibraries = @("qpdf")

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host "[ERR] $Message" -ForegroundColor Red
}

try {
    Write-Host @"
===========================================================
  FluentPDF Native Libraries Build Script
  PDFium (pre-built) + QPDF (vcpkg)
===========================================================
"@ -ForegroundColor Magenta

    Write-Host "Target Triplet: $Triplet"
    Write-Host "Output Directory: $LibsPath"
    Write-Host "Clean Build: $Clean"
    Write-Host "Use Cache: $UseCache"

    # Step 1: Clean if requested
    if ($Clean) {
        Write-Step "Cleaning existing installations and libraries..."

        if (Test-Path $VcpkgPath) {
            Remove-Item -Path $VcpkgPath -Recurse -Force
            Write-Success "Removed vcpkg installation"
        }

        if (Test-Path $LibsPath) {
            Remove-Item -Path $LibsPath -Recurse -Force
            Write-Success "Removed existing libraries"
        }

        if (Test-Path $PdfiumTempDir) {
            Remove-Item -Path $PdfiumTempDir -Recurse -Force
            Write-Success "Removed PDFium temp directory"
        }
    }

    # Step 2: Create output directories
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

    # Step 3: Download PDFium pre-built binary
    $PdfiumDll = Join-Path $BinPath "pdfium.dll"
    if (Test-Path $PdfiumDll) {
        Write-Success "PDFium binary already exists at $PdfiumDll"
    }
    else {
        Write-Step "Downloading PDFium pre-built binary..."

        if (Test-Path $PdfiumTempDir) {
            Remove-Item -Path $PdfiumTempDir -Recurse -Force
        }
        New-Item -Path $PdfiumTempDir -ItemType Directory -Force | Out-Null

        $TgzPath = Join-Path $PdfiumTempDir $PdfiumArchive

        Write-Host "Downloading from: $PdfiumUrl"
        Invoke-WebRequest -Uri $PdfiumUrl -OutFile $TgzPath -UseBasicParsing
        Write-Success "Downloaded $PdfiumArchive"

        Write-Host "Extracting archive..."
        # Extract .tgz (tar.gz) archive
        tar -xzf $TgzPath -C $PdfiumTempDir
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to extract PDFium archive"
        }
        Write-Success "Extracted archive"

        # Copy pdfium.dll from the extracted archive
        $ExtractedDll = Get-ChildItem -Path $PdfiumTempDir -Filter "pdfium.dll" -Recurse -File | Select-Object -First 1
        if ($ExtractedDll) {
            Copy-Item -Path $ExtractedDll.FullName -Destination $BinPath -Force
            Write-Success "Copied pdfium.dll to $BinPath"
        }
        else {
            throw "pdfium.dll not found in the downloaded archive"
        }

        # Copy pdfium headers if present
        $ExtractedInclude = Get-ChildItem -Path $PdfiumTempDir -Directory -Recurse -Filter "include" | Select-Object -First 1
        if ($ExtractedInclude) {
            $PdfiumIncludeDest = Join-Path $IncludePath "pdfium"
            if (-not (Test-Path $PdfiumIncludeDest)) {
                New-Item -Path $PdfiumIncludeDest -ItemType Directory -Force | Out-Null
            }
            Copy-Item -Path (Join-Path $ExtractedInclude.FullName "*") -Destination $PdfiumIncludeDest -Recurse -Force
            Write-Success "Copied PDFium headers to $PdfiumIncludeDest"
        }

        # Copy pdfium.lib if present (for linking)
        $ExtractedLib = Get-ChildItem -Path $PdfiumTempDir -Filter "pdfium.dll.lib" -Recurse -File | Select-Object -First 1
        if (-not $ExtractedLib) {
            $ExtractedLib = Get-ChildItem -Path $PdfiumTempDir -Filter "pdfium.lib" -Recurse -File | Select-Object -First 1
        }
        if ($ExtractedLib) {
            $LibOutputPath = Join-Path $LibsPath "lib"
            if (-not (Test-Path $LibOutputPath)) {
                New-Item -Path $LibOutputPath -ItemType Directory -Force | Out-Null
            }
            Copy-Item -Path $ExtractedLib.FullName -Destination $LibOutputPath -Force
            Write-Success "Copied PDFium import library to $LibOutputPath"
        }

        # Cleanup temp directory
        Remove-Item -Path $PdfiumTempDir -Recurse -Force
        Write-Success "Cleaned up temp directory"
    }

    # Step 4: Clone vcpkg if not exists (for QPDF)
    if (-not (Test-Path $VcpkgPath)) {
        Write-Step "Cloning vcpkg from GitHub (for QPDF)..."

        Push-Location $PSScriptRoot
        try {
            git clone --depth 1 https://github.com/microsoft/vcpkg.git vcpkg
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

    # Step 5: Bootstrap vcpkg if vcpkg.exe doesn't exist
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

    # Step 6: Configure binary caching if requested
    if ($UseCache) {
        Write-Step "Configuring vcpkg binary caching..."

        $CachePath = Join-Path $env:LOCALAPPDATA "vcpkg" "cache"
        if (-not (Test-Path $CachePath)) {
            New-Item -Path $CachePath -ItemType Directory -Force | Out-Null
        }

        $env:VCPKG_BINARY_SOURCES = "clear;files,$CachePath,readwrite"
        Write-Success "Binary caching enabled: $CachePath"
    }

    # Step 7: Install QPDF via vcpkg
    Write-Step "Installing vcpkg libraries: $($VcpkgLibraries -join ', ')"

    foreach ($Library in $VcpkgLibraries) {
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

    # Step 8: Copy QPDF DLLs from vcpkg
    Write-Step "Copying QPDF DLLs to libs/$ArchName/bin/..."

    $SourceBinPath = Join-Path $InstalledPath "bin"
    if (Test-Path $SourceBinPath) {
        $DllFiles = Get-ChildItem -Path $SourceBinPath -Filter "*.dll" -File

        if ($DllFiles.Count -eq 0) {
            Write-Warn "No DLL files found in $SourceBinPath"
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
        Write-Warn "Source bin path not found: $SourceBinPath"
    }

    # Step 9: Copy QPDF headers from vcpkg
    Write-Step "Copying QPDF headers to libs/$ArchName/include/..."

    $SourceIncludePath = Join-Path $InstalledPath "include"
    if (Test-Path $SourceIncludePath) {
        $QpdfInclude = Join-Path $SourceIncludePath "qpdf"
        if (Test-Path $QpdfInclude) {
            $QpdfDest = Join-Path $IncludePath "qpdf"
            Copy-Item -Path $QpdfInclude -Destination $QpdfDest -Recurse -Force
            Write-Success "Copied QPDF headers"
        }

        $HeaderFiles = Get-ChildItem -Path $SourceIncludePath -Filter "*.h" -File
        foreach ($Header in $HeaderFiles) {
            Copy-Item -Path $Header.FullName -Destination $IncludePath -Force
        }

        if ($HeaderFiles.Count -gt 0) {
            Write-Success "Copied $($HeaderFiles.Count) additional header file(s)"
        }
    }
    else {
        Write-Warn "Source include path not found: $SourceIncludePath"
    }

    # Step 10: Display summary
    Write-Host @"

===========================================================
  BUILD COMPLETE
===========================================================
"@ -ForegroundColor Green

    Write-Success "PDFium downloaded and QPDF built for $Triplet"
    Write-Success "DLLs copied to: $BinPath"
    Write-Success "Headers copied to: $IncludePath"

    # List built DLLs
    if (Test-Path $BinPath) {
        $BuiltDlls = Get-ChildItem -Path $BinPath -Filter "*.dll" -File
        if ($BuiltDlls.Count -gt 0) {
            Write-Host "`nLibraries ($($BuiltDlls.Count)):" -ForegroundColor Cyan
            foreach ($Dll in $BuiltDlls) {
                $Size = [math]::Round($Dll.Length / 1MB, 2)
                Write-Host "  - $($Dll.Name) ($Size MB)" -ForegroundColor Gray
            }
        }
    }

    Write-Host ""
    exit 0
}
catch {
    Write-Host "`n" -NoNewline
    Write-Err "BUILD FAILED: $_"
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}
