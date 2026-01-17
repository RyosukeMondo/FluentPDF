# PDFium Library Verification Tool

## Overview

The PDFium Library Verification Tool is a standalone CLI application that validates P/Invoke declarations against the actual PDFium DLL exports. It detects function signature mismatches, validates return types, and verifies functional behavior to prevent silent failures that can occur from incorrect marshalling between .NET and native code.

## Why This Tool Exists

PDFium is a critical external dependency for PDF rendering. Incorrect P/Invoke declarations can cause:

- **Silent Failures**: Functions return garbage values (e.g., `5.64e-315` instead of page width) without throwing exceptions
- **Memory Leaks**: Incorrect calling conventions or marshalling can prevent proper cleanup
- **Platform-Specific Bugs**: Marshalling errors that only occur on certain architectures or .NET versions
- **Upgrade Risks**: Library version changes may break existing P/Invoke declarations

This tool catches these issues at build time instead of runtime, significantly reducing debugging time and preventing production issues.

## Installation and Setup

### Prerequisites

- .NET 8.0 SDK or later
- PDFium DLL (`pdfium.dll` for Windows, `libpdfium.so` for Linux, `libpdfium.dylib` for macOS)
- Test PDF files (optional, for behavior verification)

### Build the Tool

```bash
# Clone the repository
git clone https://github.com/your-org/FluentPDF.git
cd FluentPDF

# Build the verification CLI
dotnet build src/FluentPDF.Verification.Cli -c Release

# The executable will be at:
# src/FluentPDF.Verification.Cli/bin/Release/net8.0/FluentPDF.Verification.Cli.exe (Windows)
# src/FluentPDF.Verification.Cli/bin/Release/net8.0/FluentPDF.Verification.Cli (Linux/macOS)
```

### Quick Start

```bash
# Verify PDFium DLL with default options
dotnet run --project src/FluentPDF.Verification.Cli -- --dll libs/x64/bin/pdfium.dll

# Or run the built executable directly
FluentPDF.Verification.Cli.exe --dll libs/x64/bin/pdfium.dll
```

## CLI Usage

### Command Syntax

```
pdfium-verify [options]

Options:
  --dll, -d <path>           Path to pdfium.dll (required)
  --test-files, -t <path>    Directory containing test PDF files
  --format, -f <format>      Output format: Console, Json, Html (default: Console)
  --output, -o <path>        Output file path (default: stdout)
  --parallel, -p             Run tests in parallel (default: true)
  --verbose, -v              Enable verbose logging
  --help, -h                 Show help message
  --version                  Show version information
```

### Options Reference

#### `--dll, -d <path>` (Required)

Path to the PDFium DLL file to verify.

**Examples:**
```bash
# Windows
--dll libs/x64/bin/pdfium.dll

# Linux
--dll libs/x64/bin/libpdfium.so

# macOS
--dll libs/x64/bin/libpdfium.dylib

# Absolute path
--dll C:\Libraries\pdfium\pdfium.dll
```

#### `--test-files, -t <path>` (Optional)

Directory containing test PDF files for behavior verification. If not specified, only signature verification is performed.

**Examples:**
```bash
--test-files tests/Fixtures
--test-files C:\TestData\PDFs
```

**Test File Requirements:**
- Valid PDF files in various formats (PDF 1.4, 1.5, 1.6, 1.7, 2.0)
- Files with known properties (page count, dimensions) for validation
- Recommended: Include corrupted PDFs to test error handling

#### `--format, -f <format>` (Optional)

Output report format. Default: `Console`

**Supported Formats:**
- `Console`: Human-readable colored output (default)
- `Json`: Machine-readable JSON for CI/CD integration
- `Html`: HTML report with styling

**Examples:**
```bash
# Console output (default)
--format Console

# JSON for CI/CD
--format Json --output verification-report.json

# HTML report
--format Html --output verification-report.html
```

