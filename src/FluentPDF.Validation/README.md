# FluentPDF.Validation

Comprehensive PDF validation library integrating industry-standard validation tools (VeraPDF, JHOVE, QPDF) for .NET 8.

## Features

- **PDF/A Compliance Validation** - VeraPDF integration for PDF/A-1, PDF/A-2, PDF/A-3 standards
- **Format Validation** - JHOVE integration for PDF format characterization and metadata extraction
- **Structural Validation** - QPDF integration for cross-reference table and structural integrity checks
- **Flexible Validation Profiles** - Quick, Standard, and Full validation modes
- **Parallel Execution** - Multiple validation tools run concurrently for performance
- **Comprehensive Reports** - JSON-serializable validation reports with detailed error information
- **Async/Await Support** - Fully asynchronous API with cancellation token support
- **Dependency Injection Ready** - Interface-based design for easy testing and DI integration

## Installation

### 1. Install Validation Tools

Run the installation script to download and configure validation tools:

```bash
# From the repository root
pwsh ./tools/validation/install-tools.ps1
```

This will install:
- **VeraPDF** 1.26.1 - PDF/A compliance validator
- **JHOVE** 1.30.1 - PDF format characterization (requires Java)
- **QPDF** 11.9.1 - Structural validation tool

**Requirements:**
- PowerShell 7.0+
- Java Runtime Environment (for JHOVE) - Download from https://adoptium.net/

See [tools/validation/README.md](../../tools/validation/README.md) for detailed installation instructions.

### 2. Add Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="../FluentPDF.Validation/FluentPDF.Validation.csproj" />
</ItemGroup>
```

## Quick Start

### Basic Usage

```csharp
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Wrappers;

// Create wrappers for validation tools
var qpdfWrapper = new QpdfWrapper();
var jhoveWrapper = new JhoveWrapper();
var veraPdfWrapper = new VeraPdfWrapper();

// Create validation service
var validationService = new PdfValidationService(
    qpdfWrapper,
    jhoveWrapper,
    veraPdfWrapper
);

// Validate a PDF with Full profile (all tools)
var result = await validationService.ValidateAsync(
    filePath: "document.pdf",
    profile: ValidationProfile.Full
);

if (result.IsSuccess)
{
    var report = result.Value;
    Console.WriteLine($"Status: {report.OverallStatus}");
    Console.WriteLine($"Summary: {report.Summary}");
    Console.WriteLine($"Duration: {report.Duration.TotalSeconds:F2}s");

    if (report.IsValid)
    {
        Console.WriteLine("✓ PDF is valid");
    }
    else
    {
        Console.WriteLine("✗ PDF validation failed");
        // Inspect individual tool results for details
        if (report.QpdfResult?.Status == "Fail")
        {
            Console.WriteLine($"  QPDF errors: {string.Join(", ", report.QpdfResult.Errors)}");
        }
    }
}
else
{
    Console.WriteLine($"Validation error: {result.Errors[0].Message}");
}
```

### Validation Profiles

Choose a profile based on your validation needs:

```csharp
// Quick - Fast structural validation (QPDF only)
var quickResult = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Quick
);

// Standard - Format + structure (QPDF + JHOVE)
var standardResult = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Standard
);

// Full - Complete validation including PDF/A (all tools)
var fullResult = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Full
);
```

| Profile   | Tools Used              | Speed  | Use Case                                    |
|-----------|------------------------|--------|---------------------------------------------|
| Quick     | QPDF                   | Fast   | Quick structural checks, CI/CD pipelines    |
| Standard  | QPDF + JHOVE           | Medium | Format validation, metadata extraction      |
| Full      | QPDF + JHOVE + VeraPDF | Slow   | PDF/A compliance, archival, comprehensive   |

### Verify Tools Installation

Check that validation tools are installed and accessible:

```csharp
var verifyResult = await validationService.VerifyToolsInstalledAsync(
    ValidationProfile.Full
);

