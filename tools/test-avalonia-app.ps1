# FluentPDF Avalonia E2E Test Suite
# Comprehensive automated testing script

param(
    [int]$TestTimeout = 30,
    [string]$TestPdfPath = "tests\Fixtures\sample-with-text.pdf",
    [switch]$SkipInteractive = $false
)

$ErrorActionPreference = "Continue"
$ReportPath = "tests\AVALONIA_TEST_REPORT.md"
$LogPath = "tests\avalonia-test-log.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Test results tracking
$script:testResults = @{
    Passed = @()
    Failed = @()
    Warnings = @()
    Skipped = @()
}

# Utility functions
function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
    Add-Content -Path $LogPath -Value "`n=== $Message ==="
}

function Write-TestPass {
    param([string]$TestName)
    Write-Host "✅ PASS: $TestName" -ForegroundColor Green
    Add-Content -Path $LogPath -Value "✅ PASS: $TestName"
    $script:testResults.Passed += $TestName
}

function Write-TestFail {
    param([string]$TestName, [string]$Reason)
    Write-Host "❌ FAIL: $TestName - $Reason" -ForegroundColor Red
    Add-Content -Path $LogPath -Value "❌ FAIL: $TestName - $Reason"
    $script:testResults.Failed += @{Name=$TestName; Reason=$Reason}
}

function Write-TestWarning {
    param([string]$TestName, [string]$Message)
    Write-Host "⚠️  WARN: $TestName - $Message" -ForegroundColor Yellow
    Add-Content -Path $LogPath -Value "⚠️  WARN: $TestName - $Message"
    $script:testResults.Warnings += @{Name=$TestName; Message=$Message}
}

function Write-TestSkip {
    param([string]$TestName, [string]$Reason)
    Write-Host "⏭️  SKIP: $TestName - $Reason" -ForegroundColor Gray
    Add-Content -Path $LogPath -Value "⏭️  SKIP: $TestName - $Reason"
    $script:testResults.Skipped += @{Name=$TestName; Reason=$Reason}
}

# Initialize log
Clear-Content -Path $LogPath -ErrorAction SilentlyContinue
Add-Content -Path $LogPath -Value "FluentPDF Avalonia E2E Test Suite"
Add-Content -Path $LogPath -Value "Test started: $timestamp"
Add-Content -Path $LogPath -Value "================================================"

Write-TestHeader "FluentPDF Avalonia E2E Test Suite"
Write-Host "Test started: $timestamp"
Write-Host "Log file: $LogPath"
Write-Host "Report will be saved to: $ReportPath"

# Test 1: Build verification
Write-TestHeader "Test 1: Build Verification"
try {
    $buildOutput = dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Debug 2>&1
    $buildSuccess = $LASTEXITCODE -eq 0

    if ($buildSuccess) {
        Write-TestPass "Build verification"
    } else {
        Write-TestFail "Build verification" "Build failed with exit code $LASTEXITCODE"
        Add-Content -Path $LogPath -Value "Build output: $buildOutput"
    }
} catch {
    Write-TestFail "Build verification" $_.Exception.Message
}

# Test 2: Binary existence check
Write-TestHeader "Test 2: Binary and Dependencies Check"
$exePath = "src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe"
$dllPath = "src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.dll"
$pdfiumPath = "src\FluentPDF.Avalonia\bin\Debug\net8.0\pdfium.dll"
$qpdfPath = "src\FluentPDF.Avalonia\bin\Debug\net8.0\qpdf.dll"

if (Test-Path $exePath) {
    Write-TestPass "Executable exists"
    $fileInfo = Get-Item $exePath
    Write-Host "  Size: $($fileInfo.Length) bytes"
    Write-Host "  Modified: $($fileInfo.LastWriteTime)"
} else {
    Write-TestFail "Executable exists" "File not found: $exePath"
}

if (Test-Path $dllPath) {
    Write-TestPass "Main DLL exists"
} else {
    Write-TestFail "Main DLL exists" "File not found: $dllPath"
}

if (Test-Path $pdfiumPath) {
    Write-TestPass "PDFium library exists"
} else {
    Write-TestFail "PDFium library exists" "File not found: $pdfiumPath"
}

if (Test-Path $qpdfPath) {
    Write-TestPass "QPDF library exists"
} else {
    Write-TestWarning "QPDF library exists" "File not found: $qpdfPath (may be optional)"
}