#### `--output, -o <path>` (Optional)

Output file path for the report. If not specified, outputs to stdout (console).

**Examples:**
```bash
# Write JSON to file
--format Json --output results.json

# Write HTML to file
--format Html --output C:\Reports\pdfium-verification.html

# Stdout (default)
# (no --output specified)
```

#### `--parallel, -p` (Optional)

Run verification tests in parallel. Default: `true`

**Examples:**
```bash
# Enable parallel execution (default)
--parallel true

# Disable parallel execution (sequential)
--parallel false
```

**Note:** Sequential execution is useful for debugging or when running in resource-constrained environments.

#### `--verbose, -v` (Optional)

Enable verbose logging output. Default: `false`

**Examples:**
```bash
# Enable verbose logging
--verbose

# Standard logging (default)
# (no --verbose flag)
```

**Verbose Output Includes:**
- Detailed DLL analysis steps
- Function signature comparisons
- Return type validation details
- Test execution timing
- Memory usage statistics

## Usage Examples

### Basic Verification

Verify PDFium DLL with default settings (console output, parallel execution):

```bash
pdfium-verify --dll libs/x64/bin/pdfium.dll
```

**Expected Output:**
```
PDFium Verification Results
===========================
Total Tests: 45
Passed: 45
Failed: 0
Duration: 2.34s
Library Version: PDFium 5.0

All tests passed!
```

### Full Verification with Test Files

Run complete verification including behavior tests with test PDFs:

```bash
pdfium-verify --dll libs/x64/bin/pdfium.dll --test-files tests/Fixtures --verbose
```

**Expected Output:**
```
[12:34:56 INF] Starting PDFium verification
[12:34:56 INF] DLL Path: libs/x64/bin/pdfium.dll
[12:34:56 DBG] Loading DLL for analysis
[12:34:56 DBG] Discovered 152 exported functions
[12:34:56 INF] Verifying 45 P/Invoke signatures
[12:34:57 DBG] Signature verification: FPDF_InitLibrary - PASSED
[12:34:57 DBG] Signature verification: FPDF_DestroyLibrary - PASSED
...
[12:34:58 INF] Running behavior tests with 8 test files
[12:34:59 DBG] Testing document load: sample.pdf - PASSED
[12:34:59 DBG] Testing page dimensions: sample.pdf - PASSED
...

PDFium Verification Results
===========================
Total Tests: 67
Passed: 67
Failed: 0
Duration: 3.12s
Library Version: PDFium 5.0

All tests passed!
```

### CI/CD Integration (JSON Output)

Generate machine-readable JSON report for CI/CD pipelines:

```bash
pdfium-verify --dll libs/x64/bin/pdfium.dll --format Json --output pdfium-verification.json
```

**JSON Output Structure:**
```json
{
  "totalTests": 45,
  "passedTests": 45,
  "failedTests": 0,
  "duration": "00:00:02.3456789",
  "libraryVersion": "PDFium 5.0",
  "results": [
    {
      "testName": "SignatureVerification_FPDF_InitLibrary",
      "success": true,
      "errorMessage": null,
      "metadata": {
        "functionName": "FPDF_InitLibrary",
        "declaredSignature": "void FPDF_InitLibrary()",
        "actualSignature": "void FPDF_InitLibrary()"
      },
      "duration": "00:00:00.0123456"
    }
  ]
}
```

### Sequential Execution for Debugging

Run tests sequentially with verbose logging for debugging:

```bash
pdfium-verify --dll libs/x64/bin/pdfium.dll --parallel false --verbose
```

### Generate HTML Report

Create a styled HTML report for documentation or review:

```bash
pdfium-verify --dll libs/x64/bin/pdfium.dll --format Html --output verification-report.html
```

Open `verification-report.html` in a browser to view the formatted results.

## Exit Codes

The tool returns standard exit codes for automation and CI/CD integration:

