# FluentPDF CLI Automation Test Script
# Tests the CLI functionality for automated PDF opening and logging

param(
    [string]$AppPath = ".\src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\FluentPDF.App.exe",
    [string]$TestPdf = "",
    [int]$Delay = 2,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " FluentPDF CLI Automation Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if app exists
if (-not (Test-Path $AppPath)) {
    Write-Host "✗ Error: FluentPDF.App.exe not found at: $AppPath" -ForegroundColor Red
    Write-Host "  Please build the app first:" -ForegroundColor Yellow
    Write-Host "  dotnet build src/FluentPDF.App -p:Platform=x64" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ App found: $AppPath" -ForegroundColor Green

# Check if test PDF is provided or exists
if ([string]::IsNullOrEmpty($TestPdf)) {
    # Try to find a test PDF in common locations
    $searchPaths = @(
        ".\tests\test-files\*.pdf",
        ".\test-files\*.pdf",
        ".\*.pdf"
    )

    foreach ($pattern in $searchPaths) {
        $found = Get-ChildItem $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $TestPdf = $found.FullName
            break
        }
    }

    if ([string]::IsNullOrEmpty($TestPdf)) {
        Write-Host "✗ Error: No test PDF specified and none found" -ForegroundColor Red
        Write-Host "  Please provide a test PDF:" -ForegroundColor Yellow
        Write-Host "  .\scripts\test-cli.ps1 -TestPdf `"C:\path\to\test.pdf`"" -ForegroundColor Yellow
        exit 1
    }
}

if (-not (Test-Path $TestPdf)) {
    Write-Host "✗ Error: Test PDF not found: $TestPdf" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Test PDF: $TestPdf" -ForegroundColor Green
Write-Host ""

# Clear old debug log
$debugLog = "$env:LOCALAPPDATA\FluentPDF_Debug.log"
if (Test-Path $debugLog) {
    Remove-Item $debugLog -Force
    Write-Host "✓ Cleared old debug log" -ForegroundColor Green
}

# Build command line arguments
$args = @(
    "-o", "`"$TestPdf`"",
    "-ac",
    "-acd", $Delay,
    "-c"
)

if ($Verbose) {
    $args += "-v"
}

Write-Host "Running: $AppPath $($args -join ' ')" -ForegroundColor Cyan
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Application Output" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Run the app
$startTime = Get-Date
try {
    $process = Start-Process -FilePath $AppPath -ArgumentList $args -Wait -PassThru -NoNewWindow
    $exitCode = $process.ExitCode
} catch {
    Write-Host "✗ Error running app: $_" -ForegroundColor Red
    exit 1
}
$endTime = Get-Date
$duration = ($endTime - $startTime).TotalSeconds

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Test Results" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Exit Code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
Write-Host "Duration: $([math]::Round($duration, 2)) seconds" -ForegroundColor Cyan
Write-Host ""

# Check debug log
if (Test-Path $debugLog) {
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host " Debug Log (Last 30 lines)" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""

    Get-Content $debugLog -Tail 30 | ForEach-Object {
        if ($_ -match "FAIL|ERROR|Exception") {
            Write-Host $_ -ForegroundColor Red
        } elseif ($_ -match "SUCCESS|loaded|rendered") {
            Write-Host $_ -ForegroundColor Green
        } else {
            Write-Host $_
        }
    }

    Write-Host ""
    Write-Host "Full log: $debugLog" -ForegroundColor Gray
} else {
    Write-Host "⚠ Warning: Debug log not found at $debugLog" -ForegroundColor Yellow
}

# Check structured log
$structuredLogDir = "$env:TEMP\FluentPDF\logs"
if (Test-Path $structuredLogDir) {
    $latestLog = Get-ChildItem "$structuredLogDir\log-*.json" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latestLog) {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Cyan
        Write-Host " Structured Log (Last 15 entries)" -ForegroundColor Cyan
        Write-Host "=====================================" -ForegroundColor Cyan
        Write-Host ""

        Get-Content $latestLog.FullName -Tail 15 | ForEach-Object {
            try {
                $json = $_ | ConvertFrom-Json
                $timestamp = $json.'@t'.ToString("HH:mm:ss")
                $level = $json.'@l'
                $message = $json.'@m'

                $color = switch ($level) {
                    "Error" { "Red" }
                    "Warning" { "Yellow" }
                    "Information" { "Cyan" }
                    "Debug" { "Gray" }
                    default { "White" }
                }

                Write-Host "[$timestamp] [$level] $message" -ForegroundColor $color
            } catch {
                # Skip malformed JSON lines
            }
        }

        Write-Host ""
        Write-Host "Full log: $($latestLog.FullName)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "✓ Test PASSED" -ForegroundColor Green
    Write-Host "  PDF opened and closed successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Test FAILED" -ForegroundColor Red
    Write-Host "  Exit code: $exitCode" -ForegroundColor Red
    Write-Host "  Check logs above for details" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "To run again with verbose logging:" -ForegroundColor Cyan
Write-Host "  .\scripts\test-cli.ps1 -TestPdf `"$TestPdf`" -Verbose" -ForegroundColor Gray
Write-Host ""

exit $exitCode
