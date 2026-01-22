using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification.Reports;
using FluentPDF.Rendering.Interop.Verification.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Unit tests for BitmapMarshalingValidator.
/// </summary>
public sealed class BitmapMarshalingValidatorTests
{
    private readonly BitmapMarshalingValidator _validator;

    public BitmapMarshalingValidatorTests()
    {
        var logger = NullLogger<BitmapMarshalingValidator>.Instance;
        _validator = new BitmapMarshalingValidator(logger);
    }

    [Fact]
    public void ValidatorName_ShouldReturnCorrectName()
    {
        // Arrange & Act
        var name = _validator.ValidatorName;

        // Assert
        Assert.Equal("Bitmap Marshaling Validator", name);
    }

    [Fact]
    public void TargetArea_ShouldReturnCorrectArea()
    {
        // Arrange & Act
        var area = _validator.TargetArea;

        // Assert
        Assert.Equal("Bitmap Marshaling", area);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValidationReport()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            Assert.NotNull(report);
            Assert.Equal("Bitmap Marshaling Validator", report.ValidatorName);
            Assert.Equal("Bitmap Marshaling", report.TargetArea);
            Assert.NotNull(report.Summary);
            Assert.NotNull(report.ResultsByArea);
            Assert.True(report.Summary.TotalTests > 0, "Should have executed tests");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeStrideCalculationResults()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            Assert.True(report.ResultsByArea.ContainsKey("Stride Calculation"));
            var results = report.ResultsByArea["Stride Calculation"];
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.TestName.Contains("1x1 pixel"));
            Assert.Contains(results, r => r.TestName.Contains("8192x8192 pixel"));
            Assert.Contains(results, r => r.TestName.Contains("1920x1080 pixel"));
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeMarshalCopyResults()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            Assert.True(report.ResultsByArea.ContainsKey("Marshal.Copy Operations"));
            var results = report.ResultsByArea["Marshal.Copy Operations"];
            Assert.NotEmpty(results);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludePixelDataIntegrityResults()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            Assert.True(report.ResultsByArea.ContainsKey("Pixel Data Integrity"));
            var results = report.ResultsByArea["Pixel Data Integrity"];
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.TestName.Contains("red color"));
            Assert.Contains(results, r => r.TestName.Contains("green color"));
            Assert.Contains(results, r => r.TestName.Contains("blue color"));
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldTestMinimumSize()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var minSizeTest = results.FirstOrDefault(r => r.TestName.Contains("1x1 pixel"));
            Assert.NotNull(minSizeTest);
            Assert.True(minSizeTest.Passed, $"1x1 pixel test should pass: {minSizeTest.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldTestLargeDimensions()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var largeTest = results.FirstOrDefault(r => r.TestName.Contains("8192x8192 pixel"));
            Assert.NotNull(largeTest);
            // Note: This test may fail on systems with insufficient memory, but should not crash
            Assert.NotNull(largeTest.Message);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldTestNonPowerOf2Dimensions()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var nonPowerOf2Test = results.FirstOrDefault(r => r.TestName.Contains("1920x1080 pixel"));
            Assert.NotNull(nonPowerOf2Test);
            Assert.True(nonPowerOf2Test.Passed, $"1920x1080 test should pass: {nonPowerOf2Test.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldHandleZeroDimensions()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var zeroTest = results.FirstOrDefault(r => r.TestName.Contains("0x0 pixel"));
            Assert.NotNull(zeroTest);
            Assert.True(zeroTest.Passed, "Should handle zero dimensions gracefully");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldHandleNegativeDimensions()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var negativeTest = results.FirstOrDefault(r => r.TestName.Contains("-1x-1 pixel"));
            Assert.NotNull(negativeTest);
            Assert.True(negativeTest.Passed, "Should handle negative dimensions gracefully");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void ValidateStrideCalculation_ShouldDetectOverflow()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = _validator.ValidateStrideCalculation();

            // Assert
            var overflowTest = results.FirstOrDefault(r => r.TestName.Contains("Overflow detection"));
            Assert.NotNull(overflowTest);
            // Should either detect overflow or PDFium should refuse to create bitmap
            Assert.NotNull(overflowTest.Message);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateMarshalCopyAsync_ShouldTestEdgeCases()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidateMarshalCopyAsync();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, r => Assert.NotNull(r.TestName));
            Assert.All(results, r => Assert.NotNull(r.Message));
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateMarshalCopyAsync_ShouldCopy1x1Bitmap()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidateMarshalCopyAsync();

            // Assert
            var test1x1 = results.FirstOrDefault(r => r.TestName.Contains("1x1 pixel bitmap"));
            Assert.NotNull(test1x1);
            Assert.True(test1x1.Passed, $"1x1 pixel Marshal.Copy should succeed: {test1x1.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateMarshalCopyAsync_ShouldCopyCommonResolution()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidateMarshalCopyAsync();

            // Assert
            var test1920x1080 = results.FirstOrDefault(r => r.TestName.Contains("1920x1080 pixel bitmap"));
            Assert.NotNull(test1920x1080);
            Assert.True(test1920x1080.Passed, $"1920x1080 Marshal.Copy should succeed: {test1920x1080.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidatePixelDataIntegrityAsync_ShouldVerifyBGRA32Format()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidatePixelDataIntegrityAsync();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, r => Assert.NotNull(r.TestName));

            // Verify at least one color test passed
            var passedTests = results.Where(r => r.Passed).ToList();
            Assert.NotEmpty(passedTests);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidatePixelDataIntegrityAsync_ShouldVerifyRedColor()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidatePixelDataIntegrityAsync();

            // Assert
            var redTest = results.FirstOrDefault(r => r.TestName.Contains("red color"));
            Assert.NotNull(redTest);
            Assert.True(redTest.Passed, $"Red color pixel data should be correct: {redTest.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidatePixelDataIntegrityAsync_ShouldVerifyWhiteColor()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidatePixelDataIntegrityAsync();

            // Assert
            var whiteTest = results.FirstOrDefault(r => r.TestName.Contains("white color"));
            Assert.NotNull(whiteTest);
            Assert.True(whiteTest.Passed, $"White color pixel data should be correct: {whiteTest.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidatePixelDataIntegrityAsync_ShouldVerifyBlackColor()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var results = await _validator.ValidatePixelDataIntegrityAsync();

            // Assert
            var blackTest = results.FirstOrDefault(r => r.TestName.Contains("black color"));
            Assert.NotNull(blackTest);
            Assert.True(blackTest.Passed, $"Black color pixel data should be correct: {blackTest.Message}");
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldCalculateSummaryCorrectly()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            var summary = report.Summary;
            Assert.Equal(
                summary.PassedCount + summary.FailedCount + summary.WarningCount,
                summary.TotalTests);

            // PassPercentage should be calculated correctly
            var expectedPercentage = summary.TotalTests > 0
                ? (double)summary.PassedCount / summary.TotalTests * 100
                : 0;
            Assert.Equal(expectedPercentage, summary.PassPercentage, precision: 2);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeContextInformation()
    {
        // Arrange
        PdfiumInterop.Initialize();

        try
        {
            // Act
            var report = await _validator.ValidateAsync();

            // Assert
            var allResults = report.ResultsByArea.Values.SelectMany(r => r).ToList();
            Assert.NotEmpty(allResults);

            // At least some results should have context information
            var resultsWithContext = allResults.Where(r => r.Context != null && r.Context.Any()).ToList();
            Assert.NotEmpty(resultsWithContext);
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }
    }
}