if (verifyResult.IsSuccess)
{
    Console.WriteLine("All validation tools are installed");
}
else
{
    Console.WriteLine($"Missing tools: {verifyResult.Errors[0].Message}");
}
```

### Using Correlation IDs for Logging

Track validation operations across distributed systems:

```csharp
var correlationId = Guid.NewGuid().ToString();

var result = await validationService.ValidateAsync(
    filePath: "document.pdf",
    profile: ValidationProfile.Full,
    correlationId: correlationId
);

// All log messages will include the correlation ID
```

### Cancellation Support

Cancel long-running validation operations:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await validationService.ValidateAsync(
        filePath: "large-document.pdf",
        profile: ValidationProfile.Full,
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Validation timed out after 30 seconds");
}
```

## Validation Report Structure

The `ValidationReport` contains aggregated results from all executed tools:

```csharp
public sealed class ValidationReport
{
    // Overall status: Pass, Warn, or Fail
    public ValidationStatus OverallStatus { get; init; }

    // Path to validated file
    public string FilePath { get; init; }

    // Timestamp of validation
    public DateTime ValidationDate { get; init; }

    // Profile used (Quick/Standard/Full)
    public ValidationProfile Profile { get; init; }

    // Individual tool results (null if tool not executed)
    public QpdfResult? QpdfResult { get; init; }
    public JhoveResult? JhoveResult { get; init; }
    public VeraPdfResult? VeraPdfResult { get; init; }

    // Total execution time
    public TimeSpan Duration { get; init; }

    // Human-readable summary
    public string Summary { get; }

    // Convenience property
    public bool IsValid { get; }
}
```

### ValidationStatus Meanings

- **Pass** - All validation checks passed successfully
- **Warn** - Validation completed with warnings but no critical failures
- **Fail** - One or more validation checks failed

### Individual Tool Results

#### QpdfResult

```csharp
public sealed class QpdfResult
{
    public string Status { get; init; }        // "Pass" or "Fail"
    public List<string> Errors { get; init; }  // Structural errors found
}
```

#### JhoveResult

```csharp
public sealed class JhoveResult
{
    public string Format { get; init; }           // e.g., "PDF 1.7"
    public string Status { get; init; }           // "Well-Formed", "Valid", "Not Valid"
    public Dictionary<string, object?> Metadata { get; init; }  // Title, author, etc.
}
```

#### VeraPdfResult

```csharp
public sealed class VeraPdfResult
{
    public bool Compliant { get; init; }              // PDF/A compliant?
    public string? Flavour { get; init; }             // e.g., "1b", "2u", "3a"
    public List<VeraPdfError> Errors { get; init; }   // Compliance errors
}

public sealed class VeraPdfError
{
    public string RuleId { get; init; }         // PDF/A rule reference
    public string Description { get; init; }    // Human-readable description
    public int? Page { get; init; }             // Page number (if applicable)
}
```

## Dependency Injection

Register validation services in your DI container:

```csharp
using Microsoft.Extensions.DependencyInjection;
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Wrappers;

var services = new ServiceCollection();

// Register wrappers as singletons (stateless)
services.AddSingleton<IQpdfWrapper, QpdfWrapper>();
services.AddSingleton<IJhoveWrapper, JhoveWrapper>();
services.AddSingleton<IVeraPdfWrapper, VeraPdfWrapper>();

// Register validation service
services.AddSingleton<IPdfValidationService, PdfValidationService>();

var serviceProvider = services.BuildServiceProvider();

// Use the service
var validationService = serviceProvider.GetRequiredService<IPdfValidationService>();
```

## Testing

### Unit Testing with Mocked Wrappers

