using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Command definitions and handlers for PdfViewerViewModel.
/// </summary>
public partial class PdfViewerViewModel
{
    #region Command Forwarding Properties

    // Navigation commands (forwarded to NavigationViewModel)
    public IAsyncRelayCommand GoToPreviousPageCommand => Navigation.GoToPreviousPageCommand;
    public IAsyncRelayCommand GoToNextPageCommand => Navigation.GoToNextPageCommand;
    public IAsyncRelayCommand<int> GoToPageCommand => Navigation.GoToPageCommand;
    public IAsyncRelayCommand FirstPageCommand => Navigation.FirstPageCommand;
    public IAsyncRelayCommand LastPageCommand => Navigation.LastPageCommand;

    // Zoom commands (forwarded to ZoomViewModel)
    public IAsyncRelayCommand ZoomInCommand => Zoom.ZoomInCommand;
    public IAsyncRelayCommand ZoomOutCommand => Zoom.ZoomOutCommand;
    public IAsyncRelayCommand ResetZoomCommand => Zoom.ResetZoomCommand;
    public IAsyncRelayCommand<double> SetZoomCommand => Zoom.SetZoomCommand;
    public IAsyncRelayCommand FitWidthCommand => Zoom.FitWidthCommand;
    public IAsyncRelayCommand FitPageCommand => Zoom.FitPageCommand;

    // View state commands (forwarded to ViewStateViewModel)
    public IRelayCommand ToggleThumbnailsCommand => ViewState.ToggleThumbnailsCommand;
    public IRelayCommand ToggleBookmarksCommand => ViewState.ToggleBookmarksCommand;
    public IRelayCommand ShowSearchCommand => ViewState.ShowSearchCommand;
    public IRelayCommand ToggleSearchPanelCommand => ViewState.ToggleSearchPanelCommand;
    public IRelayCommand ToggleViewModeCommand => ViewState.ToggleViewModeCommand;
    public IRelayCommand ToggleAnnotationsCommand => ViewState.ToggleAnnotationsCommand;
    public IRelayCommand ToggleMetadataCommand => ViewState.ToggleMetadataCommand;

    #endregion

    #region Drawing Tool Commands

    [RelayCommand]
    private void SetDrawingTool(string toolName)
    {
        if (Enum.TryParse<DrawingTool>(toolName, ignoreCase: true, out var tool))
        {
            if (tool == DrawingTool.None)
            {
                ActiveDrawingTool = DrawingTool.None;
            }
            else
            {
                ActiveDrawingTool = ActiveDrawingTool == tool ? DrawingTool.None : tool;
                if (ActiveDrawingTool != DrawingTool.None)
                    IsDrawingToolbarVisible = true;
            }
        }
        else
        {
            ActiveDrawingTool = DrawingTool.None;
        }
    }

    [RelayCommand]
    private void CloseDrawingToolbar()
    {
        ActiveDrawingTool = DrawingTool.None;
        IsDrawingToolbarVisible = false;
    }

    #endregion

    #region Page Management Commands

    private bool CanExecutePageOperation() =>
        _currentDocument != null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task RotatePageClockwiseAsync()
    {
        if (_currentDocument == null || PageOperationCallback == null) return;
        var success = await PageOperationCallback(_currentDocument, CurrentPageIndex, "rotate_cw");
        if (success)
        {
            HasPageModifications = true;
            await RenderCurrentPageAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task RotatePageCounterClockwiseAsync()
    {
        if (_currentDocument == null || PageOperationCallback == null) return;
        var success = await PageOperationCallback(_currentDocument, CurrentPageIndex, "rotate_ccw");
        if (success)
        {
            HasPageModifications = true;
            await RenderCurrentPageAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task DeleteCurrentPageAsync()
    {
        if (_currentDocument == null || PageOperationCallback == null) return;
        if (TotalPages <= 1)
        {
            _logger.LogWarning("Cannot delete the only page");
            return;
        }

        var success = await PageOperationCallback(_currentDocument, CurrentPageIndex, "delete");
        if (success)
        {
            HasPageModifications = true;
            TotalPages--;
            if (CurrentPageNumber > TotalPages)
                CurrentPageNumber = TotalPages;
            await RenderCurrentPageAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecutePageOperation))]
    private async Task InsertBlankPageAsync()
    {
        if (_currentDocument == null || PageOperationCallback == null) return;
        var success = await PageOperationCallback(_currentDocument, CurrentPageIndex + 1, "insert_blank");
        if (success)
        {
            HasPageModifications = true;
            TotalPages++;
            CurrentPageNumber = CurrentPageIndex + 2; // navigate to new page
            await RenderCurrentPageAsync();
        }
    }

    #endregion

    #region Text Selection Commands

    private bool CanSelectAllText() => _currentDocument != null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSelectAllText))]
    private async Task SelectAllTextAsync()
    {
        if (_currentDocument == null) return;

        try
        {
            var result = await _textExtractionService.ExtractTextAsync(_currentDocument, CurrentPageNumber);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                SelectedText = result.Value;
                HasSelectedText = true;
                _logger.LogInformation("Selected all text on page {Page}: {Length} characters",
                    CurrentPageNumber, result.Value.Length);
            }
            else
            {
                SelectedText = string.Empty;
                HasSelectedText = false;
                _logger.LogDebug("No text found on page {Page}", CurrentPageNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select all text on page {Page}", CurrentPageNumber);
        }
    }

    #endregion
}
