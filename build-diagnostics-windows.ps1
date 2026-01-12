# Diagnostic build script for troubleshooting WinUI 3 XAML compiler issues
# Run this on Windows to get detailed build information

param(
    [string]$Platform = "x64",
    [switch]$Clean
)

Write-Host "FluentPDF Windows Build Diagnostics" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Display environment information
Write-Host "Environment Information:" -ForegroundColor Yellow
Write-Host "  .NET SDK Version: " -NoNewline
dotnet --version
Write-Host "  Windows SDK: " -NoNewline
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty KitsRoot10 -ErrorAction SilentlyContinue
Write-Host "  Platform: $Platform"
Write-Host ""

# Display WinUI/WindowsAppSDK version
Write-Host "Package Versions:" -ForegroundColor Yellow
$appCsproj = Get-Content "src\FluentPDF.App\FluentPDF.App.csproj"
if ($appCsproj -match 'Microsoft\.WindowsAppSDK.*Version="([^"]+)"') {
    Write-Host "  WindowsAppSDK: $($matches[1])"
}
if ($appCsproj -match 'Microsoft\.Windows\.SDK\.BuildTools.*Version="([^"]+)"') {
    Write-Host "  Windows SDK BuildTools: $($matches[1])"
}
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous build artifacts..." -ForegroundColor Yellow
    dotnet clean src\FluentPDF.App\FluentPDF.App.csproj -p:Platform=$Platform
    dotnet clean tests\FluentPDF.App.Tests\FluentPDF.App.Tests.csproj -p:Platform=$Platform
    Write-Host ""
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore src\FluentPDF.App\FluentPDF.App.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Build with maximum verbosity to capture XAML compiler output
Write-Host "Building FluentPDF.App with diagnostic verbosity..." -ForegroundColor Yellow
Write-Host "This will generate detailed logs including XAML compilation" -ForegroundColor Gray
Write-Host ""

$buildLog = "build-diagnostics-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
dotnet build src\FluentPDF.App\FluentPDF.App.csproj `
    -p:Platform=$Platform `
    -v:diag `
    -fl `
    -flp:"logfile=$buildLog;verbosity=diagnostic" `
    -p:XamlDebuggingInformationGeneration=true

$buildExitCode = $LASTEXITCODE

Write-Host ""
if ($buildExitCode -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host "Detailed log saved to: $buildLog" -ForegroundColor Gray

    # Try building tests
    Write-Host ""
    Write-Host "Building FluentPDF.App.Tests..." -ForegroundColor Yellow
    $testLog = "test-build-diagnostics-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    dotnet build tests\FluentPDF.App.Tests\FluentPDF.App.Tests.csproj `
        -p:Platform=$Platform `
        -v:diag `
        -fl `
        -flp:"logfile=$testLog;verbosity=diagnostic"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Test project build succeeded!" -ForegroundColor Green
        Write-Host "Test log saved to: $testLog" -ForegroundColor Gray
    } else {
        Write-Host "Test project build failed! Exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "Check log file: $testLog" -ForegroundColor Yellow
    }
} else {
    Write-Host "Build failed! Exit code: $buildExitCode" -ForegroundColor Red
    Write-Host "Detailed log saved to: $buildLog" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Common XAML Compiler Issues:" -ForegroundColor Yellow
    Write-Host "  1. Missing or mismatched Windows SDK versions" -ForegroundColor Gray
    Write-Host "  2. Corrupted NuGet package cache (try: dotnet nuget locals all --clear)" -ForegroundColor Gray
    Write-Host "  3. XAML namespace errors (check XAML files for typos)" -ForegroundColor Gray
    Write-Host "  4. Missing code-behind files (.xaml.cs)" -ForegroundColor Gray
    Write-Host "  5. Invalid x:Name or x:Bind expressions" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Search the log file for 'XamlCompiler' to find XAML-specific errors:" -ForegroundColor Yellow
    Write-Host "  Select-String -Path '$buildLog' -Pattern 'XamlCompiler|XAML'" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To extract XAML compiler messages from the log:" -ForegroundColor Cyan
Write-Host "  Select-String -Path '$buildLog' -Pattern 'XamlCompiler' -Context 5,5" -ForegroundColor Gray
Write-Host ""
Write-Host "To check for specific errors:" -ForegroundColor Cyan
Write-Host "  Select-String -Path '$buildLog' -Pattern 'error|Error|ERROR' | Select-Object -First 20" -ForegroundColor Gray

exit $buildExitCode
