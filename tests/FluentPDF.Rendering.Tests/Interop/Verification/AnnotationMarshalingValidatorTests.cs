using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using FluentPDF.Rendering.Interop.Verification.Reports;
using FluentPDF.Rendering.Interop.Verification.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Unit tests for AnnotationMarshalingValidator.
/// </summary>
public sealed class AnnotationMarshalingValidatorTests
{
    private readonly AnnotationMarshalingValidator _validator;

    public AnnotationMarshalingValidatorTests()
    {
        var logger = NullLogger<AnnotationMarshalingValidator>.Instance;
        _validator = new AnnotationMarshalingValidator(logger);
    }

    [Fact]
    public void ValidatorName_ShouldReturnCorrectName()
    {
        // Arrange & Act
        var name = _validator.ValidatorName;

        // Assert
        Assert.Equal("Annotation Marshaling Validator", name);
    }

    [Fact]
    public void TargetArea_ShouldReturnCorrectArea()
    {
        // Arrange & Act
        var area = _validator.TargetArea;

        // Assert
        Assert.Equal("Annotation Marshaling", area);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValidationReport()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal("Annotation Marshaling Validator", report.ValidatorName);
        Assert.Equal("Annotation Marshaling", report.TargetArea);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.ResultsByArea);
        Assert.True(report.Summary.TotalTests > 0, "Should have executed tests");
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeQuadPointsResults()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.True(report.ResultsByArea.ContainsKey("Quad Points Marshaling"));
        var results = report.ResultsByArea["Quad Points Marshaling"];
        Assert.NotEmpty(results);

        // Verify all edge cases are tested
        Assert.Contains(results, r => r.TestName.Contains("Complete quad points array"));
        Assert.Contains(results, r => r.TestName.Contains("Partial quad points array"));
        Assert.Contains(results, r => r.TestName.Contains("Empty quad points array"));
        Assert.Contains(results, r => r.TestName.Contains("NaN values"));
        Assert.Contains(results, r => r.TestName.Contains("infinity values"));
        Assert.Contains(results, r => r.TestName.Contains("very large values"));
        Assert.Contains(results, r => r.TestName.Contains("negative values"));
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeRectResults()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        Assert.True(report.ResultsByArea.ContainsKey("Rectangle Marshaling"));
        var results = report.ResultsByArea["Rectangle Marshaling"];
        Assert.NotEmpty(results);

