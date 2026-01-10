using FluentAssertions;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Wrappers;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FluentPDF.Validation.Tests.Integration;

/// <summary>
/// Integration tests for PDF validation system using real validation tools.
/// These tests require QPDF, JHOVE, and VeraPDF to be installed.
/// </summary>
[Trait("Category", "Integration")]
public class ValidationIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IPdfValidationService _service;
    private readonly string _fixturesPath;
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly ILogger<PdfValidationService> _msLogger;

    public ValidationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Setup Serilog logger that writes to test output
        _serilogLogger = new LoggerConfiguration()
            .WriteTo.TestOutput(output)
            .CreateLogger();

        // Setup Microsoft.Extensions.Logging logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(_serilogLogger);
        });
        _msLogger = loggerFactory.CreateLogger<PdfValidationService>();

        // Create real wrappers
        var qpdfWrapper = new QpdfWrapper(_serilogLogger);
        var jhoveWrapper = new JhoveWrapper(_serilogLogger);
        var veraPdfWrapper = new VeraPdfWrapper(_serilogLogger);

        // Create service with real dependencies
        _service = new PdfValidationService(
            qpdfWrapper,
            jhoveWrapper,
            veraPdfWrapper,
            _msLogger);

        // Get fixtures path
        var solutionRoot = GetSolutionRoot();
        _fixturesPath = Path.Combine(solutionRoot, "tests", "Fixtures", "validation");

        _output.WriteLine($"Fixtures path: {_fixturesPath}");
        _output.WriteLine($"Fixtures exist: {Directory.Exists(_fixturesPath)}");
    }

    public void Dispose()
    {
        _serilogLogger.Dispose();
    }

    [Fact]
    public async Task QuickProfile_WithValidPdf_PassesValidation()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdf17.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.Profile.Should().Be(ValidationProfile.Quick);
        report.QpdfResult.Should().NotBeNull();
        report.QpdfResult!.Status.Should().Be(ValidationStatus.Pass);

        // Quick profile should not execute JHOVE or VeraPDF
        report.JhoveResult.Should().BeNull();
        report.VeraPdfResult.Should().BeNull();

        report.OverallStatus.Should().Be(ValidationStatus.Pass);
        report.IsValid.Should().BeTrue();

        _output.WriteLine($"Overall Status: {report.OverallStatus}");
        _output.WriteLine($"Duration: {report.Duration.TotalMilliseconds}ms");
        _output.WriteLine($"Summary: {report.Summary}");
    }

    [Fact]
    public async Task StandardProfile_WithValidPdf_DetectsFormat()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdf17.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Standard);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.Profile.Should().Be(ValidationProfile.Standard);

        // Standard profile should execute QPDF and JHOVE
        report.QpdfResult.Should().NotBeNull();
        report.QpdfResult!.Status.Should().Be(ValidationStatus.Pass);

        report.JhoveResult.Should().NotBeNull();
        report.JhoveResult!.Status.Should().Be(ValidationStatus.Pass);
        report.JhoveResult.Format.Should().NotBeNullOrEmpty();

        // VeraPDF should not be executed
        report.VeraPdfResult.Should().BeNull();

        report.OverallStatus.Should().Be(ValidationStatus.Pass);

        _output.WriteLine($"PDF Format: {report.JhoveResult.Format}");
        _output.WriteLine($"Overall Status: {report.OverallStatus}");
        _output.WriteLine($"Duration: {report.Duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task FullProfile_WithValidPdfA_VerifiesCompliance()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdfa-1b.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.Profile.Should().Be(ValidationProfile.Full);

        // Full profile should execute all tools
        report.QpdfResult.Should().NotBeNull();
        report.QpdfResult!.Status.Should().Be(ValidationStatus.Pass);

        report.JhoveResult.Should().NotBeNull();
        report.JhoveResult!.Status.Should().Be(ValidationStatus.Pass);

        report.VeraPdfResult.Should().NotBeNull();
        report.VeraPdfResult!.IsCompliant.Should().BeTrue();
        report.VeraPdfResult.Flavour.Should().Be(PdfFlavour.PdfA1b);
        report.VeraPdfResult.Status.Should().Be(ValidationStatus.Pass);

        report.OverallStatus.Should().Be(ValidationStatus.Pass);
        report.IsValid.Should().BeTrue();

        _output.WriteLine($"PDF/A Flavour: {report.VeraPdfResult.Flavour}");
        _output.WriteLine($"Compliant: {report.VeraPdfResult.IsCompliant}");
        _output.WriteLine($"Overall Status: {report.OverallStatus}");
        _output.WriteLine($"Duration: {report.Duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task FullProfile_WithPdfA2u_VerifiesCompliance()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdfa-2u.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.Profile.Should().Be(ValidationProfile.Full);
        report.OverallStatus.Should().Be(ValidationStatus.Pass);

        report.VeraPdfResult.Should().NotBeNull();
        report.VeraPdfResult!.IsCompliant.Should().BeTrue();
        report.VeraPdfResult.Flavour.Should().Be(PdfFlavour.PdfA2u);

        _output.WriteLine($"PDF/A Flavour: {report.VeraPdfResult.Flavour}");
        _output.WriteLine($"Overall Status: {report.OverallStatus}");
    }

    [Fact]
    public async Task QuickProfile_WithInvalidStructure_FailsValidation()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "invalid-structure.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.QpdfResult.Should().NotBeNull();
        report.QpdfResult!.Status.Should().Be(ValidationStatus.Fail);
        report.QpdfResult.Errors.Should().NotBeEmpty();

        report.OverallStatus.Should().Be(ValidationStatus.Fail);
        report.IsValid.Should().BeFalse();

        _output.WriteLine($"Overall Status: {report.OverallStatus}");
        _output.WriteLine($"QPDF Errors: {string.Join(", ", report.QpdfResult.Errors)}");
        _output.WriteLine($"Summary: {report.Summary}");
    }

    [Fact]
    public async Task FullProfile_WithInvalidPdfA_FailsCompliance()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "invalid-pdfa.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        // The file may pass structural validation but fail PDF/A compliance
        report.VeraPdfResult.Should().NotBeNull();
        report.VeraPdfResult!.IsCompliant.Should().BeFalse();
        report.VeraPdfResult.Errors.Should().NotBeEmpty();

        report.OverallStatus.Should().Be(ValidationStatus.Fail);
        report.IsValid.Should().BeFalse();

        _output.WriteLine($"Overall Status: {report.OverallStatus}");
        _output.WriteLine($"VeraPDF Compliant: {report.VeraPdfResult.IsCompliant}");
        _output.WriteLine($"VeraPDF Errors: {report.VeraPdfResult.Errors.Count}");

        foreach (var error in report.VeraPdfResult.Errors.Take(3))
        {
            _output.WriteLine($"  - {error.Description} (Rule: {error.RuleReference})");
        }
    }

    [Fact]
    public async Task FullProfile_ExecutesToolsInParallel()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdf17.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();

        // Act - Run multiple times to get average
        var durations = new List<TimeSpan>();
        for (int i = 0; i < 3; i++)
        {
            var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);
            result.IsSuccess.Should().BeTrue();
            durations.Add(result.Value.Duration);
            _output.WriteLine($"Run {i + 1}: {result.Value.Duration.TotalMilliseconds}ms");
        }

        // Assert
        var avgDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
        _output.WriteLine($"Average Duration: {avgDuration.TotalMilliseconds}ms");

        // If tools were executing sequentially, the duration would be much longer
        // This is a heuristic test - parallel execution should complete in reasonable time
        // We're not making hard assertions on timing as it depends on system load
        avgDuration.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "parallel execution should complete within reasonable time");
    }

    [Fact]
    public async Task ValidationReport_ContainsCorrectMetadata()
    {
        // Arrange
        var filePath = Path.Combine(_fixturesPath, "valid-pdf17.pdf");
        SkipIfFileNotFound(filePath);
        SkipIfToolsNotInstalled();
        var correlationId = $"test-{Guid.NewGuid():N}";

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick, correlationId);
        var endTime = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;

        report.FilePath.Should().Be(filePath);
        report.Profile.Should().Be(ValidationProfile.Quick);
        report.ValidationDate.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
        report.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        report.Duration.Should().BeLessThan(endTime - startTime + TimeSpan.FromSeconds(1));
        report.Summary.Should().NotBeNullOrEmpty();

        _output.WriteLine($"File Path: {report.FilePath}");
        _output.WriteLine($"Validation Date: {report.ValidationDate}");
        _output.WriteLine($"Duration: {report.Duration.TotalMilliseconds}ms");
        _output.WriteLine($"Summary: {report.Summary}");
    }

    private static string GetSolutionRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null && !File.Exists(Path.Combine(directory, "FluentPDF.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }
        return directory ?? throw new InvalidOperationException("Could not find solution root");
    }

    private static void SkipIfFileNotFound(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new SkipException($"Test fixture not found: {filePath}");
        }
    }

    private static void SkipIfToolsNotInstalled()
    {
        // Check if QPDF is available
        try
        {
            var qpdfCheck = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "qpdf",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (qpdfCheck == null)
            {
                throw new SkipException("QPDF is not installed or not in PATH");
            }

            qpdfCheck.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is not SkipException)
        {
            throw new SkipException($"QPDF is not installed or not in PATH: {ex.Message}");
        }

        // Note: We skip checking JHOVE and VeraPDF here because the tests will fail gracefully
        // if those tools are not installed. The Quick profile test only requires QPDF.
    }
}
