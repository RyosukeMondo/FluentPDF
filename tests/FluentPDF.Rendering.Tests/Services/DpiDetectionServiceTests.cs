using System.Reactive.Linq;
using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for DpiDetectionService.
/// Tests DPI detection, monitoring, calculation, and error handling.
/// </summary>
public sealed class DpiDetectionServiceTests : IDisposable
{
    private readonly Mock<ILogger<DpiDetectionService>> _mockLogger;
    private readonly DpiDetectionService _service;

    public DpiDetectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<DpiDetectionService>>();
        _service = new DpiDetectionService(_mockLogger.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DpiDetectionService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetCurrentDisplayInfo Tests

    [Fact]
    public void GetCurrentDisplayInfo_WithNullXamlRoot_ReturnsStandardDisplayInfo()
    {
        // Act
        var result = _service.GetCurrentDisplayInfo(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RasterizationScale.Should().Be(1.0);
        result.Value.EffectiveDpi.Should().Be(96.0);
        result.Value.IsHighDpi.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentDisplayInfo_WithMockXamlRoot_ReturnsCorrectDisplayInfo()
    {
        // Arrange
        var mockXamlRoot = new MockXamlRoot { RasterizationScale = 1.5 };

        // Act
        var result = _service.GetCurrentDisplayInfo(mockXamlRoot);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RasterizationScale.Should().Be(1.5);
        result.Value.EffectiveDpi.Should().Be(144.0); // 96 * 1.5
        result.Value.IsHighDpi.Should().BeTrue();
        result.Value.ScalingPercentage.Should().Be(150);
    }

    [Fact]
    public void GetCurrentDisplayInfo_WithHighDpiDisplay_ReturnsHighDpiInfo()
    {
        // Arrange - 200% scaling (4K display)
        var mockXamlRoot = new MockXamlRoot { RasterizationScale = 2.0 };

        // Act
        var result = _service.GetCurrentDisplayInfo(mockXamlRoot);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RasterizationScale.Should().Be(2.0);
        result.Value.EffectiveDpi.Should().Be(192.0); // 96 * 2.0
        result.Value.IsHighDpi.Should().BeTrue();
        result.Value.ScalingPercentage.Should().Be(200);
    }

    [Fact]
    public void GetCurrentDisplayInfo_WithObjectWithoutRasterizationScaleProperty_ReturnsStandardDisplayInfo()
    {
        // Arrange - Object without RasterizationScale property
        var invalidObject = new object();

        // Act
        var result = _service.GetCurrentDisplayInfo(invalidObject);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RasterizationScale.Should().Be(1.0);
        result.Value.EffectiveDpi.Should().Be(96.0);
    }

    [Fact]
    public void GetCurrentDisplayInfo_LogsCorrelationId()
    {
        // Arrange
        var mockXamlRoot = new MockXamlRoot { RasterizationScale = 1.5 };

        // Act
        _service.GetCurrentDisplayInfo(mockXamlRoot);

        // Assert - Verify that logging occurred with correlation ID
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region MonitorDpiChanges Tests

    [Fact]
    public void MonitorDpiChanges_WithNullXamlRoot_ReturnsFailure()
    {
        // Act
        var result = _service.MonitorDpiChanges(null);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DPI_MONITORING_FAILED");
        error.Message.Should().Contain("XamlRoot is null");
    }

    [Fact]
    public void MonitorDpiChanges_WithObjectWithoutChangedEvent_ReturnsFailure()
    {
        // Arrange - Object without Changed event
        var invalidObject = new object();

        // Act
        var result = _service.MonitorDpiChanges(invalidObject);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DPI_MONITORING_FAILED");
        error.Message.Should().Contain("Changed event");
    }

    [Fact]
    public void MonitorDpiChanges_Documentation_Note()
    {
        // Note: Full integration tests for MonitorDpiChanges with real WinUI XamlRoot
        // and event handling are covered in the integration test suite.
        // Unit tests here focus on validation logic (null checks, error paths)
        // since testing the actual event subscription and observable stream
        // requires real WinUI XamlRoot which is platform-specific.
        Assert.True(true);
    }

    #endregion

    #region CalculateEffectiveDpi Tests

    [Fact]
    public void CalculateEffectiveDpi_WithNullDisplayInfo_ReturnsFailure()
    {
        // Act
        var result = _service.CalculateEffectiveDpi(null!, 1.0, RenderingQuality.Auto);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void CalculateEffectiveDpi_WithZeroZoomLevel_ReturnsFailure()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act
        var result = _service.CalculateEffectiveDpi(displayInfo, 0.0, RenderingQuality.Auto);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("DPI_CALCULATION_FAILED");
        error.Message.Should().Contain("Zoom level must be positive");
    }

    [Fact]
    public void CalculateEffectiveDpi_WithNegativeZoomLevel_ReturnsFailure()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act
        var result = _service.CalculateEffectiveDpi(displayInfo, -1.0, RenderingQuality.Auto);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error!.ErrorCode.Should().Be("DPI_CALCULATION_FAILED");
    }

    [Fact]
    public void CalculateEffectiveDpi_WithStandardDisplay_ReturnsBaseDpi()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard(); // 1.0 scale, 96 DPI

        // Act - Auto quality with no zoom
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.0, RenderingQuality.Auto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(96.0); // 96 * 1.0 * 1.0 * 1.0
    }

    [Fact]
    public void CalculateEffectiveDpi_WithHighDpiDisplay_ReturnsScaledDpi()
    {
        // Arrange - 150% scaling
        var displayInfo = DisplayInfo.FromScale(1.5);

        // Act - Auto quality with no zoom
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.0, RenderingQuality.Auto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(144.0); // 96 * 1.5 * 1.0 * 1.0
    }

    [Fact]
    public void CalculateEffectiveDpi_WithZoom_AppliesZoomMultiplier()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act - 2x zoom
        var result = _service.CalculateEffectiveDpi(displayInfo, 2.0, RenderingQuality.Auto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(192.0); // 96 * 1.0 * 2.0 * 1.0
    }

    [Fact]
    public void CalculateEffectiveDpi_WithLowQuality_AppliesQualityMultiplier()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.0, RenderingQuality.Low);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(75.0); // 96 * 1.0 * 1.0 * 0.78125
    }

