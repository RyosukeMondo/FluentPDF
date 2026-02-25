using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Diagnostic REST API endpoints for testing PDF operations directly via PDFium.
/// These bypass the QPDF-based services and call PDFium native API directly.
/// </summary>
public static class DiagnosticEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/diag")
            .WithTags("Diagnostics");

        group.MapGet("/info/{documentId}", (
            string documentId,
            IDocumentSessionManager sessions) =>
        {
            var doc = sessions.GetDocument(documentId);
            if (doc == null)
                return Results.NotFound(new ErrorResponse("NOT_FOUND", "Document not found"));

            var docHandle = (SafePdfDocumentHandle)doc.Handle;
            var pageCount = PdfiumInterop.GetPageCount(docHandle);

            var pages = new List<object>();
            for (int i = 0; i < pageCount; i++)
            {
                using var page = PdfiumInterop.LoadPage(docHandle, i);
                if (page.IsInvalid) continue;
                pages.Add(new
                {
                    index = i,
                    width = PdfiumInterop.GetPageWidth(page),
                    height = PdfiumInterop.GetPageHeight(page),
                    rotation = PdfiumInterop.GetPageRotation(page),
                    objectCount = PdfiumInterop.GetPageObjectCount(page)
                });
            }

            return Results.Json(new
            {
                documentId,
                pageCount,
                pages
            });
        })
        .WithName("DiagDocInfo")
        .WithSummary("Get detailed document and page info via PDFium");

        group.MapPost("/rotate/{documentId}/{pageIndex}", (
            string documentId,
            int pageIndex,
            string? direction,
            IDocumentSessionManager sessions) =>
        {
            var doc = sessions.GetDocument(documentId);
            if (doc == null)
                return Results.NotFound(new ErrorResponse("NOT_FOUND", "Document not found"));

            var docHandle = (SafePdfDocumentHandle)doc.Handle;
            using var page = PdfiumInterop.LoadPage(docHandle, pageIndex);
            if (page.IsInvalid)
                return Results.BadRequest(new ErrorResponse("INVALID_PAGE", $"Cannot load page {pageIndex}"));

            var dir = direction ?? "cw";
            var oldRotation = PdfiumInterop.GetPageRotation(page);
            var newRotation = dir == "cw"
                ? (oldRotation + 1) % 4
                : (oldRotation + 3) % 4;
            PdfiumInterop.SetPageRotation(page, newRotation);

            return Results.Json(new
            {
                success = true,
                pageIndex,
                oldRotation = oldRotation * 90,
                newRotation = newRotation * 90
            });
        })
        .WithName("DiagRotatePage")
        .WithSummary("Rotate a page via PDFium (?direction=cw|ccw)");

        group.MapPost("/delete/{documentId}/{pageIndex}", (
            string documentId,
            int pageIndex,
            IDocumentSessionManager sessions) =>
        {
            var doc = sessions.GetDocument(documentId);
            if (doc == null)
                return Results.NotFound(new ErrorResponse("NOT_FOUND", "Document not found"));

            var docHandle = (SafePdfDocumentHandle)doc.Handle;
            var pageCount = PdfiumInterop.GetPageCount(docHandle);

            if (pageIndex < 0 || pageIndex >= pageCount)
                return Results.BadRequest(new ErrorResponse("INVALID_PAGE", $"Page {pageIndex} out of range (0-{pageCount - 1})"));
            if (pageCount <= 1)
                return Results.BadRequest(new ErrorResponse("LAST_PAGE", "Cannot delete the only page"));

            PdfiumInterop.DeletePage(docHandle, pageIndex);
            var newCount = PdfiumInterop.GetPageCount(docHandle);

            return Results.Json(new { success = true, deletedPage = pageIndex, newPageCount = newCount });
        })
        .WithName("DiagDeletePage")
        .WithSummary("Delete a page via PDFium");

        group.MapPost("/insert/{documentId}/{atIndex}", (
            string documentId,
            int atIndex,
            double? width,
            double? height,
            IDocumentSessionManager sessions) =>
        {
            var doc = sessions.GetDocument(documentId);
            if (doc == null)
                return Results.NotFound(new ErrorResponse("NOT_FOUND", "Document not found"));

            var docHandle = (SafePdfDocumentHandle)doc.Handle;
            var w = width ?? 612;
            var h = height ?? 792;

            using var newPage = PdfiumInterop.CreateNewPage(docHandle, atIndex, w, h);
            if (newPage.IsInvalid)
                return Results.Json(new { success = false, error = "Failed to create page" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var newCount = PdfiumInterop.GetPageCount(docHandle);
            return Results.Json(new { success = true, insertedAt = atIndex, newPageCount = newCount });
        })
        .WithName("DiagInsertPage")
        .WithSummary("Insert a blank page via PDFium");

        group.MapGet("/features", () =>
        {
            return Results.Json(new
            {
                pdfiumLoaded = PdfiumInterop.IsInitialized,
                endpoints = new[]
                {
                    "GET  /api/diag/doc/{id}/info - Document and page details",
                    "POST /api/diag/doc/{id}/page/{idx}/rotate - Rotate page (body: {direction:'cw'|'ccw'})",
                    "POST /api/diag/doc/{id}/page/{idx}/delete - Delete page",
                    "POST /api/diag/doc/{id}/page/insert - Insert blank page (body: {atIndex, width?, height?})",
                    "POST /api/shapes/rectangle - Draw rectangle",
                    "POST /api/shapes/circle - Draw circle",
                    "POST /api/shapes/line - Draw line",
                    "POST /api/shapes/freehand - Draw freehand path",
                    "POST /api/shapes/text - Add text",
                    "GET  /api/render/{id}/{page} - Render page to PNG"
                }
            });
        })
        .WithName("DiagFeatures")
        .WithSummary("List available diagnostic endpoints");
    }

}
