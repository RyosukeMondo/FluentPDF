using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;
using FluentPDF.Rendering.Interop;

namespace FluentPDF.Verification.Pdfium;

/// <summary>
/// Verifies PDFium function return values contain valid data.
/// Detects garbage values from marshalling errors and validates numeric ranges.
/// </summary>
public class PdfiumReturnTypeVerifier : ILibraryVerifier<PdfiumReturnTypeVerificationResult>
{
    private readonly VerificationOptions _options;

    // Expected ranges for PDF page dimensions (in points, 1/72 inch)
    private const double MinPageDimension = 1.0;      // Minimum valid page dimension
    private const double MaxPageDimension = 14400.0;  // Maximum valid page dimension (200 inches = 14400 points)

    // Known garbage value patterns
    private const double GarbageValueThreshold = 1e-100; // Values close to zero that are likely garbage
    private const double GarbageValueUpperThreshold = 1e100; // Extremely large values that are likely garbage

    public string LibraryName => "PDFium (Return Type Verification)";

    public PdfiumReturnTypeVerifier(VerificationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Result ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.DllPath))
        {
            return Result.Fail("DLL path is required");
        }

        if (!File.Exists(_options.DllPath))
        {
            return Result.Fail($"PDFium DLL not found at: {_options.DllPath}");
        }

        if (string.IsNullOrWhiteSpace(_options.TestFileDirectory))
        {
            return Result.Fail("Test file directory is required for return type verification");
        }

        if (!Directory.Exists(_options.TestFileDirectory))
        {
            return Result.Fail($"Test file directory not found: {_options.TestFileDirectory}");
        }

        return Result.Ok();
    }

    public async Task<Result<PdfiumReturnTypeVerificationResult>> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Initialize PDFium library
        if (!PdfiumInterop.Initialize())
        {
            return Result.Fail<PdfiumReturnTypeVerificationResult>("Failed to initialize PDFium library");
        }

        try
        {
            // Get test PDF files
            var testPdfFiles = Directory.GetFiles(_options.TestFileDirectory!, "*.pdf")
                .Where(f => !Path.GetFileName(f).StartsWith("corrupted", StringComparison.OrdinalIgnoreCase))
                .Take(5) // Limit to 5 files for performance
                .ToList();

            if (testPdfFiles.Count == 0)
            {
                return Result.Fail<PdfiumReturnTypeVerificationResult>(
                    $"No test PDF files found in: {_options.TestFileDirectory}");
            }

            // Verify return values for each test file
            foreach (var pdfPath in testPdfFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileResults = await VerifyPdfFileAsync(pdfPath, cancellationToken);
                results.AddRange(fileResults);
            }

            // Add summary tests
            results.Add(CreateSummaryResult(results));
        }
        finally
        {
            PdfiumInterop.Shutdown();
        }

        stopwatch.Stop();

        var summary = new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            LibraryVersion = null // PDFium doesn't expose version info easily
        };

        var returnTypeResult = new PdfiumReturnTypeVerificationResult
        {
            Summary = summary,
            TestedFiles = results
                .Where(r => r.Metadata.ContainsKey("PdfFile"))
                .Select(r => (string)r.Metadata["PdfFile"])
                .Distinct()
                .ToList(),
            GarbageValuesDetected = results.Any(r => !r.Success && r.Metadata.ContainsKey("IsGarbageValue") && (bool)r.Metadata["IsGarbageValue"])
        };

        return Result.Ok(returnTypeResult);
    }

    /// <summary>
    /// Verifies return values from PDFium functions for a single PDF file.
    /// </summary>
    private async Task<List<VerificationResult>> VerifyPdfFileAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();
        var fileName = Path.GetFileName(pdfPath);

        // Test document loading (pointer return value)
        var loadResult = VerifyDocumentLoading(pdfPath, fileName);
        results.Add(loadResult);

        if (!loadResult.Success)
        {
            // Can't continue testing if document failed to load
            return results;
        }

        // Load document for page tests
        SafePdfDocumentHandle? document = null;
        try
        {
            document = PdfiumInterop.LoadDocument(pdfPath);

            if (document == null || document.IsInvalid)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Document handle validation: {fileName}",
                    Success = false,
                    ErrorMessage = "Document loaded but handle is invalid",
                    Duration = TimeSpan.Zero,
                    Metadata = new Dictionary<string, object> { ["PdfFile"] = pdfPath }
                });
                return results;
            }

            // Test page count (numeric return value)
            results.Add(VerifyPageCount(document, fileName, pdfPath));

            // Test page dimensions for first page
            var pageCountResult = PdfiumInterop.GetPageCount(document);
            if (pageCountResult > 0)
            {
                results.AddRange(await VerifyPageDimensionsAsync(document, 0, fileName, pdfPath, cancellationToken));
            }

            // Test page dimensions for a middle page (if available)
            if (pageCountResult > 5)
            {
                results.AddRange(await VerifyPageDimensionsAsync(document, pageCountResult / 2, fileName, pdfPath, cancellationToken));
            }
        }
        finally
        {
            document?.Dispose();
        }

        return results;
    }

    /// <summary>
    /// Verifies that document loading returns valid pointer.
    /// </summary>
    private VerificationResult VerifyDocumentLoading(string pdfPath, string fileName)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var document = PdfiumInterop.LoadDocument(pdfPath);
            stopwatch.Stop();

            if (document == null || document.IsInvalid)
            {
                return new VerificationResult
                {
                    TestName = $"Document loading: {fileName}",
                    Success = false,
                    ErrorMessage = "FPDF_LoadDocument returned null or invalid handle",
                    SuggestedFix = "Check if PDFium DLL is correctly loaded and file path is valid",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["ReturnType"] = "IntPtr"
                    }
                };
            }

            return new VerificationResult
            {
                TestName = $"Document loading: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["ReturnType"] = "IntPtr"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Document loading: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during document loading: {ex.Message}",
                SuggestedFix = "Verify PDFium DLL is compatible and not corrupted",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["ReturnType"] = "IntPtr",
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// Verifies that page count returns a valid numeric value.
    /// </summary>
    private VerificationResult VerifyPageCount(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pageCount = PdfiumInterop.GetPageCount(document);
            stopwatch.Stop();

            // Validate page count is within reasonable range
            if (pageCount < 0)
            {
                return new VerificationResult
                {
                    TestName = $"Page count validation: {fileName}",
                    Success = false,
                    ErrorMessage = $"FPDF_GetPageCount returned negative value: {pageCount}",
                    SuggestedFix = "Check P/Invoke signature for FPDF_GetPageCount - return type may be incorrect",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["ReturnValue"] = pageCount,
                        ["ReturnType"] = "int",
                        ["IsGarbageValue"] = true
                    }
                };
            }

            if (pageCount == 0)
            {
                return new VerificationResult
                {
                    TestName = $"Page count validation: {fileName}",
                    Success = false,
                    ErrorMessage = "FPDF_GetPageCount returned 0 - document may be corrupted or invalid",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["ReturnValue"] = pageCount,
                        ["ReturnType"] = "int"
                    }
                };
            }

            if (pageCount > 10000)
            {
                return new VerificationResult
                {
                    TestName = $"Page count validation: {fileName}",
                    Success = false,
                    ErrorMessage = $"FPDF_GetPageCount returned suspiciously large value: {pageCount}",
                    SuggestedFix = "Verify return type marshalling for FPDF_GetPageCount",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["ReturnValue"] = pageCount,
                        ["ReturnType"] = "int",
                        ["IsGarbageValue"] = true
                    }
                };
            }

            return new VerificationResult
            {
                TestName = $"Page count validation: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["ReturnValue"] = pageCount,
                    ["ReturnType"] = "int"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Page count validation: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during page count retrieval: {ex.Message}",
                SuggestedFix = "Check P/Invoke signature and calling convention",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["ReturnType"] = "int",
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// Verifies page dimensions (width and height) for a specific page.
    /// </summary>
    private async Task<List<VerificationResult>> VerifyPageDimensionsAsync(
        SafePdfDocumentHandle document,
        int pageIndex,
        string fileName,
        string pdfPath,
        CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

        await Task.Run(() =>
        {
            SafePdfPageHandle? page = null;
            try
            {
                page = PdfiumInterop.LoadPage(document, pageIndex);

                if (page == null || page.IsInvalid)
                {
                    results.Add(new VerificationResult
                    {
                        TestName = $"Page loading: {fileName} (page {pageIndex})",
                        Success = false,
                        ErrorMessage = "FPDF_LoadPage returned null or invalid handle",
                        Duration = TimeSpan.Zero,
                        Metadata = new Dictionary<string, object>
                        {
                            ["PdfFile"] = pdfPath,
                            ["PageIndex"] = pageIndex,
                            ["ReturnType"] = "IntPtr"
                        }
                    });
                    return;
                }

                // Test page width
                results.Add(VerifyPageWidth(page, pageIndex, fileName, pdfPath));

                // Test page height
                results.Add(VerifyPageHeight(page, pageIndex, fileName, pdfPath));
            }
            finally
            {
                page?.Dispose();
            }
        }, cancellationToken);

        return results;
    }

    /// <summary>
    /// Verifies page width returns a valid value.
    /// </summary>
    private VerificationResult VerifyPageWidth(SafePdfPageHandle page, int pageIndex, string fileName, string pdfPath)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var width = PdfiumInterop.GetPageWidth(page);
            stopwatch.Stop();

            return ValidateNumericDimension(
                width,
                $"Page width: {fileName} (page {pageIndex})",
                "FPDF_GetPageWidth",
                pdfPath,
                pageIndex,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Page width: {fileName} (page {pageIndex})",
                Success = false,
                ErrorMessage = $"Exception during width retrieval: {ex.Message}",
                SuggestedFix = "Check P/Invoke signature for FPDF_GetPageWidth",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["PageIndex"] = pageIndex,
                    ["ReturnType"] = "double",
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// Verifies page height returns a valid value.
    /// </summary>
    private VerificationResult VerifyPageHeight(SafePdfPageHandle page, int pageIndex, string fileName, string pdfPath)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var height = PdfiumInterop.GetPageHeight(page);
            stopwatch.Stop();

            return ValidateNumericDimension(
                height,
                $"Page height: {fileName} (page {pageIndex})",
                "FPDF_GetPageHeight",
                pdfPath,
                pageIndex,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Page height: {fileName} (page {pageIndex})",
                Success = false,
                ErrorMessage = $"Exception during height retrieval: {ex.Message}",
                SuggestedFix = "Check P/Invoke signature for FPDF_GetPageHeight",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["PageIndex"] = pageIndex,
                    ["ReturnType"] = "double",
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// Validates a numeric dimension value for common error patterns.
    /// </summary>
    private VerificationResult ValidateNumericDimension(
        double value,
        string testName,
        string functionName,
        string pdfPath,
        int pageIndex,
        TimeSpan duration)
    {
        var metadata = new Dictionary<string, object>
        {
            ["PdfFile"] = pdfPath,
            ["PageIndex"] = pageIndex,
            ["ReturnValue"] = value,
            ["ReturnType"] = "double",
            ["FunctionName"] = functionName
        };

        // Check for NaN or Infinity
        if (double.IsNaN(value))
        {
            return new VerificationResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{functionName} returned NaN (Not a Number)",
                SuggestedFix = $"Check P/Invoke signature for {functionName} - return type or marshalling may be incorrect",
                Duration = duration,
                Metadata = metadata.Concat(new[] { new KeyValuePair<string, object>("IsGarbageValue", true) })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        if (double.IsInfinity(value))
        {
            return new VerificationResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{functionName} returned Infinity",
                SuggestedFix = $"Check P/Invoke signature for {functionName} - return type or marshalling may be incorrect",
                Duration = duration,
                Metadata = metadata.Concat(new[] { new KeyValuePair<string, object>("IsGarbageValue", true) })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        // Check for garbage values (extremely small or large)
        if (Math.Abs(value) < GarbageValueThreshold && value != 0)
        {
            return new VerificationResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{functionName} returned garbage value: {value:E3} (too close to zero)",
                SuggestedFix = $"Verify {functionName} signature - may be using 'float' instead of 'double', or wrong function name. Try using FPDF_GetPageWidth instead of FPDF_GetPageWidthF.",
                Duration = duration,
                Metadata = metadata.Concat(new[] { new KeyValuePair<string, object>("IsGarbageValue", true) })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        if (Math.Abs(value) > GarbageValueUpperThreshold)
        {
            return new VerificationResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{functionName} returned garbage value: {value:E3} (unreasonably large)",
                SuggestedFix = $"Check P/Invoke signature for {functionName} - marshalling may be incorrect",
                Duration = duration,
                Metadata = metadata.Concat(new[] { new KeyValuePair<string, object>("IsGarbageValue", true) })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        // Check for invalid range (outside expected PDF page dimensions)
        if (value < MinPageDimension || value > MaxPageDimension)
        {
            return new VerificationResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{functionName} returned value outside valid range: {value} points (expected {MinPageDimension}-{MaxPageDimension})",
                SuggestedFix = $"Verify {functionName} return type and marshalling - value may be in wrong units or corrupted",
                Duration = duration,
                Metadata = metadata.Concat(new[] { new KeyValuePair<string, object>("IsGarbageValue", value < 0.1) })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        // All checks passed
        return new VerificationResult
        {
            TestName = testName,
            Success = true,
            Duration = duration,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates a summary result indicating overall return type validation status.
    /// </summary>
    private VerificationResult CreateSummaryResult(List<VerificationResult> results)
    {
        var garbageValues = results.Count(r => !r.Success && r.Metadata.ContainsKey("IsGarbageValue") && (bool)r.Metadata["IsGarbageValue"]);
        var totalTests = results.Count;
        var passed = results.Count(r => r.Success);

        return new VerificationResult
        {
            TestName = "Overall Return Type Validation",
            Success = garbageValues == 0 && passed == totalTests,
            ErrorMessage = garbageValues > 0
                ? $"Detected {garbageValues} garbage value(s) indicating marshalling errors"
                : (passed < totalTests ? $"{totalTests - passed} validation(s) failed" : null),
            Duration = TimeSpan.Zero,
            Metadata = new Dictionary<string, object>
            {
                ["TotalTests"] = totalTests,
                ["PassedTests"] = passed,
                ["GarbageValuesDetected"] = garbageValues
            }
        };
    }
}

/// <summary>
/// Result of PDFium return type verification.
/// </summary>
public record PdfiumReturnTypeVerificationResult
{
    /// <summary>
    /// Overall verification summary.
    /// </summary>
    public required VerificationSummary Summary { get; init; }

    /// <summary>
    /// List of PDF files tested.
    /// </summary>
    public required IReadOnlyList<string> TestedFiles { get; init; }

    /// <summary>
    /// Whether any garbage values were detected.
    /// </summary>
    public required bool GarbageValuesDetected { get; init; }
}
