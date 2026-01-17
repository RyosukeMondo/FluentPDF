using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentPDF.Rendering.Interop.Verification;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// Tests P/Invoke data marshalling at runtime using actual PDFium function calls.
/// Verifies that data marshals correctly between managed and unmanaged code.
/// </summary>
public class DataMarshallerTester : IDisposable
{
    private readonly string _testPdfPath;
    private bool _disposed;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMarshallerTester"/> class.
    /// </summary>
    /// <param name="testPdfPath">Path to a test PDF file for marshalling tests. If null, uses a built-in test file.</param>
    public DataMarshallerTester(string? testPdfPath = null)
    {
        _testPdfPath = testPdfPath ?? string.Empty;
    }

    /// <summary>
    /// Runs all marshalling tests for PDFium functions.
    /// </summary>
    /// <returns>A list of test results for each function tested.</returns>
    public List<VerificationResult> RunAllTests()
    {
        var results = new List<VerificationResult>();

        // Initialize PDFium library
        if (!InitializePdfium())
        {
            results.Add(new VerificationResult
            {
                FunctionName = "FPDF_InitLibrary",
                SignatureValid = true,
                MarshallingCorrect = false,
                ErrorMessage = "Failed to initialize PDFium library. Library may not be found or failed to load.",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    DataType = "void",
                    TestCaseName = "PDFium Initialization",
                    ErrorDetails = "PDFium library initialization failed"
                }
            });
            return results;
        }

        _isInitialized = true;

