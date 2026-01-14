using FluentPDF.App.Controls;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Moq;

namespace FluentPDF.App.Tests.Snapshots;

/// <summary>
/// Snapshot tests for PdfViewerControl.
/// Verifies visual appearance in different states to detect regressions.
/// </summary>
public class PdfViewerSnapshotTests : SnapshotTestBase
{
    private readonly Mock<IPdfDocumentService> _mockDocumentService;
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly Mock<IDocumentEditingService> _mockEditingService;
    private readonly Mock<ITextSearchService> _mockSearchService;
    private readonly Mock<ITextExtractionService> _mockTextExtractionService;
    private readonly Mock<BookmarksViewModel> _mockBookmarksViewModel;
    private readonly Mock<FormFieldViewModel> _mockFormFieldViewModel;
    private readonly Mock<DiagnosticsPanelViewModel> _mockDiagnosticsPanelViewModel;
    private readonly Mock<LogViewerViewModel> _mockLogViewerViewModel;
    private readonly Mock<AnnotationViewModel> _mockAnnotationViewModel;
    private readonly Mock<ThumbnailsViewModel> _mockThumbnailsViewModel;
    private readonly Mock<ImageInsertionViewModel> _mockImageInsertionViewModel;
    private readonly Mock<ILogger<PdfViewerViewModel>> _mockLogger;

    public PdfViewerSnapshotTests()
    {
        _mockDocumentService = new Mock<IPdfDocumentService>();
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _mockEditingService = new Mock<IDocumentEditingService>();
        _mockSearchService = new Mock<ITextSearchService>();
        _mockTextExtractionService = new Mock<ITextExtractionService>();
        _mockLogger = new Mock<ILogger<PdfViewerViewModel>>();

        // Create lightweight mocks for child ViewModels
        _mockBookmarksViewModel = new Mock<BookmarksViewModel>(
            Mock.Of<IPdfDocumentService>(),
            Mock.Of<ILogger<BookmarksViewModel>>());

        _mockFormFieldViewModel = new Mock<FormFieldViewModel>(
            Mock.Of<IFormFieldService>(),
            Mock.Of<ILogger<FormFieldViewModel>>());

        _mockDiagnosticsPanelViewModel = new Mock<DiagnosticsPanelViewModel>(
            Mock.Of<Core.Services.IMetricsCollectionService>(),
            Mock.Of<ILogger<DiagnosticsPanelViewModel>>());

        _mockLogViewerViewModel = new Mock<LogViewerViewModel>(
            Mock.Of<ILogger<LogViewerViewModel>>());

        _mockAnnotationViewModel = new Mock<AnnotationViewModel>(
            Mock.Of<IAnnotationService>(),
            Mock.Of<ILogger<AnnotationViewModel>>());

        _mockThumbnailsViewModel = new Mock<ThumbnailsViewModel>(
            Mock.Of<IPdfDocumentService>(),
            Mock.Of<IPdfRenderingService>(),
            Mock.Of<ILogger<ThumbnailsViewModel>>());

        _mockImageInsertionViewModel = new Mock<ImageInsertionViewModel>(
            Mock.Of<IImageInsertionService>(),
            Mock.Of<ILogger<ImageInsertionViewModel>>());
    }

    /// <summary>
    /// Creates a PdfViewerControl with a ViewModel for testing.
    /// </summary>
    private PdfViewerControl CreateControl()
    {
        var viewModel = new PdfViewerViewModel(
            _mockDocumentService.Object,
            _mockRenderingService.Object,
            _mockEditingService.Object,
            _mockSearchService.Object,
            _mockTextExtractionService.Object,
            _mockBookmarksViewModel.Object,
            _mockFormFieldViewModel.Object,
            _mockDiagnosticsPanelViewModel.Object,
            _mockLogViewerViewModel.Object,
            _mockAnnotationViewModel.Object,
            _mockThumbnailsViewModel.Object,
            _mockImageInsertionViewModel.Object,
            null, // Optional metrics service
            null, // Optional DPI detection service
            null, // Optional rendering settings service
            null, // Optional settings service
            _mockLogger.Object);

        var control = new PdfViewerControl
        {
            ViewerViewModel = viewModel,
            Width = 800,
            Height = 600
        };

        return control;
    }

