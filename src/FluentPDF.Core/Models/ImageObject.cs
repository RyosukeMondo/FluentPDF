using System.Drawing;

namespace FluentPDF.Core.Models;

/// <summary>
/// Represents an image object inserted into a PDF page.
/// Images can be positioned, scaled, and rotated on the page.
/// </summary>
public class ImageObject
{
    /// <summary>
    /// Gets or sets the unique identifier for this image object.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the page index (zero-based) where this image is located.
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the position of the image on the page (in PDF points).
    /// This represents the bottom-left corner of the image in PDF coordinate space.
    /// </summary>
    public PointF Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the image (in PDF points).
    /// Minimum size is 10x10 points.
    /// </summary>
    public SizeF Size { get; set; }

    /// <summary>
    /// Gets or sets the rotation angle of the image in degrees.
    /// Positive values rotate clockwise.
    /// </summary>
    public float RotationDegrees { get; set; }

    /// <summary>
    /// Gets or sets the source file path from which the image was loaded.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the native PDFium handle for this image object.
    /// This handle is used for low-level PDFium operations.
    /// </summary>
    public IntPtr PdfiumHandle { get; set; }

    /// <summary>
    /// Gets or sets whether the image is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the image object.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification date of the image object.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
