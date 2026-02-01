#!/usr/bin/env pwsh
# Theme Visual Verification Script
# Captures screenshots of the app in different themes for manual or automated comparison.

param(
    [string]$OutputDir = "theme-screenshots",
    [switch]$AutoClose
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $color = switch ($Level) {
        "INFO"    { "Cyan" }
        "SUCCESS" { "Green" }
        "ERROR"   { "Red" }
        "WARN"    { "Yellow" }
        default   { "White" }
    }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$outputPath = Join-Path $repoRoot $OutputDir

# Create output directory
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
}

Write-Log "Theme Visual Verification" "INFO"
Write-Log "=========================" "INFO"
Write-Log "Output directory: $outputPath" "INFO"
Write-Log ""
Write-Log "MANUAL VERIFICATION STEPS:" "INFO"
Write-Log ""
Write-Log "1. Launch the app:" "INFO"
Write-Log "   src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe" "INFO"
Write-Log ""
Write-Log "2. For each theme (Light, Dark, System):" "INFO"
Write-Log "   a. Go to Tools > Settings" "INFO"
Write-Log "   b. In Appearance section, select the theme" "INFO"
Write-Log "   c. Verify the following change immediately:" "INFO"
Write-Log "      - Window background color" "INFO"
Write-Log "      - Text colors" "INFO"
Write-Log "      - Toolbar background" "INFO"
Write-Log "      - Sidebar background" "INFO"
Write-Log "      - Dialog backgrounds (open any dialog)" "INFO"
Write-Log ""
Write-Log "3. Check for issues:" "INFO"
Write-Log "   [ ] Background stays white in dark mode (BUG)" "INFO"
Write-Log "   [ ] Text not visible (contrast issue)" "INFO"
Write-Log "   [ ] Some controls don't change (hardcoded)" "INFO"
Write-Log "   [ ] Theme doesn't apply until restart (event issue)" "INFO"
Write-Log ""
Write-Log "4. Take screenshots (Win+Shift+S) and save to:" "INFO"
Write-Log "   $outputPath" "INFO"
Write-Log ""

# Checklist file
$checklistPath = Join-Path $outputPath "theme-checklist.md"
$checklist = @"
# Theme Verification Checklist

Date: $(Get-Date -Format "yyyy-MM-dd HH:mm")

## Light Theme
- [ ] Window background is light/white
- [ ] Text is dark and readable
- [ ] Toolbar background is light
- [ ] Sidebar background is light
- [ ] Context menus have light background
- [ ] Dialogs have light background
- [ ] All controls are visible

## Dark Theme
- [ ] Window background is dark/black
- [ ] Text is light and readable
- [ ] Toolbar background is dark
- [ ] Sidebar background is dark
- [ ] Context menus have dark background
- [ ] Dialogs have dark background
- [ ] All controls are visible
- [ ] No white flashes or hardcoded areas

## System Theme
- [ ] Follows Windows theme setting
- [ ] Updates when Windows theme changes
- [ ] Transitions smoothly

## Issues Found
_List any issues here_

## Screenshots
- light-theme.png
- dark-theme.png
- system-theme-light.png
- system-theme-dark.png
"@

$checklist | Out-File -FilePath $checklistPath -Encoding UTF8
Write-Log "Created checklist at: $checklistPath" "SUCCESS"
Write-Log ""
Write-Log "Open the checklist and fill it out as you test." "INFO"

# Open the output directory
Start-Process explorer.exe $outputPath
