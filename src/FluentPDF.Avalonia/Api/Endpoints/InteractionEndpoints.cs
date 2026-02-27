using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentPDF.Avalonia.Controls;
using FluentPDF.Avalonia.Views;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for diagnosing interactive UI features (select, draw, delete, text).
/// All coordinates are in PDF space (origin bottom-left, points).
/// </summary>
public static class InteractionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/gui/interact")
            .WithTags("Interaction");

        MapClickEndpoint(group);
        MapDragEndpoint(group);
        MapTextEndpoint(group);
        MapSelectionGetEndpoint(group);
        MapSelectionDeleteEndpoint(group);
        MapObjectsEndpoint(group);
        MapHitTestEndpoint(group);
    }

    private static PdfViewerPage? GetActiveViewerPage()
    {
        var appInstance = (App)global::Avalonia.Application.Current!;
        var window = appInstance.MainWindow;
        if (window == null) return null;

        // Walk visual tree to find active PdfViewerControl
        return FindPdfViewerControl(window)?.ActiveViewerPage;
    }

    private static PdfViewerControl? FindPdfViewerControl(global::Avalonia.Visual visual)
    {
        if (visual is PdfViewerControl ctrl) return ctrl;

        foreach (var child in visual.GetVisualChildren())
        {
            var result = FindPdfViewerControl(child);
            if (result != null) return result;
        }
        return null;
    }

    private static void MapClickEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/click", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<CoordsRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var hit = await page.SelectObjectAtPdfCoordsAsync(body.PdfX, body.PdfY);
                    return hit != null
                        ? new { success = true, selected = true, objectIndex = hit.Index, objectType = hit.Type,
                                left = hit.Left, bottom = hit.Bottom, right = hit.Right, top = hit.Top } as object
                        : new { success = true, selected = false } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractClick");
    }

    private static void MapDragEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/drag", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<DragRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var result = await page.DrawShapeAtPdfCoordsAsync(body.StartX, body.StartY, body.EndX, body.EndY);
                    return new { success = result } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractDrag");
    }

    private static void MapTextEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/text", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<TextRequest>();
            if (body == null || string.IsNullOrEmpty(body.Text))
                return Results.BadRequest(new { error = "pdfX, pdfY, text required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var result = await page.AddTextAtPdfCoordsAsync(
                        body.PdfX, body.PdfY, body.Text,
                        body.FontSize ?? 12f, body.FontName ?? "Helvetica");
                    return new { success = result } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractText");
    }

    private static void MapSelectionGetEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/selection", async (HttpContext ctx) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var page = GetActiveViewerPage();
                if (page == null)
                    return new { hasSelection = false, hasHighlight = false, selectedObject = (object?)null };

                var obj = page.GetSelectedObject();
                return new
                {
                    hasSelection = obj != null,
                    hasHighlight = page.HasSelectionHighlight,
                    selectedObject = obj != null
                        ? new { obj.Index, obj.Type, obj.Left, obj.Bottom, obj.Right, obj.Top } as object
                        : null
                };
            });

            return Results.Json(result);
        }).WithName("InteractSelectionGet");
    }

    private static void MapSelectionDeleteEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/selection", async (HttpContext ctx) =>
        {
            // Get selection info and document details from UI thread
            var info = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var page = GetActiveViewerPage();
                if (page == null) return (error: "No active viewer", obj: (PageObjectInfo?)null, docId: (string?)null, pageNum: 0, isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var selected = page.GetSelectedObject();
                if (selected == null) return (error: "Nothing selected", obj: (PageObjectInfo?)null, docId: (string?)null, pageNum: 0, isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var vm = page.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel;
                if (vm?.CurrentDocument == null) return (error: "No document", obj: (PageObjectInfo?)null, docId: (string?)null, pageNum: 0, isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var pageIdx = vm.CurrentPageNumber - 1;
                var isOriginal = vm.OriginalObjectCounts.TryGetValue(pageIdx, out var origCount) ? selected.Index < origCount : true;
                return (error: (string?)null, obj: selected, docId: vm.CurrentDocument.FilePath, pageNum: pageIdx, isLocked: vm.IsOriginalObjectsLocked, isOriginal, page);
            });

            if (info.error != null)
                return Results.Json(new { success = false, error = info.error });

            // Check lock
            if (info.isLocked && info.isOriginal)
                return Results.Json(new { success = false, error = "Original objects are locked" });

            // Call ShapeService directly from HTTP thread (not UI thread) to avoid PDFium thread contention
            try
            {
                var shapeService = App.GetService<IShapeService>();
                var deleted = await shapeService.RemovePageObjectAsync(info.docId!, info.pageNum, info.obj!.Index);

                if (deleted)
                {
                    // Clean up UI on UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        info.page?.ClearSelection();
                        _ = (info.page?.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel)?.RefreshCurrentPageAsync();
                    });
                }

                return Results.Json(new { success = deleted, deletedIndex = info.obj!.Index, deletedType = info.obj!.Type });
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.Message });
            }
        }).WithName("InteractSelectionDelete");
    }

    private static void MapObjectsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/objects", async (HttpContext ctx) =>
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer", objects = Array.Empty<object>() } as object;

                    var objects = await page.GetCurrentPageObjectsAsync();
                    return new
                    {
                        success = true,
                        count = objects.Count,
                        objects = objects.Select(o => new { o.Index, o.Type, o.Left, o.Bottom, o.Right, o.Top })
                    } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractObjects");
    }

    private static void MapHitTestEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/hit-test", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<CoordsRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var hit = await page.HitTestAtPdfCoordsAsync(body.PdfX, body.PdfY);
                    return hit != null
                        ? new { success = true, found = true, objectIndex = hit.Index, objectType = hit.Type,
                                left = hit.Left, bottom = hit.Bottom, right = hit.Right, top = hit.Top } as object
                        : new { success = true, found = false } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractHitTest");
    }

    private static async Task RunOnUiThread(Func<Task<object>> action, TaskCompletionSource<object> tcs)
    {
        try
        {
            var result = await action();
            tcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            tcs.TrySetResult(new { success = false, error = ex.Message });
        }
    }

    private record CoordsRequest(float PdfX, float PdfY);
    private record DragRequest(float StartX, float StartY, float EndX, float EndY);
    private record TextRequest(float PdfX, float PdfY, string Text, float? FontSize, string? FontName);
}
