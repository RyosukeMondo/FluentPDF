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
/// Integration tests validating PDF merge operations produce valid output.
/// Tests require validation tools installed (QPDF, JHOVE).
/// </summary>
[Trait("Category", "Integration")]
public class MergeValidationTests : IDisposable
{
    private readonly IDocumentEditingService _editingService;
    private readonly IPdfValidationService _validationService;
    private readonly List<string> _tempFiles = [];

    public MergeValidationTests()
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
    public async Task MergeTwoValidPdfs_ProducesValidOutput()
    {
        // Arrange
        var source1 = "tests/Fixtures/sample.pdf";
        var source2 = "tests/Fixtures/sample-with-text.pdf";
        var outputPath = GetTempOutputPath("merged.pdf");

        // Act
        var mergeResult = await _editingService.MergeAsync(
            [source1, source2],
            outputPath);

        // Assert - Merge should succeed
        mergeResult.IsSuccess.Should().BeTrue($"merge operation should succeed: {string.Join(", ", mergeResult.Errors)}");
        File.Exists(outputPath).Should().BeTrue("merged PDF should be created");

        // Validate the output with Quick profile (QPDF structural validation)
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Quick);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Merged PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "merged PDF should pass structural validation");
    }

    [Fact]
    public async Task MergeMultiplePdfs_ProducesValidOutput()
    {
        // Arrange
        var sources = new[]
        {
            "tests/Fixtures/sample.pdf",
            "tests/Fixtures/sample-with-text.pdf",
            "tests/Fixtures/no-bookmarks.pdf"
        };
        var outputPath = GetTempOutputPath("merged-multiple.pdf");

        // Act
        var mergeResult = await _editingService.MergeAsync(sources, outputPath);

        // Assert - Merge should succeed
        mergeResult.IsSuccess.Should().BeTrue($"merge operation should succeed: {string.Join(", ", mergeResult.Errors)}");

        // Validate with Standard profile (QPDF + JHOVE)
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Standard);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Merged PDF failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "merged PDF should pass format validation");
        report.JhoveResult.Should().NotBeNull("JHOVE validation should be included in Standard profile");
    }

    [Fact]
    public async Task MergePdfsWithBookmarks_ProducesValidOutput()
    {
        // Arrange
        var sources = new[]
        {
            "tests/Fixtures/bookmarked.pdf",
            "tests/Fixtures/flat-bookmarks.pdf"
        };
        var outputPath = GetTempOutputPath("merged-bookmarks.pdf");

        // Act
        var mergeResult = await _editingService.MergeAsync(sources, outputPath);

        // Assert - Merge should succeed
        mergeResult.IsSuccess.Should().BeTrue($"merge operation should succeed: {string.Join(", ", mergeResult.Errors)}");

        // Validate the output
        var validationResult = await _validationService.ValidateAsync(outputPath, ValidationProfile.Quick);

        validationResult.IsSuccess.Should().BeTrue("validation service should execute");
        var report = validationResult.Value;

        if (report.OverallStatus != ValidationStatus.Pass)
        {
            var errorDetails = FormatValidationErrors(report);
            Assert.Fail($"Merged PDF with bookmarks failed validation:\n{errorDetails}");
        }

        report.OverallStatus.Should().Be(ValidationStatus.Pass, "merged PDF with bookmarks should maintain valid structure");
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
