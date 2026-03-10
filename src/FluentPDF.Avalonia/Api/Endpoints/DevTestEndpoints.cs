// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Dev/test endpoints for exercising features not covered by existing GUI endpoints.
/// Toggle edit mode, undo/redo, user bookmarks, extended panel toggles, page summary,
/// text extraction, and a comprehensive smoke-test endpoint.
/// </summary>
public static class DevTestEndpoints
{
    public static void MapDevTestEndpoints(RouteGroupBuilder group)
    {
        MapToggleEditEndpoint(group);
        MapFeaturesEndpoint(group);
        MapUserBookmarksEndpoint(group);
        MapExtendedToggleEndpoint(group);
        MapPageInfoEndpoint(group);
        MapTextExtractEndpoint(group);
        SmokeTestEndpoint.MapSmokeTestEndpoint(group);
    }

    /// <summary>POST /api/gui/edit-mode — toggle drawing toolbar visibility.</summary>
    private static void MapToggleEditEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/edit-mode", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<EditModeRequest>();
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GuiEndpoints.GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                if (body?.Enabled != null)
                    viewer.IsDrawingToolbarVisible = body.Enabled.Value;
                else
                    viewer.IsDrawingToolbarVisible = !viewer.IsDrawingToolbarVisible;

                if (viewer.IsDrawingToolbarVisible && viewer.ActiveDrawingTool == DrawingTool.None)
                    viewer.ActiveDrawingTool = DrawingTool.Rectangle;

