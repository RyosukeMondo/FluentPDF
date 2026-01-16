# FluentPDF - Claude Code Context

## Project Structure
- `src/FluentPDF.Core` - Business logic (.NET 8, cross-platform)
- `src/FluentPDF.Rendering` - PDF rendering (cross-platform)
- `src/FluentPDF.App` - WinUI 3 UI (Windows-only)
- `tests/` - xUnit tests

## Build Commands
```bash
# Cross-platform (Linux/macOS/Windows)
dotnet build src/FluentPDF.Core
dotnet build src/FluentPDF.Rendering
dotnet test tests/FluentPDF.Core.Tests

# Windows-only (WinUI 3) - requires x64 platform
dotnet build src/FluentPDF.App -p:Platform=x64
dotnet test tests/FluentPDF.Architecture.Tests
dotnet test tests/FluentPDF.App.Tests
```

## Windows Build Environment

### SSH to Windows PC
```bash
ssh ryosu@192.168.11.48
# Project synced to: C:\dev\FluentPDF
```

### Sync files to Windows
```bash
scp -r src tests *.sln Directory.Build.props ryosu@192.168.11.48:"C:/dev/FluentPDF/"
```

### Alternative: Vagrant Windows VM
```bash
cd ~/vagrant/windows-vm
vagrant up && vagrant ssh  # Start and connect
vagrant rsync              # Sync files to C:\vagrant
```

## CLI Diagnostic Commands

FluentPDF supports command-line diagnostic operations for testing and troubleshooting:

```bash
# Test rendering of a PDF file (returns exit code for automation)
FluentPDF.App.exe --test-render "path/to/file.pdf"
# Exit codes: 0=success, 1=load failed, 2=render failed, 3=UI failed

# Display system diagnostics (OS, .NET, PDFium version, memory)
FluentPDF.App.exe --diagnostics

# Render all pages of a PDF to PNG files
FluentPDF.App.exe --render-test "path/to/file.pdf" --output "output/directory"

# Enable verbose logging for any command
FluentPDF.App.exe --diagnostics --verbose

# Capture crash dumps on failures (Windows Error Reporting)
FluentPDF.App.exe --test-render "file.pdf" --capture-crash-dump
```

These commands execute without showing UI and are useful for:
- Automated testing in CI/CD pipelines
- Troubleshooting rendering issues
- Performance profiling
- Validating PDFium integration

## Spec Workflow
Specs in `.spec-workflow/specs/`. Use `spec-status` tool to check progress.
