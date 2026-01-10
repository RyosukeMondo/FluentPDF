using FluentPDF.Core.Models;
using Windows.Foundation;

namespace FluentPDF.App.Helpers;

/// <summary>
/// Provides coordinate transformation between PDF coordinate space and screen coordinate space.
/// PDF coordinates have origin at bottom-left, screen coordinates have origin at top-left.
/// </summary>
public static class CoordinateTransformHelper
{
    /// <summary>
    /// Transforms a PDF rectangle to screen coordinates.
    /// </summary>
    /// <param name="pdfRect">The rectangle in PDF coordinates (origin at bottom-left).</param>
    /// <param name="pageHeight">The height of the PDF page in PDF units.</param>
    /// <param name="zoomLevel">The current zoom level (1.0 = 100%, 2.0 = 200%).</param>
    /// <param name="dpi">The DPI used for rendering (default 96).</param>
    /// <returns>A rectangle in screen coordinates (origin at top-left).</returns>
    public static Rect TransformPdfToScreen(PdfRectangle pdfRect, double pageHeight, double zoomLevel, double dpi = 96.0)
    {
        // PDF coordinates: origin at bottom-left, Y increases upward
        // Screen coordinates: origin at top-left, Y increases downward

        // Convert PDF units (points, 72 DPI) to screen pixels
        double scale = (dpi / 72.0) * zoomLevel;

        // Transform X coordinate (same direction, just scale)
        double screenX = pdfRect.Left * scale;

        // Transform Y coordinate (flip vertical axis)
        // In PDF: bottom edge is at pdfRect.Bottom, top edge is at pdfRect.Top
        // In screen: top edge should be at (pageHeight - pdfRect.Top) * scale
        double screenY = (pageHeight - pdfRect.Top) * scale;

        // Calculate width and height
        double screenWidth = pdfRect.Width * scale;
        double screenHeight = pdfRect.Height * scale;

        return new Rect(screenX, screenY, screenWidth, screenHeight);
    }

    /// <summary>
    /// Transforms a screen point to PDF coordinates.
    /// </summary>
    /// <param name="screenPoint">The point in screen coordinates (origin at top-left).</param>
    /// <param name="pageHeight">The height of the PDF page in PDF units.</param>
    /// <param name="zoomLevel">The current zoom level (1.0 = 100%, 2.0 = 200%).</param>
    /// <param name="dpi">The DPI used for rendering (default 96).</param>
    /// <returns>A point in PDF coordinates (origin at bottom-left).</returns>
    public static Point TransformScreenToPdf(Point screenPoint, double pageHeight, double zoomLevel, double dpi = 96.0)
    {
        // Convert screen pixels to PDF units (points, 72 DPI)
        double scale = (dpi / 72.0) * zoomLevel;

        // Transform X coordinate (same direction, just unscale)
        double pdfX = screenPoint.X / scale;

        // Transform Y coordinate (flip vertical axis)
        // In screen: Y increases downward from top
        // In PDF: Y increases upward from bottom
        double pdfY = pageHeight - (screenPoint.Y / scale);

        return new Point(pdfX, pdfY);
    }

    /// <summary>
    /// Transforms a screen rectangle to PDF coordinates.
    /// </summary>
    /// <param name="screenRect">The rectangle in screen coordinates (origin at top-left).</param>
    /// <param name="pageHeight">The height of the PDF page in PDF units.</param>
    /// <param name="zoomLevel">The current zoom level (1.0 = 100%, 2.0 = 200%).</param>
    /// <param name="dpi">The DPI used for rendering (default 96).</param>
    /// <returns>A rectangle in PDF coordinates (origin at bottom-left).</returns>
    public static PdfRectangle TransformScreenToPdf(Rect screenRect, double pageHeight, double zoomLevel, double dpi = 96.0)
    {
        // Convert screen pixels to PDF units (points, 72 DPI)
        double scale = (dpi / 72.0) * zoomLevel;

        // Transform coordinates
        double pdfLeft = screenRect.X / scale;
        double pdfRight = (screenRect.X + screenRect.Width) / scale;

        // Flip Y-axis for screen to PDF transformation
        double pdfTop = pageHeight - (screenRect.Y / scale);
        double pdfBottom = pageHeight - ((screenRect.Y + screenRect.Height) / scale);

        return new PdfRectangle(pdfLeft, pdfBottom, pdfRight, pdfTop);
    }
}
