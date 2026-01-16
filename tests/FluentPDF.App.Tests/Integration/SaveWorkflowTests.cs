using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for save workflow including Save, Save As, and close confirmation.
/// These tests verify the complete workflow of tracking unsaved changes, saving documents,
/// and handling the interaction between TabViewModel and PdfViewerViewModel for save operations.
/// </summary>
public sealed class SaveWorkflowTests : IDisposable
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
    private readonly Mock<ILogger<TabViewModel>> _tabLoggerMock;
    private readonly Mock<ILogger<BookmarksViewModel>> _bookmarksLoggerMock;
    private readonly Mock<ILogger<FormFieldViewModel>> _formLoggerMock;
    private readonly Mock<ILogger<DiagnosticsPanelViewModel>> _diagnosticsLoggerMock;
    private readonly Mock<ILogger<LogViewerViewModel>> _logViewerLoggerMock;
    private readonly Mock<ILogger<AnnotationViewModel>> _annotationLoggerMock;
    private readonly Mock<ILogger<ThumbnailsViewModel>> _thumbnailsLoggerMock;
    private readonly Mock<IThumbnailRenderingService> _thumbnailRenderingServiceMock;
    private readonly Mock<IPageOperationsService> _pageOperationsServiceMock;
    private readonly Mock<IMetricsCollectionService> _metricsServiceMock;
    private readonly PdfDocument _testDocument;

    public SaveWorkflowTests()
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
        _thumbnailRenderingServiceMock = new Mock<IThumbnailRenderingService>();
        _pageOperationsServiceMock = new Mock<IPageOperationsService>();
        _metricsServiceMock = new Mock<IMetricsCollectionService>();
        _viewerLoggerMock = new Mock<ILogger<PdfViewerViewModel>>();
        _tabLoggerMock = new Mock<ILogger<TabViewModel>>();
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
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void TabViewModel_WhenHasUnsavedChanges_DisplayNameShouldShowAsterisk()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();

        // Simulate loaded document
        SetLoadedDocument(viewerViewModel, _testDocument);

        var tabViewModel = new TabViewModel(_testDocument.FilePath, viewerViewModel, _tabLoggerMock.Object);

        var fileName = Path.GetFileName(_testDocument.FilePath);
        tabViewModel.DisplayName.Should().Be(fileName, "initially no asterisk");

        // Act - Simulate annotation modification
        SetAnnotationUnsavedChanges(viewerViewModel, true);
        TriggerPropertyChanged(viewerViewModel.AnnotationViewModel, nameof(AnnotationViewModel.HasUnsavedChanges));

        // Assert
        tabViewModel.DisplayName.Should().Be($"*{fileName}", "should show asterisk when unsaved");

        // Cleanup
        tabViewModel.Dispose();
        viewerViewModel.Dispose();
    }

    [Fact]
    public async Task TabViewModel_AfterSave_DisplayNameShouldRemoveAsterisk()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();

        // Simulate loaded document
        SetLoadedDocument(viewerViewModel, _testDocument);

        var tabViewModel = new TabViewModel(_testDocument.FilePath, viewerViewModel, _tabLoggerMock.Object);

        // Setup save mocks to return success
        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<List<Annotation>>()))
            .ReturnsAsync(Result.Ok());

        _formServiceMock
            .Setup(x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Set unsaved changes
        SetAnnotationUnsavedChanges(viewerViewModel, true);
        TriggerPropertyChanged(viewerViewModel.AnnotationViewModel, nameof(AnnotationViewModel.HasUnsavedChanges));

        var fileName = Path.GetFileName(_testDocument.FilePath);
        tabViewModel.DisplayName.Should().Be($"*{fileName}");

        // Act - Save document
        await viewerViewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        tabViewModel.DisplayName.Should().Be(fileName, "asterisk should be removed after save");

        // Cleanup
        tabViewModel.Dispose();
        viewerViewModel.Dispose();
    }

    [Fact]
    public void HasUnsavedChanges_WhenAnnotationModified_ShouldBeTrue()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        viewerViewModel.HasUnsavedChanges.Should().BeFalse("initially no changes");

        // Act - Simulate annotation modification
        SetAnnotationUnsavedChanges(viewerViewModel, true);

        // Assert
        viewerViewModel.HasUnsavedChanges.Should().BeTrue("annotation was modified");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public void HasUnsavedChanges_WhenFormModified_ShouldBeTrue()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        viewerViewModel.HasUnsavedChanges.Should().BeFalse("initially no changes");

        // Act - Simulate form field modification
        SetFormFieldModified(viewerViewModel, true);

        // Assert
        viewerViewModel.HasUnsavedChanges.Should().BeTrue("form field was modified");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public async Task SaveWorkflow_ModifySaveModifyAgain_ShouldTrackCorrectly()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        // Setup save mocks to return success
        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<List<Annotation>>()))
            .ReturnsAsync(Result.Ok());

        _formServiceMock
            .Setup(x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Act & Assert - First modification
        SetAnnotationUnsavedChanges(viewerViewModel, true);
        viewerViewModel.HasUnsavedChanges.Should().BeTrue("after first modification");

        // Save
        await viewerViewModel.SaveCommand.ExecuteAsync(null);
        viewerViewModel.HasUnsavedChanges.Should().BeFalse("after save");

        // Second modification
        SetFormFieldModified(viewerViewModel, true);
        viewerViewModel.HasUnsavedChanges.Should().BeTrue("after second modification");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public async Task SaveCommand_WhenHasUnsavedChanges_ShouldCallBothServices()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        // Setup save mocks to return success
        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<List<Annotation>>()))
            .ReturnsAsync(Result.Ok());

        _formServiceMock
            .Setup(x => x.SaveFormDataAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        // Set both annotation and form as modified
        SetAnnotationUnsavedChanges(viewerViewModel, true);
        SetFormFieldModified(viewerViewModel, true);

        // Act
        await viewerViewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _annotationServiceMock.Verify(
            x => x.SaveAnnotationsAsync(_testDocument, It.IsAny<List<Annotation>>()),
            Times.Once,
            "SaveAnnotationsAsync should be called");

        _formServiceMock.Verify(
            x => x.SaveFormDataAsync(_testDocument, _testDocument.FilePath),
            Times.Once,
            "SaveFormDataAsync should be called");

        viewerViewModel.HasUnsavedChanges.Should().BeFalse("save should clear unsaved flag");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public async Task SaveCommand_WhenSaveFails_ShouldMaintainUnsavedFlag()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        // Setup save mock to return failure
        _annotationServiceMock
            .Setup(x => x.SaveAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<List<Annotation>>()))
            .ReturnsAsync(Result.Fail("Save failed"));

        SetAnnotationUnsavedChanges(viewerViewModel, true);

        // Act
        await viewerViewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        viewerViewModel.HasUnsavedChanges.Should().BeTrue("save failure should maintain unsaved flag");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public void SaveCommand_WhenNoUnsavedChanges_ShouldNotExecute()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        // Act
        var canExecute = viewerViewModel.SaveCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeFalse("Save should be disabled when no changes");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public void SaveAsCommand_WhenDocumentLoaded_ShouldBeExecutable()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        // Act
        var canExecute = viewerViewModel.SaveAsCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue("SaveAs should always be available when document is loaded");

        // Cleanup
        viewerViewModel.Dispose();
    }

    [Fact]
    public void TabViewModel_PropertyChanged_ShouldPropagateFromViewerViewModel()
    {
        // Arrange
        var viewerViewModel = CreatePdfViewerViewModel();
        SetLoadedDocument(viewerViewModel, _testDocument);

        var tabViewModel = new TabViewModel(_testDocument.FilePath, viewerViewModel, _tabLoggerMock.Object);

        var propertyChangedEvents = new List<string>();
        tabViewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act - Trigger HasUnsavedChanges change
        SetAnnotationUnsavedChanges(viewerViewModel, true);
        TriggerPropertyChanged(viewerViewModel.AnnotationViewModel, nameof(AnnotationViewModel.HasUnsavedChanges));

        // Assert
        propertyChangedEvents.Should().Contain(nameof(TabViewModel.DisplayName),
            "DisplayName should update when HasUnsavedChanges changes");

        // Cleanup
        tabViewModel.Dispose();
        viewerViewModel.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a PdfViewerViewModel with all dependencies mocked.
    /// </summary>
    private PdfViewerViewModel CreatePdfViewerViewModel()
    {
        var bookmarksViewModel = new BookmarksViewModel(
            _bookmarkServiceMock.Object,
            _bookmarksLoggerMock.Object);

        var formFieldViewModel = new FormFieldViewModel(
            _formServiceMock.Object,
            _formValidationServiceMock.Object,
            _formLoggerMock.Object);

        var diagnosticsPanelViewModel = new DiagnosticsPanelViewModel(
            _metricsServiceMock.Object,
            _diagnosticsLoggerMock.Object);

        var logViewerViewModel = new LogViewerViewModel(
            _logViewerLoggerMock.Object);

        var annotationViewModel = new AnnotationViewModel(
            _annotationServiceMock.Object,
            _annotationLoggerMock.Object);

        var thumbnailsViewModel = new ThumbnailsViewModel(
            _thumbnailRenderingServiceMock.Object,
            _pageOperationsServiceMock.Object,
            _thumbnailsLoggerMock.Object);

        return new PdfViewerViewModel(
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

    /// <summary>
    /// Sets the internal _currentDocument field to simulate a loaded document.
    /// </summary>
    private void SetLoadedDocument(PdfViewerViewModel viewModel, PdfDocument document)
    {
        var documentField = typeof(PdfViewerViewModel)
            .GetField("_currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField!.SetValue(viewModel, document);
    }

    /// <summary>
    /// Sets the HasUnsavedChanges property on AnnotationViewModel.
    /// </summary>
    private void SetAnnotationUnsavedChanges(PdfViewerViewModel viewModel, bool value)
    {
        var propertyInfo = typeof(AnnotationViewModel)
            .GetProperty(nameof(AnnotationViewModel.HasUnsavedChanges));
        propertyInfo!.SetValue(viewModel.AnnotationViewModel, value);
    }

    /// <summary>
    /// Sets the IsModified property on FormFieldViewModel.
    /// </summary>
    private void SetFormFieldModified(PdfViewerViewModel viewModel, bool value)
    {
        var propertyInfo = typeof(FormFieldViewModel)
            .GetProperty(nameof(FormFieldViewModel.IsModified));
        propertyInfo!.SetValue(viewModel.FormFieldViewModel, value);
    }

    /// <summary>
    /// Triggers PropertyChanged event on a view model.
    /// </summary>
    private void TriggerPropertyChanged(object viewModel, string propertyName)
    {
        var onPropertyChangedMethod = viewModel.GetType()
            .GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onPropertyChangedMethod?.Invoke(viewModel, new object[] { propertyName });
    }

    #endregion
}
