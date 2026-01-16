# FluentPDF CLI Automation Guide

## Overview

FluentPDF now supports command-line arguments for automated testing and CI/CD workflows. You can launch the app, open a PDF, and automatically close it while capturing detailed logs.

## Command-Line Options

```
FluentPDF.App.exe [options] [file.pdf]

Options:
  --open-file, -o <path>        Open the specified PDF file on startup
  --auto-close, -ac             Automatically close app after opening file
  --auto-close-delay, -acd <s>  Delay in seconds before auto-close (default: 2)
  --console, -c                 Enable console logging output
  --verbose, -v                 Enable verbose/debug logging
  --log-output, -l <path>       Custom log output path
  --help, -h                    Show help message
```

## Usage Examples

### Basic File Opening

```powershell
# Open a PDF file
.\FluentPDF.App.exe --open-file "C:\Documents\test.pdf"

# Or simply
.\FluentPDF.App.exe "C:\Documents\test.pdf"
```

### Automated Testing Workflow

```powershell
# Open a PDF, wait 3 seconds, then close automatically with console logging
.\FluentPDF.App.exe -o "test.pdf" -ac -acd 3 -c -v
```

### Custom Log Output

```powershell
# Open PDF with custom log location and console output
.\FluentPDF.App.exe -o "test.pdf" -l "C:\logs\fluentpdf.log" -c
```

### Autonomous Iteration Testing

```powershell
# Test multiple PDFs in sequence
$testFiles = @("test1.pdf", "test2.pdf", "test3.pdf")

foreach ($file in $testFiles) {
    Write-Host "Testing: $file"
    .\FluentPDF.App.exe -o $file -ac -acd 2 -c -v

    # Check exit code
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ $file: Success" -ForegroundColor Green
    } else {
        Write-Host "✗ $file: Failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
}
```

## Log Locations

FluentPDF creates logs in multiple locations:

1. **Debug Log**: `%LOCALAPPDATA%\FluentPDF_Debug.log`
   - Simple timestamped text log
   - Contains startup and file opening details
   - Always enabled

2. **Structured Log**: `%TEMP%\FluentPDF\logs\log-<date>.json`
   - JSON structured logging (Serilog)
   - Contains detailed rendering and performance metrics
   - Controlled by `--verbose` flag

3. **Console Output**: stdout (when `--console` is used)
   - Real-time output for CI/CD
   - Formatted for readability

4. **Custom Log**: Specified by `--log-output` option

## Checking Logs After Execution

### PowerShell Script

```powershell
# Run the app with auto-close and verbose logging
.\FluentPDF.App.exe -o "test.pdf" -ac -acd 2 -c -v

# Check debug log
$debugLog = "$env:LOCALAPPDATA\FluentPDF_Debug.log"
if (Test-Path $debugLog) {
    Write-Host "`n=== Debug Log (last 20 lines) ===" -ForegroundColor Cyan
    Get-Content $debugLog -Tail 20
}

# Check structured log (latest file)
$structuredLog = Get-ChildItem "$env:TEMP\FluentPDF\logs\log-*.json" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($structuredLog) {
    Write-Host "`n=== Structured Log (last 10 entries) ===" -ForegroundColor Cyan
    Get-Content $structuredLog.FullName -Tail 10 |
        ForEach-Object {
            $json = $_ | ConvertFrom-Json
            Write-Host "[$($json.'@t')] [$($json.'@l')] $($json.'@m')"
        }
}
```

## Exit Codes

- `0`: Success - file opened and rendered successfully
- Non-zero: Error occurred (check logs for details)

## Troubleshooting

### PDF Not Rendering

Check the logs for:

1. **PDFium Initialization**
   ```
   Debug log: "PDFium initialization FAILED"
   ```
   - Ensure `pdfium.dll` is present in the app directory

2. **File Loading Errors**
   ```
   Structured log: "FAILED TO LOAD DOCUMENT"
   ```
   - Check file path is correct
   - Verify PDF is not corrupted
   - Check file permissions

3. **Rendering Failures**
   ```
   Structured log: "Failed to render page"
   ```
   - Check available memory
   - Verify DPI settings
   - Look for detailed error messages in logs

### Viewing Real-Time Logs

Use `--console` flag to see logs in real-time:

```powershell
.\FluentPDF.App.exe -o "test.pdf" -c -v
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Test PDF Rendering
  shell: powershell
  run: |
    .\FluentPDF.App.exe -o "test\sample.pdf" -ac -acd 3 -c -v

    # Upload logs as artifact
    if (Test-Path "$env:LOCALAPPDATA\FluentPDF_Debug.log") {
        Copy-Item "$env:LOCALAPPDATA\FluentPDF_Debug.log" "artifacts\"
    }

- name: Upload Logs
  uses: actions/upload-artifact@v3
  with:
    name: fluentpdf-logs
    path: artifacts/
```

### Azure DevOps Example

```yaml
- task: PowerShell@2
  displayName: 'Test PDF Rendering'
  inputs:
    targetType: 'inline'
    script: |
      .\FluentPDF.App.exe -o "test\sample.pdf" -ac -acd 3 -c -v

      if ($LASTEXITCODE -ne 0) {
        Write-Host "##vso[task.logissue type=error]PDF rendering failed"
        exit 1
      }
```

## Advanced Usage

### Automated Visual Regression Testing

```powershell
# Test PDF rendering and capture logs for regression analysis
$testPdf = "test.pdf"
$logFile = "regression-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

.\FluentPDF.App.exe -o $testPdf -ac -acd 5 -c -v -l $logFile

# Parse logs for performance metrics
$logs = Get-Content $logFile | ConvertFrom-Json
$renderTimes = $logs | Where-Object { $_.'@m' -like '*Page rendered successfully*' } |
    Select-Object -ExpandProperty RenderTimeMs

Write-Host "Average render time: $(($renderTimes | Measure-Object -Average).Average) ms"
```

## Next Steps

1. Run the test script: `.\test-cli.ps1`
2. Check logs in `%LOCALAPPDATA%\FluentPDF_Debug.log`
3. Integrate into your automated testing pipeline
4. Report issues with log files attached
