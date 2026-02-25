// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for document editing operations (merge, split, optimize).
/// </summary>
public static class DocumentEditingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/editing")
            .WithTags("Document Editing");

        group.MapPost("/merge", async (
            MergeDocumentsRequest request,
            IDocumentEditingService editingService) =>
        {
            if (request.SourcePaths is null || request.SourcePaths.Length < 2)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "At least 2 sourcePaths are required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            foreach (var path in request.SourcePaths)
            {
                if (!File.Exists(path))
                    return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"Source file not found: {path}"));
            }

            var result = await editingService.MergeAsync(request.SourcePaths, request.OutputPath);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = result.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Merge failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("MergeDocuments")
        .WithSummary("Merge multiple PDF documents into one");

        group.MapPost("/split", async (
            SplitDocumentRequest request,
            IDocumentEditingService editingService) =>
        {
            if (string.IsNullOrWhiteSpace(request.SourcePath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "sourcePath is required"));

            if (string.IsNullOrWhiteSpace(request.PageRanges))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "pageRanges is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            if (!File.Exists(request.SourcePath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"Source file not found: {request.SourcePath}"));

            var result = await editingService.SplitAsync(request.SourcePath, request.PageRanges, request.OutputPath);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = result.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Split failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("SplitDocument")
        .WithSummary("Split a PDF by extracting page ranges");

        group.MapPost("/optimize", async (
            OptimizeDocumentRequest request,
            IDocumentEditingService editingService) =>
        {
            if (string.IsNullOrWhiteSpace(request.SourcePath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "sourcePath is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            if (!File.Exists(request.SourcePath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"Source file not found: {request.SourcePath}"));

            var options = new OptimizationOptions
            {
                Linearize = request.Linearize ?? false
            };

            var result = await editingService.OptimizeAsync(request.SourcePath, request.OutputPath, options);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Optimize failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var opt = result.Value;
            return Results.Json(new
            {
                success = true,
                outputPath = opt.OutputPath,
                originalSizeBytes = opt.OriginalSize,
                optimizedSizeBytes = opt.OptimizedSize,
                reductionPercent = Math.Round(opt.ReductionPercentage, 2),
                linearized = opt.WasLinearized,
                processingTimeMs = opt.ProcessingTime.TotalMilliseconds
            });
        })
        .WithName("OptimizeDocument")
        .WithSummary("Optimize a PDF for reduced file size");
    }

    internal record MergeDocumentsRequest(string[] SourcePaths, string OutputPath);
    internal record SplitDocumentRequest(string SourcePath, string PageRanges, string OutputPath);
    internal record OptimizeDocumentRequest(string SourcePath, string OutputPath, bool? Linearize);
}
