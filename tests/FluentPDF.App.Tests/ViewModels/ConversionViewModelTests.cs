using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using FluentPDF.App.Views;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for ConversionViewModel demonstrating headless MVVM testing.
/// Tests conversion workflow, file selection, progress tracking, and error handling.
/// </summary>
public class ConversionViewModelTests
{
    private readonly Mock<IDocxConverterService> _converterServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<ILogger<ConversionViewModel>> _loggerMock;

    public ConversionViewModelTests()
    {
        _converterServiceMock = new Mock<IDocxConverterService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _loggerMock = new Mock<ILogger<ConversionViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.DocxFilePath.Should().BeNull();
        viewModel.OutputFilePath.Should().BeNull();
        viewModel.StatusMessage.Should().Be("Select a DOCX file to begin conversion");
        viewModel.IsConverting.Should().BeFalse();
        viewModel.ConversionProgress.Should().Be(0);
        viewModel.EnableQualityValidation.Should().BeFalse();
        viewModel.Result.Should().BeNull();
        viewModel.HasResults.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenConverterServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new ConversionViewModel(
            null!,
            _navigationServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("converterService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenNavigationServiceIsNull()
    {
        // Arrange & Act
        Action act = () => new ConversionViewModel(
            _converterServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange & Act
        Action act = () => new ConversionViewModel(
            _converterServiceMock.Object,
            _navigationServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void DocxFilePath_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ConversionViewModel.DocxFilePath))
                eventRaised = true;
        };

        // Act
        viewModel.DocxFilePath = "test.docx";

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.DocxFilePath.Should().Be("test.docx");
    }

    [Fact]
    public void OutputFilePath_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ConversionViewModel.OutputFilePath))
                eventRaised = true;
        };

        // Act
        viewModel.OutputFilePath = "output.pdf";

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.OutputFilePath.Should().Be("output.pdf");
    }

    [Fact]
    public void IsConverting_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ConversionViewModel.IsConverting))
                eventRaised = true;
        };

        // Act
        viewModel.IsConverting = true;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.IsConverting.Should().BeTrue();
    }

    [Fact]
    public void EnableQualityValidation_ShouldRaisePropertyChanged_WhenSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var eventRaised = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ConversionViewModel.EnableQualityValidation))
                eventRaised = true;
        };

        // Act
        viewModel.EnableQualityValidation = true;

        // Assert
        eventRaised.Should().BeTrue();
        viewModel.EnableQualityValidation.Should().BeTrue();
    }

    [Fact]
    public void ConvertCommand_CanExecute_ShouldReturnFalse_WhenDocxFilePathIsNull()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.OutputFilePath = "output.pdf";

        // Act
        var canExecute = viewModel.ConvertCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("ConvertCommand requires both input and output paths");
    }

    [Fact]
    public void ConvertCommand_CanExecute_ShouldReturnFalse_WhenOutputFilePathIsNull()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";

        // Act
        var canExecute = viewModel.ConvertCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("ConvertCommand requires both input and output paths");
    }

    [Fact]
    public void ConvertCommand_CanExecute_ShouldReturnTrue_WhenBothPathsAreSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";

        // Act
        var canExecute = viewModel.ConvertCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("ConvertCommand should be executable when both paths are set");
    }

    [Fact]
    public void ConvertCommand_CanExecute_ShouldReturnFalse_WhenIsConverting()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";
        viewModel.IsConverting = true;

        // Act
        var canExecute = viewModel.ConvertCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("ConvertCommand should not be executable during conversion");
    }

    [Fact]
    public void SelectDocxFileCommand_CanExecute_ShouldReturnTrue_WhenNotConverting()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.SelectDocxFileCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void SelectDocxFileCommand_CanExecute_ShouldReturnFalse_WhenConverting()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsConverting = true;

        // Act
        var canExecute = viewModel.SelectDocxFileCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void SelectOutputPathCommand_CanExecute_ShouldReturnFalse_WhenDocxFilePathIsNull()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.SelectOutputPathCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("SelectOutputPathCommand requires input file to be selected first");
    }

    [Fact]
    public void SelectOutputPathCommand_CanExecute_ShouldReturnTrue_WhenDocxFilePathIsSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";

        // Act
        var canExecute = viewModel.SelectOutputPathCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void OpenPdfCommand_CanExecute_ShouldReturnFalse_WhenResultIsNull()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.OpenPdfCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("OpenPdfCommand requires a conversion result");
    }

    [Fact]
    public void OpenPdfCommand_CanExecute_ShouldReturnFalse_WhenConverting()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsConverting = true;

        // Act
        var canExecute = viewModel.OpenPdfCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CancelConversionCommand_CanExecute_ShouldReturnFalse_WhenNotConverting()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.CancelConversionCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void IsConverting_ShouldTriggerCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var convertCanExecuteChangedRaised = false;
        var selectDocxCanExecuteChangedRaised = false;

        viewModel.ConvertCommand.CanExecuteChanged += (sender, e) =>
        {
            convertCanExecuteChangedRaised = true;
        };

        viewModel.SelectDocxFileCommand.CanExecuteChanged += (sender, e) =>
        {
            selectDocxCanExecuteChangedRaised = true;
        };

        // Act
        viewModel.IsConverting = true;

        // Assert
        convertCanExecuteChangedRaised.Should().BeTrue();
        selectDocxCanExecuteChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void DocxFilePath_ShouldTriggerCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChangedRaised = false;

        viewModel.ConvertCommand.CanExecuteChanged += (sender, e) =>
        {
            canExecuteChangedRaised = true;
        };

        // Act
        viewModel.DocxFilePath = "test.docx";

        // Assert
        canExecuteChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void OutputFilePath_ShouldTriggerCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChangedRaised = false;

        viewModel.ConvertCommand.CanExecuteChanged += (sender, e) =>
        {
            canExecuteChangedRaised = true;
        };

        // Act
        viewModel.OutputFilePath = "output.pdf";

        // Assert
        canExecuteChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void Result_ShouldTriggerOpenPdfCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChangedRaised = false;

        viewModel.OpenPdfCommand.CanExecuteChanged += (sender, e) =>
        {
            canExecuteChangedRaised = true;
        };

        // Create a temporary file for testing
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");

        try
        {
            var result = new ConversionResult
            {
                OutputPath = tempFile,
                SourcePath = "test.docx",
                ConversionTime = TimeSpan.FromSeconds(2),
                OutputSizeBytes = 1024,
                SourceSizeBytes = 2048,
                CompletedAt = DateTime.UtcNow
            };

            // Act
            viewModel.Result = result;

            // Assert
            canExecuteChangedRaised.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void OpenPdfCommand_ShouldNavigateToPdfViewerPage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");

        try
        {
            var viewModel = CreateViewModel();
            var result = new ConversionResult
            {
                OutputPath = tempFile,
                SourcePath = "test.docx",
                ConversionTime = TimeSpan.FromSeconds(2),
                OutputSizeBytes = 1024,
                SourceSizeBytes = 2048,
                CompletedAt = DateTime.UtcNow
            };
            viewModel.Result = result;

            // Act
            viewModel.OpenPdfCommand.Execute(null);

            // Assert
            _navigationServiceMock.Verify(
                x => x.NavigateTo(typeof(PdfViewerPage), tempFile),
                Times.Once,
                "OpenPdfCommand should navigate to PdfViewerPage with output path");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ConvertCommand_ShouldCallConverterService_WithCorrectParameters()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";
        viewModel.EnableQualityValidation = true;

        var expectedResult = new ConversionResult
        {
            OutputPath = "output.pdf",
            SourcePath = "test.docx",
            ConversionTime = TimeSpan.FromSeconds(2),
            OutputSizeBytes = 1024,
            SourceSizeBytes = 2048,
            PageCount = 5,
            QualityScore = 0.95,
            CompletedAt = DateTime.UtcNow
        };

        _converterServiceMock
            .Setup(x => x.ConvertDocxToPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConversionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(expectedResult));

        // Act
        await viewModel.ConvertCommand.ExecuteAsync(null);

        // Assert
        _converterServiceMock.Verify(
            x => x.ConvertDocxToPdfAsync(
                "test.docx",
                "output.pdf",
                It.Is<ConversionOptions>(o => o.EnableQualityValidation == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConvertCommand_ShouldUpdateResult_WhenConversionSucceeds()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";

        var expectedResult = new ConversionResult
        {
            OutputPath = "output.pdf",
            SourcePath = "test.docx",
            ConversionTime = TimeSpan.FromSeconds(2),
            OutputSizeBytes = 1024,
            SourceSizeBytes = 2048,
            CompletedAt = DateTime.UtcNow
        };

        _converterServiceMock
            .Setup(x => x.ConvertDocxToPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConversionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(expectedResult));

        // Act
        await viewModel.ConvertCommand.ExecuteAsync(null);

        // Assert
        viewModel.Result.Should().Be(expectedResult);
        viewModel.HasResults.Should().BeTrue();
        viewModel.StatusMessage.Should().Contain("successfully");
        viewModel.IsConverting.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertCommand_ShouldHandleError_WhenConversionFails()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";

        _converterServiceMock
            .Setup(x => x.ConvertDocxToPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConversionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Conversion failed"));

        // Act
        await viewModel.ConvertCommand.ExecuteAsync(null);

        // Assert
        viewModel.Result.Should().BeNull();
        viewModel.HasResults.Should().BeFalse();
        viewModel.StatusMessage.Should().Contain("failed");
        viewModel.IsConverting.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertCommand_ShouldSetIsConvertingToTrue_DuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.DocxFilePath = "test.docx";
        viewModel.OutputFilePath = "output.pdf";
        var wasConvertingDuringExecution = false;

        var expectedResult = new ConversionResult
        {
            OutputPath = "output.pdf",
            SourcePath = "test.docx",
            ConversionTime = TimeSpan.FromSeconds(2),
            OutputSizeBytes = 1024,
            SourceSizeBytes = 2048,
            CompletedAt = DateTime.UtcNow
        };

        _converterServiceMock
            .Setup(x => x.ConvertDocxToPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ConversionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(expectedResult));

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ConversionViewModel.IsConverting) && viewModel.IsConverting)
                wasConvertingDuringExecution = true;
        };

        // Act
        await viewModel.ConvertCommand.ExecuteAsync(null);

        // Assert
        wasConvertingDuringExecution.Should().BeTrue();
        viewModel.IsConverting.Should().BeFalse("IsConverting should be reset after completion");
    }

    [Fact]
    public void ViewModel_ShouldBeTestableWithoutUIRuntime()
    {
        // This test verifies that the ViewModel can be instantiated and tested
        // without requiring WinUI runtime (headless testing)

        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ConversionViewModel initialized")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Constructor should log initialization");
    }

    private ConversionViewModel CreateViewModel()
    {
        return new ConversionViewModel(
            _converterServiceMock.Object,
            _navigationServiceMock.Object,
            _loggerMock.Object);
    }
}