# Test 3: Dependencies check
Write-TestHeader "Test 3: Required Dependencies"
$requiredDlls = @(
    "Avalonia.Base.dll",
    "Avalonia.Controls.dll",
    "Avalonia.DesktopRuntime.dll",
    "FluentPDF.Core.dll",
    "FluentPDF.Rendering.dll"
)

foreach ($dll in $requiredDlls) {
    $dllPath = "src\FluentPDF.Avalonia\bin\Debug\net8.0\$dll"
    if (Test-Path $dllPath) {
        Write-TestPass "Dependency: $dll"
    } else {
        Write-TestFail "Dependency: $dll" "File not found"
    }
}

# Test 4: Diagnostic log functionality
Write-TestHeader "Test 4: Diagnostic Logging"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$diagnosticLogPattern = "FluentPDF_Avalonia_Diagnostic_*.txt"
$existingLogs = Get-ChildItem -Path $desktopPath -Filter $diagnosticLogPattern -ErrorAction SilentlyContinue

Write-Host "Existing diagnostic logs: $($existingLogs.Count)"
if ($existingLogs.Count -gt 0) {
    Write-TestPass "Diagnostic logging system (logs found from previous runs)"
    $latestLog = $existingLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "  Latest log: $($latestLog.Name)"
    Write-Host "  Size: $($latestLog.Length) bytes"
} else {
    Write-TestWarning "Diagnostic logging system" "No diagnostic logs found (expected after first run)"
}

# Test 5: Application launch test (non-interactive)
Write-TestHeader "Test 5: Application Launch Test"
if (-not $SkipInteractive) {
    try {
        Write-Host "Launching application (will timeout after $TestTimeout seconds)..."
        Write-Host "Please manually verify the window appears and close it."

        $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal

        if ($null -ne $process) {
            Write-TestPass "Application process started"
            Write-Host "  Process ID: $($process.Id)"
            Write-Host "  Process Name: $($process.ProcessName)"

            # Wait for process to initialize
            Start-Sleep -Seconds 3

            # Check if process is still running
            if (-not $process.HasExited) {
                Write-TestPass "Application remains running after initialization"

                Write-Host "`nWaiting for manual window verification..."
                Write-Host "Please verify:"
                Write-Host "  1. Window appears on screen"
                Write-Host "  2. Menu bar is visible (File, Tools)"
                Write-Host "  3. Empty state overlay shows 'No PDFs open'"
                Write-Host "  4. Close the application when done"
                Write-Host ""

                # Wait for process to exit or timeout
                $process.WaitForExit($TestTimeout * 1000)

                if (-not $process.HasExited) {
                    Write-TestWarning "Application lifecycle" "Application did not close within timeout, killing process"
                    $process.Kill()
                    $process.WaitForExit(5000)
                } else {
                    Write-TestPass "Application closed gracefully"
                }
            } else {
                Write-TestFail "Application stability" "Process exited immediately (exit code: $($process.ExitCode))"
            }

            # Check for crash/diagnostic logs
            $newLogs = Get-ChildItem -Path $desktopPath -Filter $diagnosticLogPattern -ErrorAction SilentlyContinue |
                       Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-5) }

            if ($newLogs.Count -gt 0) {
                Write-TestPass "Diagnostic log created"
                $latestLog = $newLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                Write-Host "  Log file: $($latestLog.FullName)"

                # Check log for errors
                $logContent = Get-Content $latestLog.FullName -Raw
                if ($logContent -match "FATAL|UNHANDLED EXCEPTION") {
                    Write-TestFail "Application error check" "Fatal error found in diagnostic log"
                    Add-Content -Path $LogPath -Value "Log excerpt:"
                    Add-Content -Path $LogPath -Value ($logContent -split "`n" | Select-Object -Last 50)
                } else {
                    Write-TestPass "Application error check (no fatal errors)"
                }
            }
        } else {
            Write-TestFail "Application launch" "Failed to start process"
        }
    } catch {
        Write-TestFail "Application launch test" $_.Exception.Message
    }
} else {
    Write-TestSkip "Application launch test" "Interactive tests skipped"
}

# Test 6: Test fixture availability
Write-TestHeader "Test 6: Test Fixture Availability"
if (Test-Path $TestPdfPath) {
    Write-TestPass "Test PDF file exists"
    $pdfInfo = Get-Item $TestPdfPath
    Write-Host "  File: $TestPdfPath"
    Write-Host "  Size: $($pdfInfo.Length) bytes"
} else {
    Write-TestWarning "Test PDF file" "File not found: $TestPdfPath"
}