```csharp
using Moq;
using FluentPDF.Validation.Wrappers;
using FluentPDF.Validation.Services;
using FluentResults;

[Fact]
public async Task ValidateAsync_WithQuickProfile_ExecutesOnlyQpdf()
{
    // Arrange
    var qpdfMock = new Mock<IQpdfWrapper>();
    qpdfMock
        .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Ok(new QpdfResult { Status = "Pass", Errors = [] }));

    var jhoveMock = new Mock<IJhoveWrapper>();
    var veraPdfMock = new Mock<IVeraPdfWrapper>();

    var service = new PdfValidationService(
        qpdfMock.Object,
        jhoveMock.Object,
        veraPdfMock.Object
    );

    // Act
    var result = await service.ValidateAsync("test.pdf", ValidationProfile.Quick);

    // Assert
    Assert.True(result.IsSuccess);
    qpdfMock.Verify(x => x.ValidateAsync("test.pdf", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    jhoveMock.Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    veraPdfMock.Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

### Integration Testing

```csharp
[Trait("Category", "Integration")]
[Fact]
public async Task ValidateAsync_WithValidPdf_ReturnsPass()
{
    // Requires validation tools installed
    var service = new PdfValidationService(
        new QpdfWrapper(),
        new JhoveWrapper(),
        new VeraPdfWrapper()
    );

    var result = await service.ValidateAsync(
        "tests/Fixtures/validation/valid-pdf17.pdf",
        ValidationProfile.Full
    );

    Assert.True(result.IsSuccess);
    Assert.Equal(ValidationStatus.Pass, result.Value.OverallStatus);
}
```

## Logging

The validation library uses structured logging with Serilog. Enable logging in your application:

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// Logs will include:
// - Tool execution commands
// - Execution durations
// - Validation results
// - Correlation IDs
// - Error details
```

Example log output:

```
[INF] Executing QPDF validation for document.pdf (CorrelationId: abc123)
[INF] QPDF validation completed in 0.45s (Status: Pass)
[INF] Executing JHOVE validation for document.pdf (CorrelationId: abc123)
[INF] JHOVE validation completed in 1.23s (Format: PDF 1.7, Status: Valid)
[INF] Executing VeraPDF validation for document.pdf (CorrelationId: abc123)
[INF] VeraPDF validation completed in 2.15s (Compliant: true, Flavour: 1b)
[INF] Validation completed (OverallStatus: Pass, Duration: 2.34s)
```

## Error Handling

All validation methods return `Result<T>` from FluentResults library:

```csharp
var result = await validationService.ValidateAsync("document.pdf", ValidationProfile.Full);

if (result.IsFailed)
{
    // Validation operation failed (tool not found, file not found, etc.)
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
else
{
    var report = result.Value;

    if (report.OverallStatus == ValidationStatus.Fail)
    {
        // Validation completed but PDF is invalid
        Console.WriteLine("PDF validation failed:");

        if (report.QpdfResult?.Status == "Fail")
        {
            Console.WriteLine("Structural errors:");
            foreach (var error in report.QpdfResult.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        if (report.VeraPdfResult?.Compliant == false)
        {
            Console.WriteLine("PDF/A compliance errors:");
            foreach (var error in report.VeraPdfResult.Errors)
            {
                Console.WriteLine($"  - {error.RuleId}: {error.Description}");
            }
        }
    }
}
```

## Performance Considerations

### Parallel Execution

The Full and Standard profiles execute multiple tools in parallel using `Task.WhenAll`:

```csharp
// Full profile executes all three tools concurrently
var result = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Full  // QPDF, JHOVE, VeraPDF run in parallel
);

// Total duration ≈ slowest tool duration (not sum of all tools)
Console.WriteLine($"Completed in {result.Value.Duration.TotalSeconds:F2}s");
```

### Choosing the Right Profile

- **Quick** - Use for CI/CD where fast feedback is critical
- **Standard** - Use when you need format information and metadata
- **Full** - Use when PDF/A compliance is required

### Timeouts

All tool wrappers have a default 30-second timeout. For large PDFs, consider:

