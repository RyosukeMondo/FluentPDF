# Quick Start - Windows Build

Quick reference for building FluentPDF on Windows after syncing from Linux.

## Initial Setup (First Time Only)

1. **Verify Prerequisites**
   ```powershell
   # Check .NET SDK (need 8.x)
   dotnet --version

   # Check Windows SDK (need 10.0.19041.0+)
   Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots" | Select-Object KitsRoot10
   ```

2. **Sync Files from Linux**

   From Linux machine (192.168.11.48 or Vagrant VM):
   ```bash
   scp -r src tests *.sln Directory.Build.props ryosu@192.168.11.48:"C:/dev/FluentPDF/"
   ```

## Daily Build Workflow

### Option 1: Validate then Build (Recommended)

```powershell
cd C:\dev\FluentPDF

# First, validate XAML files for common issues
.\validate-xaml-windows.ps1

# If validation passes, build with diagnostics
.\build-diagnostics-windows.ps1 -Clean
```

### Option 2: Quick Diagnostic Build

```powershell
cd C:\dev\FluentPDF

# Clean build with diagnostics
.\build-diagnostics-windows.ps1 -Clean

# Regular build
.\build-diagnostics-windows.ps1
```

### Option 3: Manual Commands

```powershell
cd C:\dev\FluentPDF

# Cross-platform projects (no platform needed)
dotnet build src\FluentPDF.Core
dotnet build src\FluentPDF.Rendering
dotnet test tests\FluentPDF.Core.Tests

# Windows-only projects (require platform)
dotnet build src\FluentPDF.App -p:Platform=x64
dotnet test tests\FluentPDF.App.Tests -p:Platform=x64
```

## Troubleshooting XAML Compiler Errors

If you see `MSB3073: XamlCompiler.exe exited with code 1`:

```powershell
# Quick fix attempts:
# 1. Validate XAML files first
.\validate-xaml-windows.ps1 -Verbose

# 2. Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore src\FluentPDF.App

# 3. Clean rebuild
.\build-diagnostics-windows.ps1 -Clean

# 4. If still failing, check the detailed troubleshooting guide:
# See: WINDOWS-BUILD-TROUBLESHOOTING.md
```

## Running Snapshot Tests (Task 6)

Once the build succeeds:

```powershell
# Run all snapshot tests
dotnet test tests\FluentPDF.App.Tests -p:Platform=x64 --filter "FullyQualifiedName~Snapshot"

# Check for .received.txt files
Get-ChildItem -Recurse tests\FluentPDF.App.Tests\Snapshots\ -Filter "*.received.txt"

# After reviewing, rename .received.txt to .verified.txt to approve
# Then re-run tests to ensure they pass
```

## Project Structure

```
FluentPDF/
├── src/
│   ├── FluentPDF.Core/           # Cross-platform (build anywhere)
│   ├── FluentPDF.Rendering/      # Cross-platform (build anywhere)
│   └── FluentPDF.App/            # Windows-only (WinUI 3)
├── tests/
│   ├── FluentPDF.Core.Tests/     # Cross-platform
│   └── FluentPDF.App.Tests/      # Windows-only (WinUI 3)
└── build-diagnostics-windows.ps1 # Diagnostic build helper
```

## Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| Platform not found | Missing `-p:Platform=x64` | Add platform parameter |
| XamlCompiler exit 1 | XAML compilation issue | Run diagnostic script, see troubleshooting guide |
| Missing assembly | Out-of-order build | Build Core → Rendering → App |
| NuGet restore fail | Package cache corrupt | `dotnet nuget locals all --clear` |

## Need More Help?

- Full troubleshooting guide: `WINDOWS-BUILD-TROUBLESHOOTING.md`
- Diagnostic logs: Check `build-diagnostics-*.log` files
- Spec documentation: `.spec-workflow/specs/snapshot-testing/`
