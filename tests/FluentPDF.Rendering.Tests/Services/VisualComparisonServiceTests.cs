using FluentAssertions;
using FluentPDF.Rendering.Tests.Models;
using FluentPDF.Rendering.Tests.Services;
using OpenCvSharp;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class VisualComparisonServiceTests : IDisposable
{
    private readonly VisualComparisonService _service;
    private readonly string _testOutputDir;
    private readonly List<string> _tempFiles;

    public VisualComparisonServiceTests()
    {
        _service = new VisualComparisonService();
        _testOutputDir = Path.Combine(Path.GetTempPath(), "FluentPDF.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);
        _tempFiles = new List<string>();
    }

    [Fact]
    public async Task CompareImagesAsync_IdenticalImages_ReturnsPassedWithHighSsim()
    {
        // Arrange
        var image1Path = CreateTestImage("identical1.png", 100, 100, new Scalar(128, 128, 128));
        var image2Path = CreateTestImage("identical2.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(image1Path, image2Path, diffPath, 0.95);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.SsimScore.Should().BeGreaterThan(0.99);
        comparison.Passed.Should().BeTrue();
        comparison.Threshold.Should().Be(0.95);
        comparison.BaselinePath.Should().Be(image1Path);
        comparison.ActualPath.Should().Be(image2Path);
        comparison.DifferencePath.Should().Be(diffPath);
        File.Exists(diffPath).Should().BeTrue();
    }

    [Fact]
    public async Task CompareImagesAsync_DifferentImages_ReturnsFailedWithLowSsim()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(0, 0, 0));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(255, 255, 255));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.SsimScore.Should().BeLessThan(0.5);
        comparison.Passed.Should().BeFalse();
        comparison.Threshold.Should().Be(0.95);
        File.Exists(diffPath).Should().BeTrue();
    }

    [Fact]
    public async Task CompareImagesAsync_SlightlyDifferentImages_DetectsDifference()
    {
        // Arrange - Create two images with slight difference
        var baselinePath = CreateTestImageWithSquare("baseline_square.png", 100, 100, 10, 10, 20, 20);
        var actualPath = CreateTestImageWithSquare("actual_square.png", 100, 100, 15, 15, 25, 25);
        var diffPath = GetTempFilePath("diff_square.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.SsimScore.Should().BeGreaterThan(0.7).And.BeLessThan(0.99);
        File.Exists(diffPath).Should().BeTrue();
    }

    [Fact]
    public async Task CompareImagesAsync_DifferentSizes_ReturnsFailureWithSizeMismatch()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline_100.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual_200.png", 200, 200, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("size mismatch"));
    }

    [Fact]
    public async Task CompareImagesAsync_BaselineNotFound_ReturnsFailure()
    {
        // Arrange
        var baselinePath = Path.Combine(_testOutputDir, "nonexistent.png");
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("not found"));
    }

    [Fact]
    public async Task CompareImagesAsync_ActualNotFound_ReturnsFailure()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = Path.Combine(_testOutputDir, "nonexistent.png");
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("not found"));
    }

    [Fact]
    public async Task CompareImagesAsync_EmptyBaselinePath_ReturnsFailure()
    {
        // Arrange
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync("", actualPath, diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Baseline path"));
    }

    [Fact]
    public async Task CompareImagesAsync_EmptyActualPath_ReturnsFailure()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, "", diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Actual path"));
    }

    [Fact]
    public async Task CompareImagesAsync_EmptyDifferencePath_ReturnsFailure()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, "", 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Difference path"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public async Task CompareImagesAsync_InvalidThreshold_ReturnsFailure(double invalidThreshold)
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, invalidThreshold);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Threshold must be between 0.0 and 1.0"));
    }

    [Fact]
    public async Task CompareImagesAsync_CustomThreshold_UsesProvidedValue()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(130, 130, 130)); // Slightly different
        var diffPath = GetTempFilePath("diff.png");
        var customThreshold = 0.80;

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, customThreshold);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;
        comparison.Threshold.Should().Be(customThreshold);
    }

    [Fact]
    public async Task CompareImagesAsync_CreatesDifferenceImageDirectory()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = Path.Combine(_testOutputDir, "subdir", "nested", "diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(diffPath).Should().BeTrue();
        Directory.Exists(Path.GetDirectoryName(diffPath)).Should().BeTrue();
    }

    [Fact]
    public async Task CompareImagesAsync_SetsComparedAtTimestamp()
    {
        // Arrange
        var beforeComparison = DateTime.UtcNow.AddSeconds(-1);
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var afterComparison = DateTime.UtcNow.AddSeconds(1);
        result.Value.ComparedAt.Should().BeAfter(beforeComparison).And.BeBefore(afterComparison);
    }

    [Fact]
    public async Task CompareImagesAsync_CancellationToken_CancelsOperation()
    {
        // Arrange
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("cancelled"));
    }

    [Fact]
    public async Task Dispose_AfterDisposal_ReturnsFailure()
    {
        // Arrange
        var service = new VisualComparisonService();
        var baselinePath = CreateTestImage("baseline.png", 100, 100, new Scalar(128, 128, 128));
        var actualPath = CreateTestImage("actual.png", 100, 100, new Scalar(128, 128, 128));
        var diffPath = GetTempFilePath("diff.png");

        // Act
        service.Dispose();
        var result = await service.CompareImagesAsync(baselinePath, actualPath, diffPath, 0.95);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("disposed"));
    }

    [Fact]
    public void Dispose_MultipleDisposals_DoesNotThrow()
    {
        // Arrange
        var service = new VisualComparisonService();

        // Act & Assert
        service.Dispose();
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    private string CreateTestImage(string filename, int width, int height, Scalar color)
    {
        var path = Path.Combine(_testOutputDir, filename);
        using var image = new Mat(height, width, MatType.CV_8UC3, color);
        Cv2.ImWrite(path, image);
        _tempFiles.Add(path);
        return path;
    }

    private string CreateTestImageWithSquare(string filename, int width, int height, int x, int y, int squareWidth, int squareHeight)
    {
        var path = Path.Combine(_testOutputDir, filename);
        using var image = new Mat(height, width, MatType.CV_8UC3, new Scalar(128, 128, 128));
        Cv2.Rectangle(image, new Point(x, y), new Point(x + squareWidth, y + squareHeight), new Scalar(255, 0, 0), -1);
        Cv2.ImWrite(path, image);
        _tempFiles.Add(path);
        return path;
    }

    private string GetTempFilePath(string filename)
    {
        var path = Path.Combine(_testOutputDir, filename);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        _service?.Dispose();

        // Clean up all temp files
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up test directory
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
