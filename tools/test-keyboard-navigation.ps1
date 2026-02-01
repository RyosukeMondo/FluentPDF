#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Interactive test script for keyboard navigation verification.

.DESCRIPTION
    Guides the user through testing all keyboard shortcuts implemented in Phase 2.
    Provides step-by-step instructions and tracks test results.

.PARAMETER PdfPath
    Optional path to a PDF file to use for testing. If not specified, prompts for file selection.

.EXAMPLE
    .\test-keyboard-navigation.ps1
    .\test-keyboard-navigation.ps1 -PdfPath "C:\test.pdf"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$PdfPath
)

$ErrorActionPreference = "Stop"

# Color functions
function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Step { param([string]$Message) Write-Host "→ $Message" -ForegroundColor Yellow }

# Test result tracking
$script:TestResults = @{
    Passed = 0
    Failed = 0
    Skipped = 0
}

function Test-KeyboardShortcut {
    param(
        [string]$Category,
        [string]$Shortcut,
        [string]$ExpectedAction,
        [string]$TestInstructions
    )

    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host "Test: $Category - $Shortcut" -ForegroundColor Cyan
    Write-Host "Expected: $ExpectedAction" -ForegroundColor Gray

    if ($TestInstructions) {
        Write-Host "Instructions: $TestInstructions" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Step "Press $Shortcut in the FluentPDF window..."

    $result = Read-Host "Did it work as expected? (y/n/s to skip)"

    switch ($result.ToLower()) {
        'y' {
            Write-Success "Test passed"
            $script:TestResults.Passed++
            return $true
        }
        'n' {
            Write-Failure "Test failed"
            $script:TestResults.Failed++
            return $false
        }
        's' {
            Write-Host "⊘ Test skipped" -ForegroundColor Yellow
            $script:TestResults.Skipped++
            return $null
        }
        default {
            Write-Warning "Invalid response. Marking as skipped."
            $script:TestResults.Skipped++
            return $null
        }
    }
}

function Show-TestSummary {
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host "Test Summary" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

    $total = $script:TestResults.Passed + $script:TestResults.Failed + $script:TestResults.Skipped

    Write-Host "Total Tests: $total"
    Write-Success "Passed: $($script:TestResults.Passed)"
    Write-Failure "Failed: $($script:TestResults.Failed)"
    Write-Host "Skipped: $($script:TestResults.Skipped)" -ForegroundColor Yellow

    if ($script:TestResults.Failed -eq 0 -and $script:TestResults.Passed -gt 0) {
        Write-Host ""
        Write-Success "All tests passed! 🎉"
    }
    elseif ($script:TestResults.Failed -gt 0) {
        Write-Host ""
        Write-Failure "Some tests failed. Please review the implementation."
    }
}

# Main test execution
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "FluentPDF Keyboard Navigation Test Suite" -ForegroundColor Cyan
Write-Host "Phase 2: Keyboard Navigation Verification" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Find FluentPDF executable
$exePath = Join-Path $PSScriptRoot "..\src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

if (-not (Test-Path $exePath)) {
    Write-Failure "FluentPDF.App.exe not found at: $exePath"
    Write-Info "Please build the project first: dotnet build src/FluentPDF.App -p:Platform=x64"
    exit 1
}

Write-Success "Found FluentPDF at: $exePath"
Write-Host ""

# Launch FluentPDF
Write-Step "Launching FluentPDF..."
$process = Start-Process -FilePath $exePath -PassThru -ArgumentList $(if ($PdfPath) { $PdfPath } else { "" })
Start-Sleep -Seconds 2

if ($process.HasExited) {
    Write-Failure "FluentPDF failed to start"
    exit 1
}

Write-Success "FluentPDF launched (PID: $($process.Id))"
Write-Host ""
Write-Info "Position the FluentPDF window so you can see both it and this console."
Write-Info "Press Enter when ready to begin testing..."
Read-Host

# Test Categories

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Category 1: Standard Application Shortcuts" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Test-KeyboardShortcut -Category "Standard" -Shortcut "Ctrl+O" `
    -ExpectedAction "Opens file picker dialog" `
    -TestInstructions "Press Ctrl+O. File picker should appear. Cancel it."

Test-KeyboardShortcut -Category "Standard" -Shortcut "Ctrl+S" `
    -ExpectedAction "Saves document (if modified)" `
    -TestInstructions "Make a change, then press Ctrl+S. Document should save."

Test-KeyboardShortcut -Category "Standard" -Shortcut "Ctrl+," `
    -ExpectedAction "Opens settings dialog" `
    -TestInstructions "Press Ctrl+Comma. Settings dialog should appear. Close it."

Test-KeyboardShortcut -Category "Standard" -Shortcut "Ctrl+W" `
    -ExpectedAction "Closes current tab" `
    -TestInstructions "Press Ctrl+W. Current tab should close (may prompt to save)."

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Category 2: Page Navigation" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Write-Info "Open a multi-page PDF for these tests (Ctrl+O)"
Read-Host "Press Enter when PDF is loaded..."

Test-KeyboardShortcut -Category "Navigation" -Shortcut "Page Down" `
    -ExpectedAction "Navigates to next page" `
    -TestInstructions "Press Page Down. Should move to next page."

Test-KeyboardShortcut -Category "Navigation" -Shortcut "Page Up" `
    -ExpectedAction "Navigates to previous page" `
    -TestInstructions "Press Page Up. Should move to previous page."

Test-KeyboardShortcut -Category "Navigation" -Shortcut "Home" `
    -ExpectedAction "Jumps to first page" `
    -TestInstructions "Press Home. Should jump to page 1."

Test-KeyboardShortcut -Category "Navigation" -Shortcut "End" `
    -ExpectedAction "Jumps to last page" `
    -TestInstructions "Press End. Should jump to last page."

Test-KeyboardShortcut -Category "Navigation" -Shortcut "Ctrl+G" `
    -ExpectedAction "Opens 'Go to Page' dialog" `
    -TestInstructions "Press Ctrl+G. Dialog should appear with focused text input. Type page number and press Enter."

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Category 3: Zoom Controls" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Test-KeyboardShortcut -Category "Zoom" -Shortcut "Ctrl++" `
    -ExpectedAction "Zooms in by 25%" `
    -TestInstructions "Press Ctrl+Plus. Zoom should increase."

Test-KeyboardShortcut -Category "Zoom" -Shortcut "Ctrl+-" `
    -ExpectedAction "Zooms out by 25%" `
    -TestInstructions "Press Ctrl+Minus. Zoom should decrease."

Test-KeyboardShortcut -Category "Zoom" -Shortcut "Ctrl+0" `
    -ExpectedAction "Resets zoom to 100%" `
    -TestInstructions "Press Ctrl+Zero. Zoom should reset to 100%."

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Category 4: Panel Management" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Test-KeyboardShortcut -Category "Panels" -Shortcut "Ctrl+F" `
    -ExpectedAction "Opens search panel" `
    -TestInstructions "Press Ctrl+F. Search panel should appear and text box should be focused."

Test-KeyboardShortcut -Category "Panels" -Shortcut "Escape" `
    -ExpectedAction "Closes search panel" `
    -TestInstructions "With search panel open, press Escape. Panel should close."

Test-KeyboardShortcut -Category "Panels" -Shortcut "Ctrl+T" `
    -ExpectedAction "Toggles thumbnails sidebar" `
    -TestInstructions "Press Ctrl+T. Thumbnails should appear/disappear."

Test-KeyboardShortcut -Category "Panels" -Shortcut "Escape (multiple)" `
    -ExpectedAction "Closes all open panels" `
    -TestInstructions "Open thumbnails and search panel, then press Escape multiple times. All should close."

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Category 5: Focus Management" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Test-KeyboardShortcut -Category "Focus" -Shortcut "Tab" `
    -ExpectedAction "Cycles through interactive elements" `
    -TestInstructions "Press Tab multiple times. Focus should move through buttons/controls with visible focus ring."

Test-KeyboardShortcut -Category "Focus" -Shortcut "Shift+Tab" `
    -ExpectedAction "Cycles backwards through elements" `
    -TestInstructions "Press Shift+Tab. Focus should move backwards."

# Show final summary
Show-TestSummary

# Cleanup
Write-Host ""
Write-Step "Close FluentPDF when finished testing..."
Write-Host "Test results saved to console output." -ForegroundColor Gray

# Generate markdown report
$reportPath = Join-Path $PSScriptRoot "..\test-results-keyboard-navigation.md"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$report = @"
# Keyboard Navigation Test Results

**Date**: $timestamp
**Tester**: $env:USERNAME

## Summary

- **Total Tests**: $($script:TestResults.Passed + $script:TestResults.Failed + $script:TestResults.Skipped)
- **Passed**: $($script:TestResults.Passed)
- **Failed**: $($script:TestResults.Failed)
- **Skipped**: $($script:TestResults.Skipped)

## Test Status

"@

if ($script:TestResults.Failed -eq 0 -and $script:TestResults.Passed -gt 0) {
    $report += "`n✅ **All tests passed**`n"
} else {
    $report += "`n⚠️ **Some tests failed - review required**`n"
}

$report | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host ""
Write-Info "Test report saved to: $reportPath"
