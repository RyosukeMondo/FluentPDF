# FluentPDF Avalonia Deployment Guide

## Overview

FluentPDF Avalonia is a cross-platform PDF viewer built with Avalonia UI and .NET 8. This guide covers building, packaging, and deploying for Windows, macOS, and Linux.

## Prerequisites

### All Platforms
- .NET 8 SDK or later
- Git

### Windows
- Visual Studio 2022 (optional, for development)
- Windows 10/11 (x64 or ARM64)

### macOS
- Xcode Command Line Tools
- macOS 10.15 (Catalina) or later

### Linux
- Ubuntu 20.04+ / Debian 11+ / Fedora 36+ / Arch Linux
- Required packages: `libx11-dev`, `libice-dev`, `libsm-dev`

## Quick Start

### Windows

```powershell
# Clone repository
git clone https://github.com/your-org/FluentPDF.git
cd FluentPDF

# Build
.\build-avalonia.ps1

# Run
cd artifacts\win-x64
.\FluentPDF.Avalonia.exe
```

### macOS / Linux

```bash
# Clone repository
git clone https://github.com/your-org/FluentPDF.git
cd FluentPDF

# Build
chmod +x build-avalonia.sh
./build-avalonia.sh

# Run
cd artifacts/osx-x64  # or artifacts/linux-x64
./FluentPDF.Avalonia
```

## Build Options

### Standard Build

```bash
# Debug build (default)
dotnet build src/FluentPDF.Avalonia

# Release build
dotnet build src/FluentPDF.Avalonia --configuration Release
```

### Platform-Specific Build

```bash
# Windows
dotnet build src/FluentPDF.Avalonia --runtime win-x64 --configuration Release

# macOS (Intel)
dotnet build src/FluentPDF.Avalonia --runtime osx-x64 --configuration Release

# macOS (Apple Silicon)
dotnet build src/FluentPDF.Avalonia --runtime osx-arm64 --configuration Release

# Linux
dotnet build src/FluentPDF.Avalonia --runtime linux-x64 --configuration Release
```

### Self-Contained Deployment

```bash
# Windows (includes .NET runtime)
dotnet publish src/FluentPDF.Avalonia \
  --runtime win-x64 \
  --configuration Release \
  --self-contained true \
  --output artifacts/win-x64-standalone

# macOS
dotnet publish src/FluentPDF.Avalonia \
  --runtime osx-x64 \
  --configuration Release \
  --self-contained true \
  --output artifacts/osx-x64-standalone

# Linux
dotnet publish src/FluentPDF.Avalonia \
  --runtime linux-x64 \
  --configuration Release \
  --self-contained true \
  --output artifacts/linux-x64-standalone
```

### Single File Deployment

```bash
# Create single executable file (Windows example)
dotnet publish src/FluentPDF.Avalonia \
  --runtime win-x64 \
  --configuration Release \
  --self-contained true \
  --output artifacts/win-x64-single \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## Packaging

### Windows

#### Option 1: ZIP Archive

```powershell
# Build and package
.\build-avalonia.ps1 -Configuration Release -Runtime win-x64

# Create ZIP
Compress-Archive -Path artifacts\win-x64\* -DestinationPath FluentPDF-Avalonia-win-x64.zip
```

#### Option 2: MSIX Package (Microsoft Store)

```bash
# Install MSIX Packaging Tool
# Create MSIX manifest
# Build MSIX package
dotnet publish src/FluentPDF.Avalonia -c Release -r win-x64 -p:Platform=x64 -p:PublishProfile=MsixPackaging
```

### macOS

#### Option 1: .app Bundle

```bash
# Build
./build-avalonia.sh Release

# Create .app structure
mkdir -p FluentPDF.app/Contents/MacOS
mkdir -p FluentPDF.app/Contents/Resources

