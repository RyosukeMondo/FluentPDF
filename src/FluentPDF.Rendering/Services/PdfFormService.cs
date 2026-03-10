using System.Runtime.InteropServices;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service implementation for PDF form field operations using PDFium.
/// Handles form field detection, value manipulation, and persistence.
/// </summary>
public sealed partial class PdfFormService : IPdfFormService
{
    private readonly ILogger<PdfFormService> _logger;

    public PdfFormService(ILogger<PdfFormService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PdfFormField>>> GetFormFieldsAsync(
        PdfDocument document,
        int pageNumber)
    {
        if (document == null)
        {
            return Result.Fail(new PdfError(
                "FORM_INVALID_DOCUMENT",
                "Document cannot be null.",
                ErrorCategory.Validation,
                ErrorSeverity.Error));
        }

        if (pageNumber < 1 || pageNumber > document.PageCount)
        {
            return Result.Fail(new PdfError(
                "FORM_INVALID_PAGE",
                $"Page number {pageNumber} is out of range (1-{document.PageCount}).",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("PageNumber", pageNumber)
                .WithContext("PageCount", document.PageCount));
        }

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation(
                    "Loading form fields for document {FilePath}, page {PageNumber}",
                    document.FilePath,
                    pageNumber);

                var docHandle = (SafePdfDocumentHandle)document.Handle;
                var fields = new List<PdfFormField>();

                // Initialize form environment
                // Note: Passing IntPtr.Zero for forminfo is acceptable for basic form reading
                // Full form editing requires proper FPDF_FORMFILLINFO callbacks
                using var formHandle = PdfiumFormInterop.InitFormFillEnvironment(docHandle, IntPtr.Zero);
                if (formHandle.IsInvalid)
                {
                    // This is expected for PDFs without forms or when forminfo callbacks aren't provided
                    // Log as Debug since it's non-critical for viewing
                    _logger.LogDebug(
                        "Form environment not available for {FilePath} (PDF may not contain forms)",
                        document.FilePath);
                    return Result.Ok<IReadOnlyList<PdfFormField>>(fields);
                }

                // Load the page
                using var pageHandle = PdfiumInterop.LoadPage(docHandle, pageNumber - 1);
                if (pageHandle.IsInvalid)
                {
                    return Result.Fail(new PdfError(
                        "FORM_PAGE_LOAD_FAILED",
                        $"Failed to load page {pageNumber}.",
                        ErrorCategory.Rendering,
                        ErrorSeverity.Error)
                        .WithContext("PageNumber", pageNumber));
                }

                // Get annotation count (form fields are widget annotations)
                var annotCount = PdfiumFormInterop.GetAnnotationCount(pageHandle);
                if (annotCount <= 0)
                {
                    _logger.LogInformation(
                        "No form fields found on page {PageNumber}",
                        pageNumber);
                    return Result.Ok<IReadOnlyList<PdfFormField>>(fields);
                }

                // Enumerate annotations and filter for form fields
                for (int i = 0; i < annotCount; i++)
                {
                    var annot = PdfiumFormInterop.GetAnnotation(pageHandle, i);
                    if (annot == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        var subtype = PdfiumFormInterop.GetAnnotationSubtype(annot);
                        if (subtype != PdfiumFormInterop.AnnotationSubtype.Widget)
                        {
                            continue; // Not a form field
                        }

                        var field = ExtractFormField(formHandle, annot, pageNumber, i);
                        if (field != null)
                        {
                            fields.Add(field);
                        }
                    }
                    finally
                    {
                        PdfiumFormInterop.CloseAnnotation(annot);
                    }
                }

                _logger.LogInformation(
                    "Found {FieldCount} form fields on page {PageNumber}",
                    fields.Count,
                    pageNumber);

                return Result.Ok<IReadOnlyList<PdfFormField>>(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading form fields from page {PageNumber}",
                    pageNumber);

                return Result.Fail(new PdfError(
                    "FORM_LOAD_ERROR",
                    $"Failed to load form fields: {ex.Message}",
                    ErrorCategory.Rendering,
                    ErrorSeverity.Error)
                    .WithContext("PageNumber", pageNumber)
                    .WithContext("Exception", ex.GetType().Name));
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<PdfFormField?>> GetFormFieldAtPointAsync(
        PdfDocument document,
        int pageNumber,
        double x,
        double y)
    {
        if (document == null)
        {
            return Result.Fail(new PdfError(
                "FORM_INVALID_DOCUMENT",
                "Document cannot be null.",
                ErrorCategory.Validation,
                ErrorSeverity.Error));
        }

        if (pageNumber < 1 || pageNumber > document.PageCount)
        {
            return Result.Fail(new PdfError(
                "FORM_INVALID_PAGE",
                $"Page number {pageNumber} is out of range (1-{document.PageCount}).",
                ErrorCategory.Validation,
                ErrorSeverity.Error));
        }

        return await Task.Run(() =>
        {
            try
            {
                var docHandle = (SafePdfDocumentHandle)document.Handle;

                using var formHandle = PdfiumFormInterop.InitFormFillEnvironment(docHandle, IntPtr.Zero);
                if (formHandle.IsInvalid)
                {
                    return Result.Ok<PdfFormField?>(null);
                }

                using var pageHandle = PdfiumInterop.LoadPage(docHandle, pageNumber - 1);
                if (pageHandle.IsInvalid)
                {
                    return Result.Fail(new PdfError(
                        "FORM_PAGE_LOAD_FAILED",
                        $"Failed to load page {pageNumber}.",
                        ErrorCategory.Rendering,
                        ErrorSeverity.Error));
                }

                var fieldType = PdfiumFormInterop.GetFormFieldTypeAtPoint(formHandle, pageHandle, x, y);
                if (fieldType == PdfiumFormInterop.FieldType.Unknown)
                {
                    return Result.Ok<PdfFormField?>(null);
                }

                // Find the annotation at this point
                var annotCount = PdfiumFormInterop.GetAnnotationCount(pageHandle);
                for (int i = 0; i < annotCount; i++)
                {
                    var annot = PdfiumFormInterop.GetAnnotation(pageHandle, i);
                    if (annot == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        if (!PdfiumFormInterop.GetAnnotationRect(annot, out var left, out var bottom, out var right, out var top))
                        {
                            continue;
                        }

                        // Check if point is within bounds
                        if (x >= left && x <= right && y >= bottom && y <= top)
                        {
                            var field = ExtractFormField(formHandle, annot, pageNumber, i);
                            return Result.Ok(field);
                        }
                    }
                    finally
                    {
                        PdfiumFormInterop.CloseAnnotation(annot);
                    }
                }

                return Result.Ok<PdfFormField?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error finding form field at point ({X}, {Y}) on page {PageNumber}",
                    x,
                    y,
                    pageNumber);

                return Result.Fail(new PdfError(
                    "FORM_FIELD_NOT_FOUND",
                    $"Error finding form field: {ex.Message}",
                    ErrorCategory.Rendering,
                    ErrorSeverity.Error));
            }
        });
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<PdfFormField>> GetFieldsInTabOrder(
        IReadOnlyList<PdfFormField> fields)
    {
        if (fields == null)
        {
            return Result.Fail(new PdfError(
                "FORM_INVALID_FIELDS",
                "Fields collection cannot be null.",
                ErrorCategory.Validation,
                ErrorSeverity.Error));
        }

        try
        {
            var sortedFields = fields
                .OrderBy(f => f.TabOrder >= 0 ? f.TabOrder : int.MaxValue)
                .ThenBy(f => f.Bounds.Top)
                .ThenBy(f => f.Bounds.Left)
                .ToList();

            return Result.Ok<IReadOnlyList<PdfFormField>>(sortedFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sorting form fields by tab order");

            return Result.Fail(new PdfError(
                "FORM_SORT_FAILED",
                $"Failed to sort fields: {ex.Message}",
                ErrorCategory.Validation,
                ErrorSeverity.Error));
        }
    }

    private PdfFormField? ExtractFormField(
        SafePdfFormHandle formHandle,
        IntPtr annot,
        int pageNumber,
        int index)
    {
        try
        {
            var name = PdfiumFormInterop.GetFormFieldName(annot);
            if (string.IsNullOrEmpty(name))
            {
                name = $"Field_{index}";
            }

            var flags = PdfiumFormInterop.GetFormFieldFlags(formHandle, annot);
            var isReadOnly = (flags & PdfiumFormInterop.FieldFlags.ReadOnly) != 0;
            var isRequired = (flags & PdfiumFormInterop.FieldFlags.Required) != 0;

            if (!PdfiumFormInterop.GetAnnotationRect(annot, out var left, out var bottom, out var right, out var top))
            {
                _logger.LogWarning("Failed to get bounds for field {FieldName}", name);
                return null;
            }

            var bounds = new PdfRectangle(left, top, right, bottom);

            // Determine field type from annotation
            var fieldType = DetermineFieldType(formHandle, annot);

            var field = new PdfFormField
            {
                Name = name,
                Type = fieldType,
                PageNumber = pageNumber,
                Bounds = bounds,
                TabOrder = index,
                IsRequired = isRequired,
                IsReadOnly = isReadOnly,
                NativeHandle = annot
            };

            // Extract field-type-specific properties
            if (fieldType == FormFieldType.Text)
            {
                field.Value = PdfiumFormInterop.GetFormFieldValue(formHandle, annot);
            }
            else if (fieldType == FormFieldType.Checkbox || fieldType == FormFieldType.RadioButton)
            {
                field.IsChecked = PdfiumFormInterop.IsFormFieldChecked(formHandle, annot);
            }

            return field;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting form field at index {Index}", index);
            return null;
        }
    }

    private FormFieldType DetermineFieldType(SafePdfFormHandle formHandle, IntPtr annot)
    {
        // We need a better way to determine field type
        // For now, check if it's checkable (checkbox/radio) or text
        var value = PdfiumFormInterop.GetFormFieldValue(formHandle, annot);
        if (!string.IsNullOrEmpty(value))
        {
            return FormFieldType.Text;
        }

        var isChecked = PdfiumFormInterop.IsFormFieldChecked(formHandle, annot);
        // If IsChecked API works, assume it's a checkbox
        // This is a simplification - we'd need more PDFium APIs to determine exact type
        return FormFieldType.Checkbox;
    }
}
