using System.Drawing;

namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a PDF annotation.
/// Annotations include highlights, underlines, comments, shapes, and freehand drawings.
/// </summary>
public class Annotation
{
    /// <summary>
    /// Gets or sets the unique identifier for this annotation.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the type of annotation.
    /// </summary>
    public AnnotationType Type { get; set; }

    /// <summary>
    /// Gets or sets the page number (zero-based) where this annotation is located.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the bounding rectangle for the annotation in PDF coordinates.
    /// </summary>
    public PdfRectangle Bounds { get; set; }

    /// <summary>
    /// Gets or sets the fill color for the annotation (used for highlights, shapes).
    /// </summary>
    public Color FillColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Gets or sets the stroke (border) color for the annotation.
    /// </summary>
    public Color StrokeColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the text content of the annotation (used for comments and text annotations).
    /// </summary>
    public string Contents { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opacity of the annotation (0.0 to 1.0).
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the stroke width for drawing annotations.
    /// </summary>
    public double StrokeWidth { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the ink points for freehand drawing annotations.
    /// Each point is represented as (x, y) coordinates in PDF space.
    /// </summary>
    public List<PointF> InkPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the quad points for text markup annotations (highlight, underline, strikethrough).
    /// Each quad is represented by 8 values: x1,y1,x2,y2,x3,y3,x4,y4.
    /// </summary>
    public List<float> QuadPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date of the annotation.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification date of the annotation.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the annotation is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the author of the annotation.
    /// </summary>
    public string Author { get; set; } = string.Empty;
}

/// <summary>
/// Represents the type of PDF annotation.
/// </summary>
public enum AnnotationType
{
    /// <summary>
    /// Unknown or unsupported annotation type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Text annotation (sticky note).
    /// </summary>
    Text = 1,

    /// <summary>
    /// Link annotation.
    /// </summary>
    Link = 2,

    /// <summary>
    /// Free text annotation.
    /// </summary>
    FreeText = 3,

    /// <summary>
    /// Line annotation.
    /// </summary>
    Line = 4,

    /// <summary>
    /// Square (rectangle) annotation.
    /// </summary>
    Square = 5,

    /// <summary>
    /// Circle annotation.
    /// </summary>
    Circle = 6,

    /// <summary>
    /// Polygon annotation.
    /// </summary>
    Polygon = 7,

    /// <summary>
    /// Polyline annotation.
    /// </summary>
    PolyLine = 8,

    /// <summary>
    /// Highlight text markup annotation.
    /// </summary>
    Highlight = 9,

    /// <summary>
    /// Underline text markup annotation.
    /// </summary>
    Underline = 10,

    /// <summary>
    /// Squiggly underline text markup annotation.
    /// </summary>
    Squiggly = 11,

    /// <summary>
    /// Strikethrough text markup annotation.
    /// </summary>
    StrikeOut = 12,

    /// <summary>
    /// Rubber stamp annotation.
    /// </summary>
    Stamp = 13,

    /// <summary>
    /// Caret annotation.
    /// </summary>
    Caret = 14,

    /// <summary>
    /// Ink (freehand drawing) annotation.
    /// </summary>
    Ink = 15,

    /// <summary>
    /// Popup annotation.
    /// </summary>
    Popup = 16,

    /// <summary>
    /// File attachment annotation.
    /// </summary>
    FileAttachment = 17,

    /// <summary>
    /// Sound annotation.
    /// </summary>
    Sound = 18,

    /// <summary>
    /// Movie annotation.
    /// </summary>
    Movie = 19,

    /// <summary>
    /// Widget annotation (form field).
    /// </summary>
    Widget = 20,

    /// <summary>
    /// Screen annotation.
    /// </summary>
    Screen = 21,

    /// <summary>
    /// Printer's mark annotation.
    /// </summary>
    PrinterMark = 22,

    /// <summary>
    /// Trap network annotation.
    /// </summary>
    TrapNet = 23,

    /// <summary>
    /// Watermark annotation.
    /// </summary>
    Watermark = 24,

    /// <summary>
    /// 3D annotation.
    /// </summary>
    ThreeD = 25,

    /// <summary>
    /// Redaction annotation.
    /// </summary>
    Redact = 26
}
