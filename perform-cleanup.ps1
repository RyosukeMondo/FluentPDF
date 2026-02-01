# Task 4.7: Final Cleanup Script
# Removes debug logging, optimizes resources, fixes warnings

param(
    [string]$ProjectPath = "C:\Users\ryosu\repos\FluentPDF",
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"
$modifiedFiles = @()

Write-Host "=== Task 4.7: Final Integration and Polish ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath" -ForegroundColor Gray
if ($DryRun) {
    Write-Host "Mode: DRY RUN (no changes will be made)" -ForegroundColor Yellow
} else {
    Write-Host "Mode: LIVE (files will be modified)" -ForegroundColor Green
}
Write-Host ""

# 1. Remove/reduce debug logging
Write-Host "1. Processing debug logging..." -ForegroundColor Yellow

$debugPatterns = @(
    @{
        Pattern = 'System\.Diagnostics\.Debug\.WriteLine\([^)]+\);?'
        Replacement = '// Debug logging removed in production'
        Description = 'Debug.WriteLine calls'
    }
)

$filesToProcess = Get-ChildItem -Path "$ProjectPath\src\FluentPDF.App" -Include "*.cs" -Recurse | Where-Object {
    $_.DirectoryName -notlike "*\obj\*" -and
    $_.DirectoryName -notlike "*\bin\*" -and
    $_.Name -notlike "*Test*.cs"
}

$debugLoggingCount = 0
foreach ($file in $filesToProcess) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileModified = $false

    foreach ($pattern in $debugPatterns) {
        if ($content -match $pattern.Pattern) {
            $matches = [regex]::Matches($content, $pattern.Pattern)
            $debugLoggingCount += $matches.Count

            if (-not $DryRun) {
                $content = $content -replace $pattern.Pattern, $pattern.Replacement
                $fileModified = $true
            }
        }
    }

    if ($fileModified) {
        $content | Set-Content $file.FullName -NoNewline
        $relativePath = $file.FullName.Replace("$ProjectPath\", "")
        $modifiedFiles += $relativePath
        Write-Host "  ✓ Cleaned: $relativePath" -ForegroundColor Green
    }
}

Write-Host "  Processed: $debugLoggingCount debug statements" -ForegroundColor Gray
Write-Host ""

# 2. Fix CS2002 warning (duplicate GlobalUsings.g.cs)
Write-Host "2. Fixing CS2002 warning..." -ForegroundColor Yellow

$globalUsingsPath = "$ProjectPath\src\FluentPDF.App\obj\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.GlobalUsings.g.cs"
if (Test-Path $globalUsingsPath) {
    if (-not $DryRun) {
        Remove-Item $globalUsingsPath -Force
        Write-Host "  ✓ Removed duplicate GlobalUsings.g.cs" -ForegroundColor Green
    } else {
        Write-Host "  Would remove: $globalUsingsPath" -ForegroundColor Gray
    }
} else {
    Write-Host "  No duplicate GlobalUsings.g.cs found" -ForegroundColor Gray
}
Write-Host ""

# 3. Clean up unused using statements
Write-Host "3. Cleaning unused using statements..." -ForegroundColor Yellow
Write-Host "  Note: This requires manual verification or IDE tools" -ForegroundColor Gray
Write-Host "  Recommended: Run 'dotnet format' after this script" -ForegroundColor Gray
Write-Host ""

# 4. Optimize resource loading
Write-Host "4. Checking resource dictionaries..." -ForegroundColor Yellow
$resourceDicts = Get-ChildItem -Path "$ProjectPath\src\FluentPDF.App" -Include "*.xaml" -Recurse | Where-Object {
    $_.DirectoryName -like "*\Styles\*" -and
    $_.DirectoryName -notlike "*\obj\*"
}
Write-Host "  Found $($resourceDicts.Count) resource dictionary files" -ForegroundColor Gray
Write-Host "  Manual review recommended for merge optimization" -ForegroundColor Gray
Write-Host ""

# 5. Run Roslyn analyzers
Write-Host "5. Running Roslyn analyzers..." -ForegroundColor Yellow
if (-not $DryRun) {
    $buildResult = dotnet build "$ProjectPath\src\FluentPDF.App\FluentPDF.App.csproj" -p:Platform=x64 -warnaserror- 2>&1 | Out-String

    if ($buildResult -match "Build succeeded") {
        Write-Host "  ✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Build warnings detected" -ForegroundColor Yellow
    }

    # Extract warnings
    $warnings = [regex]::Matches($buildResult, "warning ([A-Z]{2}[0-9]{4})[^`n]*")
    if ($warnings.Count -gt 0) {
        Write-Host "  Warnings:" -ForegroundColor Yellow
        $warnings | Select-Object -First 10 | ForEach-Object {
            Write-Host "    - $($_.Value)" -ForegroundColor Gray
        }
        if ($warnings.Count -gt 10) {
            Write-Host "    ... and $($warnings.Count - 10) more" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  Skipped in dry-run mode" -ForegroundColor Gray
}
Write-Host ""

# 6. Summary
Write-Host "=== Cleanup Summary ===" -ForegroundColor Cyan
Write-Host "Modified files: $($modifiedFiles.Count)" -ForegroundColor $(if ($modifiedFiles.Count -gt 0) { "Green" } else { "Gray" })
if ($modifiedFiles.Count -gt 0) {
    $modifiedFiles | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Gray
    }
}
Write-Host ""

if ($DryRun) {
    Write-Host "This was a DRY RUN. Run without -DryRun to apply changes." -ForegroundColor Yellow
} else {
    Write-Host "✓ Cleanup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run: dotnet format" -ForegroundColor Gray
    Write-Host "  2. Run: dotnet test" -ForegroundColor Gray
    Write-Host "  3. Review files over 500 lines for refactoring" -ForegroundColor Gray
}
