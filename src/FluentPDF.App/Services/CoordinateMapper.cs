using System.Drawing;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Services;

/// <summary>
/// Service for converting between screen coordinates and PDF page coordinates.
/// Implements coordinate transformations with zoom level and page dimension support.
/// </summary>
public sealed class CoordinateMapper : ICoordinateMapper
{
    private readonly ILogger<CoordinateMapper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateMapper"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public CoordinateMapper(ILogger<CoordinateMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public PointF ScreenToPdf(
        Point screenPoint,
        int pageIndex,
        double zoomLevel,
        double pageWidth,
        double pageHeight)
    {
        if (zoomLevel <= 0)
        {
            throw new ArgumentException("Zoom level must be greater than zero.", nameof(zoomLevel));
        }

        if (pageWidth <= 0 || pageHeight <= 0)
        {
            throw new ArgumentException("Page dimensions must be greater than zero.");
        }

        // Convert screen coordinates to PDF coordinates
        // PDF coordinates: origin at bottom-left, Y increases upward
        // Screen coordinates: origin at top-left, Y increases downward

        // Apply inverse zoom to get PDF points
        var pdfX = (float)(screenPoint.X / zoomLevel);
        var pdfY = (float)(pageHeight - (screenPoint.Y / zoomLevel));

        _logger.LogTrace(
            "Screen to PDF conversion. PageIndex={PageIndex}, Screen=({ScreenX},{ScreenY}), " +
            "PDF=({PdfX},{PdfY}), Zoom={Zoom}, PageSize=({Width}x{Height})",
            pageIndex, screenPoint.X, screenPoint.Y, pdfX, pdfY, zoomLevel, pageWidth, pageHeight);

        return new PointF(pdfX, pdfY);
    }

    /// <inheritdoc />
    public Point PdfToScreen(
        PointF pdfPoint,
        int pageIndex,
        double zoomLevel,
        double pageWidth,
        double pageHeight)
    {
        if (zoomLevel <= 0)
        {
            throw new ArgumentException("Zoom level must be greater than zero.", nameof(zoomLevel));
        }

        if (pageWidth <= 0 || pageHeight <= 0)
        {
            throw new ArgumentException("Page dimensions must be greater than zero.");
        }

        // Convert PDF coordinates to screen coordinates
        // PDF coordinates: origin at bottom-left, Y increases upward
        // Screen coordinates: origin at top-left, Y increases downward

        // Apply zoom and flip Y axis
        var screenX = (int)(pdfPoint.X * zoomLevel);
        var screenY = (int)((pageHeight - pdfPoint.Y) * zoomLevel);

        _logger.LogTrace(
            "PDF to Screen conversion. PageIndex={PageIndex}, PDF=({PdfX},{PdfY}), " +
            "Screen=({ScreenX},{ScreenY}), Zoom={Zoom}, PageSize=({Width}x{Height})",
            pageIndex, pdfPoint.X, pdfPoint.Y, screenX, screenY, zoomLevel, pageWidth, pageHeight);

        return new Point(screenX, screenY);
    }
}
