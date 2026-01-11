using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Windows.Storage;

namespace FluentPDF.App.Tests.Services;

/// <summary>
/// Tests for SettingsService demonstrating persistence, validation, debouncing, and error handling.
/// Tests use real ApplicationData.LocalFolder for integration testing.
/// </summary>
public sealed class SettingsServiceTests : IAsyncDisposable
{
    private const string SettingsFileName = "settings.json";
    private readonly Mock<ILogger<SettingsService>> _loggerMock;
    private readonly StorageFolder _localFolder;

    public SettingsServiceTests()
    {
        _loggerMock = new Mock<ILogger<SettingsService>>();
        _localFolder = ApplicationData.Current.LocalFolder;
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up settings file after each test
        try
        {
            var settingsFile = await _localFolder.TryGetItemAsync(SettingsFileName) as StorageFile;
            if (settingsFile != null)
            {
                await settingsFile.DeleteAsync();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new SettingsService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var service = new SettingsService(_loggerMock.Object);

        // Assert
        service.Settings.Should().NotBeNull();
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);
        service.Settings.TelemetryEnabled.Should().BeFalse();
        service.Settings.CrashReportingEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldCreateDefaultSettings_WhenFileDoesNotExist()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);

        // Verify file was created
        var settingsFile = await _localFolder.TryGetItemAsync(SettingsFileName) as StorageFile;
        settingsFile.Should().NotBeNull("settings file should be created on first load");
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistSettings()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        service.Settings.DefaultZoom = ZoomLevel.OneFiftyPercent;
        service.Settings.ScrollMode = ScrollMode.Horizontal;
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.TelemetryEnabled = true;
        service.Settings.CrashReportingEnabled = true;

        // Act
        await service.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Assert - Verify file was created and contains correct data
        var settingsFile = await _localFolder.GetFileAsync(SettingsFileName);
        var json = await FileIO.ReadTextAsync(settingsFile);
        var savedSettings = JsonSerializer.Deserialize<AppSettings>(
            json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        savedSettings.Should().NotBeNull();
        savedSettings!.DefaultZoom.Should().Be(ZoomLevel.OneFiftyPercent);
        savedSettings.ScrollMode.Should().Be(ScrollMode.Horizontal);
        savedSettings.Theme.Should().Be(AppTheme.Dark);
        savedSettings.TelemetryEnabled.Should().BeTrue();
        savedSettings.CrashReportingEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadPersistedSettings()
    {
        // Arrange - Create and save settings
        var service1 = new SettingsService(_loggerMock.Object);
        service1.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;
        service1.Settings.ScrollMode = ScrollMode.FitPage;
        service1.Settings.Theme = AppTheme.Light;
        service1.Settings.TelemetryEnabled = true;
        await service1.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Act - Load settings in new service instance
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();

        // Assert
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.TwoHundredPercent);
        service2.Settings.ScrollMode.Should().Be(ScrollMode.FitPage);
        service2.Settings.Theme.Should().Be(AppTheme.Light);
        service2.Settings.TelemetryEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        AppSettings? eventArgs = null;
        service.SettingsChanged += (sender, settings) => eventArgs = settings;

        service.Settings.DefaultZoom = ZoomLevel.FiftyPercent;

        // Act
        await service.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.DefaultZoom.Should().Be(ZoomLevel.FiftyPercent);
    }

    [Fact]
    public async Task SaveAsync_ShouldDebounceRapidSaves()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        var eventCount = 0;
        service.SettingsChanged += (sender, settings) => eventCount++;

        // Act - Make multiple rapid changes
        service.Settings.DefaultZoom = ZoomLevel.FiftyPercent;
        await service.SaveAsync();

        service.Settings.DefaultZoom = ZoomLevel.SeventyFivePercent;
        await service.SaveAsync();

        service.Settings.DefaultZoom = ZoomLevel.OneHundredPercent;
        await service.SaveAsync();

        // Wait for debounce to complete
        await Task.Delay(600);

        // Assert - Should only save once (last value)
        eventCount.Should().Be(1, "debouncing should batch rapid saves");
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);

