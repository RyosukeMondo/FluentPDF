# FluentPDF Command Reference

## 🎯 Simplest Commands (Use These!)

```powershell
# Go to project directory (if not already there)
cd C:\Users\ryosu\repos\FluentPDF

# Launch FluentPDF UI
.\run.ps1

# Launch with a PDF file
.\run.ps1 ui tests\Fixtures\sample.pdf

# Start API Server (for testing)
.\run.ps1 api

# Run all autonomous tests
.\run.ps1 test

# Build the application
.\run.ps1 build
```

---

## 📝 Alternative: Full Launch Script

```powershell
# Launch UI
pwsh tools\launch-uat.ps1

# Launch UI and build first
pwsh tools\launch-uat.ps1 -BuildFirst

# Launch API server
pwsh tools\launch-uat.ps1 -ApiServer

# Run complete test suite
pwsh tools\launch-uat.ps1 -RunTests
```

---

## 🔧 Direct Executable Commands

If you prefer to call the executable directly:

```powershell
# Set variable for convenience
$app = ".\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

# Launch UI
& $app

# Open PDF
& $app "tests\Fixtures\sample.pdf"

# Start API server
& $app --api-server --port 5000

# Test rendering
& $app --test-render "tests\Fixtures\sample.pdf"

# Export images
& $app --test-export-images "tests\Fixtures\sample.pdf" --output "C:\temp\export" --format png --dpi 150

# Apply stamp
& $app --test-stamp "tests\Fixtures\sample.pdf" --stamp APPROVED --output "stamped.pdf"
```

---

## 🧪 REST API Testing

```powershell
# 1. Start API server (in one terminal)
.\run.ps1 api

# 2. In another terminal, test endpoints
curl http://localhost:5000/api/health
curl http://localhost:5000/api/status

# 3. Test element verification
curl -X POST http://localhost:5000/api/verify/element `
  -H "Content-Type: application/json" `
  -d '{"automationId":"OpenFileButton","expectedProperties":{"isEnabled":true}}'
```

---

## 🏗️ Build Commands

```powershell
# Build FluentPDF.App only
dotnet build src\FluentPDF.App -p:Platform=x64

# Build specific configuration
dotnet build src\FluentPDF.App -p:Platform=x64 -c Release

# Build all projects
dotnet build FluentPDF.sln -p:Platform=x64

# Clean and rebuild
dotnet clean src\FluentPDF.App
dotnet build src\FluentPDF.App -p:Platform=x64
```

---

## 📊 Test Commands

```powershell
# Run unit tests
dotnet test tests\FluentPDF.Core.Tests
dotnet test tests\FluentPDF.Rendering.Tests

# Run autonomous test suite
pwsh tools\run-autonomous-tests.ps1

# View test reports
start tests\reports\test-report.html
```

---

## 🎨 UI Testing Commands

### Launch and Test Manually

```powershell
# 1. Launch FluentPDF
.\run.ps1

# 2. Try these actions:
#    - Open a PDF (Ctrl+O)
#    - Navigate pages (Arrow keys)
#    - Zoom in/out (Ctrl+/Ctrl-)
#    - Search (Ctrl+F)
#    - Add annotations (Highlight tool)
#    - Toggle presentation mode (F5)
```

---

## 🔍 Diagnostic Commands

```powershell
# Show diagnostics
& $app --diagnostics

# Test render with diagnostics
& $app --test-render "tests\Fixtures\sample.pdf" --verbose

# Capture crash dumps (if crashes occur)
& $app --test-render "tests\Fixtures\sample.pdf" --capture-crash-dump
```

---

## 📂 File Management

```powershell
# Find test PDFs
dir tests\Fixtures\*.pdf

# Create output directory for tests
New-Item -ItemType Directory -Path "C:\temp\fluentpdf-test" -Force

# View logs
dir "$env:LOCALAPPDATA\FluentPDF\logs"
```

