// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// GUI endpoints for page navigation, zoom, refresh, and page operations.
/// All GUI access is marshalled to the Avalonia UI thread.
/// </summary>
public static class GuiNavigationEndpoints
{
    public static void MapGuiNavigationEndpoints(RouteGroupBuilder group)
    {
        MapNavigateEndpoint(group);
        MapZoomEndpoint(group);
        MapRefreshEndpoint(group);
        MapPageOpEndpoint(group);
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

    private static async Task NavigateOnUiThread(NavigateRequest body, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GuiEndpoints.GetActiveViewer();
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

    private static async Task ZoomOnUiThread(ZoomRequest body, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GuiEndpoints.GetActiveViewer();
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
            var viewer = GuiEndpoints.GetActiveViewer();
            if (viewer == null) { tcs.TrySetResult(new { success = false, error = "No active document" }); return; }

            // Navigate to the same page triggers re-render
            var page = viewer.CurrentPageNumber;
            await viewer.GoToPageCommand.ExecuteAsync(page);
            await Task.Delay(500);

            tcs.TrySetResult(new { success = true, currentPage = viewer.CurrentPageNumber });
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
            var viewer = GuiEndpoints.GetActiveViewer();
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

    private record NavigateRequest(int? Page, string? Action);
    private record ZoomRequest(double? Level, string? Action);
    private record PageOpRequest(string? Action);
}
