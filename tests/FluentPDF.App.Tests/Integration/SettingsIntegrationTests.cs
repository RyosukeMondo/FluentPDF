using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Moq;
using Windows.Storage;
using Xunit;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for the settings system, including persistence, theme switching,
/// and recovery from corrupt data.
/// These tests verify the complete workflow of loading, saving, and applying settings
/// across application restarts.
/// </summary>
public sealed class SettingsIntegrationTests : IDisposable
{
    private const string SettingsFileName = "settings.json";
    private readonly StorageFolder _localFolder;
    private readonly Mock<ILogger<SettingsService>> _loggerMock;

    public SettingsIntegrationTests()
    {
        _localFolder = ApplicationData.Current.LocalFolder;
        _loggerMock = new Mock<ILogger<SettingsService>>();

        // Clean up settings file before each test
        CleanupSettingsFileAsync().Wait();
    }

    public void Dispose()
    {
        // Clean up settings file after each test
        CleanupSettingsFileAsync().Wait();
    }

    private async Task CleanupSettingsFileAsync()
    {
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
    public async Task LoadAsync_WhenNoSettingsFileExists_CreatesDefaultSettings()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);
        service.Settings.TelemetryEnabled.Should().BeFalse();
        service.Settings.CrashReportingEnabled.Should().BeFalse();

        // Verify settings file was created
        var settingsFile = await _localFolder.TryGetItemAsync(SettingsFileName) as StorageFile;
        settingsFile.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_PersistsSettingsToFile()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        await service.LoadAsync();

        // Modify settings
        service.Settings.DefaultZoom = ZoomLevel.OneFiftyPercent;
        service.Settings.ScrollMode = ScrollMode.Horizontal;
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.TelemetryEnabled = true;
        service.Settings.CrashReportingEnabled = true;

        // Act
        await service.SaveAsync();

        // Wait for debounce
        await Task.Delay(600);

        // Assert - Read file directly
        var settingsFile = await _localFolder.GetFileAsync(SettingsFileName);
        var json = await FileIO.ReadTextAsync(settingsFile);
        var savedSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        savedSettings.Should().NotBeNull();
        savedSettings!.DefaultZoom.Should().Be(ZoomLevel.OneFiftyPercent);
        savedSettings.ScrollMode.Should().Be(ScrollMode.Horizontal);
        savedSettings.Theme.Should().Be(AppTheme.Dark);
        savedSettings.TelemetryEnabled.Should().BeTrue();
        savedSettings.CrashReportingEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_PersistsAcrossServiceInstances()
    {
        // Arrange - First service instance saves settings
        var service1 = new SettingsService(_loggerMock.Object);
        await service1.LoadAsync();
        service1.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;
        service1.Settings.Theme = AppTheme.Light;
        await service1.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Act - Second service instance loads settings
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();

        // Assert
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.TwoHundredPercent);
        service2.Settings.Theme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_RestoresDefaultValues()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        await service.LoadAsync();

        // Modify settings
        service.Settings.DefaultZoom = ZoomLevel.TwoHundredPercent;
        service.Settings.ScrollMode = ScrollMode.Horizontal;
        service.Settings.Theme = AppTheme.Dark;
        service.Settings.TelemetryEnabled = true;
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
    public async Task LoadAsync_RecoveryFromCorruptFile_UsesDefaults()
    {
        // Arrange - Create corrupt settings file
        var settingsFile = await _localFolder.CreateFileAsync(
            SettingsFileName,
            CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(settingsFile, "{ invalid json data }");

        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert - Should fall back to defaults
        service.Settings.Should().NotBeNull();
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);

        // Verify corrupted file was replaced with valid defaults
        var json = await FileIO.ReadTextAsync(settingsFile);
        var action = () => JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        action.Should().NotThrow();
    }

    [Fact]
    public async Task LoadAsync_ValidationOfInvalidEnumValues_CorrectsToDefaults()
    {
        // Arrange - Create settings file with invalid enum values
        var settingsFile = await _localFolder.CreateFileAsync(
            SettingsFileName,
            CreationCollisionOption.ReplaceExisting);

        // Write JSON with invalid enum values (outside defined range)
        var invalidJson = @"{
            ""defaultZoom"": 9999,
            ""scrollMode"": 9999,
            ""theme"": 9999,
            ""telemetryEnabled"": true,
            ""crashReportingEnabled"": true
        }";
        await FileIO.WriteTextAsync(settingsFile, invalidJson);

        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert - Invalid enums should be corrected to defaults
        service.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service.Settings.Theme.Should().Be(AppTheme.UseSystem);

