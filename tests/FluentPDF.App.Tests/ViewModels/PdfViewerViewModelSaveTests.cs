using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for PdfViewerViewModel save functionality.
/// Verifies SaveCommand, SaveAsCommand, and HasUnsavedChanges behavior.
/// </summary>
public class PdfViewerViewModelSaveTests : IDisposable
{
    private readonly Mock<IPdfDocumentService> _documentServiceMock;
    private readonly Mock<IPdfRenderingService> _renderingServiceMock;
    private readonly Mock<IDocumentEditingService> _editingServiceMock;
    private readonly Mock<ITextSearchService> _searchServiceMock;
    private readonly Mock<ITextExtractionService> _textExtractionServiceMock;
    private readonly Mock<IBookmarkService> _bookmarkServiceMock;
    private readonly Mock<IPdfFormService> _formServiceMock;
    private readonly Mock<IFormValidationService> _formValidationServiceMock;
    private readonly Mock<IAnnotationService> _annotationServiceMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _viewerLoggerMock;
    private readonly Mock<ILogger<BookmarksViewModel>> _bookmarksLoggerMock;
    private readonly Mock<ILogger<FormFieldViewModel>> _formLoggerMock;
    private readonly Mock<ILogger<DiagnosticsPanelViewModel>> _diagnosticsLoggerMock;
    private readonly Mock<ILogger<LogViewerViewModel>> _logViewerLoggerMock;
    private readonly Mock<ILogger<AnnotationViewModel>> _annotationLoggerMock;
    private readonly Mock<ILogger<ThumbnailsViewModel>> _thumbnailsLoggerMock;
    private readonly PdfViewerViewModel _viewModel;
    private readonly PdfDocument _testDocument;

    public PdfViewerViewModelSaveTests()
    {
        _documentServiceMock = new Mock<IPdfDocumentService>();
        _renderingServiceMock = new Mock<IPdfRenderingService>();
        _editingServiceMock = new Mock<IDocumentEditingService>();
        _searchServiceMock = new Mock<ITextSearchService>();
        _textExtractionServiceMock = new Mock<ITextExtractionService>();
        _bookmarkServiceMock = new Mock<IBookmarkService>();
        _formServiceMock = new Mock<IPdfFormService>();
        _formValidationServiceMock = new Mock<IFormValidationService>();
        _annotationServiceMock = new Mock<IAnnotationService>();
        _viewerLoggerMock = new Mock<ILogger<PdfViewerViewModel>>();
        _bookmarksLoggerMock = new Mock<ILogger<BookmarksViewModel>>();
        _formLoggerMock = new Mock<ILogger<FormFieldViewModel>>();
        _diagnosticsLoggerMock = new Mock<ILogger<DiagnosticsPanelViewModel>>();
        _logViewerLoggerMock = new Mock<ILogger<LogViewerViewModel>>();
        _annotationLoggerMock = new Mock<ILogger<AnnotationViewModel>>();
        _thumbnailsLoggerMock = new Mock<ILogger<ThumbnailsViewModel>>();

        // Create test document
        var mockHandle = new Mock<IDisposable>();
        _testDocument = new PdfDocument
        {
            Handle = mockHandle.Object,
            FilePath = "/path/to/test.pdf",
            PageCount = 5,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };

        // Create child view models
        var bookmarksViewModel = new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _bookmarksLoggerMock.Object);

        var formFieldViewModel = new FormFieldViewModel(
            _formServiceMock.Object,
            _formValidationServiceMock.Object,
            _formLoggerMock.Object);

        var diagnosticsPanelViewModel = new DiagnosticsPanelViewModel(
            _diagnosticsLoggerMock.Object);

        var logViewerViewModel = new LogViewerViewModel(
            _logViewerLoggerMock.Object);

        var annotationViewModel = new AnnotationViewModel(
            _annotationServiceMock.Object,
            _annotationLoggerMock.Object);

