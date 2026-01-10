# FluentPDF

High-quality, ethically-designed PDF application for Windows built on WinUI 3.

[![Build Status](https://github.com/rmondo/FluentPDF/actions/workflows/build.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/build.yml)
[![Tests](https://github.com/rmondo/FluentPDF/actions/workflows/test.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/test.yml)
[![Quality Analysis](https://github.com/rmondo/FluentPDF/actions/workflows/quality-analysis.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/quality-analysis.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- **Enterprise-grade PDF rendering** with PDFium
- **Modern MVVM architecture** with CommunityToolkit.Mvvm source generators
- **Verifiable quality** with ArchUnitNET automated architecture tests
- **Comprehensive observability** with Serilog + OpenTelemetry
- **Type-safe error handling** using FluentResults Result pattern
- **Testable architecture** with dependency injection and interface-based design

## Prerequisites

- **Windows 10 (version 1809 or later)** or **Windows 11**
- **Visual Studio 2022** with the following workloads:
  - .NET desktop development
  - Desktop development with C++ (for vcpkg)
  - Windows application development (for WinUI 3)
- **Git** (for cloning and vcpkg)
- **PowerShell 5.1+** (included with Windows)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/FluentPDF.git
cd FluentPDF
```

### 2. Build Native Dependencies

Build PDFium and QPDF native libraries using the automated vcpkg script:

```powershell
.\tools\build-libs.ps1
```

**Note:** First-time build takes approximately 30-60 minutes as vcpkg compiles PDFium and QPDF from source.

**For ARM64 Windows:**
```powershell
.\tools\build-libs.ps1 -Triplet arm64-windows
```

**For faster subsequent builds (with binary caching):**
```powershell
.\tools\build-libs.ps1 -UseCache
```

**To force a clean rebuild:**
```powershell
.\tools\build-libs.ps1 -Clean
```

### 3. Open and Build the Solution

1. Open `FluentPDF.sln` in Visual Studio 2022
2. Restore NuGet packages (automatic on first build)
3. Build the solution: **Ctrl+Shift+B**
4. Run the application: **F5**

**Command-line build:**
```bash
dotnet restore FluentPDF.sln
dotnet build FluentPDF.sln
```

## Architecture

```
FluentPDF
├── FluentPDF.App          # WinUI 3 presentation layer (MVVM)
│   ├── ViewModels/        # CommunityToolkit.Mvvm ViewModels
│   ├── Views/             # XAML pages and controls
│   └── Services/          # UI-specific services (navigation)
│
├── FluentPDF.Core         # Business logic (UI-agnostic, headless testable)
│   ├── ErrorHandling/     # FluentResults error types
│   ├── Logging/           # Serilog configuration
│   └── Services/          # Domain services and interfaces
│
└── FluentPDF.Rendering    # PDF rendering infrastructure
    └── P/Invoke/          # PDFium and QPDF native interop
```

### Architectural Principles

- **Core is UI-agnostic**: `FluentPDF.Core` has zero UI dependencies and can run headless
- **Dependency flow**: `App` → `Core` + `Rendering`, `Rendering` → `Core`
- **Interface-based design**: All services implement `I*Service` interfaces for testability
- **MVVM pattern**: ViewModels use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Error handling**: FluentResults `Result<T>` pattern instead of exceptions for expected failures
- **Observability**: Structured logging with Serilog (JSON format) + OpenTelemetry

## Testing

### Run All Tests

```bash
dotnet test FluentPDF.sln
```

### Run Specific Test Projects

```bash
# Architecture tests (enforces layering rules)
dotnet test tests/FluentPDF.Architecture.Tests

# Core unit tests (headless, no UI dependencies)
dotnet test tests/FluentPDF.Core.Tests

# UI tests (requires Windows runtime)
dotnet test tests/FluentPDF.App.Tests
```

### Code Quality Standards

- **ArchUnitNET** enforces architectural rules automatically:
  - Core cannot depend on App or Rendering
  - ViewModels must inherit from `ObservableObject`
  - Services must implement `I*Service` interfaces
- **File size limit**: Maximum 500 lines per file
- **Method size limit**: Maximum 50 lines per method
- **Test coverage**: Minimum 80% (90% for critical paths)

### Visual Test Explorer

Open **Test Explorer** in Visual Studio:
- **View** → **Test Explorer** (Ctrl+E, T)
- Run, debug, and view results for all tests

## Observability

### Structured Logging

Logs are written in JSON format to enable AI-powered analysis:

- **Log location**: `%LOCALAPPDATA%\Packages\FluentPDF_*\LocalState\logs\`
- **Format**: JSON (Serilog Compact JSON formatter)
- **Enrichers**: Machine name, environment, thread ID, correlation IDs
- **Rolling**: Daily log files, 7-day retention

### OpenTelemetry Integration

During development, logs are sent to .NET Aspire Dashboard:

```bash
# Start .NET Aspire Dashboard (if installed)
docker run --rm -it -p 18888:18888 -p 4317:4317 mcr.microsoft.com/dotnet/aspire-dashboard:8.0
```

Then navigate to: [http://localhost:18888](http://localhost:18888)

### Correlation IDs

All unhandled exceptions are logged with unique correlation IDs:
- Displayed in error dialogs for user support
- Logged with full context for debugging
- Enables tracing across distributed operations

## Error Handling

FluentPDF uses a multi-layered error handling approach:

1. **Result Pattern** (FluentResults): Type-safe error handling for expected failures
2. **Global UI Exception Handler**: Catches unhandled UI thread exceptions
3. **Task Exception Handler**: Catches unobserved background task exceptions
4. **AppDomain Handler**: Final safety net for non-UI thread exceptions

All exceptions are logged with structured metadata for AI analysis.

## Building Native Dependencies

The `build-libs.ps1` script automates native library compilation:

1. **Clones vcpkg** from Microsoft's official repository
2. **Bootstraps vcpkg** (compiles vcpkg itself)
3. **Installs PDFium and QPDF** using vcpkg's package manager
4. **Copies binaries** to `libs/{arch}/bin/` directory
5. **Copies headers** to `libs/{arch}/include/` for P/Invoke development

### Script Options

```powershell
# Basic usage
.\tools\build-libs.ps1

# Advanced options
.\tools\build-libs.ps1 `
    -Triplet arm64-windows `  # Target architecture
    -Clean `                  # Force rebuild
    -UseCache                 # Enable binary caching
```

### Troubleshooting vcpkg

If builds fail:

1. **Check Visual Studio installation**: Ensure C++ desktop development workload is installed
2. **Clean rebuild**: Run with `-Clean` flag
3. **Check vcpkg logs**: `tools/vcpkg/buildtrees/{package}/build-*.log`
4. **Update vcpkg**: Delete `tools/vcpkg/` and re-run script

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Ensure all tests pass: `dotnet test`
5. Ensure architecture tests pass (validates layering rules)
6. Submit a pull request

### Code Style

- Follow `.editorconfig` settings (enforced by Visual Studio)
- Use CommunityToolkit.Mvvm source generators for ViewModels
- Use `I*Service` interface pattern for all services
- Keep files under 500 lines, methods under 50 lines
- Write XML documentation for public APIs

## Project Structure

```
FluentPDF/
├── .spec-workflow/          # Specification workflow documents
│   └── specs/
│       └── project-foundation/
│           ├── requirements.md
│           ├── design.md
│           └── tasks.md
├── docs/                    # Additional documentation
│   ├── ARCHITECTURE.md      # Architectural decision records
│   └── TESTING.md           # Testing strategy
├── src/                     # Source code
│   ├── FluentPDF.App/       # WinUI 3 application
│   ├── FluentPDF.Core/      # Business logic
│   └── FluentPDF.Rendering/ # PDF rendering
├── tests/                   # Test projects
│   ├── FluentPDF.Architecture.Tests/
│   ├── FluentPDF.Core.Tests/
│   └── FluentPDF.App.Tests/
├── tools/                   # Build scripts
│   └── build-libs.ps1       # vcpkg build automation
├── libs/                    # Native libraries (generated by build-libs.ps1)
│   ├── x64/
│   │   ├── bin/             # DLL files
│   │   └── include/         # Header files
│   └── arm64/
├── Directory.Build.props    # Shared MSBuild properties
└── FluentPDF.sln            # Visual Studio solution
```

## License

[MIT License](LICENSE)

## Acknowledgments

- **PDFium**: Google's PDF rendering engine
- **QPDF**: PDF transformation library by Jay Berkenbilt
- **vcpkg**: Microsoft's C/C++ package manager
- **WinUI 3**: Microsoft's native Windows UI framework
- **CommunityToolkit.Mvvm**: .NET Community MVVM toolkit
- **FluentResults**: Functional error handling library
- **Serilog**: Structured logging framework
- **ArchUnitNET**: Architecture testing framework
