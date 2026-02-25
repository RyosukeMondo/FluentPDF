// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Drawing;
using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for stamp operations.
/// </summary>
public static class StampEndpoints
{
    public static void Map(WebApplication app)
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

    internal record ApplyStampRequest(string DocumentId, int PageNumber, string Type, float? X, float? Y);
}
