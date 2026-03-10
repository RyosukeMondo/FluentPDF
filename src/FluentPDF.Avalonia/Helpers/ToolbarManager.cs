using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Manages toolbar event handling and state updates.
/// Extracted from MainWindow to reduce complexity.
/// </summary>
public sealed class ToolbarManager
{
    private readonly Window _owner;
    private readonly MainViewModel _viewModel;
    private readonly Func<Task>? _onOpenFile;
    private bool _suppressZoomSelectionChanged;
    private readonly ILogger? _logger;
    private PdfViewerViewModel? _subscribedViewer;

    public ToolbarManager(Window owner, MainViewModel viewModel, ILogger? logger = null, Func<Task>? onOpenFile = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;
        _onOpenFile = onOpenFile;
    }

    /// <summary>
    /// Sets up all toolbar event handlers.
    /// </summary>
    public void Initialize()
    {
        SetupFileOperations();
        SetupNavigation();
        SetupZoom();
        SetupViewToggles();
        SetupDrawingTools();
    }

    private void SetupFileOperations()
    {
        var toolbarOpenButton = _owner.FindControl<Button>("ToolbarOpenButton");
        if (toolbarOpenButton != null)
        {
            if (_onOpenFile != null)
                toolbarOpenButton.Click += async (s, e) => await _onOpenFile();
            else
                toolbarOpenButton.Click += (s, e) => _viewModel.OpenFileInNewTabCommand.Execute(null);
        }
    }

    private void SetupNavigation()
    {
        var prevButton = _owner.FindControl<Button>("ToolbarPreviousPageButton");
        if (prevButton != null)
            prevButton.Click += OnPreviousPageClick;

        var nextButton = _owner.FindControl<Button>("ToolbarNextPageButton");
        if (nextButton != null)
            nextButton.Click += OnNextPageClick;

        var pageNumberBox = _owner.FindControl<TextBox>("ToolbarPageNumberBox");
        if (pageNumberBox != null)
            pageNumberBox.KeyDown += OnPageNumberKeyDown;
    }

