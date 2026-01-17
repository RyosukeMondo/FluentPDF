using FluentPDF.Verification.Core;

namespace FluentPDF.Verification.Core.Tests;

/// <summary>
/// Unit tests for verification framework model classes.
/// Tests data integrity, immutability, and calculated properties.
/// </summary>
public class ModelsTests
{
    [Fact]
    public void DllInfo_Record_SupportsValueEquality()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["Key"] = "Value" };

        var dllInfo1 = new DllInfo
        {
            FilePath = "C:\\test.dll",
            Architecture = "x64",
            ExportCount = 100,
            FileSizeBytes = 1024,
            Version = "1.0",
            Metadata = metadata
        };

        var dllInfo2 = new DllInfo
        {
            FilePath = "C:\\test.dll",
            Architecture = "x64",
            ExportCount = 100,
            FileSizeBytes = 1024,
            Version = "1.0",
            Metadata = metadata // Use same instance
        };

        // Act & Assert
        dllInfo1.Should().Be(dllInfo2);
        (dllInfo1 == dllInfo2).Should().BeTrue();
    }

    [Fact]
    public void DllInfo_Metadata_DefaultsToEmptyDictionary()
    {
        // Arrange & Act
        var dllInfo = new DllInfo
        {
            FilePath = "test.dll",
            Architecture = "x64",
            ExportCount = 50,
            FileSizeBytes = 2048
        };

        // Assert
        dllInfo.Metadata.Should().NotBeNull();
        dllInfo.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void FunctionSignature_FullSignature_WithParameters()
    {
        // Arrange
        var signature = new FunctionSignature
        {
            Name = "FPDF_LoadDocument",
            ReturnType = "FPDF_DOCUMENT",
            Parameters = new[]
            {
                new ParameterInfo { Name = "filePath", Type = "const char*" },
                new ParameterInfo { Name = "password", Type = "const char*" }
            },
            CallingConvention = "Cdecl"
        };

        // Act
        var fullSig = signature.FullSignature;

        // Assert
        fullSig.Should().Be("FPDF_DOCUMENT FPDF_LoadDocument(const char* filePath, const char* password)");
    }

    [Fact]
    public void FunctionSignature_FullSignature_NoParameters()
    {
        // Arrange
        var signature = new FunctionSignature
        {
            Name = "FPDF_InitLibrary",
            ReturnType = "void",
            Parameters = Array.Empty<ParameterInfo>()
        };

        // Act
        var fullSig = signature.FullSignature;

        // Assert
        fullSig.Should().Be("void FPDF_InitLibrary()");
    }

    [Fact]
    public void ParameterInfo_WithMarshalAs_StoresAttribute()
    {
        // Arrange & Act
        var param = new ParameterInfo
        {
            Name = "buffer",
            Type = "byte[]",
            IsOut = true,
            MarshalAs = "UnmanagedType.LPArray"
        };

        // Assert
        param.MarshalAs.Should().Be("UnmanagedType.LPArray");
        param.IsOut.Should().BeTrue();
    }

    [Fact]
    public void VerificationSummary_Success_WhenNoFailures()
    {
        // Arrange
        var summary = new VerificationSummary
        {
            TotalTests = 5,
            PassedTests = 5,
            FailedTests = 0,
            Results = Array.Empty<VerificationResult>(),
            TotalDuration = TimeSpan.FromSeconds(1)
        };

        // Act & Assert
        summary.Success.Should().BeTrue();
    }

    [Fact]
    public void VerificationSummary_Failure_WhenHasFailures()
    {
        // Arrange
        var summary = new VerificationSummary
        {
            TotalTests = 5,
            PassedTests = 4,
            FailedTests = 1,
            Results = Array.Empty<VerificationResult>(),
            TotalDuration = TimeSpan.FromSeconds(1)
        };

        // Act & Assert
        summary.Success.Should().BeFalse();
    }

    [Fact]
    public void VerificationResult_WithMetadata_StoresCustomData()
    {
        // Arrange & Act
        var result = new VerificationResult
        {
            TestName = "Signature Test",
            Success = false,
            ErrorMessage = "Type mismatch",
            SuggestedFix = "Change int to long",
            Duration = TimeSpan.FromMilliseconds(100),
            Metadata = new Dictionary<string, object>
            {
                ["Expected"] = "long",
                ["Actual"] = "int",
                ["Severity"] = "High"
            }
        };

        // Assert
        result.Metadata.Should().HaveCount(3);
        result.Metadata["Expected"].Should().Be("long");
        result.Metadata["Actual"].Should().Be("int");
        result.Metadata["Severity"].Should().Be("High");
    }

    [Fact]
    public void VerificationResult_Metadata_DefaultsToEmptyDictionary()
    {
        // Arrange & Act
        var result = new VerificationResult
        {
            TestName = "Test",
            Success = true,
            Duration = TimeSpan.Zero
        };

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void SignatureMatch_WithDifferences_StoresList()
    {
        // Arrange & Act
        var match = new SignatureMatch
        {
            FunctionName = "FPDF_GetPageWidthF",
            Matches = false,
            DeclaredSignature = "float FPDF_GetPageWidthF(FPDF_PAGE)",
            ActualSignature = "double FPDF_GetPageWidthF(FPDF_PAGE)",
            SuggestedFix = "Change return type from float to double",
            Differences = new[] { "Return type mismatch: float vs double" }
        };

        // Assert
        match.Matches.Should().BeFalse();
        match.Differences.Should().HaveCount(1);
        match.Differences[0].Should().Contain("Return type mismatch");
        match.SuggestedFix.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SignatureMatch_WhenMatches_NoDifferences()
    {
        // Arrange & Act
        var match = new SignatureMatch
        {
            FunctionName = "FPDF_CloseDocument",
            Matches = true,
            DeclaredSignature = "void FPDF_CloseDocument(FPDF_DOCUMENT)",
            ActualSignature = "void FPDF_CloseDocument(FPDF_DOCUMENT)"
        };

        // Assert
        match.Matches.Should().BeTrue();
        match.Differences.Should().BeEmpty();
    }

    [Fact]
    public void VerificationOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new VerificationOptions
        {
            DllPath = "pdfium.dll"
        };

        // Assert
        options.ParallelExecution.Should().BeTrue();
        options.OutputFormat.Should().Be(ReportFormat.Console);
        options.TestTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.OutputPath.Should().BeNull();
        options.TestFileDirectory.Should().BeNull();
    }

    [Fact]
    public void VerificationOptions_AllPropertiesSet()
    {
        // Arrange & Act
        var options = new VerificationOptions
        {
            DllPath = "pdfium.dll",
            TestFileDirectory = "C:\\Tests",
            ParallelExecution = false,
            OutputFormat = ReportFormat.Json,
            OutputPath = "report.json",
            TestTimeout = TimeSpan.FromSeconds(10)
        };

        // Assert
        options.DllPath.Should().Be("pdfium.dll");
        options.TestFileDirectory.Should().Be("C:\\Tests");
        options.ParallelExecution.Should().BeFalse();
        options.OutputFormat.Should().Be(ReportFormat.Json);
        options.OutputPath.Should().Be("report.json");
        options.TestTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ReportFormat_AllEnumValues()
    {
        // Assert - Verify all expected enum values exist
        Enum.IsDefined(typeof(ReportFormat), ReportFormat.Console).Should().BeTrue();
        Enum.IsDefined(typeof(ReportFormat), ReportFormat.Json).Should().BeTrue();
        Enum.IsDefined(typeof(ReportFormat), ReportFormat.Html).Should().BeTrue();

        // Verify there are exactly 3 values
        Enum.GetValues<ReportFormat>().Should().HaveCount(3);
    }

    [Fact]
    public void DllInfo_Immutability_WithExpression()
    {
        // Arrange
        var original = new DllInfo
        {
            FilePath = "original.dll",
            Architecture = "x64",
            ExportCount = 100,
            FileSizeBytes = 1024
        };

        // Act
        var modified = original with { FilePath = "modified.dll" };

        // Assert
        original.FilePath.Should().Be("original.dll");
        modified.FilePath.Should().Be("modified.dll");
        modified.Architecture.Should().Be("x64");
        modified.ExportCount.Should().Be(100);
    }

    [Fact]
    public void VerificationSummary_Immutability_WithExpression()
    {
        // Arrange
        var original = new VerificationSummary
        {
            TotalTests = 5,
            PassedTests = 5,
            FailedTests = 0,
            Results = Array.Empty<VerificationResult>(),
            TotalDuration = TimeSpan.FromSeconds(1),
            LibraryVersion = "1.0"
        };

        // Act
        var modified = original with { LibraryVersion = "2.0" };

        // Assert
        original.LibraryVersion.Should().Be("1.0");
        modified.LibraryVersion.Should().Be("2.0");
        modified.TotalTests.Should().Be(5);
    }

    [Fact]
    public void ParameterInfo_AllProperties()
    {
        // Arrange & Act
        var param = new ParameterInfo
        {
            Name = "buffer",
            Type = "byte*",
            IsOut = true,
            IsIn = false,
            MarshalAs = "UnmanagedType.LPArray"
        };

        // Assert
        param.Name.Should().Be("buffer");
        param.Type.Should().Be("byte*");
        param.IsOut.Should().BeTrue();
        param.IsIn.Should().BeFalse();
        param.MarshalAs.Should().Be("UnmanagedType.LPArray");
    }

    [Fact]
    public void FunctionSignature_CallingConvention_Optional()
    {
        // Arrange & Act
        var signature = new FunctionSignature
        {
            Name = "TestFunc",
            ReturnType = "void",
            Parameters = Array.Empty<ParameterInfo>()
        };

        // Assert
        signature.CallingConvention.Should().BeNull();
    }

    [Fact]
    public void VerificationResult_OptionalFields()
    {
        // Arrange & Act
        var result = new VerificationResult
        {
            TestName = "Test",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50)
        };

        // Assert
        result.ErrorMessage.Should().BeNull();
        result.SuggestedFix.Should().BeNull();
    }
}
