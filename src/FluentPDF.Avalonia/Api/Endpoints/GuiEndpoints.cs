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
        MapPageOpEndpoint(group);
        MapDrawEndpoint(group);
        MapSelectTextEndpoint(group);
        MapWatermarkEndpoint(group);
        MapDrawShapeEndpoint(group);
        MapSaveEndpoint(group);
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

    private static void MapPageOpEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/page-op", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<PageOpRequest>();
                if (body?.Action == null)
                    return Results.BadRequest(new { error = "action is required (rotate_cw|rotate_ccw|delete|insert_blank)" });

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = PageOpOnUiThread(body.Action, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("PageOp")
        .WithSummary("Execute page operation")
        .WithDescription("Rotate, delete, or insert pages via ViewModel commands.");
    }

    private static async Task PageOpOnUiThread(string action, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            switch (action.ToLowerInvariant())
            {
                case "rotate_cw":
                    await viewer.RotatePageClockwiseCommand.ExecuteAsync(null);
                    break;
                case "rotate_ccw":
                    await viewer.RotatePageCounterClockwiseCommand.ExecuteAsync(null);
                    break;
                case "delete":
                    await viewer.DeleteCurrentPageCommand.ExecuteAsync(null);
                    break;
                case "insert_blank":
                    await viewer.InsertBlankPageCommand.ExecuteAsync(null);
                    break;
                default:
                    tcs.TrySetResult(new { success = false, error = $"Unknown action: {action}" });
                    return;
            }

            await Task.Delay(300);
            tcs.TrySetResult(new
            {
                success = true,
                currentPage = viewer.CurrentPageNumber,
                totalPages = viewer.TotalPages,
                action
            });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private static void MapDrawEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/draw", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<DrawRequest>();
                if (body?.Tool == null)
                    return Results.BadRequest(new { error = "tool is required (Rectangle|Circle|Line|Freehand|Text|None)" });

                var result = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var viewer = GetActiveViewer();
                    if (viewer == null)
                        return (object)new { success = false, error = "No active document" };

                    if (!Enum.TryParse<DrawingTool>(body.Tool, ignoreCase: true, out var tool))
                        return (object)new { success = false, error = $"Unknown tool: {body.Tool}" };

                    viewer.ActiveDrawingTool = tool;
                    if (tool != DrawingTool.None)
                        viewer.IsDrawingToolbarVisible = true;

                    if (body.StrokeColor != null)
                        viewer.DrawingStrokeColor = body.StrokeColor;
                    if (body.FillColor != null)
                        viewer.DrawingFillColor = body.FillColor;
                    if (body.StrokeWidth.HasValue)
                        viewer.DrawingStrokeWidth = body.StrokeWidth.Value;

                    return (object)new
                    {
                        success = true,
                        activeTool = viewer.ActiveDrawingTool.ToString(),
                        strokeColor = viewer.DrawingStrokeColor,
                        fillColor = viewer.DrawingFillColor,
                        strokeWidth = viewer.DrawingStrokeWidth
                    };
                });

                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Draw")
        .WithSummary("Set drawing tool and properties")
        .WithDescription("Sets the active drawing tool, stroke color, fill color, and stroke width.");
    }

    private static void MapSelectTextEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/select-text", async (HttpContext ctx) =>
        {
            try
            {
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = SelectTextOnUiThread(tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("SelectText")
        .WithSummary("Select all text on current page")
        .WithDescription("Selects all text on the current page and returns the selected text content.");
    }

    private static async Task SelectTextOnUiThread(TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            await viewer.SelectAllTextCommand.ExecuteAsync(null);

            tcs.TrySetResult(new
            {
                success = true,
                hasSelectedText = viewer.HasSelectedText,
                selectedText = viewer.SelectedText ?? string.Empty,
                charCount = viewer.SelectedText?.Length ?? 0
            });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.ToString() }); }
    }

    private static void MapWatermarkEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/watermark", async (HttpContext ctx, IWatermarkService watermarkService) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<WatermarkRequest>();
                if (string.IsNullOrWhiteSpace(body?.Text))
                    return Results.BadRequest(new { error = "text is required" });

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() =>
                {
                    _ = WatermarkOnUiThread(body, watermarkService, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("WatermarkGui")
        .WithSummary("Apply text watermark")
        .WithDescription("Applies a text watermark to the active document.");
    }

    private static async Task WatermarkOnUiThread(
        WatermarkRequest body,
        IWatermarkService watermarkService,
        TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            var document = viewer.CurrentDocument;
            if (document == null) { tcs.TrySetResult(new { success = false, error = "No document loaded" }); return; }

            var config = new TextWatermarkConfig
            {
                Text = body.Text!,
                Opacity = body.Opacity ?? 0.3f,
                FontSize = body.FontSize ?? 48,
                RotationDegrees = body.Rotation ?? -45f
            };

            var result = await watermarkService.ApplyTextWatermarkAsync(
                document, config, WatermarkPageRange.All);

            if (!result.IsSuccess)
            {
                tcs.TrySetResult(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Failed" });
                return;
            }

            // Re-render to show the watermark
            await viewer.GoToPageCommand.ExecuteAsync(viewer.CurrentPageNumber);
            await Task.Delay(500);

            tcs.TrySetResult(new { success = true, text = body.Text });
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
    private record PageOpRequest(string? Action);
    private record DrawRequest(string? Tool, string? StrokeColor, string? FillColor, float? StrokeWidth);
    private record WatermarkRequest(string? Text, float? Opacity, int? FontSize, float? Rotation);
    private record DrawShapeRequest(
        string? Shape, int? PageNumber,
        float? X, float? Y, float? Width, float? Height,
        float? X2, float? Y2,
        string? FillColor, string? StrokeColor, float? StrokeWidth);
    private record SaveRequest(string? OutputPath);

    /// <summary>
    /// Draws a shape directly on the PDF page via ShapeService (for autonomous testing).
    /// POST /api/gui/draw-shape { shape, pageNumber, x, y, width, height, ... }
    /// </summary>
    private static void MapDrawShapeEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/draw-shape", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<DrawShapeRequest>();
                if (body?.Shape == null)
                    return Results.BadRequest(new { error = "shape is required (Rectangle|Circle|Line)" });

                var result = await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var viewer = GetActiveViewer();
                    if (viewer?.CurrentDocument == null)
                        return (object)new { success = false, error = "No active document" };

                    var shapeService = App.GetService<IShapeService>();
                    var docId = viewer.CurrentDocument.FilePath;
                    var page = (body.PageNumber ?? 1) - 1;
                    var fill = body.FillColor ?? "#FF000080";
                    var stroke = body.StrokeColor ?? "#000000";
                    var sw = body.StrokeWidth ?? 2f;
                    bool ok = false;

                    switch (body.Shape.ToLowerInvariant())
                    {
                        case "rectangle":
                            ok = await shapeService.AddRectangleAsync(docId, page,
                                body.X ?? 100, body.Y ?? 100, body.Width ?? 50, body.Height ?? 30,
                                fill, stroke, sw);
                            break;
                        case "circle":
                            ok = await shapeService.AddCircleAsync(docId, page,
                                body.X ?? 150, body.Y ?? 150, body.Width ?? 25,
                                fill, stroke, sw);
                            break;
                        case "line":
                            ok = await shapeService.AddLineAsync(docId, page,
                                body.X ?? 100, body.Y ?? 100, body.X2 ?? 200, body.Y2 ?? 200,
                                stroke, sw);
                            break;
                        default:
                            return (object)new { success = false, error = $"Unknown shape: {body.Shape}" };
                    }

                    if (ok)
                        await viewer.RefreshCurrentPageAsync();

                    return (object)new { success = ok, shape = body.Shape, page = page + 1 };
                });

                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("DrawShape")
        .WithSummary("Draw a shape on the current PDF page");
    }

    /// <summary>
    /// Saves the current document. Triggers QPDF content stream patching if shapes were added.
    /// POST /api/gui/save { outputPath? }
    /// </summary>
    private static void MapSaveEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/save", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<SaveRequest>();

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var viewer = GetActiveViewer();
                        if (viewer?.CurrentDocument == null)
                        {
                            tcs.SetResult(new { success = false, error = "No active document" });
                            return;
                        }

                        // Trigger save via ViewModel command
                        if (viewer.SaveCommand.CanExecute(null))
                        {
                            viewer.SaveCommand.Execute(null);
                            // Wait a bit for save to complete
                            await Task.Delay(2000);
                            tcs.SetResult(new
                            {
                                success = true,
                                filePath = viewer.CurrentDocument.FilePath,
                                hasUnsavedChanges = viewer.HasUnsavedChanges
                            });
                        }
                        else
                        {
                            tcs.SetResult(new { success = false, error = "Save command not available (no unsaved changes?)" });
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(new { success = false, error = ex.ToString() });
                    }
                });

                var result = await Task.WhenAny(tcs.Task, Task.Delay(10000));
                if (result == tcs.Task)
                    return Results.Json(await tcs.Task);
                return Results.Json(new { success = false, error = "Save timed out after 10 seconds" });
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Save")
        .WithSummary("Save the current document");
    }
}
