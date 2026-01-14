using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Observability;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for LogViewerViewModel demonstrating headless MVVM testing with filtering logic.
/// </summary>
[Collection("UI Thread")]
public class LogViewerViewModelTests
{
    private readonly Mock<ILogExportService> _logExportServiceMock;
    private readonly Mock<ILogger<LogViewerViewModel>> _loggerMock;

    public LogViewerViewModelTests()
    {
        _logExportServiceMock = new Mock<ILogExportService>();
        _loggerMock = new Mock<ILogger<LogViewerViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LogEntries.Should().BeEmpty("initial log entries should be empty");
        viewModel.SelectedLogEntry.Should().BeNull("no log entry should be selected initially");
        viewModel.MinimumLevel.Should().BeNull("minimum level filter should be null");
        viewModel.CorrelationIdFilter.Should().BeNull("correlation ID filter should be null");
        viewModel.ComponentFilter.Should().BeNull("component filter should be null");
        viewModel.SearchText.Should().BeNull("search text should be null");
        viewModel.StartTime.Should().BeNull("start time filter should be null");
        viewModel.EndTime.Should().BeNull("end time filter should be null");
        viewModel.IsLoading.Should().BeFalse("should not be loading initially");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLogExportServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new LogViewerViewModel(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logExportService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new LogViewerViewModel(
            _logExportServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LoadLogsCommand_ShouldPopulateLogEntries_WhenSuccessful()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = CreateTestLogs(10);

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        // Act
        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Assert
        viewModel.LogEntries.Should().HaveCount(10);
        viewModel.IsLoading.Should().BeFalse("loading should complete");
    }

    [Fact]
    public async Task LoadLogsCommand_ShouldSetIsLoading_DuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = CreateTestLogs(5);
        var isLoadingDuringExecution = false;

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(() =>
            {
                isLoadingDuringExecution = viewModel.IsLoading;
                return Result.Ok<IReadOnlyList<LogEntry>>(testLogs);
            });

        // Act
        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Assert
        isLoadingDuringExecution.Should().BeTrue("IsLoading should be true during execution");
        viewModel.IsLoading.Should().BeFalse("IsLoading should be false after completion");
    }

    [Fact]
    public async Task LoadLogsCommand_ShouldClearLogEntries_WhenFailure()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.LogEntries.Add(CreateTestLogEntry(1));

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Fail<IReadOnlyList<LogEntry>>("Failed to load logs"));

