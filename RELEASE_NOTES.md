# FluentPDF Avalonia v1.0.0

**Release Date:** January 28, 2026

## Overview

FluentPDF Avalonia is a modern, cross-platform PDF viewer built on Avalonia UI framework. This is the first production release, migrated from WinUI 3 to provide true cross-platform support.

## What's New

### Core Features
- ✅ **Cross-Platform PDF Viewer** - Windows, macOS, and Linux support
- ✅ **Modern UI Framework** - Built on Avalonia UI 11.3.9
- ✅ **File Dialog Support** - Native file dialogs for all platforms
- ✅ **Theme System** - Dark and light mode with system detection
- ✅ **REST API** - Optional automation API for testing (experimental)

### Architecture
- ✅ **Clean Migration** - WinUI 3 → Avalonia UI completed
- ✅ **Reusable Core** - 100% code reuse from FluentPDF.Core and FluentPDF.Rendering
- ✅ **Dependency Injection** - Microsoft.Extensions.Hosting with service registration
- ✅ **MVVM Pattern** - CommunityToolkit.Mvvm for ViewModels
- ✅ **Structured Logging** - Serilog with JSON formatting

### Technical Improvements
- ✅ **PDFium Integration** - Native library embedding for all platforms
- ✅ **Value Converters** - Type-safe data binding converters
- ✅ **Error Handling** - FluentResults for railway-oriented programming
- ✅ **Self-Contained Builds** - No .NET runtime installation required

## System Requirements

### Windows
- **OS:** Windows 10 version 1809 or later, Windows 11
- **Architecture:** x64, ARM64
- **Disk Space:** ~60-80 MB
- **Runtime:** None (self-contained)

### macOS
- **OS:** macOS 10.15 (Catalina) or later
- **Architecture:** Intel (x64), Apple Silicon (ARM64)
- **Disk Space:** ~60-80 MB
- **Runtime:** None (self-contained)

### Linux
- **OS:** Ubuntu 20.04+, Fedora 36+, Debian 11+, or equivalent
- **Architecture:** x64, ARM64
- **Dependencies:** libX11, libICE, libSM (usually pre-installed)
- **Disk Space:** ~60-80 MB
- **Runtime:** None (self-contained)

## Installation

### Windows

1. Download `FluentPDF-win-x64-v1.0.0.zip`
2. Verify checksum (optional):
   ```powershell
   Get-FileHash FluentPDF-win-x64-v1.0.0.zip -Algorithm SHA256
   # Compare with FluentPDF-win-x64-v1.0.0.zip.sha256
   ```
3. Extract to desired location (e.g., `C:\Program Files\FluentPDF`)
4. Run `FluentPDF.Avalonia.exe`

**Note:** Windows may show SmartScreen warning for unsigned executables. Click "More info" → "Run anyway".

### macOS

1. Download `FluentPDF-osx-x64-v1.0.0.zip` (Intel) or `FluentPDF-osx-arm64-v1.0.0.zip` (Apple Silicon)
2. Verify checksum (optional):
   ```bash
   shasum -a 256 FluentPDF-osx-x64-v1.0.0.zip
   # Compare with FluentPDF-osx-x64-v1.0.0.zip.sha256
   ```
3. Extract and move to Applications:
   ```bash
   unzip FluentPDF-osx-x64-v1.0.0.zip -d ~/Applications/
   ```
4. First launch requires Gatekeeper approval:
   - Right-click `FluentPDF.Avalonia.app` → Open
   - Or: `xattr -d com.apple.quarantine FluentPDF.Avalonia.app`

### Linux

1. Download `FluentPDF-linux-x64-v1.0.0.tar.gz`
2. Verify checksum (optional):
   ```bash
   sha256sum FluentPDF-linux-x64-v1.0.0.tar.gz
   # Compare with FluentPDF-linux-x64-v1.0.0.tar.gz.sha256
   ```
3. Extract and install:
   ```bash
   tar -xzf FluentPDF-linux-x64-v1.0.0.tar.gz
   sudo mv FluentPDF.Avalonia /usr/local/bin/
   chmod +x /usr/local/bin/FluentPDF.Avalonia
   ```
4. Run: `FluentPDF.Avalonia`

## Usage

### Opening PDF Files