    private void SetupZoom()
    {
        var zoomInButton = _owner.FindControl<Button>("ToolbarZoomInButton");
        if (zoomInButton != null)
            zoomInButton.Click += OnZoomInClick;

        var zoomOutButton = _owner.FindControl<Button>("ToolbarZoomOutButton");
        if (zoomOutButton != null)
            zoomOutButton.Click += OnZoomOutClick;

        var zoomComboBox = _owner.FindControl<ComboBox>("ToolbarZoomComboBox");
        if (zoomComboBox != null)
            zoomComboBox.SelectionChanged += OnZoomSelectionChanged;

        // Subscribe to active tab changes to track zoom level
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ActiveTab))
                SubscribeToActiveViewerZoom();
        };

        // Initial subscription
        SubscribeToActiveViewerZoom();

        // Show default 100%
        UpdateZoomDisplay(1.0);
    }

    private void SubscribeToActiveViewerZoom()
    {
        // Unsubscribe from previous viewer
        if (_subscribedViewer != null)
        {
            _subscribedViewer.Zoom.PropertyChanged -= OnZoomLevelChanged;
            _subscribedViewer = null;
        }

        var viewer = _viewModel.ActiveTab?.ViewerViewModel;
        if (viewer != null)
        {
            _subscribedViewer = viewer;
            viewer.Zoom.PropertyChanged += OnZoomLevelChanged;
            UpdateZoomDisplay(viewer.ZoomLevel);
        }
        else
        {
            UpdateZoomDisplay(1.0);
        }
    }

    private void OnZoomLevelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ZoomViewModel.ZoomLevel))
        {
            var zoom = _subscribedViewer?.ZoomLevel ?? 1.0;
            UpdateZoomDisplay(zoom);
        }
    }

    /// <summary>
    /// Updates the zoom ComboBox display to reflect the current zoom level.
    /// </summary>
    public void UpdateZoomDisplay(double zoomLevel)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var zoomComboBox = _owner.FindControl<ComboBox>("ToolbarZoomComboBox");
            if (zoomComboBox == null) return;

            _suppressZoomSelectionChanged = true;
            try
            {
                var percentText = $"{(int)(zoomLevel * 100)}%";

                // Try to match an existing item
                for (int i = 0; i < zoomComboBox.ItemCount; i++)
                {
                    if (zoomComboBox.Items[i] is ComboBoxItem item && item.Content is string text && text == percentText)
                    {
                        zoomComboBox.SelectedIndex = i;
                        return;
                    }
                }

                // No exact match - deselect so the combobox shows empty (no feedback loop)
                zoomComboBox.SelectedIndex = -1;
            }
            finally
            {
                _suppressZoomSelectionChanged = false;
            }
        }, DispatcherPriority.Background);
    }

    private void SetupViewToggles()
    {
        // Primary toolbar: Thumbnails toggle
        var thumbnailsButton = _owner.FindControl<ToggleButton>("ToolbarToggleThumbnailsButton");
        if (thumbnailsButton != null)
            thumbnailsButton.Click += OnToggleThumbnailsClick;

        // Overflow flyout: Bookmarks, Metadata, Search, Edit
        var overflowBookmarks = _owner.FindControl<Button>("OverflowToggleBookmarksButton");
        if (overflowBookmarks != null)
            overflowBookmarks.Click += OnToggleBookmarksClick;

        var overflowMetadata = _owner.FindControl<Button>("OverflowToggleMetadataButton");
        if (overflowMetadata != null)
            overflowMetadata.Click += OnToggleMetadataClick;

        var overflowSearch = _owner.FindControl<Button>("OverflowSearchButton");
        if (overflowSearch != null)
            overflowSearch.Click += OnSearchClick;

        var overflowEdit = _owner.FindControl<Button>("OverflowEditButton");
        if (overflowEdit != null)
        {
            overflowEdit.Click += (s, e) =>
            {
                var viewer = _viewModel.ActiveTab?.ViewerViewModel;
                if (viewer == null) return;
                viewer.IsDrawingToolbarVisible = !viewer.IsDrawingToolbarVisible;
                if (viewer.IsDrawingToolbarVisible && viewer.ActiveDrawingTool == DrawingTool.None)
                    viewer.ActiveDrawingTool = DrawingTool.Rectangle;
            };
        }
    }

    private void OnPreviousPageClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.Navigation?.GoToPreviousPageCommand is { } cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            UpdatePageNumber();
        }
    }

    private void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.Navigation?.GoToNextPageCommand is { } cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            UpdatePageNumber();
        }
    }

    private void OnPageNumberKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var textBox = sender as TextBox;
        if (textBox != null && int.TryParse(textBox.Text, out int pageNumber))
        {
            if (_viewModel.ActiveTab?.ViewerViewModel?.Navigation?.GoToPageCommand is { } cmd && cmd.CanExecute(pageNumber))
            {
                cmd.Execute(pageNumber);
                UpdatePageNumber();
            }
        }
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.Zoom?.ZoomInCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.Zoom?.ZoomOutCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnZoomSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressZoomSelectionChanged) return;

        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem is not ComboBoxItem item || item.Content is not string zoomText) return;

        var viewModel = _viewModel.ActiveTab?.ViewerViewModel;
        if (viewModel == null) return;

        if (zoomText.EndsWith("%") && double.TryParse(zoomText.TrimEnd('%'), out double zoomPercent))
        {
            var zoomLevel = zoomPercent / 100.0;
            if (viewModel.Zoom?.SetZoomCommand?.CanExecute(zoomLevel) == true)
                viewModel.Zoom.SetZoomCommand.Execute(zoomLevel);
        }
        else if (zoomText == "Fit Width" && viewModel.Zoom?.FitWidthCommand?.CanExecute(null) == true)
        {
            viewModel.Zoom.FitWidthCommand.Execute(null);
        }
        else if (zoomText == "Fit Page" && viewModel.Zoom?.FitPageCommand?.CanExecute(null) == true)
        {
            viewModel.Zoom.FitPageCommand.Execute(null);
        }
    }

    private void OnToggleThumbnailsClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.ToggleThumbnailsCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnToggleBookmarksClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.ToggleBookmarksCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnToggleMetadataClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.ToggleMetadataCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveTab?.ViewerViewModel?.ShowSearchCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void SetupDrawingTools()
    {
        // Edit/Draw button is now in the overflow flyout, wired in SetupViewToggles
    }

    private void WireDrawingButton(string buttonName, string toolName)
    {
        var button = _owner.FindControl<Button>(buttonName);
        if (button != null)
        {
            button.Click += (s, e) =>
            {
                var viewer = _viewModel.ActiveTab?.ViewerViewModel;
                if (viewer == null) return;
                if (viewer.SetDrawingToolCommand is { } cmd && cmd.CanExecute(toolName))
                    cmd.Execute(toolName);
                viewer.IsDrawingToolbarVisible = true;
            };
        }
    }

    /// <summary>
    /// Updates toolbar page number display based on active tab.
    /// </summary>
    public void UpdatePageNumber()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var pageNumberBox = _owner.FindControl<TextBox>("ToolbarPageNumberBox");
            var totalPagesText = _owner.FindControl<TextBlock>("ToolbarTotalPagesText");
            var prevButton = _owner.FindControl<Button>("ToolbarPreviousPageButton");
            var nextButton = _owner.FindControl<Button>("ToolbarNextPageButton");

            if (_viewModel.ActiveTab?.ViewerViewModel != null)
            {
                var currentPage = _viewModel.ActiveTab.ViewerViewModel.Navigation.CurrentPageNumber;
                var totalPages = _viewModel.ActiveTab.ViewerViewModel.TotalPages;

                if (pageNumberBox != null) pageNumberBox.Text = currentPage.ToString();
                if (totalPagesText != null) totalPagesText.Text = $"of {totalPages}";
                if (prevButton != null) prevButton.IsEnabled = currentPage > 1;
                if (nextButton != null) nextButton.IsEnabled = currentPage < totalPages;
            }
            else
            {
                if (pageNumberBox != null) pageNumberBox.Text = "1";
                if (totalPagesText != null) totalPagesText.Text = "of 0";
                if (prevButton != null) prevButton.IsEnabled = false;
                if (nextButton != null) nextButton.IsEnabled = false;
            }
        }, DispatcherPriority.Background);
    }
}