| Exit Code | Meaning | Description |
|-----------|---------|-------------|
| `0` | Success | All verification tests passed |
| `1` | Verification Failure | One or more verification tests failed |
| `2` | Invalid Arguments | Command-line arguments are invalid or missing |
| `3` | Unexpected Error | An unexpected error occurred during execution |

**CI/CD Usage:**
```bash
# Run verification and fail the build on errors
pdfium-verify --dll pdfium.dll --format Json --output results.json
if [ $? -ne 0 ]; then
  echo "PDFium verification failed!"
  exit 1
fi
```

## Verification Rules

The tool performs three categories of verification:

### 1. Function Signature Verification

Validates that all P/Invoke declarations match the actual DLL exports.

**Checks:**
- Function name exists in DLL
- Parameter count matches
- Parameter types match (including marshalling attributes)
- Return type matches
- Calling convention matches (Cdecl, StdCall, etc.)

**Example Failure:**
```
Failed Tests:
  - SignatureVerification_FPDF_GetPageWidthF
    Error: Function not found in DLL exports
    Suggested Fix: Use FPDF_GetPageWidth instead of FPDF_GetPageWidthF
```

**Example Mismatch:**
```
Failed Tests:
  - SignatureVerification_FPDF_LoadDocument
    Error: Parameter type mismatch at position 1
    Expected: IntPtr (declared)
    Actual: char* (DLL export)
    Suggested Fix: Add [MarshalAs(UnmanagedType.LPStr)] to parameter
```

### 2. Return Type Validation

Validates that return values are within expected ranges to detect garbage values from marshalling errors.

**Checks:**
- Numeric returns are within valid ranges (e.g., page dimensions: 1-10000 points)
- Pointer returns are not null for success cases
- Error codes match expected values
- Handle values are valid

**Example Failure:**
```
Failed Tests:
  - ReturnTypeValidation_FPDF_GetPageWidth
    Error: Return value out of expected range
    Returned: 5.64e-315 (garbage value)
    Expected Range: 1.0 - 10000.0 points
    Suggested Fix: Check P/Invoke signature for FPDF_GetPageWidth
```

**Validation Rules:**
- Page width/height: 1.0 - 10000.0 points (0.01" - 138.89")
- Page count: 1 - 10000 pages
- Document handles: Non-zero IntPtr
- Error codes: Within defined enum range

### 3. Behavior Verification

Tests PDFium functions with known inputs and expected outputs.

**Checks:**
- Document loading succeeds for valid PDFs
- Document loading fails for corrupted PDFs
- Page count matches known test files
- Page dimensions match expected values
- Rendering produces valid bitmaps
- Memory cleanup occurs properly

**Example Failure:**
```
Failed Tests:
  - BehaviorVerification_LoadDocument_CorruptedPdf
    Error: Expected load to fail, but succeeded
    Test File: corrupted.pdf
    Suggested Fix: Verify error handling in FPDF_LoadDocument wrapper
```

## Troubleshooting

### DLL Not Found

**Error:**
```
Error: DLL file not found: libs/x64/bin/pdfium.dll
```

**Solutions:**
1. Verify the DLL path is correct and file exists
2. Use absolute path instead of relative path
3. Check file permissions (must be readable)
4. Ensure the correct platform DLL (x64/x86, Windows/Linux/macOS)

**Example:**
```bash
# Verify file exists
ls -l libs/x64/bin/pdfium.dll

# Use absolute path
pdfium-verify --dll C:\Projects\FluentPDF\libs\x64\bin\pdfium.dll
```

### Test Files Directory Not Found

**Error:**
```
Error: Test files directory not found: tests/Fixtures
```

**Solutions:**
1. Verify directory exists and contains PDF files
2. Use absolute path
3. Ensure directory is readable

**Example:**
```bash
# Check directory contents
ls -l tests/Fixtures

# Use absolute path
pdfium-verify --dll pdfium.dll --test-files C:\Projects\FluentPDF\tests\Fixtures
```

