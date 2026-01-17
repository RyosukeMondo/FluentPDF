using FluentAssertions;
using FluentPDF.Verification.Core;
using FluentPDF.Verification.Pdfium;
using FluentResults;
using NSubstitute;
using Xunit;

namespace FluentPDF.Verification.Pdfium.Tests;

/// <summary>
/// Tests for PdfiumSignatureVerifier.
/// Validates signature matching logic, error detection, and suggested fixes.
/// </summary>
public class PdfiumSignatureVerifierTests
{
    private readonly IDllAnalyzer _mockDllAnalyzer;
    private readonly VerificationOptions _options;

    public PdfiumSignatureVerifierTests()
    {
        _mockDllAnalyzer = Substitute.For<IDllAnalyzer>();
        _options = new VerificationOptions
        {
            DllPath = @"C:\test\pdfium.dll",
            ParallelExecution = false
        };
    }

    [Fact]
    public async Task VerifyAsync_WithValidDll_ReturnsSuccessResult()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "1.0.0",
            Architecture = "x64",
            ExportCount = 10,
            FileSizeBytes = 1000000
        };

        var exports = new List<string>
        {
            "FPDF_InitLibrary",
            "FPDF_DestroyLibrary",
            "FPDF_LoadDocument",
            "FPDF_CloseDocument",
            "FPDF_GetPageCount",
            "FPDF_LoadPage",
            "FPDF_ClosePage",
            "FPDF_GetPageWidth",
            "FPDF_GetPageHeight",
            "FPDFBitmap_Create"
        };

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DllInfo.Should().Be(dllInfo);
        result.Value.Summary.TotalTests.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task VerifyAsync_WithMissingDll_ReturnsFailure()
    {
        // Arrange
        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<DllInfo>("DLL not found"));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("DLL analysis failed");
    }

    [Fact]
    public async Task VerifyAsync_DetectsMissingExports()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "1.0.0",
            Architecture = "x64",
            ExportCount = 5,
            FileSizeBytes = 1000000
        };

        // Only include a subset of the functions that PdfiumInterop declares
        var exports = new List<string>
        {
            "FPDF_InitLibrary",
            "FPDF_DestroyLibrary"
        };

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MissingExports.Should().NotBeEmpty();
        result.Value.Summary.FailedTests.Should().BeGreaterThan(0);

        // Should have failures for missing exports
        var missingExportResults = result.Value.Summary.Results
            .Where(r => r.TestName.StartsWith("Export exists:") && !r.Success);
        missingExportResults.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_ValidatesAllPInvokeDeclarations()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "1.0.0",
            Architecture = "x64",
            ExportCount = 100,
            FileSizeBytes = 5000000
        };

        // Create a comprehensive list of PDFium exports
        var exports = GetAllPdfiumExports();

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SignatureMatches.Should().NotBeEmpty();

        // Each P/Invoke declaration should be validated
        var signatureTests = result.Value.Summary.Results
            .Where(r => r.TestName.StartsWith("Signature:"));
        signatureTests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_IncludesDetailedMetadata()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "6000",
            Architecture = "x64",
            ExportCount = 50,
            FileSizeBytes = 3000000
        };

        var exports = GetAllPdfiumExports();

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.LibraryVersion.Should().Be("6000");

        // Verify metadata is included in test results
        var signatureResults = result.Value.Summary.Results
            .Where(r => r.TestName.StartsWith("Signature:"));

        foreach (var testResult in signatureResults)
        {
            testResult.Metadata.Should().ContainKey("DeclaredSignature");
            testResult.Metadata.Should().ContainKey("ActualSignature");
            testResult.Metadata.Should().ContainKey("FunctionName");
        }
    }

    [Fact]
    public async Task VerifyAsync_GeneratesSuggestedFixes_ForFailures()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "1.0.0",
            Architecture = "x64",
            ExportCount = 2,
            FileSizeBytes = 1000000
        };

        // Missing some expected exports to trigger failures
        var exports = new List<string>
        {
            "FPDF_InitLibrary"
        };

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        var failedTests = result.Value.Summary.Results.Where(r => !r.Success);
        failedTests.Should().NotBeEmpty();

        foreach (var failedTest in failedTests)
        {
            // Each failure should have a suggested fix
            failedTest.SuggestedFix.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ValidateConfiguration_WithValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new VerificationOptions
        {
            DllPath = GetTestPdfiumDllPath()
        };

        var verifier = new PdfiumSignatureVerifier(options, _mockDllAnalyzer);

        // Act
        var result = verifier.ValidateConfiguration();

        // Assert
        if (File.Exists(options.DllPath))
        {
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Contain("not found");
        }
    }

    [Fact]
    public void ValidateConfiguration_WithMissingDllPath_ReturnsFailure()
    {
        // Arrange
        var options = new VerificationOptions
        {
            DllPath = ""
        };

        var verifier = new PdfiumSignatureVerifier(options, _mockDllAnalyzer);

        // Act
        var result = verifier.ValidateConfiguration();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("required");
    }

    [Fact]
    public void LibraryName_ReturnsPDFium()
    {
        // Arrange
        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act & Assert
        verifier.LibraryName.Should().Be("PDFium");
    }

    [Fact]
    public async Task VerifyAsync_MeasuresDuration()
    {
        // Arrange
        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Version = "1.0.0",
            Architecture = "x64",
            ExportCount = 10,
            FileSizeBytes = 1000000
        };

        var exports = GetAllPdfiumExports();

        _mockDllAnalyzer.AnalyzeAsync(_options.DllPath, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(dllInfo));

        _mockDllAnalyzer.GetExportedFunctions()
            .Returns(Result.Ok<IReadOnlyList<string>>(exports));

        var verifier = new PdfiumSignatureVerifier(_options, _mockDllAnalyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);

        // Each individual test should have a duration
        foreach (var testResult in result.Value.Summary.Results)
        {
            testResult.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Gets a comprehensive list of PDFium exports for testing.
    /// This includes all the functions that PdfiumInterop declares.
    /// </summary>
    private List<string> GetAllPdfiumExports()
    {
        return new List<string>
        {
            // Initialization
            "FPDF_InitLibrary",
            "FPDF_DestroyLibrary",

            // Document functions
            "FPDF_LoadDocument",
            "FPDF_CloseDocument",
            "FPDF_GetPageCount",

            // Page functions
            "FPDF_LoadPage",
            "FPDF_ClosePage",
            "FPDF_GetPageWidth",
            "FPDF_GetPageHeight",

            // Bitmap functions
            "FPDFBitmap_Create",
            "FPDFBitmap_Destroy",
            "FPDFBitmap_GetBuffer",
            "FPDFBitmap_GetStride",
            "FPDFBitmap_FillRect",

            // Rendering functions
            "FPDF_RenderPageBitmap",

            // Error functions
            "FPDF_GetLastError",

            // Bookmark functions
            "FPDFBookmark_GetFirstChild",
            "FPDFBookmark_GetNextSibling",
            "FPDFBookmark_GetTitle",
            "FPDFBookmark_GetDest",
            "FPDFDest_GetDestPageIndex",
            "FPDFDest_GetLocationInPage",

            // Text extraction
            "FPDFText_LoadPage",
            "FPDFText_ClosePage",
            "FPDFText_CountChars",
            "FPDFText_GetText",
            "FPDFText_GetCharBox",

            // Text search
            "FPDFText_FindStart",
            "FPDFText_FindNext",
            "FPDFText_FindPrev",
            "FPDFText_GetSchResultIndex",
            "FPDFText_GetSchCount",
            "FPDFText_FindClose",

            // Annotations
            "FPDFPage_GetAnnotCount",
            "FPDFPage_GetAnnot",
            "FPDFPage_CreateAnnot",
            "FPDFPage_RemoveAnnot",
            "FPDFPage_CloseAnnot",
            "FPDFAnnot_GetSubtype",
            "FPDFAnnot_SetColor",
            "FPDFAnnot_GetColor",
            "FPDFAnnot_SetRect",
            "FPDFAnnot_GetRect",
            "FPDFAnnot_SetStringValue",
            "FPDFAnnot_GetStringValue",
            "FPDFAnnot_SetAttachmentPoints",
            "FPDF_SaveAsCopy",

            // Page objects
            "FPDFPageObj_NewImageObj",
            "FPDFImageObj_LoadJpegFile",
            "FPDFImageObj_LoadJpegFileInline",
            "FPDFImageObj_SetBitmap",
            "FPDFPageObj_Transform",
            "FPDFPageObj_SetMatrix",
            "FPDFPage_InsertObject",
            "FPDFPage_RemoveObject",
            "FPDFPageObj_GetBounds",
            "FPDFPageObj_Destroy",
            "FPDFPageObj_CreateTextObj",
            "FPDFText_LoadStandardFont",
            "FPDFText_SetText",
            "FPDFPageObj_SetFillColor",
            "FPDFPageObj_SetStrokeColor",
            "FPDFPage_GenerateContent",
            "FPDFPage_CountObjects",
            "FPDFPage_GetObject"
        };
    }

    /// <summary>
    /// Gets the path to the test PDFium DLL.
    /// In a real test environment, this would point to the actual PDFium DLL.
    /// </summary>
    private string GetTestPdfiumDllPath()
    {
        // Try to find pdfium.dll in common locations
        var possiblePaths = new[]
        {
            @"C:\Windows\System32\pdfium.dll",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfium.dll"),
            Path.Combine(Environment.CurrentDirectory, "pdfium.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "pdfium.dll")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Return a test path - the ValidateConfiguration test will handle the missing file
        return @"C:\test\pdfium.dll";
    }
}
