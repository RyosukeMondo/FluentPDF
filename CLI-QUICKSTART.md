# FluentPDF CLI Quick Start

## TL;DR - Run Automated Test

```powershell
# Build the app
dotnet build src/FluentPDF.App -p:Platform=x64

# Run automated test (opens PDF, waits, closes, shows logs)
.\scripts\test-cli.ps1 -TestPdf "path\to\your\test.pdf"
```

## CLI Options

```bash
FluentPDF.App.exe [options] file.pdf

Options:
  -o, --open-file <path>    Open PDF file
  -ac, --auto-close         Auto-close after opening
  -acd <seconds>            Delay before closing (default: 2)
  -c, --console             Show console output
  -v, --verbose             Debug logging
  -l, --log-output <path>   Custom log file
```

## Common Use Cases

### 1. Test if PDF Renders (Autonomous Testing)

```powershell
# Opens, renders, closes automatically, shows console output
.\FluentPDF.App.exe -o "test.pdf" -ac -acd 3 -c -v
```

### 2. Check Logs After Run

```powershell
# Debug log (always available)
type "%LOCALAPPDATA%\FluentPDF_Debug.log"

# Structured log
type "%TEMP%\FluentPDF\logs\log-*.json" | tail -50
```

### 3. Test Multiple PDFs in CI/CD

```powershell
# Test all PDFs in a directory
Get-ChildItem *.pdf | ForEach-Object {
    Write-Host "Testing: $($_.Name)"
    .\FluentPDF.App.exe -o $_.FullName -ac -acd 2 -c

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ PASS" -ForegroundColor Green
    } else {
        Write-Host "✗ FAIL" -ForegroundColor Red
    }
}
```

### 4. Debug Rendering Issues

```powershell
# Enable verbose logging and console output
.\FluentPDF.App.exe -o "problem.pdf" -c -v

# Then check detailed logs
type "%LOCALAPPDATA%\FluentPDF_Debug.log"
```

## Log Files

| Log Type | Location | Purpose |
|----------|----------|---------|
| Debug | `%LOCALAPPDATA%\FluentPDF_Debug.log` | Simple timestamped startup/file opening log |
| Structured | `%TEMP%\FluentPDF\logs\log-<date>.json` | Detailed JSON logs with rendering metrics |
| Console | stdout | Real-time output when using `-c` flag |

## Troubleshooting

### Problem: PDF selected but nothing renders

1. **Check PDFium initialization**:
   ```powershell
   type "%LOCALAPPDATA%\FluentPDF_Debug.log" | findstr PDFium
   ```
   Should show: `PDFium initialized successfully`

2. **Check document loading**:
   ```powershell
   type "%LOCALAPPDATA%\FluentPDF_Debug.log" | findstr "LoadDocument"
   ```
   Look for: `LoadDocumentFromPathAsync SUCCESS`

3. **Check rendering**:
   ```powershell
   type "%LOCALAPPDATA%\FluentPDF_Debug.log" | findstr "render"
   ```
   Should show: `Page rendered successfully`

### Get detailed diagnostics:

```powershell
# Run with full logging
.\FluentPDF.App.exe -o "test.pdf" -c -v -ac -acd 5

# The console will show real-time log output
# After it closes, check:
type "%LOCALAPPDATA%\FluentPDF_Debug.log"
```

## Next Steps

- See full documentation: [docs/CLI-AUTOMATION.md](docs/CLI-AUTOMATION.md)
- Run test script: `.\scripts\test-cli.ps1 -TestPdf "your.pdf" -Verbose`
- Integrate into your CI/CD pipeline (examples in docs)

## Exit Codes

- `0` = Success (PDF opened and rendered)
- Non-zero = Error (check logs)
