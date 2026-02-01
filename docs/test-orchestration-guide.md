# FluentPDF Autonomous Test Orchestration Guide

## Overview

The `run-autonomous-tests.ps1` PowerShell script provides comprehensive autonomous test orchestration for FluentPDF, combining CLI verification commands, REST API tests, and visual/theme validation into a single unified test suite.

## Features

- **API Server Management**: Automatic startup and shutdown of the FluentPDF REST API server
- **CLI Test Execution**: Runs diagnostic and verification CLI commands
- **REST API Testing**: Comprehensive endpoint testing (health, document load/render/verify, batch operations)
- **Health Checks**: Validates API server startup and PDFium initialization
- **Multi-Format Reporting**: JSON, HTML, and console log output
- **CI/CD Integration**: Exit codes for automation (0=success, 1=failures, 2=infrastructure error)
- **Verbose Logging**: Detailed execution logs for debugging
- **Performance Metrics**: Execution times for all test operations

## Quick Start

### Basic Usage

```bash
# Run all tests with defaults (API on port 5000, output to tests/reports)
pwsh tools/run-autonomous-tests.ps1

# Run tests on custom port
pwsh tools/run-autonomous-tests.ps1 -Port 8080

# Run with verbose logging
pwsh tools/run-autonomous-tests.ps1 -Verbose

# Keep server running after tests (no cleanup)
pwsh tools/run-autonomous-tests.ps1 -NoCleanup
```

### CI/CD Integration

```bash
# GitHub Actions example
- name: Run autonomous tests
  run: pwsh tools/run-autonomous-tests.ps1

# Jenkins example
stage('Test') {
  steps {
    powershell 'pwsh tools/run-autonomous-tests.ps1'
  }
}

# Check exit code
pwsh tools/run-autonomous-tests.ps1
if ($LASTEXITCODE -eq 0) {
  Write-Host "All tests passed"
} else {
  Write-Host "Tests failed with code: $LASTEXITCODE"
  exit $LASTEXITCODE
}
```

## Test Phases

### Phase 1: CLI Verification Tests

Tests command-line interface commands:

| Command | Purpose | Exit Code |
|---------|---------|-----------|
| `--diagnostics` | System diagnostics (OS, .NET, PDFium) | 0 = success |
| `--test-render` | PDF rendering verification | 0 = success, 2 = render failed |

**Future CLI Tests** (when implemented):
- `--test-merge`: Document merging
- `--test-split`: Document splitting
- `--test-optimize`: PDF optimization
- `--test-watermark`: Watermark operations
- `--test-annotations`: Annotation handling
- `--test-forms`: Form field operations
- `--test-conversion`: Document format conversion

### Phase 2: REST API Verification Tests

Tests all REST API endpoints:

#### Health Check (`GET /api/health`)
- Validates API server responsiveness
- Checks PDFium initialization status
- Returns version information
- Exit early if health check fails

#### Document Management
- **Load** (`POST /api/document/load`): Load PDF and create session
- **Info** (`GET /api/document/{id}`): Retrieve document metadata
- **Close** (`DELETE /api/document/{id}`): Release session resources

#### Rendering
- **Render** (`GET /api/render/{documentId}/{pageIndex}`): Render page to PNG
- Tests first 3 pages of sample PDF
- Measures render performance
- Validates PNG output (min. 100 bytes)

#### Verification
- **Verify Single** (`POST /api/verify/render`): Render and hash verification
- **Batch Verify** (`POST /api/verify/batch`): Verify multiple pages in parallel
- Validates hash computation

### Phase 3: Theme & Visual Tests

Tests visual appearance consistency (skipped until theme API implemented).

### Phase 4: Annotation Tests

Tests annotation functionality (skipped until annotation API implemented).

### Phase 5: Form Field Tests

Tests form filling and submission (skipped until form API implemented).

## Output Reports

### JSON Report

Located: `tests/reports/test-report.json`

