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
/// REST API endpoints for watermark operations.
/// </summary>
public static class WatermarkEndpoints
{
    public static void Map(WebApplication app)
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

    internal record ApplyTextWatermarkRequest(string DocumentId, string Text, double? FontSize, double? Opacity, double? RotationDegrees, string? Position, string? PageRange);
    internal record WatermarkPreviewRequest(string DocumentId, int PageIndex, string? Text, double? FontSize, double? Opacity);
}
