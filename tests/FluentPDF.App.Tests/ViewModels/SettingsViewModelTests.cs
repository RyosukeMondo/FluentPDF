using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for SettingsViewModel functionality.
/// </summary>
public class SettingsViewModelTests : IDisposable
{
    private readonly Mock<ILogger<SettingsViewModel>> _loggerMock;
    private readonly Mock<IRenderingSettingsService> _renderingSettingsServiceMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly AppSettings _testSettings;
    private bool _disposed;

    public SettingsViewModelTests()
    {
        _loggerMock = new Mock<ILogger<SettingsViewModel>>();
        _renderingSettingsServiceMock = new Mock<IRenderingSettingsService>();
        _settingsServiceMock = new Mock<ISettingsService>();

        _testSettings = new AppSettings
        {
            DefaultZoom = ZoomLevel.OneHundredPercent,
            ScrollMode = ScrollMode.Vertical,
            Theme = AppTheme.UseSystem,
            TelemetryEnabled = false,
            CrashReportingEnabled = false
        };

        // Setup default behavior for rendering settings service
        _renderingSettingsServiceMock
            .Setup(s => s.GetRenderingQuality())
            .Returns(Result.Ok(RenderingQuality.Auto));

        _renderingSettingsServiceMock
            .Setup(s => s.ObserveRenderingQuality())
            .Returns(System.Reactive.Linq.Observable.Never<RenderingQuality>());

        // Setup default behavior for settings service
        _settingsServiceMock
            .Setup(s => s.Settings)
            .Returns(_testSettings);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        using var viewModel = CreateViewModel();

        // Assert
        viewModel.QualityOptions.Should().NotBeNull();
        viewModel.QualityOptions.Should().HaveCount(5);
        viewModel.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        viewModel.ScrollMode.Should().Be(ScrollMode.Vertical);
        viewModel.Theme.Should().Be(AppTheme.UseSystem);
        viewModel.TelemetryEnabled.Should().BeFalse();
        viewModel.CrashReportingEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new SettingsViewModel(
            null!,
            _renderingSettingsServiceMock.Object,
            _settingsServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenRenderingSettingsServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new SettingsViewModel(
            _loggerMock.Object,
            null!,
            _settingsServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("renderingSettingsService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenSettingsServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new SettingsViewModel(
            _loggerMock.Object,
            _renderingSettingsServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settingsService");
    }

    [Fact]
    public void DefaultZoom_WhenChanged_ShouldUpdateSettingsAndSave()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        viewModel.DefaultZoom = ZoomLevel.OneFiftyPercent;

        // Assert
        _testSettings.DefaultZoom.Should().Be(ZoomLevel.OneFiftyPercent);
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public void ScrollMode_WhenChanged_ShouldUpdateSettingsAndSave()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        viewModel.ScrollMode = ScrollMode.Horizontal;

        // Assert
        _testSettings.ScrollMode.Should().Be(ScrollMode.Horizontal);
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public void Theme_WhenChanged_ShouldUpdateSettingsAndSave()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        viewModel.Theme = AppTheme.Dark;

        // Assert
        _testSettings.Theme.Should().Be(AppTheme.Dark);
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public void TelemetryEnabled_WhenChanged_ShouldUpdateSettingsAndSave()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        viewModel.TelemetryEnabled = true;

        // Assert
        _testSettings.TelemetryEnabled.Should().BeTrue();
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public void CrashReportingEnabled_WhenChanged_ShouldUpdateSettingsAndSave()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        viewModel.CrashReportingEnabled = true;

        // Assert
        _testSettings.CrashReportingEnabled.Should().BeTrue();
        saveCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ShouldCallServiceAndReloadSettings()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var resetCalled = false;
        _settingsServiceMock
            .Setup(s => s.ResetToDefaultsAsync())
            .Callback(() =>
            {
                resetCalled = true;
                // Simulate service resetting values
                _testSettings.DefaultZoom = ZoomLevel.OneHundredPercent;
                _testSettings.ScrollMode = ScrollMode.Vertical;
                _testSettings.Theme = AppTheme.UseSystem;
                _testSettings.TelemetryEnabled = false;
                _testSettings.CrashReportingEnabled = false;
            })
            .Returns(Task.CompletedTask);

        // Change some values first
        viewModel.DefaultZoom = ZoomLevel.TwoHundredPercent;
        viewModel.Theme = AppTheme.Dark;

        // Act
        await viewModel.ResetToDefaultsCommand.ExecuteAsync(null);

        // Assert
        resetCalled.Should().BeTrue();
        viewModel.DefaultZoom.Should().Be(ZoomLevel.OneHundredPercent);
        viewModel.ScrollMode.Should().Be(ScrollMode.Vertical);
        viewModel.Theme.Should().Be(AppTheme.UseSystem);
        viewModel.TelemetryEnabled.Should().BeFalse();
        viewModel.CrashReportingEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ResetToDefaultsAsync_WhenServiceThrowsException_ShouldLogError()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        _settingsServiceMock
            .Setup(s => s.ResetToDefaultsAsync())
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        await viewModel.ResetToDefaultsCommand.ExecuteAsync(null);

        // Assert - should not throw, just log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void PropertyChanges_DuringLoading_ShouldNotTriggerSave()
    {
        // Arrange
        var saveCalled = false;
        _settingsServiceMock
            .Setup(s => s.SaveAsync())
            .Callback(() => saveCalled = true)
            .Returns(Task.CompletedTask);

        // Act - Constructor loads settings
        using var viewModel = CreateViewModel();

        // Assert - Save should not be called during initial load
        saveCalled.Should().BeFalse();
    }

    [Fact]
    public void IsUltraQualitySelected_WhenUltraSelected_ShouldReturnTrue()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var ultraOption = viewModel.QualityOptions.First(q => q.Quality == RenderingQuality.Ultra);

        // Act
        viewModel.SelectedQualityOption = ultraOption;

        // Assert
        viewModel.IsUltraQualitySelected.Should().BeTrue();
    }

    [Fact]
    public void IsUltraQualitySelected_WhenNonUltraSelected_ShouldReturnFalse()
    {
        // Arrange
        using var viewModel = CreateViewModel();
        var autoOption = viewModel.QualityOptions.First(q => q.Quality == RenderingQuality.Auto);

        // Act
        viewModel.SelectedQualityOption = autoOption;

        // Assert
        viewModel.IsUltraQualitySelected.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert - Should not throw
        viewModel.Dispose(); // Second call should be safe
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(
            _loggerMock.Object,
            _renderingSettingsServiceMock.Object,
            _settingsServiceMock.Object);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
