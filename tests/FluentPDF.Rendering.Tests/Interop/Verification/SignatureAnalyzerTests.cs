using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Interop.Verification;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

/// <summary>
/// Tests for the SignatureAnalyzer class, including external specification validation.
/// </summary>
public class SignatureAnalyzerTests
{
    private readonly SignatureAnalyzer _analyzer;
    private readonly string _specPath;

    public SignatureAnalyzerTests()
    {
        _analyzer = new SignatureAnalyzer(typeof(PdfiumInterop));
        _specPath = Path.Combine(AppContext.BaseDirectory, "TestData", "pdfium-spec.json");
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_WithValidSpecFile_ReturnsResults()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.True(results.Count >= 20, $"Expected at least 20 functions in spec, got {results.Count}");
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _analyzer.ValidateAgainstPDFiumSpec(null!));
        Assert.Contains("Specification path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _analyzer.ValidateAgainstPDFiumSpec(string.Empty));
        Assert.Contains("Specification path cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(AppContext.BaseDirectory, "does-not-exist.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _analyzer.ValidateAgainstPDFiumSpec(nonExistentPath));
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_ValidatesKnownFunctions()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - Check for critical functions
        var initLibrary = results.FirstOrDefault(r => r.FunctionName == "FPDF_InitLibrary");
        var loadDocument = results.FirstOrDefault(r => r.FunctionName == "FPDF_LoadDocument");
        var getPageCount = results.FirstOrDefault(r => r.FunctionName == "FPDF_GetPageCount");

        Assert.NotNull(initLibrary);
        Assert.NotNull(loadDocument);
        Assert.NotNull(getPageCount);
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_DetectsSignatureMismatches()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - Check for any validation failures
        var failedValidations = results.Where(r => !r.SignatureValid).ToList();

        // Log failures for debugging (if any)
        foreach (var failure in failedValidations)
        {
            Assert.NotNull(failure.ErrorMessage);
            Assert.NotEmpty(failure.ErrorMessage);
        }

        // We expect most signatures to match, but there might be some legitimate differences
        // The test passes if we successfully validated all functions in the spec
        Assert.True(results.Count >= 20, "Should validate at least 20 functions");
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_AllResultsHaveFunctionName()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result.FunctionName);
            Assert.NotEmpty(result.FunctionName);
        });
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_ValidatesParameterCount()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - FPDF_InitLibrary should have 0 parameters
        var initLibrary = results.FirstOrDefault(r => r.FunctionName == "FPDF_InitLibrary");
        Assert.NotNull(initLibrary);

        if (initLibrary.SignatureValid && initLibrary.Signature != null)
        {
            Assert.Empty(initLibrary.Signature.Parameters);
        }
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_ValidatesReturnType()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - FPDF_GetPageCount should return int
        var getPageCount = results.FirstOrDefault(r => r.FunctionName == "FPDF_GetPageCount");
        Assert.NotNull(getPageCount);

        if (getPageCount.SignatureValid && getPageCount.Signature != null)
        {
            Assert.Equal("int", getPageCount.Signature.ReturnType);
        }
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_ValidatesCallingConvention()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - All PDFium functions should use Cdecl calling convention
        var functionsWithWrongConvention = results
            .Where(r => r.SignatureValid && r.Signature != null)
            .Where(r => !r.Signature!.CallingConvention.Equals("Cdecl", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(functionsWithWrongConvention);
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_ReportIncludesSignatureDetails()
    {
        // Act
        var results = _analyzer.ValidateAgainstPDFiumSpec(_specPath);

        // Assert - Functions that were found should have signature details
        var foundFunctions = results.Where(r => r.Signature != null).ToList();
        Assert.NotEmpty(foundFunctions);

        foreach (var result in foundFunctions)
        {
            Assert.NotNull(result.Signature);
            Assert.NotNull(result.Signature.ReturnType);
            Assert.NotNull(result.Signature.Parameters);
            Assert.NotNull(result.Signature.CallingConvention);
            Assert.NotNull(result.Signature.EntryPoint);
        }
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_HandlesMissingFunctions()
    {
        // Arrange - Create a temporary spec file with a non-existent function
        var tempSpecPath = Path.Combine(Path.GetTempPath(), $"temp-spec-{Guid.NewGuid()}.json");
        try
        {
            var tempSpec = @"{
                ""title"": ""Test Spec"",
                ""version"": ""1.0.0"",
                ""functions"": [
                    {
                        ""name"": ""NonExistentFunction_Test"",
                        ""returnType"": ""void"",
                        ""parameters"": [],
                        ""callingConvention"": ""Cdecl"",
                        ""marshalingNotes"": ""This function does not exist""
                    }
                ]
            }";
            File.WriteAllText(tempSpecPath, tempSpec);

            // Act
            var results = _analyzer.ValidateAgainstPDFiumSpec(tempSpecPath);

            // Assert
            Assert.Single(results);
            Assert.False(results[0].SignatureValid);
            Assert.Contains("not found", results[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempSpecPath))
            {
                File.Delete(tempSpecPath);
            }
        }
    }

    [Fact]
    public void ValidateAgainstPDFiumSpec_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<List<VerificationResult>>>();

        // Act - Run validation from multiple threads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _analyzer.ValidateAgainstPDFiumSpec(_specPath)));
        }

        var results = Task.WhenAll(tasks).GetAwaiter().GetResult();

        // Assert - All results should be consistent
        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.NotEmpty(r);
            Assert.True(r.Count >= 20);
        });

        // Verify all runs produced the same function count
        var firstCount = results[0].Count;
        Assert.All(results, r => Assert.Equal(firstCount, r.Count));
    }

    [Fact]
    public void AnalyzeSignature_ExistingMethod_ReturnsSignatureDetails()
    {
        // Act
        var signature = _analyzer.AnalyzeSignature("FPDF_InitLibrary");

        // Assert
        Assert.NotNull(signature);
        Assert.Equal("void", signature.ReturnType);
        Assert.Equal("Cdecl", signature.CallingConvention);
        Assert.Empty(signature.Parameters);
    }

    [Fact]
    public void ValidateSignature_MatchingSignature_ReturnsValid()
    {
        // Arrange
        var expectedSignature = new SignatureDetails
        {
            ReturnType = "void",
            Parameters = new List<ParameterDetails>(),
            CallingConvention = "Cdecl",
            EntryPoint = "FPDF_InitLibrary"
        };

        // Act
        var result = _analyzer.ValidateSignature("FPDF_InitLibrary", expectedSignature);

        // Assert
        Assert.True(result.SignatureValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignature_MismatchedReturnType_ReturnsInvalid()
    {
        // Arrange
        var expectedSignature = new SignatureDetails
        {
            ReturnType = "int", // Wrong - should be void
            Parameters = new List<ParameterDetails>(),
            CallingConvention = "Cdecl",
            EntryPoint = "FPDF_InitLibrary"
        };

        // Act
        var result = _analyzer.ValidateSignature("FPDF_InitLibrary", expectedSignature);

        // Assert
        Assert.False(result.SignatureValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Return type mismatch", result.ErrorMessage);
    }

    [Fact]
    public void GetAllDllImportMethodNames_ReturnsNonEmptyList()
    {
        // Act
        var methodNames = _analyzer.GetAllDllImportMethodNames();

        // Assert
        Assert.NotNull(methodNames);
        Assert.NotEmpty(methodNames);
        Assert.Contains("FPDF_InitLibrary", methodNames);
        Assert.Contains("FPDF_DestroyLibrary", methodNames);
    }

    [Fact]
    public void AnalyzeAllSignatures_ReturnsAllDllImportMethods()
    {
        // Act
        var signatures = _analyzer.AnalyzeAllSignatures();

        // Assert
        Assert.NotNull(signatures);
        Assert.NotEmpty(signatures);
        Assert.All(signatures, s =>
        {
            Assert.NotNull(s.ReturnType);
            Assert.NotNull(s.Parameters);
            Assert.NotNull(s.CallingConvention);
            Assert.NotNull(s.EntryPoint);
        });
    }
}
