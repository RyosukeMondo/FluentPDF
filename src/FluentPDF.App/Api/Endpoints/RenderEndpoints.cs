// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Diagnostics;
using FluentPDF.App.Api.Models;
using FluentPDF.App.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// Page rendering endpoints.
/// </summary>
public static class RenderEndpoints
{
    /// <summary>
    /// Maps render endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        // Render page (POST)
        app.MapPost("/api/render", async (
            RenderRequest request,
            IDocumentSessionManager sessionManager,
            IPdfRenderingService renderingService,
            HttpContext context) =>
        {
            var document = sessionManager.GetDocument(request.DocumentId);
            if (document is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {request.DocumentId}"));
            }

            if (request.PageIndex < 0 || request.PageIndex >= document.PageCount)
            {
                return Results.BadRequest(new ErrorResponse(
                    "PAGE_OUT_OF_RANGE",
                    $"Page index {request.PageIndex} is out of range",
                    Details: new Dictionary<string, object> { ["maxPage"] = document.PageCount - 1 }));
            }

            var stopwatch = Stopwatch.StartNew();

            // Render to PNG stream (1-based page number)
            var renderResult = await renderingService.RenderPageAsync(
                document,
                request.PageIndex + 1,
                request.Zoom,
                request.Dpi);

            stopwatch.Stop();

            if (!renderResult.IsSuccess)
            {
                return Results.Json(
                    new ErrorResponse("RENDERING_FAILED", renderResult.Errors.FirstOrDefault()?.Message ?? "Rendering failed"),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Set render time header
            context.Response.Headers["X-Render-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();

            // Return PNG stream
            var stream = renderResult.Value;
            stream.Position = 0;
            return Results.Stream(stream, "image/png");
        })
        .WithName("RenderPage")
        .WithTags("Render")
        .WithSummary("Render a page to PNG")
        .WithDescription("Renders a specific page of a document to PNG format with configurable DPI and zoom level.")
        .Produces(StatusCodes.Status200OK, contentType: "image/png")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/json");

        // Render page (GET - convenience endpoint)
        app.MapGet("/api/render/{documentId}/{pageIndex}", async (
            string documentId,
            int pageIndex,
            int? dpi,
            double? zoom,
            IDocumentSessionManager sessionManager,
            IPdfRenderingService renderingService,
            HttpContext context) =>
        {
            var document = sessionManager.GetDocument(documentId);
            if (document is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {documentId}"));
            }

            if (pageIndex < 0 || pageIndex >= document.PageCount)
            {
                return Results.BadRequest(new ErrorResponse(
                    "PAGE_OUT_OF_RANGE",
                    $"Page index {pageIndex} is out of range",
                    Details: new Dictionary<string, object> { ["maxPage"] = document.PageCount - 1 }));
            }

            var stopwatch = Stopwatch.StartNew();

            // Render to PNG stream (1-based page number)
            var renderResult = await renderingService.RenderPageAsync(
                document,
                pageIndex + 1,
                zoom ?? 1.0,
                dpi ?? 96);

            stopwatch.Stop();

            if (!renderResult.IsSuccess)
            {
                return Results.Json(
                    new ErrorResponse("RENDERING_FAILED", renderResult.Errors.FirstOrDefault()?.Message ?? "Rendering failed"),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Set render time header
            context.Response.Headers["X-Render-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();

            // Return PNG stream
            var stream = renderResult.Value;
            stream.Position = 0;
            return Results.Stream(stream, "image/png");
        })
        .WithName("RenderPageGet")
        .WithTags("Render")
        .WithSummary("Render a page to PNG (convenience GET endpoint)")
        .WithDescription("Convenience GET endpoint for rendering a page. Accepts DPI and zoom as query parameters.")
        .Produces(StatusCodes.Status200OK, contentType: "image/png")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/json");
    }
}
