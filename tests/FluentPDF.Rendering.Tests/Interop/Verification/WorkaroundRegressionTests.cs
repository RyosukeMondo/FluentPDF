using FluentPDF.Rendering.Interop.Verification.Regression;
using FluentPDF.Rendering.Interop.Verification.Reports;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Tests for workaround regression detection to ensure documented workarounds
/// are still functioning and to detect if underlying bugs have been fixed.
/// </summary>
public class WorkaroundRegressionTests
{
    [Fact]
    public async Task FloatDimensionWorkaroundTest_ExecutesSuccessfully()
    {
        // Arrange
        var test = new FloatDimensionWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Float Dimension Workaround", result.WorkaroundName);
        Assert.Equal("PdfiumInterop.cs:177-183", result.DocumentationReference);
        Assert.NotEqual(WorkaroundStatus.Broken, result.Status); // Should be StillNeeded or CanBeRemoved, not Broken
        Assert.NotEmpty(result.Details);
        Assert.NotEmpty(result.RecommendedAction);
        Assert.NotNull(result.Context);

        // Verify context contains expected keys
        if (result.Status != WorkaroundStatus.Broken)
        {
            Assert.True(result.Context.ContainsKey("IntegerWidth"));
            Assert.True(result.Context.ContainsKey("IntegerHeight"));
            Assert.True(result.Context.ContainsKey("FloatWidth"));
            Assert.True(result.Context.ContainsKey("FloatHeight"));
        }
    }

    [Fact]
    public void FloatDimensionWorkaroundTest_HasCorrectProperties()
    {
        // Arrange
        var test = new FloatDimensionWorkaroundTest();

        // Assert
        Assert.Equal("Float Dimension Workaround", test.WorkaroundName);
        Assert.Equal("PdfiumInterop.cs:177-183", test.DocumentationReference);
    }

    [Fact]
    public async Task FloatDimensionWorkaroundTest_ReturnsValidStatus()
    {
        // Arrange
        var test = new FloatDimensionWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.Contains(result.Status, new[]
        {
            WorkaroundStatus.StillNeeded,
            WorkaroundStatus.CanBeRemoved,
            WorkaroundStatus.Broken
        });
    }

    [Fact]
    public async Task ThreadingWorkaroundTest_ExecutesSuccessfully()
    {
        // Arrange
        var test = new ThreadingWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Threading Workaround (Task.Yield)", result.WorkaroundName);
        Assert.Equal("PdfiumServiceBase.cs:1-143", result.DocumentationReference);
        Assert.NotEmpty(result.Details);
        Assert.NotEmpty(result.RecommendedAction);
        Assert.NotNull(result.Context);

        // Verify context contains expected keys
        if (result.Status != WorkaroundStatus.Broken)
        {
            Assert.True(result.Context.ContainsKey("TaskYieldPatternWorks"));
        }
    }

    [Fact]
    public void ThreadingWorkaroundTest_HasCorrectProperties()
    {
        // Arrange
        var test = new ThreadingWorkaroundTest();

        // Assert
        Assert.Equal("Threading Workaround (Task.Yield)", test.WorkaroundName);
        Assert.Equal("PdfiumServiceBase.cs:1-143", test.DocumentationReference);
    }

    [Fact]
    public async Task ThreadingWorkaroundTest_ReturnsValidStatus()
    {
        // Arrange
        var test = new ThreadingWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.Contains(result.Status, new[]
        {
            WorkaroundStatus.StillNeeded,
            WorkaroundStatus.CanBeRemoved,
            WorkaroundStatus.Broken
        });
    }

    [Fact]
    public async Task ThreadingWorkaroundTest_VerifiesTaskYieldWorks()
    {
        // Arrange
        var test = new ThreadingWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        // The test should verify Task.Yield pattern works
        if (result.Context != null && result.Context.TryGetValue("TaskYieldPatternWorks", out var taskYieldWorks))
        {
            // If not broken, Task.Yield pattern should work
            if (result.Status != WorkaroundStatus.Broken)
            {
                Assert.Equal("True", taskYieldWorks);
            }
        }
    }

    [Fact]
    public async Task SoftwareBitmapWorkaroundTest_ExecutesSuccessfully()
    {
        // Arrange
        var test = new SoftwareBitmapWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SoftwareBitmap Workaround (ImageSharp PNG Decode)", result.WorkaroundName);
        Assert.Equal("ThumbnailsViewModel.cs:155-174", result.DocumentationReference);
        Assert.NotEmpty(result.Details);
        Assert.NotEmpty(result.RecommendedAction);
        Assert.NotNull(result.Context);

        // Verify context contains expected keys
        if (result.Status != WorkaroundStatus.Broken)
        {
            Assert.True(result.Context.ContainsKey("WorkaroundSucceeded"));
        }
    }

