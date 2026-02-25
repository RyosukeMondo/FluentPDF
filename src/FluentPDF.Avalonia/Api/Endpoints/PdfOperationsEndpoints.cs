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
/// Covers page operations, annotations, text extraction/replacement, export, and document editing.
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
        MapDocumentEditingEndpoints(app);
        MapFormFieldEndpoints(app);
        MapWatermarkEndpoints(app);
        MapStampEndpoints(app);
        MapFdfEndpoints(app);
        MapImageInsertionEndpoints(app);
        MapSecurityEndpoints(app);
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

    private static void MapDocumentEditingEndpoints(WebApplication app)
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

    private static void MapFormFieldEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/forms")
            .WithTags("Form Fields");

        group.MapGet("/{documentId}/{pageNumber:int}", async (
            string documentId,
            int pageNumber,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            var document = sessions.GetDocument(documentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {documentId}"));

            var result = await formService.GetFormFieldsAsync(document, pageNumber);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var fields = result.Value.Select(f => new
            {
                name = f.Name,
                fieldType = f.Type.ToString(),
                value = f.Value,
                isReadOnly = f.IsReadOnly,
                isRequired = f.IsRequired,
                pageNumber = f.PageNumber,
                bounds = new { x = f.Bounds.Left, y = f.Bounds.Bottom, width = f.Bounds.Width, height = f.Bounds.Height },
                options = f.Options
            }).ToList();

            return Results.Json(new { success = true, pageNumber, count = fields.Count, fields });
        })
        .WithName("ListFormFields")
        .WithSummary("List form fields on a page");

        group.MapPost("/set-value", async (
            SetFormFieldValueRequest request,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.FieldName))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "fieldName is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var fieldsResult = await formService.GetFormFieldsAsync(document, request.PageNumber);
            if (!fieldsResult.IsSuccess)
                return Results.Json(new { success = false, error = fieldsResult.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var field = fieldsResult.Value.FirstOrDefault(f => f.Name == request.FieldName);
            if (field is null)
                return Results.NotFound(new ErrorResponse("FIELD_NOT_FOUND", $"No form field named '{request.FieldName}' on page {request.PageNumber}"));

            var result = await formService.SetFieldValueAsync(field, request.Value ?? string.Empty);

            return result.IsSuccess
                ? Results.Json(new { success = true, fieldName = request.FieldName, value = request.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Set value failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("SetFormFieldValue")
        .WithSummary("Set the value of a form field");

        group.MapPost("/set-checkbox", async (
            SetCheckboxStateRequest request,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.FieldName))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "fieldName is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var fieldsResult = await formService.GetFormFieldsAsync(document, request.PageNumber);
            if (!fieldsResult.IsSuccess)
                return Results.Json(new { success = false, error = fieldsResult.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var field = fieldsResult.Value.FirstOrDefault(f => f.Name == request.FieldName);
            if (field is null)
                return Results.NotFound(new ErrorResponse("FIELD_NOT_FOUND", $"No form field named '{request.FieldName}' on page {request.PageNumber}"));

            var result = await formService.SetCheckboxStateAsync(field, request.IsChecked);

            return result.IsSuccess
                ? Results.Json(new { success = true, fieldName = request.FieldName, isChecked = request.IsChecked })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Set checkbox failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("SetCheckboxState")
        .WithSummary("Set the checked state of a checkbox form field");
    }

    private static void MapWatermarkEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/watermark")
            .WithTags("Watermark");

        group.MapPost("/apply-text", async (
            ApplyTextWatermarkRequest request,
            IWatermarkService watermarkService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "text is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var position = Enum.TryParse<WatermarkPosition>(request.Position, true, out var pos) ? pos : WatermarkPosition.Center;
            var config = new TextWatermarkConfig
            {
                Text = request.Text,
                FontSize = (float)(request.FontSize ?? 48),
                Opacity = (float)(request.Opacity ?? 0.3),
                RotationDegrees = (float)(request.RotationDegrees ?? -45),
                Position = position
            };

            var pageRange = string.IsNullOrWhiteSpace(request.PageRange)
                ? WatermarkPageRange.All
                : WatermarkPageRange.Parse(request.PageRange);

            var result = await watermarkService.ApplyTextWatermarkAsync(document, config, pageRange);

            return result.IsSuccess
                ? Results.Json(new { success = true, text = request.Text, position = position.ToString() })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Watermark failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("ApplyTextWatermark")
        .WithSummary("Apply a text watermark to document pages");

        group.MapPost("/preview", async (
            WatermarkPreviewRequest request,
            IWatermarkService watermarkService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var textConfig = string.IsNullOrWhiteSpace(request.Text) ? null : new TextWatermarkConfig
            {
                Text = request.Text,
                FontSize = (float)(request.FontSize ?? 48),
                Opacity = (float)(request.Opacity ?? 0.3)
            };

            var result = await watermarkService.GeneratePreviewAsync(document, request.PageIndex, textConfig, null);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Preview failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            return Results.File(result.Value, "image/png", "watermark-preview.png");
        })
        .WithName("WatermarkPreview")
        .WithSummary("Generate a watermark preview as PNG");
    }

    private static void MapStampEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/stamps")
            .WithTags("Stamps");

        group.MapPost("/apply", async (
            ApplyStampRequest request,
            IStampService stampService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            if (!Enum.TryParse<StampType>(request.Type, true, out var stampType))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "Invalid stamp type"));

            var stamp = stampService.CreateStamp(stampType);
            var position = new PointF(request.X ?? 100f, request.Y ?? 100f);
            var result = await stampService.ApplyStampAsync(document, stamp, request.PageNumber, position);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Stamp failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var annotation = result.Value;
            return Results.Json(new { success = true, annotationId = annotation.Id, type = request.Type, pageNumber = request.PageNumber });
        })
        .WithName("ApplyStamp")
        .WithSummary("Apply a stamp annotation to a page");
    }

    private static void MapFdfEndpoints(WebApplication app)
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

    private static void MapImageInsertionEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/images")
            .WithTags("Image Insertion");

        group.MapPost("/insert", async (
            InsertImageRequest request,
            IImageInsertionService imageService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.ImagePath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "imagePath is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            if (!File.Exists(request.ImagePath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"Image file not found: {request.ImagePath}"));

            var position = new PointF(request.X ?? 0f, request.Y ?? 0f);
            var result = await imageService.InsertImageAsync(document, request.PageIndex, request.ImagePath, position);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Insert failed" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var img = result.Value;
            return Results.Json(new { success = true, imageId = img.Id, position = new { x = img.Position.X, y = img.Position.Y }, size = new { width = img.Size.Width, height = img.Size.Height } });
        })
        .WithName("InsertImage")
        .WithSummary("Insert an image into a page");
    }

    private static void MapSecurityEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/security")
            .WithTags("Security");

        group.MapPost("/check-encrypted", async (
            CheckEncryptedRequest request,
            ISecurityService securityService) =>
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "filePath is required"));

            if (!File.Exists(request.FilePath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"File not found: {request.FilePath}"));

            var result = await securityService.IsEncryptedAsync(request.FilePath);

            return result.IsSuccess
                ? Results.Json(new { success = true, filePath = request.FilePath, isEncrypted = result.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Check failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("CheckEncrypted")
        .WithSummary("Check if a PDF file is encrypted");

        group.MapPost("/encrypt", async (
            EncryptDocumentRequest request,
            ISecurityService securityService) =>
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "inputPath is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            if (string.IsNullOrWhiteSpace(request.OwnerPassword))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "ownerPassword is required"));

            if (!File.Exists(request.InputPath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"File not found: {request.InputPath}"));

            var permissions = Enum.TryParse<PdfPermissions>(request.Permissions, true, out var perms)
                ? perms : PdfPermissions.None;
            var strength = Enum.TryParse<EncryptionStrength>(request.Strength, true, out var str)
                ? str : EncryptionStrength.Aes256;

            var settings = new EncryptionSettings
            {
                UserPassword = request.UserPassword,
                OwnerPassword = request.OwnerPassword,
                Permissions = permissions,
                Strength = strength
            };

            var result = await securityService.EncryptDocumentAsync(request.InputPath, request.OutputPath, settings);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = request.OutputPath, strength = strength.ToString() })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Encryption failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("EncryptDocument")
        .WithSummary("Encrypt a PDF document with password protection");
    }

    // --- Helper methods ---

    private static PageSize? ParsePageSize(string? size) => size?.ToLowerInvariant() switch
    {
        "letter" => Core.Services.PageSize.Letter,
        "a4" => Core.Services.PageSize.A4,
        "legal" => Core.Services.PageSize.Legal,
        "same" => Core.Services.PageSize.SameAsCurrent,
        _ => null
    };

    private static AnnotationType? ParseAnnotationType(string? type) => type?.ToLowerInvariant() switch
    {
        "highlight" => AnnotationType.Highlight,
        "underline" => AnnotationType.Underline,
        "strikeout" or "strikethrough" => AnnotationType.StrikeOut,
        "text" => AnnotationType.Text,
        "ink" => AnnotationType.Ink,
        _ => null
    };

    private static Color ParseColor(string? hex)
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

    private static string ColorToHex(Color color) =>
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

    private record MergeDocumentsRequest(string[] SourcePaths, string OutputPath);
    private record SplitDocumentRequest(string SourcePath, string PageRanges, string OutputPath);
    private record OptimizeDocumentRequest(string SourcePath, string OutputPath, bool? Linearize);

    private record SetFormFieldValueRequest(string DocumentId, int PageNumber, string FieldName, string? Value);
    private record SetCheckboxStateRequest(string DocumentId, int PageNumber, string FieldName, bool IsChecked);

    private record ApplyTextWatermarkRequest(string DocumentId, string Text, double? FontSize, double? Opacity, double? RotationDegrees, string? Position, string? PageRange);
    private record WatermarkPreviewRequest(string DocumentId, int PageIndex, string? Text, double? FontSize, double? Opacity);

    private record ApplyStampRequest(string DocumentId, int PageNumber, string Type, float? X, float? Y);

    private record ExportAnnotationsRequest(string DocumentId, string OutputPath, int[]? PageFilter);
    private record ImportAnnotationsRequest(string DocumentId, string XfdfPath, string? MergeBehavior);
    private record ExportFormDataRequest(string DocumentId, string OutputPath);
    private record ImportFormDataRequest(string DocumentId, string XfdfPath);

    private record InsertImageRequest(string DocumentId, int PageIndex, string ImagePath, float? X, float? Y);

    private record CheckEncryptedRequest(string FilePath);
    private record EncryptDocumentRequest(string InputPath, string OutputPath, string? UserPassword, string OwnerPassword, string? Permissions, string? Strength);
}
