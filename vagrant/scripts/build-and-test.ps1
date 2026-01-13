# FluentPDF Build and Test Script
# Run from Windows VM to build, test, and validate the application

param(
    [switch]$Build,
    [switch]$Test,
    [switch]$E2E,
    [switch]$Smoke,
    [switch]$All,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$projectRoot = "C:\fluentpdf"
$outputDir = "$projectRoot\src\FluentPDF.App\bin\x64\$Configuration\net8.0-windows10.0.19041.0\win-x64"
$libsDir = "$projectRoot\libs\x64\bin"
$resultsDir = "$projectRoot\TestResults"

# Colors
function Write-Success($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Error($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Write-Warning($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }

# Header
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FluentPDF Build & Test Automation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Default to All if no specific flag
if (-not ($Build -or $Test -or $E2E -or $Smoke)) {
    $All = $true
}

if ($All) {
    $Build = $true
    $Test = $true
    $Smoke = $true
}

# === PRE-FLIGHT CHECKS ===
Write-Info "Running pre-flight checks..."

if (-not (Test-Path "$projectRoot\FluentPDF.sln")) {
    Write-Error "FluentPDF.sln not found at $projectRoot"
    Write-Error "Run 'vagrant rsync' from host to sync files"
    exit 1
}

# Check native DLLs
$missingDlls = @()
if (-not (Test-Path "$libsDir\pdfium.dll")) { $missingDlls += "pdfium.dll" }
if (-not (Test-Path "$libsDir\qpdf.dll")) { $missingDlls += "qpdf.dll" }

if ($missingDlls.Count -gt 0) {
    Write-Warning "Missing native DLLs: $($missingDlls -join ', ')"
    Write-Info "Downloading missing dependencies..."

    # Download PDFium if missing
    if ("pdfium.dll" -in $missingDlls) {
        Write-Info "Downloading PDFium..."
        $pdfiumTgz = "$env:TEMP\pdfium.tgz"
        Invoke-WebRequest -Uri "https://github.com/bblanchon/pdfium-binaries/releases/latest/download/pdfium-win-x64.tgz" -OutFile $pdfiumTgz
        $extractPath = "$env:TEMP\pdfium-extract"
        New-Item -ItemType Directory -Force -Path $extractPath | Out-Null
        tar -xzf $pdfiumTgz -C $extractPath
        $dll = Get-ChildItem -Path $extractPath -Recurse -Filter "pdfium.dll" | Select-Object -First 1
        if ($dll) {
            New-Item -ItemType Directory -Force -Path $libsDir | Out-Null
            Copy-Item $dll.FullName "$libsDir\pdfium.dll" -Force
            Write-Success "pdfium.dll downloaded"
        }
        Remove-Item $pdfiumTgz, $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Download QPDF if missing
    if ("qpdf.dll" -in $missingDlls) {
        Write-Info "Downloading QPDF..."
        $qpdfZip = "$env:TEMP\qpdf.zip"
        Invoke-WebRequest -Uri "https://github.com/qpdf/qpdf/releases/download/v11.9.1/qpdf-11.9.1-msvc64.zip" -OutFile $qpdfZip
        Expand-Archive -Path $qpdfZip -DestinationPath "$env:TEMP\qpdf-extract" -Force
        $dll = Get-ChildItem -Path "$env:TEMP\qpdf-extract" -Recurse -Filter "qpdf*.dll" | Where-Object { $_.Name -match "qpdf\d+\.dll" } | Select-Object -First 1
        if ($dll) {
            New-Item -ItemType Directory -Force -Path $libsDir | Out-Null
            Copy-Item $dll.FullName "$libsDir\qpdf.dll" -Force
            Write-Success "qpdf.dll downloaded"
        }
        Remove-Item $qpdfZip, "$env:TEMP\qpdf-extract" -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Success "Pre-flight checks passed"
Write-Host ""

# === BUILD ===
if ($Build) {
    Write-Info "Building FluentPDF ($Configuration)..."

    Set-Location $projectRoot

    # Restore
    Write-Info "Restoring packages..."
    dotnet restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package restore failed"
        exit 1
    }

    # Build Core and Rendering (cross-platform)
    Write-Info "Building FluentPDF.Core..."
    dotnet build src\FluentPDF.Core -c $Configuration --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FluentPDF.Core build failed"
        exit 1
    }

    Write-Info "Building FluentPDF.Rendering..."
    dotnet build src\FluentPDF.Rendering -c $Configuration --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FluentPDF.Rendering build failed"
        exit 1
    }

    # Build App (Windows-only)
    Write-Info "Building FluentPDF.App..."
    dotnet build src\FluentPDF.App\FluentPDF.App.csproj -c $Configuration -p:Platform=x64 --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FluentPDF.App build failed"
        exit 1
    }

    # Copy native DLLs to output
    Write-Info "Copying native DLLs to output..."
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    Copy-Item "$libsDir\pdfium.dll" "$outputDir\" -Force -ErrorAction SilentlyContinue
    Copy-Item "$libsDir\qpdf.dll" "$outputDir\" -Force -ErrorAction SilentlyContinue

    Write-Success "Build completed successfully"
    Write-Host ""
}

# === UNIT TESTS ===
if ($Test) {
    Write-Info "Running unit tests..."

    Set-Location $projectRoot
    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

    # Run Core tests
    Write-Info "Running FluentPDF.Core.Tests..."
    dotnet test tests\FluentPDF.Core.Tests -c $Configuration --no-build --logger "trx;LogFileName=core-tests.trx" --results-directory $resultsDir
    $coreResult = $LASTEXITCODE

    # Run Rendering tests (requires native DLLs)
    Write-Info "Running FluentPDF.Rendering.Tests..."

    # Copy DLLs to test output
    $renderingTestOutput = "tests\FluentPDF.Rendering.Tests\bin\$Configuration\net8.0"
    New-Item -ItemType Directory -Force -Path $renderingTestOutput | Out-Null
    Copy-Item "$libsDir\pdfium.dll" "$renderingTestOutput\" -Force -ErrorAction SilentlyContinue
    Copy-Item "$libsDir\qpdf.dll" "$renderingTestOutput\" -Force -ErrorAction SilentlyContinue

    dotnet test tests\FluentPDF.Rendering.Tests -c $Configuration --no-build --logger "trx;LogFileName=rendering-tests.trx" --results-directory $resultsDir
    $renderingResult = $LASTEXITCODE

    if ($coreResult -eq 0 -and $renderingResult -eq 0) {
        Write-Success "All unit tests passed"
    } else {
        Write-Error "Some tests failed. See $resultsDir for details."
    }
    Write-Host ""
}

# === SMOKE TEST ===
if ($Smoke) {
    Write-Info "Running smoke test..."

    $exePath = "$outputDir\FluentPDF.App.exe"
    $debugLog = "$env:LOCALAPPDATA\FluentPDF_Debug.log"

    if (-not (Test-Path $exePath)) {
        Write-Error "FluentPDF.App.exe not found. Run with -Build first."
        exit 1
    }

    # Clear old log
    Remove-Item $debugLog -Force -ErrorAction SilentlyContinue

    # Launch app
    Write-Info "Launching FluentPDF.App.exe..."
    $process = Start-Process -FilePath $exePath -PassThru

    # Wait for initialization
    $timeout = 15
    $elapsed = 0
    $success = $false

    while ($elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++

        if ($process.HasExited) {
            Write-Error "App exited unexpectedly with code $($process.ExitCode)"
            break
        }

        if (Test-Path $debugLog) {
            $log = Get-Content $debugLog -Raw
            if ($log -match "Window activated") {
                $success = $true
                break
            }
        }
    }

    # Close app
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 2
        if (-not $process.HasExited) {
            $process.Kill()
        }
    }

    # Check results
    if ($success) {
        Write-Success "Smoke test passed - App launched successfully"

        # Show key log entries
        if (Test-Path $debugLog) {
            Write-Info "Key log entries:"
            Get-Content $debugLog | Select-String "PDFium|Host started|Window activated" | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Gray
            }
        }
    } else {
        Write-Error "Smoke test failed"
        if (Test-Path $debugLog) {
            Write-Info "Debug log:"
            Get-Content $debugLog
        }
        exit 1
    }
    Write-Host ""
}

# === E2E TESTS ===
if ($E2E) {
    Write-Info "Running E2E tests..."
    Write-Warning "E2E tests require a GUI session (RDP or virt-manager)"

    # Build E2E test project
    Write-Info "Building E2E tests..."
    dotnet build tests\FluentPDF.E2E.Tests -c $Configuration --verbosity quiet 2>$null

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "E2E test project has build errors - skipping"
    } else {
        # Run E2E tests
        dotnet test tests\FluentPDF.E2E.Tests -c $Configuration --logger "trx;LogFileName=e2e-tests.trx" --results-directory $resultsDir

        if ($LASTEXITCODE -eq 0) {
            Write-Success "E2E tests passed"
        } else {
            Write-Error "E2E tests failed"
        }
    }
    Write-Host ""
}

# === SUMMARY ===
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (Test-Path "$outputDir\FluentPDF.App.exe") {
    Write-Success "FluentPDF.App.exe ready at:"
    Write-Host "  $outputDir" -ForegroundColor Gray
}

if (Test-Path $resultsDir) {
    $testFiles = Get-ChildItem "$resultsDir\*.trx" -ErrorAction SilentlyContinue
    if ($testFiles) {
        Write-Info "Test results at: $resultsDir"
    }
}

Write-Host ""
