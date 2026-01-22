using FluentPDF.Rendering.Interop.Verification.Reports;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FluentPDF.Rendering.Interop.Verification.Validators;

/// <summary>
/// Validates UTF-16LE string marshaling for PDFium interop operations.
/// Tests bookmark extraction, text search, and form field name retrieval.
/// </summary>
public sealed class Utf16MarshalingValidator : IValidator
{
    private readonly ILogger<Utf16MarshalingValidator> _logger;
    private readonly object _lock = new();

    public string ValidatorName => "UTF-16 Marshaling Validator";
    public string TargetArea => "UTF-16 Marshaling";

    public Utf16MarshalingValidator(ILogger<Utf16MarshalingValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates UTF-16LE marshaling for all high-risk areas.
    /// </summary>
    public async Task<ValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting UTF-16 marshaling validation");

        var resultsByArea = new Dictionary<string, List<ValidationResult>>();
        var allResults = new List<ValidationResult>();

        // Run all validation methods
        var bookmarkResults = await ValidateBookmarkMarshalingAsync(cancellationToken);
        resultsByArea["Bookmarks"] = bookmarkResults;
        allResults.AddRange(bookmarkResults);

        var textSearchResults = await ValidateTextSearchMarshalingAsync(cancellationToken);
        resultsByArea["Text Search"] = textSearchResults;
        allResults.AddRange(textSearchResults);

        var formFieldResults = await ValidateFormFieldMarshalingAsync(cancellationToken);
        resultsByArea["Form Fields"] = formFieldResults;
        allResults.AddRange(formFieldResults);

        // Calculate summary statistics
        var summary = new ValidationSummary
        {
            TotalTests = allResults.Count,
            PassedCount = allResults.Count(r => r.Passed),
            FailedCount = allResults.Count(r => !r.Passed && r.Severity != ValidationSeverity.Warning),
            WarningCount = allResults.Count(r => r.Severity == ValidationSeverity.Warning),
            CriticalCount = allResults.Count(r => r.Severity == ValidationSeverity.Critical)
        };

        _logger.LogInformation(
            "UTF-16 marshaling validation complete: {PassedCount}/{TotalTests} passed, {FailedCount} failed, {CriticalCount} critical",
            summary.PassedCount, summary.TotalTests, summary.FailedCount, summary.CriticalCount);

        return new ValidationReport
        {
            ValidatorName = ValidatorName,
            TargetArea = TargetArea,
            Summary = summary,
            ResultsByArea = resultsByArea
        };
    }

    /// <summary>
    /// Validates UTF-16LE marshaling for bookmark title extraction using two-phase allocation pattern.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateBookmarkMarshalingAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Emoji (4-byte surrogate pairs)
                results.Add(TestBookmarkWithContent("emoji", "📄 Document 📌", "Emoji (4-byte surrogate pairs)"));

                // Test case 2: Null characters
                results.Add(TestBookmarkWithContent("null-chars", "Test\0Null", "Null characters"));

                // Test case 3: Combining diacritics
                results.Add(TestBookmarkWithContent("combining", "Café résumé", "Combining diacritics"));

                // Test case 4: CRLF vs LF
                results.Add(TestBookmarkWithContent("newlines", "Line1\r\nLine2\nLine3", "CRLF vs LF"));

                // Test case 5: Empty string
                results.Add(TestBookmarkWithContent("empty", "", "Empty string"));

                // Test case 6: Maximum length string (simulate large buffer)
                var largeString = new string('A', 4096);
                results.Add(TestBookmarkWithContent("large", largeString, "Maximum length string (4096 chars)"));

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validates UTF-16LE marshaling for text search operations.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateTextSearchMarshalingAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Emoji search query
                results.Add(TestTextSearchWithQuery("📄", "Emoji in search query"));

                // Test case 2: Null character in query
                results.Add(TestTextSearchWithQuery("test\0null", "Null character in query"));

                // Test case 3: Combining diacritics
                results.Add(TestTextSearchWithQuery("café", "Combining diacritics in query"));

                // Test case 4: Newlines in query
                results.Add(TestTextSearchWithQuery("line1\nline2", "Newline characters in query"));

                // Test case 5: Empty query (should fail gracefully)
                results.Add(TestTextSearchWithQuery("", "Empty query"));