        // But boolean values should be preserved
        service.Settings.TelemetryEnabled.Should().BeTrue();
        service.Settings.CrashReportingEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SettingsChanged_EventFiredOnSave()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        await service.LoadAsync();

        AppSettings? capturedSettings = null;
        service.SettingsChanged += (sender, settings) => capturedSettings = settings;

        // Modify settings
        service.Settings.Theme = AppTheme.Dark;

        // Act
        await service.SaveAsync();
        await Task.Delay(600); // Wait for debounce

        // Assert
        capturedSettings.Should().NotBeNull();
        capturedSettings!.Theme.Should().Be(AppTheme.Dark);
    }

    [Fact]
    public async Task SettingsChanged_EventFiredOnReset()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        await service.LoadAsync();
        service.Settings.Theme = AppTheme.Dark;
        await service.SaveAsync();
        await Task.Delay(600);

        AppSettings? capturedSettings = null;
        service.SettingsChanged += (sender, settings) => capturedSettings = settings;

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        capturedSettings.Should().NotBeNull();
        capturedSettings!.Theme.Should().Be(AppTheme.UseSystem);
    }

    [Fact]
    public async Task SaveAsync_DebouncesRapidSaves()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        await service.LoadAsync();

        var settingsChangedCount = 0;
        service.SettingsChanged += (sender, settings) => settingsChangedCount++;

        // Act - Make rapid changes
        service.Settings.Theme = AppTheme.Dark;
        await service.SaveAsync();

        service.Settings.Theme = AppTheme.Light;
        await service.SaveAsync();

        service.Settings.Theme = AppTheme.UseSystem;
        await service.SaveAsync();

        // Wait for debounce to complete
        await Task.Delay(700);

        // Assert - Should only fire event once due to debouncing
        // The last save should win
        settingsChangedCount.Should().Be(1);

        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();
        service2.Settings.Theme.Should().Be(AppTheme.UseSystem);
    }

    [Fact]
    public async Task FullWorkflow_LoadModifySaveReload_MaintainsState()
    {
        // Arrange & Act - Complete workflow
        // 1. Initial load (creates defaults)
        var service1 = new SettingsService(_loggerMock.Object);
        await service1.LoadAsync();
        service1.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);

        // 2. Modify all settings
        service1.Settings.DefaultZoom = ZoomLevel.OneFiftyPercent;
        service1.Settings.ScrollMode = ScrollMode.FitPage;
        service1.Settings.Theme = AppTheme.Dark;
        service1.Settings.TelemetryEnabled = true;
        service1.Settings.CrashReportingEnabled = true;
        await service1.SaveAsync();
        await Task.Delay(600);

        // 3. Simulate app restart - new service instance
        var service2 = new SettingsService(_loggerMock.Object);
        await service2.LoadAsync();

        // Assert - All settings should persist
        service2.Settings.DefaultZoom.Should().Be(ZoomLevel.OneFiftyPercent);
        service2.Settings.ScrollMode.Should().Be(ScrollMode.FitPage);
        service2.Settings.Theme.Should().Be(AppTheme.Dark);
        service2.Settings.TelemetryEnabled.Should().BeTrue();
        service2.Settings.CrashReportingEnabled.Should().BeTrue();

        // 4. Reset to defaults
        await service2.ResetToDefaultsAsync();

        // 5. Simulate another restart
        var service3 = new SettingsService(_loggerMock.Object);
        await service3.LoadAsync();

        // Assert - Should be back to defaults
        service3.Settings.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        service3.Settings.ScrollMode.Should().Be(ScrollMode.Vertical);
        service3.Settings.Theme.Should().Be(AppTheme.UseSystem);
        service3.Settings.TelemetryEnabled.Should().BeFalse();
        service3.Settings.CrashReportingEnabled.Should().BeFalse();
    }
}