Structure:
```json
{
  "timestamp": "2026-01-25T10:30:00Z",
  "duration": {
    "totalSeconds": 45,
    "totalMinutes": 0.75
  },
  "summary": {
    "totalTests": 12,
    "totalPassed": 12,
    "totalFailed": 0,
    "successRate": 100.0
  },
  "skipped": ["Theme tests", "Annotation tests"],
  "cliTests": {
    "diagnostics": {
      "name": "diagnostics",
      "category": "CLI",
      "passed": true,
      "message": "Command executed successfully",
      "timestamp": "2026-01-25T10:30:05Z",
      "details": {
        "command": "FluentPDF.App.exe --diagnostics",
        "exitCode": 0,
        "elapsedMs": 1250
      }
    }
  },
  "apiTests": {
    "health-check": {
      "name": "health-check",
      "category": "API",
      "passed": true,
      "message": "Status: 200, Time: 45ms",
      "timestamp": "2026-01-25T10:30:10Z",
      "details": {
        "method": "GET",
        "endpoint": "/health",
        "statusCode": 200,
        "expectedStatus": 200,
        "elapsedMs": 45,
        "contentLength": 256
      }
    }
  },
  "failures": []
}
```

### HTML Report

Located: `tests/reports/test-report.html`

Visual summary with:
- Pass/fail/skip counts
- Success rate percentage
- Tabular test results with HTTP status codes
- Failure details with error messages
- Skipped tests list
- Execution duration

### Console Output

Real-time colored output:
```
[10:30:05] [INFO] FluentPDF Autonomous Test Orchestration
[10:30:05] [INFO] ========================================
[10:30:06] [INFO] Initializing test environment...
[10:30:06] [SUCCESS] Test environment initialized successfully
[10:30:07] [INFO] Starting API server on port 5000...
[10:30:08] [SUCCESS] API server is ready
[10:30:08] [INFO] ============================================
[10:30:08] [INFO] PHASE 1: CLI VERIFICATION TESTS
...
```

### Log File

Located: `tests/reports/orchestration.log`

Complete execution log with timestamps and log levels.

## Helper Functions

### Test Invocation Functions

#### `Invoke-ApiTest`
Executes a single REST API test with assertion validation.

```powershell
Invoke-ApiTest -TestName "document-load" `
    -Method POST `
    -Endpoint "/document/load" `
    -Body @{ path = "C:\test.pdf" } `
    -ExpectedStatus 200 `
    -Assertion { param($response)
        if (-not $response.documentId) { throw "No documentId" }
    }
```

**Parameters:**
- `-TestName`: Display name for test
- `-Method`: HTTP method (GET, POST, DELETE, etc.)
- `-Endpoint`: API endpoint path (without base URL)
- `-Body`: Request body (hashtable, converted to JSON)
- `-ExpectedStatus`: Expected HTTP status code (default: 200)
- `-Assertion`: ScriptBlock for custom validation

**Returns:**
- `@{ passed = bool; content = object; response = object; stopwatch = Stopwatch }`

#### `Invoke-CliCommand`
Executes a CLI command and validates exit code.

```powershell
Invoke-CliCommand -CommandName "diagnostics" `
    -Arguments "--diagnostics" `
    -Description "System diagnostics"
```

**Parameters:**
- `-CommandName`: Test identifier
- `-Arguments`: Array of CLI arguments
- `-Description`: Test description

### Server Management Functions

#### `Start-ApiServer`
Starts the FluentPDF API server and waits for health check.

**Returns:** `$true` if server started and healthy, `$false` otherwise

#### `Stop-ApiServer`
Gracefully stops the API server process.

#### `Wait-ForServerReady`
Polls health endpoint until server is ready or timeout.

**Parameters:**
- `-MaxWaitSeconds`: Timeout in seconds (default: 30)

**Returns:** `$true` if ready, `$false` on timeout

### Utility Functions

#### `Write-Log`
Structured logging with timestamps and color-coded levels.

```powershell
Write-Log "Server started successfully" "SUCCESS"
Write-Log "Database error occurred" "ERROR"
```

**Log Levels:** INFO, SUCCESS, ERROR, WARN, DEBUG

#### `Write-TestResult`
Records a test result with metadata.

```powershell
Write-TestResult -TestName "api-health" `
    -Category "API" `
    -Passed $true `
    -Message "Health check passed" `
    -Details @{ statusCode = 200; elapsedMs = 45 }