    /// <summary>
    /// Verifies the default state of PdfViewerControl with no document loaded.
    /// </summary>
    [Fact]
    public Task DefaultState_NoDocumentLoaded()
    {
        // Arrange
        var control = CreateControl();

        // Act & Assert
        return VerifyControl(control, "DefaultState");
    }

    /// <summary>
    /// Verifies PdfViewerControl layout at default size (800x600).
    /// </summary>
    [Fact]
    public Task ControlLayout_DefaultSize()
    {
        // Arrange
        var control = CreateControl();
        control.Width = 800;
        control.Height = 600;

        // Act & Assert
        return VerifyControl(control, "DefaultSize");
    }

    /// <summary>
    /// Verifies PdfViewerControl layout at small size (400x300).
    /// </summary>
    [Fact]
    public Task ControlLayout_SmallSize()
    {
        // Arrange
        var control = CreateControl();
        control.Width = 400;
        control.Height = 300;

        // Act & Assert
        return VerifyControl(control, "SmallSize");
    }

    /// <summary>
    /// Verifies PdfViewerControl layout at large size (1200x900).
    /// </summary>
    [Fact]
    public Task ControlLayout_LargeSize()
    {
        // Arrange
        var control = CreateControl();
        control.Width = 1200;
        control.Height = 900;

        // Act & Assert
        return VerifyControl(control, "LargeSize");
    }

    /// <summary>
    /// Verifies PdfViewerControl with ViewModel properties set.
    /// Tests that the control responds to ViewModel state changes.
    /// </summary>
    [Fact]
    public Task ViewModelState_LoadingIndicator()
    {
        // Arrange
        var control = CreateControl();
        if (control.ViewerViewModel != null)
        {
            // Simulate loading state
            control.ViewerViewModel.GetType()
                .GetProperty("IsLoading")
                ?.SetValue(control.ViewerViewModel, true);
            control.ViewerViewModel.GetType()
                .GetProperty("StatusMessage")
                ?.SetValue(control.ViewerViewModel, "Loading document...");
        }

        // Act & Assert
        return VerifyControl(control, "LoadingState");
    }

    /// <summary>
    /// Verifies PdfViewerControl XAML structure and hierarchy.
    /// </summary>
    [Fact]
    public Task XamlStructure_Hierarchy()
    {
        // Arrange
        var control = CreateControl();

        // Act - Load the control (triggers InitializeComponent)
        control.UpdateLayout();

        // Assert
        return VerifyControl(control, "XamlHierarchy");
    }

    /// <summary>
    /// Verifies PdfViewerControl with null ViewModel.
    /// Tests that the control handles null ViewModel gracefully.
    /// </summary>
    [Fact]
    public Task NullViewModel_HandledGracefully()
    {
        // Arrange
        var control = new PdfViewerControl
        {
            ViewerViewModel = null,
            Width = 800,
            Height = 600
        };

        // Act & Assert
        return VerifyControl(control, "NullViewModel");
    }

    /// <summary>
    /// Verifies PdfViewerControl visibility states.
    /// </summary>
    [Fact]
    public Task VisibilityState_Hidden()
    {
        // Arrange
        var control = CreateControl();
        control.Visibility = Visibility.Collapsed;

        // Act & Assert
        return VerifyControl(control, "Hidden");
    }

    /// <summary>
    /// Verifies PdfViewerControl with minimum dimensions.
    /// </summary>
    [Fact]
    public Task ControlLayout_MinimumSize()
    {
        // Arrange
        var control = CreateControl();
        control.Width = 100;
        control.Height = 100;
        control.MinWidth = 100;
        control.MinHeight = 100;

        // Act & Assert
        return VerifyControl(control, "MinimumSize");
    }
}
