using FluentAssertions;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Wrappers;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Xunit;

namespace FluentPDF.Core.Tests.Integration;

/// <summary>
/// Integration tests validating PDF optimize operations produce valid output.
/// Tests require validation tools installed (QPDF, JHOVE).
/// </summary>
[Trait("Category", "Integration")]
public class OptimizeValidationTests : IDisposable
{
    private readonly IDocumentEditingService _editingService;
    private readonly IPdfValidationService _validationService;
    private readonly List<string> _tempFiles = [];

    public OptimizeValidationTests()
    {
        // Initialize services with NullLogger for testing
        _editingService = new DocumentEditingService(NullLogger<DocumentEditingService>.Instance);

        // Initialize validation wrappers with Serilog's silent logger
        var qpdfWrapper = new QpdfWrapper(Serilog.Core.Logger.None);
        var jhoveWrapper = new JhoveWrapper(Serilog.Core.Logger.None);
        var veraPdfWrapper = new VeraPdfWrapper(Serilog.Core.Logger.None);

        _validationService = new PdfValidationService(
            qpdfWrapper,
            jhoveWrapper,
            veraPdfWrapper,
            NullLogger<PdfValidationService>.Instance);
    }

    [Fact]
    public async Task OptimizePdfDefaultOptions_ProducesValidOutput()
    {
        // Arrange
        var sourcePath = "tests/Fixtures/sample.pdf";
        var outputPath = GetTempOutputPath("optimized-default.pdf");
        var options = new OptimizationOptions(); // Default settings

        // Act
        var optimizeResult = await _editingService.OptimizeAsync(
            sourcePath,
            outputPath,
            options);

        // Assert - Optimize should succeed
        optimizeResult.IsSuccess.Should().BeTrue($"optimize operation should succeed: {string.Join(", ", optimizeResult.Errors)}");
        File.Exists(outputPath).Should().BeTrue("optimized PDF should be created");

        var result = optimizeResult.Value;
        result.OutputPath.Should().Be(outputPath);

        // Validate the output with Quick profile (QPDF structural validation)
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Quick);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Optimized PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "optimized PDF should pass structural validation");
    }

    [Fact]
    public async Task OptimizePdfWithLinearization_ProducesValidOutput()
    {
        // Arrange
        var sourcePath = "tests/Fixtures/sample-with-text.pdf";
        var outputPath = GetTempOutputPath("optimized-linearized.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = true,
            DeduplicateResources = true,
            Linearize = true
        };

        // Act
        var optimizeResult = await _editingService.OptimizeAsync(
            sourcePath,
            outputPath,
            options);

        // Assert - Optimize should succeed
        optimizeResult.IsSuccess.Should().BeTrue($"optimize operation should succeed: {string.Join(", ", optimizeResult.Errors)}");

        var result = optimizeResult.Value;
        result.WasLinearized.Should().BeTrue("PDF should be linearized");

        // Validate with Standard profile (QPDF + JHOVE)
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Standard);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Linearized PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "linearized PDF should pass format validation");
        report.JhoveResult.Should().NotBeNull("JHOVE validation should be included in Standard profile");
    }

    [Fact]
    public async Task OptimizePdfCompressOnly_ProducesValidOutput()
    {
        // Arrange
        var sourcePath = "tests/Fixtures/bookmarked.pdf";
        var outputPath = GetTempOutputPath("optimized-compress.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = false,
            DeduplicateResources = false,
            Linearize = false
        };

        // Act
        var optimizeResult = await _editingService.OptimizeAsync(
            sourcePath,
            outputPath,
            options);

        // Assert - Optimize should succeed
        optimizeResult.IsSuccess.Should().BeTrue($"optimize operation should succeed: {string.Join(", ", optimizeResult.Errors)}");

        // Validate the output
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Quick);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Compressed PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "compressed PDF should maintain valid structure");
    }

    [Fact]
    public async Task OptimizePdfFullOptions_ProducesValidOutput()
    {
        // Arrange
        var sourcePath = "tests/Fixtures/sample-form.pdf";
        var outputPath = GetTempOutputPath("optimized-full.pdf");
        var options = new OptimizationOptions
        {
            CompressStreams = true,
            RemoveUnusedObjects = true,
            DeduplicateResources = true,
            Linearize = true,
            PreserveEncryption = true
        };

        // Act
        var optimizeResult = await _editingService.OptimizeAsync(
            sourcePath,
            outputPath,
            options);

        // Assert - Optimize should succeed
        optimizeResult.IsSuccess.Should().BeTrue($"optimize operation should succeed: {string.Join(", ", optimizeResult.Errors)}");

        var result = optimizeResult.Value;
        result.OptimizedSize.Should().BeLessThanOrEqualTo(result.OriginalSize, "optimized file should not be larger than original");

        // Validate the output with Standard profile
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Standard);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Fully optimized PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "fully optimized PDF should pass validation");
    }

    private string GetTempOutputPath(string filename)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"fluentpdf-test-{Guid.NewGuid():N}-{filename}");
        _tempFiles.Add(tempPath);
        return tempPath;
    }

    private static string FormatValidationErrors(ValidationReport report)
    {
        var errors = new List<string>
        {
            $"Overall Status: {report.OverallStatus}",
            $"File: {report.FilePath}",
            $"Profile: {report.Profile}",
            ""
        };

        if (report.QpdfResult != null)
        {
            errors.Add($"QPDF Status: {report.QpdfResult.Status}");
            if (report.QpdfResult.Errors.Count > 0)
            {
                errors.Add("QPDF Errors:");
                errors.AddRange(report.QpdfResult.Errors.Select(e => $"  - {e}"));
            }
            errors.Add("");
        }

        if (report.JhoveResult != null)
        {
            errors.Add($"JHOVE Format: {report.JhoveResult.Format}");
            errors.Add($"JHOVE Validity: {report.JhoveResult.Validity}");
            if (report.JhoveResult.Messages.Count > 0)
            {
                errors.Add("JHOVE Messages:");
                errors.AddRange(report.JhoveResult.Messages.Select(e => $"  - {e}"));
            }
            errors.Add("");
        }

        if (report.VeraPdfResult != null)
        {
            errors.Add($"VeraPDF Compliant: {report.VeraPdfResult.IsCompliant}");
            errors.Add($"VeraPDF Flavour: {report.VeraPdfResult.Flavour}");
            if (report.VeraPdfResult.Errors.Count > 0)
            {
                errors.Add("VeraPDF Errors:");
                errors.AddRange(report.VeraPdfResult.Errors.Select(e => $"  - {e}"));
            }
        }

        return string.Join(Environment.NewLine, errors);
    }

    public void Dispose()
    {
        // Clean up temporary files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
