using FluentAssertions;
using FluentPDF.App.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for MainViewModel demonstrating headless MVVM testing.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<ILogger<MainViewModel>> _loggerMock;

    public MainViewModelTests()
    {
        _loggerMock = new Mock<ILogger<MainViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Assert
        viewModel.Title.Should().Be("FluentPDF");
        viewModel.StatusMessage.Should().Be("Ready");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new MainViewModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Title_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Title))
                eventRaised = true;
        };

        // Act
        viewModel.Title = "New Title";

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.Title.Should().Be("New Title");
    }

    [Fact]
    public void StatusMessage_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.StatusMessage))
                eventRaised = true;
        };

        // Act
        viewModel.StatusMessage = "Loading...";

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.StatusMessage.Should().Be("Loading...");
    }

    [Fact]
    public void IsLoading_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoading))
                eventRaised = true;
        };

        // Act
        viewModel.IsLoading = true;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.IsLoading.Should().BeTrue();
    }

    [Fact]
    public async Task LoadDocumentCommand_ShouldUpdateProperties_WhenExecuted()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Act
        await viewModel.LoadDocumentCommand.ExecuteAsync(null);

        // Assert
        viewModel.IsLoading.Should().BeFalse("loading should complete");
        viewModel.StatusMessage.Should().Be("Ready");
    }

    [Fact]
    public async Task LoadDocumentCommand_ShouldSetIsLoadingToTrue_DuringExecution()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        var wasLoadingDuringExecution = false;

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoading) && viewModel.IsLoading)
                wasLoadingDuringExecution = true;
        };

        // Act
        await viewModel.LoadDocumentCommand.ExecuteAsync(null);

        // Assert
        wasLoadingDuringExecution.Should().BeTrue("IsLoading should be set to true during execution");
    }

    [Fact]
    public void SaveCommand_ShouldUpdateStatusMessage_WhenExecuted()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        viewModel.StatusMessage.Should().Be("Saved!");
    }

    [Fact]
    public void SaveCommand_CanExecute_ShouldReturnTrue_WhenNotLoading()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("SaveCommand should be executable when not loading");
    }

    [Fact]
    public void SaveCommand_CanExecute_ShouldReturnFalse_WhenLoading()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        viewModel.IsLoading = true;

        // Act
        var canExecute = viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("SaveCommand should not be executable when loading");
    }

    [Fact]
    public void IsLoading_ShouldTriggerSaveCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);
        var canExecuteChangedRaised = false;

        viewModel.SaveCommand.CanExecuteChanged += (sender, e) =>
        {
            canExecuteChangedRaised = true;
        };

        // Act
        viewModel.IsLoading = true;

        // Assert
        canExecuteChangedRaised.Should().BeTrue("CanExecuteChanged should be raised when IsLoading changes");
    }

    [Fact]
    public async Task LoadDocumentCommand_ShouldHandleExceptions_Gracefully()
    {
        // Arrange
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Since we can't inject a failing service, this test verifies the command completes
        // In a real scenario, you'd mock a document service and make it throw

        // Act
        Func<Task> act = async () => await viewModel.LoadDocumentCommand.ExecuteAsync(null);

        // Assert
        await act.Should().NotThrowAsync("LoadDocumentCommand should handle errors gracefully");
        viewModel.IsLoading.Should().BeFalse("IsLoading should be reset even if errors occur");
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the ViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Arrange & Act
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange & Act
        var viewModel = new MainViewModel(_loggerMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MainViewModel initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Constructor should log initialization");
    }
}
