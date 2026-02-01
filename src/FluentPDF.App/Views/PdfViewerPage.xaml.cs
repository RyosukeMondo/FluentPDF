using CommunityToolkit.Mvvm.Messaging;
using FluentPDF.App.Helpers;
using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace FluentPDF.App.Views;

/// <summary>
/// PDF viewer page that displays PDF documents with navigation and zoom controls.
/// Implements data binding to PdfViewerViewModel following MVVM pattern.
/// </summary>
public sealed partial class PdfViewerPage : Page, IDisposable
{
    /// <summary>
    /// Gets the view model for this page.
    /// </summary>
    public PdfViewerViewModel ViewModel { get; }

    // Panning state for middle-mouse button drag
    private bool _isPanning;
    private Windows.Foundation.Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    // Text selection state
    private Windows.Foundation.Point _selectionStartPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfViewerPage"/> class.
    /// </summary>
    /// <param name="viewModel">Optional view model to use. If null, creates from DI container.</param>
    public PdfViewerPage(PdfViewerViewModel? viewModel = null)
    {
        this.InitializeComponent();

        // Use provided ViewModel or resolve from DI container
        if (viewModel != null)
        {
            ViewModel = viewModel;
        }
        else
        {
            ViewModel = App.GetService<PdfViewerViewModel>();
        }

        // Set DataContext for runtime binding (x:Bind doesn't need this, but good practice)
        this.DataContext = ViewModel;

        // Hook up keyboard handlers for form field navigation and page navigation
        this.KeyDown += OnPageKeyDown;

        // Set up keyboard accelerators for page navigation (Task 2.2)
        SetupPageNavigationAccelerators();

        // Hook up event handler for search panel visibility changes
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Hook up event handler for annotation tool changes (Task 2.3)
        if (ViewModel.AnnotationViewModel != null)
        {
            ViewModel.AnnotationViewModel.PropertyChanged += OnAnnotationViewModelPropertyChanged;
        }

        // Hook up lifecycle events for DPI monitoring
        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;

        // Register for accessibility notification messages
        WeakReferenceMessenger.Default.Register<AccessibilityNotificationMessage>(this, OnAccessibilityNotification);

        // Hook up mouse wheel and middle-button panning events (will be attached in OnPageLoaded)
    }

    /// <summary>
    /// Sets up keyboard accelerators for page navigation and zoom controls.
    /// Implements Phase 2: Keyboard Navigation (Tasks 2.2, 2.3).
    /// </summary>
    private void SetupPageNavigationAccelerators()
    {
        // Page Up: Previous page
        var pageUpAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.PageUp
        };
        pageUpAccelerator.Invoked += OnPageUpAccelerator;
        this.KeyboardAccelerators.Add(pageUpAccelerator);

