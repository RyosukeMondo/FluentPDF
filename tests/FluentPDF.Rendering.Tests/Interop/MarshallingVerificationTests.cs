using FluentAssertions;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for P/Invoke marshalling verification components.
/// Tests SignatureAnalyzer, DataMarshallerTester, and CoverageReporter.
/// </summary>
public class MarshallingVerificationTests : IDisposable
{
    private readonly string _testPdfPath;
    private readonly string _bookmarkedPdfPath;

    public MarshallingVerificationTests()
    {
        _testPdfPath = Path.Combine("tests", "Fixtures", "sample.pdf");
        _bookmarkedPdfPath = Path.Combine("tests", "Fixtures", "bookmarked.pdf");
    }

    public void Dispose()
    {
        // Clean up any resources
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    #region SignatureAnalyzer Tests

    [Fact]
    public void SignatureAnalyzer_Constructor_WithNullType_ShouldThrow()
    {
        // Act
        Action act = () => new SignatureAnalyzer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*interopType*");
    }

    [Fact]
    public void SignatureAnalyzer_AnalyzeAllSignatures_ShouldReturnSignatures()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));

        // Act
        var signatures = analyzer.AnalyzeAllSignatures();

        // Assert
        signatures.Should().NotBeEmpty("PdfiumInterop should have DllImport methods");
        signatures.Should().OnlyContain(s => !string.IsNullOrEmpty(s.ReturnType), "all signatures should have return types");
        signatures.Should().OnlyContain(s => !string.IsNullOrEmpty(s.EntryPoint), "all signatures should have entry points");
    }

    [Fact]
    public void SignatureAnalyzer_GetAllDllImportMethodNames_ShouldReturnSortedList()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));

        // Act
        var methodNames = analyzer.GetAllDllImportMethodNames();

        // Assert
        methodNames.Should().NotBeEmpty();
        methodNames.Should().BeInAscendingOrder("method names should be sorted alphabetically");
        methodNames.Should().Contain("FPDF_InitLibrary");
        methodNames.Should().Contain("FPDF_LoadDocument");
    }

    [Fact]
    public void SignatureAnalyzer_AnalyzeSignature_WithValidMethodName_ShouldReturnDetails()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));

        // Act
        var signature = analyzer.AnalyzeSignature("FPDF_InitLibrary");

        // Assert
        signature.Should().NotBeNull();
        signature!.EntryPoint.Should().Be("FPDF_InitLibrary");
        signature.CallingConvention.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SignatureAnalyzer_AnalyzeSignature_WithInvalidMethodName_ShouldReturnNull()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));

        // Act
        var signature = analyzer.AnalyzeSignature("NonExistentMethod");

        // Assert
        signature.Should().BeNull();
    }

    [Fact]
    public void SignatureAnalyzer_AnalyzeSignature_WithNullMethodName_ShouldThrow()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));

        // Act
        Action act = () => analyzer.AnalyzeSignature(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*methodName*");
    }

    [Fact]
    public void SignatureAnalyzer_ValidateSignature_WithMatchingSignature_ShouldPass()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        var actualSignature = analyzer.AnalyzeSignature("FPDF_InitLibrary");
        actualSignature.Should().NotBeNull();

        // Act
        var result = analyzer.ValidateSignature("FPDF_InitLibrary", actualSignature!);

        // Assert
        result.Should().NotBeNull();
        result.SignatureValid.Should().BeTrue("identical signature should validate");
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public void SignatureAnalyzer_ValidateSignature_WithMismatchedReturnType_ShouldFail()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        var actualSignature = analyzer.AnalyzeSignature("FPDF_InitLibrary");
        actualSignature.Should().NotBeNull();

        var wrongSignature = actualSignature! with { ReturnType = "int" }; // Wrong return type

        // Act
        var result = analyzer.ValidateSignature("FPDF_InitLibrary", wrongSignature);

        // Assert
        result.Should().NotBeNull();
        result.SignatureValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Return type mismatch");
    }

    [Fact]
    public void SignatureAnalyzer_ValidateSignature_WithMismatchedCallingConvention_ShouldFail()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        var actualSignature = analyzer.AnalyzeSignature("FPDF_InitLibrary");
        actualSignature.Should().NotBeNull();

        var wrongSignature = actualSignature! with { CallingConvention = "Winapi" }; // Different calling convention

        // Act
        var result = analyzer.ValidateSignature("FPDF_InitLibrary", wrongSignature);

        // Assert
        result.Should().NotBeNull();
        result.SignatureValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Calling convention mismatch");
    }

    [Fact]
    public void SignatureAnalyzer_ValidateSignature_WithMismatchedParameterCount_ShouldFail()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        var actualSignature = analyzer.AnalyzeSignature("FPDF_LoadDocument");
        actualSignature.Should().NotBeNull();

        var wrongSignature = actualSignature! with
        {
            Parameters = new List<ParameterDetails>() // Empty parameters
        };

        // Act
        var result = analyzer.ValidateSignature("FPDF_LoadDocument", wrongSignature);

        // Assert
        result.Should().NotBeNull();
        result.SignatureValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Parameter count mismatch");
    }

    [Fact]
    public void SignatureAnalyzer_ValidateSignature_WithNonExistentMethod_ShouldFail()
    {
        // Arrange
        var analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        var expectedSignature = new SignatureDetails
        {
            ReturnType = "void",
            Parameters = new List<ParameterDetails>(),
            CallingConvention = "Cdecl",
            EntryPoint = "NonExistent"
        };

        // Act
        var result = analyzer.ValidateSignature("NonExistent", expectedSignature);

        // Assert
        result.Should().NotBeNull();
        result.SignatureValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region DataMarshallerTester Tests

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_WithValidPdf_ShouldTestAllTypes()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            // Skip test if fixture not available
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_testPdfPath);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().ContainKey("IntPtr", "should test handle marshalling");
        results.Should().ContainKey("int", "should test integer marshalling");
        results.Should().ContainKey("double", "should test double marshalling");
        results.Should().ContainKey("uint", "should test uint marshalling");
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_IntPtrMarshalling_ShouldSucceed()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_testPdfPath);

        // Assert
        results["IntPtr"].Should().NotBeNull();
        results["IntPtr"].Success.Should().BeTrue("document handle should load successfully");
        results["IntPtr"].DataType.Should().Be("IntPtr");
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_IntMarshalling_ShouldSucceed()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_testPdfPath);

        // Assert
        results["int"].Should().NotBeNull();
        results["int"].Success.Should().BeTrue("page count should be retrieved correctly");
        results["int"].DataType.Should().Be("int");
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_DoubleMarshalling_ShouldDetectFloatAPIIssue()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_testPdfPath);

        // Assert
        results["double"].Should().NotBeNull();
        results["double"].DataType.Should().Be("double");

        // This test verifies that we're using the correct FPDF_GetPageWidth (double API)
        // If this fails, it indicates we're using FPDF_GetPageWidthF (float API) incorrectly
        if (!results["double"].Success)
        {
            results["double"].ErrorDetails.Should().Contain("float API",
                "failure should indicate float API marshalling issue");
        }
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_UIntMarshalling_ShouldSucceed()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_testPdfPath);

        // Assert
        results["uint"].Should().NotBeNull();
        results["uint"].Success.Should().BeTrue("error code should be retrieved correctly");
        results["uint"].DataType.Should().Be("uint");
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_StringMarshalling_WithBookmarks_ShouldSucceed()
    {
        // Arrange
        if (!File.Exists(_bookmarkedPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_bookmarkedPdfPath);

        // Act
        var results = tester.TestMarshallingTypes(_bookmarkedPdfPath);

        // Assert
        results.Should().ContainKey("string");
        results["string"].Should().NotBeNull();
        results["string"].DataType.Should().Be("string");
    }

    [Fact]
    public void DataMarshallerTester_TestMarshallingTypes_WithNonExistentFile_ShouldThrow()
    {
        // Arrange
        var nonExistentPath = "nonexistent_file_12345.pdf";
        using var tester = new DataMarshallerTester();

        // Act
        Action act = () => tester.TestMarshallingTypes(nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void DataMarshallerTester_TestFunction_WithValidFunction_ShouldReturnResult()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var result = tester.TestFunction("FPDF_LoadDocument", _testPdfPath);

        // Assert
        result.Should().NotBeNull();
        result.FunctionName.Should().Be("FPDF_LoadDocument");
        result.SignatureValid.Should().BeTrue();
        result.TestResult.Should().NotBeNull();
    }

    [Fact]
    public void DataMarshallerTester_TestFunction_FPDF_GetPageWidth_ShouldReturnValidDimensions()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var result = tester.TestFunction("FPDF_GetPageWidth", _testPdfPath);

        // Assert
        result.Should().NotBeNull();
        result.FunctionName.Should().Be("FPDF_GetPageWidth");

        if (result.MarshallingCorrect == true)
        {
            result.TestResult!.ActualValue.Should().NotBeNull();
        }
        else
        {
            // If test fails, it should identify the float API issue
            result.ErrorMessage.Should().Contain("range");
        }
    }

    [Fact]
    public void DataMarshallerTester_TestFunction_FPDF_GetLastError_ShouldReturnValidErrorCode()
    {
        // Arrange
        using var tester = new DataMarshallerTester();

        // Act
        var result = tester.TestFunction("FPDF_GetLastError", _testPdfPath);

        // Assert
        result.Should().NotBeNull();
        result.FunctionName.Should().Be("FPDF_GetLastError");
        result.MarshallingCorrect.Should().BeTrue("error code retrieval should always work");
    }

    [Fact]
    public void DataMarshallerTester_TestFunction_WithUnsupportedFunction_ShouldReturnErrorResult()
    {
        // Arrange
        using var tester = new DataMarshallerTester();

        // Act
        var result = tester.TestFunction("UnsupportedFunction", _testPdfPath);

        // Assert
        result.Should().NotBeNull();
        result.FunctionName.Should().Be("UnsupportedFunction");
        result.MarshallingCorrect.Should().BeNull("untested function should return null for marshalling");
        result.ErrorMessage.Should().Contain("No test implementation");
    }

    [Fact]
    public void DataMarshallerTester_RunAllTests_WithValidPdf_ShouldRunMultipleTests()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        using var tester = new DataMarshallerTester(_testPdfPath);

        // Act
        var results = tester.RunAllTests();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.FunctionName == "FPDF_InitLibrary");
        results.Should().OnlyContain(r => r.SignatureValid, "all functions should have valid signatures");
    }

    [Fact]
    public void DataMarshallerTester_Dispose_ShouldShutdownPDFium()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            return;
        }

        var tester = new DataMarshallerTester(_testPdfPath);
        tester.RunAllTests(); // Initialize PDFium

        // Act
        tester.Dispose();

        // Assert - No exception should occur
        // The test passing means Dispose worked correctly
    }

    #endregion

    #region CoverageReporter Tests

    [Fact]
    public void CoverageReporter_GenerateMarkdownReport_WithValidReport_ShouldGenerateMarkdown()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport();

        // Act
        var markdown = reporter.GenerateMarkdownReport(report);

        // Assert
        markdown.Should().NotBeNullOrEmpty();
        markdown.Should().Contain("# P/Invoke Marshalling Verification Report");
        markdown.Should().Contain("## Summary");
        markdown.Should().Contain("Total Functions");
    }

    [Fact]
    public void CoverageReporter_GenerateMarkdownReport_WithCriticalGaps_ShouldIncludeCriticalSection()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport(hasFailures: true);

        // Act
        var markdown = reporter.GenerateMarkdownReport(report);

        // Assert
        markdown.Should().Contain("⚠️ Critical Issues");
        markdown.Should().Contain("Failed Verification");
    }

    [Fact]
    public void CoverageReporter_GenerateMarkdownReport_WithUntestedFunctions_ShouldListThem()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport(hasUntested: true);

        // Act
        var markdown = reporter.GenerateMarkdownReport(report);

        // Assert
        markdown.Should().Contain("Untested Functions");
    }

    [Fact]
    public void CoverageReporter_GenerateMarkdownReport_WithoutDetails_ShouldExcludeDetailedResults()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport();

        // Act
        var markdown = reporter.GenerateMarkdownReport(report, includeDetails: false);

        // Assert
        markdown.Should().NotContain("## Detailed Results");
    }

    [Fact]
    public void CoverageReporter_GenerateConsoleSummary_ShouldGenerateReadableSummary()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport();

        // Act
        var summary = reporter.GenerateConsoleSummary(report);

        // Assert
        summary.Should().NotBeNullOrEmpty();
        summary.Should().Contain("P/Invoke Marshalling Verification Summary");
        summary.Should().Contain("Total Functions:");
        summary.Should().Contain("Verified:");
        summary.Should().Contain("Tested:");
    }

    [Fact]
    public void CoverageReporter_GenerateConsoleSummary_WithFailures_ShouldShowCriticalWarning()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport(hasFailures: true);

        // Act
        var summary = reporter.GenerateConsoleSummary(report);

        // Assert
        summary.Should().Contain("⚠️  CRITICAL GAPS DETECTED");
        summary.Should().Contain("failed verification");
    }

    [Fact]
    public void CoverageReporter_GenerateConsoleSummary_WithAllPassed_ShouldShowSuccess()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport(hasFailures: false, hasUntested: false);

        // Act
        var summary = reporter.GenerateConsoleSummary(report);

        // Assert
        summary.Should().Contain("✓ All functions verified successfully");
    }

    [Fact]
    public void CoverageReporter_SaveReport_ShouldWriteFileToOutputPath()
    {
        // Arrange
        var reporter = new CoverageReporter();
        var report = CreateSampleCoverageReport();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            reporter.SaveReport(report, tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().Contain("# P/Invoke Marshalling Verification Report");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static CoverageReport CreateSampleCoverageReport(bool hasFailures = false, bool hasUntested = false)
    {
        var results = new List<VerificationResult>
        {
            new VerificationResult
            {
                FunctionName = "FPDF_InitLibrary",
                SignatureValid = true,
                MarshallingCorrect = true,
                ErrorMessage = null,
                TestResult = new MarshallingTestResult
                {
                    Success = true,
                    DataType = "void",
                    TestCaseName = "Initialization"
                }
            },
            new VerificationResult
            {
                FunctionName = "FPDF_LoadDocument",
                SignatureValid = true,
                MarshallingCorrect = true,
                ErrorMessage = null,
                TestResult = new MarshallingTestResult
                {
                    Success = true,
                    DataType = "IntPtr",
                    TestCaseName = "Load Document"
                }
            }
        };

        if (hasFailures)
        {
            results.Add(new VerificationResult
            {
                FunctionName = "FPDF_GetPageWidthF",
                SignatureValid = true,
                MarshallingCorrect = false,
                ErrorMessage = "Float API marshalling corruption detected",
                TestResult = new MarshallingTestResult
                {
                    Success = false,
                    DataType = "float",
                    TestCaseName = "Get Page Width (Float)",
                    ErrorDetails = "Value out of expected range - float API issue"
                }
            });
        }

        var totalFunctions = results.Count + (hasUntested ? 2 : 0);
        var verifiedFunctions = results.Count;
        var testedFunctions = results.Count(r => r.MarshallingCorrect.HasValue);
        var passedFunctions = results.Count(r => r.SignatureValid && r.MarshallingCorrect == true);
        var failedFunctions = results.Count(r => !r.SignatureValid || r.MarshallingCorrect == false);

        var untestedFunctions = new List<string>();
        if (hasUntested)
        {
            untestedFunctions.Add("FPDF_SomeUntestedFunction");
            untestedFunctions.Add("FPDF_AnotherUntestedFunction");
        }

        var failedFunctionNames = results
            .Where(r => !r.SignatureValid || r.MarshallingCorrect == false)
            .Select(r => r.FunctionName)
            .ToList();

        return new CoverageReport
        {
            TotalFunctions = totalFunctions,
            VerifiedFunctions = verifiedFunctions,
            TestedFunctions = testedFunctions,
            PassedFunctions = passedFunctions,
            FailedFunctions = failedFunctions,
            UntestedFunctions = untestedFunctions,
            FailedFunctionNames = failedFunctionNames,
            Results = results,
            GeneratedAt = DateTime.UtcNow
        };
    }

    #endregion
}