# Copy files
cp -r artifacts/osx-x64/* FluentPDF.app/Contents/MacOS/
cp -r assets/icon.icns FluentPDF.app/Contents/Resources/

# Create Info.plist
cat > FluentPDF.app/Contents/Info.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>FluentPDF.Avalonia</string>
    <key>CFBundleIconFile</key>
    <string>icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.fluentpdf.avalonia</string>
    <key>CFBundleName</key>
    <string>FluentPDF</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
</dict>
</plist>
EOF

# Create DMG
hdiutil create -volname "FluentPDF" -srcfolder FluentPDF.app -ov -format UDZO FluentPDF-Avalonia-osx.dmg
```

#### Option 2: Homebrew Cask

Create a Cask formula in `homebrew-fluentpdf` repository.

### Linux

#### Option 1: AppImage

```bash
# Install appimagetool
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage

# Create AppDir structure
mkdir -p FluentPDF.AppDir/usr/bin
mkdir -p FluentPDF.AppDir/usr/share/applications
mkdir -p FluentPDF.AppDir/usr/share/icons/hicolor/256x256/apps

# Copy files
cp -r artifacts/linux-x64/* FluentPDF.AppDir/usr/bin/
cp assets/icon.png FluentPDF.AppDir/usr/share/icons/hicolor/256x256/apps/fluentpdf.png

# Create desktop entry
cat > FluentPDF.AppDir/usr/share/applications/fluentpdf.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=FluentPDF
Exec=FluentPDF.Avalonia
Icon=fluentpdf
Categories=Office;Viewer;
EOF

# Build AppImage
./appimagetool-x86_64.AppImage FluentPDF.AppDir FluentPDF-Avalonia-linux-x86_64.AppImage
```

#### Option 2: Flatpak

Create a Flatpak manifest (`com.fluentpdf.Avalonia.yaml`):

```yaml
app-id: com.fluentpdf.Avalonia
runtime: org.freedesktop.Platform
runtime-version: '23.08'
sdk: org.freedesktop.Sdk
sdk-extensions:
  - org.freedesktop.Sdk.Extension.dotnet8
command: FluentPDF.Avalonia
finish-args:
  - --share=ipc
  - --socket=x11
  - --socket=wayland
  - --filesystem=home
modules:
  - name: FluentPDF
    buildsystem: simple
    build-commands:
      - dotnet publish -c Release -r linux-x64 --self-contained true -o /app/bin
    sources:
      - type: git
        url: https://github.com/your-org/FluentPDF.git
        tag: v1.0.0
```

Build with:
```bash
flatpak-builder --user --install --force-clean build-dir com.fluentpdf.Avalonia.yaml
```

#### Option 3: Snap

Create `snap/snapcraft.yaml`:

```yaml
name: fluentpdf-avalonia
version: '1.0.0'
summary: Cross-platform PDF viewer
description: |
  FluentPDF is a modern, cross-platform PDF viewer built with Avalonia UI.

base: core22
confinement: strict
grade: stable

apps:
  fluentpdf-avalonia:
    command: FluentPDF.Avalonia
    plugs:
      - home
      - desktop
      - desktop-legacy
      - x11
      - wayland

parts:
  fluentpdf:
    plugin: dotnet
    source: .
    dotnet-runtime-version: '8.0'
    dotnet-build-configuration: Release
```

Build with:
```bash
snapcraft
```

## Distribution

### Windows
- Microsoft Store (MSIX)
- Direct download (ZIP)
- Chocolatey package
- Winget package

### macOS
- Mac App Store
- Homebrew Cask
- Direct download (DMG)

### Linux
- Flathub
- Snap Store
- AppImage (direct download)
- Distribution-specific repositories (AUR, PPA, etc.)

## CI/CD Configuration

### GitHub Actions

Create `.github/workflows/build-avalonia.yml`:

```yaml
name: Build Avalonia

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, macos-latest, windows-latest]
        include:
          - os: ubuntu-latest
            runtime: linux-x64
          - os: macos-latest
            runtime: osx-x64
          - os: windows-latest
            runtime: win-x64

    runs-on: ${{ matrix.os }}

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore src/FluentPDF.Avalonia

    - name: Build
      run: dotnet build src/FluentPDF.Avalonia --configuration Release --runtime ${{ matrix.runtime }}

    - name: Test
      run: |
        dotnet test tests/FluentPDF.Core.Tests
        dotnet test tests/FluentPDF.Rendering.Tests

    - name: Publish
      run: dotnet publish src/FluentPDF.Avalonia --configuration Release --runtime ${{ matrix.runtime }} --self-contained true --output artifacts/${{ matrix.runtime }}

    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: fluentpdf-avalonia-${{ matrix.runtime }}
        path: artifacts/${{ matrix.runtime }}
```

## Troubleshooting

### Linux: Missing Dependencies

```bash
# Ubuntu/Debian
sudo apt-get install libx11-dev libice-dev libsm-dev

# Fedora
sudo dnf install libX11-devel libICE-devel libSM-devel

# Arch Linux
sudo pacman -S libx11 libice libsm
```

### macOS: Code Signing

For distribution outside the App Store:
```bash
codesign --deep --force --verify --verbose --sign "Developer ID Application: Your Name" FluentPDF.app
```

### Windows: Native Dependencies

Ensure `pdfium.dll` is included in the output directory. The build process automatically copies it from `runtimes/win-x64/native/`.

## Version Management

Use semantic versioning (SemVer):
- `1.0.0` - Initial Avalonia release
- `1.0.1` - Patch release
- `1.1.0` - Minor feature release
- `2.0.0` - Major breaking release

Update version in:
- `src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj`
- `CHANGELOG.md`
- Package manifests (MSIX, Flatpak, Snap)

## Support

For deployment issues, please file an issue at:
https://github.com/your-org/FluentPDF/issues
