using FluentAssertions;
using FluentPDF.Rendering.Tests.Models;

namespace FluentPDF.Rendering.Tests.Models;

public class ComparisonResultTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange & Act
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            DifferencePath = "/path/to/diff.png",
            ComparedAt = DateTime.UtcNow
        };

        // Assert
        result.SsimScore.Should().Be(0.95);
        result.Passed.Should().BeTrue();
        result.Threshold.Should().Be(0.90);
        result.BaselinePath.Should().Be("/path/to/baseline.png");
        result.ActualPath.Should().Be("/path/to/actual.png");
        result.DifferencePath.Should().Be("/path/to/diff.png");
        result.ComparedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithoutDifferencePath_ShouldCreateInstance()
    {
        // Arrange & Act
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            DifferencePath = null,
            ComparedAt = DateTime.UtcNow
        };

        // Assert
        result.DifferencePath.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidPassedResult_ShouldNotThrow()
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithValidFailedResult_ShouldNotThrow()
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.85,
            Passed = false,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidSsimScore_ShouldThrow(double invalidScore)
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = invalidScore,
            Passed = false,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(ComparisonResult.SsimScore));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Validate_WithInvalidThreshold_ShouldThrow(double invalidThreshold)
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = invalidThreshold,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(ComparisonResult.Threshold));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidBaselinePath_ShouldThrow(string? invalidPath)
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = invalidPath!,
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ComparisonResult.BaselinePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidActualPath_ShouldThrow(string? invalidPath)
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = invalidPath!,
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<ArgumentException>()
            .WithParameterName(nameof(ComparisonResult.ActualPath));
    }

    [Fact]
    public void Validate_WithPassedButScoreBelowThreshold_ShouldThrow()
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.85,
            Passed = true, // Inconsistent: marked as passed but score is below threshold
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*passed*below threshold*");
    }

    [Fact]
    public void Validate_WithFailedButScoreAboveThreshold_ShouldThrow()
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.95,
            Passed = false, // Inconsistent: marked as failed but score meets threshold
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*failed*meets or exceeds threshold*");
    }

    [Fact]
    public void Validate_WithScoreEqualToThreshold_ShouldPassIfMarkedAsPassed()
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = 0.90,
            Passed = true,
            Threshold = 0.90,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0.0, 0.0, true)]   // Perfect match at zero
    [InlineData(1.0, 1.0, true)]   // Perfect match at one
    [InlineData(0.5, 0.5, true)]   // Exact threshold match
    [InlineData(0.999, 0.95, true)] // High similarity
    [InlineData(0.0, 0.95, false)]  // Complete difference
    public void Validate_WithVariousScenarios_ShouldValidateCorrectly(
        double score, double threshold, bool passed)
    {
        // Arrange
        var result = new ComparisonResult
        {
            SsimScore = score,
            Passed = passed,
            Threshold = threshold,
            BaselinePath = "/path/to/baseline.png",
            ActualPath = "/path/to/actual.png",
            ComparedAt = DateTime.UtcNow
        };

        // Act & Assert
        result.Invoking(r => r.Validate()).Should().NotThrow();
    }
}
