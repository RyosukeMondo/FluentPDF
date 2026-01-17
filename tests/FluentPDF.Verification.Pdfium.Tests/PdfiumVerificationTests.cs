using FluentAssertions;
using FluentPDF.Verification.Core;
using FluentPDF.Verification.Pdfium;
using FluentResults;
using Xunit;

namespace FluentPDF.Verification.Pdfium.Tests;

/// <summary>
/// Integration tests for complete PDFium verification workflow.
/// Tests all three verifiers (Signature, ReturnType, Behavior) with real PDFium DLL and test PDFs.
/// </summary>
public class PdfiumVerificationTests
{
    private readonly string _pdfiumDllPath;
    private readonly string _testFileDirectory;

    public PdfiumVerificationTests()
    {
        // Attempt to locate PDFium DLL and test fixtures
        _pdfiumDllPath = LocatePdfiumDll();
        _testFileDirectory = LocateTestFixtures();
    }

    [Fact]
    public async Task CompleteVerificationWorkflow_WithRealDll_ExecutesSuccessfully()
    {
        // Skip test if PDFium DLL or test files are not available
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            // This is expected in some CI environments - not a failure
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory,
            ParallelExecution = false
        };

        // Test 1: Signature Verification
        var dllAnalyzer = new DllAnalyzer();
        var signatureVerifier = new PdfiumSignatureVerifier(options, dllAnalyzer);

        var configValidation = signatureVerifier.ValidateConfiguration();
        configValidation.IsSuccess.Should().BeTrue("configuration should be valid");

        var signatureResult = await signatureVerifier.VerifyAsync();
        signatureResult.IsSuccess.Should().BeTrue("signature verification should complete");
        signatureResult.Value.Should().NotBeNull();
        signatureResult.Value.Summary.TotalTests.Should().BeGreaterThan(0);

        // Test 2: Return Type Verification
        var returnTypeVerifier = new PdfiumReturnTypeVerifier(options);

        configValidation = returnTypeVerifier.ValidateConfiguration();
        configValidation.IsSuccess.Should().BeTrue("configuration should be valid");

        var returnTypeResult = await returnTypeVerifier.VerifyAsync();
        returnTypeResult.IsSuccess.Should().BeTrue("return type verification should complete");
        returnTypeResult.Value.Should().NotBeNull();
        returnTypeResult.Value.Summary.TotalTests.Should().BeGreaterThan(0);
        returnTypeResult.Value.TestedFiles.Should().NotBeEmpty();

        // Test 3: Behavior Verification
        var behaviorVerifier = new PdfiumBehaviorVerifier(options);

        configValidation = behaviorVerifier.ValidateConfiguration();
        configValidation.IsSuccess.Should().BeTrue("configuration should be valid");

        var behaviorResult = await behaviorVerifier.VerifyAsync();
        behaviorResult.IsSuccess.Should().BeTrue("behavior verification should complete");
        behaviorResult.Value.Should().NotBeNull();
        behaviorResult.Value.Summary.TotalTests.Should().BeGreaterThan(0);
        behaviorResult.Value.TestedFiles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SignatureVerifier_WithRealDll_DetectsAllExports()
    {
        if (!File.Exists(_pdfiumDllPath))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath
        };

        var dllAnalyzer = new DllAnalyzer();
        var verifier = new PdfiumSignatureVerifier(options, dllAnalyzer);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.DllInfo.Should().NotBeNull();
        result.Value.DllInfo.FilePath.Should().Be(_pdfiumDllPath);
        result.Value.SignatureMatches.Should().NotBeEmpty();

        // The DLL should export standard PDFium functions
        var exportTests = result.Value.Summary.Results
            .Where(r => r.TestName.StartsWith("Export exists:"));
        exportTests.Should().NotBeEmpty();

        // Check for critical PDFium functions
        var criticalFunctions = new[]
        {
            "FPDF_InitLibrary",
            "FPDF_DestroyLibrary",
            "FPDF_LoadDocument",
            "FPDF_CloseDocument",
            "FPDF_GetPageCount"
        };