**File Menu:**
1. Launch FluentPDF.Avalonia
2. Click "Open" button or use File menu
3. Select PDF file from dialog

**Command Line:**
```bash
# Windows
FluentPDF.Avalonia.exe "C:\path\to\document.pdf"

# macOS/Linux
./FluentPDF.Avalonia /path/to/document.pdf
```

**Drag and Drop:**
- Drag PDF file onto application window (feature in development)

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O (Cmd+O) | Open file |
| Ctrl+W (Cmd+W) | Close file |
| Ctrl+Q (Cmd+Q) | Quit application |
| F11 | Toggle fullscreen |
| Ctrl+T (Cmd+T) | Toggle theme (dark/light) |

### Theme System

FluentPDF automatically detects your system theme preference. You can manually toggle themes:

- **Windows:** Respects Windows Settings → Personalization → Colors
- **macOS:** Respects System Preferences → General → Appearance
- **Linux:** Respects GTK theme (dark/light variant)
- **Manual:** Use Ctrl+T (Cmd+T) or Settings menu

## Known Issues

### v1.0.0

1. **REST API (Experimental)**
   - REST API server requires manual configuration
   - Not recommended for production use
   - Documentation: `docs/verification-api.md`

2. **UI Components**
   - Some UI components still in development
   - Annotation tools not yet implemented
   - Bookmark navigation incomplete

3. **File Associations**
   - No automatic file association on installation
   - Manual setup required for "Open with FluentPDF"

4. **Performance**
   - Large PDF files (>500 pages) may have slow initial load
   - Memory usage optimization in progress

5. **macOS Notarization**
   - App not notarized, requires Gatekeeper bypass
   - Code signing planned for future releases

## Changelog

### v1.0.0 (2026-01-28)

**Migration**
- Complete WinUI 3 → Avalonia UI migration
- Cross-platform support (Windows, macOS, Linux)
- Self-contained deployment (no runtime required)

**Features**
- File dialog integration (all platforms)
- Theme system with dark/light modes
- System theme detection
- Value converters for data binding

**Architecture**
- Dependency injection with Microsoft.Extensions.Hosting
- MVVM with CommunityToolkit.Mvvm
- Serilog structured logging
- FluentResults error handling

**Build**
- Production build scripts
- Self-contained single-file executables
- Trimmed assemblies for smaller size
- Native library embedding

**Documentation**
- Avalonia deployment guide
- Component mapping (React ↔ XAML)
- Build and development workflows

## Upgrade Notes

This is the first release of FluentPDF Avalonia. For users of the previous WinUI 3 version:

- **No migration required** - FluentPDF Avalonia is a standalone application
- **Settings do not carry over** - Fresh installation
- **Feature parity** - Core PDF viewing features maintained
- **New features** - Cross-platform support, improved theme system

## Support

### Documentation
- User Guide: `docs/USER_GUIDE.md`
- Developer Guide: `docs/AVALONIA_DEPLOYMENT_GUIDE.md`
- Component Mapping: `docs/component-mapping.md`

### Issues
- GitHub Issues: https://github.com/yourusername/FluentPDF/issues
- Include OS version, error messages, and reproduction steps

### Community
- Discussions: GitHub Discussions
- Feature Requests: GitHub Issues with `enhancement` label

## Credits

### Dependencies
- **Avalonia UI** (11.3.9) - Cross-platform XAML framework
- **PDFium** - PDF rendering engine
- **CommunityToolkit.Mvvm** (8.2.1) - MVVM helpers
- **Serilog** (4.x) - Structured logging
- **FluentResults** (4.x) - Railway-oriented programming

### Contributors
- Lead Developer: [Your Name]
- PDF Engine: PDFium Team
- UI Framework: Avalonia Team

## License

[Specify your license here - e.g., MIT, Apache 2.0, GPL]

## Roadmap

### v1.1.0 (Q2 2026)
- File association support
- Drag-and-drop file opening
- macOS app notarization
- Performance optimizations

### v1.2.0 (Q3 2026)
- Annotation tools
- Bookmark navigation
- Search functionality
- Print support

### v2.0.0 (Q4 2026)
- Form filling
- Digital signatures
- Cloud storage integration
- Multi-document tabs

---

**Thank you for using FluentPDF!**

For the latest updates, visit: https://github.com/yourusername/FluentPDF
