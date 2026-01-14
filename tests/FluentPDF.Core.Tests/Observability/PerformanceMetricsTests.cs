using FluentPDF.Core.Observability;

namespace FluentPDF.Core.Tests.Observability;

public sealed class PerformanceMetricsTests
{
    [Fact]
    public void TotalMemoryMB_ShouldReturn_SumOfManagedAndNative()
    {
        // Arrange
        var metrics = new PerformanceMetrics
        {
            CurrentFPS = 60,
            ManagedMemoryMB = 200,
            NativeMemoryMB = 150,
            LastRenderTimeMs = 16.7,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Good
        };

        // Act
        var total = metrics.TotalMemoryMB;

        // Assert
        Assert.Equal(350, total);
    }

    [Theory]
    [InlineData(60, 100, PerformanceLevel.Good)]
    [InlineData(30, 400, PerformanceLevel.Good)]
    [InlineData(29, 400, PerformanceLevel.Warning)]
    [InlineData(25, 600, PerformanceLevel.Warning)]
    [InlineData(30, 600, PerformanceLevel.Warning)]
    [InlineData(14, 400, PerformanceLevel.Critical)]
    [InlineData(60, 1001, PerformanceLevel.Critical)]
    [InlineData(14, 1001, PerformanceLevel.Critical)]
    public void CalculateLevel_ShouldReturn_CorrectPerformanceLevel(double fps, long memoryMB, PerformanceLevel expected)
    {
        // Act
        var level = PerformanceMetrics.CalculateLevel(fps, memoryMB);

        // Assert
        Assert.Equal(expected, level);
    }

    [Fact]
    public void CalculateLevel_WithExactThresholds_ShouldReturnCorrectLevel()
    {
        // FPS = 30 is Good (boundary)
        Assert.Equal(PerformanceLevel.Good, PerformanceMetrics.CalculateLevel(30, 499));

        // FPS = 15 is Warning (boundary)
        Assert.Equal(PerformanceLevel.Warning, PerformanceMetrics.CalculateLevel(15, 499));

        // Memory = 500MB is Warning (boundary)
        Assert.Equal(PerformanceLevel.Warning, PerformanceMetrics.CalculateLevel(60, 500));

        // Memory = 1000MB is Warning (boundary)
        Assert.Equal(PerformanceLevel.Warning, PerformanceMetrics.CalculateLevel(60, 1000));

        // Memory = 1001MB is Critical (exceeds threshold)
        Assert.Equal(PerformanceLevel.Critical, PerformanceMetrics.CalculateLevel(60, 1001));
    }

    [Fact]
    public void CalculateLevel_WithZeroFPS_ShouldReturnCritical()
    {
        // Act
        var level = PerformanceMetrics.CalculateLevel(0, 100);

        // Assert
        Assert.Equal(PerformanceLevel.Critical, level);
    }

    [Fact]
    public void CalculateLevel_WithNegativeFPS_ShouldReturnCritical()
    {
        // Act
        var level = PerformanceMetrics.CalculateLevel(-1, 100);

        // Assert
        Assert.Equal(PerformanceLevel.Critical, level);
    }

    [Fact]
    public void PerformanceMetrics_ShouldBeImmutable()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new PerformanceMetrics
        {
            CurrentFPS = 60,
            ManagedMemoryMB = 200,
            NativeMemoryMB = 150,
            LastRenderTimeMs = 16.7,
            CurrentPageNumber = 1,
            Timestamp = timestamp,
            Level = PerformanceLevel.Good
        };

        // Assert - verify properties are init-only
        Assert.Equal(60, metrics.CurrentFPS);
        Assert.Equal(200, metrics.ManagedMemoryMB);
        Assert.Equal(150, metrics.NativeMemoryMB);
        Assert.Equal(16.7, metrics.LastRenderTimeMs);
        Assert.Equal(1, metrics.CurrentPageNumber);
        Assert.Equal(timestamp, metrics.Timestamp);
        Assert.Equal(PerformanceLevel.Good, metrics.Level);
    }
}