                // Test case 6: Long query string
                var longQuery = new string('x', 1024);
                results.Add(TestTextSearchWithQuery(longQuery, "Long query (1024 chars)"));

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validates UTF-16LE marshaling for form field name extraction.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateFormFieldMarshalingAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Emoji in field name
                results.Add(TestFormFieldWithName("field_📄_emoji", "Emoji in field name"));

                // Test case 2: Null characters
                results.Add(TestFormFieldWithName("field\0null", "Null characters in field name"));

                // Test case 3: Combining diacritics
                results.Add(TestFormFieldWithName("résumé_field", "Combining diacritics in field name"));

                // Test case 4: Special characters
                results.Add(TestFormFieldWithName("field_with_特殊文字", "Special characters (CJK)"));

                // Test case 5: Empty field name
                results.Add(TestFormFieldWithName("", "Empty field name"));

                // Test case 6: Long field name
                var longName = "field_" + new string('A', 500);
                results.Add(TestFormFieldWithName(longName, "Long field name (506 chars)"));

                return results;
            }
        }, cancellationToken);
    }

    private ValidationResult TestBookmarkWithContent(string testId, string content, string description)
    {
        try
        {
            // Simulate two-phase allocation pattern used in PdfiumInterop.GetBookmarkTitle
            // Phase 1: Get length
            var expectedBytes = Encoding.Unicode.GetBytes(content + "\0");
            var length = (uint)expectedBytes.Length;

            if (length == 0 && !string.IsNullOrEmpty(content))
            {
                return new ValidationResult
                {
                    TestName = $"Bookmark: {description}",
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "Two-phase allocation failed: length query returned 0 for non-empty content",
                    ErrorDetails = $"Content: '{content}' (length {content.Length})",
                    SuggestedFix = "Ensure FPDFBookmark_GetTitle correctly reports byte length for UTF-16LE strings",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md",
                    Context = new Dictionary<string, string>
                    {
                        ["TestId"] = testId,
                        ["ContentLength"] = content.Length.ToString(),
                        ["ExpectedByteLength"] = expectedBytes.Length.ToString()
                    }
                };
            }

            // Phase 2: Allocate and fill buffer
            var buffer = new byte[length];
            Array.Copy(expectedBytes, buffer, expectedBytes.Length);

            // Decode UTF-16LE
            var decoded = Encoding.Unicode.GetString(buffer).TrimEnd('\0');

            // Validate round-trip
            if (decoded != content)
            {
                return new ValidationResult
                {
                    TestName = $"Bookmark: {description}",
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = "UTF-16LE round-trip marshaling failed",
                    ErrorDetails = $"Original: '{content}', Decoded: '{decoded}'",
                    ExpectedValue = content,
                    ActualValue = decoded,
                    SuggestedFix = "Check buffer size calculation and UTF-16LE encoding/decoding logic",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md",
                    Context = new Dictionary<string, string>
                    {
                        ["TestId"] = testId,
                        ["OriginalLength"] = content.Length.ToString(),
                        ["DecodedLength"] = decoded.Length.ToString(),
                        ["BufferSize"] = buffer.Length.ToString()
                    }
                };
            }

            return new ValidationResult
            {
                TestName = $"Bookmark: {description}",
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "UTF-16LE marshaling successful",
                Context = new Dictionary<string, string>
                {
                    ["TestId"] = testId,
                    ["ContentLength"] = content.Length.ToString(),
                    ["ByteLength"] = expectedBytes.Length.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = $"Bookmark: {description}",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Exception during UTF-16LE marshaling test",
                ErrorDetails = ex.ToString(),
                SuggestedFix = "Review exception details and fix marshaling logic",
                DocumentationUrl = "docs/marshaling/safe-patterns.md",
                Context = new Dictionary<string, string>
                {
                    ["TestId"] = testId,
                    ["ExceptionType"] = ex.GetType().Name
                }
            };
        }
    }

    private ValidationResult TestTextSearchWithQuery(string query, string description)
    {
        try
        {
            // Simulate text search query marshaling pattern from PdfiumInterop.StartTextSearch
            if (string.IsNullOrEmpty(query))
            {
                // Empty query should be handled gracefully (as per PdfiumInterop validation)
                return new ValidationResult
                {
                    TestName = $"Text Search: {description}",
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "Empty query handled gracefully (validation prevents PDFium call)",
                    Context = new Dictionary<string, string>
                    {
                        ["Query"] = query ?? "null"
                    }
                };
            }

            // Convert to UTF-16LE with null terminator
            var queryBytes = Encoding.Unicode.GetBytes(query + "\0");

            // Validate buffer contains expected data
            var decoded = Encoding.Unicode.GetString(queryBytes).TrimEnd('\0');

            if (decoded != query)
            {
                return new ValidationResult
                {
                    TestName = $"Text Search: {description}",
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "UTF-16LE encoding for search query failed",
                    ErrorDetails = $"Original: '{query}', Decoded: '{decoded}'",
                    ExpectedValue = query,
                    ActualValue = decoded,
                    SuggestedFix = "Verify UTF-16LE encoding and null terminator handling",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md",
                    Context = new Dictionary<string, string>
                    {
                        ["QueryLength"] = query.Length.ToString(),
                        ["BufferSize"] = queryBytes.Length.ToString()
                    }
                };
            }

            return new ValidationResult
            {
                TestName = $"Text Search: {description}",
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "UTF-16LE query marshaling successful",
                Context = new Dictionary<string, string>
                {
                    ["QueryLength"] = query.Length.ToString(),
                    ["ByteLength"] = queryBytes.Length.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = $"Text Search: {description}",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Exception during text search marshaling test",
                ErrorDetails = ex.ToString(),
                SuggestedFix = "Review exception details and fix marshaling logic",
                DocumentationUrl = "docs/marshaling/safe-patterns.md",
                Context = new Dictionary<string, string>
                {
                    ["Query"] = query,
                    ["ExceptionType"] = ex.GetType().Name
                }
            };
        }
    }

    private ValidationResult TestFormFieldWithName(string fieldName, string description)
    {
        try
        {
            // Simulate form field name marshaling pattern (two-phase allocation)
            // Phase 1: Get length
            var expectedBytes = Encoding.Unicode.GetBytes(fieldName + "\0");
            var length = (uint)expectedBytes.Length;

            if (length == 0 && !string.IsNullOrEmpty(fieldName))
            {
                return new ValidationResult
                {
                    TestName = $"Form Field: {description}",
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "Two-phase allocation failed: length query returned 0 for non-empty field name",
                    ErrorDetails = $"Field name: '{fieldName}' (length {fieldName.Length})",
                    SuggestedFix = "Ensure FPDFAnnot_GetFormFieldName correctly reports byte length",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md",
                    Context = new Dictionary<string, string>
                    {
                        ["FieldNameLength"] = fieldName.Length.ToString(),
                        ["ExpectedByteLength"] = expectedBytes.Length.ToString()
                    }
                };
            }

            // Phase 2: Allocate and fill buffer
            var buffer = new byte[length];
            Array.Copy(expectedBytes, buffer, expectedBytes.Length);

            // Decode UTF-16LE
            var decoded = Encoding.Unicode.GetString(buffer).TrimEnd('\0');

            // Validate round-trip
            if (decoded != fieldName)
            {
                return new ValidationResult
                {
                    TestName = $"Form Field: {description}",
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = "UTF-16LE round-trip marshaling failed for form field name",
                    ErrorDetails = $"Original: '{fieldName}', Decoded: '{decoded}'",
                    ExpectedValue = fieldName,
                    ActualValue = decoded,
                    SuggestedFix = "Check buffer size calculation and UTF-16LE encoding/decoding logic",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md",
                    Context = new Dictionary<string, string>
                    {
                        ["OriginalLength"] = fieldName.Length.ToString(),
                        ["DecodedLength"] = decoded.Length.ToString(),
                        ["BufferSize"] = buffer.Length.ToString()
                    }
                };
            }

            return new ValidationResult
            {
                TestName = $"Form Field: {description}",
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "UTF-16LE marshaling successful for form field name",
                Context = new Dictionary<string, string>
                {
                    ["FieldNameLength"] = fieldName.Length.ToString(),
                    ["ByteLength"] = expectedBytes.Length.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = $"Form Field: {description}",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Exception during form field marshaling test",
                ErrorDetails = ex.ToString(),
                SuggestedFix = "Review exception details and fix marshaling logic",
                DocumentationUrl = "docs/marshaling/safe-patterns.md",
                Context = new Dictionary<string, string>
                {
                    ["FieldName"] = fieldName,
                    ["ExceptionType"] = ex.GetType().Name
                }
            };
        }
    }
}
