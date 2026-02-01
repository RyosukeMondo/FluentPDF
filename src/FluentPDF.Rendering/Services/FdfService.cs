using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for exporting and importing PDF annotations using XFDF (XML Forms Data Format).
/// XFDF is the XML-based variant of FDF, providing a human-readable format for annotation exchange.
/// </summary>
public sealed class FdfService : IFdfService
{
    private readonly IAnnotationService _annotationService;
    private readonly ILogger<FdfService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FdfService"/> class.
    /// </summary>
    /// <param name="annotationService">Service for annotation operations.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public FdfService(
        IAnnotationService annotationService,
        ILogger<FdfService> logger)
    {
        _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> ExportAnnotationsToXfdfAsync(
        PdfDocument document,
        string outputPath,
        int[]? pageFilter = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Exporting annotations to XFDF. CorrelationId={CorrelationId}, FilePath={FilePath}, OutputPath={OutputPath}",
            correlationId, document.FilePath, outputPath);

        try
        {
            // Collect all annotations from all pages (or filtered pages)
            var allAnnotations = new List<Annotation>();
            var pagesToProcess = pageFilter ?? Enumerable.Range(0, document.PageCount).ToArray();

            foreach (var pageNumber in pagesToProcess)
            {
                if (pageNumber < 0 || pageNumber >= document.PageCount)
                {
                    _logger.LogWarning(
                        "Skipping invalid page number {PageNumber}. CorrelationId={CorrelationId}",
                        pageNumber, correlationId);
                    continue;
                }

                var result = await _annotationService.GetAnnotationsAsync(document, pageNumber);
                if (result.IsSuccess)
                {
                    allAnnotations.AddRange(result.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to get annotations from page {PageNumber}: {Errors}. CorrelationId={CorrelationId}",
                        pageNumber, string.Join(", ", result.Errors), correlationId);
                }
            }

            // Create XFDF XML document
            var xfdf = CreateXfdfDocument(document, allAnnotations);

            // Save to file
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = false
            };

            await using var writer = XmlWriter.Create(outputPath, settings);
            xfdf.WriteTo(writer);

            _logger.LogInformation(
                "Exported {Count} annotations to XFDF. CorrelationId={CorrelationId}",
                allAnnotations.Count, correlationId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "FDF_EXPORT_FAILED",
                $"Failed to export annotations to XFDF: {ex.Message}",
                ErrorCategory.Rendering,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("FilePath", document.FilePath)
                .WithContext("OutputPath", outputPath)
                .WithContext("Exception", ex.ToString());

            _logger.LogError(ex,
                "Failed to export annotations to XFDF. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> ImportAnnotationsFromXfdfAsync(
        PdfDocument document,
        string xfdfPath,
        AnnotationMergeBehavior mergeBehavior = AnnotationMergeBehavior.Merge)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(xfdfPath))
        {
            throw new ArgumentException("XFDF path cannot be null or empty.", nameof(xfdfPath));
        }

        if (!File.Exists(xfdfPath))
        {
            return Result.Fail<int>($"XFDF file not found: {xfdfPath}");
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Importing annotations from XFDF. CorrelationId={CorrelationId}, FilePath={FilePath}, XfdfPath={XfdfPath}",
            correlationId, document.FilePath, xfdfPath);

        try
        {
            // Parse XFDF file
            var xfdf = XDocument.Load(xfdfPath);
            var annotations = ParseXfdfAnnotations(xfdf);

            if (annotations.Count == 0)
            {
                _logger.LogWarning(
                    "No annotations found in XFDF file. CorrelationId={CorrelationId}",
                    correlationId);
                return Result.Ok(0);
            }

            // Handle merge behavior
            if (mergeBehavior == AnnotationMergeBehavior.Replace)
            {
                // Delete all existing annotations first
                for (int pageNum = 0; pageNum < document.PageCount; pageNum++)
                {
                    var existing = await _annotationService.GetAnnotationsAsync(document, pageNum);
                    if (existing.IsSuccess)
                    {
                        for (int i = existing.Value.Count - 1; i >= 0; i--)
                        {
                            await _annotationService.DeleteAnnotationAsync(document, pageNum, i);
                        }
                    }
                }
            }

            // Import annotations
            int importedCount = 0;
            foreach (var annotation in annotations)
            {
                // Check for duplicates if needed
                if (mergeBehavior == AnnotationMergeBehavior.SkipDuplicates)
                {
                    var existing = await _annotationService.GetAnnotationsAsync(document, annotation.PageNumber);
                    if (existing.IsSuccess && IsDuplicate(annotation, existing.Value))
                    {
                        _logger.LogDebug(
                            "Skipping duplicate annotation on page {PageNumber}. CorrelationId={CorrelationId}",
                            annotation.PageNumber, correlationId);
                        continue;
                    }
                }

                var result = await _annotationService.CreateAnnotationAsync(document, annotation);
                if (result.IsSuccess)
                {
                    importedCount++;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to import annotation: {Errors}. CorrelationId={CorrelationId}",
                        string.Join(", ", result.Errors), correlationId);
                }
            }

            _logger.LogInformation(
                "Imported {Count} annotations from XFDF. CorrelationId={CorrelationId}",
                importedCount, correlationId);

            return Result.Ok(importedCount);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "FDF_IMPORT_FAILED",
                $"Failed to import annotations from XFDF: {ex.Message}",
                ErrorCategory.Rendering,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("FilePath", document.FilePath)
                .WithContext("XfdfPath", xfdfPath)
                .WithContext("Exception", ex.ToString());

            _logger.LogError(ex,
                "Failed to import annotations from XFDF. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail<int>(error);
        }
    }

    /// <inheritdoc />
    public Task<Result> ExportFormDataToXfdfAsync(PdfDocument document, string outputPath)
    {
        // Form data export is not yet implemented
        // This would export form field values (not annotations)
        _logger.LogWarning("Form data export is not yet implemented");
        return Task.FromResult(Result.Fail("Form data export is not yet implemented"));
    }

    /// <inheritdoc />
    public Task<Result> ImportFormDataFromXfdfAsync(PdfDocument document, string xfdfPath)
    {
        // Form data import is not yet implemented
        // This would import form field values (not annotations)
        _logger.LogWarning("Form data import is not yet implemented");
        return Task.FromResult(Result.Fail("Form data import is not yet implemented"));
    }

    /// <summary>
    /// Creates an XFDF XML document from a list of annotations.
    /// </summary>
    private XDocument CreateXfdfDocument(PdfDocument document, List<Annotation> annotations)
    {
        var xfdf = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("xfdf",
                new XAttribute("xmlns", "http://ns.adobe.com/xfdf/"),
                new XElement("f", new XAttribute("href", Path.GetFileName(document.FilePath))),
                new XElement("annots",
                    annotations.Select(a => CreateAnnotationElement(a))
                )
            )
        );

        return xfdf;
    }

    /// <summary>
    /// Creates an XML element for a single annotation.
    /// </summary>
    private XElement CreateAnnotationElement(Annotation annotation)
    {
        var elementName = GetXfdfElementName(annotation.Type);

        var element = new XElement(elementName,
            new XAttribute("page", annotation.PageNumber),
            new XAttribute("rect", FormatRect(annotation.Bounds)),
            new XAttribute("color", FormatColor(annotation.FillColor)),
            new XAttribute("opacity", annotation.Opacity.ToString(CultureInfo.InvariantCulture))
        );

        // Add annotation-specific attributes
        if (annotation.Type == AnnotationType.Ink && annotation.InkPoints.Count > 0)
        {
            element.Add(new XAttribute("width", annotation.StrokeWidth));
            element.Add(new XElement("inklist",
                new XElement("gesture", FormatInkPoints(annotation.InkPoints))
            ));
        }
        else if (annotation.Type == AnnotationType.Text)
        {
            element.Add(new XAttribute("icon", "Comment"));
        }

        // Add contents if present
        if (!string.IsNullOrEmpty(annotation.Contents))
        {
            element.Add(new XElement("contents", annotation.Contents));
        }

        // Add author if present
        if (!string.IsNullOrEmpty(annotation.Author))
        {
            element.Add(new XAttribute("title", annotation.Author));
        }

        // Add dates
        element.Add(new XAttribute("date", annotation.CreatedDate.ToString("o")));

        return element;
    }

    /// <summary>
    /// Parses annotations from an XFDF document.
    /// </summary>
    private List<Annotation> ParseXfdfAnnotations(XDocument xfdf)
    {
        var annotations = new List<Annotation>();
        var annotsElement = xfdf.Root?.Element("annots");

        if (annotsElement == null)
        {
            return annotations;
        }

        foreach (var element in annotsElement.Elements())
        {
            try
            {
                var annotation = ParseAnnotationElement(element);
                if (annotation != null)
                {
                    annotations.Add(annotation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse annotation element: {ElementName}", element.Name);
            }
        }

        return annotations;
    }

    /// <summary>
    /// Parses a single annotation element from XFDF.
    /// </summary>
    private Annotation? ParseAnnotationElement(XElement element)
    {
        var type = GetAnnotationType(element.Name.LocalName);
        if (type == AnnotationType.Unknown)
        {
            return null;
        }

        var annotation = new Annotation
        {
            Type = type,
            PageNumber = int.Parse(element.Attribute("page")?.Value ?? "0"),
            Bounds = ParseRect(element.Attribute("rect")?.Value ?? "0,0,0,0"),
            FillColor = ParseColor(element.Attribute("color")?.Value ?? "#FFFF00"),
            Opacity = double.Parse(element.Attribute("opacity")?.Value ?? "1.0", CultureInfo.InvariantCulture),
            Contents = element.Element("contents")?.Value ?? string.Empty,
            Author = element.Attribute("title")?.Value ?? string.Empty
        };

        // Parse ink points if present
        if (type == AnnotationType.Ink)
        {
            var inkList = element.Element("inklist");
            if (inkList != null)
            {
                var gesture = inkList.Element("gesture")?.Value;
                if (!string.IsNullOrEmpty(gesture))
                {
                    annotation.InkPoints = ParseInkPoints(gesture);
                }
            }

            annotation.StrokeWidth = double.Parse(element.Attribute("width")?.Value ?? "2.0", CultureInfo.InvariantCulture);
        }

        return annotation;
    }

    /// <summary>
    /// Gets the XFDF element name for an annotation type.
    /// </summary>
    private string GetXfdfElementName(AnnotationType type)
    {
        return type switch
        {
            AnnotationType.Highlight => "highlight",
            AnnotationType.Underline => "underline",
            AnnotationType.StrikeOut => "strikeout",
            AnnotationType.Text => "text",
            AnnotationType.Square => "square",
            AnnotationType.Circle => "circle",
            AnnotationType.Ink => "ink",
            _ => "text"
        };
    }

    /// <summary>
    /// Gets the annotation type from an XFDF element name.
    /// </summary>
    private AnnotationType GetAnnotationType(string elementName)
    {
        return elementName.ToLowerInvariant() switch
        {
            "highlight" => AnnotationType.Highlight,
            "underline" => AnnotationType.Underline,
            "strikeout" => AnnotationType.StrikeOut,
            "text" => AnnotationType.Text,
            "square" => AnnotationType.Square,
            "circle" => AnnotationType.Circle,
            "ink" => AnnotationType.Ink,
            _ => AnnotationType.Unknown
        };
    }

    /// <summary>
    /// Formats a rectangle as a comma-separated string.
    /// </summary>
    private string FormatRect(PdfRectangle rect)
    {
        return $"{rect.Left:F2},{rect.Bottom:F2},{rect.Right:F2},{rect.Top:F2}";
    }

    /// <summary>
    /// Parses a rectangle from a comma-separated string.
    /// </summary>
    private PdfRectangle ParseRect(string rectString)
    {
        var parts = rectString.Split(',');
        if (parts.Length != 4)
        {
            return new PdfRectangle(0, 0, 0, 0);
        }

        return new PdfRectangle(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture),
            double.Parse(parts[2], CultureInfo.InvariantCulture),
            double.Parse(parts[3], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Formats a color as a hex string (#RRGGBB).
    /// </summary>
    private string FormatColor(System.Drawing.Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Parses a color from a hex string (#RRGGBB or #RRGGBBAA).
    /// </summary>
    private System.Drawing.Color ParseColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString) || !colorString.StartsWith("#"))
        {
            return System.Drawing.Color.Yellow;
        }

        var hex = colorString[1..];
        if (hex.Length == 6)
        {
            return System.Drawing.Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
        }
        else if (hex.Length == 8)
        {
            return System.Drawing.Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16),
                Convert.ToInt32(hex.Substring(6, 2), 16)
            );
        }

        return System.Drawing.Color.Yellow;
    }

    /// <summary>
    /// Formats ink points as a space-separated string of x,y pairs.
    /// </summary>
    private string FormatInkPoints(List<System.Drawing.PointF> points)
    {
        return string.Join(";", points.Select(p => $"{p.X:F2},{p.Y:F2}"));
    }

    /// <summary>
    /// Parses ink points from a semicolon-separated string of x,y pairs.
    /// </summary>
    private List<System.Drawing.PointF> ParseInkPoints(string pointsString)
    {
        var points = new List<System.Drawing.PointF>();
        var pairs = pointsString.Split(';');

        foreach (var pair in pairs)
        {
            var coords = pair.Split(',');
            if (coords.Length == 2 &&
                float.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                points.Add(new System.Drawing.PointF(x, y));
            }
        }

        return points;
    }

    /// <summary>
    /// Checks if an annotation is a duplicate of any in the existing list.
    /// </summary>
    private bool IsDuplicate(Annotation annotation, List<Annotation> existing)
    {
        return existing.Any(e =>
            e.Type == annotation.Type &&
            Math.Abs(e.Bounds.Left - annotation.Bounds.Left) < 1.0 &&
            Math.Abs(e.Bounds.Bottom - annotation.Bounds.Bottom) < 1.0 &&
            Math.Abs(e.Bounds.Right - annotation.Bounds.Right) < 1.0 &&
            Math.Abs(e.Bounds.Top - annotation.Bounds.Top) < 1.0);
    }
}
