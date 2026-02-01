# FluentPDF API Integration Tests

This test project contains integration tests for the FluentPDF Verification API.

## Overview

These tests verify the REST API endpoints work correctly by making HTTP requests to a running instance of FluentPDF.App. The tests are marked with `[Fact(Skip = "Requires running API server")]` by default to prevent failures when the API server is not running.

## Prerequisites

1. **Build FluentPDF.App** (Windows-only):
   ```bash
   dotnet build src/FluentPDF.App -p:Platform=x64
   ```

2. **Test PDF File**: The tests use `tests/Fixtures/sample-with-text.pdf` as the test document.

## Running Tests

### Option 1: Manual Server Start

1. **Start the API server** (in one terminal):
   ```bash
   cd src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0
   FluentPDF.App.exe --api-server --headless
   ```

2. **Run the tests** (in another terminal):
   ```bash
   cd tests/FluentPDF.Api.Tests
   dotnet test --filter "FullyQualifiedName!~Skip"
   ```

### Option 2: Automated Script

Use the provided PowerShell script that automatically starts the server, runs tests, and cleans up:

```powershell
# Run from repository root
pwsh tests/FluentPDF.Api.Tests/run-api-tests.ps1
```

### Option 3: Unskip Tests

To run tests without the skip attribute:

1. Remove the `Skip` parameter from test attributes in the test files
2. Start the API server manually
3. Run `dotnet test`

## Test Structure

### ApiIntegrationTests.cs

Core API functionality tests:
- **Health Endpoint**: Server health and status checks
- **Document Endpoint**: Load, query, and close documents
- **Render Endpoint**: Page rendering to PNG
- **Verify Endpoint**: Hash-based render verification

### ErrorHandlingTests.cs

Error scenarios and edge cases:
- Invalid request handling (empty paths, missing fields)
- Malformed JSON handling
- Correlation ID propagation
- Response format consistency
- Performance benchmarks

## Configuration

### Custom API URL

Override the default API base URL (http://localhost:5000) using an environment variable:

```bash
# Windows
set TEST_API_BASE_URL=http://localhost:8080
dotnet test

# Linux/macOS
export TEST_API_BASE_URL=http://localhost:8080
dotnet test
```

### Custom Test PDF

The tests use a relative path to find the test PDF. If your repository structure differs, update the `_testPdfPath` in `ApiIntegrationTests.cs`:

```csharp
_testPdfPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "..", "..", "..", "..", "..", "tests", "Fixtures", "sample-with-text.pdf"
);
```

## Test Coverage

| Category | Tests | Description |
|----------|-------|-------------|
| Health | 3 | Server health, status, correlation IDs |
| Document Operations | 6 | Load, query, close documents |
| Rendering | 3 | Page rendering with various options |
| Verification | 4 | Hash-based verification (single + batch) |
| Error Handling | 8 | Invalid inputs, malformed requests |
| **Total** | **24** | **Complete API coverage** |

## CI/CD Integration

### GitHub Actions Example

```yaml
name: API Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build FluentPDF.App
        run: dotnet build src/FluentPDF.App -p:Platform=x64

      - name: Start API Server
        run: |
          Start-Process -FilePath "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/FluentPDF.App.exe" `
            -ArgumentList "--api-server","--headless" `
            -NoNewWindow

      - name: Wait for API Server
        run: |
          $retries = 30
          while ($retries -gt 0) {
            try {
              $response = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -TimeoutSec 1
              if ($response.StatusCode -eq 200) { break }
            } catch {}
            Start-Sleep -Seconds 1
            $retries--
          }

      - name: Run API Tests
        run: dotnet test tests/FluentPDF.Api.Tests --logger "trx;LogFileName=api-tests.xml"

      - name: Stop API Server
        if: always()
        run: Stop-Process -Name "FluentPDF.App" -Force -ErrorAction SilentlyContinue
```

## Troubleshooting

### Tests Timeout

- Ensure the API server is running and accessible at the configured URL
- Check Windows Firewall isn't blocking localhost connections
- Increase the `_client.Timeout` in test constructors if needed

### Test PDF Not Found

- Verify `tests/Fixtures/sample-with-text.pdf` exists
- Check the relative path calculation in `_testPdfPath`
- Use an absolute path for debugging

### Port Already in Use

- Change the API server port: `FluentPDF.App.exe --api-server --port 8080`
- Update `TEST_API_BASE_URL` environment variable accordingly

### Tests Are Skipped

- By design! Remove the `Skip` parameter from `[Fact]` attributes to enable tests
- Or use the provided PowerShell script which handles server lifecycle

## Development Workflow

When developing new API features:

1. Add endpoint to `src/FluentPDF.App/Api/Endpoints/`
2. Add corresponding test to `ApiIntegrationTests.cs`
3. Start API server: `FluentPDF.App.exe --api-server --headless`
4. Run tests: `dotnet test --filter "FullyQualifiedName~NewFeature"`
5. Iterate until tests pass

## Future Improvements

- [ ] Add WebApplicationFactory for in-process testing (requires refactoring to avoid WinUI dependency)
- [ ] Add performance benchmarks for all endpoints
- [ ] Add stress tests (concurrent requests)
- [ ] Add authentication tests (if auth is added to API)
- [ ] Add HTTPS/TLS tests