        // Verify all edge cases are tested
        Assert.Contains(results, r => r.TestName.Contains("Normal rectangle"));
        Assert.Contains(results, r => r.TestName.Contains("Inverted rectangle (left > right)"));
        Assert.Contains(results, r => r.TestName.Contains("Inverted rectangle (bottom > top)"));
        Assert.Contains(results, r => r.TestName.Contains("Zero-size rectangle"));
        Assert.Contains(results, r => r.TestName.Contains("negative coordinates"));
        Assert.Contains(results, r => r.TestName.Contains("NaN values"));
        Assert.Contains(results, r => r.TestName.Contains("infinity values"));
        Assert.Contains(results, r => r.TestName.Contains("very large coordinates"));
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_CompleteArray_ShouldPass()
    {
        // Arrange
        var completeQuadPoints = new[] { 100f, 200f, 300f, 200f, 300f, 100f, 100f, 100f };

        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var completeTest = results.FirstOrDefault(r => r.TestName.Contains("Complete quad points array"));
        Assert.NotNull(completeTest);
        Assert.True(completeTest.Passed, "Complete quad points array should pass");
        Assert.Equal(ValidationSeverity.Info, completeTest.Severity);
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_PartialArray_ShouldHandleGracefully()
    {
        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var partialTest = results.FirstOrDefault(r => r.TestName.Contains("Partial quad points array"));
        Assert.NotNull(partialTest);
        // Should not throw exception, should handle gracefully
        Assert.NotNull(partialTest.Message);
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_NaNValues_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var nanTest = results.FirstOrDefault(r => r.TestName.Contains("NaN values"));
        Assert.NotNull(nanTest);
        Assert.False(nanTest.Passed, "NaN values should be detected");
        Assert.Equal(ValidationSeverity.Warning, nanTest.Severity);
        Assert.Contains("NaN", nanTest.Message);
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_InfinityValues_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var infinityTest = results.FirstOrDefault(r => r.TestName.Contains("infinity values"));
        Assert.NotNull(infinityTest);
        Assert.False(infinityTest.Passed, "Infinity values should be detected");
        Assert.Equal(ValidationSeverity.Warning, infinityTest.Severity);
        Assert.Contains("infinity", infinityTest.Message);
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_LargeValues_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var largeTest = results.FirstOrDefault(r => r.TestName.Contains("very large values"));
        Assert.NotNull(largeTest);
        Assert.True(largeTest.Passed, "Large coordinate values should be handled");
    }

    [Fact]
    public async Task ValidateQuadPointsMarshalingAsync_NegativeValues_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateQuadPointsMarshalingAsync();

        // Assert
        var negativeTest = results.FirstOrDefault(r => r.TestName.Contains("negative values"));
        Assert.NotNull(negativeTest);
        Assert.True(negativeTest.Passed, "Negative coordinate values should be handled");
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_NormalRect_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var normalTest = results.FirstOrDefault(r => r.TestName.Contains("Normal rectangle"));
        Assert.NotNull(normalTest);
        Assert.True(normalTest.Passed, "Normal rectangle should pass");
        Assert.Equal(ValidationSeverity.Info, normalTest.Severity);
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_InvertedX_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var invertedTest = results.FirstOrDefault(r => r.TestName.Contains("Inverted rectangle (left > right)"));
        Assert.NotNull(invertedTest);
        Assert.False(invertedTest.Passed, "Inverted coordinates should be detected");
        Assert.Equal(ValidationSeverity.Warning, invertedTest.Severity);
        Assert.Contains("inverted", invertedTest.Message?.ToLower() ?? string.Empty);
        Assert.Contains("swap left/right", invertedTest.SuggestedFix ?? string.Empty);
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_InvertedY_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var invertedTest = results.FirstOrDefault(r => r.TestName.Contains("Inverted rectangle (bottom > top)"));
        Assert.NotNull(invertedTest);
        Assert.False(invertedTest.Passed, "Inverted coordinates should be detected");
        Assert.Equal(ValidationSeverity.Warning, invertedTest.Severity);
        Assert.Contains("inverted", invertedTest.Message?.ToLower() ?? string.Empty);
        Assert.Contains("swap bottom/top", invertedTest.SuggestedFix ?? string.Empty);
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_ZeroSize_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var zeroSizeTest = results.FirstOrDefault(r => r.TestName.Contains("Zero-size rectangle"));
        Assert.NotNull(zeroSizeTest);
        Assert.True(zeroSizeTest.Passed, "Zero-size rectangle should be handled");
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_NegativeCoordinates_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var negativeTest = results.FirstOrDefault(r => r.TestName.Contains("negative coordinates"));
        Assert.NotNull(negativeTest);
        Assert.True(negativeTest.Passed, "Negative coordinates should be handled");
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_NaNValues_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var nanTest = results.FirstOrDefault(r => r.TestName.Contains("NaN values"));
        Assert.NotNull(nanTest);
        Assert.False(nanTest.Passed, "NaN values should be detected");
        Assert.Equal(ValidationSeverity.Warning, nanTest.Severity);
        Assert.Contains("NaN", nanTest.Message);
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_InfinityValues_ShouldDetect()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var infinityTest = results.FirstOrDefault(r => r.TestName.Contains("infinity values"));
        Assert.NotNull(infinityTest);
        Assert.False(infinityTest.Passed, "Infinity values should be detected");
        Assert.Equal(ValidationSeverity.Warning, infinityTest.Severity);
        Assert.Contains("infinity", infinityTest.Message);
    }

    [Fact]
    public async Task ValidateRectMarshalingAsync_LargeCoordinates_ShouldPass()
    {
        // Act
        var results = await _validator.ValidateRectMarshalingAsync();

        // Assert
        var largeTest = results.FirstOrDefault(r => r.TestName.Contains("very large coordinates"));
        Assert.NotNull(largeTest);
        Assert.True(largeTest.Passed, "Large coordinates should be handled");
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeContextInformation()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        foreach (var area in report.ResultsByArea.Values)
        {
            foreach (var result in area)
            {
                Assert.NotNull(result.Context);
                Assert.NotEmpty(result.Context);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldProvideSuggestedFixesForFailures()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        var failedResults = report.ResultsByArea.Values
            .SelectMany(results => results)
            .Where(r => !r.Passed);

        foreach (var result in failedResults)
        {
            Assert.NotNull(result.SuggestedFix);
            Assert.NotEmpty(result.SuggestedFix);
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldCalculateSummaryCorrectly()
    {
        // Act
        var report = await _validator.ValidateAsync();

        // Assert
        var totalResults = report.ResultsByArea.Values.SelectMany(r => r).ToList();
        Assert.Equal(totalResults.Count, report.Summary.TotalTests);
        Assert.Equal(totalResults.Count(r => r.Passed), report.Summary.PassedCount);
        Assert.Equal(
            totalResults.Count(r => !r.Passed && r.Severity != ValidationSeverity.Warning),
            report.Summary.FailedCount);
        Assert.Equal(
            totalResults.Count(r => r.Severity == ValidationSeverity.Warning),
            report.Summary.WarningCount);
        Assert.Equal(
            totalResults.Count(r => r.Severity == ValidationSeverity.Critical),
            report.Summary.CriticalCount);
    }
}
