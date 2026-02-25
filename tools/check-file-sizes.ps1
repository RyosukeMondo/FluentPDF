#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks C# files for KPI compliance (max 500 lines excluding comments/blanks)

.DESCRIPTION
    Scans FluentPDF.Avalonia for C# files and reports any exceeding 500 code lines.
    Excludes comments, blank lines, and generated files (obj/, bin/).

.PARAMETER Path
    Path to scan (defaults to src/FluentPDF.Avalonia)

.PARAMETER Threshold
    Line limit threshold (default: 500)

.PARAMETER ShowAll
    Show all files, not just violations

.EXAMPLE
    .\check-file-sizes.ps1
    # Scans default path and shows violations only

.EXAMPLE
    .\check-file-sizes.ps1 -ShowAll
    # Shows all files with line counts

.EXAMPLE
    .\check-file-sizes.ps1 -Threshold 400
    # Uses stricter threshold
#>

param(
    [string]$Path = "src/FluentPDF.Avalonia",
    [int]$Threshold = 500,
    [switch]$ShowAll
)

$ErrorActionPreference = "Stop"

# Resolve full path
$rootPath = Split-Path -Parent $PSScriptRoot
$scanPath = Join-Path $rootPath $Path

if (-not (Test-Path $scanPath)) {
    Write-Error "Path not found: $scanPath"
    exit 1
}

Write-Host "Scanning: $scanPath" -ForegroundColor Cyan
Write-Host "Threshold: $Threshold code lines (excluding comments/blanks)" -ForegroundColor Cyan
Write-Host ""

# Find all C# files (exclude obj/bin)
$files = Get-ChildItem -Path $scanPath -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "[\\/](obj|bin)[\\/]" }

$results = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $lines = $content -split "`r?`n"

    # Count total lines
    $totalLines = $lines.Count

    # Count code lines (exclude blanks and comments)
    $codeLines = 0
    $inBlockComment = $false

    foreach ($line in $lines) {
        $trimmed = $line.Trim()

        # Skip empty lines
        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        # Handle block comments
        if ($trimmed -match "^/\*") {
            $inBlockComment = $true
        }
        if ($inBlockComment) {
            if ($trimmed -match "\*/$") {
                $inBlockComment = $false
            }
            continue
        }

        # Skip single-line comments
        if ($trimmed -match "^//") {
            continue
        }

        # Skip XML documentation comments
        if ($trimmed -match "^///") {
            continue
        }

        # Count as code line
        $codeLines++
    }

    $relativePath = $file.FullName.Substring($rootPath.Length + 1)
    $overLimit = $codeLines - $Threshold
    $status = if ($codeLines -gt $Threshold) { "FAIL" } else { "PASS" }

    $results += [PSCustomObject]@{
        File = $relativePath
        TotalLines = $totalLines
        CodeLines = $codeLines
        OverLimit = $overLimit
        Status = $status
    }
}

# Sort by code lines descending
$results = $results | Sort-Object -Property CodeLines -Descending

# Filter violations if not showing all
$violations = $results | Where-Object { $_.Status -eq "FAIL" }
$displayResults = if ($ShowAll) { $results } else { $violations }

# Display results
if ($displayResults.Count -eq 0) {
    Write-Host "✅ All files pass! No violations found." -ForegroundColor Green
} else {
    $displayResults | Format-Table -AutoSize -Property `
        @{Label="Status"; Expression={
            if ($_.Status -eq "PASS") { "✅" } else { "❌" }
        }},
        @{Label="File"; Expression={$_.File}},
        @{Label="Total"; Expression={$_.TotalLines}; Align="Right"},
        @{Label="Code"; Expression={$_.CodeLines}; Align="Right"},
        @{Label="Over"; Expression={
            if ($_.OverLimit -gt 0) { "+$($_.OverLimit)" } else { $_.OverLimit }
        }; Align="Right"}
}

# Summary
$passCount = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = $violations.Count
$totalCount = $results.Count

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total Files: $totalCount" -ForegroundColor White
Write-Host "  Passing: $passCount" -ForegroundColor $(if ($passCount -eq $totalCount) { "Green" } else { "Yellow" })
Write-Host "  Violations: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

if ($failCount -gt 0) {
    Write-Host ""
    Write-Host "⚠️  $failCount file(s) exceed the $Threshold line limit" -ForegroundColor Yellow
    Write-Host "See FILE_SIZE_ANALYSIS.md for refactoring plan" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host ""
    Write-Host "✅ All files comply with the $Threshold line KPI" -ForegroundColor Green
    exit 0
}
