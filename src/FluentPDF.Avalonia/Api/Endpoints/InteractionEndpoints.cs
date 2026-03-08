using System.Linq;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentPDF.Avalonia.Controls;
using FluentPDF.Avalonia.Views;
using FluentPDF.Core.Models;
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
        MapMultiSelectionEndpoint(group);
        MapMoveSelectionEndpoint(group);
        MapMarqueeSelectEndpoint(group);
        MapContainmentModeEndpoint(group);
        MapResizeSelectionEndpoint(group);
        MapSelectionHandlesEndpoint(group);
        MapLockToggleEndpoint(group);
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
                    return new { hasSelection = false, count = 0, selectedObjects = Array.Empty<object>(), containmentMode = "intersect" } as object;

                var objects = page.GetSelectedObjects();
                var primary = page.GetSelectedObject();
                return new
                {
                    hasSelection = objects.Count > 0,
                    count = objects.Count,
                    primaryIndex = primary?.Index,
                    containmentMode = page.ContainmentMode.ToString().ToLower(),
                    selectedObjects = objects.Select(o => new { o.Index, o.Type, o.Left, o.Bottom, o.Right, o.Top }).ToArray()
                } as object;
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
                if (page == null) return (error: "No active viewer", obj: (PageObjectInfo?)null, docId: (string?)null, pageIndex: default(PageIndex), isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var selected = page.GetSelectedObject();
                if (selected == null) return (error: "Nothing selected", obj: (PageObjectInfo?)null, docId: (string?)null, pageIndex: default(PageIndex), isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var vm = page.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel;
                if (vm?.CurrentDocument == null) return (error: "No document", obj: (PageObjectInfo?)null, docId: (string?)null, pageIndex: default(PageIndex), isLocked: false, isOriginal: false, page: (PdfViewerPage?)null);
                var pi = new PageIndex(vm.CurrentPageNumber - 1);
                var isOriginal = vm.OriginalObjectCounts.TryGetValue(pi.Value, out var origCount) ? selected.Index < origCount : true;
                return (error: (string?)null, obj: selected, docId: vm.CurrentDocument.FilePath, pageIndex: pi, isLocked: vm.IsOriginalObjectsLocked, isOriginal, page);
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
                var deleted = await shapeService.RemovePageObjectAsync(info.docId!, info.pageIndex, info.obj!.Index);

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

    private static void MapMultiSelectionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/selection/multi-click", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<MultiClickRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var hit = await page.SelectObjectAtPdfCoordsAsync(body.PdfX, body.PdfY);
                    // Note: SelectObjectAtPdfCoordsAsync does single select.
                    // For modifier-aware selection, we simulate via HandleSelectClickAsync indirectly.
                    var selected = page.GetSelectedObjects();
                    return new
                    {
                        success = true,
                        count = selected.Count,
                        selectedObjects = selected.Select(o => new { o.Index, o.Type, o.Left, o.Bottom, o.Right, o.Top }).ToArray()
                    } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractMultiClick");
    }

    private static void MapMoveSelectionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/selection/move", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<MoveRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required (deltaPdfX, deltaPdfY)" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var vm = page.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel;
                    if (vm?.CurrentDocument == null) return new { success = false, error = "No document" } as object;

                    var selected = page.GetSelectedObjects();
                    if (selected.Count == 0) return new { success = false, error = "Nothing selected" } as object;

                    var shapeService = App.GetService<IShapeService>();
                    var docId = vm.CurrentDocument.FilePath;
                    var pageIndex = new PageIndex(vm.CurrentPageNumber - 1);
                    int moved = 0;

                    foreach (var obj in selected)
                    {
                        if (await shapeService.MovePageObjectAsync(docId, pageIndex, obj.Index, body.DeltaPdfX, body.DeltaPdfY))
                            moved++;
                    }

                    if (moved > 0)
                        await vm.RefreshCurrentPageAsync();

                    page.ClearSelection();
                    return new { success = true, movedCount = moved } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractMoveSelection");
    }

    private static void MapMarqueeSelectEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/selection/marquee", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<MarqueeRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var vm = page.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel;
                    if (vm?.CurrentDocument == null) return new { success = false, error = "No document" } as object;

                    var shapeService = App.GetService<IShapeService>();
                    var docId = vm.CurrentDocument.FilePath;
                    var pageIndex = new PageIndex(vm.CurrentPageNumber - 1);
                    var objects = await shapeService.GetPageObjectsAsync(docId, pageIndex);

                    var left = Math.Min(body.PdfX1, body.PdfX2);
                    var right = Math.Max(body.PdfX1, body.PdfX2);
                    var bottom = Math.Min(body.PdfY1, body.PdfY2);
                    var top = Math.Max(body.PdfY1, body.PdfY2);

                    bool fullyContained = body.FullyContained ?? (page.ContainmentMode == Helpers.SelectionContainment.FullyContained);
                    var hits = new System.Collections.Generic.List<PageObjectInfo>();

                    foreach (var obj in objects)
                    {
                        if (fullyContained)
                        {
                            if (obj.Left >= left && obj.Right <= right && obj.Bottom >= bottom && obj.Top <= top)
                                hits.Add(obj);
                        }
                        else
                        {
                            if (obj.Right >= left && obj.Left <= right && obj.Top >= bottom && obj.Bottom <= top)
                                hits.Add(obj);
                        }
                    }

                    return new
                    {
                        success = true,
                        count = hits.Count,
                        selectedObjects = hits.Select(o => new { o.Index, o.Type, o.Left, o.Bottom, o.Right, o.Top }).ToArray()
                    } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractMarqueeSelect");
    }

    private static void MapContainmentModeEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/selection/containment", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<ContainmentRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required (mode: intersect|fullyContained)" });

            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var page = GetActiveViewerPage();
                if (page == null) return new { success = false, error = "No active viewer" } as object;

                page.ContainmentMode = body.Mode?.ToLower() == "fullycontained"
                    ? Helpers.SelectionContainment.FullyContained
                    : Helpers.SelectionContainment.Intersect;

                return new { success = true, mode = page.ContainmentMode.ToString() } as object;
            });

            return Results.Json(result);
        }).WithName("InteractContainmentMode");
    }

    private static void MapLockToggleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/lock-toggle", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var page = GetActiveViewerPage();
                var vm = page?.DataContext as FluentPDF.Core.ViewModels.PdfViewerViewModel;
                if (vm == null) return (object)new { success = false, error = "No active viewer" };
                vm.IsOriginalObjectsLocked = !vm.IsOriginalObjectsLocked;
                return (object)new { success = true, locked = vm.IsOriginalObjectsLocked };
            });
            return Results.Json(result);
        }).WithName("InteractLockToggle");
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
    private record MultiClickRequest(float PdfX, float PdfY, string? Modifier);
    private record MoveRequest(float DeltaPdfX, float DeltaPdfY);
    private record MarqueeRequest(float PdfX1, float PdfY1, float PdfX2, float PdfY2, bool? FullyContained);
    private record ContainmentRequest(string? Mode);
    private record ResizeRequest(float ScaleX, float ScaleY, float? AnchorPdfX, float? AnchorPdfY);

    private static void MapResizeSelectionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/selection/resize", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<ResizeRequest>();
            if (body == null) return Results.BadRequest(new { error = "Body required (scaleX, scaleY)" });

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = RunOnUiThread(async () =>
                {
                    var page = GetActiveViewerPage();
                    if (page == null) return new { success = false, error = "No active viewer" } as object;

                    var selected = page.GetSelectedObject();
                    if (selected == null) return new { success = false, error = "Nothing selected" } as object;

                    // Default anchor: center of object
                    var anchorX = body.AnchorPdfX ?? (selected.Left + selected.Right) / 2;
                    var anchorY = body.AnchorPdfY ?? (selected.Bottom + selected.Top) / 2;

                    var result = await page.ResizeSelectedObjectAsync(body.ScaleX, body.ScaleY, anchorX, anchorY);
                    return new { success = result, scaleX = body.ScaleX, scaleY = body.ScaleY, anchorPdfX = anchorX, anchorPdfY = anchorY } as object;
                }, tcs);
            });

            return Results.Json(await tcs.Task);
        }).WithName("InteractResizeSelection");
    }

    private static void MapSelectionHandlesEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/selection/handles", async (HttpContext ctx) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var page = GetActiveViewerPage();
                if (page == null) return new { hasSelection = false, handles = Array.Empty<object>() } as object;

                var selected = page.GetSelectedObject();
                if (selected == null) return new { hasSelection = false, handles = Array.Empty<object>() } as object;

                var cx = (selected.Left + selected.Right) / 2;
                var cy = (selected.Bottom + selected.Top) / 2;
                var handles = new object[]
                {
                    new { position = "topLeft", pdfX = selected.Left, pdfY = selected.Top },
                    new { position = "topRight", pdfX = selected.Right, pdfY = selected.Top },
                    new { position = "bottomLeft", pdfX = selected.Left, pdfY = selected.Bottom },
                    new { position = "bottomRight", pdfX = selected.Right, pdfY = selected.Bottom },
                    new { position = "middleLeft", pdfX = selected.Left, pdfY = cy },
                    new { position = "middleRight", pdfX = selected.Right, pdfY = cy },
                    new { position = "topMiddle", pdfX = cx, pdfY = selected.Top },
                    new { position = "bottomMiddle", pdfX = cx, pdfY = selected.Bottom },
                };
                return new
                {
                    hasSelection = true,
                    objectIndex = selected.Index,
                    objectType = selected.Type,
                    bounds = new { selected.Left, selected.Bottom, selected.Right, selected.Top },
                    handles
                } as object;
            });

            return Results.Json(result);
        }).WithName("InteractSelectionHandles");
    }
}
