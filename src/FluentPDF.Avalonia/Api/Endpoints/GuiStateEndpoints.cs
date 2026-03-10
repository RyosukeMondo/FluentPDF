// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// GUI endpoints for UI state queries, panel visibility, tab management, and screenshots.
/// All GUI access is marshalled to the Avalonia UI thread.
/// </summary>
public static class GuiStateEndpoints
{
    public static void MapGuiStateEndpoints(RouteGroupBuilder group)
    {
        MapStateEndpoint(group);
        MapToggleEndpoint(group);
        MapCloseTabEndpoint(group);
        MapScreenshotEndpoint(group);
        MapNotifyEndpoint(group);
    }

    private static void MapStateEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/state", async (HttpContext _) =>
        {
            var state = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = GuiEndpoints.GetMainWindow();
                if (window == null)
                    return (object)new { error = "No main window" };

                var vm = window.ViewModel;
                var activeViewer = vm.ActiveTab?.ViewerViewModel;

                return (object)new
                {
                    windowTitle = window.Title,
                    windowSize = new { width = window.Width, height = window.Height },
                    tabCount = vm.Tabs.Count,
                    activeTab = vm.ActiveTab != null ? new
                    {
                        fileName = vm.ActiveTab.FileName,
                        filePath = vm.ActiveTab.FilePath,
                        hasUnsavedChanges = vm.ActiveTab.HasUnsavedChanges
                    } : null,
                    viewer = activeViewer != null ? new
                    {
                        currentPage = activeViewer.Navigation.CurrentPageNumber,
                        totalPages = activeViewer.Navigation.TotalPages,
                        zoomLevel = activeViewer.Zoom.ZoomLevel,
                        isLoading = activeViewer.IsLoading,
                        statusMessage = activeViewer.StatusMessage,
                        hasDocument = activeViewer.CurrentDocument != null,
                        sidebarVisible = activeViewer.ViewState.IsSidebarVisible,
                        bookmarksVisible = activeViewer.ViewState.IsBookmarksPanelVisible,
                        searchVisible = activeViewer.ViewState.IsSearchPanelVisible,
                        drawingTool = activeViewer.ActiveDrawingTool.ToString(),
                        drawingStrokeColor = activeViewer.DrawingStrokeColor,
                        drawingFillColor = activeViewer.DrawingFillColor,
                        drawingStrokeWidth = activeViewer.DrawingStrokeWidth,
                        pageOpCallbackSet = activeViewer.PageOperationCallback != null,
                        renderCallbackSet = activeViewer.RenderPageCallback != null,
                        hasSelectedText = activeViewer.HasSelectedText,
                        selectedTextLength = activeViewer.SelectedText?.Length ?? 0
                    } : null
                };
            });

            return Results.Json(state);
        })
        .WithName("GetGuiState")
        .WithSummary("Get full UI state")
        .WithDescription("Returns the current state of the GUI including window, tabs, and active viewer information.");
    }

    private static void MapToggleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/toggle", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<ToggleRequest>();
            if (body?.Panel == null)
                return Results.BadRequest(new { error = "panel is required (thumbnails|bookmarks|search)" });

            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GuiEndpoints.GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                switch (body.Panel.ToLowerInvariant())
                {
                    case "thumbnails":
                        viewer.ToggleThumbnailsCommand.Execute(null);
                        break;
                    case "bookmarks":
                        viewer.ToggleBookmarksCommand.Execute(null);
                        break;
                    case "search":
                        viewer.ShowSearchCommand.Execute(null);
                        break;
                    default:
                        return (object)new { success = false, error = $"Unknown panel: {body.Panel}" };
                }

                return (object)new
                {
                    success = true,
                    sidebarVisible = viewer.IsSidebarVisible,
                    bookmarksVisible = viewer.IsBookmarksPanelVisible,
                    searchVisible = viewer.IsSearchPanelVisible
                };
            });

            return Results.Json(result);
        })
        .WithName("TogglePanel")
        .WithSummary("Toggle a panel")
        .WithDescription("Toggle thumbnails, bookmarks, or search panel visibility.");
    }

    private static void MapCloseTabEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/close-tab", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var vm = GuiEndpoints.GetMainWindow()?.ViewModel;
                if (vm?.ActiveTab == null)
                    return (object)new { success = false, error = "No active tab" };

                vm.CloseTabCommand.Execute(vm.ActiveTab);
                return (object)new { success = true, remainingTabs = vm.Tabs.Count };
            });

            return Results.Json(result);
        })
        .WithName("CloseTab")
        .WithSummary("Close the active tab")
        .WithDescription("Closes the currently active document tab.");
    }

    private static void MapScreenshotEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/screenshot", async (HttpContext _) =>
        {
            var pngBytes = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = GuiEndpoints.GetMainWindow();
                if (window == null)
                    return (byte[]?)null;

                // Let any pending layout/render passes complete before capturing.
                // This prevents crashes when screenshot is taken during panel transitions.
                await Task.Delay(100);
                window.InvalidateVisual();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);

                var pixelSize = new global::Avalonia.PixelSize(
                    (int)window.Bounds.Width,
                    (int)window.Bounds.Height);

                if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
                    return null;

                try
                {
                    var renderTarget = new global::Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize);
                    renderTarget.Render(window);

                    using var ms = new MemoryStream();
                    renderTarget.Save(ms);
                    return ms.ToArray();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Screenshot render failed: {ex.Message}");
                    return null;
                }
            });

            if (pngBytes == null)
                return Results.Problem("No window available or window has zero size");

            return Results.File(pngBytes, "image/png", "screenshot.png");
        })
        .WithName("Screenshot")
        .WithSummary("Capture a screenshot")
        .WithDescription("Captures a PNG screenshot of the main application window.");
    }

    private static void MapNotifyEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/notify", (NotifyRequest request) =>
        {
            var notificationService = App.GetService<FluentPDF.Core.Services.INotificationService>();
            if (notificationService == null)
                return Results.Problem("Notification service not available");

            Dispatcher.UIThread.Post(() =>
            {
                switch (request.Level?.ToLowerInvariant())
                {
                    case "success": notificationService.ShowSuccess(request.Message ?? ""); break;
                    case "warning": notificationService.ShowWarning(request.Message ?? ""); break;
                    case "error": notificationService.ShowError(request.Message ?? ""); break;
                    default: notificationService.ShowInfo(request.Message ?? ""); break;
                }
            });

            return Results.Ok(new { sent = true });
        })
        .WithName("Notify")
        .WithSummary("Show a toast notification")
        .WithDescription("Displays a non-blocking toast notification in the UI. Levels: success, info, warning, error.");
    }

    private record ToggleRequest(string? Panel);
    private record NotifyRequest(string? Message, string? Level);
}
