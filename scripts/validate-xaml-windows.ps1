# XAML Validation Script for Windows
# Checks for common issues that cause XamlCompiler.exe failures

param(
    [switch]$Verbose
)

Write-Host "FluentPDF XAML Validation" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

$ErrorCount = 0
$WarningCount = 0

# Function to report errors
function Report-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    $script:ErrorCount++
}

# Function to report warnings
function Report-Warning {
    param([string]$Message)
    Write-Host "[WARN]  $Message" -ForegroundColor Yellow
    $script:WarningCount++
}

# Function to report success
function Report-Success {
    param([string]$Message)
    Write-Host "[OK]    $Message" -ForegroundColor Green
}

# Check 1: Verify all XAML files have code-behind (except ResourceDictionaries)
Write-Host "1. Checking XAML code-behind files..." -ForegroundColor Yellow

$xamlFiles = Get-ChildItem -Recurse -Path "src\FluentPDF.App" -Filter "*.xaml" | Where-Object { $_.Name -notlike "*.xaml.cs" }

foreach ($xaml in $xamlFiles) {
    $content = Get-Content $xaml.FullName -Raw
    $isResourceDictionary = $content -match '<ResourceDictionary'

    $codeFile = "$($xaml.FullName).cs"

    if (-not $isResourceDictionary -and -not (Test-Path $codeFile)) {
        Report-Error "Missing code-behind: $($xaml.Name) -> $($xaml.Name).cs"
    } elseif ($Verbose) {
        if ($isResourceDictionary) {
            Report-Success "$($xaml.Name) (ResourceDictionary, no code-behind needed)"
        } else {
            Report-Success "$($xaml.Name) has $($xaml.Name).cs"
        }
    }
}

# Check 2: Verify all converter classes exist
Write-Host ""
Write-Host "2. Checking converter class references..." -ForegroundColor Yellow

$converterRefs = Get-ChildItem -Recurse -Path "src\FluentPDF.App" -Filter "*.xaml" |
    ForEach-Object { Select-String -Path $_.FullName -Pattern 'converters:([A-Za-z]+)' } |
    ForEach-Object { $_.Matches.Groups[1].Value } |
    Sort-Object -Unique

$converterFiles = Get-ChildItem -Path "src\FluentPDF.App\Converters" -Filter "*.cs" |
    ForEach-Object { $_.BaseName }

foreach ($ref in $converterRefs) {
    if ($converterFiles -notcontains $ref) {
        Report-Error "Referenced converter not found: $ref"
    } elseif ($Verbose) {
        Report-Success "Converter exists: $ref"
    }
}

# Check 3: Verify namespace declarations
Write-Host ""
Write-Host "3. Checking XAML namespace declarations..." -ForegroundColor Yellow

foreach ($xaml in $xamlFiles) {
    $content = Get-Content $xaml.FullName -Raw

    # Check for common namespace issues
    if ($content -match 'xmlns:converters="using:FluentPDF\.App\.Converter"') {
        Report-Error "$($xaml.Name): Incorrect converter namespace (should be 'Converters' not 'Converter')"
    }

    if ($content -match 'xmlns:viewmodels="using:FluentPDF\.App\.ViewModel"') {
        Report-Error "$($xaml.Name): Incorrect viewmodel namespace (should be 'ViewModels' not 'ViewModel')"
    }

    if ($Verbose -and ($content -match 'xmlns:converters="using:FluentPDF\.App\.Converters"' -or
                       $content -match 'xmlns:viewmodels="using:FluentPDF\.App\.ViewModels"')) {
        Report-Success "$($xaml.Name) has correct namespace declarations"
    }
}

# Check 4: Verify x:Name uniqueness within files
Write-Host ""
Write-Host "4. Checking x:Name uniqueness..." -ForegroundColor Yellow

foreach ($xaml in $xamlFiles) {
    $content = Get-Content $xaml.FullName -Raw
    $names = [regex]::Matches($content, 'x:Name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }

    $duplicates = $names | Group-Object | Where-Object { $_.Count -gt 1 }

    if ($duplicates) {
        foreach ($dup in $duplicates) {
            Report-Error "$($xaml.Name): Duplicate x:Name '$($dup.Name)' found $($dup.Count) times"
        }
    } elseif ($Verbose -and $names.Count -gt 0) {
        Report-Success "$($xaml.Name) has $($names.Count) unique x:Name attributes"
    }
}

# Check 5: Check for common typos in XAML
Write-Host ""
Write-Host "5. Checking for common XAML typos..." -ForegroundColor Yellow

$typoPatterns = @{
    'Visiblity' = 'Visibility'
    'Visibilty' = 'Visibility'
    'Hieght' = 'Height'
    'Widht' = 'Width'
}

foreach ($xaml in $xamlFiles) {
    $content = Get-Content $xaml.FullName -Raw

    foreach ($typo in $typoPatterns.Keys) {
        if ($content -match $typo) {
            Report-Warning "$($xaml.Name): Possible typo '$typo' (did you mean '$($typoPatterns[$typo])'?)"
        }
    }
}

# Check 6: Verify ViewModel classes exist
Write-Host ""
Write-Host "6. Checking ViewModel references..." -ForegroundColor Yellow

$viewmodelRefs = Get-ChildItem -Recurse -Path "src\FluentPDF.App" -Filter "*.xaml" |
    ForEach-Object { Select-String -Path $_.FullName -Pattern 'viewmodels:([A-Za-z]+ViewModel)' } |
    ForEach-Object { $_.Matches.Groups[1].Value } |
    Sort-Object -Unique

if (Test-Path "src\FluentPDF.App\ViewModels") {
    $viewmodelFiles = Get-ChildItem -Path "src\FluentPDF.App\ViewModels" -Filter "*.cs" |
        ForEach-Object { $_.BaseName }

    foreach ($ref in $viewmodelRefs) {
        if ($viewmodelFiles -notcontains $ref) {
            Report-Error "Referenced ViewModel not found: $ref"
        } elseif ($Verbose) {
            Report-Success "ViewModel exists: $ref"
        }
    }
} elseif ($viewmodelRefs.Count -gt 0) {
    Report-Warning "ViewModels referenced but ViewModels directory not found"
}

# Summary
Write-Host ""
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "Errors:   $ErrorCount" -ForegroundColor $(if ($ErrorCount -gt 0) { "Red" } else { "Green" })
Write-Host "Warnings: $WarningCount" -ForegroundColor $(if ($WarningCount -gt 0) { "Yellow" } else { "Green" })
Write-Host ""

if ($ErrorCount -eq 0) {
    Write-Host "XAML validation passed! No blocking issues found." -ForegroundColor Green
    Write-Host "If build still fails, run: .\build-diagnostics-windows.ps1 -Clean" -ForegroundColor Gray
    exit 0
} else {
    Write-Host "XAML validation failed! Fix the errors above before building." -ForegroundColor Red
    exit 1
}