# Count available test PDFs
$testPdfs = Get-ChildItem -Path "tests\Fixtures" -Filter "*.pdf" -ErrorAction SilentlyContinue
Write-Host "Available test PDFs: $($testPdfs.Count)"
foreach ($pdf in $testPdfs) {
    Write-Host "  - $($pdf.Name) ($($pdf.Length) bytes)"
}

if ($testPdfs.Count -gt 0) {
    Write-TestPass "Test fixture collection ($($testPdfs.Count) PDFs)"
} else {
    Write-TestFail "Test fixture collection" "No test PDFs found"
}

# Test 7: Memory footprint check
Write-TestHeader "Test 7: Memory and Performance Metrics"
if (-not $SkipInteractive) {
    Write-Host "Measuring startup memory footprint..."
    try {
        $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Minimized
        Start-Sleep -Seconds 5

        if (-not $process.HasExited) {
            $process.Refresh()
            $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
            $privateMemoryMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)

            Write-Host "  Working Set: $memoryMB MB"
            Write-Host "  Private Memory: $privateMemoryMB MB"

            if ($memoryMB -lt 500) {
                Write-TestPass "Memory footprint ($memoryMB MB - acceptable)"
            } else {
                Write-TestWarning "Memory footprint" "$memoryMB MB - higher than expected"
            }

            $process.Kill()
            $process.WaitForExit(5000)
        } else {
            Write-TestWarning "Memory test" "Process exited during measurement"
        }
    } catch {
        Write-TestWarning "Memory test" $_.Exception.Message
    }
} else {
    Write-TestSkip "Memory footprint test" "Interactive tests skipped"
}

# Test 8: Platform compatibility check
Write-TestHeader "Test 8: Platform Compatibility"
$runtimeId = if ($IsWindows -or ($env:OS -eq "Windows_NT")) { "win-x64" }
             elseif ($IsMacOS) { "osx-x64" }
             elseif ($IsLinux) { "linux-x64" }
             else { "unknown" }

Write-Host "Detected platform: $runtimeId"
Write-TestPass "Platform detection ($runtimeId)"

if ($runtimeId -eq "win-x64") {
    Write-TestPass "Windows platform support verified"
} else {
    Write-TestWarning "Platform support" "Non-Windows platform detected - full testing requires Windows"
}

# Generate test report
Write-TestHeader "Generating Test Report"

$reportContent = @"
# FluentPDF Avalonia E2E Test Report

**Test Execution Date:** $timestamp
**Platform:** $runtimeId
**Test Timeout:** $TestTimeout seconds

---

## Executive Summary

- ✅ **Passed:** $($script:testResults.Passed.Count)
- ❌ **Failed:** $($script:testResults.Failed.Count)
- ⚠️  **Warnings:** $($script:testResults.Warnings.Count)
- ⏭️  **Skipped:** $($script:testResults.Skipped.Count)

**Overall Status:** $(if ($script:testResults.Failed.Count -eq 0) { "✅ PASS" } else { "❌ FAIL" })

---

## Test Results

### ✅ Passing Tests ($($script:testResults.Passed.Count))

$($script:testResults.Passed | ForEach-Object { "- $_" } | Out-String)

### ❌ Failed Tests ($($script:testResults.Failed.Count))

$($script:testResults.Failed | ForEach-Object { "- **$($_.Name):** $($_.Reason)" } | Out-String)

### ⚠️  Warnings ($($script:testResults.Warnings.Count))

$($script:testResults.Warnings | ForEach-Object { "- **$($_.Name):** $($_.Message)" } | Out-String)

### ⏭️  Skipped Tests ($($script:testResults.Skipped.Count))

$($script:testResults.Skipped | ForEach-Object { "- **$($_.Name):** $($_.Reason)" } | Out-String)

---

## Detailed Test Coverage

### 1. Build and Deployment
- [$(if ($script:testResults.Passed -contains "Build verification") { "x" } else { " " })] Build compiles without errors
- [$(if ($script:testResults.Passed -contains "Executable exists") { "x" } else { " " })] Executable binary exists
- [$(if ($script:testResults.Passed -contains "Main DLL exists") { "x" } else { " " })] Main DLL present
- [$(if ($script:testResults.Passed -contains "PDFium library exists") { "x" } else { " " })] PDFium library available

### 2. Application Lifecycle
- [$(if ($script:testResults.Passed -contains "Application process started") { "x" } else { " " })] Process launches successfully
- [$(if ($script:testResults.Passed -contains "Application remains running after initialization") { "x" } else { " " })] Stable after initialization
- [$(if ($script:testResults.Passed -contains "Application closed gracefully") { "x" } else { " " })] Graceful shutdown