        foreach (var funcName in criticalFunctions)
        {
            var funcTest = exportTests.FirstOrDefault(r => r.TestName.Contains(funcName));
            if (funcTest != null)
            {
                funcTest.Success.Should().BeTrue($"{funcName} should be exported");
            }
        }
    }

    [Fact]
    public async Task SignatureVerifier_WithRealDll_ProvidesDllMetadata()
    {
        if (!File.Exists(_pdfiumDllPath))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath
        };

        var dllAnalyzer = new DllAnalyzer();
        var verifier = new PdfiumSignatureVerifier(options, dllAnalyzer);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.DllInfo.Architecture.Should().NotBeNullOrEmpty();
        result.Value.DllInfo.ExportCount.Should().BeGreaterThan(0);
        result.Value.DllInfo.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReturnTypeVerifier_WithRealPdfs_ValidatesPageDimensions()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumReturnTypeVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalTests.Should().BeGreaterThan(0);

        // Check for page dimension tests
        var dimensionTests = result.Value.Summary.Results
            .Where(r => r.TestName.Contains("Page width") || r.TestName.Contains("Page height"));

        dimensionTests.Should().NotBeEmpty("should have tested page dimensions");

        // Dimensions should be valid (no garbage values)
        foreach (var test in dimensionTests)
        {
            if (test.Metadata.ContainsKey("IsGarbageValue"))
            {
                test.Metadata["IsGarbageValue"].Should().Be(false,
                    $"test {test.TestName} should not detect garbage values");
            }
        }
    }

    [Fact]
    public async Task ReturnTypeVerifier_WithRealPdfs_DetectsGarbageValues()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumReturnTypeVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();

        // In a properly configured system, garbage values should NOT be detected
        result.Value.GarbageValuesDetected.Should().BeFalse(
            "no garbage values should be detected with correct P/Invoke signatures");

        // Verify the summary result exists
        var summaryResult = result.Value.Summary.Results
            .FirstOrDefault(r => r.TestName == "Overall Return Type Validation");

        summaryResult.Should().NotBeNull();
        summaryResult!.Success.Should().BeTrue(
            "overall validation should pass when all signatures are correct");
    }

    [Fact]
    public async Task BehaviorVerifier_WithRealPdfs_ValidatesDocumentLoading()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumBehaviorVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.TestedFiles.Should().NotBeEmpty();

        // Check for document loading tests
        var loadingTests = result.Value.Summary.Results
            .Where(r => r.TestName.Contains("Document loading behavior"));

        loadingTests.Should().NotBeEmpty("should have tested document loading");

        // All valid PDFs should load successfully
        foreach (var test in loadingTests)
        {
            test.Success.Should().BeTrue($"document should load: {test.TestName}");
        }
    }

    [Fact]
    public async Task BehaviorVerifier_WithRealPdfs_ValidatesRenderingPipeline()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumBehaviorVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();

        // Check for rendering pipeline tests
        var renderingTests = result.Value.Summary.Results
            .Where(r => r.TestName.Contains("Bitmap") || r.TestName.Contains("rendering"));

        renderingTests.Should().NotBeEmpty("should have tested rendering pipeline");

        // Bitmap creation and rendering should succeed
        var bitmapCreation = renderingTests.FirstOrDefault(r => r.TestName.Contains("Bitmap creation"));
        if (bitmapCreation != null)
        {
            bitmapCreation.Success.Should().BeTrue("bitmap creation should succeed");
        }

        var pageRendering = renderingTests.FirstOrDefault(r => r.TestName.Contains("Page rendering"));
        if (pageRendering != null)
        {
            pageRendering.Success.Should().BeTrue("page rendering should succeed");
        }
    }

    [Fact]
    public async Task BehaviorVerifier_WithRealPdfs_ValidatesErrorHandling()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumBehaviorVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();

        // Check for error handling tests
        var errorTests = result.Value.Summary.Results
            .Where(r => r.TestName.Contains("Error handling"));

        errorTests.Should().NotBeEmpty("should have tested error handling");

        // Non-existent file should be handled gracefully
        var nonExistentFileTest = errorTests.FirstOrDefault(r => r.TestName.Contains("non-existent file"));
        if (nonExistentFileTest != null)
        {
            nonExistentFileTest.Success.Should().BeTrue(
                "PDFium should handle non-existent files gracefully");
        }
    }

    [Fact]
    public async Task BehaviorVerifier_WithRealPdfs_ValidatesResourceCleanup()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var verifier = new PdfiumBehaviorVerifier(options);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();

        // Check for resource cleanup test
        var cleanupTest = result.Value.Summary.Results
            .FirstOrDefault(r => r.TestName == "Resource cleanup");

        cleanupTest.Should().NotBeNull("should have tested resource cleanup");
        cleanupTest!.Success.Should().BeTrue(
            "resource cleanup should work without crashes or memory exhaustion");
    }

    [Fact]
    public async Task AllVerifiers_MeasureExecutionTime()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory,
            ParallelExecution = false
        };

        var dllAnalyzer = new DllAnalyzer();
        var signatureVerifier = new PdfiumSignatureVerifier(options, dllAnalyzer);
        var returnTypeVerifier = new PdfiumReturnTypeVerifier(options);
        var behaviorVerifier = new PdfiumBehaviorVerifier(options);

        var signatureResult = await signatureVerifier.VerifyAsync();
        var returnTypeResult = await returnTypeVerifier.VerifyAsync();
        var behaviorResult = await behaviorVerifier.VerifyAsync();

        // All verifiers should measure total duration
        signatureResult.Value.Summary.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        returnTypeResult.Value.Summary.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        behaviorResult.Value.Summary.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);

        // Individual tests should have durations
        foreach (var test in signatureResult.Value.Summary.Results)
        {
            test.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }

        foreach (var test in returnTypeResult.Value.Summary.Results)
        {
            test.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }

        foreach (var test in behaviorResult.Value.Summary.Results)
        {
            test.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }
    }

    [Fact]
    public async Task AllVerifiers_ProvideDetailedMetadata()
    {
        if (!File.Exists(_pdfiumDllPath) || !Directory.Exists(_testFileDirectory))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = _testFileDirectory
        };

        var dllAnalyzer = new DllAnalyzer();
        var signatureVerifier = new PdfiumSignatureVerifier(options, dllAnalyzer);
        var returnTypeVerifier = new PdfiumReturnTypeVerifier(options);
        var behaviorVerifier = new PdfiumBehaviorVerifier(options);

        var signatureResult = await signatureVerifier.VerifyAsync();
        var returnTypeResult = await returnTypeVerifier.VerifyAsync();
        var behaviorResult = await behaviorVerifier.VerifyAsync();

        // Signature verifier metadata
        var signatureTests = signatureResult.Value.Summary.Results
            .Where(r => r.TestName.StartsWith("Signature:"));

        foreach (var test in signatureTests)
        {
            test.Metadata.Should().ContainKey("FunctionName");
            test.Metadata.Should().ContainKey("DeclaredSignature");
            test.Metadata.Should().ContainKey("ActualSignature");
        }

        // Return type verifier metadata
        var dimensionTests = returnTypeResult.Value.Summary.Results
            .Where(r => r.TestName.Contains("Page width") || r.TestName.Contains("Page height"));

        foreach (var test in dimensionTests)
        {
            test.Metadata.Should().ContainKey("PdfFile");
            test.Metadata.Should().ContainKey("ReturnType");
            test.Metadata.Should().ContainKey("FunctionName");
        }

        // Behavior verifier metadata
        var behaviorTests = behaviorResult.Value.Summary.Results
            .Where(r => r.Metadata.ContainsKey("Operation"));

        behaviorTests.Should().NotBeEmpty();
        foreach (var test in behaviorTests)
        {
            test.Metadata["Operation"].Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Verifiers_HandleMissingDll_Gracefully()
    {
        var options = new VerificationOptions
        {
            DllPath = @"C:\non_existent_path\pdfium.dll",
            TestFileDirectory = _testFileDirectory
        };

        var dllAnalyzer = new DllAnalyzer();
        var signatureVerifier = new PdfiumSignatureVerifier(options, dllAnalyzer);
        var returnTypeVerifier = new PdfiumReturnTypeVerifier(options);
        var behaviorVerifier = new PdfiumBehaviorVerifier(options);

        // Configuration validation should fail
        signatureVerifier.ValidateConfiguration().IsFailed.Should().BeTrue(
            "signature verifier should fail validation with missing DLL");
        returnTypeVerifier.ValidateConfiguration().IsFailed.Should().BeTrue(
            "return type verifier should fail validation with missing DLL");
        behaviorVerifier.ValidateConfiguration().IsFailed.Should().BeTrue(
            "behavior verifier should fail validation with missing DLL");

        // Verification should fail with missing DLL
        var signatureResult = await signatureVerifier.VerifyAsync();
        signatureResult.IsFailed.Should().BeTrue(
            "signature verification should fail with missing DLL");

        // Return type and behavior verifiers will fail during initialization
        // We don't test them here as they might throw exceptions during PDFium initialization
    }

    [Fact]
    public void Verifiers_HandleMissingTestFiles_Gracefully()
    {
        if (!File.Exists(_pdfiumDllPath))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath,
            TestFileDirectory = @"C:\non_existent_test_directory"
        };

        var returnTypeVerifier = new PdfiumReturnTypeVerifier(options);
        var behaviorVerifier = new PdfiumBehaviorVerifier(options);

        // Configuration validation should fail
        returnTypeVerifier.ValidateConfiguration().IsFailed.Should().BeTrue(
            "return type verifier should fail validation with missing test directory");
        behaviorVerifier.ValidateConfiguration().IsFailed.Should().BeTrue(
            "behavior verifier should fail validation with missing test directory");
    }

    [Fact]
    public async Task SignatureVerifier_GeneratesSuggestedFixes()
    {
        if (!File.Exists(_pdfiumDllPath))
        {
            return;
        }

        var options = new VerificationOptions
        {
            DllPath = _pdfiumDllPath
        };

        var dllAnalyzer = new DllAnalyzer();
        var verifier = new PdfiumSignatureVerifier(options, dllAnalyzer);

        var result = await verifier.VerifyAsync();

        result.IsSuccess.Should().BeTrue();

        // Check if any failures have suggested fixes
        var failedTests = result.Value.Summary.Results.Where(r => !r.Success);
        foreach (var test in failedTests)
        {
            test.SuggestedFix.Should().NotBeNullOrEmpty(
                $"failed test '{test.TestName}' should have a suggested fix");
        }
    }

    [Fact]
    public void VerificationOptions_ValidatesRequiredFields()
    {
        // Empty DLL path
        var options = new VerificationOptions
        {
            DllPath = ""
        };

        var dllAnalyzer = new DllAnalyzer();
        var verifier = new PdfiumSignatureVerifier(options, dllAnalyzer);

        var result = verifier.ValidateConfiguration();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("required");
    }

    /// <summary>
    /// Attempts to locate the PDFium DLL in common locations.
    /// </summary>
    private string LocatePdfiumDll()
    {
        var possiblePaths = new[]
        {
            // Current directory (where tests run)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfium.dll"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native", "pdfium.dll"),

            // App directory (WinUI app output)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "src", "FluentPDF.App", "bin", "Debug", "net9.0-windows10.0.19041.0", "win-x64", "pdfium.dll"),

            // System directories
            Path.Combine(Environment.SystemDirectory, "pdfium.dll"),
            @"C:\Windows\System32\pdfium.dll",

            // Common development paths
            @"C:\dev\pdfium\pdfium.dll",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "pdfium.dll")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Return a default test path (tests will skip if not found)
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfium.dll");
    }

    /// <summary>
    /// Attempts to locate the test fixtures directory.
    /// </summary>
    private string LocateTestFixtures()
    {
        var possiblePaths = new[]
        {
            // Standard test fixtures location
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures"),

            // Relative to solution root
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures")),

            // Explicit path
            @"C:\Users\ryosu\repos\FluentPDF\tests\Fixtures"
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        // Return a default test path (tests will skip if not found)
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Fixtures");
    }
}
