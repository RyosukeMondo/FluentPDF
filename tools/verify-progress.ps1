#!/usr/bin/env pwsh
# FluentPDF Progress Verification Script
# Run: pwsh tools/verify-progress.ps1

param(
    [switch]$Quick,      # Skip tests, just check build + file sizes
    [switch]$Verbose     # Show detailed output
)

$ErrorActionPreference = "Continue"
$script:passed = 0
$script:failed = 0
$script:warnings = 0

function Check($name, $condition, $detail = "") {
    if ($condition) {
        Write-Host "  PASS  $name" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  FAIL  $name $(if($detail){"- $detail"})" -ForegroundColor Red
        $script:failed++
    }
}

function Warn($name, $detail = "") {
    Write-Host "  WARN  $name $(if($detail){"- $detail"})" -ForegroundColor Yellow
    $script:warnings++
}

Write-Host "`n=== FluentPDF Progress Verification ===" -ForegroundColor Cyan
Write-Host "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm')`n"

# --- BUILD HEALTH ---
Write-Host "--- Build Health ---" -ForegroundColor Cyan

$buildCore = dotnet build src/FluentPDF.Core -c Debug --nologo -v q 2>&1
Check "FluentPDF.Core builds" ($LASTEXITCODE -eq 0)

$buildRendering = dotnet build src/FluentPDF.Rendering -c Debug --nologo -v q 2>&1
Check "FluentPDF.Rendering builds" ($LASTEXITCODE -eq 0)

$buildAvalonia = dotnet build src/FluentPDF.Avalonia -c Debug --nologo -v q 2>&1
Check "FluentPDF.Avalonia builds" ($LASTEXITCODE -eq 0)

# --- TEST HEALTH ---
if (-not $Quick) {
    Write-Host "`n--- Test Health ---" -ForegroundColor Cyan

    $testCore = dotnet test tests/FluentPDF.Core.Tests --nologo -v q 2>&1 | Out-String
    $corePass = $testCore -match "失敗:\s+0" -or $testCore -match "Failed:\s+0"
    Check "Core tests pass" $corePass

    $testArch = dotnet test tests/FluentPDF.Architecture.Tests --nologo -v q 2>&1 | Out-String
    $archPass = $testArch -match "失敗:\s+0" -or $testArch -match "Failed:\s+0"
    Check "Architecture tests pass" $archPass
}

# --- CODE QUALITY KPIs ---
Write-Host "`n--- Code Quality KPIs ---" -ForegroundColor Cyan

$maxLoc = 500
$maxFuncLoc = 50
$oversizedFiles = @()
$srcFiles = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin|Generated)\\" }

foreach ($file in $srcFiles) {
    $lines = (Get-Content $file.FullName | Where-Object { $_.Trim() -ne "" -and $_.Trim() -notmatch "^//" -and $_.Trim() -notmatch "^\*" }).Count
    if ($lines -gt $maxLoc) {
        $rel = $file.FullName.Replace((Get-Location).Path + "\", "")
        $oversizedFiles += "$rel ($lines LOC)"
    }
}

if ($oversizedFiles.Count -eq 0) {
    Check "All files <= $maxLoc LOC" $true
} else {
    Check "All files <= $maxLoc LOC" $false "$($oversizedFiles.Count) oversized"
    if ($Verbose) {
        $oversizedFiles | ForEach-Object { Write-Host "         $_" -ForegroundColor DarkYellow }
    } else {
        $oversizedFiles | Select-Object -First 5 | ForEach-Object { Write-Host "         $_" -ForegroundColor DarkYellow }
        if ($oversizedFiles.Count -gt 5) { Write-Host "         ... and $($oversizedFiles.Count - 5) more" -ForegroundColor DarkYellow }
    }
}

# --- FEATURE CHECKS ---
Write-Host "`n--- Feature Checks ---" -ForegroundColor Cyan

# Search highlighting
$hasSearchHighlight = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" } |
    Select-String -Pattern "SearchHighlight|HighlightOverlay|SearchOverlay" -Quiet
if ($hasSearchHighlight) { Check "Search highlight overlay exists" $true }
else { Warn "Search highlight overlay not yet implemented" }

# Search results panel
$hasSearchPanel = Get-ChildItem -Path "src" -Filter "*.axaml" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" } |
    Select-String -Pattern "SearchResult|ResultsPanel" -Quiet
if ($hasSearchPanel) { Check "Search results panel exists" $true }
else { Warn "Search results panel not yet implemented" }

# Annotation list sidebar
$hasAnnotList = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" } |
    Select-String -Pattern "AnnotationsListView|AnnotationListSidebar" -Quiet
if ($hasAnnotList) { Check "Annotation list sidebar exists" $true }
else { Warn "Annotation list sidebar not yet implemented" }

# Document metadata panel
$hasMetadata = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" } |
    Select-String -Pattern "MetadataPanel|DocumentMetadata" -Quiet
if ($hasMetadata) { Check "Document metadata panel exists" $true }
else { Warn "Document metadata panel not yet implemented" }

# Accessibility: AutomationProperties
$axamlFiles = Get-ChildItem -Path "src" -Filter "*.axaml" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$totalButtons = ($axamlFiles | Select-String -Pattern "<Button " | Measure-Object).Count
$labeledButtons = ($axamlFiles | Select-String -Pattern "AutomationProperties\.(Name|HelpText)" | Measure-Object).Count
if ($totalButtons -gt 0) {
    $pct = [math]::Round(($labeledButtons / $totalButtons) * 100)
    if ($pct -ge 80) { Check "Accessibility: $pct% buttons labeled ($labeledButtons/$totalButtons)" $true }
    else { Warn "Accessibility: $pct% buttons labeled ($labeledButtons/$totalButtons) — target 80%" }
} else {
    Warn "No buttons found in AXAML"
}

# --- SUMMARY ---
Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "  Passed:   $script:passed" -ForegroundColor Green
Write-Host "  Failed:   $script:failed" -ForegroundColor $(if($script:failed -gt 0){"Red"}else{"Green"})
Write-Host "  Warnings: $script:warnings" -ForegroundColor $(if($script:warnings -gt 0){"Yellow"}else{"Green"})

# --- NEXT TASK ---
Write-Host "`n--- Next Task ---" -ForegroundColor Cyan
$plan = Get-Content "tools/MASTER_PLAN.md" -Raw
if ($plan -match "\[NEXT\]\s+(.+)") {
    Write-Host "  $($Matches[1])" -ForegroundColor White
} else {
    Write-Host "  No [NEXT] task found in MASTER_PLAN.md" -ForegroundColor Yellow
}

Write-Host ""
exit $script:failed
