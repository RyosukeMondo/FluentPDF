// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Drawing;
using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for image insertion operations.
/// </summary>
public static class ImageEndpoints
{
    public static void Map(WebApplication app)
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

    internal record InsertImageRequest(string DocumentId, int PageIndex, string ImagePath, float? X, float? Y);
}
