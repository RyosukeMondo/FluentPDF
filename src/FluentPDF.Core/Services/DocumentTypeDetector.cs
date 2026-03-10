namespace FluentPDF.Core.Services;

/// <summary>
/// Detects document type from metadata and content for AI-driven suggestions.
/// </summary>
public static class DocumentTypeDetector
{
    /// <summary>
    /// Detects the document type and returns a contextual suggestion.
    /// Returns null if no suggestion is appropriate.
    /// </summary>
    public static string? GetSuggestion(string? title, string? author, int pageCount, string? firstPageText)
    {
        var text = (firstPageText ?? "").ToLowerInvariant();
        var titleLower = (title ?? "").ToLowerInvariant();

        // Regulatory / standards documents
        if (ContainsAny(text, "regulation", "un-r", "fmvss", "iso ", "iec ", "standard", "directive") ||
            ContainsAny(titleLower, "regulation", "standard", "directive", "iso", "iec"))
        {
            return pageCount > 50
                ? $"This looks like a {pageCount}-page regulation. Want me to find the key requirements?"
                : $"This looks like a regulatory document. Want me to summarize the main requirements?";
        }

        // Contracts / legal
        if (ContainsAny(text, "agreement", "contract", "whereas", "hereinafter", "party", "clause", "indemnif"))
        {
            return "This looks like a contract. Want me to find the key clauses and obligations?";
        }

        // Research papers / academic
        if (ContainsAny(text, "abstract", "keywords", "introduction", "methodology", "references", "doi:"))
        {
            return "This looks like a research paper. Want me to summarize the key findings?";
        }

        // Financial reports
        if (ContainsAny(text, "balance sheet", "income statement", "cash flow", "revenue", "fiscal year", "quarterly"))
        {
            return "This looks like a financial report. Want me to highlight the key figures?";
        }

        // Technical manuals
        if (ContainsAny(text, "table of contents", "installation", "troubleshooting", "maintenance", "specifications"))
        {
            return "This looks like a technical manual. Want me to find the section you need?";
        }

        // Forms
        if (ContainsAny(text, "please fill", "signature", "date of birth", "applicant", "form no"))
        {
            return "This document contains forms. Want me to identify the fillable fields?";
        }

        // Large documents without specific type
        if (pageCount > 100)
        {
            return $"This is a {pageCount}-page document. Want me to search for something specific?";
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
