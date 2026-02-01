# FluentPDF - Build Quick Reference

Quick reference for building FluentPDF Avalonia for production and development.

## Production Build (Windows)

### Quick Start
```powershell
# Build production executable
.\build-production.ps1

# Output:
# - artifacts/win-x64/FluentPDF.Avalonia.exe (65 MB)
# - releases/v1.0.0/win-x64/FluentPDF-win-x64-v1.0.0.zip (60 MB)
```

### Custom Options
```powershell
# Specific version
.\build-production.ps1 -Version "1.0.1"

# Different runtime
.\build-production.ps1 -Runtime "win-arm64"

# Skip tests
.\build-production.ps1 -SkipTests

# No ZIP package
.\build-production.ps1 -CreateZip:$false
```

### Test Build
```powershell
cd artifacts\win-x64
.\FluentPDF.Avalonia.exe
```

## Development Build (Windows)

### Standard Build
```powershell
# Build with existing script
.\build-avalonia.ps1

# Or manually
dotnet build src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj
```

### Run from Source
```powershell
cd src\FluentPDF.Avalonia
dotnet run
```

### Debug Mode
```powershell
# Build Debug configuration
dotnet build src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj -c Debug

# Run with debugger
dotnet run --project src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj -c Debug
```

## Cross-Platform Builds

### Linux (on Linux machine)
```bash
# Build
./build-avalonia.sh linux-x64

# Output
artifacts/linux-x64/FluentPDF.Avalonia
```

### macOS (on macOS machine)
```bash
# Build Intel
./build-avalonia.sh osx-x64

# Build Apple Silicon
./build-avalonia.sh osx-arm64

# Output
artifacts/osx-x64/FluentPDF.Avalonia.app
```

### Cross-Compile (Not Recommended)
```powershell
# Windows → Linux (may have issues)
dotnet publish -r linux-x64 --self-contained

# Windows → macOS (may have issues)
dotnet publish -r osx-x64 --self-contained
```

## Build Configurations

### Release (Production)
```powershell
dotnet build -c Release
```
- Optimized code
- No debug symbols
- Compressed output
- Self-contained

### Debug (Development)
```powershell
dotnet build -c Debug
```
- Debugging enabled
- Symbols included
- No optimization
- Framework-dependent

## Common Build Commands

### Clean Build
```powershell
# Clean all projects
dotnet clean

# Clean specific project
dotnet clean src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj

# Remove artifacts
Remove-Item -Recurse -Force artifacts\
```

### Restore Dependencies
```powershell
# Restore all
dotnet restore

# Restore specific
dotnet restore src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj
```

### Run Tests
```powershell
# All tests
dotnet test

# Specific project
dotnet test tests\FluentPDF.Core.Tests\FluentPDF.Core.Tests.csproj

# With verbosity
dotnet test --verbosity detailed
```

## Build Verification

### Check Output
```powershell
# List artifacts
dir artifacts\win-x64\

# Check size
(Get-Item artifacts\win-x64\FluentPDF.Avalonia.exe).Length / 1MB

# Verify checksum
Get-FileHash artifacts\win-x64\FluentPDF.Avalonia.exe -Algorithm SHA256
```

### Test Executable
```powershell
# Version info
.\artifacts\win-x64\FluentPDF.Avalonia.exe --version

# Open PDF
.\artifacts\win-x64\FluentPDF.Avalonia.exe test.pdf

# API server
.\artifacts\win-x64\FluentPDF.Avalonia.exe --api-server
```

## Troubleshooting

### Build Fails
```powershell
# Clean and rebuild
dotnet clean
Remove-Item -Recurse -Force artifacts\, releases\
dotnet restore
.\build-production.ps1
```

### Missing Dependencies
```powershell
# Restore NuGet packages
dotnet restore

# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

### XAML Errors
```powershell
# Build with detailed output
dotnet build -v detailed

# Check specific project
dotnet build src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj -v detailed
```

### Runtime Errors
```powershell
# Check for missing DLLs
cd artifacts\win-x64
dir *.dll

# Verify PDFium
Test-Path .\pdfium.dll
```

## Performance Tips

### Faster Builds
```powershell
# Parallel build
dotnet build -maxcpucount

# Skip tests
.\build-production.ps1 -SkipTests

# Incremental build (reuse cached)
dotnet build --no-restore
```

### Smaller Output
```powershell
# Enable trimming (requires code changes)
dotnet publish -p:PublishTrimmed=true

# Disable debug info
dotnet publish -p:DebugType=none -p:DebugSymbols=false
```

## CI/CD Integration

### GitHub Actions (Example)
```yaml
- name: Build Production
  run: |
    pwsh -File build-production.ps1 -Version ${{ github.ref_name }}

- name: Upload Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: FluentPDF-Windows
    path: releases/**
```

### Local Automation
```powershell
# Build all platforms (example)
.\build-production.ps1 -Runtime win-x64
.\build-production.ps1 -Runtime win-arm64

# Create release package
Compress-Archive -Path releases\* -DestinationPath FluentPDF-v1.0.0-All-Platforms.zip
```

## File Locations

### Source Code
- `src/FluentPDF.Core/` - Business logic
- `src/FluentPDF.Rendering/` - PDF rendering
- `src/FluentPDF.Avalonia/` - UI application

### Build Output
- `artifacts/win-x64/` - Build output
- `releases/v1.0.0/win-x64/` - Release packages
- `bin/`, `obj/` - Intermediate files

### Documentation
- `docs/` - User and developer guides
- `RELEASE_NOTES.md` - Version history
- `PRODUCTION_BUILD_SUMMARY.md` - Build details

## Version Management

### Update Version
Edit version in:
1. `src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj`
2. `src/FluentPDF.Core/FluentPDF.Core.csproj`
3. `src/FluentPDF.Rendering/FluentPDF.Rendering.csproj`

Or use build parameter:
```powershell
.\build-production.ps1 -Version "1.0.1"
```

### Version Format
- Development: `1.0.0-dev`
- Beta: `1.0.0-beta.1`
- Release Candidate: `1.0.0-rc.1`
- Production: `1.0.0`

## Quick Commands Cheat Sheet

```powershell
# Development
dotnet run --project src\FluentPDF.Avalonia\FluentPDF.Avalonia.csproj

# Production build
.\build-production.ps1

# Test
dotnet test

# Clean
dotnet clean && Remove-Item -Recurse -Force artifacts\

# Restore
dotnet restore

# Check version
.\artifacts\win-x64\FluentPDF.Avalonia.exe --version

# Generate checksums
Get-FileHash artifacts\win-x64\FluentPDF.Avalonia.exe -Algorithm SHA256
```

## Support

For more information:
- **Production Build:** See `PRODUCTION_BUILD_SUMMARY.md`
- **Deployment:** See `releases/v1.0.0/DEPLOYMENT_CHECKLIST.md`
- **Installation:** See `docs/INSTALLATION_GUIDE.md`
- **User Guide:** See `docs/USER_GUIDE.md`
