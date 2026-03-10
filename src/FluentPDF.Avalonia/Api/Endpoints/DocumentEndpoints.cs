// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Document management endpoints.
/// </summary>
public static class DocumentEndpoints
{
    /// <summary>
    /// Maps document endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        // Load document
        app.MapPost("/api/document/load", async (
            LoadDocumentRequest request,
            IPdfDocumentService documentService,
            IDocumentSessionManager sessionManager,
            ILogger<IDocumentSessionManager> logger) =>
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "Path is required"));
            }

            if (!File.Exists(request.Path))
            {
                return Results.NotFound(new ErrorResponse(
                    "PDF_FILE_NOT_FOUND",
                    $"File not found: {request.Path}"));
            }

            var result = await documentService.LoadDocumentAsync(request.Path, request.Password);

            if (!result.IsSuccess)
            {
                var error = result.Errors.FirstOrDefault();
                var errorCode = error?.Message ?? "LOAD_FAILED";

                // Map error codes to HTTP status
                return errorCode switch
                {
                    "PDF_REQUIRES_PASSWORD" => Results.Json(
                        new ErrorResponse("PDF_REQUIRES_PASSWORD", "Document requires a password"),
                        statusCode: StatusCodes.Status401Unauthorized),
                    "PDF_CORRUPTED" => Results.Json(
                        new ErrorResponse("PDF_CORRUPTED", "Document is corrupted"),
                        statusCode: StatusCodes.Status422UnprocessableEntity),
                    _ => Results.Json(
                        new ErrorResponse(errorCode, error?.Message ?? "Failed to load document"),
                        statusCode: StatusCodes.Status500InternalServerError)
                };
            }

            var document = result.Value;
            var sessionId = sessionManager.CreateSession(document);

            // Get first page dimensions for metadata
            var pageResult = await documentService.GetPageInfoAsync(document, 1); // 1-based page number
            double width = 0, height = 0;
            if (pageResult.IsSuccess)
            {
                width = pageResult.Value.Width;
                height = pageResult.Value.Height;
            }

            return Results.Ok(new LoadDocumentResponse(
                sessionId,
                document.PageCount,
                new DocumentMetadataDto(
                    null, // Title not stored in PdfDocument
                    null, // Author not stored in PdfDocument
                    width,
                    height)));
        })
        .WithName("LoadDocument")
        .WithTags("Document")
        .WithSummary("Load a PDF document")
        .WithDescription("Loads a PDF document from the file system and returns a session ID for subsequent operations.")
        .Produces<LoadDocumentResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity, "application/json");

        // Get document info
        app.MapGet("/api/document/{documentId}", (
            string documentId,
            IDocumentSessionManager sessionManager,
            IPdfDocumentService documentService) =>
        {
            var document = sessionManager.GetDocument(documentId);
            if (document is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {documentId}"));
            }

            return Results.Ok(new LoadDocumentResponse(
                documentId,
                document.PageCount,
                new DocumentMetadataDto(
                    null,
                    null,
                    0, 0))); // Simplified - would need to get page dimensions
        })
        .WithName("GetDocument")
        .WithTags("Document")
        .WithSummary("Get document information")
        .WithDescription("Retrieves metadata for a loaded document by session ID.")
        .Produces<LoadDocumentResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json");

        // Close document
        app.MapDelete("/api/document/{documentId}", (
            string documentId,
            IDocumentSessionManager sessionManager) =>
        {
            var closed = sessionManager.CloseSession(documentId);
            if (!closed)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {documentId}"));
            }

            return Results.Ok(new CloseDocumentResponse(true));
        })
        .WithName("CloseDocument")
        .WithTags("Document")
        .WithSummary("Close a document session")
        .WithDescription("Closes a document session and releases associated resources.")
        .Produces<CloseDocumentResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json");

        // Get document metadata
        app.MapGet("/api/document/{documentId}/metadata", (
            string documentId,
            IDocumentSessionManager sessionManager) =>
        {
            var document = sessionManager.GetDocument(documentId);
            if (document is null)
            {
                return Results.NotFound(new ErrorResponse(
                    "DOCUMENT_NOT_FOUND",
                    $"No document with ID: {documentId}"));
            }

            var docHandle = (SafePdfDocumentHandle)document.Handle;

            var title = PdfiumInterop.GetMetaText(docHandle, "Title");
            var author = PdfiumInterop.GetMetaText(docHandle, "Author");
            var subject = PdfiumInterop.GetMetaText(docHandle, "Subject");
            var keywords = PdfiumInterop.GetMetaText(docHandle, "Keywords");
            var creator = PdfiumInterop.GetMetaText(docHandle, "Creator");
            var producer = PdfiumInterop.GetMetaText(docHandle, "Producer");
            var creationDate = PdfiumInterop.GetMetaText(docHandle, "CreationDate");
            var modDate = PdfiumInterop.GetMetaText(docHandle, "ModDate");

            long fileSizeBytes = 0;
            try
            {
                if (File.Exists(document.FilePath))
                    fileSizeBytes = new FileInfo(document.FilePath).Length;
            }
            catch { /* ignore */ }

            string pdfVersion = string.Empty;
            if (PdfiumInterop.GetFileVersion(docHandle, out int version))
            {
                pdfVersion = $"{version / 10}.{version % 10}";
            }

            uint permFlags = PdfiumInterop.GetDocPermissions(docHandle);
            bool isEncrypted = permFlags != 0 && permFlags != 0xFFFFFFFF;

            var permParts = new System.Collections.Generic.List<string>();
            if (!isEncrypted)
            {
                permParts.Add("All");
            }
            else
            {
                if ((permFlags & (1 << 2)) != 0) permParts.Add("Print");
                if ((permFlags & (1 << 4)) != 0) permParts.Add("Copy");
                if ((permFlags & (1 << 3)) != 0) permParts.Add("Modify");
                if ((permFlags & (1 << 5)) != 0) permParts.Add("Annotate");
            }

            string fileSize = FluentPDF.Core.ViewModels.MetadataViewModel.FormatFileSize(fileSizeBytes);

            return Results.Ok(new FullDocumentMetadataDto(
                title,
                author,
                subject,
                keywords,
                creator,
                producer,
                document.PageCount,
                fileSizeBytes,
                fileSize,
                creationDate,
                modDate,
                pdfVersion,
                isEncrypted,
                string.Join(", ", permParts)
            ));
        })
        .WithName("GetDocumentMetadata")
        .WithTags("Document")
        .WithSummary("Get document metadata")
        .WithDescription("Retrieves PDF metadata (Title, Author, Subject, Keywords, Creator, Producer, dates, version, security) via PDFium.")
        .Produces<FullDocumentMetadataDto>(StatusCodes.Status200OK, "application/json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/json");
    }
}
