using FluentPDF.Verification.Core;

namespace FluentPDF.Verification.Core.Tests;

/// <summary>
/// Unit tests for DllAnalyzer class.
/// Tests DLL analysis, export extraction, and error handling.
/// </summary>
public class DllAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_WithNullPath_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be null");
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyPath_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync("");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public async Task AnalyzeAsync_WithWhitespacePath_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync("   ");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dll");

        // Act
        var result = await analyzer.AnalyzeAsync(nonExistentPath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task AnalyzeAsync_WithInvalidDllFile_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create a file with invalid content
            await File.WriteAllTextAsync(tempFile, "This is not a valid DLL file");

            // Act
            var result = await analyzer.AnalyzeAsync(tempFile);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().ContainSingle()
                .Which.Message.Should().Match(m =>
                    m.Contains("Invalid") ||
                    m.Contains("Error") ||
                    m.Contains("corrupted") ||
                    m.Contains("Failed"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidSystemDll_ReturnsSuccess()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        // Skip test if running on non-Windows platform
        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        // Act
        var result = await analyzer.AnalyzeAsync(kernel32Path);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FilePath.Should().Be(kernel32Path);
        result.Value.Architecture.Should().NotBeNullOrEmpty();
        result.Value.ExportCount.Should().BeGreaterThan(0);
        result.Value.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeAsync_SamePath_UsesCachedResult()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        // Act
        var result1 = await analyzer.AnalyzeAsync(kernel32Path);
        var result2 = await analyzer.AnalyzeAsync(kernel32Path);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Both results should reference the same object (cached)
        ReferenceEquals(result1.Value, result2.Value).Should().BeTrue();
    }

    [Fact]
    public void GetFunctionSignature_BeforeAnalyze_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = analyzer.GetFunctionSignature("SomeFunction");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("not been analyzed");
    }

    [Fact]
    public void GetFunctionSignature_WithNullName_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = analyzer.GetFunctionSignature(null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public void GetFunctionSignature_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = analyzer.GetFunctionSignature("");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public async Task GetFunctionSignature_AfterAnalyze_WithValidFunction_ReturnsSignature()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        await analyzer.AnalyzeAsync(kernel32Path);

        // Act
        var result = analyzer.GetFunctionSignature("GetCurrentProcess");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("GetCurrentProcess");
        result.Value.ReturnType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFunctionSignature_WithNonExistentFunction_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        await analyzer.AnalyzeAsync(kernel32Path);

        // Act
        var result = analyzer.GetFunctionSignature("ThisFunctionDoesNotExist");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("not found");
    }

    [Fact]
    public void GetExportedFunctions_BeforeAnalyze_ReturnsFailure()
    {
        // Arrange
        var analyzer = new DllAnalyzer();

        // Act
        var result = analyzer.GetExportedFunctions();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("not been analyzed");
    }

    [Fact]
    public async Task GetExportedFunctions_AfterAnalyze_ReturnsExports()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        await analyzer.AnalyzeAsync(kernel32Path);

        // Act
        var result = analyzer.GetExportedFunctions();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain("GetCurrentProcess");
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var act = async () => await analyzer.AnalyzeAsync(kernel32Path, cts.Token);

        // Assert - TaskCanceledException is a subclass of OperationCanceledException
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AnalyzeAsync_DllInfo_ContainsMetadata()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        // Act
        var result = await analyzer.AnalyzeAsync(kernel32Path);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.Should().NotBeNull();
        result.Value.Metadata.Should().ContainKeys("ProductName", "FileDescription", "LastModified");
    }

    [Fact]
    public async Task AnalyzeAsync_Architecture_IsDetectedCorrectly()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        // Act
        var result = await analyzer.AnalyzeAsync(kernel32Path);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Architecture should match the system architecture
        var expectedArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        result.Value.Architecture.Should().Be(expectedArch);
    }

    [Fact]
    public async Task AnalyzeAsync_ThreadSafety_MultipleSimultaneousCalls()
    {
        // Arrange
        var analyzer = new DllAnalyzer();
        var kernel32Path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        if (!File.Exists(kernel32Path))
        {
            return; // Skip on non-Windows
        }

        // Act - Call AnalyzeAsync multiple times in parallel
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => analyzer.AnalyzeAsync(kernel32Path))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All results should be successful and identical (cached)
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // All results should reference the same cached object
        var firstValue = results[0].Value;
        results.Should().AllSatisfy(r =>
            ReferenceEquals(r.Value, firstValue).Should().BeTrue()
        );
    }

    [Fact]
    public void FunctionSignature_FullSignature_FormatsCorrectly()
    {
        // Arrange
        var signature = new FunctionSignature
        {
            Name = "TestFunction",
            ReturnType = "int",
            Parameters = new[]
            {
                new ParameterInfo { Name = "param1", Type = "string" },
                new ParameterInfo { Name = "param2", Type = "int" }
            }
        };

        // Act
        var fullSignature = signature.FullSignature;

        // Assert
        fullSignature.Should().Be("int TestFunction(string param1, int param2)");
    }

    [Fact]
    public void FunctionSignature_FullSignature_WithNoParameters_FormatsCorrectly()
    {
        // Arrange
        var signature = new FunctionSignature
        {
            Name = "NoParamsFunction",
            ReturnType = "void",
            Parameters = Array.Empty<ParameterInfo>()
        };

        // Act
        var fullSignature = signature.FullSignature;

        // Assert
        fullSignature.Should().Be("void NoParamsFunction()");
    }
}
