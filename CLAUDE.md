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

## Spec Workflow
Specs in `.spec-workflow/specs/`. Use `spec-status` tool to check progress.
