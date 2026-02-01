# Task 4.7 Cleanup Analysis Script
param(
    [string]$ProjectPath = "C:\Users\ryosu\repos\FluentPDF"
)

$results = @{
    FilesOver500Lines = @()
    DebugLogging = @()
    UnusedUsings = @()
    DeadCode = @()
    Warnings = @()
}

Write-Host "=== Task 4.7: Final Integration and Polish ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check file sizes
Write-Host "1. Checking file sizes (max 500 lines)..." -ForegroundColor Yellow
Get-ChildItem -Path "$ProjectPath\src\FluentPDF.App" -Include "*.cs" -Recurse | Where-Object {
    $_.DirectoryName -notlike "*\obj\*" -and $_.DirectoryName -notlike "*\bin\*"
} | ForEach-Object {
    $lines = (Get-Content $_.FullName | Measure-Object -Line).Lines
    if ($lines -gt 500) {
        $relativePath = $_.FullName.Replace("$ProjectPath\", "")
        $results.FilesOver500Lines += [PSCustomObject]@{
            File = $relativePath
            Lines = $lines
        }
        Write-Host "  ⚠ $relativePath : $lines lines" -ForegroundColor Red
    }
}

if ($results.FilesOver500Lines.Count -eq 0) {
    Write-Host "  ✓ All files under 500 lines" -ForegroundColor Green
}
Write-Host ""

# 2. Check for debug logging
Write-Host "2. Checking for debug logging..." -ForegroundColor Yellow
Get-ChildItem -Path "$ProjectPath\src\FluentPDF.App" -Include "*.cs" -Recurse | Where-Object {
    $_.DirectoryName -notlike "*\obj\*" -and $_.DirectoryName -notlike "*\bin\*"
} | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match '_logger\.LogDebug|_logger\.Debug|Debug\.WriteLine|Console\.WriteLine') {
        $relativePath = $_.FullName.Replace("$ProjectPath\", "")
        $results.DebugLogging += $relativePath
    }
}

Write-Host "  Found $($results.DebugLogging.Count) files with debug logging" -ForegroundColor $(if ($results.DebugLogging.Count -gt 0) { "Yellow" } else { "Green" })
Write-Host ""

# 3. Build warnings check
Write-Host "3. Checking for build warnings..." -ForegroundColor Yellow
$buildOutput = dotnet build "$ProjectPath\src\FluentPDF.App\FluentPDF.App.csproj" -p:Platform=x64 --no-incremental 2>&1 | Out-String
$warningMatches = [regex]::Matches($buildOutput, "warning [A-Z]{2}[0-9]{4}")
$results.Warnings = $warningMatches | ForEach-Object { $_.Value } | Select-Object -Unique

if ($results.Warnings.Count -eq 0) {
    Write-Host "  ✓ No warnings found" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Found $($results.Warnings.Count) unique warning types:" -ForegroundColor Yellow
    $results.Warnings | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
}
Write-Host ""

# 4. Summary
Write-Host "=== Cleanup Summary ===" -ForegroundColor Cyan
Write-Host "Files over 500 lines: $($results.FilesOver500Lines.Count)" -ForegroundColor $(if ($results.FilesOver500Lines.Count -eq 0) { "Green" } else { "Red" })
Write-Host "Files with debug logging: $($results.DebugLogging.Count)" -ForegroundColor $(if ($results.DebugLogging.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host "Unique warning types: $($results.Warnings.Count)" -ForegroundColor $(if ($results.Warnings.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host ""

# Export results
$results | ConvertTo-Json -Depth 10 | Out-File "$ProjectPath\cleanup-analysis-results.json"
Write-Host "✓ Results exported to cleanup-analysis-results.json" -ForegroundColor Green

return $results
