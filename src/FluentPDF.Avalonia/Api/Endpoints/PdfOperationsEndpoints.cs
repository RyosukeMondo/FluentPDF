// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Drawing;
using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for PDF operations used in autonomous E2E testing.
/// Coordinates all endpoint groups and contains page, annotation, text, and export operations.
/// </summary>
public static class PdfOperationsEndpoints
{
    /// <summary>
    /// Maps all PDF operation endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        MapPageOperations(app);
        MapAnnotationEndpoints(app);
        MapTextEndpoints(app);
        MapExportEndpoints(app);
        DocumentEditingEndpoints.Map(app);
        FormFieldEndpoints.Map(app);
        WatermarkEndpoints.Map(app);
        StampEndpoints.Map(app);
        FdfEndpoints.Map(app);
        ImageEndpoints.Map(app);
        SecurityEndpoints.Map(app);
    }

    private static void MapPageOperations(WebApplication app)
    {
        var group = app.MapGroup("/api/pages")
            .WithTags("Page Operations");

        group.MapPost("/rotate", async (
            RotatePagesRequest request,
            IPageOperationsService pageOps,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (request.PageIndices is null || request.PageIndices.Length == 0)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "pageIndices is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            if (!Enum.IsDefined(typeof(RotationAngle), request.Angle))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "angle must be 90, 180, or 270"));

            var result = await pageOps.RotatePagesAsync(document, request.PageIndices, (RotationAngle)request.Angle);

            return result.IsSuccess
                ? Results.Json(new { success = true, pagesRotated = request.PageIndices.Length, angle = request.Angle })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Rotation failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("RotatePages")
        .WithSummary("Rotate pages by a given angle");

        group.MapPost("/delete", async (
            DeletePagesRequest request,
            IPageOperationsService pageOps,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (request.PageIndices is null || request.PageIndices.Length == 0)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "pageIndices is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var result = await pageOps.DeletePagesAsync(document, request.PageIndices);

            return result.IsSuccess
                ? Results.Json(new { success = true, pagesDeleted = request.PageIndices.Length, remainingPages = document.PageCount })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Delete failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("DeletePages")
        .WithSummary("Delete pages from the document");

        group.MapPost("/reorder", async (
            ReorderPagesRequest request,
            IPageOperationsService pageOps,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (request.PageIndices is null || request.PageIndices.Length == 0)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "pageIndices is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var result = await pageOps.ReorderPagesAsync(document, request.PageIndices, request.TargetIndex);

            return result.IsSuccess
                ? Results.Json(new { success = true, pagesMoved = request.PageIndices.Length, targetIndex = request.TargetIndex })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Reorder failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ReorderPages")
        .WithSummary("Move pages to a new position");

        group.MapPost("/insert-blank", async (
            InsertBlankPageRequest request,
            IPageOperationsService pageOps,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var pageSize = ParsePageSize(request.PageSize);
            if (pageSize is null)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "pageSize must be 'letter', 'a4', 'legal', or 'same'"));

            var result = await pageOps.InsertBlankPageAsync(document, request.InsertAt, pageSize.Value);

            return result.IsSuccess
                ? Results.Json(new { success = true, insertedAt = request.InsertAt, totalPages = document.PageCount })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Insert failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("InsertBlankPage")
        .WithSummary("Insert a blank page at the specified position");
    }

    private static void MapAnnotationEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/annotations")
            .WithTags("Annotations");

        group.MapGet("/{documentId}/{pageNumber:int}", async (
            string documentId,
            int pageNumber,
            IAnnotationService annotationService,
            IDocumentSessionManager sessions) =>
        {
            var document = sessions.GetDocument(documentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {documentId}"));

            var result = await annotationService.GetAnnotationsAsync(document, pageNumber);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Failed to get annotations" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var annotations = result.Value.Select(a => new
            {
                id = a.Id,
                type = a.Type.ToString(),
                pageNumber = a.PageNumber,
                bounds = new { x = a.Bounds.Left, y = a.Bounds.Bottom, width = a.Bounds.Width, height = a.Bounds.Height },
                fillColor = ColorToHex(a.FillColor),
                contents = a.Contents,
                opacity = a.Opacity,
                author = a.Author,
                createdDate = a.CreatedDate,
                modifiedDate = a.ModifiedDate
            }).ToList();

            return Results.Json(new { success = true, pageNumber, count = annotations.Count, annotations });
        })
        .WithName("ListAnnotations")
        .WithSummary("List annotations on a page");

        group.MapPost("/create", async (
            CreateAnnotationRequest request,
            IAnnotationService annotationService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var annotationType = ParseAnnotationType(request.Type);
            if (annotationType is null)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST",
                    "type must be 'highlight', 'underline', 'strikeout', 'text', or 'ink'"));

            var color = ParseColor(request.Color);
            var annotation = new Annotation
            {
                Type = annotationType.Value,
                PageNumber = request.PageNumber,
                Bounds = new PdfRectangle(
                    request.Bounds.X,
                    request.Bounds.Y,
                    request.Bounds.X + request.Bounds.Width,
                    request.Bounds.Y + request.Bounds.Height),
                FillColor = color,
                Contents = request.Contents ?? string.Empty,
                Opacity = request.Opacity ?? 1.0
            };

            var result = await annotationService.CreateAnnotationAsync(document, annotation);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Create failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var created = result.Value;
            return Results.Json(new
            {
                success = true,
                annotationId = created.Id,
                type = created.Type.ToString(),
                pageNumber = created.PageNumber
            });
        })
        .WithName("CreateAnnotation")
        .WithSummary("Create an annotation on a page");

        group.MapDelete("/{documentId}/{pageNumber:int}/{annotationIndex:int}", async (
            string documentId,
            int pageNumber,
            int annotationIndex,
            IAnnotationService annotationService,
            IDocumentSessionManager sessions) =>
        {
            var document = sessions.GetDocument(documentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {documentId}"));

            var result = await annotationService.DeleteAnnotationAsync(document, pageNumber, annotationIndex);

            return result.IsSuccess
                ? Results.Json(new { success = true, pageNumber, deletedIndex = annotationIndex })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Delete failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("DeleteAnnotation")
        .WithSummary("Delete an annotation by index");
    }

    private static void MapTextEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/text")
            .WithTags("Text Operations");

        group.MapPost("/extract", async (
            ExtractTextRequest request,
            ITextExtractionService textService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var result = await textService.ExtractTextAsync(document, request.PageNumber);

            return result.IsSuccess
                ? Results.Json(new { success = true, pageNumber = request.PageNumber, text = result.Value, length = result.Value.Length })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Extraction failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ExtractText")
        .WithSummary("Extract all text from a page");

        group.MapPost("/extract-bounds", async (
            ExtractTextBoundsRequest request,
            ITextExtractionService textService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var bounds = new RectangleF(
                (float)request.Bounds.X,
                (float)request.Bounds.Y,
                (float)request.Bounds.Width,
                (float)request.Bounds.Height);

            var result = await textService.ExtractTextInBoundsAsync(document, request.PageNumber, bounds);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Extraction failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var selection = result.Value;
            return Results.Json(new
            {
                success = true,
                pageNumber = request.PageNumber,
                text = selection.Text,
                length = selection.Text.Length
            });
        })
        .WithName("ExtractTextInBounds")
        .WithSummary("Extract text within a rectangular region");

        group.MapPost("/replace", async (
            TextReplaceRequest request,
            ITextReplacementService replaceService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrEmpty(request.FindText))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "findText is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var options = new SearchOptions
            {
                CaseSensitive = request.CaseSensitive
            };

            var result = await replaceService.ReplaceAllAsync(
                document, request.FindText, request.ReplaceText, options);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Replace failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var replacements = result.Value;
            return Results.Json(new
            {
                success = true,
                findText = request.FindText,
                replaceText = request.ReplaceText,
                replacementCount = replacements.Count
            });
        })
        .WithName("ReplaceText")
        .WithSummary("Find and replace all occurrences of text");
    }

    private static void MapExportEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/export")
            .WithTags("Export");

        group.MapPost("/images", async (
            ExportImagesRequest request,
            IImageExportService exportService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.OutputDir))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputDir is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var format = (request.Format?.ToLowerInvariant()) switch
            {
                "jpg" or "jpeg" => ImageFormat.Jpg,
                _ => ImageFormat.Png
            };

            var options = new ImageExportOptions
            {
                Format = format,
                Dpi = request.Dpi ?? 150,
                PageRange = request.PageRange ?? "all",
                OverwriteExisting = true
            };

            var result = await exportService.ExportAsync(document, request.OutputDir, options);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Export failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var exportResult = result.Value;
            return Results.Json(new
            {
                success = true,
                pageCount = exportResult.PageCount,
                exportedFiles = exportResult.ExportedFiles,
                totalSizeBytes = exportResult.TotalSize,
                processingTimeMs = exportResult.ProcessingTime.TotalMilliseconds
            });
        })
        .WithName("ExportImages")
        .WithSummary("Export pages as images");
    }

    // --- Helper methods ---

    internal static PageSize? ParsePageSize(string? size) => size?.ToLowerInvariant() switch
    {
        "letter" => Core.Services.PageSize.Letter,
        "a4" => Core.Services.PageSize.A4,
        "legal" => Core.Services.PageSize.Legal,
        "same" => Core.Services.PageSize.SameAsCurrent,
        _ => null
    };

    internal static AnnotationType? ParseAnnotationType(string? type) => type?.ToLowerInvariant() switch
    {
        "highlight" => AnnotationType.Highlight,
        "underline" => AnnotationType.Underline,
        "strikeout" or "strikethrough" => AnnotationType.StrikeOut,
        "text" => AnnotationType.Text,
        "ink" => AnnotationType.Ink,
        _ => null
    };

    internal static Color ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Color.Yellow;

        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return Color.Yellow;
        }
    }

    internal static string ColorToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    // --- Request DTOs ---

    private record RotatePagesRequest(string DocumentId, int[] PageIndices, int Angle);
    private record DeletePagesRequest(string DocumentId, int[] PageIndices);
    private record ReorderPagesRequest(string DocumentId, int[] PageIndices, int TargetIndex);
    private record InsertBlankPageRequest(string DocumentId, int InsertAt, string? PageSize);

    private record BoundsDto(double X, double Y, double Width, double Height);

    private record CreateAnnotationRequest(
        string DocumentId,
        string? Type,
        int PageNumber,
        BoundsDto Bounds,
        string? Color,
        string? Contents,
        double? Opacity);

    private record ExtractTextRequest(string DocumentId, int PageNumber);
    private record ExtractTextBoundsRequest(string DocumentId, int PageNumber, BoundsDto Bounds);
    private record TextReplaceRequest(string DocumentId, string FindText, string ReplaceText, bool CaseSensitive = false);

    private record ExportImagesRequest(string DocumentId, string OutputDir, string? Format, int? Dpi, string? PageRange);
}
