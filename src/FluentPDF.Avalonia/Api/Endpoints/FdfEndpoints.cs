// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for FDF/XFDF import and export operations.
/// </summary>
public static class FdfEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/fdf")
            .WithTags("FDF/XFDF");

        group.MapPost("/export-annotations", async (
            ExportAnnotationsRequest request,
            IFdfService fdfService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var result = await fdfService.ExportAnnotationsToXfdfAsync(document, request.OutputPath, request.PageFilter);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = request.OutputPath })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Export failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ExportAnnotationsToXfdf")
        .WithSummary("Export annotations to XFDF file");

        group.MapPost("/import-annotations", async (
            ImportAnnotationsRequest request,
            IFdfService fdfService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.XfdfPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "xfdfPath is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            if (!File.Exists(request.XfdfPath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"XFDF file not found: {request.XfdfPath}"));

            var mergeBehavior = Enum.TryParse<AnnotationMergeBehavior>(request.MergeBehavior, true, out var mb)
                ? mb : AnnotationMergeBehavior.Merge;

            var result = await fdfService.ImportAnnotationsFromXfdfAsync(document, request.XfdfPath, mergeBehavior);

            return result.IsSuccess
                ? Results.Json(new { success = true, importedCount = result.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Import failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ImportAnnotationsFromXfdf")
        .WithSummary("Import annotations from XFDF file");

        group.MapPost("/export-form-data", async (
            ExportFormDataRequest request,
            IFdfService fdfService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var result = await fdfService.ExportFormDataToXfdfAsync(document, request.OutputPath);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = request.OutputPath })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Export failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ExportFormDataToXfdf")
        .WithSummary("Export form data to XFDF file");

        group.MapPost("/import-form-data", async (
            ImportFormDataRequest request,
            IFdfService fdfService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.XfdfPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "xfdfPath is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            if (!File.Exists(request.XfdfPath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"XFDF file not found: {request.XfdfPath}"));

            var result = await fdfService.ImportFormDataFromXfdfAsync(document, request.XfdfPath);

            return result.IsSuccess
                ? Results.Json(new { success = true })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Import failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ImportFormDataFromXfdf")
        .WithSummary("Import form data from XFDF file");
    }

    internal record ExportAnnotationsRequest(string DocumentId, string OutputPath, int[]? PageFilter);
    internal record ImportAnnotationsRequest(string DocumentId, string XfdfPath, string? MergeBehavior);
    internal record ExportFormDataRequest(string DocumentId, string OutputPath);
    internal record ImportFormDataRequest(string DocumentId, string XfdfPath);
}
