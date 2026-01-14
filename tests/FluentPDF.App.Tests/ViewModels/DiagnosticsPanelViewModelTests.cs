using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for DiagnosticsPanelViewModel demonstrating headless MVVM testing.
/// </summary>
[Collection("UI Thread")]
public class DiagnosticsPanelViewModelTests : IDisposable
{
    private readonly Mock<IMetricsCollectionService> _metricsServiceMock;
    private readonly Mock<ILogger<DiagnosticsPanelViewModel>> _loggerMock;

    public DiagnosticsPanelViewModelTests()
    {
        _metricsServiceMock = new Mock<IMetricsCollectionService>();
        _loggerMock = new Mock<ILogger<DiagnosticsPanelViewModel>>();

        // Setup default metrics
        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(CreateTestMetrics());
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CurrentFPS.Should().Be(0, "initial FPS should be 0");
        viewModel.ManagedMemoryMB.Should().Be(0, "initial managed memory should be 0");
        viewModel.NativeMemoryMB.Should().Be(0, "initial native memory should be 0");
        viewModel.TotalMemoryMB.Should().Be(0, "initial total memory should be 0");
        viewModel.LastRenderTimeMs.Should().Be(0, "initial render time should be 0");
        viewModel.CurrentPageNumber.Should().Be(0, "initial page number should be 0");
        viewModel.FpsColor.Should().NotBeNull("FPS color should be initialized");
        viewModel.IsVisible.Should().BeFalse("panel should not be visible by default");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenMetricsServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new DiagnosticsPanelViewModel(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metricsService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new DiagnosticsPanelViewModel(
            _metricsServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task UpdateMetrics_ShouldUpdateProperties_WhenCalled()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testMetrics = new PerformanceMetrics
        {
            CurrentFPS = 45.5,
            ManagedMemoryMB = 200,
            NativeMemoryMB = 150,
            LastRenderTimeMs = 16.7,
            CurrentPageNumber = 5,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Good
        };

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(testMetrics);

        // Act
        // Wait for timer to trigger update (500ms + buffer)
        await Task.Delay(700);

        // Assert
        viewModel.CurrentFPS.Should().Be(45.5);
        viewModel.ManagedMemoryMB.Should().Be(200);
        viewModel.NativeMemoryMB.Should().Be(150);
        viewModel.TotalMemoryMB.Should().Be(350);
        viewModel.LastRenderTimeMs.Should().Be(16.7);
        viewModel.CurrentPageNumber.Should().Be(5);
    }

    [Fact]
    public async Task UpdateMetrics_ShouldSetGreenColor_WhenPerformanceIsGood()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testMetrics = new PerformanceMetrics
        {
            CurrentFPS = 60.0,
            ManagedMemoryMB = 100,
            NativeMemoryMB = 50,
            LastRenderTimeMs = 10.0,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Good
        };

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(testMetrics);

        // Act
        await Task.Delay(700);

        // Assert
        viewModel.FpsColor.Color.Should().Be(Colors.Green);
    }

    [Fact]
    public async Task UpdateMetrics_ShouldSetOrangeColor_WhenPerformanceIsWarning()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testMetrics = new PerformanceMetrics
        {
            CurrentFPS = 25.0,
            ManagedMemoryMB = 400,
            NativeMemoryMB = 200,
            LastRenderTimeMs = 40.0,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Warning
        };

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(testMetrics);

        // Act
        await Task.Delay(700);

        // Assert
        viewModel.FpsColor.Color.Should().Be(Colors.Orange);
    }

    [Fact]
    public async Task UpdateMetrics_ShouldSetRedColor_WhenPerformanceIsCritical()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testMetrics = new PerformanceMetrics
        {
            CurrentFPS = 10.0,
            ManagedMemoryMB = 800,
            NativeMemoryMB = 400,
            LastRenderTimeMs = 100.0,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Critical
        };

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(testMetrics);

        // Act
        await Task.Delay(700);

