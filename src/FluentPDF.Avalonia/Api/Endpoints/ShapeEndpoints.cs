using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for drawing shapes on PDF pages.
/// </summary>
public static class ShapeEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/shapes")
            .WithTags("Shapes");

        group.MapPost("/rectangle", async (
            AddRectangleRequest request,
            IShapeService shapeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var result = await shapeService.AddRectangleAsync(
                request.DocumentId, request.PageNumber,
                request.X, request.Y, request.Width, request.Height,
                request.FillColor ?? "#FF0000",
                request.StrokeColor ?? "#000000",
                request.StrokeWidth ?? 1f,
                request.Opacity ?? 1f);

            return Results.Json(new { success = result });
        })
        .WithName("AddRectangle")
        .WithSummary("Add a rectangle to a PDF page");

        group.MapPost("/circle", async (
            AddCircleRequest request,
            IShapeService shapeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var result = await shapeService.AddCircleAsync(
                request.DocumentId, request.PageNumber,
                request.CenterX, request.CenterY, request.Radius,
                request.FillColor ?? "#0000FF",
                request.StrokeColor ?? "#000000",
                request.StrokeWidth ?? 1f,
                request.Opacity ?? 1f);

            return Results.Json(new { success = result });
        })
        .WithName("AddCircle")
        .WithSummary("Add a circle to a PDF page");

        group.MapPost("/line", async (
            AddLineRequest request,
            IShapeService shapeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            var result = await shapeService.AddLineAsync(
                request.DocumentId, request.PageNumber,
                request.X1, request.Y1, request.X2, request.Y2,
                request.StrokeColor ?? "#000000",
                request.StrokeWidth ?? 2f);

            return Results.Json(new { success = result });
        })
        .WithName("AddLine")
        .WithSummary("Add a line to a PDF page");

        group.MapPost("/freehand", async (
            AddFreehandRequest request,
            IShapeService shapeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (request.Points == null || request.Points.Length < 4)
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "points must contain at least 4 values (2 x,y pairs)"));

            var result = await shapeService.AddFreehandPathAsync(
                request.DocumentId, request.PageNumber,
                request.Points,
                request.StrokeColor ?? "#000000",
                request.StrokeWidth ?? 2f);

            return Results.Json(new { success = result });
        })
        .WithName("AddFreehandPath")
        .WithSummary("Add a freehand path to a PDF page");

        group.MapPost("/text", async (
            AddTextRequest request,
            IShapeService shapeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "text is required"));

            var result = await shapeService.AddTextAsync(
                request.DocumentId, request.PageNumber,
                request.X, request.Y, request.Text,
                request.FontSize ?? 12f,
                request.FontName ?? "Helvetica",
                request.Color ?? "#000000");

            return Results.Json(new { success = result });
        })
        .WithName("AddText")
        .WithSummary("Add text to a PDF page");
    }

    internal record AddRectangleRequest(
        string DocumentId, int PageNumber,
        double X, double Y, double Width, double Height,
        string? FillColor, string? StrokeColor, float? StrokeWidth, float? Opacity);

    internal record AddCircleRequest(
        string DocumentId, int PageNumber,
        double CenterX, double CenterY, double Radius,
        string? FillColor, string? StrokeColor, float? StrokeWidth, float? Opacity);

    internal record AddLineRequest(
        string DocumentId, int PageNumber,
        double X1, double Y1, double X2, double Y2,
        string? StrokeColor, float? StrokeWidth);

    internal record AddFreehandRequest(
        string DocumentId, int PageNumber,
        double[] Points,
        string? StrokeColor, float? StrokeWidth);

    internal record AddTextRequest(
        string DocumentId, int PageNumber,
        double X, double Y, string Text,
        float? FontSize, string? FontName, string? Color);
}
