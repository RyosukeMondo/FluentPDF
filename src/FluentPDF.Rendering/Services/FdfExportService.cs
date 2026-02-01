using System.Text;
using System.Xml;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for exporting PDF form data to FDF (Forms Data Format) XML.
/// Follows the FDF specification from ISO 32000-1:2008 Section 12.7.7.
/// </summary>
public sealed class FdfExportService
{
    private readonly ILogger _logger;

    public FdfExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports form field data from all pages to an FDF XML file.
    /// </summary>
    /// <param name="document">The PDF document containing form fields.</param>
    /// <param name="outputPath">The file path where the FDF should be saved.</param>
    /// <param name="formService">The form service to retrieve field data.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> ExportAsync(
        PdfDocument document,
        string outputPath,
        IPdfFormService formService)
    {
        try
        {
            _logger.LogInformation(
                "Starting FDF export for document {FilePath} to {OutputPath}",
                document.FilePath,
                outputPath);

            var allFields = new List<PdfFormField>();

            // Collect form fields from all pages
            for (int page = 1; page <= document.PageCount; page++)
            {
                var fieldsResult = await formService.GetFormFieldsAsync(document, page);
                if (fieldsResult.IsSuccess && fieldsResult.Value.Count > 0)
                {
                    allFields.AddRange(fieldsResult.Value);
                }
            }

            if (allFields.Count == 0)
            {
                _logger.LogWarning("No form fields found in document");
                return Result.Fail(new PdfError(
                    "FDF_NO_FIELDS",
                    "No form fields found to export.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Warning));
            }

            // Generate FDF XML
            var fdfXml = GenerateFdfXml(document.FilePath, allFields);

            // Write to file
            await File.WriteAllTextAsync(outputPath, fdfXml, Encoding.UTF8);

            _logger.LogInformation(
                "Successfully exported {FieldCount} form fields to FDF",
                allFields.Count);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting FDF");
            return Result.Fail(new PdfError(
                "FDF_EXPORT_ERROR",
                $"Failed to export FDF: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("OutputPath", outputPath));
        }
    }

    private string GenerateFdfXml(string pdfPath, List<PdfFormField> fields)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var writer = XmlWriter.Create(stringWriter, settings);

        writer.WriteStartDocument();

        // FDF root element
        writer.WriteStartElement("xfdf");
        writer.WriteAttributeString("xmlns", "http://ns.adobe.com/xfdf/");
        writer.WriteAttributeString("xml:space", "preserve");

        // Source PDF file reference
        writer.WriteStartElement("f");
        writer.WriteAttributeString("href", Path.GetFileName(pdfPath));
        writer.WriteEndElement(); // f

        // Fields container
        writer.WriteStartElement("fields");

        foreach (var field in fields)
        {
            WriteFieldElement(writer, field);
        }

        writer.WriteEndElement(); // fields

        writer.WriteEndElement(); // xfdf
        writer.WriteEndDocument();

        return stringWriter.ToString();
    }

    private void WriteFieldElement(XmlWriter writer, PdfFormField field)
    {
        writer.WriteStartElement("field");
        writer.WriteAttributeString("name", field.Name);

        switch (field.Type)
        {
            case FormFieldType.Text:
                if (!string.IsNullOrEmpty(field.Value))
                {
                    writer.WriteStartElement("value");
                    writer.WriteString(field.Value);
                    writer.WriteEndElement();
                }
                break;

            case FormFieldType.Checkbox:
            case FormFieldType.RadioButton:
                writer.WriteStartElement("value");
                writer.WriteString(field.IsChecked == true ? "Yes" : "Off");
                writer.WriteEndElement();
                break;

            case FormFieldType.ComboBox:
            case FormFieldType.ListBox:
                if (field.SelectedOption != null)
                {
                    writer.WriteStartElement("value");
                    writer.WriteString(field.SelectedOption);
                    writer.WriteEndElement();
                }
                break;
        }

        writer.WriteEndElement(); // field
    }
}
