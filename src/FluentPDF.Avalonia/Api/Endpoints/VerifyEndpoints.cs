// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Render verification endpoints.
/// </summary>
public static class VerifyEndpoints
{
    /// <summary>
    /// Maps verify endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        // Verify single page
        app.MapPost("/api/verify/render", async (
            VerifyRequest request,
            IDocumentSessionManager sessionManager,
            IPdfRenderingService renderingService,
            IHashingService hashingService) =>
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

            // Render to PNG stream (1-based page number)
            var renderResult = await renderingService.RenderPageAsync(
                document,
                request.PageIndex + 1,
                1.0,
                request.Dpi);

            if (!renderResult.IsSuccess)
            {
                return Results.Json(
                    new ErrorResponse("RENDERING_FAILED", renderResult.Errors.FirstOrDefault()?.Message ?? "Rendering failed"),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Compute hash
            var currentHash = hashingService.ComputeHash(renderResult.Value);

            // Compare with baseline if provided
            if (string.IsNullOrEmpty(request.BaselineHash))
            {
                // No baseline - just return current hash
                return Results.Ok(new VerifyResponse(
                    Match: true,
                    Hash: currentHash,
                    Ssim: null,
                    DiffUrl: null));
            }

            var match = string.Equals(currentHash, request.BaselineHash, StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new VerifyResponse(
                Match: match,
                Hash: currentHash,
                Ssim: match ? 1.0 : null, // Simple: 1.0 if match, null if not
                DiffUrl: null));
        })
        .WithName("VerifyRender")
        .WithTags("Verify")
        .WithSummary("Verify page rendering with hash comparison")
        .WithDescription("Renders a page and compares its SHA256 hash against a baseline for visual regression testing.")
        .Produces<VerifyResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/json");

        // Batch verify multiple pages
        app.MapPost("/api/verify/batch", async (
            BatchVerifyRequest request,
            IDocumentSessionManager sessionManager,
            IPdfRenderingService renderingService,
            IHashingService hashingService) =>
        {
            var document = sessionManager.GetDocument(request.DocumentId);
            if (document is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {request.DocumentId}"));
            }

            var results = new Dictionary<int, VerifyResponse>();
            var failures = new List<BatchVerifyFailure>();

            // Process pages in parallel
            var tasks = request.Baselines.Select(async kvp =>
            {
                var pageIndex = kvp.Key;
                var expectedHash = kvp.Value;

                if (pageIndex < 0 || pageIndex >= document.PageCount)
                {
                    return (PageIndex: pageIndex, Response: new VerifyResponse(false, "", null, null), Failed: true);
                }

                // Render (1-based page number)
                var renderResult = await renderingService.RenderPageAsync(
                    document,
                    pageIndex + 1,
                    1.0,
                    request.Dpi);

                if (!renderResult.IsSuccess)
                {
                    return (PageIndex: pageIndex, Response: new VerifyResponse(false, "", null, null), Failed: true);
                }

                // Compute hash
                var currentHash = hashingService.ComputeHash(renderResult.Value);
                var match = string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                return (PageIndex: pageIndex, Response: new VerifyResponse(match, currentHash, match ? 1.0 : null, null), Failed: false);
            }).ToList();

            var taskResults = await Task.WhenAll(tasks);

            foreach (var result in taskResults)
            {
                results[result.PageIndex] = result.Response;

                if (!result.Response.Match && !result.Failed)
                {
                    var expectedHash = request.Baselines.TryGetValue(result.PageIndex, out var h) ? h : "";
                    failures.Add(new BatchVerifyFailure(
                        result.PageIndex,
                        expectedHash,
                        result.Response.Hash,
                        result.Response.Ssim));
                }
            }

            var allMatch = failures.Count == 0 && results.Values.All(r => r.Match);

            return Results.Ok(new BatchVerifyResponse(allMatch, results, failures));
        })
        .WithName("BatchVerify")
        .WithTags("Verify")
        .WithSummary("Batch verify multiple pages in parallel")
        .WithDescription("Verifies multiple pages concurrently using hash comparison for efficient regression testing.")
        .Produces<BatchVerifyResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json");
    }
}
