# FluentPDF Test Orchestration Quick Reference

## One-Line Commands

```bash
# Default (API on port 5000, reports to tests/reports)
pwsh tools/run-autonomous-tests.ps1

# Custom port
pwsh tools/run-autonomous-tests.ps1 -Port 8080

# Verbose logging
pwsh tools/run-autonomous-tests.ps1 -Verbose

# Keep server running after tests
pwsh tools/run-autonomous-tests.ps1 -NoCleanup

# Combined options
pwsh tools/run-autonomous-tests.ps1 -Port 8080 -Verbose -OutputDir ./my-reports
```

## What Gets Tested

### CLI Tests (Phase 1)
- `--diagnostics` - System info check
- `--test-render` - PDF rendering verification

### API Tests (Phase 2)
- **Health**: `/api/health` - Server ready check
- **Documents**:
  - `POST /api/document/load` - Load PDF
  - `GET /api/document/{id}` - Get info
  - `DELETE /api/document/{id}` - Close
- **Rendering**:
  - `GET /api/render/{id}/{page}` - Render page to PNG
- **Verification**:
  - `POST /api/verify/render` - Single page verification
  - `POST /api/verify/batch` - Multiple page verification

### Skipped Tests
- Theme tests (pending implementation)
- Annotation tests (pending implementation)
- Form tests (pending implementation)

## Output Files

```
tests/reports/
├── test-report.json        # Machine-readable results
├── test-report.html        # Browser-viewable summary
├── orchestration.log       # Complete execution log
└── cli-*.json              # Individual CLI test outputs
```

## Exit Codes

- **0** = All tests passed ✓
- **1** = Some tests failed ✗
- **2** = Infrastructure error (server won't start, etc.)

## Performance Metrics

| Operation | Time |
|-----------|------|
| Server startup | 2-3 sec |
| CLI tests | 3-5 sec |
| API tests (health + document + render + verify) | 8-10 sec |
| Report generation | 0.5 sec |
| **Total** | **~15-20 sec** |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Server won't start" | Build first: `dotnet build src/FluentPDF.App -p:Platform=x64` |
| "Port 5000 in use" | Use different port: `-Port 8080` |
| "No test fixtures" | Check `tests/Fixtures/` has `.pdf` files |
| "Timeout waiting for health check" | Increase timeout: `-TimeoutSeconds 60` |
| "Verbose output needed" | Add flag: `-Verbose` |

## CI/CD Integration

### GitHub Actions
```yaml
- name: Run autonomous tests
  run: pwsh tools/run-autonomous-tests.ps1

- name: Upload test reports
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: test-reports
    path: tests/reports/
```

### Azure Pipelines
```yaml
- task: PowerShell@2
  inputs:
    targetType: 'inline'
    script: 'pwsh tools/run-autonomous-tests.ps1'
```

### Jenkins
```groovy
stage('Test') {
  steps {
    powershell 'pwsh tools/run-autonomous-tests.ps1'
  }
}
```

## Manual API Testing

While tests are running (`-NoCleanup` flag):

```bash
# Health check
curl http://localhost:5000/api/health | jq

# Load document
curl -X POST http://localhost:5000/api/document/load \
  -H "Content-Type: application/json" \
  -d '{"path":"C:\\test.pdf"}'

# Render page 0
curl http://localhost:5000/api/render/{SESSION_ID}/0 \
  --output page0.png

# Verify page
curl -X POST http://localhost:5000/api/verify/render \
  -H "Content-Type: application/json" \
  -d '{"documentId":"{SESSION_ID}","pageIndex":0}' | jq
```

## Report Formats

### JSON Report (`test-report.json`)
Machine-readable format with:
- Test results (pass/fail/skip)
- Execution times
- Error details
- Failure stack traces

Perfect for CI/CD integration and log aggregation.

### HTML Report (`test-report.html`)
Open in browser to see:
- Visual summary (pass/fail counts)
- Success percentage
- Test results table
- Failure details with JSON
- Execution duration

### Console Log (`orchestration.log`)
Plain text with timestamps:
```
[10:30:05.123] [INFO] Initializing test environment...
[10:30:06.456] [SUCCESS] Test environment initialized
[10:30:07.789] [ERROR] Some test failed: details here
```

## Parameter Reference

```
-Port <int>
    API server port (default: 5000)

-TimeoutSeconds <int>
    API server startup timeout (default: 30)

-OutputDir <string>
    Report output directory (default: tests/reports)

-Verbose
    Enable detailed logging and debug output

-NoCleanup
    Don't stop API server or delete reports after running
```

## Full Documentation

See [`docs/test-orchestration-guide.md`](../docs/test-orchestration-guide.md) for:
- Detailed API endpoint documentation
- Helper function reference
- Advanced usage examples
- Performance analysis
- Troubleshooting guide
- Future enhancements

## Common Workflows

### Pre-commit Testing
```bash
# Quick validation before committing
pwsh tools/run-autonomous-tests.ps1
if ($LASTEXITCODE -ne 0) {
  Write-Host "Tests failed, commit aborted"
  exit 1
}
git commit -m "..."
```

### Local Development
```bash
# Start server for development (no auto-cleanup)
pwsh tools/run-autonomous-tests.ps1 -NoCleanup -Port 8080

# In another terminal, run manual API calls
# When done: Get-Process FluentPDF.App | Stop-Process
```

### CI/CD Pipeline
```bash
# In CI, report failures clearly
$output = pwsh tools/run-autonomous-tests.ps1
if ($LASTEXITCODE -eq 0) {
  Write-Host "All tests passed" -ForegroundColor Green
} else {
  Write-Host "Tests failed - check test-reports/" -ForegroundColor Red
  cat tests/reports/test-report.json | jq '.failures'
  exit 1
}
```

### Parallel Testing on Different Ports
```bash
# Run suite A on port 5000
Start-Job { pwsh tools/run-autonomous-tests.ps1 -Port 5000 }

# Run suite B on port 8080
Start-Job { pwsh tools/run-autonomous-tests.ps1 -Port 8080 }

# Wait for both
Get-Job | Wait-Job

# Check results
ls tests/reports/test-report.json
```

## Key Features

✓ Automatic API server lifecycle management
✓ Health checks before running tests
✓ CLI and REST API test orchestration
✓ Multi-format reporting (JSON, HTML, logs)
✓ Performance metrics tracking
✓ CI/CD ready with exit codes
✓ Verbose logging for debugging
✓ No manual test configuration needed
✓ Tests first 3 pages of each PDF
✓ Graceful error handling and cleanup
