using System.Net;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for DOCX document parsing using DocumentFormat.OpenXml library.
/// Implements asynchronous operations with comprehensive error handling and structured logging.
/// Converts DOCX files to clean HTML with embedded images as base64 data URIs.
/// </summary>
public sealed class DocxParserService : IDocxParserService
{
    private readonly ILogger<DocxParserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocxParserService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public DocxParserService(ILogger<DocxParserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<string>> ParseDocxToHtmlAsync(string filePath)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Parsing DOCX document. CorrelationId={CorrelationId}, FilePath={FilePath}",
            correlationId, filePath);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            var error = new PdfError(
                "DOCX_FILE_NOT_FOUND",
                $"DOCX file not found: {filePath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "DOCX file not found. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }

        // Validate file extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".docx")
        {
            var error = new PdfError(
                "DOCX_INVALID_FORMAT",
                $"File is not a DOCX document. Extension: {extension}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("Extension", extension)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Invalid file format. CorrelationId={CorrelationId}, FilePath={FilePath}, Extension={Extension}",
                correlationId, filePath, extension);

            return Result.Fail(error);
        }

        try
        {
            // Parse DOCX on background thread
            var html = await Task.Run(() =>
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;

                if (body == null)
                {
                    return "<html><body></body></html>";
                }

                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head><meta charset='utf-8'></head>");
                htmlBuilder.AppendLine("<body>");

                ConvertBodyToHtml(body, htmlBuilder, doc.MainDocumentPart);

                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                return htmlBuilder.ToString();
            });

            var htmlLength = html.Length;
            var fileSize = new FileInfo(filePath).Length;

            _logger.LogInformation(
                "DOCX parsed successfully. CorrelationId={CorrelationId}, FilePath={FilePath}, HtmlLength={HtmlLength}, FileSizeBytes={FileSizeBytes}",
                correlationId, filePath, htmlLength, fileSize);

            return Result.Ok(html);
        }
        catch (OpenXmlPackageException ex)
        {
            var error = new PdfError(
                "DOCX_INVALID_FORMAT",
                $"Invalid DOCX format or corrupted file: {ex.Message}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Invalid DOCX format. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
        catch (IOException ex)
        {
            var error = new PdfError(
                "DOCX_READ_FAILED",
                $"Failed to read DOCX file: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to read DOCX file. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DOCX_PARSE_FAILED",
                $"Failed to parse DOCX document: {ex.Message}",
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("FilePath", filePath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to parse DOCX document. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);

            return Result.Fail(error);
        }
    }

    private void ConvertBodyToHtml(Body body, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph paragraph:
                    ConvertParagraphToHtml(paragraph, htmlBuilder, mainPart);
                    break;
                case Table table:
                    ConvertTableToHtml(table, htmlBuilder, mainPart);
                    break;
            }
        }
    }

    private void ConvertParagraphToHtml(Paragraph paragraph, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        var hasContent = paragraph.Elements<Run>().Any() || paragraph.Elements<Hyperlink>().Any();
        if (!hasContent)
        {
            htmlBuilder.AppendLine("<p>&nbsp;</p>");
            return;
        }

        htmlBuilder.Append("<p>");

        foreach (var element in paragraph.Elements())
        {
            switch (element)
            {
                case Run run:
                    ConvertRunToHtml(run, htmlBuilder, mainPart);
                    break;
                case Hyperlink hyperlink:
                    ConvertHyperlinkToHtml(hyperlink, htmlBuilder, mainPart);
                    break;
            }
        }

        htmlBuilder.AppendLine("</p>");
    }

    private void ConvertRunToHtml(Run run, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        var runProperties = run.RunProperties;
        var isBold = runProperties?.Bold != null;
        var isItalic = runProperties?.Italic != null;
        var isUnderline = runProperties?.Underline != null;

        if (isBold)
            htmlBuilder.Append("<strong>");
        if (isItalic)
            htmlBuilder.Append("<em>");
        if (isUnderline)
            htmlBuilder.Append("<u>");

        foreach (var element in run.Elements())
        {
            switch (element)
            {
                case Text text:
                    htmlBuilder.Append(WebUtility.HtmlEncode(text.Text));
                    break;
                case Break:
                    htmlBuilder.Append("<br/>");
                    break;
                case Drawing drawing:
                    ConvertDrawingToHtml(drawing, htmlBuilder, mainPart);
                    break;
            }
        }

        if (isUnderline)
            htmlBuilder.Append("</u>");
        if (isItalic)
            htmlBuilder.Append("</em>");
        if (isBold)
            htmlBuilder.Append("</strong>");
    }

    private void ConvertHyperlinkToHtml(Hyperlink hyperlink, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        var id = hyperlink.Id?.Value;
        string? url = null;

        if (!string.IsNullOrEmpty(id) && mainPart != null)
        {
            var relationship = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == id);
            url = relationship?.Uri?.ToString();
        }

        if (!string.IsNullOrEmpty(url))
        {
            htmlBuilder.Append($"<a href=\"{WebUtility.HtmlEncode(url)}\">");
        }

        foreach (var run in hyperlink.Elements<Run>())
        {
            ConvertRunToHtml(run, htmlBuilder, mainPart);
        }

        if (!string.IsNullOrEmpty(url))
        {
            htmlBuilder.Append("</a>");
        }
    }

    private void ConvertDrawingToHtml(Drawing drawing, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        if (mainPart == null)
            return;

        var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
        if (blip?.Embed?.Value == null)
            return;

        try
        {
            var imagePart = mainPart.GetPartById(blip.Embed.Value) as ImagePart;
            if (imagePart == null)
                return;

            using var stream = imagePart.GetStream();
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(imageBytes);
            var contentType = imagePart.ContentType;

            htmlBuilder.Append($"<img src=\"data:{contentType};base64,{base64}\" />");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert drawing to HTML image");
        }
    }

    private void ConvertTableToHtml(Table table, StringBuilder htmlBuilder, MainDocumentPart? mainPart)
    {
        htmlBuilder.AppendLine("<table border='1' style='border-collapse: collapse;'>");

        foreach (var row in table.Elements<TableRow>())
        {
            htmlBuilder.AppendLine("<tr>");
            foreach (var cell in row.Elements<TableCell>())
            {
                htmlBuilder.Append("<td>");
                foreach (var paragraph in cell.Elements<Paragraph>())
                {
                    ConvertParagraphToHtml(paragraph, htmlBuilder, mainPart);
                }
                htmlBuilder.AppendLine("</td>");
            }
            htmlBuilder.AppendLine("</tr>");
        }

        htmlBuilder.AppendLine("</table>");
    }
}
