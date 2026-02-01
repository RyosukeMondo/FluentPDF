using System.Xml;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for importing PDF form data from FDF (Forms Data Format) XML.
/// Follows the FDF specification from ISO 32000-1:2008 Section 12.7.7.
/// </summary>
public sealed class FdfImportService
{
    private readonly ILogger _logger;

    public FdfImportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Imports form field data from an FDF XML file and fills the PDF form.
    /// </summary>
    /// <param name="document">The PDF document to fill.</param>
    /// <param name="fdfPath">The file path of the FDF file to import.</param>
    /// <param name="formService">The form service to set field values.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> ImportAsync(
        PdfDocument document,
        string fdfPath,
        IPdfFormService formService)
    {
        try
        {
            _logger.LogInformation(
                "Starting FDF import from {FdfPath} to document {FilePath}",
                fdfPath,
                document.FilePath);

            // Parse FDF XML
            var fdfData = ParseFdfXml(fdfPath);

            if (fdfData.Count == 0)
            {
                _logger.LogWarning("No field data found in FDF file");
                return Result.Fail(new PdfError(
                    "FDF_NO_DATA",
                    "No field data found in FDF file.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning));
            }

            // Collect all form fields from PDF
            var pdfFields = new Dictionary<string, PdfFormField>();
            for (int page = 1; page <= document.PageCount; page++)
            {
                var fieldsResult = await formService.GetFormFieldsAsync(document, page);
                if (fieldsResult.IsSuccess)
                {
                    foreach (var field in fieldsResult.Value)
                    {
                        pdfFields[field.Name] = field;
                    }
                }
            }

            if (pdfFields.Count == 0)
            {
                _logger.LogWarning("No form fields found in PDF document");
                return Result.Fail(new PdfError(
                    "FDF_NO_PDF_FIELDS",
                    "No form fields found in PDF document.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning));
            }

            // Apply FDF data to PDF fields
            var importedCount = 0;
            var skippedCount = 0;

            foreach (var (fieldName, fieldValue) in fdfData)
            {
                if (!pdfFields.TryGetValue(fieldName, out var pdfField))
                {
                    _logger.LogWarning(
                        "Field '{FieldName}' from FDF not found in PDF",
                        fieldName);
                    skippedCount++;
                    continue;
                }

                var setResult = await SetFieldFromFdf(formService, pdfField, fieldValue);
                if (setResult.IsSuccess)
                {
                    importedCount++;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to set field '{FieldName}': {Errors}",
                        fieldName,
                        setResult.Errors);
                    skippedCount++;
                }
            }

            _logger.LogInformation(
                "FDF import completed. Imported={Imported}, Skipped={Skipped}",
                importedCount,
                skippedCount);

            if (importedCount == 0)
            {
                return Result.Fail(new PdfError(
                    "FDF_NO_FIELDS_IMPORTED",
                    "No fields were successfully imported from FDF.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning)
                    .WithContext("SkippedCount", skippedCount));
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing FDF");
            return Result.Fail(new PdfError(
                "FDF_IMPORT_ERROR",
                $"Failed to import FDF: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FdfPath", fdfPath));
        }
    }

    private Dictionary<string, string> ParseFdfXml(string fdfPath)
    {
        var fieldData = new Dictionary<string, string>();

        try
        {
            using var reader = XmlReader.Create(fdfPath);

            string? currentFieldName = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "field")
                    {
                        currentFieldName = reader.GetAttribute("name");
                    }
                    else if (reader.Name == "value" && currentFieldName != null)
                    {
                        var value = reader.ReadElementContentAsString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            fieldData[currentFieldName] = value;
                        }
                        currentFieldName = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FDF XML file");
            throw;
        }

        return fieldData;
    }

    private async Task<Result> SetFieldFromFdf(
        IPdfFormService formService,
        PdfFormField field,
        string value)
    {
        try
        {
            switch (field.Type)
            {
                case FormFieldType.Text:
                    return await formService.SetFieldValueAsync(field, value);

                case FormFieldType.Checkbox:
                case FormFieldType.RadioButton:
                    var isChecked = value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    return await formService.SetCheckboxStateAsync(field, isChecked);

                case FormFieldType.ComboBox:
                case FormFieldType.ListBox:
                    if (field.Options != null)
                    {
                        var index = field.Options.IndexOf(value);
                        if (index >= 0)
                        {
                            return await formService.SetComboBoxSelectionAsync(field, value);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Option '{Value}' not found in field '{FieldName}'",
                                value,
                                field.Name);
                            return Result.Fail(new PdfError(
                                "FDF_OPTION_NOT_FOUND",
                                $"Option '{value}' not found in combo box.",
                                ErrorCategory.Validation,
                                ErrorSeverity.Warning)
                                .WithContext("FieldName", field.Name)
                                .WithContext("Value", value));
                        }
                    }
                    break;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting field value from FDF");
            return Result.Fail(new PdfError(
                "FDF_SET_FIELD_ERROR",
                $"Failed to set field value: {ex.Message}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FieldName", field.Name));
        }
    }
}