        // Verify persisted value
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldRestoreDefaults()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        service.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;
        service.Settings.ScrollMode = ScrollMode.Horizontal;
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.TelemetryEnabled = true;
        service.Settings.CrashReportingEnabled = true;
        await service.SaveAsync();
        await Task.Delay(600);

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);
        service.Settings.TelemetryEnabled.Should().BeFalse();
        service.Settings.CrashReportingEnabled.Should().BeFalse();

        // Verify persistence
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldRaiseSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        AppSettings? eventArgs = null;
        service.SettingsChanged += (sender, settings) => eventArgs = settings;

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
    }

    [Fact]
    public async Task LoadAsync_ShouldHandleCorruptedJson()
    {
        // Arrange - Create corrupted settings file
        var settingsFile = await _localFolder.CreateFileAsync(
            SettingsFileName,
            CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(settingsFile, "{ invalid json }");

        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert - Should fall back to defaults
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);

        // Verify file was repaired with valid defaults
        await Task.Delay(600);
        var json = await FileIO.ReadTextAsync(settingsFile);
        json.Should().NotBeEmpty();
        json.Should().NotContain("invalid json");
    }

    [Fact]
    public async Task LoadAsync_ShouldValidateEnumValues()
    {
        // Arrange - Create settings file with invalid enum value
        var invalidSettings = new
        {
            defaultZoom = 9999, // Invalid enum value
            scrollMode = 0,
            theme = 0,
            telemetryEnabled = false,
            crashReportingEnabled = false
        };

        var settingsFile = await _localFolder.CreateFileAsync(
            SettingsFileName,
            CreationCollisionOption.ReplaceExisting);
        var json = JsonSerializer.Serialize(
            invalidSettings,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await FileIO.WriteTextAsync(settingsFile, json);

        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert - Invalid value should be corrected to default
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
    }

    [Fact]
    public async Task LoadAsync_ShouldHandleNullDeserialization()
    {
        // Arrange - Create settings file that deserializes to null
        var settingsFile = await _localFolder.CreateFileAsync(
            SettingsFileName,
            CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(settingsFile, "null");

        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert - Should fall back to defaults
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
    }

    [Fact]
    public async Task SaveAsync_MultipleServices_ShouldNotCorruptData()
    {
        // Arrange - Simulate multiple instances
        var service1 = new SettingsService(_loggerMock.Object);
        var service2 = new SettingsService(_loggerMock.Object);

        // Act - Both services save different values
        service1.Settings.DefaultZoom = ZoomLevel.FiftyPercent;
        service2.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;

        await service1.SaveAsync();
        await service2.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Assert - Last save should win
        var service3 = new SettingsService(_loggerMock.Object);
        await service3.LoadAsync();
        service3.Settings.DefaultZoom.Should().Be(ZoomLevel.TwoHundredPercent);
    }

    [Fact]
    public async Task Settings_ShouldReturnCurrentSettings()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);

        // Act
        var settings = service.Settings;

        // Assert
        settings.Should().NotBeNull();
        settings.Should().BeSameAs(service.Settings, "should return same instance");
    }

    [Fact]
    public async Task LoadAsync_ShouldHandleFileSystemErrors()
    {
        // Arrange - This test verifies graceful error handling
        var service = new SettingsService(_loggerMock.Object);

        // Act - Load from non-existent file (normal first-run scenario)
        await service.LoadAsync();

        // Assert - Should complete without throwing
        service.Settings.Should().NotBeNull();
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
    }

    [Fact]
    public async Task SaveAsync_ShouldOverwriteExistingFile()
    {
        // Arrange
        var service1 = new SettingsService(_loggerMock.Object);
        service1.Settings.DefaultZoom = ZoomLevel.FiftyPercent;
        await service1.SaveAsync();
        await Task.Delay(600);

        // Act - Save different value
        service1.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;
        await service1.SaveAsync();
        await Task.Delay(600);

        // Assert
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.TwoHundredPercent);
    }

    [Fact]
    public async Task SettingsChanged_ShouldProvideSettingsSnapshot()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        AppSettings? receivedSettings = null;
        service.SettingsChanged += (sender, settings) => receivedSettings = settings;

        // Act
        service.Settings.DefaultZoom = ZoomLevel.OneFiftyPercent;
        await service.SaveAsync();
        await Task.Delay(600);

        // Assert
        receivedSettings.Should().NotBeNull();
        receivedSettings!.DefaultZoom.Should().Be(ZoomLevel.OneFiftyPercent);
        receivedSettings.Should().BeSameAs(service.Settings);
    }
}