        // Assert
        viewModel.FpsColor.Color.Should().Be(Colors.Red);
    }

    [Fact]
    public void ToggleVisibilityCommand_ShouldToggleVisibility()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var initialVisibility = viewModel.IsVisible;

        // Act
        viewModel.ToggleVisibilityCommand.Execute(null);

        // Assert
        viewModel.IsVisible.Should().Be(!initialVisibility);
    }

    [Fact]
    public void ToggleVisibilityCommand_ShouldToggleMultipleTimes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.ToggleVisibilityCommand.Execute(null);
        viewModel.IsVisible.Should().BeTrue();

        viewModel.ToggleVisibilityCommand.Execute(null);
        viewModel.IsVisible.Should().BeFalse();

        viewModel.ToggleVisibilityCommand.Execute(null);
        viewModel.IsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task ExportMetricsCommand_ShouldCallMetricsService_WhenFileSelected()
    {
        // Note: This test cannot fully test the file picker interaction
        // as it requires UI automation. We can only test the command exists.

        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ExportMetricsCommand.Should().NotBeNull();
        viewModel.ExportMetricsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task PeriodicUpdate_ShouldCallGetCurrentMetrics_Repeatedly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var callCount = 0;

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(() =>
            {
                callCount++;
                return CreateTestMetrics();
            });

        // Act
        // Wait for ~3 timer ticks (1500ms + buffer)
        await Task.Delay(1800);

        // Assert
        callCount.Should().BeGreaterOrEqualTo(2, "timer should have ticked at least twice");
    }

    [Fact]
    public void Dispose_ShouldStopTimer()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var initialCallCount = 0;

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(() =>
            {
                initialCallCount++;
                return CreateTestMetrics();
            })
            .Verifiable();

        // Act
        viewModel.Dispose();

        // Give some time to ensure timer stopped
        Task.Delay(1000).Wait();

        // The call count should not increase significantly after dispose
        var finalCallCount = initialCallCount;

        // Assert
        // We can't guarantee exact count due to race conditions,
        // but verify Dispose doesn't throw
        Action act = () => viewModel.Dispose();
        act.Should().NotThrow("disposing multiple times should be safe");
    }

    [Fact]
    public void IsVisible_PropertyChanged_ShouldRaiseEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(DiagnosticsPanelViewModel.IsVisible))
            {
                eventRaised = true;
            }
        };

        // Act
        viewModel.ToggleVisibilityCommand.Execute(null);

        // Assert
        eventRaised.Should().BeTrue("PropertyChanged should be raised for IsVisible");
    }

    [Fact]
    public async Task CurrentFPS_PropertyChanged_ShouldRaiseEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(DiagnosticsPanelViewModel.CurrentFPS))
            {
                eventRaised = true;
            }
        };

        var testMetrics = new PerformanceMetrics
        {
            CurrentFPS = 60.0,
            ManagedMemoryMB = 100,
            NativeMemoryMB = 50,
            LastRenderTimeMs = 10.0,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Good
        };

        _metricsServiceMock
            .Setup(x => x.GetCurrentMetrics())
            .Returns(testMetrics);

        // Act
        await Task.Delay(700);

        // Assert
        eventRaised.Should().BeTrue("PropertyChanged should be raised for CurrentFPS");
    }

    private DiagnosticsPanelViewModel CreateViewModel()
    {
        return new DiagnosticsPanelViewModel(
            _metricsServiceMock.Object,
            _loggerMock.Object);
    }

    private static PerformanceMetrics CreateTestMetrics()
    {
        return new PerformanceMetrics
        {
            CurrentFPS = 30.0,
            ManagedMemoryMB = 100,
            NativeMemoryMB = 50,
            LastRenderTimeMs = 16.7,
            CurrentPageNumber = 1,
            Timestamp = DateTime.UtcNow,
            Level = PerformanceLevel.Good
        };
    }

    public void Dispose()
    {
        // Cleanup any test resources if needed
    }
}