```csharp
// Use cancellation token with longer timeout
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

var result = await validationService.ValidateAsync(
    "large-document.pdf",
    ValidationProfile.Full,
    cancellationToken: cts.Token
);
```

## JSON Schema

A JSON Schema is available for ValidationReport serialization:

```
schemas/validation-report.schema.json
```

Use it to validate JSON output from the validation service:

```csharp
using System.Text.Json;

var report = result.Value;
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true
});

// Validate against schema
// Use your preferred JSON Schema validator
```

## Common Scenarios

### Validating PDF Generation Output

```csharp
// After generating a PDF, validate it
var pdfBytes = GeneratePdf();
File.WriteAllBytes("output.pdf", pdfBytes);

var result = await validationService.ValidateAsync(
    "output.pdf",
    ValidationProfile.Quick
);

if (result.Value.OverallStatus != ValidationStatus.Pass)
{
    throw new InvalidOperationException(
        $"Generated PDF failed validation: {result.Value.Summary}"
    );
}
```

### Batch Validation

```csharp
var files = Directory.GetFiles("pdfs", "*.pdf");
var validationTasks = files.Select(file =>
    validationService.ValidateAsync(file, ValidationProfile.Standard)
);

var results = await Task.WhenAll(validationTasks);

foreach (var (file, result) in files.Zip(results))
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"{file}: {result.Value.OverallStatus}");
    }
}
```

### Conditional PDF/A Validation

```csharp
// Start with quick validation
var quickResult = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Quick
);

if (quickResult.Value.OverallStatus == ValidationStatus.Pass)
{
    // If structurally sound, check PDF/A compliance
    var fullResult = await validationService.ValidateAsync(
        "document.pdf",
        ValidationProfile.Full
    );

    return fullResult.Value.VeraPdfResult?.Compliant ?? false;
}

return false;
```

## Troubleshooting

### Tool Not Found Errors

```
Error: QPDF executable not found
```

**Solution:** Ensure validation tools are installed:
```bash
pwsh ./tools/validation/install-tools.ps1
```

Verify installation:
```csharp
var verifyResult = await validationService.VerifyToolsInstalledAsync(ValidationProfile.Full);
if (verifyResult.IsFailed)
{
    Console.WriteLine("Missing tools - please run install-tools.ps1");
}
```

### Java Not Found (JHOVE)

```
Error: Java not found. JHOVE requires Java Runtime Environment.
```

**Solution:** Install Java from https://adoptium.net/ and ensure `java` is in your PATH.

### Timeout Errors

```
Error: Validation timed out after 30 seconds
```

**Solution:** Increase timeout using cancellation token:
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
var result = await validationService.ValidateAsync("file.pdf", profile, cancellationToken: cts.Token);
```

## API Reference

### IPdfValidationService

```csharp
public interface IPdfValidationService
{
    Task<Result<ValidationReport>> ValidateAsync(
        string filePath,
        ValidationProfile profile,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );

    Task<Result> VerifyToolsInstalledAsync(ValidationProfile profile);
}
```

### IQpdfWrapper, IJhoveWrapper, IVeraPdfWrapper

```csharp
public interface IQpdfWrapper
{
    Task<Result<QpdfResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}

// Similar interfaces for IJhoveWrapper and IVeraPdfWrapper
```

## Further Reading

- [Validation Architecture](../../docs/VALIDATION.md) - Detailed architecture documentation
- [Tool Installation Guide](../../tools/validation/README.md) - Installation and troubleshooting
- [VeraPDF Documentation](https://docs.verapdf.org/) - PDF/A validation details
- [JHOVE Documentation](https://jhove.openpreserve.org/documentation/) - Format validation
- [QPDF Documentation](https://qpdf.readthedocs.io/) - Structural validation

## License

This library is part of FluentPDF and follows the project's license.

External validation tools have their own licenses:
- VeraPDF: GPL v3 / MPL 2.0
- JHOVE: LGPL v2.1
- QPDF: Apache 2.0
