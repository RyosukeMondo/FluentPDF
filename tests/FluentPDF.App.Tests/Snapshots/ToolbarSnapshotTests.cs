using FluentPDF.App.ViewModels;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Moq;

namespace FluentPDF.App.Tests.Snapshots;

/// <summary>
/// Snapshot tests for toolbar controls.
/// Verifies visual appearance in different states to detect regressions.
/// </summary>
public class ToolbarSnapshotTests : SnapshotTestBase
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

    public ToolbarSnapshotTests()
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
    /// Creates a PdfViewerViewModel for testing toolbar states.
    /// </summary>
    private PdfViewerViewModel CreateViewModel()
    {
        return new PdfViewerViewModel(
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
    }

    /// <summary>
    /// Creates a CommandBar with basic navigation buttons for testing.
    /// Mimics the main toolbar structure from PdfViewerPage.
    /// </summary>
    private CommandBar CreateMainToolbar(PdfViewerViewModel viewModel)
    {
        var toolbar = new CommandBar
        {
            DefaultLabelPosition = CommandBarDefaultLabelPosition.Right,
            Width = 800
        };

        // Open File button
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.OpenFile),
            Label = "Open"
        });

        toolbar.PrimaryCommands.Add(new AppBarSeparator());

        // Navigation - Previous
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.Previous),
            Label = "Previous"
        });

        // Page indicator
        var pageIndicator = new AppBarElementContainer();
        var pageTextBlock = new TextBlock
        {
            Text = $"Page {viewModel.CurrentPageNumber} of {viewModel.TotalPages}",
            Margin = new Microsoft.UI.Xaml.Thickness(12, 0, 12, 0),
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
        };
        pageIndicator.Content = pageTextBlock;
        toolbar.PrimaryCommands.Add(pageIndicator);

        // Navigation - Next
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.Next),
            Label = "Next"
        });

        toolbar.PrimaryCommands.Add(new AppBarSeparator());

        // Thumbnails
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE8FD" },
            Label = "Thumbnails"
        });

        // Bookmarks
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.AlignLeft),
            Label = "Bookmarks"
        });

        // Search
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new SymbolIcon(Symbol.Find),
            Label = "Search"
        });

        toolbar.PrimaryCommands.Add(new AppBarSeparator());

        // Zoom Out
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE71F" },
            Label = "Zoom Out"
        });

        // Zoom Level indicator
        var zoomIndicator = new AppBarElementContainer();
        var zoomTextBlock = new TextBlock
        {
            Text = $"{viewModel.ZoomLevel:P0}",
            Margin = new Microsoft.UI.Xaml.Thickness(12, 0, 12, 0),
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            MinWidth = 50,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center
        };
        zoomIndicator.Content = zoomTextBlock;
        toolbar.PrimaryCommands.Add(zoomIndicator);

        // Zoom In
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE71E" },
            Label = "Zoom In"
        });

        // Reset Zoom
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE8A0" },
            Label = "Reset Zoom"
        });

        return toolbar;
    }

    /// <summary>
    /// Creates a CommandBar with annotation tools for testing.
    /// Mimics the annotation toolbar structure from PdfViewerPage.
    /// </summary>
    private CommandBar CreateAnnotationToolbar()
    {
        var toolbar = new CommandBar
        {
            DefaultLabelPosition = CommandBarDefaultLabelPosition.Right,
            Width = 800
        };

        // Highlight
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE7E6" },
            Label = "Highlight"
        });

        // Underline
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE8DC" },
            Label = "Underline"
        });

        // Strikethrough
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uEDE0" },
            Label = "Strikethrough"
        });

        toolbar.PrimaryCommands.Add(new AppBarSeparator());

        // Comment
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE90A" },
            Label = "Comment"
        });

        toolbar.PrimaryCommands.Add(new AppBarSeparator());

        // Rectangle
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE761" },
            Label = "Rectangle"
        });

        // Circle
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uEA3A" },
            Label = "Circle"
        });

        // Freehand
        toolbar.PrimaryCommands.Add(new AppBarButton
        {
            Icon = new FontIcon { Glyph = "\uE76D" },
            Label = "Freehand"
        });

        return toolbar;
    }

    /// <summary>
    /// Verifies the main toolbar in its default state (no document loaded).
    /// </summary>
    [Fact]
    public Task MainToolbar_DefaultState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var toolbar = CreateMainToolbar(viewModel);

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_DefaultState");
    }

    /// <summary>
    /// Verifies the main toolbar when a document is loaded.
    /// </summary>
    [Fact]
    public Task MainToolbar_DocumentLoaded()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate document loaded
        viewModel.GetType().GetProperty("TotalPages")?.SetValue(viewModel, 10);
        viewModel.GetType().GetProperty("CurrentPageNumber")?.SetValue(viewModel, 1);

        var toolbar = CreateMainToolbar(viewModel);

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_DocumentLoaded");
    }

    /// <summary>
    /// Verifies the main toolbar when zoomed in.
    /// </summary>
    [Fact]
    public Task MainToolbar_ZoomedIn()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate document loaded and zoomed
        viewModel.GetType().GetProperty("TotalPages")?.SetValue(viewModel, 10);
        viewModel.GetType().GetProperty("CurrentPageNumber")?.SetValue(viewModel, 5);
        viewModel.GetType().GetProperty("ZoomLevel")?.SetValue(viewModel, 1.5);

        var toolbar = CreateMainToolbar(viewModel);

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_ZoomedIn");
    }

    /// <summary>
    /// Verifies the main toolbar with disabled commands.
    /// Tests when no document is loaded and commands should be disabled.
    /// </summary>
    [Fact]
    public Task MainToolbar_CommandsDisabled()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var toolbar = CreateMainToolbar(viewModel);

        // Disable navigation buttons (simulating no document loaded)
        foreach (var command in toolbar.PrimaryCommands)
        {
            if (command is AppBarButton button)
            {
                if (button.Label == "Previous" || button.Label == "Next")
                {
                    button.IsEnabled = false;
                }
            }
        }

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_CommandsDisabled");
    }

    /// <summary>
    /// Verifies the annotation toolbar in its default state.
    /// </summary>
    [Fact]
    public Task AnnotationToolbar_DefaultState()
    {
        // Arrange
        var toolbar = CreateAnnotationToolbar();

        // Act & Assert
        return VerifyControl(toolbar, "AnnotationToolbar_DefaultState");
    }

    /// <summary>
    /// Verifies the annotation toolbar with a tool selected.
    /// </summary>
    [Fact]
    public Task AnnotationToolbar_ToolSelected()
    {
        // Arrange
        var toolbar = CreateAnnotationToolbar();

        // Simulate Highlight tool selected by toggling first button
        if (toolbar.PrimaryCommands[0] is AppBarButton highlightButton)
        {
            highlightButton.IsCompact = false;
        }

        // Act & Assert
        return VerifyControl(toolbar, "AnnotationToolbar_ToolSelected");
    }

    /// <summary>
    /// Verifies the annotation toolbar with all tools disabled.
    /// </summary>
    [Fact]
    public Task AnnotationToolbar_AllDisabled()
    {
        // Arrange
        var toolbar = CreateAnnotationToolbar();

        // Disable all buttons
        foreach (var command in toolbar.PrimaryCommands)
        {
            if (command is AppBarButton button)
            {
                button.IsEnabled = false;
            }
        }

        // Act & Assert
        return VerifyControl(toolbar, "AnnotationToolbar_AllDisabled");
    }

    /// <summary>
    /// Verifies the main toolbar at minimum width.
    /// Tests responsive behavior.
    /// </summary>
    [Fact]
    public Task MainToolbar_MinimumWidth()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var toolbar = CreateMainToolbar(viewModel);
        toolbar.Width = 400;

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_MinimumWidth");
    }

    /// <summary>
    /// Verifies the main toolbar at maximum width.
    /// </summary>
    [Fact]
    public Task MainToolbar_MaximumWidth()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var toolbar = CreateMainToolbar(viewModel);
        toolbar.Width = 1200;

        // Act & Assert
        return VerifyControl(toolbar, "MainToolbar_MaximumWidth");
    }

    /// <summary>
    /// Verifies the annotation toolbar with compact mode.
    /// </summary>
    [Fact]
    public Task AnnotationToolbar_CompactMode()
    {
        // Arrange
        var toolbar = CreateAnnotationToolbar();

        // Set compact mode on all buttons
        foreach (var command in toolbar.PrimaryCommands)
        {
            if (command is AppBarButton button)
            {
                button.IsCompact = true;
            }
        }

        // Act & Assert
        return VerifyControl(toolbar, "AnnotationToolbar_CompactMode");
    }
}
