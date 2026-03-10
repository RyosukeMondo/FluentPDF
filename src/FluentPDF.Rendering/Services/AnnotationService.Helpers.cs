using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

public sealed partial class AnnotationService
{
    /// <inheritdoc />
    public async Task<Result> SaveAnnotationsAsync(PdfDocument document, string filePath, bool createBackup = true)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Saving annotations. CorrelationId={CorrelationId}, FilePath={FilePath}, CreateBackup={CreateBackup}",
            correlationId, filePath, createBackup);

        return await Task.Run(() =>
        {
            string? backupPath = null;

            try
            {
                // Check if file is read-only BEFORE attempting any PDFium operations
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.IsReadOnly)
                    {
                        return Result.Fail(CreateError(
                            "ANNOTATION_FILE_READONLY",
                            "Cannot save to read-only file. Please use Save As.",
                            filePath,
                            correlationId));
                    }
                }

                var documentHandle = (SafePdfDocumentHandle)document.Handle;

                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "ANNOTATION_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                // Create backup if requested and file exists
                if (createBackup && File.Exists(filePath))
                {
                    backupPath = filePath + ".bak";
                    File.Copy(filePath, backupPath, overwrite: true);
                    _logger.LogDebug(
                        "Backup created at {BackupPath}. CorrelationId={CorrelationId}",
                        backupPath, correlationId);
                }

                // Save the document with annotations
                var success = PdfiumInterop.SaveDocument(documentHandle, filePath, flags: 0);

                if (!success)
                {
                    // Restore backup if save failed
                    if (backupPath != null && File.Exists(backupPath))
                    {
                        File.Copy(backupPath, filePath, overwrite: true);
                        _logger.LogWarning(
                            "Save failed, backup restored. CorrelationId={CorrelationId}",
                            correlationId);
                    }

                    return Result.Fail(CreateError(
                        "ANNOTATION_SAVE_FAILED",
                        "Failed to save annotations to PDF.",
                        filePath,
                        correlationId));
                }

                _logger.LogInformation(
                    "Annotations saved successfully. CorrelationId={CorrelationId}",
                    correlationId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                // Restore backup on exception
                if (backupPath != null && File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, filePath, overwrite: true);
                        _logger.LogWarning(
                            "Save failed, backup restored. CorrelationId={CorrelationId}",
                            correlationId);
                    }
                    catch (Exception restoreEx)
                    {
                        _logger.LogError(restoreEx,
                            "Failed to restore backup. CorrelationId={CorrelationId}",
                            correlationId);
                    }
                }

                var error = CreateError(
                    "ANNOTATION_SAVE_FAILED",
                    $"Failed to save annotations: {ex.Message}",
                    filePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Failed to save annotations. CorrelationId={CorrelationId}",
                    correlationId);

                return Result.Fail(error);
            }
        });
    }

    /// <summary>
    /// Converts a PDFium annotation handle to a domain Annotation object.
    /// </summary>
    private Annotation ConvertToAnnotation(SafeAnnotationHandle annotHandle, int pageNumber, int index)
    {
        var type = PdfiumInterop.GetAnnotationSubtype(annotHandle);
        var annotation = new Annotation
        {
            Type = MapToDomainAnnotationType(type),
            PageNumber = pageNumber
        };

        // Get bounds
        if (PdfiumInterop.GetAnnotationRect(annotHandle, out var left, out var bottom, out var right, out var top))
        {
            annotation.Bounds = new PdfRectangle(left, bottom, right, top);
        }

        // Get fill color
        if (PdfiumInterop.GetAnnotationColor(annotHandle, PdfiumInterop.AnnotationColorType.Fill,
            out var fr, out var fg, out var fb, out var fa))
        {
            annotation.FillColor = Color.FromArgb((int)fa, (int)fr, (int)fg, (int)fb);
        }

        // Get stroke color
        if (PdfiumInterop.GetAnnotationColor(annotHandle, PdfiumInterop.AnnotationColorType.Stroke,
            out var sr, out var sg, out var sb, out var sa))
        {
            annotation.StrokeColor = Color.FromArgb((int)sa, (int)sr, (int)sg, (int)sb);
        }

        // Get contents
        annotation.Contents = PdfiumInterop.GetAnnotationContents(annotHandle);

        return annotation;
    }

    /// <summary>
    /// Applies annotation properties to a PDFium annotation handle.
    /// </summary>
    private void ApplyAnnotationProperties(SafeAnnotationHandle annotHandle, Annotation annotation)
    {
        // Set bounds
        PdfiumInterop.SetAnnotationRect(
            annotHandle,
            (float)annotation.Bounds.Left,
            (float)annotation.Bounds.Bottom,
            (float)annotation.Bounds.Right,
            (float)annotation.Bounds.Top);

        // Set fill color
        PdfiumInterop.SetAnnotationColor(
            annotHandle,
            PdfiumInterop.AnnotationColorType.Fill,
            annotation.FillColor.R,
            annotation.FillColor.G,
            annotation.FillColor.B,
            annotation.FillColor.A);

        // Set stroke color
        PdfiumInterop.SetAnnotationColor(
            annotHandle,
            PdfiumInterop.AnnotationColorType.Stroke,
            annotation.StrokeColor.R,
            annotation.StrokeColor.G,
            annotation.StrokeColor.B,
            annotation.StrokeColor.A);

        // Set contents
        if (!string.IsNullOrEmpty(annotation.Contents))
        {
            PdfiumInterop.SetAnnotationContents(annotHandle, annotation.Contents);
        }

        // Set quad points for text markup annotations
        if (annotation.QuadPoints.Count > 0 &&
            (annotation.Type == AnnotationType.Highlight ||
             annotation.Type == AnnotationType.Underline ||
             annotation.Type == AnnotationType.StrikeOut))
        {
            PdfiumInterop.SetAnnotationQuadPoints(annotHandle, annotation.QuadPoints.ToArray());
        }
    }

    /// <summary>
    /// Maps domain AnnotationType to PDFium AnnotationType.
    /// </summary>
    private PdfiumInterop.AnnotationType MapToPdfiumAnnotationType(AnnotationType type)
    {
        return type switch
        {
            AnnotationType.Text => PdfiumInterop.AnnotationType.Text,
            AnnotationType.Link => PdfiumInterop.AnnotationType.Link,
            AnnotationType.FreeText => PdfiumInterop.AnnotationType.FreeText,
            AnnotationType.Line => PdfiumInterop.AnnotationType.Line,
            AnnotationType.Square => PdfiumInterop.AnnotationType.Square,
            AnnotationType.Circle => PdfiumInterop.AnnotationType.Circle,
            AnnotationType.Polygon => PdfiumInterop.AnnotationType.Polygon,
            AnnotationType.PolyLine => PdfiumInterop.AnnotationType.PolyLine,
            AnnotationType.Highlight => PdfiumInterop.AnnotationType.Highlight,
            AnnotationType.Underline => PdfiumInterop.AnnotationType.Underline,
            AnnotationType.Squiggly => PdfiumInterop.AnnotationType.Squiggly,
            AnnotationType.StrikeOut => PdfiumInterop.AnnotationType.StrikeOut,
            AnnotationType.Stamp => PdfiumInterop.AnnotationType.Stamp,
            AnnotationType.Caret => PdfiumInterop.AnnotationType.Caret,
            AnnotationType.Ink => PdfiumInterop.AnnotationType.Ink,
            AnnotationType.Popup => PdfiumInterop.AnnotationType.Popup,
            AnnotationType.FileAttachment => PdfiumInterop.AnnotationType.FileAttachment,
            AnnotationType.Sound => PdfiumInterop.AnnotationType.Sound,
            AnnotationType.Movie => PdfiumInterop.AnnotationType.Movie,
            AnnotationType.Widget => PdfiumInterop.AnnotationType.Widget,
            AnnotationType.Screen => PdfiumInterop.AnnotationType.Screen,
            AnnotationType.PrinterMark => PdfiumInterop.AnnotationType.PrinterMark,
            AnnotationType.TrapNet => PdfiumInterop.AnnotationType.TrapNet,
            AnnotationType.Watermark => PdfiumInterop.AnnotationType.Watermark,
            AnnotationType.ThreeD => PdfiumInterop.AnnotationType.ThreeD,
            AnnotationType.Redact => PdfiumInterop.AnnotationType.Redact,
            _ => PdfiumInterop.AnnotationType.Unknown
        };
    }

    /// <summary>
    /// Maps PDFium AnnotationType to domain AnnotationType.
    /// </summary>
    private AnnotationType MapToDomainAnnotationType(PdfiumInterop.AnnotationType type)
    {
        return type switch
        {
            PdfiumInterop.AnnotationType.Text => AnnotationType.Text,
            PdfiumInterop.AnnotationType.Link => AnnotationType.Link,
            PdfiumInterop.AnnotationType.FreeText => AnnotationType.FreeText,
            PdfiumInterop.AnnotationType.Line => AnnotationType.Line,
            PdfiumInterop.AnnotationType.Square => AnnotationType.Square,
            PdfiumInterop.AnnotationType.Circle => AnnotationType.Circle,
            PdfiumInterop.AnnotationType.Polygon => AnnotationType.Polygon,
            PdfiumInterop.AnnotationType.PolyLine => AnnotationType.PolyLine,
            PdfiumInterop.AnnotationType.Highlight => AnnotationType.Highlight,
            PdfiumInterop.AnnotationType.Underline => AnnotationType.Underline,
            PdfiumInterop.AnnotationType.Squiggly => AnnotationType.Squiggly,
            PdfiumInterop.AnnotationType.StrikeOut => AnnotationType.StrikeOut,
            PdfiumInterop.AnnotationType.Stamp => AnnotationType.Stamp,
            PdfiumInterop.AnnotationType.Caret => AnnotationType.Caret,
            PdfiumInterop.AnnotationType.Ink => AnnotationType.Ink,
            PdfiumInterop.AnnotationType.Popup => AnnotationType.Popup,
            PdfiumInterop.AnnotationType.FileAttachment => AnnotationType.FileAttachment,
            PdfiumInterop.AnnotationType.Sound => AnnotationType.Sound,
            PdfiumInterop.AnnotationType.Movie => AnnotationType.Movie,
            PdfiumInterop.AnnotationType.Widget => AnnotationType.Widget,
            PdfiumInterop.AnnotationType.Screen => AnnotationType.Screen,
            PdfiumInterop.AnnotationType.PrinterMark => AnnotationType.PrinterMark,
            PdfiumInterop.AnnotationType.TrapNet => AnnotationType.TrapNet,
            PdfiumInterop.AnnotationType.Watermark => AnnotationType.Watermark,
            PdfiumInterop.AnnotationType.ThreeD => AnnotationType.ThreeD,
            PdfiumInterop.AnnotationType.Redact => AnnotationType.Redact,
            _ => AnnotationType.Unknown
        };
    }

    /// <summary>
    /// Creates a standardized PdfError for annotation operations.
    /// </summary>
    private PdfError CreateError(
        string code,
        string message,
        string? filePath,
        Guid correlationId,
        Exception? exception = null)
    {
        var error = new PdfError(code, message, ErrorCategory.Rendering, ErrorSeverity.Error)
            .WithContext("CorrelationId", correlationId);

        if (filePath != null)
        {
            error = error.WithContext("FilePath", filePath);
        }

        if (exception != null)
        {
            error = error.WithContext("Exception", exception.ToString());
        }

        return error;
    }
}