### Signature Verification Failures

**Error:**
```
Failed Tests:
  - SignatureVerification_FPDF_GetPageWidthF
    Error: Function not found in DLL exports
```

**Solutions:**
1. Check PDFium documentation for correct function name
2. Verify PDFium version compatibility
3. Update P/Invoke declarations in `PdfiumInterop.cs`

**Common Issues:**
- Using deprecated functions (e.g., `FPDF_GetPageWidthF` → `FPDF_GetPageWidth`)
- Incorrect parameter types or marshalling attributes
- Missing calling convention attributes

### Return Type Validation Failures

**Error:**
```
Failed Tests:
  - ReturnTypeValidation_FPDF_GetPageWidth
    Error: Return value out of expected range
    Returned: 5.64e-315
```

**Solutions:**
1. This indicates a serious P/Invoke marshalling error
2. Check the function signature in `PdfiumInterop.cs`
3. Verify return type matches PDFium documentation
4. Check marshalling attributes (e.g., `[return: MarshalAs(...)]`)

**Root Cause:**
Garbage values typically indicate:
- Incorrect return type (e.g., `float` vs `double`)
- Missing marshalling attributes
- Calling convention mismatch
- Stack corruption from parameter mismatch

### Behavior Verification Failures

**Error:**
```
Failed Tests:
  - BehaviorVerification_LoadDocument_ValidPdf
    Error: Document load failed for valid PDF
```

**Solutions:**
1. Verify test PDF is not corrupted
2. Check PDFium initialization succeeded
3. Ensure PDFium DLL is correct version
4. Review verbose logs for detailed error messages

**Debugging Steps:**
```bash
# Run with verbose logging
pdfium-verify --dll pdfium.dll --test-files tests/Fixtures --verbose

# Check specific test file
pdfium-verify --dll pdfium.dll --test-files tests/Fixtures --parallel false --verbose
```

### Performance Issues

**Symptom:** Verification takes longer than 10 seconds

**Solutions:**
1. Reduce number of test PDF files
2. Enable parallel execution (should be enabled by default)
3. Verify system has sufficient resources (CPU, memory)
4. Check for antivirus interference with DLL loading

**Performance Tuning:**
```bash
# Ensure parallel execution is enabled
pdfium-verify --dll pdfium.dll --parallel true

# Measure execution time
time pdfium-verify --dll pdfium.dll --verbose
```

**Expected Performance:**
- Signature verification: < 1 second (45 functions)
- Return type validation: < 2 seconds (20 tests)
- Behavior verification: < 5 seconds (20 tests with 8 PDFs)
- **Total: < 10 seconds**

### Memory Issues

**Symptom:** Out of memory errors or excessive memory usage

**Solutions:**
1. Ensure PDFium resources are properly disposed
2. Run with sequential execution to reduce memory pressure
3. Reduce number of concurrent tests

**Memory Management:**
```bash
# Sequential execution (lower memory usage)
pdfium-verify --dll pdfium.dll --parallel false

# Monitor memory usage (Linux)
/usr/bin/time -v pdfium-verify --dll pdfium.dll
```

**Expected Memory Usage:**
- Peak memory: < 100 MB
- Baseline: ~35 MB

### Verbose Logging Not Working

**Symptom:** `--verbose` flag doesn't produce detailed output

**Solution:**
Ensure `--verbose` appears before other options in some shells:

```bash
# Correct
pdfium-verify --verbose --dll pdfium.dll

# Also correct
pdfium-verify --dll pdfium.dll --verbose
```

### CI/CD Integration Issues

**Symptom:** Tool works locally but fails in CI/CD

**Common Causes:**
1. DLL not included in CI build artifacts
2. Incorrect path separators (Windows vs Linux)
3. Missing test files in CI environment
4. Insufficient permissions

**Solutions:**