                return (object)new
                {
                    success = true,
                    editMode = viewer.IsDrawingToolbarVisible,
                    activeTool = viewer.ActiveDrawingTool.ToString()
                };
            });
            return Results.Json(result);
        })
        .WithName("ToggleEditMode")
        .WithSummary("Toggle edit/drawing mode");
    }

    /// <summary>GET /api/gui/features — list all available dev test endpoints.</summary>
    private static void MapFeaturesEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/features", (HttpContext _) =>
        {
            return Results.Json(new
            {
                endpoints = new[]
                {
                    "POST /api/gui/edit-mode — toggle drawing toolbar",
                    "GET  /api/gui/user-bookmarks — list user bookmarks",
                    "POST /api/gui/user-bookmarks/toggle — bookmark current page",
                    "POST /api/gui/panel — toggle any panel (thumbnails|bookmarks|search|annotations|metadata)",
                    "GET  /api/gui/page-info — page summary, dimensions, state",
                    "POST /api/gui/extract-text — extract text from page",
                    "POST /api/gui/smoke-test — comprehensive feature smoke test",
                    "GET  /api/gui/features — this list"
                }
            });
        })
        .WithName("DevFeatures")
        .WithSummary("List dev test endpoints");
    }

    /// <summary>GET/POST /api/gui/user-bookmarks — list or toggle user bookmarks.</summary>
    private static void MapUserBookmarksEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/user-bookmarks", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GuiEndpoints.GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                return (object)new
                {
                    success = true,
                    currentPageBookmarked = viewer.IsCurrentPageBookmarked,
                    bookmarks = viewer.UserBookmarks?.Select(b => new
                    {
                        pageNumber = b.PageNumber,
                        label = b.DisplayLabel,
                        createdAt = b.CreatedAt
                    }).ToArray() ?? Array.Empty<object>()
                };
            });
            return Results.Json(result);
        })
        .WithName("GetUserBookmarks")
        .WithSummary("List user bookmarks for current document");

        group.MapPost("/user-bookmarks/toggle", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GuiEndpoints.GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                var wasBefore = viewer.IsCurrentPageBookmarked;
                viewer.ToggleUserBookmarkCommand.Execute(null);
                return (object)new
                {
                    success = true,
                    page = viewer.CurrentPageNumber,
                    bookmarked = viewer.IsCurrentPageBookmarked,
                    action = wasBefore ? "removed" : "added"
                };
            });
            return Results.Json(result);
        })
        .WithName("ToggleUserBookmark")
        .WithSummary("Toggle bookmark on current page");
    }

    /// <summary>POST /api/gui/panel — extended panel toggle covering ALL panels.</summary>
    private static void MapExtendedToggleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/panel", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<PanelRequest>();
            if (body?.Panel == null)
                return Results.BadRequest(new { error = "panel required: thumbnails|bookmarks|search|annotations|metadata" });

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
                        viewer.ToggleSearchPanelCommand.Execute(null);
                        break;
                    case "annotations":
                        viewer.ToggleAnnotationsCommand.Execute(null);
                        break;
                    case "metadata":
                        viewer.ToggleMetadataCommand.Execute(null);
                        break;
                    default:
                        return (object)new { success = false, error = $"Unknown panel: {body.Panel}" };
                }

                return (object)new
                {
                    success = true,
                    panel = body.Panel,
                    thumbnails = viewer.ViewState.IsSidebarVisible,
                    bookmarks = viewer.ViewState.IsBookmarksPanelVisible,
                    search = viewer.ViewState.IsSearchPanelVisible,
                    annotations = viewer.ViewState.IsAnnotationsPanelVisible,
                    metadata = viewer.ViewState.IsMetadataPanelVisible
                };
            });
            return Results.Json(result);
        })
        .WithName("TogglePanelExtended")
        .WithSummary("Toggle any panel visibility (extended: includes annotations, metadata)");
    }

    /// <summary>GET /api/gui/page-info — page summary, dimensions, text, object count.</summary>
    private static void MapPageInfoEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/page-info", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GuiEndpoints.GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                return (object)new
                {
                    success = true,
                    currentPage = viewer.CurrentPageNumber,
                    totalPages = viewer.TotalPages,
                    pageSummary = viewer.PageSummary,
                    pageWidth = viewer.CurrentPageWidth,
                    pageHeight = viewer.CurrentPageHeight,
                    zoomLevel = viewer.ZoomLevel,
                    isEditMode = viewer.IsDrawingToolbarVisible,
                    activeTool = viewer.ActiveDrawingTool.ToString(),
                    hasSelectedText = viewer.HasSelectedText,
                    selectedTextLength = viewer.SelectedText?.Length ?? 0,
                    isCurrentPageBookmarked = viewer.IsCurrentPageBookmarked,
                    hasUnsavedChanges = viewer.HasUnsavedChanges,
                    statusMessage = viewer.StatusMessage
                };
            });
            return Results.Json(result);
        })
        .WithName("GetPageInfo")
        .WithSummary("Get detailed page info including summary, dimensions, state");
    }

    /// <summary>POST /api/gui/extract-text — extract text from current page.</summary>
    private static void MapTextExtractEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/extract-text", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<ExtractTextRequest>();

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(() =>
            {
                _ = ExtractTextOnUiThread(body?.PageNumber, tcs);
            });
            return Results.Json(await tcs.Task);
        })
        .WithName("ExtractTextDev")
        .WithSummary("Extract text from a page");
    }

    private static async Task ExtractTextOnUiThread(int? pageNumber, TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GuiEndpoints.GetActiveViewer();
            if (viewer?.CurrentDocument == null)
            {
                tcs.TrySetResult(new { success = false, error = "No active document" });
                return;
            }

            var textService = App.GetService<ITextExtractionService>();
            if (textService == null)
            {
                tcs.TrySetResult(new { success = false, error = "Text service unavailable" });
                return;
            }

            var page = pageNumber ?? viewer.CurrentPageNumber;
            var textResult = await textService.ExtractTextAsync(viewer.CurrentDocument, page);
            var text = textResult.IsSuccess ? textResult.Value : null;

            tcs.TrySetResult(new
            {
                success = true,
                pageNumber = page,
                text = text ?? "",
                charCount = text?.Length ?? 0
            });
        }
        catch (Exception ex) { tcs.TrySetResult(new { success = false, error = ex.Message }); }
    }

    private record EditModeRequest(bool? Enabled);
    private record PanelRequest(string? Panel);
    private record ExtractTextRequest(int? PageNumber);
}
