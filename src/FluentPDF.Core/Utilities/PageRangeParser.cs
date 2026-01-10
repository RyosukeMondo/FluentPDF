using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Utilities;

/// <summary>
/// Utility for parsing page range strings into structured PageRange objects.
/// Supports formats like "1-5", "10", "1-5, 10, 15-20".
/// </summary>
public static class PageRangeParser
{
    /// <summary>
    /// Parses a page range string into a list of PageRange objects.
    /// </summary>
    /// <param name="rangeString">
    /// Page range specification using comma-separated values.
    /// Examples: "1-5", "10", "1-5, 10, 15-20", "1-3,7,10-15"
    /// Whitespace is ignored. Pages are 1-based.
    /// </param>
    /// <returns>
    /// A Result containing a list of PageRange objects if successful,
    /// or a PdfError with ErrorCategory.Validation if the format is invalid.
    /// </returns>
    /// <remarks>
    /// Valid formats:
    /// - Single page: "5" → [(5,5)]
    /// - Range: "1-5" → [(1,5)]
    /// - Multiple ranges: "1-5, 10, 15-20" → [(1,5), (10,10), (15,20)]
    ///
    /// Invalid formats return validation errors:
    /// - Null or empty string
    /// - Negative or zero page numbers
    /// - Invalid syntax (missing numbers, extra dashes, etc.)
    /// - Reverse ranges where start > end
    /// </remarks>
    public static Result<List<PageRange>> Parse(string rangeString)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(rangeString))
        {
            return Result.Fail(new PdfError(
                "PDF_VALIDATION_PAGE_RANGE_EMPTY",
                "Page range string cannot be null or empty.",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("RangeString", rangeString ?? "null"));
        }

        var ranges = new List<PageRange>();
        var parts = rangeString.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return Result.Fail(new PdfError(
                "PDF_VALIDATION_PAGE_RANGE_EMPTY",
                "Page range string contains no valid parts.",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("RangeString", rangeString));
        }

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();

            if (string.IsNullOrWhiteSpace(trimmedPart))
            {
                continue; // Skip empty parts
            }

            var parseResult = ParseSingleRange(trimmedPart);
            if (parseResult.IsFailed)
            {
                // Add context about which part failed
                var error = parseResult.Errors[0] as PdfError;
                if (error != null)
                {
                    error = error.WithContext("FullRangeString", rangeString);
                }
                return Result.Fail(parseResult.Errors);
            }

            ranges.Add(parseResult.Value);
        }

        if (ranges.Count == 0)
        {
            return Result.Fail(new PdfError(
                "PDF_VALIDATION_PAGE_RANGE_NO_VALID_PARTS",
                "No valid page ranges found in the input string.",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("RangeString", rangeString));
        }

        return Result.Ok(ranges);
    }

    /// <summary>
    /// Parses a single range part (either "5" or "1-5").
    /// </summary>
    private static Result<PageRange> ParseSingleRange(string rangePart)
    {
        var dashIndex = rangePart.IndexOf('-');

        if (dashIndex == -1)
        {
            // Single page number
            if (!int.TryParse(rangePart, out var pageNumber))
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"Invalid page number format: '{rangePart}'. Expected a positive integer.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart));
            }

            if (pageNumber <= 0)
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"Page number must be greater than 0. Got: {pageNumber}",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("PageNumber", pageNumber));
            }

            return Result.Ok(new PageRange { StartPage = pageNumber, EndPage = pageNumber });
        }
        else
        {
            // Range format "start-end"
            var startStr = rangePart.Substring(0, dashIndex).Trim();
            var endStr = rangePart.Substring(dashIndex + 1).Trim();

            // Check for multiple dashes or empty parts
            if (string.IsNullOrWhiteSpace(startStr) || string.IsNullOrWhiteSpace(endStr))
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_FORMAT",
                    $"Invalid range format: '{rangePart}'. Expected format: 'start-end'.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart));
            }

            // Check for additional dashes
            if (endStr.Contains('-'))
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_FORMAT",
                    $"Invalid range format: '{rangePart}'. Too many dashes.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart));
            }

            if (!int.TryParse(startStr, out var startPage))
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"Invalid start page number: '{startStr}'. Expected a positive integer.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("StartString", startStr));
            }

            if (!int.TryParse(endStr, out var endPage))
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"Invalid end page number: '{endStr}'. Expected a positive integer.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("EndString", endStr));
            }

            if (startPage <= 0)
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"Start page must be greater than 0. Got: {startPage}",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("StartPage", startPage));
            }

            if (endPage <= 0)
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER",
                    $"End page must be greater than 0. Got: {endPage}",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("EndPage", endPage));
            }

            if (startPage > endPage)
            {
                return Result.Fail(new PdfError(
                    "PDF_VALIDATION_PAGE_RANGE_REVERSE",
                    $"Invalid range: start page ({startPage}) is greater than end page ({endPage}). Use format: 'smaller-larger'.",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("Part", rangePart)
                    .WithContext("StartPage", startPage)
                    .WithContext("EndPage", endPage));
            }

            return Result.Ok(new PageRange { StartPage = startPage, EndPage = endPage });
        }
    }
}