**GitHub Actions Example:**
```yaml
- name: Copy PDFium DLL
  run: |
    mkdir -p ${{ github.workspace }}/libs/x64/bin
    cp vendor/pdfium/pdfium.dll ${{ github.workspace }}/libs/x64/bin/

- name: Run PDFium Verification
  run: |
    dotnet run --project src/FluentPDF.Verification.Cli -- \
      --dll ${{ github.workspace }}/libs/x64/bin/pdfium.dll \
      --format Json \
      --output pdfium-verification.json

- name: Upload Verification Report
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: pdfium-verification
    path: pdfium-verification.json
```

**Azure DevOps Example:**
```yaml
- task: PowerShell@2
  displayName: 'Run PDFium Verification'
  inputs:
    targetType: 'inline'
    script: |
      dotnet run --project src/FluentPDF.Verification.Cli -- `
        --dll $(Build.SourcesDirectory)\libs\x64\bin\pdfium.dll `
        --format Json `
        --output $(Build.ArtifactStagingDirectory)\pdfium-verification.json

      if ($LASTEXITCODE -ne 0) {
        Write-Host "##vso[task.logissue type=error]PDFium verification failed"
        exit 1
      }
```

## CI/CD Integration

### Build Pipeline Integration

Add PDFium verification as a pre-build step to catch issues before deployment:

```yaml
# .github/workflows/build.yml
name: Build and Verify

on: [push, pull_request]

jobs:
  verify-pdfium:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Run PDFium Verification
        run: |
          dotnet run --project src/FluentPDF.Verification.Cli -- \
            --dll libs/x64/bin/pdfium.dll \
            --test-files tests/Fixtures \
            --format Json \
            --output pdfium-verification.json

      - name: Upload Verification Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: pdfium-verification
          path: pdfium-verification.json

      - name: Fail on Verification Errors
        if: failure()
        run: |
          echo "PDFium verification failed! See artifact for details."
          exit 1

  build:
    needs: verify-pdfium
    runs-on: windows-latest
    steps:
      # ... rest of build steps
```

### Pre-Commit Hook

Add as a pre-commit hook to catch issues before committing:

```bash
# .git/hooks/pre-commit
#!/bin/bash

echo "Running PDFium verification..."
dotnet run --project src/FluentPDF.Verification.Cli -- \
  --dll libs/x64/bin/pdfium.dll \
  --format Console

if [ $? -ne 0 ]; then
  echo "PDFium verification failed! Commit aborted."
  exit 1
fi

echo "PDFium verification passed!"
```

Make executable:
```bash
chmod +x .git/hooks/pre-commit
```

### Regular Verification Schedule

Run verification on a schedule to detect library drift:

```yaml
# .github/workflows/verify-pdfium-schedule.yml
name: Scheduled PDFium Verification

on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday at midnight

jobs:
  verify:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Run Full Verification
        run: |
          dotnet run --project src/FluentPDF.Verification.Cli -- \
            --dll libs/x64/bin/pdfium.dll \
            --test-files tests/Fixtures \
            --format Html \
            --output pdfium-verification.html \
            --verbose

      - name: Upload HTML Report
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: pdfium-verification-report
          path: pdfium-verification.html

      - name: Notify on Failure
        if: failure()
        uses: actions/github-script@v6
        with:
          script: |
            github.rest.issues.create({
              owner: context.repo.owner,
              repo: context.repo.repo,
              title: 'PDFium Verification Failed',
              body: 'Scheduled PDFium verification detected issues. See workflow artifacts for details.'
            })
```

## Best Practices

### 1. Run Verification Before Upgrading PDFium

Always verify the new PDFium version before upgrading:

```bash
# Verify current version
pdfium-verify --dll libs/x64/bin/pdfium.dll --format Json --output current.json

# Verify new version
pdfium-verify --dll vendor/pdfium-new/pdfium.dll --format Json --output new.json