```

### Reporting Functions

#### `Generate-JsonReport`
Creates structured JSON report with all test results.

#### `Generate-HtmlReport`
Creates visual HTML report for browser viewing.

#### `Print-ConsoleSummary`
Prints formatted summary to console.

## Exit Codes

| Code | Meaning | Action |
|------|---------|--------|
| 0 | All tests passed | Deploy/continue |
| 1 | One or more tests failed | Block deployment, investigate failures |
| 2 | Infrastructure error | Check API server startup, fixtures, build |

## Performance Expectations

Typical execution times (depends on PDF complexity):

| Phase | Typical Duration |
|-------|-----------------|
| Environment init | 0.5 sec |
| API server startup | 2-3 sec |
| CLI tests | 3-5 sec |
| API health check | 0.5 sec |
| Document load | 1-2 sec |
| Page renders (3 pages) | 3-5 sec |
| Verify operations | 2-3 sec |
| Report generation | 0.5 sec |
| **Total** | **15-25 sec** |

## Troubleshooting

### API Server Won't Start

**Error:** "Server failed to start within 30 seconds"

**Solutions:**
1. Verify FluentPDF.App.exe exists: `test-path "src/FluentPDF.App/bin/x64/Debug/.../FluentPDF.App.exe"`
2. Build the project: `dotnet build src/FluentPDF.App -p:Platform=x64`
3. Check port isn't in use: `netstat -ano | findstr :5000`
4. Increase timeout: `pwsh tools/run-autonomous-tests.ps1 -TimeoutSeconds 60`

### Document Load Fails

**Error:** "documentId validation failed"

**Solutions:**
1. Verify test fixtures exist: `ls tests/Fixtures/*.pdf`
2. Check file permissions: `Get-Item tests/Fixtures/sample-with-text.pdf`
3. Validate PDF file integrity using `--test-render`: `FluentPDF.App.exe --test-render tests/Fixtures/sample-with-text.pdf`

### Render Tests Timeout

**Error:** "Response timeout from /api/render/{id}/0"

**Solutions:**
1. Increase timeout: `pwsh tools/run-autonomous-tests.ps1 -TimeoutSeconds 60`
2. Check system resources: `Get-Process | Where-Object {$_.ProcessName -eq 'FluentPDF.App'} | Select-Object WorkingSet`
3. Run smaller tests: Custom script with single PDF page

### Port Already in Use

**Error:** "Server failed to start (Connection refused or timeout)"

**Solutions:**
1. Check what's using the port: `netstat -ano | findstr :5000`
2. Kill existing process: `Stop-Process -Id <PID> -Force`
3. Use different port: `pwsh tools/run-autonomous-tests.ps1 -Port 8080`

## Advanced Usage

### Custom Test with Verbose Logging

```bash
pwsh -Command {
    $VerbosePreference = 'Continue'
    .\tools\run-autonomous-tests.ps1 -Verbose
}
```

### Keep Server Running for Manual Testing

```bash
pwsh tools/run-autonomous-tests.ps1 -NoCleanup -Port 8080

# In another terminal:
curl http://localhost:8080/api/health

# Then stop manually:
Get-Process FluentPDF.App | Stop-Process
```

### Generate Report for Specific PDF

Edit script to test specific PDF:
```powershell
$testPdf = "C:\custom-test.pdf"
```

Then run normally.

### Parallel Execution

Run multiple test suites on different ports:
```bash
# Terminal 1
pwsh tools/run-autonomous-tests.ps1 -Port 5000

# Terminal 2
pwsh tools/run-autonomous-tests.ps1 -Port 8080
```

## Integration Examples

### GitHub Actions Workflow

```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0'

      - name: Build
        run: dotnet build src/FluentPDF.App -p:Platform=x64

      - name: Run tests
        run: pwsh tools/run-autonomous-tests.ps1

      - name: Upload reports
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-reports
          path: tests/reports/
```

### LocalStack/Docker Testing

```bash
# Run in Docker container with .NET
docker run -it --rm `
  -v ${PWD}:/workspace `
  -w /workspace `
  mcr.microsoft.com/windows/servercore:ltsc2022 `
  pwsh tools/run-autonomous-tests.ps1
```

## Future Enhancements

Planned features for future versions:

1. **Performance Profiling**: Memory and CPU tracking per test
2. **Screenshot Capture**: Render page screenshots on failure
3. **Baseline Comparison**: Visual regression detection using SSIM
4. **Performance Trends**: Historical test execution tracking
5. **Parallel Execution**: Run independent tests concurrently
6. **Custom Test Scripts**: Support for user-provided test suites
7. **Slack/Email Notifications**: Result reporting to team
8. **Test Coverage Metrics**: Code coverage for implementation
9. **Flaky Test Detection**: Identify intermittent failures
10. **Performance Budget Enforcement**: Fail if slower than threshold

## See Also

- [Verification API Documentation](./verification-api.md)
- [CLI Commands Reference](../CLAUDE.md#cli-diagnostic-commands)
- [Test Fixtures](../tests/Fixtures/)
- [Autonomous Verification API Spec](./.spec-workflow/specs/autonomous-verification-api/)
