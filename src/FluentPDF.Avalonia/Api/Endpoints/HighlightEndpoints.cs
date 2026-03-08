// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using FluentPDF.Avalonia.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Highlight annotation endpoints for adding highlight annotations to PDF pages.
/// </summary>
public static class HighlightEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/gui")
            .WithTags("Highlight");

        MapHighlightEndpoint(group);
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

    private static void MapHighlightEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/highlight", async (
            HttpContext ctx,
            IAnnotationService annotationService) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<HighlightRequest>();
                if (body == null)
                    return Results.BadRequest(new { error = "Request body is required" });

                var tcs = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                Dispatcher.UIThread.Post(() =>
                {
                    _ = HighlightOnUiThread(body, annotationService, tcs);
                });

                var result = await tcs.Task;
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("AddHighlight")
        .WithSummary("Add highlight annotation")
        .WithDescription(
            "Creates a highlight annotation on the specified page at the given bounds.");
    }

    private static async Task HighlightOnUiThread(
        HighlightRequest body,
        IAnnotationService annotationService,
        TaskCompletionSource<object> tcs)
    {
        try
        {
            var viewer = GetActiveViewer();
            if (viewer == null)
            {
                tcs.TrySetResult(new { success = false, error = "No active document" });
                return;
            }

            var document = viewer.CurrentDocument;
            if (document == null)
            {
                tcs.TrySetResult(new { success = false, error = "No document loaded" });
                return;
            }

            var pageNumber = (body.PageNumber ?? viewer.CurrentPageNumber) - 1; // to 0-based

            var fillColor = System.Drawing.Color.FromArgb(80, 255, 255, 0); // default yellow
            if (!string.IsNullOrEmpty(body.Color))
            {
                try
                {
                    var parsed = System.Drawing.ColorTranslator.FromHtml(body.Color);
                    fillColor = System.Drawing.Color.FromArgb(80, parsed.R, parsed.G, parsed.B);
                }
                catch { /* keep default */ }
            }

            float left = body.Bounds?.Left ?? 50;
            float bottom = body.Bounds?.Bottom ?? 700;
            float right = body.Bounds?.Right ?? 250;
            float top = body.Bounds?.Top ?? 730;

            var annotation = new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = pageNumber,
                Bounds = new PdfRectangle(left, bottom, right, top),
                FillColor = fillColor,
                StrokeColor = System.Drawing.Color.FromArgb(
                    255, fillColor.R, fillColor.G, fillColor.B),
                Contents = body.Contents ?? "",
                Opacity = body.Opacity ?? 0.5f
            };

            var result = await annotationService.CreateAnnotationAsync(document, annotation);

            if (!result.IsSuccess)
            {
                tcs.TrySetResult(new
                {
                    success = false,
                    error = result.Errors.FirstOrDefault()?.Message ?? "Failed"
                });
                return;
            }

            // Re-render to show the highlight
            await viewer.GoToPageCommand.ExecuteAsync(viewer.CurrentPageNumber);
            await Task.Delay(500);

            tcs.TrySetResult(new
            {
                success = true,
                annotationId = result.Value.Id,
                pageNumber = pageNumber + 1,
                type = "Highlight"
            });
        }
        catch (Exception ex)
        {
            tcs.TrySetResult(new { success = false, error = ex.ToString() });
        }
    }

    private record BoundsDto(float Left, float Bottom, float Right, float Top);

    private record HighlightRequest(
        int? PageNumber,
        BoundsDto? Bounds,
        string? Color,
        string? Contents,
        float? Opacity);
}