        var thumbnailsViewModel = new ThumbnailsViewModel(
            _renderingServiceMock.Object,
            _thumbnailsLoggerMock.Object);

        _viewModel = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _editingServiceMock.Object,
            _searchServiceMock.Object,
            _textExtractionServiceMock.Object,
            bookmarksViewModel,
            formFieldViewModel,
            diagnosticsPanelViewModel,
            logViewerViewModel,
            annotationViewModel,
            thumbnailsViewModel,
            null, // metricsService
            null, // dpiDetectionService
            null, // renderingSettingsService
            null, // settingsService
            Mock.Of<FluentPDF.App.Services.RenderingCoordinator>(),
            Mock.Of<FluentPDF.App.Services.UIBindingVerifier>(),
            _viewerLoggerMock.Object);
    }

    [Fact]
    public void HasUnsavedChanges_WhenAnnotationModified_ReturnsTrue()
    {
        // Arrange
        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            PageNumber = 1,
            Type = AnnotationType.Text,
            Content = "Test annotation"
        };

        _viewModel.AnnotationViewModel.Annotations.Add(annotation);

        // Manually set HasUnsavedChanges on AnnotationViewModel to simulate unsaved changes
        var propertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        propertyInfo!.SetValue(_viewModel.AnnotationViewModel, true);

        // Act
        var hasUnsavedChanges = _viewModel.HasUnsavedChanges;

        // Assert
        hasUnsavedChanges.Should().BeTrue("annotation has unsaved changes");
    }

    [Fact]
    public void HasUnsavedChanges_WhenFormFieldModified_ReturnsTrue()
    {
        // Arrange - Manually set IsModified on FormFieldViewModel
        var propertyInfo = typeof(FormFieldViewModel)
            .GetProperty(nameof(FormFieldViewModel.IsModified));
        propertyInfo!.SetValue(_viewModel.FormFieldViewModel, true);

        // Act
        var hasUnsavedChanges = _viewModel.HasUnsavedChanges;

        // Assert
        hasUnsavedChanges.Should().BeTrue("form field has been modified");
    }

    [Fact]
    public void HasUnsavedChanges_WhenNoChanges_ReturnsFalse()
    {
        // Act
        var hasUnsavedChanges = _viewModel.HasUnsavedChanges;

        // Assert
        hasUnsavedChanges.Should().BeFalse("no changes have been made");
    }

    [Fact]
    public void HasUnsavedChanges_PropertyChanged_IsRaisedWhenAnnotationChanges()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.HasUnsavedChanges))
                propertyChangedEvents.Add(e.PropertyName);
        };

        // Act - Simulate AnnotationViewModel property change
        var annotationPropertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        annotationPropertyInfo!.SetValue(_viewModel.AnnotationViewModel, true);

        // Raise PropertyChanged on AnnotationViewModel
        var onPropertyChangedMethod = typeof(AnnotationViewModel)
            .GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onPropertyChangedMethod!.Invoke(_viewModel.AnnotationViewModel, new[] { nameof(AnnotationViewModel.HasUnsavedChanges) });

        // Assert
        propertyChangedEvents.Should().Contain(nameof(PdfViewerViewModel.HasUnsavedChanges));
    }

    [Fact]
    public void HasUnsavedChanges_PropertyChanged_IsRaisedWhenFormFieldChanges()
    {
        // Arrange
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.HasUnsavedChanges))
                propertyChangedEvents.Add(e.PropertyName);
        };

        // Act - Simulate FormFieldViewModel property change
        var formPropertyInfo = typeof(FormFieldViewModel)
            .GetProperty(nameof(FormFieldViewModel.IsModified));
        formPropertyInfo!.SetValue(_viewModel.FormFieldViewModel, true);

        // Raise PropertyChanged on FormFieldViewModel
        var onPropertyChangedMethod = typeof(FormFieldViewModel)
            .GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onPropertyChangedMethod!.Invoke(_viewModel.FormFieldViewModel, new[] { nameof(FormFieldViewModel.IsModified) });

        // Assert
        propertyChangedEvents.Should().Contain(nameof(PdfViewerViewModel.HasUnsavedChanges));
    }

    [Fact]
    public void SaveCommand_CanExecute_WhenNoDocument_ReturnsFalse()
    {
        // Act
        var canExecute = _viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("no document is loaded");
    }

    [Fact]
    public void SaveCommand_CanExecute_WhenNoUnsavedChanges_ReturnsFalse()
    {
        // Arrange - Load a document but don't make any changes
        _documentServiceMock
            .Setup(x => x.LoadDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(_testDocument));

        // Act
        var canExecute = _viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("there are no unsaved changes");
    }

    [Fact]
    public void SaveCommand_CanExecute_WhenHasUnsavedChangesAndDocumentLoaded_ReturnsTrue()
    {
        // Arrange - Simulate loaded document by setting internal field
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(_viewModel, _testDocument);

        // Set unsaved changes on annotation
        var annotationPropertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        annotationPropertyInfo!.SetValue(_viewModel.AnnotationViewModel, true);

        // Act
        var canExecute = _viewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("document is loaded and has unsaved changes");
    }

    [Fact]
    public async Task SaveCommand_WhenExecuted_CallsAnnotationServiceSave()
    {
        // Arrange - Simulate loaded document
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(_viewModel, _testDocument);

        // Set annotation to have unsaved changes
        var annotationPropertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        annotationPropertyInfo!.SetValue(_viewModel.AnnotationViewModel, true);

        // Setup annotation service to return success
        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<List<Annotation>>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _annotationServiceMock.Verify(
            x => x.SaveAnnotationsAsync(_testDocument, It.IsAny<List<Annotation>>()),
            Times.Once,
            "SaveAnnotationsAsync should be called when annotations have unsaved changes");
    }

    [Fact]
    public async Task SaveCommand_WhenExecuted_CallsFormServiceSave()
    {
        // Arrange - Simulate loaded document
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(_viewModel, _testDocument);

        // Set form to be modified
        var formPropertyInfo = typeof(FormFieldViewModel)
            .GetProperty(nameof(FormFieldViewModel.IsModified));
        formPropertyInfo!.SetValue(_viewModel.FormFieldViewModel, true);

        // Setup form service to return success
        _formServiceMock
            .Setup(x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _formServiceMock.Verify(
            x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), _testDocument.FilePath),
            Times.Once,
            "SaveFormDataAsync should be called when form has been modified");
    }

    [Fact]
    public void SaveAsCommand_CanExecute_WhenNoDocument_ReturnsFalse()
    {
        // Act
        var canExecute = _viewModel.SaveAsCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("no document is loaded");
    }

    [Fact]
    public void SaveAsCommand_CanExecute_WhenDocumentLoaded_ReturnsTrue()
    {
        // Arrange - Simulate loaded document
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(_viewModel, _testDocument);

        // Act
        var canExecute = _viewModel.SaveAsCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("document is loaded");
    }

    [Fact]
    public void SaveCommand_NotifyCanExecuteChanged_WhenHasUnsavedChangesChanges()
    {
        // Arrange - Simulate loaded document
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(_viewModel, _testDocument);

        var canExecuteChangedCount = 0;
        _viewModel.SaveCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

        // Act - Simulate HasUnsavedChanges changing
        var annotationPropertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        annotationPropertyInfo!.SetValue(_viewModel.AnnotationViewModel, true);

        // Trigger the property changed event
        var onPropertyChangedMethod = typeof(PdfViewerViewModel)
            .GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onPropertyChangedMethod!.Invoke(_viewModel, new object[] {
            new System.ComponentModel.PropertyChangedEventArgs(nameof(PdfViewerViewModel.HasUnsavedChanges))
        });

        // Assert
        canExecuteChangedCount.Should().BeGreaterThan(0, "CanExecuteChanged should be raised when HasUnsavedChanges changes");
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }
}
