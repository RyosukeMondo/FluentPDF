using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Document intelligence features: page summary extraction and document type suggestions.
/// </summary>
public partial class PdfViewerViewModel
{
    private async void ExtractPageSummaryAsync(PdfDocument document, int pageNumber)
    {
        try
        {
            var result = await _textExtractionService.ExtractTextAsync(document, pageNumber);
            if (result.IsFailed || string.IsNullOrWhiteSpace(result.Value))
            {
                PageSummary = null;
                return;
            }

            var text = result.Value.Trim();
            var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(firstLine))
            {
                PageSummary = null;
                return;
            }

            PageSummary = firstLine.Length > 120 ? firstLine[..117] + "..." : firstLine;
        }
        catch
        {
            PageSummary = null;
        }
    }

    private async void ShowDocumentSuggestionAsync(PdfDocument document)
    {
        if (_notificationService == null || _textExtractionService == null) return;

        try
        {
            string? firstPageText = null;
            try
            {
                var textResult = await _textExtractionService.ExtractTextAsync(document, 1);
                if (textResult.IsSuccess)
                    firstPageText = textResult.Value.Length > 500 ? textResult.Value[..500] : textResult.Value;
            }
            catch { /* non-critical */ }

            var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
            var suggestion = DocumentTypeDetector.GetSuggestion(
                fileName, null, document.PageCount, firstPageText);

            if (suggestion != null)
            {
                _notificationService.ShowInfo(suggestion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Document suggestion detection failed (non-fatal)");
        }
    }
}