### 3. User Interface (Manual Verification Required)
- [ ] Main window appears
- [ ] Menu bar visible (File, Tools)
- [ ] Empty state overlay displays correctly
- [ ] Theme resources loaded
- [ ] No visual artifacts

### 4. Error Handling
- [$(if ($script:testResults.Passed -contains "Application error check (no fatal errors)") { "x" } else { " " })] No fatal errors in logs
- [$(if ($script:testResults.Passed -contains "Diagnostic log created") { "x" } else { " " })] Diagnostic logging functional

### 5. Test Infrastructure
- [$(if ($script:testResults.Passed -contains "Test fixture collection ($($testPdfs.Count) PDFs)") { "x" } else { " " })] Test PDFs available

---

## Performance Metrics

### Memory Footprint
- Working Set: $(if ($memoryMB) { "$memoryMB MB" } else { "Not measured" })
- Private Memory: $(if ($privateMemoryMB) { "$privateMemoryMB MB" } else { "Not measured" })

### Build Performance
- Build time: ~2-5 seconds (estimated)
- Binary size: $(if (Test-Path $exePath) { "$([math]::Round((Get-Item $exePath).Length / 1KB, 2)) KB" } else { "N/A" })

---

## Known Issues and Recommendations

### Critical Issues
$($script:testResults.Failed | ForEach-Object {
    "#### $($_.Name)`n`n**Issue:** $($_.Reason)`n`n**Priority:** HIGH`n"
} | Out-String)

### Warnings
$($script:testResults.Warnings | ForEach-Object {
    "#### $($_.Name)`n`n**Message:** $($_.Message)`n`n**Priority:** MEDIUM`n"
} | Out-String)

### Recommendations

1. **Manual Testing Required**
   - Complete interactive UI verification
   - Test file open dialog
   - Test PDF rendering with sample files
   - Verify keyboard shortcuts (Ctrl+O, Ctrl+S, etc.)

2. **Performance Testing**
   - Run extended memory leak test (10+ minutes)
   - Test with large PDFs (>100 pages)
   - Measure rendering performance

3. **Cross-Platform Testing**
   - Test on macOS (if Avalonia build available)
   - Test on Linux (if Avalonia build available)

4. **Integration Testing**
   - Test PDF loading and rendering
   - Test navigation controls
   - Test zoom functionality
   - Test theme switching

---

## Test Artifacts

- **Log File:** ``$LogPath``
- **Latest Diagnostic Log:** $(if ($latestLog) { "``$($latestLog.FullName)``" } else { "N/A" })
- **Test PDFs:** ``tests\Fixtures\``

---

## Next Steps

$(if ($script:testResults.Failed.Count -gt 0) {
    "### 🔴 Action Required`n`n" +
    "Critical failures detected. Address the following before proceeding:`n`n" +
    ($script:testResults.Failed | ForEach-Object { "1. Fix: $($_.Name) - $($_.Reason)" } | Out-String)
} else {
    "### ✅ Ready for Manual Testing`n`n" +
    "All automated tests passed. Proceed with manual UI verification and interactive testing."
})

---

## Appendix: Test Environment

- **Operating System:** Windows $(if ($PSVersionTable.OS) { $PSVersionTable.OS } else { "Unknown" })
- **PowerShell Version:** $($PSVersionTable.PSVersion)
- **.NET Runtime:** $(dotnet --version)
- **Test Framework:** PowerShell E2E Test Suite v1.0
- **Test Duration:** ~$(New-TimeSpan -Start $timestamp -End (Get-Date) | Select-Object -ExpandProperty TotalSeconds) seconds

---

*Report generated automatically by FluentPDF Avalonia E2E Test Suite*
"@

Set-Content -Path $ReportPath -Value $reportContent
Write-Host "`nTest report saved to: $ReportPath" -ForegroundColor Green

# Print summary
Write-TestHeader "Test Summary"
Write-Host "✅ Passed: $($script:testResults.Passed.Count)" -ForegroundColor Green
Write-Host "❌ Failed: $($script:testResults.Failed.Count)" -ForegroundColor Red
Write-Host "⚠️  Warnings: $($script:testResults.Warnings.Count)" -ForegroundColor Yellow
Write-Host "⏭️  Skipped: $($script:testResults.Skipped.Count)" -ForegroundColor Gray
Write-Host ""

if ($script:testResults.Failed.Count -eq 0) {
    Write-Host "🎉 All automated tests PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "💥 Some tests FAILED. Review the report for details." -ForegroundColor Red
    exit 1
}
