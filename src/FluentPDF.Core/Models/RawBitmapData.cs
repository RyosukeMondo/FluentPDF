namespace FluentPDF.Core.Models;

/// <summary>
/// Raw BGRA pixel data from PDFium rendering, suitable for direct blitting to platform bitmaps.
/// </summary>
/// <param name="Pixels">BGRA pixel data.</param>
/// <param name="Width">Bitmap width in pixels.</param>
/// <param name="Height">Bitmap height in pixels.</param>
/// <param name="Stride">Number of bytes per row.</param>
public record RawBitmapData(byte[] Pixels, int Width, int Height, int Stride);
