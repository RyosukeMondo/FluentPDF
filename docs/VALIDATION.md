# PDF Validation Architecture

This document provides a comprehensive overview of FluentPDF's PDF validation system, including architecture design, tool integration, validation profiles, and best practices.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Validation Tools](#validation-tools)
- [Validation Profiles](#validation-profiles)
- [Component Details](#component-details)
- [Integration Patterns](#integration-patterns)
- [Performance Considerations](#performance-considerations)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

FluentPDF integrates three industry-standard PDF validation tools to provide comprehensive validation capabilities:

1. **VeraPDF** - PDF/A compliance validation
2. **JHOVE** - PDF format characterization
3. **QPDF** - Structural validation

The validation system is designed with:
- **Modularity** - Each tool wrapped in a separate interface
- **Flexibility** - Profile-based execution (Quick/Standard/Full)
- **Performance** - Parallel tool execution
- **Reliability** - Comprehensive error handling and timeouts
- **Testability** - Interface-based design for mocking

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                       │
│  (Tests, PDF Generation, User Code)                     │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│              IPdfValidationService                       │
│  - ValidateAsync(file, profile)                         │
│  - VerifyToolsInstalledAsync(profile)                   │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────┐
│           PdfValidationService                           │
│  - Orchestrates validation execution                    │
│  - Determines which tools to run based on profile       │
│  - Executes tools in parallel (Task.WhenAll)            │
│  - Aggregates results into ValidationReport             │
│  - Determines overall status (Pass/Warn/Fail)           │
└───┬──────────────┬──────────────┬─────────────────────┘
    │              │              │
    ▼              ▼              ▼
┌─────────┐  ┌─────────┐  ┌──────────────┐
│ IQpdf   │  │ IJhove  │  │ IVeraPdf     │
│ Wrapper │  │ Wrapper │  │ Wrapper      │
└────┬────┘  └────┬────┘  └──────┬───────┘
     │            │              │
     ▼            ▼              ▼
┌─────────┐  ┌─────────┐  ┌──────────────┐
│  QPDF   │  │  JHOVE  │  │   VeraPDF    │
│  CLI    │  │  CLI    │  │   CLI        │
└─────────┘  └─────────┘  └──────────────┘
```

### Component Responsibilities

#### PdfValidationService
- **Purpose**: Orchestrate validation execution
- **Responsibilities**:
  - Accept validation requests with profile and file path
  - Determine which tools to execute based on profile
  - Execute tools in parallel for performance
  - Aggregate individual results into unified report
  - Calculate overall validation status
  - Handle logging with correlation IDs
  - Verify tool installation

#### Wrapper Layer (QpdfWrapper, JhoveWrapper, VeraPdfWrapper)
- **Purpose**: Encapsulate CLI tool execution
- **Responsibilities**:
  - Execute external process with correct arguments
  - Parse CLI output (JSON, stderr, exit codes)
  - Handle process timeouts (30s default)
  - Convert tool output to domain models
  - Return Result<T> with success/failure
  - Log execution details
  - Validate file paths before execution

#### Models
- **Purpose**: Represent validation data
- **Components**:
  - `ValidationReport` - Aggregated validation result
  - `QpdfResult` - Structural validation result
  - `JhoveResult` - Format validation result
  - `VeraPdfResult` - PDF/A compliance result
  - `ValidationProfile` - Execution profile enum
  - `ValidationStatus` - Overall status enum

### Data Flow

```
1. Application calls IPdfValidationService.ValidateAsync(file, profile)
   ↓
2. PdfValidationService determines which tools to run based on profile
   ↓
3. Service executes tool wrappers in parallel (Task.WhenAll)
   ↓
   ┌──────────────────────────────────────────────┐
   │                                              │
   ▼                       ▼                      ▼
   QpdfWrapper            JhoveWrapper            VeraPdfWrapper
   - Executes qpdf        - Executes java -jar   - Executes verapdf
   - Parses stderr        - Parses JSON stdout   - Parses JSON stdout
   - Returns QpdfResult   - Returns JhoveResult  - Returns VeraPdfResult
   │                       │                      │
   └──────────────────────────────────────────────┘
                          ↓
4. Service aggregates results into ValidationReport
   ↓
5. Service determines OverallStatus:
   - Pass: All tools passed
   - Warn: Some tools have warnings
   - Fail: Any tool failed
   ↓
6. Return Result<ValidationReport> to application
```

## Validation Tools

### VeraPDF

**Purpose**: PDF/A compliance validation

**What it validates**:
- PDF/A-1 (ISO 19005-1) - Basic archival standard
- PDF/A-2 (ISO 19005-2) - Allows JPEG2000, transparency
- PDF/A-3 (ISO 19005-3) - Allows embedded files
- Flavours: a (accessible), b (basic), u (Unicode)

**Output**: JSON with compliance status, flavour, and rule violations

**Example output**:
```json
{
  "report": {
    "jobs": [{
      "validationReport": {
        "isCompliant": true,
        "profileName": "PDF/A-1b",
        "details": {
          "failedRules": []
        }
      }
    }]
  }
}
```

**Use cases**:
- Long-term archival validation
- Ensuring accessibility compliance
- Meeting regulatory requirements

**Performance**: ~2-5 seconds for typical documents

### JHOVE

**Purpose**: PDF format characterization and validation

**What it validates**:
- PDF format structure conformance
- PDF version (1.0 - 2.0)
- Format well-formedness and validity
- Metadata extraction (title, author, dates, etc.)
- Security settings (encryption, permissions)

**Output**: JSON with format information, validity status, metadata

**Example output**:
```json
{
  "format": "PDF",
  "version": "1.7",
  "status": "Well-Formed and valid",
  "properties": {
    "title": "Document Title",
    "author": "Author Name",
    "creationDate": "2024-01-10",
    "pageCount": 10
  }
}
```

**Use cases**:
- Format identification
- Metadata extraction
- Version detection
- General validity checking

**Performance**: ~1-3 seconds for typical documents

### QPDF

**Purpose**: Structural validation and corruption detection

**What it validates**:
- Cross-reference table integrity
- Object stream structure
- Encryption consistency
- Internal PDF structure

**Output**: Exit code + stderr with error messages

**Example outputs**:

Success:
```
(exit code 0, no stderr output)
```

Failure:
```
(exit code 2)
file.pdf: invalid cross-reference table
file.pdf: object stream 5 0 has invalid syntax
```

**Use cases**:
- Quick structural validation
- Corruption detection
- CI/CD pipeline validation
- Pre-processing checks

**Performance**: ~0.5-1 second for typical documents (fastest)

## Validation Profiles

FluentPDF provides three validation profiles optimizing for different use cases:

### Quick Profile

**Tools**: QPDF only

**Speed**: Fast (~0.5-1s)

**Use cases**:
- CI/CD pipelines requiring fast feedback
- Pre-flight checks before processing
- Basic structural validation
- High-volume batch processing

**What you get**:
- Structural integrity validation
- Corruption detection
- Cross-reference table validation

**What you don't get**:
- Format details or metadata
- PDF/A compliance

**Example**:
```csharp
var result = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Quick
);

// Fast structural check, suitable for CI
```

### Standard Profile

**Tools**: QPDF + JHOVE

**Speed**: Medium (~1.5-4s)

**Use cases**:
- Format validation with metadata extraction
- Version detection
- General-purpose validation
- Quality assurance workflows

**What you get**:
- Everything from Quick profile
- PDF format validation
- Version identification
- Metadata extraction (title, author, dates, page count)
- Security settings

**What you don't get**:
- PDF/A compliance validation

**Example**:
```csharp
var result = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Standard
);

// Structure + format validation
Console.WriteLine($"PDF Version: {result.Value.JhoveResult?.Format}");
```

### Full Profile

**Tools**: QPDF + JHOVE + VeraPDF

**Speed**: Slow (~2.5-8s)

**Use cases**:
- PDF/A archival validation
- Compliance verification
- Comprehensive quality checks
- Pre-archival validation

**What you get**:
- Everything from Standard profile
- PDF/A compliance validation
- Flavour detection (1a, 1b, 2u, etc.)
- Detailed rule violation reports
- Accessibility checks (for PDF/A-a flavour)

**Example**:
```csharp
var result = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Full
);

// Complete validation including PDF/A
if (result.Value.VeraPdfResult?.Compliant == true)
{
    Console.WriteLine($"PDF/A compliant: {result.Value.VeraPdfResult.Flavour}");
}
```

### Profile Comparison Table

| Feature                    | Quick | Standard | Full |
|---------------------------|-------|----------|------|
| Structural validation      | ✓     | ✓        | ✓    |
| Corruption detection       | ✓     | ✓        | ✓    |
| Format validation          | ✗     | ✓        | ✓    |
| Metadata extraction        | ✗     | ✓        | ✓    |
| Version detection          | ✗     | ✓        | ✓    |
| PDF/A compliance           | ✗     | ✗        | ✓    |
| Parallel execution         | N/A   | ✓        | ✓    |
| Typical duration           | 0.5s  | 2s       | 4s   |
| CI/CD suitability          | ★★★   | ★★☆      | ★☆☆  |
| Archival validation        | ☆☆☆   | ★☆☆      | ★★★  |

## Component Details

### PdfValidationService Implementation

**Key methods**:

```csharp
public async Task<Result<ValidationReport>> ValidateAsync(
    string filePath,
    ValidationProfile profile,
    string? correlationId = null,
    CancellationToken cancellationToken = default)
{
    // 1. Validate file exists
    if (!File.Exists(filePath))
        return Result.Fail($"File not found: {filePath}");

    // 2. Determine which tools to run
    var tasks = new List<Task<Result>>();

    if (profile >= ValidationProfile.Quick)
        tasks.Add(ExecuteQpdfAsync(filePath, correlationId, cancellationToken));

    if (profile >= ValidationProfile.Standard)
        tasks.Add(ExecuteJhoveAsync(filePath, correlationId, cancellationToken));

    if (profile >= ValidationProfile.Full)
        tasks.Add(ExecuteVeraPdfAsync(filePath, correlationId, cancellationToken));

    // 3. Execute tools in parallel
    await Task.WhenAll(tasks);

    // 4. Aggregate results and determine overall status
    var overallStatus = DetermineOverallStatus(results);

    // 5. Build and return report
    return Result.Ok(new ValidationReport { ... });
}
```

**Status aggregation logic**:

```csharp
private ValidationStatus DetermineOverallStatus(...)
{
    // Any tool failed → Fail
    if (qpdf?.Status == "Fail" || jhove?.Status == "Not Valid" || verapdf?.Compliant == false)
        return ValidationStatus.Fail;

    // Any warnings → Warn
    if (HasWarnings(...))
        return ValidationStatus.Warn;

    // All passed → Pass
    return ValidationStatus.Pass;
}
```

### Wrapper Implementation Pattern

All wrappers follow a consistent pattern:

```csharp
public async Task<Result<TResult>> ValidateAsync(
    string filePath,
    string? correlationId = null,
    CancellationToken cancellationToken = default)
{
    // 1. Validate inputs
    if (!File.Exists(filePath))
        return Result.Fail("File not found");

    // 2. Configure process execution
    var psi = new ProcessStartInfo
    {
        FileName = "tool-executable",
        Arguments = $"--format json {filePath}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    // 3. Execute with timeout
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(30));

    using var process = new Process { StartInfo = psi };
    process.Start();

    var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
    var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

    await process.WaitForExitAsync(cts.Token);

    var output = await outputTask;
    var error = await errorTask;

    // 4. Parse output
    var result = ParseOutput(output, error, process.ExitCode);

    // 5. Log and return
    LogResult(correlationId, result);
    return Result.Ok(result);
}
```

## Integration Patterns

### Pattern 1: Validate Generated PDFs

```csharp
public async Task<byte[]> GeneratePdfWithValidation()
{
    // Generate PDF
    var pdfBytes = GeneratePdf(...);
    var tempFile = Path.GetTempFileName();

    try
    {
        File.WriteAllBytes(tempFile, pdfBytes);

        // Validate generated PDF
        var result = await validationService.ValidateAsync(
            tempFile,
            ValidationProfile.Quick
        );

        if (result.Value.OverallStatus != ValidationStatus.Pass)
        {
            throw new InvalidOperationException(
                $"Generated PDF failed validation: {result.Value.Summary}"
            );
        }

        return pdfBytes;
    }
    finally
    {
        File.Delete(tempFile);
    }
}
```

### Pattern 2: Integration Tests

```csharp
[Trait("Category", "Integration")]
[Fact]
public async Task MergePdfs_ShouldProduceValidOutput()
{
    // Arrange
    var service = CreatePdfMergeService();
    var validationService = CreateValidationService();

    // Act
    var mergedPdf = await service.MergeAsync("file1.pdf", "file2.pdf");

    // Assert - validate output
    var result = await validationService.ValidateAsync(
        mergedPdf,
        ValidationProfile.Standard
    );

    Assert.True(result.IsSuccess);
    Assert.Equal(ValidationStatus.Pass, result.Value.OverallStatus);

    // Additional assertions on structure
    Assert.NotNull(result.Value.JhoveResult);
    Assert.Contains("PDF 1.", result.Value.JhoveResult.Format);
}
```

### Pattern 3: Conditional Validation

```csharp
public async Task<bool> ValidateForArchival(string filePath)
{
    // Start with quick structural check
    var quickResult = await validationService.ValidateAsync(
        filePath,
        ValidationProfile.Quick
    );

    if (quickResult.Value.OverallStatus != ValidationStatus.Pass)
    {
        // Structurally invalid, no need for PDF/A check
        return false;
    }

    // Structure is valid, check PDF/A compliance
    var fullResult = await validationService.ValidateAsync(
        filePath,
        ValidationProfile.Full
    );

    return fullResult.Value.VeraPdfResult?.Compliant ?? false;
}
```

### Pattern 4: Batch Validation with Progress

```csharp
public async Task<Dictionary<string, ValidationReport>> ValidateBatch(
    IEnumerable<string> files,
    IProgress<int> progress)
{
    var results = new Dictionary<string, ValidationReport>();
    var completed = 0;

    foreach (var file in files)
    {
        var result = await validationService.ValidateAsync(
            file,
            ValidationProfile.Standard
        );

        if (result.IsSuccess)
        {
            results[file] = result.Value;
        }

        completed++;
        progress.Report(completed);
    }

    return results;
}
```

### Pattern 5: Dependency Injection

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register validation wrappers as singletons (stateless)
        services.AddSingleton<IQpdfWrapper, QpdfWrapper>();
        services.AddSingleton<IJhoveWrapper, JhoveWrapper>();
        services.AddSingleton<IVeraPdfWrapper, VeraPdfWrapper>();

        // Register validation service
        services.AddSingleton<IPdfValidationService, PdfValidationService>();

        // Register your PDF generation services
        services.AddScoped<IPdfGenerator, PdfGenerator>();
    }
}

// Usage in your service
public class PdfGenerator
{
    private readonly IPdfValidationService _validationService;

    public PdfGenerator(IPdfValidationService validationService)
    {
        _validationService = validationService;
    }

    public async Task<byte[]> GenerateAsync(...)
    {
        var pdf = CreatePdf(...);

        // Validate before returning
        var result = await _validationService.ValidateAsync(...);

        return pdf;
    }
}
```

## Performance Considerations

### Parallel Execution

The Full and Standard profiles execute multiple tools concurrently:

```csharp
// Sequential execution (NOT used)
await QpdfAsync();   // 0.5s
await JhoveAsync();  // 2.0s
await VeraPdfAsync(); // 3.0s
// Total: 5.5s

// Parallel execution (USED)
await Task.WhenAll(
    QpdfAsync(),      // Start: 0s, End: 0.5s
    JhoveAsync(),     // Start: 0s, End: 2.0s
    VeraPdfAsync()    // Start: 0s, End: 3.0s
);
// Total: 3.0s (slowest tool)
```

**Speedup**: ~40-60% faster for Full profile

### Optimization Strategies

#### 1. Choose the Right Profile

```csharp
// CI/CD - Use Quick for fast feedback
var ciResult = await validationService.ValidateAsync(file, ValidationProfile.Quick);

// Production archival - Use Full only when needed
if (requiresPdfA)
{
    var fullResult = await validationService.ValidateAsync(file, ValidationProfile.Full);
}
else
{
    var standardResult = await validationService.ValidateAsync(file, ValidationProfile.Standard);
}
```

#### 2. Batch Processing with Parallelism

```csharp
// Process multiple files in parallel
var files = GetPdfFiles();
var batchSize = Environment.ProcessorCount * 2;

var results = await files
    .Chunk(batchSize)
    .SelectMany(async batch =>
    {
        var tasks = batch.Select(file =>
            validationService.ValidateAsync(file, ValidationProfile.Quick)
        );
        return await Task.WhenAll(tasks);
    })
    .ToListAsync();
```

#### 3. Early Exit on Failure

```csharp
// Validate structure first, skip expensive checks if invalid
var quickResult = await validationService.ValidateAsync(file, ValidationProfile.Quick);

if (quickResult.Value.OverallStatus == ValidationStatus.Fail)
{
    // Structure invalid, no point running JHOVE/VeraPDF
    return quickResult.Value;
}

// Structure valid, proceed with full validation
return await validationService.ValidateAsync(file, ValidationProfile.Full);
```

#### 4. Timeout Management

```csharp
// For large PDFs, increase timeout
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

var result = await validationService.ValidateAsync(
    largePdf,
    ValidationProfile.Full,
    cancellationToken: cts.Token
);
```

### Performance Benchmarks

Typical execution times (varies by PDF size and complexity):

| Profile   | Small PDF (< 1MB) | Medium PDF (1-10MB) | Large PDF (> 10MB) |
|-----------|------------------|---------------------|-------------------|
| Quick     | 0.2 - 0.5s       | 0.5 - 1.5s          | 1.5 - 3s          |
| Standard  | 0.8 - 2s         | 2 - 5s              | 5 - 15s           |
| Full      | 1.5 - 4s         | 4 - 10s             | 10 - 30s          |

**Notes**:
- VeraPDF is typically the slowest tool
- QPDF is the fastest (useful for CI)
- JHOVE performance depends on Java JIT warmup
- First validation may be slower (process startup)

## Best Practices

### 1. Always Verify Tools Installation

```csharp
public class Startup
{
    public async Task InitializeAsync(IServiceProvider services)
    {
        var validation = services.GetRequiredService<IPdfValidationService>();

        var result = await validation.VerifyToolsInstalledAsync(ValidationProfile.Full);

        if (result.IsFailed)
        {
            throw new InvalidOperationException(
                "PDF validation tools not installed. Run tools/validation/install-tools.ps1"
            );
        }
    }
}
```

### 2. Use Correlation IDs for Distributed Tracing

```csharp
public async Task ProcessRequest(HttpRequest request)
{
    var correlationId = request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

    var result = await validationService.ValidateAsync(
        file,
        profile,
        correlationId: correlationId
    );

    // All logs will include correlation ID for tracing
}
```

### 3. Handle Failures Gracefully

```csharp
var result = await validationService.ValidateAsync(file, profile);

if (result.IsFailed)
{
    // Tool execution failed (tool not found, timeout, etc.)
    logger.LogError("Validation execution failed: {Errors}",
        string.Join(", ", result.Errors.Select(e => e.Message)));
    return;
}

if (result.Value.OverallStatus == ValidationStatus.Fail)
{
    // Validation completed but PDF is invalid
    logger.LogWarning("PDF validation failed: {Summary}", result.Value.Summary);

    // Report specific errors
    ReportErrors(result.Value);
}
```

### 4. Include Validation in CI/CD

```yaml
# .github/workflows/test.yml
- name: Install PDF validation tools
  run: pwsh ./tools/validation/install-tools.ps1

- name: Run integration tests with validation
  run: dotnet test --filter "Category=Integration"

- name: Upload validation reports
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: validation-reports
    path: '**/*-validation-report.json'
```

### 5. Cache Tool Installation

```yaml
# Cache validation tools in CI
- name: Cache validation tools
  uses: actions/cache@v4
  with:
    path: tools/validation/
    key: validation-tools-${{ runner.os }}-v1

- name: Install validation tools
  if: steps.cache.outputs.cache-hit != 'true'
  run: pwsh ./tools/validation/install-tools.ps1
```

### 6. Unit Test with Mocked Wrappers

```csharp
[Fact]
public async Task ValidateAsync_FullProfile_RunsAllTools()
{
    // Arrange
    var qpdfMock = new Mock<IQpdfWrapper>();
    qpdfMock.Setup(x => x.ValidateAsync(...))
        .ReturnsAsync(Result.Ok(new QpdfResult { Status = "Pass" }));

    var jhoveMock = new Mock<IJhoveWrapper>();
    jhoveMock.Setup(x => x.ValidateAsync(...))
        .ReturnsAsync(Result.Ok(new JhoveResult { Status = "Valid" }));

    var veraPdfMock = new Mock<IVeraPdfWrapper>();
    veraPdfMock.Setup(x => x.ValidateAsync(...))
        .ReturnsAsync(Result.Ok(new VeraPdfResult { Compliant = true }));

    var service = new PdfValidationService(
        qpdfMock.Object,
        jhoveMock.Object,
        veraPdfMock.Object
    );

    // Act
    var result = await service.ValidateAsync("test.pdf", ValidationProfile.Full);

    // Assert
    qpdfMock.Verify(x => x.ValidateAsync(...), Times.Once);
    jhoveMock.Verify(x => x.ValidateAsync(...), Times.Once);
    veraPdfMock.Verify(x => x.ValidateAsync(...), Times.Once);
}
```

### 7. Store Validation Reports

```csharp
public async Task ValidateAndStoreReport(string pdfPath)
{
    var result = await validationService.ValidateAsync(
        pdfPath,
        ValidationProfile.Full
    );

    if (result.IsSuccess)
    {
        // Serialize report to JSON
        var reportJson = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Store alongside PDF
        var reportPath = Path.ChangeExtension(pdfPath, ".validation.json");
        await File.WriteAllTextAsync(reportPath, reportJson);
    }
}
```

## Troubleshooting

### Common Issues

#### 1. Tool Not Found

**Symptom**: `Error: QPDF executable not found`

**Solution**:
```bash
# Install validation tools
pwsh ./tools/validation/install-tools.ps1

# Verify installation
./tools/validation/verapdf/verapdf --version
java -jar ./tools/validation/jhove/jhove.jar -v
qpdf --version
```

#### 2. Java Not Found (JHOVE)

**Symptom**: `Error: Java not found. JHOVE requires Java Runtime Environment.`

**Solution**:
```bash
# Install Java
# Ubuntu/Debian
sudo apt-get install default-jre

# macOS
brew install openjdk@17

# Windows
# Download from https://adoptium.net/

# Verify
java -version
```

#### 3. Permission Denied (Linux/macOS)

**Symptom**: `Permission denied: ./verapdf/verapdf`

**Solution**:
```bash
chmod +x ./tools/validation/verapdf/verapdf
```

#### 4. Timeout Errors

**Symptom**: `Validation timed out after 30 seconds`

**Solution**:
```csharp
// Increase timeout for large PDFs
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

var result = await validationService.ValidateAsync(
    largePdf,
    profile,
    cancellationToken: cts.Token
);
```

#### 5. JSON Parsing Errors

**Symptom**: `Error parsing VeraPDF JSON output`

**Cause**: Tool version mismatch, corrupted output

**Solution**:
```bash
# Update to latest tool versions
pwsh ./tools/validation/install-tools.ps1 -Force

# Verify tools work standalone
./tools/validation/verapdf/verapdf --format json test.pdf
```

#### 6. CI Failures

**Symptom**: Tests pass locally but fail in CI

**Solution**:
```yaml
# Ensure tools are installed in CI
- name: Install validation tools
  run: pwsh ./tools/validation/install-tools.ps1

# Verify installation
- name: Verify tools
  run: |
    ./tools/validation/verapdf/verapdf --version
    java -jar ./tools/validation/jhove/jhove.jar -v
    qpdf --version

# Use conditional execution for integration tests
- name: Run tests
  run: dotnet test --filter "Category=Integration"
```

### Debugging Tips

#### Enable Detailed Logging

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Enable debug logs
    .WriteTo.Console()
    .WriteTo.File("validation-logs.txt")
    .CreateLogger();

// Logs will include:
// - Tool execution commands
// - Process output (stdout/stderr)
// - Execution durations
// - Parsing details
```

#### Test Wrappers Independently

```csharp
// Test QPDF wrapper directly
var qpdf = new QpdfWrapper();
var result = await qpdf.ValidateAsync("test.pdf");

Console.WriteLine($"Status: {result.Value.Status}");
Console.WriteLine($"Errors: {string.Join(", ", result.Value.Errors)}");
```

#### Capture Raw Tool Output

```bash
# Run tools manually to see raw output
./tools/validation/verapdf/verapdf --format json test.pdf > verapdf-output.json
java -jar ./tools/validation/jhove/jhove.jar -m PDF-hul -h json test.pdf > jhove-output.json
qpdf --check test.pdf 2> qpdf-errors.txt
```

## Summary

FluentPDF's validation system provides:

- **Comprehensive validation** using industry-standard tools
- **Flexible profiles** optimizing for different use cases
- **High performance** through parallel execution
- **Easy integration** with interface-based design
- **Production-ready** error handling and logging

Choose your validation profile based on needs:
- **Quick** for fast CI/CD validation
- **Standard** for format validation and metadata
- **Full** for PDF/A compliance and archival

For more information:
- [FluentPDF.Validation README](../src/FluentPDF.Validation/README.md)
- [Tool Installation Guide](../tools/validation/README.md)
- [VeraPDF Documentation](https://docs.verapdf.org/)
- [JHOVE Documentation](https://jhove.openpreserve.org/documentation/)
- [QPDF Documentation](https://qpdf.readthedocs.io/)