# Compare results
diff current.json new.json
```

### 2. Include in Code Review Checklist

When reviewing P/Invoke changes:

- [ ] PDFium verification passes
- [ ] Signature matches PDFium documentation
- [ ] Return type validation passes
- [ ] Behavior tests pass with test PDFs
- [ ] Marshalling attributes are correct

### 3. Add Custom Test PDFs

Create test PDFs that exercise edge cases:

```
tests/Fixtures/
├── simple-1page.pdf          # Basic single-page PDF
├── multi-page-10.pdf         # Multi-page document
├── large-dimensions.pdf      # Maximum size PDF (10000x10000 points)
├── small-dimensions.pdf      # Minimum size PDF (1x1 point)
├── corrupted.pdf             # Invalid/corrupted PDF
├── encrypted-password.pdf    # Password-protected PDF
└── pdf-2.0.pdf              # Latest PDF specification
```

### 4. Monitor Verification Performance

Track verification execution time to detect performance regressions:

```bash
# Benchmark verification
for i in {1..10}; do
  time pdfium-verify --dll pdfium.dll --test-files tests/Fixtures
done | grep real | awk '{sum+=$2; count++} END {print "Average:", sum/count, "seconds"}'
```

### 5. Document Known Issues

Maintain a list of known PDFium quirks:

```
docs/verification/known-issues.md

# Known PDFium Verification Issues

## FPDF_GetPageWidthF Deprecated
- **Issue**: Function removed in PDFium 5.0
- **Solution**: Use FPDF_GetPageWidth instead
- **Status**: Fixed in commit abc123

## Unicode Path Handling
- **Issue**: PDFium has issues with Unicode file paths on Windows
- **Workaround**: Use short paths or ASCII-only paths
- **Status**: Upstream issue pending
```

## Advanced Usage

### Custom Verification Rules

Extend the verification framework for project-specific rules:

```csharp
// src/FluentPDF.Verification.Custom/CustomPdfiumVerifier.cs
public class CustomPdfiumVerifier : ILibraryVerifier<VerificationResult>
{
    public async Task<Result<VerificationResult>> VerifyAsync()
    {
        // Custom verification logic
        // Example: Verify specific function combinations
        // Example: Test performance benchmarks
        // Example: Validate memory usage patterns
    }
}
```

### Batch Verification

Verify multiple PDFium versions in a single run:

```bash
#!/bin/bash
# verify-all-versions.sh

for version in 4.0 4.5 5.0; do
  echo "Verifying PDFium $version..."
  pdfium-verify \
    --dll vendor/pdfium-$version/pdfium.dll \
    --format Json \
    --output pdfium-$version-verification.json

  if [ $? -ne 0 ]; then
    echo "FAILED: PDFium $version"
  else
    echo "PASSED: PDFium $version"
  fi
done
```

### Automated Fix Suggestions

Parse verification output and apply suggested fixes:

```bash
# parse-and-fix.sh
pdfium-verify --dll pdfium.dll --format Json --output results.json

# Parse results and extract suggested fixes
cat results.json | jq '.results[] | select(.success == false) | .suggestedFix'

# Example output:
# "Use FPDF_GetPageWidth instead of FPDF_GetPageWidthF"
# "Add [MarshalAs(UnmanagedType.LPStr)] to parameter"
```

## References

- [PDFium Documentation](https://pdfium.googlesource.com/pdfium/)
- [P/Invoke Marshalling Guide](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/best-practices)
- [FluentPDF Architecture](../ARCHITECTURE.md)
- [Extending Verification for Other Libraries](extending-verification.md)

## Support and Feedback

If you encounter issues not covered in this guide:

1. Check verbose logs: `pdfium-verify --dll pdfium.dll --verbose`
2. Review [GitHub Issues](https://github.com/your-org/FluentPDF/issues)
3. Create a new issue with:
   - Verification output (with `--verbose`)
   - PDFium version
   - Operating system and .NET version
   - Steps to reproduce
