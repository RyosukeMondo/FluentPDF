using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Windows.Storage;

namespace FluentPDF.App.Tests.Services;

/// <summary>
/// Tests for RenderingSettingsService demonstrating persistence and observable changes.
/// Tests use real ApplicationData.LocalSettings for integration testing.
/// </summary>
public sealed class RenderingSettingsServiceTests : IDisposable
{
    private const string StorageKey = "RenderingQuality";
    private readonly Mock<ILogger<RenderingSettingsService>> _loggerMock;
    private readonly ApplicationDataContainer _settings;

    public RenderingSettingsServiceTests()
    {
        _loggerMock = new Mock<ILogger<RenderingSettingsService>>();
        _settings = ApplicationData.Current.LocalSettings;

        // Clean up storage before each test
        _settings.Values.Remove(StorageKey);
    }

    public void Dispose()
    {
        // Clean up storage after each test
        _settings.Values.Remove(StorageKey);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new RenderingSettingsService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithAutoQuality_WhenNoStoredData()
    {
        // Act
        using var service = new RenderingSettingsService(_loggerMock.Object);

        // Assert
        var result = service.GetRenderingQuality();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(RenderingQuality.Auto);
    }

    [Fact]
    public void GetRenderingQuality_ShouldReturnAutoQuality_WhenNoQualitySet()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);

        // Act
        var result = service.GetRenderingQuality();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(RenderingQuality.Auto);
    }

    [Theory]
    [InlineData(RenderingQuality.Auto)]
    [InlineData(RenderingQuality.Low)]
    [InlineData(RenderingQuality.Medium)]
    [InlineData(RenderingQuality.High)]
    [InlineData(RenderingQuality.Ultra)]
    public void SetRenderingQuality_ShouldPersistQuality_ForValidValues(RenderingQuality quality)
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);

        // Act
        var setResult = service.SetRenderingQuality(quality);
        var getResult = service.GetRenderingQuality();

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be(quality);
    }

    [Fact]
    public void SetRenderingQuality_ShouldPersistToStorage()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);
        var quality = RenderingQuality.High;

        // Act
        service.SetRenderingQuality(quality);

        // Assert - Verify it's actually in storage
        _settings.Values.TryGetValue(StorageKey, out var storedValue).Should().BeTrue();
        storedValue.Should().Be((int)quality);
    }

    [Fact]
    public void SetRenderingQuality_ShouldReturnFailure_ForInvalidValue()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);
        var invalidQuality = (RenderingQuality)999;

        // Act
        var result = service.SetRenderingQuality(invalidQuality);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldLoadPersistedQuality_WhenStorageContainsValidData()
    {
        // Arrange - Pre-populate storage
        var expectedQuality = RenderingQuality.Ultra;
        _settings.Values[StorageKey] = (int)expectedQuality;

        // Act
        using var service = new RenderingSettingsService(_loggerMock.Object);
        var result = service.GetRenderingQuality();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedQuality);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultQuality_WhenStorageContainsInvalidData()
    {
        // Arrange - Pre-populate storage with invalid value
        _settings.Values[StorageKey] = 999;

        // Act
        using var service = new RenderingSettingsService(_loggerMock.Object);
        var result = service.GetRenderingQuality();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(RenderingQuality.Auto);
    }

    [Fact]
    public async Task ObserveRenderingQuality_ShouldEmitCurrentValue_OnSubscribe()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);
        service.SetRenderingQuality(RenderingQuality.High);

        // Act
        var emittedValue = await service.ObserveRenderingQuality().FirstAsync();

        // Assert
        emittedValue.Should().Be(RenderingQuality.High);
    }

    [Fact]
    public async Task ObserveRenderingQuality_ShouldEmitUpdates_WhenQualityChanges()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);
        var emittedValues = new System.Collections.Generic.List<RenderingQuality>();

        using var subscription = service.ObserveRenderingQuality()
            .Subscribe(quality => emittedValues.Add(quality));

        // Act - Change quality multiple times
        service.SetRenderingQuality(RenderingQuality.Low);
        service.SetRenderingQuality(RenderingQuality.High);
        service.SetRenderingQuality(RenderingQuality.Ultra);

        // Wait for observable to emit
        await Task.Delay(100);

        // Assert
        emittedValues.Should().HaveCountGreaterOrEqualTo(4); // Initial + 3 changes
        emittedValues.Should().Contain(RenderingQuality.Auto); // Initial value
        emittedValues.Should().Contain(RenderingQuality.Low);
        emittedValues.Should().Contain(RenderingQuality.High);
        emittedValues.Should().Contain(RenderingQuality.Ultra);
    }

    [Fact]
    public async Task ObserveRenderingQuality_ShouldNotEmit_WhenQualitySetToSameValue()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);
        service.SetRenderingQuality(RenderingQuality.Medium);

        var emittedValues = new System.Collections.Generic.List<RenderingQuality>();
        using var subscription = service.ObserveRenderingQuality()
            .Skip(1) // Skip initial value
            .Subscribe(quality => emittedValues.Add(quality));

        // Act - Set to same value multiple times
        service.SetRenderingQuality(RenderingQuality.Medium);
        service.SetRenderingQuality(RenderingQuality.Medium);
        service.SetRenderingQuality(RenderingQuality.Medium);

        // Wait for observable
        await Task.Delay(100);

        // Assert - Should not emit duplicate values
        emittedValues.Should().BeEmpty();
    }

    [Fact]
    public void SetRenderingQuality_ShouldUpdateMultipleTimes()
    {
        // Arrange
        using var service = new RenderingSettingsService(_loggerMock.Object);

        // Act & Assert - Change quality multiple times
        service.SetRenderingQuality(RenderingQuality.Low);
        service.GetRenderingQuality().Value.Should().Be(RenderingQuality.Low);

        service.SetRenderingQuality(RenderingQuality.High);
        service.GetRenderingQuality().Value.Should().Be(RenderingQuality.High);

        service.SetRenderingQuality(RenderingQuality.Auto);
        service.GetRenderingQuality().Value.Should().Be(RenderingQuality.Auto);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var service = new RenderingSettingsService(_loggerMock.Object);

        // Act
        service.Dispose();

        // Assert - Should not throw
        // Multiple disposes should be safe
        Action act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void PersistenceRoundTrip_ShouldMaintainQualityAcrossServiceInstances()
    {
        // Arrange & Act
        using (var service1 = new RenderingSettingsService(_loggerMock.Object))
        {
            service1.SetRenderingQuality(RenderingQuality.Ultra);
        }

        // Create new service instance
        using var service2 = new RenderingSettingsService(_loggerMock.Object);
        var result = service2.GetRenderingQuality();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(RenderingQuality.Ultra);
    }
}
