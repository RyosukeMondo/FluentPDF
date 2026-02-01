using System.Drawing;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for converting between screen coordinates and PDF page coordinates.
/// Handles coordinate transformations accounting for zoom level, page dimensions, and rendering offset.
/// </summary>
public interface ICoordinateMapper
{
    /// <summary>
    /// Converts screen coordinates to PDF page coordinates.
    /// </summary>
    /// <param name="screenPoint">Point in screen/UI coordinates (pixels).</param>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="zoomLevel">Current zoom level (1.0 = 100%).</param>
    /// <param name="pageWidth">Width of the PDF page in points.</param>
    /// <param name="pageHeight">Height of the PDF page in points.</param>
    /// <returns>Point in PDF coordinate space (points).</returns>
    PointF ScreenToPdf(
        Point screenPoint,
        int pageIndex,
        double zoomLevel,
        double pageWidth,
        double pageHeight);

    /// <summary>
    /// Converts PDF coordinates to screen coordinates.
    /// </summary>
    /// <param name="pdfPoint">Point in PDF coordinate space (points).</param>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="zoomLevel">Current zoom level (1.0 = 100%).</param>
    /// <param name="pageWidth">Width of the PDF page in points.</param>
    /// <param name="pageHeight">Height of the PDF page in points.</param>
    /// <returns>Point in screen/UI coordinates (pixels).</returns>
    Point PdfToScreen(
        PointF pdfPoint,
        int pageIndex,
        double zoomLevel,
        double pageWidth,
        double pageHeight);
}
