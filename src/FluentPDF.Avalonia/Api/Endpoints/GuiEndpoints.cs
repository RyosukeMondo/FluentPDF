// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Avalonia.Views;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// GUI automation endpoints for remote UI control and state inspection.
/// All GUI access is marshalled to the Avalonia UI thread.
/// </summary>
public static class GuiEndpoints
{
    /// <summary>
    /// Maps GUI automation endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/gui")
            .WithTags("GUI Automation");

        MapStateEndpoint(group);
        MapOpenEndpoint(group);
        MapNavigateEndpoint(group);
        MapZoomEndpoint(group);
        MapToggleEndpoint(group);
        MapCloseTabEndpoint(group);
        MapScreenshotEndpoint(group);
        MapRefreshEndpoint(group);
        MapAnnotateEndpoint(group);
    }

    private static MainWindow? GetMainWindow()
    {
        var appInstance = (App)global::Avalonia.Application.Current!;
        return appInstance.MainWindow;
    }

    private static PdfViewerViewModel? GetActiveViewer()
    {
        return GetMainWindow()?.ViewModel.ActiveTab?.ViewerViewModel;
    }

    private static async Task OpenFileOnUiThread(MainViewModel vm, string filePath, TaskCompletionSource<object> tcs)
    {
        try
        {
            await vm.OpenFileInTabAsync(filePath);
            await Task.Delay(500);

            var viewer = vm.ActiveTab?.ViewerViewModel;
            tcs.TrySetResult(new
            {
                success = true,
                tabCount = vm.Tabs.Count,
                activeFile = vm.ActiveTab?.FileName,
                pageCount = viewer?.TotalPages ?? 0,
                currentPage = viewer?.CurrentPageNumber ?? 0,
                statusMessage = viewer?.StatusMessage ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            tcs.TrySetResult(new { success = false, error = ex.ToString() });
        }
    }

    private static async Task NavigateOnUiThread(NavigateRequest body, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            if (body.Page.HasValue)
                await viewer.GoToPageCommand.ExecuteAsync(body.Page.Value);
            else if (body.Action == "next")
                await viewer.GoToNextPageCommand.ExecuteAsync(null);
            else if (body.Action == "previous")
                await viewer.GoToPreviousPageCommand.ExecuteAsync(null);

            await Task.Delay(300);
            tcs.TrySetResult(new { success = true, currentPage = viewer.CurrentPageNumber, totalPages = viewer.TotalPages });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private static async Task ZoomOnUiThread(ZoomRequest body, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            if (body.Level.HasValue)
                await viewer.SetZoomCommand.ExecuteAsync(body.Level.Value);
            else if (body.Action == "in")
                await viewer.ZoomInCommand.ExecuteAsync(null);
            else if (body.Action == "out")
                await viewer.ZoomOutCommand.ExecuteAsync(null);

            await Task.Delay(300);
            tcs.TrySetResult(new { success = true, zoomLevel = viewer.ZoomLevel });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private static void MapStateEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/state", async (HttpContext _) =>
        {
            var state = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = GetMainWindow();
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
                        searchVisible = activeViewer.ViewState.IsSearchPanelVisible
                    } : null
                };
            });

            return Results.Json(state);
        })
        .WithName("GetGuiState")
        .WithSummary("Get full UI state")
        .WithDescription("Returns the current state of the GUI including window, tabs, and active viewer information.");
    }

    private static void MapOpenEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/open", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<OpenFileRequest>();
                if (body?.FilePath == null)
                    return Results.BadRequest(new { error = "filePath is required" });

                if (!File.Exists(body.FilePath))
                    return Results.BadRequest(new { error = $"File not found: {body.FilePath}" });

                // Get the ViewModel reference from UI thread
                var vm = await Dispatcher.UIThread.InvokeAsync(() => GetMainWindow()?.ViewModel);
                if (vm == null)
                    return Results.Json(new { success = false, error = "No main window" });

                // OpenFileInTabAsync may need UI thread internally, so invoke it there
                // but use a simple Post + TCS to avoid deadlock
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    // Kick off the async work as fire-and-forget on UI thread
                    _ = OpenFileOnUiThread(vm, body.FilePath, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("OpenFile")
        .WithSummary("Open a PDF file")
        .WithDescription("Opens a PDF file by path in a new tab.");
    }

    private static void MapNavigateEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/navigate", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<NavigateRequest>();
                if (body == null)
                    return Results.BadRequest(new { error = "Request body is required" });

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = NavigateOnUiThread(body, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Navigate")
        .WithSummary("Navigate to a page")
        .WithDescription("Navigate to a specific page number or use 'next'/'previous' actions.");
    }

    private static void MapZoomEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/zoom", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<ZoomRequest>();
                if (body == null)
                    return Results.BadRequest(new { error = "Request body is required" });

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = ZoomOnUiThread(body, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Zoom")
        .WithSummary("Control zoom level")
        .WithDescription("Set a specific zoom level or use 'in'/'out' actions.");
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
                var viewer = GetActiveViewer();
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
                var vm = GetMainWindow()?.ViewModel;
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
                var window = GetMainWindow();
                if (window == null)
                    return (byte[]?)null;

                var pixelSize = new global::Avalonia.PixelSize(
                    (int)window.Bounds.Width,
                    (int)window.Bounds.Height);

                if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
                    return null;

                var renderTarget = new global::Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize);
                renderTarget.Render(window);

                using var ms = new MemoryStream();
                renderTarget.Save(ms);
                return ms.ToArray();
            });

            if (pngBytes == null)
                return Results.Problem("No window available or window has zero size");

            return Results.File(pngBytes, "image/png", "screenshot.png");
        })
        .WithName("Screenshot")
        .WithSummary("Capture a screenshot")
        .WithDescription("Captures a PNG screenshot of the main application window.");
    }

    private static void MapRefreshEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/refresh", async (HttpContext ctx) =>
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RefreshOnUiThread(tcs);
            });

            var result = await tcs.Task;
            return Results.Json(result);
        })
        .WithName("RefreshPage")
        .WithSummary("Force re-render current page")
        .WithDescription("Forces the active viewer to re-render the current page, reflecting any annotation or content changes.");
    }

    private static async Task RefreshOnUiThread(TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            // Navigate to the same page triggers re-render
            var page = viewer.CurrentPageNumber;
            await viewer.GoToPageCommand.ExecuteAsync(page);
            await Task.Delay(500);

            tcs.TrySetResult(new { success = true, currentPage = viewer.CurrentPageNumber });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private static void MapAnnotateEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/annotate", async (HttpContext ctx, IAnnotationService annotationService) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<AnnotateRequest>();
                if (body == null)
                    return Results.BadRequest(new { error = "Request body is required" });

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = AnnotateOnUiThread(body, annotationService, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("AnnotateGui")
        .WithSummary("Create annotation on GUI document")
        .WithDescription("Creates an annotation on the active GUI document and re-renders the page to show it.");
    }

    private static async Task AnnotateOnUiThread(
        AnnotateRequest body,
        IAnnotationService annotationService,
        TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            var document = viewer.CurrentDocument;
            if (document == null) { tcs.TrySetResult(new { success = false, error = "No document loaded" }); return; }

            var annotType = Enum.TryParse<AnnotationType>(body.Type, true, out var at) ? at : AnnotationType.Highlight;
            var pageNumber = body.PageNumber ?? (viewer.CurrentPageNumber - 1); // 0-based

            var fillColor = System.Drawing.Color.FromArgb(80, 255, 255, 0); // default yellow highlight
            if (!string.IsNullOrEmpty(body.Color))
            {
                try { fillColor = System.Drawing.ColorTranslator.FromHtml(body.Color); } catch { }
            }

            float x = body.X ?? 50;
            float y = body.Y ?? 700;
            float w = body.Width ?? 200;
            float h = body.Height ?? 30;

            var annotation = new Annotation
            {
                Type = annotType,
                PageNumber = pageNumber,
                Bounds = new PdfRectangle(x, y, x + w, y + h),
                FillColor = fillColor,
                StrokeColor = System.Drawing.Color.FromArgb(255, fillColor.R, fillColor.G, fillColor.B),
                Contents = body.Contents ?? "",
                Opacity = body.Opacity ?? 0.5f
            };

            var result = await annotationService.CreateAnnotationAsync(document, annotation);

            if (!result.IsSuccess)
            {
                tcs.TrySetResult(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Failed" });
                return;
            }

            // Re-render to show the annotation
            await viewer.GoToPageCommand.ExecuteAsync(viewer.CurrentPageNumber);
            await Task.Delay(500);

            tcs.TrySetResult(new
            {
                success = true,
                type = annotType.ToString(),
                pageNumber,
                annotationId = result.Value.Id
            });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private record OpenFileRequest(string? FilePath);
    private record NavigateRequest(int? Page, string? Action);
    private record ZoomRequest(double? Level, string? Action);
    private record ToggleRequest(string? Panel);
    private record AnnotateRequest(
        string? Type, int? PageNumber,
        float? X, float? Y, float? Width, float? Height,
        string? Color, string? Contents, float? Opacity);
}
