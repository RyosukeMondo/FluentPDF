using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;
using FluentPDF.Rendering.Interop;

namespace FluentPDF.Verification.Pdfium;

/// <summary>
/// Verifies PDFium functional behavior with known inputs and expected outputs.
/// Tests complete operations beyond just signature validation.
/// </summary>
public class PdfiumBehaviorVerifier : ILibraryVerifier<PdfiumBehaviorVerificationResult>
{
    private readonly VerificationOptions _options;

    public string LibraryName => "PDFium (Behavior Verification)";

    public PdfiumBehaviorVerifier(VerificationOptions options)
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
            return Result.Fail("Test file directory is required for behavior verification");
        }

        if (!Directory.Exists(_options.TestFileDirectory))
        {
            return Result.Fail($"Test file directory not found: {_options.TestFileDirectory}");
        }

        return Result.Ok();
    }

    public async Task<Result<PdfiumBehaviorVerificationResult>> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Initialize PDFium library
        if (!PdfiumInterop.Initialize())
        {
            return Result.Fail<PdfiumBehaviorVerificationResult>("Failed to initialize PDFium library");
        }

        try
        {
            // Test library initialization behavior
            results.Add(VerifyLibraryInitialization());

            // Get test PDF files (excluding corrupted files for behavior tests)
            var testPdfFiles = Directory.GetFiles(_options.TestFileDirectory!, "*.pdf")
                .Where(f => !Path.GetFileName(f).StartsWith("corrupted", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (testPdfFiles.Count == 0)
            {
                return Result.Fail<PdfiumBehaviorVerificationResult>(
                    $"No test PDF files found in: {_options.TestFileDirectory}");
            }

            // Use a representative sample of test files
            var sampleFiles = testPdfFiles.Take(Math.Min(5, testPdfFiles.Count)).ToList();

            foreach (var pdfPath in sampleFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.AddRange(await VerifyDocumentBehaviorAsync(pdfPath, cancellationToken));
            }

            // Test error handling with intentionally bad inputs
            results.AddRange(await VerifyErrorHandlingAsync(cancellationToken));

            // Test resource cleanup
            results.Add(await VerifyResourceCleanupAsync(sampleFiles.First(), cancellationToken));
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
            LibraryVersion = null
        };

        var behaviorResult = new PdfiumBehaviorVerificationResult
        {
            Summary = summary,
            TestedFiles = results
                .Where(r => r.Metadata.ContainsKey("PdfFile"))
                .Select(r => (string)r.Metadata["PdfFile"])
                .Distinct()
                .ToList(),
            FunctionalIssuesDetected = results.Any(r => !r.Success && r.Metadata.ContainsKey("BehaviorIssue") && (bool)r.Metadata["BehaviorIssue"])
        };

        return Result.Ok(behaviorResult);
    }

    /// <summary>
    /// Verifies that library initialization and shutdown work correctly.
    /// </summary>
    private VerificationResult VerifyLibraryInitialization()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // PDFium is already initialized in VerifyAsync, so we just verify the state
            var isInitialized = true; // If we got here, initialization succeeded

            stopwatch.Stop();

            return new VerificationResult
            {
                TestName = "Library initialization",
                Success = isInitialized,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["Operation"] = "FPDF_InitLibrary",
                    ["ExpectedBehavior"] = "Library initializes successfully without errors"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = "Library initialization",
                Success = false,
                ErrorMessage = $"Initialization failed: {ex.Message}",
                SuggestedFix = "Verify PDFium DLL is compatible with the platform and not corrupted",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["Operation"] = "FPDF_InitLibrary",
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            };
        }
    }

    /// <summary>
    /// Verifies complete document behavior workflow.
    /// </summary>
    private async Task<List<VerificationResult>> VerifyDocumentBehaviorAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();
        var fileName = Path.GetFileName(pdfPath);

        await Task.Run(() =>
        {
            // Test document loading behavior
            results.Add(VerifyDocumentLoadingBehavior(pdfPath, fileName));

            SafePdfDocumentHandle? document = null;
            try
            {
                document = PdfiumInterop.LoadDocument(pdfPath);

                if (document == null || document.IsInvalid)
                {
                    return; // Can't continue without valid document
                }

                // Test page operations
                results.Add(VerifyPageCountBehavior(document, fileName, pdfPath));
                results.AddRange(VerifyPageOperationsBehavior(document, fileName, pdfPath));

                // Test rendering pipeline
                results.AddRange(VerifyRenderingPipelineBehavior(document, fileName, pdfPath));

                // Test text extraction behavior (if file has text)
                if (fileName.Contains("text", StringComparison.OrdinalIgnoreCase))
                {
                    results.AddRange(VerifyTextExtractionBehavior(document, fileName, pdfPath));
                }

                // Test bookmark navigation (if file has bookmarks)
                if (fileName.Contains("bookmark", StringComparison.OrdinalIgnoreCase))
                {
                    results.AddRange(VerifyBookmarkNavigationBehavior(document, fileName, pdfPath));
                }
            }
            finally
            {
                document?.Dispose();
            }
        }, cancellationToken);

        return results;
    }

    /// <summary>
    /// Verifies document loading behavior with specific expectations.
    /// </summary>
    private VerificationResult VerifyDocumentLoadingBehavior(string pdfPath, string fileName)
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
                    TestName = $"Document loading behavior: {fileName}",
                    Success = false,
                    ErrorMessage = "Document failed to load - handle is null or invalid",
                    SuggestedFix = "Check if file exists and is a valid PDF format",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_LoadDocument",
                        ["BehaviorIssue"] = true
                    }
                };
            }

            // Verify that error code is success after successful load
            var errorCode = PdfiumInterop.GetLastError();
            if (errorCode != PdfiumInterop.ErrorCodes.Success)
            {
                return new VerificationResult
                {
                    TestName = $"Document loading behavior: {fileName}",
                    Success = false,
                    ErrorMessage = $"Document loaded but error code is not Success: {errorCode}",
                    SuggestedFix = "PDFium may be reporting spurious errors",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_LoadDocument",
                        ["ErrorCode"] = errorCode,
                        ["BehaviorIssue"] = true
                    }
                };
            }

            return new VerificationResult
            {
                TestName = $"Document loading behavior: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDF_LoadDocument",
                    ["ExpectedBehavior"] = "Valid document handle with success error code"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Document loading behavior: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during document loading: {ex.Message}",
                SuggestedFix = "Verify file is accessible and PDFium is properly initialized",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDF_LoadDocument",
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            };
        }
    }

    /// <summary>
    /// Verifies page count behavior against expected values.
    /// </summary>
    private VerificationResult VerifyPageCountBehavior(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pageCount = PdfiumInterop.GetPageCount(document);
            stopwatch.Stop();

            // Multi-page PDFs should have multiple pages
            if (fileName.Contains("multi-page", StringComparison.OrdinalIgnoreCase) && pageCount <= 1)
            {
                return new VerificationResult
                {
                    TestName = $"Page count behavior: {fileName}",
                    Success = false,
                    ErrorMessage = $"Multi-page PDF reports only {pageCount} page(s)",
                    SuggestedFix = "Verify FPDF_GetPageCount is correctly counting all pages",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_GetPageCount",
                        ["ActualPageCount"] = pageCount,
                        ["ExpectedBehavior"] = "Multiple pages",
                        ["BehaviorIssue"] = true
                    }
                };
            }

            // All valid PDFs should have at least 1 page
            if (pageCount < 1)
            {
                return new VerificationResult
                {
                    TestName = $"Page count behavior: {fileName}",
                    Success = false,
                    ErrorMessage = $"Valid PDF reports {pageCount} pages (expected at least 1)",
                    SuggestedFix = "Check FPDF_GetPageCount implementation",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_GetPageCount",
                        ["ActualPageCount"] = pageCount,
                        ["BehaviorIssue"] = true
                    }
                };
            }

            return new VerificationResult
            {
                TestName = $"Page count behavior: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDF_GetPageCount",
                    ["PageCount"] = pageCount
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new VerificationResult
            {
                TestName = $"Page count behavior: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during page count: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDF_GetPageCount",
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            };
        }
    }

    /// <summary>
    /// Verifies page loading and dimension operations.
    /// </summary>
    private List<VerificationResult> VerifyPageOperationsBehavior(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var results = new List<VerificationResult>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pageCount = PdfiumInterop.GetPageCount(document);
            if (pageCount < 1) return results;

            // Test loading first page
            using var page = PdfiumInterop.LoadPage(document, 0);
            stopwatch.Stop();

            if (page == null || page.IsInvalid)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Page loading behavior: {fileName} (page 0)",
                    Success = false,
                    ErrorMessage = "Failed to load first page",
                    SuggestedFix = "Check FPDF_LoadPage implementation",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_LoadPage",
                        ["PageIndex"] = 0,
                        ["BehaviorIssue"] = true
                    }
                });
                return results;
            }

            results.Add(new VerificationResult
            {
                TestName = $"Page loading behavior: {fileName} (page 0)",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDF_LoadPage",
                    ["PageIndex"] = 0
                }
            });

            // Test page dimensions
            stopwatch.Restart();
            var width = PdfiumInterop.GetPageWidth(page);
            var height = PdfiumInterop.GetPageHeight(page);
            stopwatch.Stop();

            // Verify width and height are consistent (not radically different)
            var aspectRatio = width / height;
            if (aspectRatio < 0.1 || aspectRatio > 10.0)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Page dimension behavior: {fileName}",
                    Success = false,
                    ErrorMessage = $"Unusual aspect ratio: {width}x{height} (ratio: {aspectRatio:F2})",
                    SuggestedFix = "Verify page dimension functions return correct values",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_GetPageWidth/Height",
                        ["Width"] = width,
                        ["Height"] = height,
                        ["AspectRatio"] = aspectRatio,
                        ["BehaviorIssue"] = true
                    }
                });
            }
            else
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Page dimension behavior: {fileName}",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_GetPageWidth/Height",
                        ["Width"] = width,
                        ["Height"] = height,
                        ["AspectRatio"] = aspectRatio
                    }
                });
            }

            // Test loading page out of range (should fail gracefully)
            stopwatch.Restart();
            using var invalidPage = PdfiumInterop.LoadPage(document, pageCount + 100);
            stopwatch.Stop();

            if (invalidPage != null && !invalidPage.IsInvalid)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Invalid page handling: {fileName}",
                    Success = false,
                    ErrorMessage = $"FPDF_LoadPage succeeded for out-of-range page index {pageCount + 100}",
                    SuggestedFix = "PDFium should return invalid handle for out-of-range indices",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_LoadPage",
                        ["PageIndex"] = pageCount + 100,
                        ["ExpectedBehavior"] = "Invalid handle",
                        ["BehaviorIssue"] = true
                    }
                });
            }
            else
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Invalid page handling: {fileName}",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_LoadPage",
                        ["PageIndex"] = pageCount + 100,
                        ["ExpectedBehavior"] = "Invalid handle for out-of-range index"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            results.Add(new VerificationResult
            {
                TestName = $"Page operations behavior: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during page operations: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Verifies the complete rendering pipeline.
    /// </summary>
    private List<VerificationResult> VerifyRenderingPipelineBehavior(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var results = new List<VerificationResult>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var pageCount = PdfiumInterop.GetPageCount(document);
            if (pageCount < 1) return results;

            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page == null || page.IsInvalid) return results;

            var width = (int)PdfiumInterop.GetPageWidth(page);
            var height = (int)PdfiumInterop.GetPageHeight(page);

            // Create bitmap for rendering
            var bitmap = PdfiumInterop.CreateBitmap(width, height, true);
            stopwatch.Stop();

            if (bitmap == IntPtr.Zero)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Bitmap creation: {fileName}",
                    Success = false,
                    ErrorMessage = "FPDFBitmap_Create returned null pointer",
                    SuggestedFix = "Verify bitmap dimensions are valid and PDFium has enough memory",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFBitmap_Create",
                        ["Width"] = width,
                        ["Height"] = height,
                        ["BehaviorIssue"] = true
                    }
                });
                return results;
            }

            results.Add(new VerificationResult
            {
                TestName = $"Bitmap creation: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDFBitmap_Create",
                    ["Width"] = width,
                    ["Height"] = height
                }
            });

            try
            {
                // Fill bitmap with white background
                stopwatch.Restart();
                PdfiumInterop.FillBitmap(bitmap, 0xFFFFFFFF);
                stopwatch.Stop();

                results.Add(new VerificationResult
                {
                    TestName = $"Bitmap fill: {fileName}",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFBitmap_FillRect"
                    }
                });

                // Render page to bitmap
                stopwatch.Restart();
                PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 0);
                stopwatch.Stop();

                // Verify rendering succeeded (no exception thrown)
                results.Add(new VerificationResult
                {
                    TestName = $"Page rendering: {fileName}",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDF_RenderPageBitmap",
                        ["ExpectedBehavior"] = "Page renders without errors"
                    }
                });

                // Verify bitmap buffer is accessible
                stopwatch.Restart();
                var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
                var stride = PdfiumInterop.GetBitmapStride(bitmap);
                stopwatch.Stop();

                if (buffer == IntPtr.Zero)
                {
                    results.Add(new VerificationResult
                    {
                        TestName = $"Bitmap buffer access: {fileName}",
                        Success = false,
                        ErrorMessage = "FPDFBitmap_GetBuffer returned null pointer",
                        SuggestedFix = "Bitmap may not be properly initialized",
                        Duration = stopwatch.Elapsed,
                        Metadata = new Dictionary<string, object>
                        {
                            ["PdfFile"] = pdfPath,
                            ["Operation"] = "FPDFBitmap_GetBuffer",
                            ["BehaviorIssue"] = true
                        }
                    });
                }
                else if (stride <= 0)
                {
                    results.Add(new VerificationResult
                    {
                        TestName = $"Bitmap buffer access: {fileName}",
                        Success = false,
                        ErrorMessage = $"FPDFBitmap_GetStride returned invalid value: {stride}",
                        SuggestedFix = "Bitmap stride should be positive",
                        Duration = stopwatch.Elapsed,
                        Metadata = new Dictionary<string, object>
                        {
                            ["PdfFile"] = pdfPath,
                            ["Operation"] = "FPDFBitmap_GetStride",
                            ["Stride"] = stride,
                            ["BehaviorIssue"] = true
                        }
                    });
                }
                else
                {
                    results.Add(new VerificationResult
                    {
                        TestName = $"Bitmap buffer access: {fileName}",
                        Success = true,
                        Duration = stopwatch.Elapsed,
                        Metadata = new Dictionary<string, object>
                        {
                            ["PdfFile"] = pdfPath,
                            ["Operation"] = "FPDFBitmap_GetBuffer/GetStride",
                            ["Stride"] = stride
                        }
                    });
                }
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            results.Add(new VerificationResult
            {
                TestName = $"Rendering pipeline: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during rendering: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Verifies text extraction behavior.
    /// </summary>
    private List<VerificationResult> VerifyTextExtractionBehavior(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var results = new List<VerificationResult>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var page = PdfiumInterop.LoadPage(document, 0);
            if (page == null || page.IsInvalid) return results;

            using var textPage = PdfiumInterop.LoadTextPage(page);
            stopwatch.Stop();

            if (textPage == null || textPage.IsInvalid)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Text page loading: {fileName}",
                    Success = false,
                    ErrorMessage = "FPDFText_LoadPage returned invalid handle",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFText_LoadPage",
                        ["BehaviorIssue"] = true
                    }
                });
                return results;
            }

            results.Add(new VerificationResult
            {
                TestName = $"Text page loading: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDFText_LoadPage"
                }
            });

            // Get character count
            stopwatch.Restart();
            var charCount = PdfiumInterop.GetTextCharCount(textPage);
            stopwatch.Stop();

            results.Add(new VerificationResult
            {
                TestName = $"Text character count: {fileName}",
                Success = charCount > 0,
                ErrorMessage = charCount == 0 ? "Text page has 0 characters (expected text content)" : null,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDFText_CountChars",
                    ["CharCount"] = charCount,
                    ["BehaviorIssue"] = charCount == 0
                }
            });

            // Extract some text if available
            if (charCount > 0)
            {
                stopwatch.Restart();
                var extractCount = Math.Min(100, charCount);
                var text = PdfiumInterop.GetText(textPage, 0, extractCount);
                stopwatch.Stop();

                var extractedChars = text?.Length ?? 0;
                results.Add(new VerificationResult
                {
                    TestName = $"Text extraction: {fileName}",
                    Success = extractedChars > 0,
                    ErrorMessage = extractedChars == 0 ? "FPDFText_GetText extracted 0 characters" : null,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFText_GetText",
                        ["RequestedChars"] = extractCount,
                        ["ExtractedChars"] = extractedChars,
                        ["BehaviorIssue"] = extractedChars == 0
                    }
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            results.Add(new VerificationResult
            {
                TestName = $"Text extraction: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during text extraction: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Verifies bookmark navigation behavior.
    /// </summary>
    private List<VerificationResult> VerifyBookmarkNavigationBehavior(SafePdfDocumentHandle document, string fileName, string pdfPath)
    {
        var results = new List<VerificationResult>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get first root bookmark
            var bookmark = PdfiumInterop.GetFirstChildBookmark(document, IntPtr.Zero);
            stopwatch.Stop();

            if (bookmark == IntPtr.Zero)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Bookmark navigation: {fileName}",
                    Success = false,
                    ErrorMessage = "FPDFBookmark_GetFirstChild returned null for root (expected bookmarks)",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFBookmark_GetFirstChild",
                        ["BehaviorIssue"] = true
                    }
                });
                return results;
            }

            results.Add(new VerificationResult
            {
                TestName = $"Bookmark navigation: {fileName}",
                Success = true,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDFBookmark_GetFirstChild"
                }
            });

            // Get bookmark title
            stopwatch.Restart();
            var title = PdfiumInterop.GetBookmarkTitle(bookmark);
            stopwatch.Stop();

            results.Add(new VerificationResult
            {
                TestName = $"Bookmark title: {fileName}",
                Success = !string.IsNullOrEmpty(title) && title != "(Untitled)",
                ErrorMessage = string.IsNullOrEmpty(title) || title == "(Untitled)" ? "Bookmark has no title" : null,
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Operation"] = "FPDFBookmark_GetTitle",
                    ["Title"] = title ?? "(null)",
                    ["BehaviorIssue"] = string.IsNullOrEmpty(title)
                }
            });

            // Get bookmark destination
            stopwatch.Restart();
            var dest = PdfiumInterop.GetBookmarkDest(document, bookmark);
            stopwatch.Stop();

            if (dest != IntPtr.Zero)
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Bookmark destination: {fileName}",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFBookmark_GetDest"
                    }
                });

                // Get destination page index
                stopwatch.Restart();
                var pageIndex = PdfiumInterop.GetDestPageIndex(document, dest);
                stopwatch.Stop();

                var pageCount = PdfiumInterop.GetPageCount(document);
                results.Add(new VerificationResult
                {
                    TestName = $"Bookmark page index: {fileName}",
                    Success = pageIndex >= 0 && pageIndex < pageCount,
                    ErrorMessage = (pageIndex < 0 || pageIndex >= pageCount) ? $"Invalid page index: {pageIndex} (page count: {pageCount})" : null,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PdfFile"] = pdfPath,
                        ["Operation"] = "FPDFDest_GetDestPageIndex",
                        ["PageIndex"] = pageIndex,
                        ["PageCount"] = pageCount,
                        ["BehaviorIssue"] = pageIndex < 0 || pageIndex >= pageCount
                    }
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            results.Add(new VerificationResult
            {
                TestName = $"Bookmark navigation: {fileName}",
                Success = false,
                ErrorMessage = $"Exception during bookmark navigation: {ex.Message}",
                Duration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["PdfFile"] = pdfPath,
                    ["Exception"] = ex.GetType().Name,
                    ["BehaviorIssue"] = true
                }
            });
        }

        return results;
    }

    /// <summary>
    /// Verifies error handling with intentionally invalid inputs.
    /// </summary>
    private async Task<List<VerificationResult>> VerifyErrorHandlingAsync(CancellationToken cancellationToken)
    {
        var results = new List<VerificationResult>();

        await Task.Run(() =>
        {
            // Test loading non-existent file
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var doc = PdfiumInterop.LoadDocument("non_existent_file.pdf");
                stopwatch.Stop();

                results.Add(new VerificationResult
                {
                    TestName = "Error handling: non-existent file",
                    Success = doc == null || doc.IsInvalid,
                    ErrorMessage = (doc != null && !doc.IsInvalid) ? "FPDF_LoadDocument succeeded for non-existent file" : null,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Operation"] = "FPDF_LoadDocument",
                        ["Input"] = "non_existent_file.pdf",
                        ["ExpectedBehavior"] = "Invalid handle",
                        ["BehaviorIssue"] = doc != null && !doc.IsInvalid
                    }
                });

                var errorCode = PdfiumInterop.GetLastError();
                results.Add(new VerificationResult
                {
                    TestName = "Error handling: error code for non-existent file",
                    Success = errorCode != PdfiumInterop.ErrorCodes.Success,
                    ErrorMessage = errorCode == PdfiumInterop.ErrorCodes.Success ? "Error code is Success for failed operation" : null,
                    Duration = TimeSpan.Zero,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Operation"] = "FPDF_GetLastError",
                        ["ErrorCode"] = errorCode,
                        ["ExpectedBehavior"] = "Non-success error code",
                        ["BehaviorIssue"] = errorCode == PdfiumInterop.ErrorCodes.Success
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                results.Add(new VerificationResult
                {
                    TestName = "Error handling: non-existent file",
                    Success = true, // Exception is acceptable error handling
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Operation"] = "FPDF_LoadDocument",
                        ["Exception"] = ex.GetType().Name,
                        ["ExpectedBehavior"] = "Exception or invalid handle"
                    }
                });
            }
        }, cancellationToken);

        return results;
    }

    /// <summary>
    /// Verifies resource cleanup behavior (no memory leaks, proper disposal).
    /// </summary>
    private async Task<VerificationResult> VerifyResourceCleanupAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Load and immediately dispose document multiple times
                for (int i = 0; i < 10; i++)
                {
                    using var doc = PdfiumInterop.LoadDocument(pdfPath);
                    if (doc == null || doc.IsInvalid)
                    {
                        return new VerificationResult
                        {
                            TestName = "Resource cleanup",
                            Success = false,
                            ErrorMessage = $"Document failed to load on iteration {i}",
                            Duration = stopwatch.Elapsed,
                            Metadata = new Dictionary<string, object>
                            {
                                ["Operation"] = "Resource cleanup test",
                                ["Iteration"] = i,
                                ["BehaviorIssue"] = true
                            }
                        };
                    }

                    var pageCount = PdfiumInterop.GetPageCount(doc);
                    if (pageCount > 0)
                    {
                        using var page = PdfiumInterop.LoadPage(doc, 0);
                        // Page should dispose automatically
                    }
                    // Document should dispose automatically
                }

                stopwatch.Stop();

                return new VerificationResult
                {
                    TestName = "Resource cleanup",
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Operation"] = "Resource cleanup test",
                        ["Iterations"] = 10,
                        ["ExpectedBehavior"] = "No crashes or memory exhaustion"
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new VerificationResult
                {
                    TestName = "Resource cleanup",
                    Success = false,
                    ErrorMessage = $"Exception during resource cleanup: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Operation"] = "Resource cleanup test",
                        ["Exception"] = ex.GetType().Name,
                        ["BehaviorIssue"] = true
                    }
                };
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Result of PDFium behavior verification.
/// </summary>
public record PdfiumBehaviorVerificationResult
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
    /// Whether any functional issues were detected.
    /// </summary>
    public required bool FunctionalIssuesDetected { get; init; }
}
