// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using FluentPDF.App.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Api.Endpoints;

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
        .Produces<LoadDocumentResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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
        .Produces<LoadDocumentResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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
        .Produces<CloseDocumentResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }
}