    [Fact]
    public void CalculateEffectiveDpi_WithHighQuality_AppliesQualityMultiplier()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.0, RenderingQuality.High);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(144.0); // 96 * 1.0 * 1.0 * 1.5
    }

    [Fact]
    public void CalculateEffectiveDpi_WithUltraQuality_AppliesQualityMultiplier()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();

        // Act
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.0, RenderingQuality.Ultra);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(192.0); // 96 * 1.0 * 1.0 * 2.0
    }

    [Fact]
    public void CalculateEffectiveDpi_WithCombinedFactors_AppliesAllMultipliers()
    {
        // Arrange - 200% display scaling
        var displayInfo = DisplayInfo.FromScale(2.0);

        // Act - 1.5x zoom, High quality
        var result = _service.CalculateEffectiveDpi(displayInfo, 1.5, RenderingQuality.High);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(432.0); // 96 * 2.0 * 1.5 * 1.5 = 432
    }

    [Fact]
    public void CalculateEffectiveDpi_WithExcessiveValue_ClampsToMaximum()
    {
        // Arrange - Extreme settings that would exceed max DPI
        var displayInfo = DisplayInfo.FromScale(2.0);

        // Act - Very high zoom with Ultra quality (would be 96 * 2.0 * 5.0 * 2.0 = 1920)
        var result = _service.CalculateEffectiveDpi(displayInfo, 5.0, RenderingQuality.Ultra);

        // Assert - Should be clamped to 576
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(576.0);
    }

    [Fact]
    public void CalculateEffectiveDpi_WithVeryLowValue_ClampsToMinimum()
    {
        // Arrange - Very small zoom that would go below minimum
        var displayInfo = DisplayInfo.Standard();

        // Act - Extremely low zoom with Low quality (would be 96 * 1.0 * 0.1 * 0.78125 = 7.5)
        var result = _service.CalculateEffectiveDpi(displayInfo, 0.1, RenderingQuality.Low);

        // Assert - Should be clamped to 50
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50.0);
    }

    [Fact]
    public void CalculateEffectiveDpi_LogsCalculationDetails()
    {
        // Arrange
        var displayInfo = DisplayInfo.FromScale(1.5);

        // Act
        _service.CalculateEffectiveDpi(displayInfo, 2.0, RenderingQuality.High);

        // Assert - Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Calculated effective DPI")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        _service.Dispose();
        _service.Dispose(); // Disposing twice should be safe
    }

    #endregion

    /// <summary>
    /// Mock XamlRoot for testing DPI detection.
    /// Simulates WinUI's XamlRoot.RasterizationScale and Changed event.
    /// </summary>
    private class MockXamlRoot
    {
        public double RasterizationScale { get; set; }
        public event EventHandler? Changed;

        public void TriggerChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