        // Act
        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Assert
        viewModel.LogEntries.Should().BeEmpty("log entries should be cleared on failure");
    }

    [Fact]
    public async Task ApplyFiltersCommand_ShouldFilterByMinimumLevel()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, LogLevel.Debug),
            CreateTestLogEntry(2, LogLevel.Information),
            CreateTestLogEntry(3, LogLevel.Warning),
            CreateTestLogEntry(4, LogLevel.Error)
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.MinimumLevel = LogLevel.Warning;

        // Wait for filter to apply (property changed triggers ApplyFiltersAsync)
        await Task.Delay(100);

        // Assert
        viewModel.LogEntries.Should().HaveCount(2, "only Warning and Error should be shown");
        viewModel.LogEntries.Should().Contain(log => log.Level == LogLevel.Warning);
        viewModel.LogEntries.Should().Contain(log => log.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ApplyFiltersCommand_ShouldFilterByCorrelationId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, LogLevel.Information, "correlation-123"),
            CreateTestLogEntry(2, LogLevel.Information, "correlation-456"),
            CreateTestLogEntry(3, LogLevel.Information, "correlation-123")
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.CorrelationIdFilter = "correlation-123";

        await Task.Delay(100);

        // Assert
        viewModel.LogEntries.Should().HaveCount(2, "only logs with correlation-123 should be shown");
        viewModel.LogEntries.Should().AllSatisfy(log =>
            log.CorrelationId.Should().Be("correlation-123"));
    }

    [Fact]
    public async Task ApplyFiltersCommand_ShouldFilterByComponent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, component: "FluentPDF.Rendering.Service"),
            CreateTestLogEntry(2, component: "FluentPDF.Core.Service"),
            CreateTestLogEntry(3, component: "FluentPDF.Rendering.Worker")
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.ComponentFilter = "FluentPDF.Rendering";

        await Task.Delay(100);

        // Assert
        viewModel.LogEntries.Should().HaveCount(2, "only logs from Rendering component should be shown");
        viewModel.LogEntries.Should().AllSatisfy(log =>
            log.Component.Should().StartWith("FluentPDF.Rendering"));
    }

    [Fact]
    public async Task ApplyFiltersCommand_ShouldFilterByTimeRange()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var baseTime = DateTime.UtcNow;
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, timestamp: baseTime.AddHours(-2)),
            CreateTestLogEntry(2, timestamp: baseTime.AddHours(-1)),
            CreateTestLogEntry(3, timestamp: baseTime),
            CreateTestLogEntry(4, timestamp: baseTime.AddHours(1))
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.StartTime = new DateTimeOffset(baseTime.AddHours(-1.5));
        viewModel.EndTime = new DateTimeOffset(baseTime.AddHours(0.5));

        await Task.Delay(100);

        // Assert
        viewModel.LogEntries.Should().HaveCount(2, "only logs within time range should be shown");
    }

    [Fact]
    public async Task SearchText_ShouldDebounceFiltering()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, message: "Starting operation"),
            CreateTestLogEntry(2, message: "Processing data"),
            CreateTestLogEntry(3, message: "Operation completed")
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.SearchText = "operation";

        // Wait less than debounce time
        await Task.Delay(200);
        var countBefore = viewModel.LogEntries.Count;

        // Wait for debounce to complete (500ms + buffer)
        await Task.Delay(400);
        var countAfter = viewModel.LogEntries.Count;

        // Assert
        countBefore.Should().Be(3, "filter should not have applied yet");
        countAfter.Should().Be(2, "filter should apply after debounce");
        viewModel.LogEntries.Should().AllSatisfy(log =>
            log.Message.Should().ContainEquivalentOf("operation"));
    }

    [Fact]
    public async Task SearchText_ShouldCancelPreviousSearch_WhenTextChangesQuickly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = CreateTestLogs(10);

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act
        viewModel.SearchText = "first";
        await Task.Delay(100);
        viewModel.SearchText = "second";
        await Task.Delay(100);
        viewModel.SearchText = "Log entry 5";

        // Wait for final debounce
        await Task.Delay(600);

        // Assert
        viewModel.LogEntries.Should().HaveCount(1, "only final search should execute");
        viewModel.LogEntries.First().Message.Should().Contain("Log entry 5");
    }

    [Fact]
    public async Task ClearFiltersCommand_ShouldResetAllFilters()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = CreateTestLogs(10);

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        viewModel.MinimumLevel = LogLevel.Warning;
        viewModel.CorrelationIdFilter = "test-123";
        viewModel.ComponentFilter = "Test.Component";
        viewModel.SearchText = "test";
        viewModel.StartTime = DateTimeOffset.Now;
        viewModel.EndTime = DateTimeOffset.Now;

        // Act
        viewModel.ClearFiltersCommand.Execute(null);

        await Task.Delay(100);

        // Assert
        viewModel.MinimumLevel.Should().BeNull();
        viewModel.CorrelationIdFilter.Should().BeNull();
        viewModel.ComponentFilter.Should().BeNull();
        viewModel.SearchText.Should().BeNull();
        viewModel.StartTime.Should().BeNull();
        viewModel.EndTime.Should().BeNull();
        viewModel.LogEntries.Should().HaveCount(10, "all logs should be shown after clearing filters");
    }

    [Fact]
    public async Task ExportLogsCommand_ShouldBeAvailable()
    {
        // Note: Cannot fully test file picker interaction in headless tests
        // This verifies the command exists and is executable

        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ExportLogsCommand.Should().NotBeNull();
        viewModel.ExportLogsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CopyCorrelationIdCommand_ShouldNotCopy_WhenNoLogSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.CopyCorrelationIdCommand.Execute(null);

        // Assert
        // Command should complete without error
        viewModel.SelectedLogEntry.Should().BeNull();
    }

    [Fact]
    public void CopyCorrelationIdCommand_ShouldNotCopy_WhenSelectedLogHasNoCorrelationId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedLogEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Message = "Test",
            Component = "Test.Component",
            CorrelationId = null
        };

        // Act
        viewModel.CopyCorrelationIdCommand.Execute(null);

        // Assert
        // Command should complete without error
        viewModel.SelectedLogEntry.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task MultipleFilters_ShouldCombineCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testLogs = new List<LogEntry>
        {
            CreateTestLogEntry(1, LogLevel.Information, "corr-1", "App.Service", "Starting process"),
            CreateTestLogEntry(2, LogLevel.Warning, "corr-1", "App.Service", "Warning occurred"),
            CreateTestLogEntry(3, LogLevel.Error, "corr-2", "App.Service", "Error occurred"),
            CreateTestLogEntry(4, LogLevel.Information, "corr-1", "Other.Service", "Other log")
        };

        _logExportServiceMock
            .Setup(x => x.GetRecentLogsAsync(1000))
            .ReturnsAsync(Result.Ok<IReadOnlyList<LogEntry>>(testLogs));

        await viewModel.LoadLogsCommand.ExecuteAsync(null);

        // Act - Apply multiple filters
        viewModel.MinimumLevel = LogLevel.Information;
        viewModel.CorrelationIdFilter = "corr-1";
        viewModel.ComponentFilter = "App";

        await Task.Delay(100);

        // Assert
        viewModel.LogEntries.Should().HaveCount(2, "only logs matching all filters");
        viewModel.LogEntries.Should().AllSatisfy(log =>
        {
            log.CorrelationId.Should().Be("corr-1");
            log.Component.Should().StartWith("App");
            log.Level.Should().BeOneOf(LogLevel.Information, LogLevel.Warning, LogLevel.Error);
        });
    }

    [Fact]
    public void SelectedLogEntry_PropertyChanged_ShouldRaiseEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(LogViewerViewModel.SelectedLogEntry))
            {
                eventRaised = true;
            }
        };

        // Act
        viewModel.SelectedLogEntry = CreateTestLogEntry(1);

        // Assert
        eventRaised.Should().BeTrue("PropertyChanged should be raised for SelectedLogEntry");
    }

    [Fact]
    public void MinimumLevel_PropertyChanged_ShouldRaiseEvent()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(LogViewerViewModel.MinimumLevel))
            {
                eventRaised = true;
            }
        };

        // Act
        viewModel.MinimumLevel = LogLevel.Warning;

        // Assert
        eventRaised.Should().BeTrue("PropertyChanged should be raised for MinimumLevel");
    }

    [Fact]
    public void LoadLogsCommand_ShouldBeAvailable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LoadLogsCommand.Should().NotBeNull();
        viewModel.LoadLogsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ClearFiltersCommand_ShouldBeAvailable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ClearFiltersCommand.Should().NotBeNull();
        viewModel.ClearFiltersCommand.CanExecute(null).Should().BeTrue();
    }

    private LogViewerViewModel CreateViewModel()
    {
        return new LogViewerViewModel(
            _logExportServiceMock.Object,
            _loggerMock.Object);
    }

    private static List<LogEntry> CreateTestLogs(int count)
    {
        var logs = new List<LogEntry>();
        for (int i = 1; i <= count; i++)
        {
            logs.Add(CreateTestLogEntry(i));
        }
        return logs;
    }

    private static LogEntry CreateTestLogEntry(
        int id,
        Core.Observability.LogLevel level = Core.Observability.LogLevel.Information,
        string? correlationId = null,
        string component = "FluentPDF.Test",
        string? message = null,
        DateTime? timestamp = null)
    {
        return new LogEntry
        {
            Timestamp = timestamp ?? DateTime.UtcNow,
            Level = level,
            Message = message ?? $"Log entry {id}",
            CorrelationId = correlationId,
            Component = component
        };
    }
}
