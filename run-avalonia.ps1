#!/usr/bin/env pwsh
# Launch FluentPDF Avalonia App

Write-Host "🚀 Launching FluentPDF Avalonia..." -ForegroundColor Cyan
Write-Host ""

# Build first
Write-Host "📦 Building project..." -ForegroundColor Yellow
Set-Location "$PSScriptRoot/src/FluentPDF.Avalonia"

$buildResult = dotnet build --nologo -v:quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""

# Check for PDFium library
$pdfiumPath = "bin/Debug/net8.0/pdfium.dll"
if (Test-Path $pdfiumPath) {
    $pdfiumSize = (Get-Item $pdfiumPath).Length / 1MB
    Write-Host "✅ PDFium library found: $([Math]::Round($pdfiumSize, 2)) MB" -ForegroundColor Green
} else {
    Write-Host "⚠️  PDFium library not found at $pdfiumPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎨 Starting Avalonia UI..." -ForegroundColor Cyan
Write-Host "   (Close the window to exit)" -ForegroundColor Gray
Write-Host ""

# Run the app
try {
    dotnet run --no-build
} catch {
    Write-Host "❌ Error running app: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ App closed successfully" -ForegroundColor Green
