# Quick fix for ThumbnailsSidebar.axaml XAML compilation error
# Issue: Border control with multiple children (AVLN3000)
# Fix: Wrap children in Grid

$ErrorActionPreature = "Stop"

$filePath = "src\FluentPDF.Avalonia\Controls\ThumbnailsSidebar.axaml"

Write-Host "Fixing XAML compilation error in ThumbnailsSidebar.axaml..." -ForegroundColor Cyan

if (-not (Test-Path $filePath)) {
    Write-Host "ERROR: File not found: $filePath" -ForegroundColor Red
    exit 1
}

# Read file content
$content = Get-Content $filePath -Raw

# Create backup
$backupPath = "$filePath.backup"
Copy-Item $filePath $backupPath -Force
Write-Host "Created backup: $backupPath" -ForegroundColor Green

# Define the broken pattern
$brokenPattern = @'
                                    <Border Grid.Row="0"
                                            Background="White"
                                            MinHeight="160"
                                            MaxHeight="200"
                                            HorizontalAlignment="Center"
                                            VerticalAlignment="Center"
                                            Margin="4">

                                        <!-- Loading Indicator -->
                                        <ProgressBar IsIndeterminate="True"
                                                     Width="100"
                                                     IsVisible="{Binding IsLoading}"/>

                                        <!-- Thumbnail Image -->
                                        <Image Source="{Binding Thumbnail}"
                                               Stretch="Uniform"
                                               MaxWidth="140"
                                               MaxHeight="190"
                                               IsVisible="{Binding !IsLoading}"/>
                                    </Border>
'@

# Define the fixed pattern
$fixedPattern = @'
                                    <Border Grid.Row="0"
                                            Background="White"
                                            MinHeight="160"
                                            MaxHeight="200"
                                            HorizontalAlignment="Center"
                                            VerticalAlignment="Center"
                                            Margin="4">

                                        <Grid>
                                            <!-- Loading Indicator -->
                                            <ProgressBar IsIndeterminate="True"
                                                         Width="100"
                                                         IsVisible="{Binding IsLoading}"/>

                                            <!-- Thumbnail Image -->
                                            <Image Source="{Binding Thumbnail}"
                                                   Stretch="Uniform"
                                                   MaxWidth="140"
                                                   MaxHeight="190"
                                                   IsVisible="{Binding !IsLoading}"/>
                                        </Grid>
                                    </Border>
'@

# Check if pattern exists
if ($content -match [regex]::Escape($brokenPattern)) {
    Write-Host "Found broken pattern, applying fix..." -ForegroundColor Yellow
    $newContent = $content -replace [regex]::Escape($brokenPattern), $fixedPattern
    Set-Content -Path $filePath -Value $newContent -NoNewline
    Write-Host "✅ Fix applied successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Broken pattern not found. File may already be fixed or have different formatting." -ForegroundColor Yellow
    Write-Host "Attempting alternative fix method..." -ForegroundColor Yellow

    # Alternative: Use line-based replacement
    $lines = Get-Content $filePath
    $newLines = @()
    $inBorderSection = $false
    $borderIndent = ""

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Detect Border start
        if ($line -match '<Border Grid.Row="0"' -and $i -lt $lines.Count - 1 -and $lines[$i+1] -match 'Background="White"') {
            $inBorderSection = $true
            $borderIndent = ($line -match '^(\s+)' | Out-Null; $matches[1])
            $newLines += $line
            continue
        }

        # Skip to ProgressBar
        if ($inBorderSection -and $line -match '<ProgressBar') {
            # Insert Grid before ProgressBar
            $newLines += "$borderIndent                                        <Grid>"
            $newLines += $line
            continue
        }

        # Detect Border end
        if ($inBorderSection -and $line -match '</Border>') {
            # Insert Grid close before Border close
            $newLines += "$borderIndent                                        </Grid>"
            $newLines += $line
            $inBorderSection = $false
            continue
        }

        $newLines += $line
    }

    Set-Content -Path $filePath -Value $newLines
    Write-Host "✅ Alternative fix applied!" -ForegroundColor Green
}

Write-Host "`nVerifying fix..." -ForegroundColor Cyan
Write-Host "Building project to verify XAML is valid..." -ForegroundColor Cyan

$buildResult = & dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Debug 2>&1
$buildSuccess = $LASTEXITCODE -eq 0

if ($buildSuccess) {
    Write-Host "✅ Build succeeded! XAML error fixed." -ForegroundColor Green
    Write-Host "`nYou can now run the automated test suite:" -ForegroundColor Cyan
    Write-Host "  pwsh tools/test-avalonia-app.ps1" -ForegroundColor White
    Remove-Item $backupPath -Force
    Write-Host "Backup removed (no longer needed)" -ForegroundColor Gray
} else {
    Write-Host "❌ Build still failing. XAML fix may not have worked." -ForegroundColor Red
    Write-Host "Restoring from backup..." -ForegroundColor Yellow
    Copy-Item $backupPath $filePath -Force
    Write-Host "Original file restored." -ForegroundColor Yellow
    Write-Host "`nBuild output:" -ForegroundColor Red
    Write-Host $buildResult
}
