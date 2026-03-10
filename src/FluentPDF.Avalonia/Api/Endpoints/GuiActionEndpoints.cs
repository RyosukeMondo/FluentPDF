// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// GUI endpoints for document actions: open, draw, select-text, annotate, watermark, draw-shape, save.
/// All GUI access is marshalled to the Avalonia UI thread.
/// </summary>
public static class GuiActionEndpoints
{
    public static void MapGuiActionEndpoints(RouteGroupBuilder group)
    {
        MapOpenEndpoint(group);
        MapAnnotateEndpoint(group);
        MapDrawEndpoint(group);
        MapSelectTextEndpoint(group);
        MapWatermarkEndpoint(group);
        MapDrawShapeEndpoint(group);
        MapSaveEndpoint(group);
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
                var vm = await Dispatcher.UIThread.InvokeAsync(() => GuiEndpoints.GetMainWindow()?.ViewModel);
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
            var viewer = GuiEndpoints.GetActiveViewer();
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
                    var viewer = GuiEndpoints.GetActiveViewer();
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
            var viewer = GuiEndpoints.GetActiveViewer();
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
            var viewer = GuiEndpoints.GetActiveViewer();
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
                    var viewer = GuiEndpoints.GetActiveViewer();
                    if (viewer?.CurrentDocument == null)
                        return (object)new { success = false, error = "No active document" };

                    var shapeService = App.GetService<IShapeService>();
                    var docId = viewer.CurrentDocument.FilePath;
                    var pageIndex = PageIndex.FromPageNumber(body.PageNumber ?? 1);
                    var fill = body.FillColor ?? "#FF000080";
                    var stroke = body.StrokeColor ?? "#000000";
                    var sw = body.StrokeWidth ?? 2f;
                    string? shapeId = null;

                    switch (body.Shape.ToLowerInvariant())
                    {
                        case "rectangle":
                            shapeId = await shapeService.AddRectangleAsync(docId, pageIndex,
                                body.X ?? 100, body.Y ?? 100, body.Width ?? 50, body.Height ?? 30,
                                fill, stroke, sw, source: "ai");
                            break;
                        case "circle":
                            shapeId = await shapeService.AddCircleAsync(docId, pageIndex,
                                body.X ?? 150, body.Y ?? 150, body.Width ?? 25,
                                fill, stroke, sw, source: "ai");
                            break;
                        case "line":
                            shapeId = await shapeService.AddLineAsync(docId, pageIndex,
                                body.X ?? 100, body.Y ?? 100, body.X2 ?? 200, body.Y2 ?? 200,
                                stroke, sw, source: "ai");
                            break;
                        default:
                            return (object)new { success = false, error = $"Unknown shape: {body.Shape}" };
                    }

                    if (shapeId != null)
                    {
                        viewer.HasPageModifications = true;
                        await viewer.RefreshCurrentPageAsync();
                    }

                    return (object)new { success = shapeId != null, id = shapeId, shape = body.Shape, page = pageIndex.ToPageNumber() };
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
                        var viewer = GuiEndpoints.GetActiveViewer();
                        if (viewer?.CurrentDocument == null)
                        {
                            tcs.SetResult(new { success = false, error = "No active document" });
                            return;
                        }

                        // Invoke save callback directly (awaitable, unlike fire-and-forget command)
                        if (viewer.SaveDocumentCallback != null)
                        {
                            viewer.PreSaveAction?.Invoke(viewer.CurrentDocument.FilePath);
                            var outputPath = body?.OutputPath ?? viewer.CurrentDocument.FilePath;
                            var saved = await viewer.SaveDocumentCallback(viewer.CurrentDocument, outputPath);
                            if (saved) viewer.HasPageModifications = false;
                            tcs.SetResult(new
                            {
                                success = saved,
                                filePath = viewer.CurrentDocument.FilePath,
                                hasUnsavedChanges = viewer.HasUnsavedChanges
                            });
                        }
                        else
                        {
                            tcs.SetResult(new { success = false, error = "Save callback not configured" });
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(new { success = false, error = ex.ToString() });
                    }
                });

                var result = await Task.WhenAny(tcs.Task, Task.Delay(30000));
                if (result == tcs.Task)
                    return Results.Json(await tcs.Task);
                return Results.Json(new { success = false, error = "Save timed out after 30 seconds" });
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Save")
        .WithSummary("Save the current document");
    }

    private record OpenFileRequest(string? FilePath);
    private record AnnotateRequest(
        string? Type, int? PageNumber,
        float? X, float? Y, float? Width, float? Height,
        string? Color, string? Contents, float? Opacity);
    private record DrawRequest(string? Tool, string? StrokeColor, string? FillColor, float? StrokeWidth);
    private record WatermarkRequest(string? Text, float? Opacity, int? FontSize, float? Rotation);
    private record DrawShapeRequest(
        string? Shape, int? PageNumber,
        float? X, float? Y, float? Width, float? Height,
        float? X2, float? Y2,
        string? FillColor, string? StrokeColor, float? StrokeWidth);
    private record SaveRequest(string? OutputPath);
}