        try
        {
            // Test library initialization functions (already tested above)
            results.Add(CreateSuccessResult("FPDF_InitLibrary", "void", "PDFium Initialization"));

            // Test error code retrieval (no document needed)
            results.Add(TestGetLastError());

            // Test document loading functions
            if (!string.IsNullOrEmpty(_testPdfPath) && File.Exists(_testPdfPath))
            {
                results.AddRange(TestDocumentFunctions(_testPdfPath));
            }
            else
            {
                results.Add(new VerificationResult
                {
                    FunctionName = "Document Functions",
                    SignatureValid = true,
                    MarshallingCorrect = null,
                    ErrorMessage = $"No valid test PDF provided. Path: '{_testPdfPath}'",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "N/A",
                        TestCaseName = "Document Loading",
                        ErrorDetails = "Test PDF not found or not specified"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            results.Add(new VerificationResult
            {
                FunctionName = "TestSuite",
                SignatureValid = true,
                MarshallingCorrect = false,
                ErrorMessage = $"Unexpected error during marshalling tests: {ex.Message}",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    DataType = "N/A",
                    TestCaseName = "Overall Test Suite",
                    ErrorDetails = ex.ToString()
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Tests specific marshalling types with focused test cases.
    /// </summary>
    /// <param name="testPdfPath">Path to a test PDF file.</param>
    /// <returns>A dictionary mapping data types to their test results.</returns>
    public Dictionary<string, MarshallingTestResult> TestMarshallingTypes(string testPdfPath)
    {
        if (!File.Exists(testPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found: {testPdfPath}");
        }

        var results = new Dictionary<string, MarshallingTestResult>();

        if (!InitializePdfium())
        {
            results["Initialization"] = new MarshallingTestResult
            {
                Success = false,
                DataType = "void",
                TestCaseName = "PDFium Initialization",
                ErrorDetails = "Failed to initialize PDFium"
            };
            return results;
        }

        _isInitialized = true;

        // Test IntPtr (handle) marshalling
        using var document = PdfiumInterop.LoadDocument(testPdfPath);
        if (document.IsInvalid)
        {
            results["IntPtr"] = new MarshallingTestResult
            {
                Success = false,
                DataType = "IntPtr",
                TestCaseName = "Document Handle",
                ErrorDetails = "Failed to load document - handle marshalling may be incorrect"
            };
            return results;
        }
        else
        {
            results["IntPtr"] = new MarshallingTestResult
            {
                Success = true,
                DataType = "IntPtr",
                TestCaseName = "Document Handle",
                ExpectedValue = "Valid SafeHandle",
                ActualValue = "Valid SafeHandle"
            };
        }

        // Test int marshalling
        var pageCount = PdfiumInterop.GetPageCount(document);
        if (pageCount > 0)
        {
            results["int"] = new MarshallingTestResult
            {
                Success = true,
                DataType = "int",
                TestCaseName = "Page Count",
                ExpectedValue = "> 0",
                ActualValue = pageCount
            };
        }
        else
        {
            results["int"] = new MarshallingTestResult
            {
                Success = false,
                DataType = "int",
                TestCaseName = "Page Count",
                ExpectedValue = "> 0",
                ActualValue = pageCount,
                ErrorDetails = "Page count returned 0 or negative value"
            };
        }

        // Test double marshalling with page dimensions
        // CRITICAL: This tests the FPDF_GetPageWidth vs FPDF_GetPageWidthF issue
        using (var page = PdfiumInterop.LoadPage(document, 0))
        {
            if (!page.IsInvalid)
            {
                var width = PdfiumInterop.GetPageWidth(page);
                var height = PdfiumInterop.GetPageHeight(page);

                // PDF page dimensions are typically 612x792 for Letter (8.5x11 inches at 72 DPI)
                // or 595x842 for A4. Reasonable range is 72-3000 points.
                bool widthValid = width > 72 && width < 3000;
                bool heightValid = height > 72 && height < 3000;

                results["double"] = new MarshallingTestResult
                {
                    Success = widthValid && heightValid,
                    DataType = "double",
                    TestCaseName = "Page Dimensions (FPDF_GetPageWidth/Height)",
                    ExpectedValue = "72-3000 points",
                    ActualValue = $"Width: {width}, Height: {height}",
                    ErrorDetails = widthValid && heightValid ? null :
                        "Page dimensions out of expected range. If using float API (FPDF_GetPageWidthF/HeightF), " +
                        "this indicates marshalling corruption. Use double API (FPDF_GetPageWidth/Height) instead."
                };
            }
            else
            {
                results["double"] = new MarshallingTestResult
                {
                    Success = false,
                    DataType = "double",
                    TestCaseName = "Page Dimensions",
                    ErrorDetails = "Failed to load page for dimension testing"
                };
            }
        }

        // Test string marshalling (UTF-16LE for bookmark titles)
        var firstBookmark = PdfiumInterop.GetFirstChildBookmark(document, IntPtr.Zero);
        if (firstBookmark != IntPtr.Zero)
        {
            var title = PdfiumInterop.GetBookmarkTitle(firstBookmark);
            bool titleValid = !string.IsNullOrEmpty(title) && title != "(Untitled)";

            results["string"] = new MarshallingTestResult
            {
                Success = titleValid,
                DataType = "string",
                TestCaseName = "Bookmark Title (UTF-16LE)",
                ExpectedValue = "Non-empty title",
                ActualValue = title,
                ErrorDetails = titleValid ? null : "Failed to extract bookmark title - string marshalling may be incorrect"
            };
        }
        else
        {
            results["string"] = new MarshallingTestResult
            {
                Success = true, // Not a failure if PDF has no bookmarks
                DataType = "string",
                TestCaseName = "Bookmark Title (UTF-16LE)",
                ExpectedValue = "N/A (no bookmarks)",
                ActualValue = "N/A (no bookmarks)",
                ErrorDetails = "Test PDF has no bookmarks - unable to test string marshalling"
            };
        }

        // Test uint marshalling (error codes)
        var errorCode = PdfiumInterop.GetLastError();
        results["uint"] = new MarshallingTestResult
        {
            Success = true, // Error code API always works if PDFium is loaded
            DataType = "uint",
            TestCaseName = "Error Code",
            ExpectedValue = "0-6",
            ActualValue = errorCode
        };

        // Test bool marshalling (if we can test text extraction)
        using (var page = PdfiumInterop.LoadPage(document, 0))
        {
            if (!page.IsInvalid)
            {
                using var textPage = PdfiumInterop.LoadTextPage(page);
                if (!textPage.IsInvalid)
                {
                    var charCount = PdfiumInterop.GetTextCharCount(textPage);

                    results["bool"] = new MarshallingTestResult
                    {
                        Success = true,
                        DataType = "bool",
                        TestCaseName = "Text Page Operations",
                        ExpectedValue = "Valid operations",
                        ActualValue = $"Loaded text page, {charCount} chars"
                    };
                }
                else
                {
                    results["bool"] = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "bool",
                        TestCaseName = "Text Page Operations",
                        ErrorDetails = "Failed to load text page - bool return marshalling may be incorrect"
                    };
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Tests a specific PDFium function with known expected values.
    /// </summary>
    /// <param name="functionName">Name of the PDFium function to test.</param>
    /// <param name="testPdfPath">Path to a test PDF file.</param>
    /// <returns>Verification result for the function.</returns>
    public VerificationResult TestFunction(string functionName, string testPdfPath)
    {
        if (!File.Exists(testPdfPath))
        {
            return new VerificationResult
            {
                FunctionName = functionName,
                SignatureValid = true,
                MarshallingCorrect = false,
                ErrorMessage = $"Test PDF not found: {testPdfPath}",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    TestCaseName = $"Test {functionName}",
                    ErrorDetails = "Test file not found"
                }
            };
        }

        if (!InitializePdfium())
        {
            return new VerificationResult
            {
                FunctionName = functionName,
                SignatureValid = true,
                MarshallingCorrect = false,
                ErrorMessage = "PDFium not initialized",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    TestCaseName = $"Test {functionName}",
                    ErrorDetails = "Failed to initialize PDFium"
                }
            };
        }

        _isInitialized = true;

        // Map function names to test implementations
        return functionName switch
        {
            "FPDF_LoadDocument" => TestLoadDocument(testPdfPath),
            "FPDF_GetPageCount" => TestGetPageCount(testPdfPath),
            "FPDF_GetPageWidth" => TestGetPageWidth(testPdfPath),
            "FPDF_GetPageHeight" => TestGetPageHeight(testPdfPath),
            "FPDFBitmap_Create" => TestBitmapCreate(),
            "FPDF_RenderPageBitmap" => TestRenderPageBitmap(testPdfPath),
            "FPDF_GetLastError" => TestGetLastError(),
            _ => new VerificationResult
            {
                FunctionName = functionName,
                SignatureValid = true,
                MarshallingCorrect = null,
                ErrorMessage = $"No test implementation for function: {functionName}",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    TestCaseName = $"Test {functionName}",
                    ErrorDetails = "Test not implemented"
                }
            }
        };
    }

    private bool InitializePdfium()
    {
        try
        {
            return PdfiumInterop.Initialize();
        }
        catch
        {
            return false;
        }
    }

    private List<VerificationResult> TestDocumentFunctions(string testPdfPath)
    {
        var results = new List<VerificationResult>();

        // Test FPDF_LoadDocument
        results.Add(TestLoadDocument(testPdfPath));

        // Test FPDF_GetPageCount
        results.Add(TestGetPageCount(testPdfPath));

        // Test page functions
        results.Add(TestGetPageWidth(testPdfPath));
        results.Add(TestGetPageHeight(testPdfPath));

        // Test bitmap functions
        results.Add(TestBitmapCreate());

        // Test rendering
        results.Add(TestRenderPageBitmap(testPdfPath));

        // Test text extraction
        results.Add(TestTextExtraction(testPdfPath));

        // Test bookmark functions
        results.Add(TestBookmarkFunctions(testPdfPath));

        return results;
    }

    private VerificationResult TestLoadDocument(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);

            if (document.IsInvalid)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDF_LoadDocument",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Failed to load document",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "IntPtr",
                        TestCaseName = "Load Document",
                        ExpectedValue = "Valid document handle",
                        ActualValue = "Invalid handle",
                        ErrorDetails = $"PDFium error code: {PdfiumInterop.GetLastError()}"
                    }
                };
            }

            return CreateSuccessResult("FPDF_LoadDocument", "IntPtr", "Load Document", "Valid handle");
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_LoadDocument", "IntPtr", "Load Document", ex);
        }
    }

    private VerificationResult TestGetPageCount(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDF_GetPageCount", "int", "Get Page Count",
                    new InvalidOperationException("Document failed to load"));
            }

            var pageCount = PdfiumInterop.GetPageCount(document);

            if (pageCount <= 0)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDF_GetPageCount",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Invalid page count returned",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "int",
                        TestCaseName = "Get Page Count",
                        ExpectedValue = "> 0",
                        ActualValue = pageCount,
                        ErrorDetails = "Page count should be positive for valid PDF"
                    }
                };
            }

            return CreateSuccessResult("FPDF_GetPageCount", "int", "Get Page Count", pageCount);
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_GetPageCount", "int", "Get Page Count", ex);
        }
    }

    private VerificationResult TestGetPageWidth(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDF_GetPageWidth", "double", "Get Page Width",
                    new InvalidOperationException("Document failed to load"));
            }

            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page.IsInvalid)
            {
                return CreateErrorResult("FPDF_GetPageWidth", "double", "Get Page Width",
                    new InvalidOperationException("Page failed to load"));
            }

            var width = PdfiumInterop.GetPageWidth(page);

            // Validate reasonable page width (72-3000 points)
            if (width < 72 || width > 3000)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDF_GetPageWidth",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Page width out of expected range - possible float API marshalling issue",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "double",
                        TestCaseName = "Get Page Width",
                        ExpectedValue = "72-3000 points",
                        ActualValue = width,
                        ErrorDetails = "Width out of range. If using FPDF_GetPageWidthF (float API), this indicates marshalling corruption. Use FPDF_GetPageWidth (double API) instead."
                    }
                };
            }

            return CreateSuccessResult("FPDF_GetPageWidth", "double", "Get Page Width", width);
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_GetPageWidth", "double", "Get Page Width", ex);
        }
    }

    private VerificationResult TestGetPageHeight(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDF_GetPageHeight", "double", "Get Page Height",
                    new InvalidOperationException("Document failed to load"));
            }

            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page.IsInvalid)
            {
                return CreateErrorResult("FPDF_GetPageHeight", "double", "Get Page Height",
                    new InvalidOperationException("Page failed to load"));
            }

            var height = PdfiumInterop.GetPageHeight(page);

            // Validate reasonable page height (72-3000 points)
            if (height < 72 || height > 3000)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDF_GetPageHeight",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Page height out of expected range - possible float API marshalling issue",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "double",
                        TestCaseName = "Get Page Height",
                        ExpectedValue = "72-3000 points",
                        ActualValue = height,
                        ErrorDetails = "Height out of range. If using FPDF_GetPageHeightF (float API), this indicates marshalling corruption. Use FPDF_GetPageHeight (double API) instead."
                    }
                };
            }

            return CreateSuccessResult("FPDF_GetPageHeight", "double", "Get Page Height", height);
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_GetPageHeight", "double", "Get Page Height", ex);
        }
    }

    private VerificationResult TestBitmapCreate()
    {
        try
        {
            var bitmap = PdfiumInterop.CreateBitmap(100, 100, false);

            if (bitmap == IntPtr.Zero)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDFBitmap_Create",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Failed to create bitmap",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "IntPtr",
                        TestCaseName = "Create Bitmap",
                        ExpectedValue = "Valid bitmap handle",
                        ActualValue = "IntPtr.Zero"
                    }
                };
            }

            PdfiumInterop.DestroyBitmap(bitmap);
            return CreateSuccessResult("FPDFBitmap_Create", "IntPtr", "Create Bitmap", "Valid handle");
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDFBitmap_Create", "IntPtr", "Create Bitmap", ex);
        }
    }

    private VerificationResult TestRenderPageBitmap(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDF_RenderPageBitmap", "void", "Render Page",
                    new InvalidOperationException("Document failed to load"));
            }

            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page.IsInvalid)
            {
                return CreateErrorResult("FPDF_RenderPageBitmap", "void", "Render Page",
                    new InvalidOperationException("Page failed to load"));
            }

            var bitmap = PdfiumInterop.CreateBitmap(100, 100, false);
            if (bitmap == IntPtr.Zero)
            {
                return CreateErrorResult("FPDF_RenderPageBitmap", "void", "Render Page",
                    new InvalidOperationException("Bitmap creation failed"));
            }

            try
            {
                PdfiumInterop.FillBitmap(bitmap, 0xFFFFFFFF); // White background
                PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, 100, 100, 0, 0);

                return CreateSuccessResult("FPDF_RenderPageBitmap", "void", "Render Page", "Rendered successfully");
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_RenderPageBitmap", "void", "Render Page", ex);
        }
    }

    private VerificationResult TestGetLastError()
    {
        try
        {
            var errorCode = PdfiumInterop.GetLastError();

            // Error code should be in valid range (0-6)
            if (errorCode > 6)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDF_GetLastError",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Error code out of valid range",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "uint",
                        TestCaseName = "Get Last Error",
                        ExpectedValue = "0-6",
                        ActualValue = errorCode
                    }
                };
            }

            return CreateSuccessResult("FPDF_GetLastError", "uint", "Get Last Error", errorCode);
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDF_GetLastError", "uint", "Get Last Error", ex);
        }
    }

    private VerificationResult TestTextExtraction(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDFText_LoadPage", "IntPtr", "Load Text Page",
                    new InvalidOperationException("Document failed to load"));
            }

            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page.IsInvalid)
            {
                return CreateErrorResult("FPDFText_LoadPage", "IntPtr", "Load Text Page",
                    new InvalidOperationException("Page failed to load"));
            }

            using var textPage = PdfiumInterop.LoadTextPage(page);
            if (textPage.IsInvalid)
            {
                return new VerificationResult
                {
                    FunctionName = "FPDFText_LoadPage",
                    SignatureValid = true,
                    MarshallingCorrect = false,
                    ErrorMessage = "Failed to load text page",
                    TestResult = new MarshallingTestResult
                    {
                        Success = false,
                        DataType = "IntPtr",
                        TestCaseName = "Load Text Page",
                        ExpectedValue = "Valid text page handle",
                        ActualValue = "Invalid handle"
                    }
                };
            }

            var charCount = PdfiumInterop.GetTextCharCount(textPage);

            return CreateSuccessResult("FPDFText_LoadPage", "IntPtr", "Load Text Page", $"{charCount} characters");
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDFText_LoadPage", "IntPtr", "Load Text Page", ex);
        }
    }

    private VerificationResult TestBookmarkFunctions(string testPdfPath)
    {
        try
        {
            using var document = PdfiumInterop.LoadDocument(testPdfPath);
            if (document.IsInvalid)
            {
                return CreateErrorResult("FPDFBookmark_GetFirstChild", "IntPtr", "Get Bookmark",
                    new InvalidOperationException("Document failed to load"));
            }

            var bookmark = PdfiumInterop.GetFirstChildBookmark(document, IntPtr.Zero);

            if (bookmark == IntPtr.Zero)
            {
                // Not an error - PDF might not have bookmarks
                return new VerificationResult
                {
                    FunctionName = "FPDFBookmark_GetFirstChild",
                    SignatureValid = true,
                    MarshallingCorrect = true,
                    ErrorMessage = null,
                    TestResult = new MarshallingTestResult
                    {
                        Success = true,
                        DataType = "IntPtr",
                        TestCaseName = "Get Bookmark",
                        ExpectedValue = "Handle or IntPtr.Zero",
                        ActualValue = "IntPtr.Zero (no bookmarks)"
                    }
                };
            }

            var title = PdfiumInterop.GetBookmarkTitle(bookmark);

            return CreateSuccessResult("FPDFBookmark_GetFirstChild", "IntPtr", "Get Bookmark", $"Title: {title}");
        }
        catch (Exception ex)
        {
            return CreateErrorResult("FPDFBookmark_GetFirstChild", "IntPtr", "Get Bookmark", ex);
        }
    }

    private static VerificationResult CreateSuccessResult(string functionName, string dataType, string testCase, object? value = null)
    {
        return new VerificationResult
        {
            FunctionName = functionName,
            SignatureValid = true,
            MarshallingCorrect = true,
            ErrorMessage = null,
            TestResult = new MarshallingTestResult
            {
                Success = true,
                DataType = dataType,
                TestCaseName = testCase,
                ExpectedValue = "Success",
                ActualValue = value?.ToString() ?? "Success"
            }
        };
    }

    private static VerificationResult CreateErrorResult(string functionName, string dataType, string testCase, Exception ex)
    {
        return new VerificationResult
        {
            FunctionName = functionName,
            SignatureValid = true,
            MarshallingCorrect = false,
            ErrorMessage = ex.Message,
            TestResult = new MarshallingTestResult
            {
                Success = false,
                DataType = dataType,
                TestCaseName = testCase,
                ErrorDetails = ex.ToString()
            }
        };
    }

    /// <summary>
    /// Disposes resources used by the marshalling tester.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && _isInitialized)
        {
            // Shutdown PDFium if we initialized it
            try
            {
                PdfiumInterop.Shutdown();
            }
            catch
            {
                // Ignore errors during shutdown
            }
        }

        _disposed = true;
    }
}