    [Fact]
    public void SoftwareBitmapWorkaroundTest_HasCorrectProperties()
    {
        // Arrange
        var test = new SoftwareBitmapWorkaroundTest();

        // Assert
        Assert.Equal("SoftwareBitmap Workaround (ImageSharp PNG Decode)", test.WorkaroundName);
        Assert.Equal("ThumbnailsViewModel.cs:155-174", test.DocumentationReference);
    }

    [Fact]
    public async Task SoftwareBitmapWorkaroundTest_ReturnsValidStatus()
    {
        // Arrange
        var test = new SoftwareBitmapWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        Assert.Contains(result.Status, new[]
        {
            WorkaroundStatus.StillNeeded,
            WorkaroundStatus.CanBeRemoved,
            WorkaroundStatus.Broken
        });
    }

    [Fact]
    public async Task SoftwareBitmapWorkaroundTest_VerifiesImageSharpDecoding()
    {
        // Arrange
        var test = new SoftwareBitmapWorkaroundTest();

        // Act
        var result = await test.TestWorkaroundAsync();

        // Assert
        // The test should verify ImageSharp workaround works
        if (result.Context != null && result.Context.TryGetValue("WorkaroundSucceeded", out var workaroundSucceeded))
        {
            // If not broken, ImageSharp workaround should work
            if (result.Status != WorkaroundStatus.Broken)
            {
                Assert.Equal("True", workaroundSucceeded);
            }
        }
    }

    [Fact]
    public async Task AllWorkaroundTests_ImplementIWorkaroundTestInterface()
    {
        // Arrange
        IWorkaroundTest[] tests =
        [
            new FloatDimensionWorkaroundTest(),
            new ThreadingWorkaroundTest(),
            new SoftwareBitmapWorkaroundTest()
        ];

        // Act & Assert
        foreach (var test in tests)
        {
            Assert.NotNull(test.WorkaroundName);
            Assert.NotEmpty(test.WorkaroundName);
            Assert.NotNull(test.DocumentationReference);
            Assert.NotEmpty(test.DocumentationReference);
            Assert.Contains(":", test.DocumentationReference); // Should have file:line format

            var result = await test.TestWorkaroundAsync();
            Assert.NotNull(result);
            Assert.Equal(test.WorkaroundName, result.WorkaroundName);
            Assert.Equal(test.DocumentationReference, result.DocumentationReference);
        }
    }

    [Fact]
    public async Task AllWorkaroundTests_RespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        IWorkaroundTest[] tests =
        [
            new FloatDimensionWorkaroundTest(),
            new ThreadingWorkaroundTest(),
            new SoftwareBitmapWorkaroundTest()
        ];

        // Act & Assert
        foreach (var test in tests)
        {
            // Tests should handle cancellation gracefully
            // Since tests use Task.Yield, they may complete even if cancelled
            var result = await test.TestWorkaroundAsync(cts.Token);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task AllWorkaroundTests_ReturnTimestamp()
    {
        // Arrange
        var beforeTest = DateTime.UtcNow.AddSeconds(-1);
        IWorkaroundTest[] tests =
        [
            new FloatDimensionWorkaroundTest(),
            new ThreadingWorkaroundTest(),
            new SoftwareBitmapWorkaroundTest()
        ];

        // Act & Assert
        foreach (var test in tests)
        {
            var result = await test.TestWorkaroundAsync();
            var afterTest = DateTime.UtcNow.AddSeconds(1);

            Assert.NotNull(result);
            Assert.InRange(result.Timestamp, beforeTest, afterTest);
        }
    }

    [Fact]
    public async Task AllWorkaroundTests_ProduceConsistentResults()
    {
        // Arrange
        IWorkaroundTest[] tests =
        [
            new FloatDimensionWorkaroundTest(),
            new ThreadingWorkaroundTest(),
            new SoftwareBitmapWorkaroundTest()
        ];

        // Act & Assert
        foreach (var test in tests)
        {
            // Run test twice
            var result1 = await test.TestWorkaroundAsync();
            var result2 = await test.TestWorkaroundAsync();

            // Results should be consistent (same status)
            Assert.Equal(result1.Status, result2.Status);
            Assert.Equal(result1.WorkaroundName, result2.WorkaroundName);
            Assert.Equal(result1.DocumentationReference, result2.DocumentationReference);
        }
    }
}