---

## ⚡ Power User Commands

```powershell
# Build + Launch in one command
dotnet build src\FluentPDF.App -p:Platform=x64 && .\run.ps1

# Test multiple features at once
& $app --test-merge "file1.pdf;file2.pdf" --output "merged.pdf"
& $app --test-split "merged.pdf" --ranges "1-5;6-10" --output "C:\temp"
& $app --test-optimize "merged.pdf" --output "optimized.pdf"

# Chain API tests
.\run.ps1 api &
Start-Sleep 3
curl http://localhost:5000/api/health
curl http://localhost:5000/api/status
```

---

## 🚨 Troubleshooting Commands

```powershell
# Check if executable exists
Test-Path ".\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

# Check if port 5000 is in use
netstat -ano | findstr :5000

# Kill process using port 5000 (if needed)
# Get PID from netstat output, then:
Stop-Process -Id <PID> -Force

# Check .NET version
dotnet --version

# List running FluentPDF processes
Get-Process | Where-Object { $_.ProcessName -like "*FluentPDF*" }

# Kill all FluentPDF processes
Get-Process | Where-Object { $_.ProcessName -like "*FluentPDF*" } | Stop-Process -Force
```

---

## 📚 Documentation Commands

```powershell
# View quick start
cat QUICK_START.md

# View full UAT guide
cat UAT_GUIDE.md

# View implementation summary
cat IMPLEMENTATION_COMPLETE.md

# View Phase 2 features
cat PHASE_2_IMPLEMENTATION_COMPLETE.md

# View all markdown docs
dir *.md
```

---

## 💾 Backup Commands

```powershell
# Backup test results
Copy-Item -Recurse tests\reports "C:\backup\fluentpdf-reports-$(Get-Date -Format 'yyyyMMdd')"

# Backup configuration
Copy-Item -Recurse .claude-flow "C:\backup\claude-flow-$(Get-Date -Format 'yyyyMMdd')"
```

---

## 🎯 Common Workflows

### Workflow 1: Fresh Build and Test
```powershell
cd C:\Users\ryosu\repos\FluentPDF
.\run.ps1 build
.\run.ps1 test
start tests\reports\test-report.html
```

### Workflow 2: Quick Manual UAT
```powershell
cd C:\Users\ryosu\repos\FluentPDF
.\run.ps1 ui tests\Fixtures\sample.pdf
# Test manually using UAT_GUIDE.md checklist
```

### Workflow 3: API Development/Testing
```powershell
# Terminal 1: Start API server
cd C:\Users\ryosu\repos\FluentPDF
.\run.ps1 api

# Terminal 2: Test endpoints
curl http://localhost:5000/api/health
curl http://localhost:5000/
# Browse Swagger UI in browser
```

### Workflow 4: Feature Testing
```powershell
cd C:\Users\ryosu\repos\FluentPDF
$app = ".\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

# Test export images
& $app --test-export-images "tests\Fixtures\sample.pdf" --output "C:\temp\export" --format png --dpi 150

# Verify output
dir C:\temp\export\*.png
```

---

## 📖 Help Commands

```powershell
# Show run.ps1 help
.\run.ps1 help

# Show all available CLI options (when implemented)
& $app --help

# List all test commands
cat tools\TESTING.md
```

---

## 💡 Pro Tips

1. **Use tab completion**: Type `.\run.ps1 ` and press Tab to see options
2. **Alias for convenience**: Add to your PowerShell profile:
   ```powershell
   Set-Alias fp "C:\Users\ryosu\repos\FluentPDF\run.ps1"
   # Then use: fp ui, fp test, etc.
   ```
3. **Keep terminal open**: Use `-NoExit` flag to keep PowerShell open after command
4. **Quick rebuild**: `.\run.ps1 build && .\run.ps1 ui`
5. **Background API**: Add `&` at end to run in background (PowerShell 7+)