        // Page Down: Next page
        var pageDownAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.PageDown
        };
        pageDownAccelerator.Invoked += OnPageDownAccelerator;
        this.KeyboardAccelerators.Add(pageDownAccelerator);

        // Home: First page
        var homeAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Home
        };
        homeAccelerator.Invoked += OnHomeAccelerator;
        this.KeyboardAccelerators.Add(homeAccelerator);

        // End: Last page
        var endAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.End
        };
        endAccelerator.Invoked += OnEndAccelerator;
        this.KeyboardAccelerators.Add(endAccelerator);

        // Ctrl+G: Go to page dialog
        var goToPageAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.G,
            Modifiers = VirtualKeyModifiers.Control
        };
        goToPageAccelerator.Invoked += OnGoToPageAccelerator;
        this.KeyboardAccelerators.Add(goToPageAccelerator);

        // Note: Zoom shortcuts (Ctrl+Plus/Minus/0) are already defined in XAML on the toolbar buttons
        // but we'll add alternative key bindings for better accessibility

        // Ctrl+Plus (main keyboard): Zoom in
        var zoomInAccelerator = new KeyboardAccelerator
        {
            Key = (VirtualKey)187, // VirtualKey.Add/Plus
            Modifiers = VirtualKeyModifiers.Control
        };
        zoomInAccelerator.Invoked += OnZoomInAccelerator;
        this.KeyboardAccelerators.Add(zoomInAccelerator);

        // Ctrl+Minus (main keyboard): Zoom out
        var zoomOutAccelerator = new KeyboardAccelerator
        {
            Key = (VirtualKey)189, // VirtualKey.Subtract/Minus
            Modifiers = VirtualKeyModifiers.Control
        };
        zoomOutAccelerator.Invoked += OnZoomOutAccelerator;
        this.KeyboardAccelerators.Add(zoomOutAccelerator);

        // Escape: Close panels and dialogs (Task 2.3 - Focus Management)
        var escapeAccelerator = new KeyboardAccelerator
        {
            Key = VirtualKey.Escape
        };
        escapeAccelerator.Invoked += OnEscapeAccelerator;
        this.KeyboardAccelerators.Add(escapeAccelerator);
    }

    /// <summary>
    /// Handles view mode changes to wire up the appropriate viewer control.
    /// </summary>
    private async Task UpdateViewModeAsync()
    {
        if (ViewModel.CurrentDocument == null)
        {
            return;
        }

        // Unsubscribe from previous mode events
        UnsubscribeViewModeEvents();

        switch (ViewModel.ViewMode)
        {
            case PageViewMode.ContinuousScroll:
                await ContinuousScrollViewerControl.LoadDocumentAsync(ViewModel.CurrentDocument, ViewModel.ZoomLevel);
                // Subscribe to continuous scroll events
                ContinuousScrollViewerControl.CurrentPageChanged += OnContinuousScrollPageChanged;
                ContinuousScrollViewerControl.TextSelectionStarted += OnContinuousScrollTextSelectionStarted;
                ContinuousScrollViewerControl.TextSelectionUpdated += OnContinuousScrollTextSelectionUpdated;
                ContinuousScrollViewerControl.TextSelectionEnded += OnContinuousScrollTextSelectionEnded;
                break;

            case PageViewMode.TwoPage:
                await TwoPageViewerControl.LoadDocumentAsync(ViewModel.CurrentDocument, ViewModel.CurrentPageNumber, ViewModel.ZoomLevel);
                // Subscribe to two-page viewer events if needed
                TwoPageViewerControl.CurrentPageChanged += OnTwoPageViewerPageChanged;
                break;

            case PageViewMode.SinglePage:
            default:
                // Single page mode is handled directly by ViewModel
                break;
        }
    }

    /// <summary>
    /// Unsubscribes from view mode specific events when switching modes.
    /// </summary>
    private void UnsubscribeViewModeEvents()
    {
        ContinuousScrollViewerControl.CurrentPageChanged -= OnContinuousScrollPageChanged;
        ContinuousScrollViewerControl.TextSelectionStarted -= OnContinuousScrollTextSelectionStarted;
        ContinuousScrollViewerControl.TextSelectionUpdated -= OnContinuousScrollTextSelectionUpdated;
        ContinuousScrollViewerControl.TextSelectionEnded -= OnContinuousScrollTextSelectionEnded;

        TwoPageViewerControl.CurrentPageChanged -= OnTwoPageViewerPageChanged;
    }

    /// <summary>
    /// Handles current page changes from continuous scroll viewer.
    /// </summary>
    private void OnContinuousScrollPageChanged(object? sender, int pageNumber)
    {
        // Update ViewModel's current page number
        if (ViewModel.CurrentPageNumber != pageNumber)
        {
            ViewModel.CurrentPageNumber = pageNumber;
        }
    }

    /// <summary>
    /// Handles current page changes from two-page viewer.
    /// </summary>
    private void OnTwoPageViewerPageChanged(object? sender, int pageNumber)
    {
        // Update ViewModel's current page number
        if (ViewModel.CurrentPageNumber != pageNumber)
        {
            ViewModel.CurrentPageNumber = pageNumber;
        }
    }

    /// <summary>
    /// Handles text selection started in continuous scroll mode.
    /// </summary>
    private void OnContinuousScrollTextSelectionStarted(object? sender, Controls.TextSelectionEventArgs e)
    {
        // Store the selection start point and page
        _selectionStartPoint = e.StartPoint;

        // Update ViewModel's current page to the selected page
        if (ViewModel.CurrentPageNumber != e.PageIndex + 1)
        {
            ViewModel.CurrentPageNumber = e.PageIndex + 1;
        }

        // Begin text selection in ViewModel
        ViewModel.BeginTextSelectionCommand.Execute(e.StartPoint);
    }

    /// <summary>
    /// Handles text selection updated in continuous scroll mode.
    /// </summary>
    private void OnContinuousScrollTextSelectionUpdated(object? sender, Controls.TextSelectionEventArgs e)
    {
        // Update text selection in ViewModel
        ViewModel.UpdateTextSelectionCommand.Execute(e.EndPoint);
    }

    /// <summary>
    /// Handles text selection ended in continuous scroll mode.
    /// </summary>
    private async void OnContinuousScrollTextSelectionEnded(object? sender, Controls.TextSelectionEventArgs e)
    {
        // End text selection in ViewModel
        await ViewModel.EndTextSelectionCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Handles ViewModel property changes to manage search TextBox focus and update highlights.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsSearchPanelVisible) && ViewModel.IsSearchPanelVisible)
        {
            // Focus the search TextBox when panel becomes visible
            _ = SearchTextBox.DispatcherQueue.TryEnqueue(() =>
            {
                SearchTextBox.Focus(FocusState.Programmatic);
                SearchTextBox.SelectAll();
            });
        }
        else if (e.PropertyName == nameof(ViewModel.SearchMatches) ||
                 e.PropertyName == nameof(ViewModel.CurrentMatchIndex) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageNumber) ||
                 e.PropertyName == nameof(ViewModel.ZoomLevel) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageImage) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageHeight))
        {
            // Update search highlights when matches, page, zoom, or dimensions change
            _ = SearchHighlightCanvas.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateSearchHighlights();
            });

            // Clear selection rectangle when page changes
            if (e.PropertyName == nameof(ViewModel.CurrentPageNumber))
            {
                ClearSelectionRectangle();
            }

            // Handle zoom changes in continuous scroll mode
            if (e.PropertyName == nameof(ViewModel.ZoomLevel) && ViewModel.ViewMode == PageViewMode.ContinuousScroll)
            {
                _ = DispatcherQueue.TryEnqueue(async () =>
                {
                    await ContinuousScrollViewerControl.UpdateZoomAsync(ViewModel.ZoomLevel);
                });
            }
        }
        else if (e.PropertyName == nameof(ViewModel.ViewMode))
        {
            // Update viewer controls when view mode changes
            _ = DispatcherQueue.TryEnqueue(async () =>
            {
                await UpdateViewModeAsync();
            });
        }
    }

    /// <summary>
    /// Handles AnnotationViewModel property changes to update cursor and status.
    /// </summary>
    private void OnAnnotationViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.AnnotationViewModel.ActiveTool))
        {
            UpdateCursorForActiveTool();
        }
        else if (e.PropertyName == nameof(ViewModel.AnnotationViewModel.StatusMessage))
        {
            // Status message is already bound in XAML, no action needed here
        }
    }

    /// <summary>
    /// Updates the cursor based on the currently active annotation tool.
    /// </summary>
    private void UpdateCursorForActiveTool()
    {
        if (ViewModel?.AnnotationViewModel == null)
        {
            return;
        }

        var tool = ViewModel.AnnotationViewModel.ActiveTool;

        // Update cursor for PdfPageImage (where user interacts with PDF)
        var cursor = tool switch
        {
            AnnotationTool.Highlight => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross),
            AnnotationTool.Underline => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross),
            AnnotationTool.Strikethrough => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross),
            AnnotationTool.Freehand => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand),
            AnnotationTool.Rectangle => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross),
            AnnotationTool.Circle => Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Cross),
            _ => null // Default cursor
        };

        // Apply cursor to the page (this will be inherited by PdfPageImage)
        this.ProtectedCursor = cursor;
    }

    /// <summary>
    /// Handles the page Loaded event to initialize DPI monitoring.
    /// </summary>
    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Start monitoring DPI changes for this page's XamlRoot
        ViewModel.StartDpiMonitoring(this.XamlRoot);

        // Wire up mouse wheel zoom on the ScrollViewer
        if (PdfScrollViewer != null)
        {
            PdfScrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
            PdfScrollViewer.PointerPressed += OnScrollViewerPointerPressed;
            PdfScrollViewer.PointerMoved += OnScrollViewerPointerMoved;
            PdfScrollViewer.PointerReleased += OnScrollViewerPointerReleased;
        }
    }

    /// <summary>
    /// Handles the page Unloaded event to clean up resources.
    /// </summary>
    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        // XamlRoot will become null on unload, which is handled by the ViewModel
        // No explicit cleanup needed here as Dispose will handle subscription cleanup
    }

    /// <summary>
    /// Handles navigation to the DOCX conversion page.
    /// </summary>
    private void OnConvertDocxClick(object sender, RoutedEventArgs e)
    {
        var navigationService = App.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ConversionPage));
    }

    /// <summary>
    /// Handles the watermark button click to show the watermark dialog.
    /// </summary>
    private async void OnWatermarkClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentDocument == null)
        {
            return;
        }

        try
        {
            var watermarkViewModel = App.GetService<WatermarkViewModel>();

            var applied = await WatermarkDialog.ShowAsync(
                this.XamlRoot,
                watermarkViewModel,
                ViewModel.CurrentDocument,
                ViewModel.CurrentPageNumber,
                ViewModel.TotalPages);

            if (applied)
            {
                // Refresh the current page display (this also marks the document as modified)
                await ViewModel.RefreshCurrentPageAsync();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to show watermark dialog");
        }
    }

    /// <summary>
    /// Handles keyboard events for form field navigation.
    /// Tab/Shift+Tab navigate between form fields in tab order.
    /// </summary>
    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Only handle Tab key when form fields are present
        if (!ViewModel.FormFieldViewModel.HasFormFields)
        {
            return;
        }

        if (e.Key == VirtualKey.Tab)
        {
            // Check if Shift is pressed
            var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (shiftPressed)
            {
                // Shift+Tab: Navigate to previous field
                ViewModel.FormFieldViewModel.FocusPreviousFieldCommand.Execute(null);
            }
            else
            {
                // Tab: Navigate to next field
                ViewModel.FormFieldViewModel.FocusNextFieldCommand.Execute(null);
            }

            // Mark event as handled to prevent default Tab behavior
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles scroll viewer view changes (zoom/scroll).
    /// Updates form field positions when the view changes.
    /// </summary>
    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        // Form field positions are bound to ZoomLevel which is already updated
        // The FormFieldControl will automatically recalculate positions based on zoom
        // This handler is primarily for future enhancements if needed
    }

    /// <summary>
    /// Handles scroll viewer size changes to maintain viewer grid minimum size.
    /// Ensures the grid always fills the viewport even when the PDF image is smaller.
    /// </summary>
    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateViewerGridSize();
    }

    /// <summary>
    /// Handles PDF image size changes to update the viewer grid size.
    /// Ensures the grid is at least as large as the viewport.
    /// </summary>
    private void OnPdfImageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateViewerGridSize();
    }

    /// <summary>
    /// Updates the viewer grid size to be at least as large as the ScrollViewer viewport.
    /// This keeps the viewer area constant - only the image scales with zoom.
    /// </summary>
    private void UpdateViewerGridSize()
    {
        if (PdfScrollViewer == null || PdfViewerGrid == null || PdfPageImage == null)
        {
            return;
        }

        // Get the viewport size
        var viewportWidth = PdfScrollViewer.ViewportWidth;
        var viewportHeight = PdfScrollViewer.ViewportHeight;

        // Get the image size
        var imageWidth = PdfPageImage.ActualWidth;
        var imageHeight = PdfPageImage.ActualHeight;

        // Set grid minimum size to at least the viewport size
        // This ensures the grid doesn't shrink when the image is smaller than viewport
        PdfViewerGrid.MinWidth = Math.Max(viewportWidth, imageWidth);
        PdfViewerGrid.MinHeight = Math.Max(viewportHeight, imageHeight);
    }

    /// <summary>
    /// Handles "Go to field" requests from the validation error panel.
    /// Focuses the specified form field.
    /// </summary>
    private void OnGoToFieldRequested(object sender, string fieldName)
    {
        ViewModel.FormFieldViewModel.FocusFieldByNameCommand.Execute(fieldName);
    }

    /// <summary>
    /// Handles Ctrl+F keyboard accelerator to toggle search panel.
    /// </summary>
    private void OnSearchKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ToggleSearchPanelCommand.Execute(null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles Escape key in search TextBox to close search panel.
    /// </summary>
    private void OnSearchEscapePressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.IsSearchPanelVisible)
        {
            ViewModel.ToggleSearchPanelCommand.Execute(null);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Handles Ctrl+C keyboard accelerator to copy selected text.
    /// </summary>
    private void OnCopyKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.HasSelectedText)
        {
            _ = ViewModel.CopyToClipboardCommand.ExecuteAsync(null);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Handles Ctrl+Shift+D keyboard accelerator to toggle diagnostics panel.
    /// </summary>
    private void OnToggleDiagnosticsKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ToggleDiagnosticsCommand.Execute(null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+Shift+L keyboard accelerator to open log viewer.
    /// </summary>
    private void OnOpenLogViewerKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = ViewModel.OpenLogViewerCommand.ExecuteAsync(null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles F5 or Ctrl+L keyboard accelerator to enter presentation mode.
    /// </summary>
    private void OnPresentationModeKeyboardAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.EnterPresentationModeCommand.Execute(null);
        args.Handled = true;
    }

    /// <summary>
    /// Handles pointer pressed event to begin text selection.
    /// </summary>
    private void OnImagePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Don't handle if an annotation tool is active - let AnnotationLayer handle it
        var activeTool = ViewModel.AnnotationViewModel?.ActiveTool ?? ViewModels.AnnotationTool.None;
        if (activeTool != ViewModels.AnnotationTool.None)
        {
            return;
        }

        var properties = e.GetCurrentPoint(PdfPageImage).Properties;

        // Only start selection on left-click
        if (properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(PdfPageImage).Position;
            _selectionStartPoint = point;
            ViewModel.BeginTextSelectionCommand.Execute(point);
            PdfPageImage.CapturePointer(e.Pointer);

            // Show and position selection rectangle using Canvas positioning
            SelectionRectangle.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(SelectionRectangle, point.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(SelectionRectangle, point.Y);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved event to update text selection.
    /// </summary>
    private void OnImagePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel.IsSelecting)
        {
            var point = e.GetCurrentPoint(PdfPageImage).Position;
            ViewModel.UpdateTextSelectionCommand.Execute(point);

            // Update selection rectangle using Canvas positioning
            var x = Math.Min(_selectionStartPoint.X, point.X);
            var y = Math.Min(_selectionStartPoint.Y, point.Y);
            var width = Math.Abs(point.X - _selectionStartPoint.X);
            var height = Math.Abs(point.Y - _selectionStartPoint.Y);

            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(SelectionRectangle, x);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer released event to end text selection.
    /// </summary>
    private void OnImagePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel.IsSelecting)
        {
            PdfPageImage.ReleasePointerCapture(e.Pointer);
            _ = ViewModel.EndTextSelectionCommand.ExecuteAsync(null);

            // Keep selection rectangle visible if there's a selection, hide on next click
            // The rectangle stays visible to show what was selected
            e.Handled = true;
        }
    }

    /// <summary>
    /// Clears the visual selection rectangle.
    /// </summary>
    private void ClearSelectionRectangle()
    {
        SelectionRectangle.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;
    }

    /// <summary>
    /// Handles mouse wheel events for Ctrl+scroll zoom.
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(PdfScrollViewer).Properties;
        var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrlPressed)
        {
            var delta = properties.MouseWheelDelta;
            if (delta > 0)
            {
                // Zoom in
                _ = ViewModel.ZoomInCommand.ExecuteAsync(null);
            }
            else if (delta < 0)
            {
                // Zoom out
                _ = ViewModel.ZoomOutCommand.ExecuteAsync(null);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer pressed on ScrollViewer for middle-button panning.
    /// </summary>
    private void OnScrollViewerPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(PdfScrollViewer).Properties;

        // Check for middle mouse button
        if (properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = e.GetCurrentPoint(PdfScrollViewer).Position;
            _panStartHorizontalOffset = PdfScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = PdfScrollViewer.VerticalOffset;
            PdfScrollViewer.CapturePointer(e.Pointer);

            // Change cursor to indicate panning
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeAll);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved on ScrollViewer for panning.
    /// </summary>
    private void OnScrollViewerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            var currentPoint = e.GetCurrentPoint(PdfScrollViewer).Position;
            var deltaX = _panStartPoint.X - currentPoint.X;
            var deltaY = _panStartPoint.Y - currentPoint.Y;

            // Calculate new offsets (clamped to valid range)
            var newHorizontalOffset = Math.Max(0, Math.Min(
                PdfScrollViewer.ScrollableWidth,
                _panStartHorizontalOffset + deltaX));
            var newVerticalOffset = Math.Max(0, Math.Min(
                PdfScrollViewer.ScrollableHeight,
                _panStartVerticalOffset + deltaY));

            PdfScrollViewer.ChangeView(newHorizontalOffset, newVerticalOffset, null, true);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer released on ScrollViewer to end panning.
    /// </summary>
    private void OnScrollViewerPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            PdfScrollViewer.ReleasePointerCapture(e.Pointer);
            this.ProtectedCursor = null; // Reset cursor
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles accessibility notification messages and announces them to screen readers.
    /// </summary>
    private void OnAccessibilityNotification(object recipient, AccessibilityNotificationMessage message)
    {
        try
        {
            // Get the AutomationPeer for this page
            var peer = FrameworkElementAutomationPeer.FromElement(this);
            if (peer == null)
            {
                peer = FrameworkElementAutomationPeer.CreatePeerForElement(this);
            }

            if (peer != null)
            {
                // Raise the notification event for screen readers
                peer.RaiseNotificationEvent(
                    AutomationNotificationKind.ActionCompleted,
                    AutomationNotificationProcessing.ImportantMostRecent,
                    message.Message,
                    Guid.NewGuid().ToString());
            }
        }
        catch (Exception)
        {
            // Silently fail if accessibility notification fails
            // This is not critical functionality
        }
    }

    /// <summary>
    /// Updates the search highlight overlays on the current page.
    /// Renders rectangles for all search matches on the current page.
    /// </summary>
    private void UpdateSearchHighlights()
    {
        // Clear existing highlights
        SearchHighlightCanvas.Children.Clear();

        // If we don't have valid dimensions or no matches, nothing to render
        if (ViewModel.CurrentPageHeight == 0 || ViewModel.SearchMatches.Count == 0 || ViewModel.CurrentPageImage == null)
        {
            return;
        }

        // Get matches for the current page (1-based page number in ViewModel, 0-based in SearchMatch)
        var currentPageMatches = ViewModel.SearchMatches
            .Where(m => m.PageNumber == ViewModel.CurrentPageNumber - 1)
            .ToList();

        if (currentPageMatches.Count == 0)
        {
            return;
        }

        // Determine which match is the current match
        int currentMatchIndexOnPage = -1;
        if (ViewModel.CurrentMatchIndex >= 0 && ViewModel.CurrentMatchIndex < ViewModel.SearchMatches.Count)
        {
            var currentMatch = ViewModel.SearchMatches[ViewModel.CurrentMatchIndex];
            if (currentMatch.PageNumber == ViewModel.CurrentPageNumber - 1)
            {
                currentMatchIndexOnPage = currentPageMatches.IndexOf(currentMatch);
            }
        }

        // Render highlight rectangles for each match
        for (int i = 0; i < currentPageMatches.Count; i++)
        {
            var match = currentPageMatches[i];
            var screenRect = CoordinateTransformHelper.TransformPdfToScreen(
                match.BoundingBox,
                ViewModel.CurrentPageHeight,
                ViewModel.ZoomLevel);

            // Create highlight rectangle
            var rect = new Rectangle
            {
                Width = screenRect.Width,
                Height = screenRect.Height,
                Fill = i == currentMatchIndexOnPage
                    ? new SolidColorBrush(Colors.Orange) { Opacity = 0.5 }  // Current match: orange
                    : new SolidColorBrush(Colors.Yellow) { Opacity = 0.3 }, // Other matches: yellow
                Stroke = i == currentMatchIndexOnPage
                    ? new SolidColorBrush(Colors.DarkOrange)
                    : new SolidColorBrush(Colors.Gold),
                StrokeThickness = 1
            };

            // Position the rectangle on the canvas
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(rect, screenRect.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(rect, screenRect.Y);

            // Add to canvas
            SearchHighlightCanvas.Children.Add(rect);
        }

        // Update canvas size to match the image
        if (PdfPageImage.ActualWidth > 0 && PdfPageImage.ActualHeight > 0)
        {
            SearchHighlightCanvas.Width = PdfPageImage.ActualWidth;
            SearchHighlightCanvas.Height = PdfPageImage.ActualHeight;
        }
    }

    /// <summary>
    /// Handles Page Up accelerator to navigate to previous page.
    /// </summary>
    private void OnPageUpAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.GoToPreviousPageCommand.CanExecute(null))
        {
            _ = ViewModel.GoToPreviousPageCommand.ExecuteAsync(null);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles Page Down accelerator to navigate to next page.
    /// </summary>
    private void OnPageDownAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.GoToNextPageCommand.CanExecute(null))
        {
            _ = ViewModel.GoToNextPageCommand.ExecuteAsync(null);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles Home accelerator to navigate to first page.
    /// </summary>
    private void OnHomeAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.CurrentDocument != null)
        {
            ViewModel.CurrentPageNumber = 1;
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles End accelerator to navigate to last page.
    /// </summary>
    private void OnEndAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.CurrentDocument != null)
        {
            ViewModel.CurrentPageNumber = ViewModel.TotalPages;
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+G accelerator to show go to page dialog.
    /// </summary>
    private async void OnGoToPageAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.CurrentDocument == null)
        {
            args.Handled = true;
            return;
        }

        try
        {
            var textBox = new TextBox
            {
                PlaceholderText = $"Enter page number (1-{ViewModel.TotalPages})",
                Text = ViewModel.CurrentPageNumber.ToString()
            };

            var dialog = new ContentDialog
            {
                Title = "Go to Page",
                Content = textBox,
                PrimaryButtonText = "Go",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // Focus the textbox when dialog opens
            dialog.Opened += (s, e) =>
            {
                textBox.Focus(FocusState.Programmatic);
                textBox.SelectAll();
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (int.TryParse(textBox.Text, out int pageNumber))
                {
                    if (pageNumber >= 1 && pageNumber <= ViewModel.TotalPages)
                    {
                        ViewModel.CurrentPageNumber = pageNumber;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to show go to page dialog");
        }

        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+Plus accelerator to zoom in.
    /// </summary>
    private void OnZoomInAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.ZoomInCommand.CanExecute(null))
        {
            _ = ViewModel.ZoomInCommand.ExecuteAsync(null);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles Ctrl+Minus accelerator to zoom out.
    /// </summary>
    private void OnZoomOutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.ZoomOutCommand.CanExecute(null))
        {
            _ = ViewModel.ZoomOutCommand.ExecuteAsync(null);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Handles Escape accelerator to close panels and dialogs (Focus Management - Task 2.3).
    /// </summary>
    private void OnEscapeAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        // Priority order: Close search panel -> Close bookmarks -> Close thumbnails -> Close diagnostics
        if (ViewModel.IsSearchPanelVisible)
        {
            ViewModel.ToggleSearchPanelCommand.Execute(null);
            args.Handled = true;
            return;
        }

        if (ViewModel.BookmarksViewModel.IsPanelVisible)
        {
            ViewModel.BookmarksViewModel.TogglePanelCommand.Execute(null);
            args.Handled = true;
            return;
        }

        if (ViewModel.IsSidebarVisible)
        {
            ViewModel.ToggleSidebarCommand.Execute(null);
            args.Handled = true;
            return;
        }

        if (ViewModel.DiagnosticsPanelViewModel.IsVisible)
        {
            ViewModel.ToggleDiagnosticsCommand.Execute(null);
            args.Handled = true;
            return;
        }

        // No panels to close, don't mark as handled to allow default Escape behavior
        args.Handled = false;
    }

    /// <summary>
    /// Disposes resources used by the page.
    /// </summary>
    public void Dispose()
    {
        this.KeyDown -= OnPageKeyDown;
        this.Loaded -= OnPageLoaded;
        this.Unloaded -= OnPageUnloaded;

        // Unregister scroll viewer events
        if (PdfScrollViewer != null)
        {
            PdfScrollViewer.PointerWheelChanged -= OnScrollViewerPointerWheelChanged;
            PdfScrollViewer.PointerPressed -= OnScrollViewerPointerPressed;
            PdfScrollViewer.PointerMoved -= OnScrollViewerPointerMoved;
            PdfScrollViewer.PointerReleased -= OnScrollViewerPointerReleased;
        }

        // Unsubscribe from view mode events
        UnsubscribeViewModeEvents();

        // Unregister accessibility notification message handler
        WeakReferenceMessenger.Default.Unregister<AccessibilityNotificationMessage>(this);

        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            if (ViewModel.AnnotationViewModel != null)
            {
                ViewModel.AnnotationViewModel.PropertyChanged -= OnAnnotationViewModelPropertyChanged;
            }

            ViewModel.Dispose();
        }
    }
}
