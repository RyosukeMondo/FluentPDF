using FluentAssertions;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

public sealed class BaselineManagerTests : IDisposable
{
    private readonly string _tempBaselineRoot;
    private readonly string _tempSourceDir;
    private readonly BaselineManager _manager;

    public BaselineManagerTests()
    {
        // Create temporary directories for testing
        _tempBaselineRoot = Path.Combine(Path.GetTempPath(), $"baseline_tests_{Guid.NewGuid()}");
        _tempSourceDir = Path.Combine(Path.GetTempPath(), $"source_tests_{Guid.NewGuid()}");

        Directory.CreateDirectory(_tempBaselineRoot);
        Directory.CreateDirectory(_tempSourceDir);

        _manager = new BaselineManager(_tempBaselineRoot);
    }

    public void Dispose()
    {
        _manager.Dispose();

        // Clean up temporary directories
        if (Directory.Exists(_tempBaselineRoot))
        {
            Directory.Delete(_tempBaselineRoot, recursive: true);
        }

        if (Directory.Exists(_tempSourceDir))
        {
            Directory.Delete(_tempSourceDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new BaselineManager(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baselineRootPath");
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new BaselineManager(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baselineRootPath");
    }

    [Fact]
    public void GetBaselinePath_WithValidParameters_ReturnsCorrectPath()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "SimpleText";
        const int pageNumber = 0;

        // Act
        var path = _manager.GetBaselinePath(category, testName, pageNumber);

        // Assert
        path.Should().Contain(category);
        path.Should().Contain(testName);
        path.Should().EndWith("page_0.png");
        Path.IsPathRooted(path).Should().BeTrue();
    }

    [Fact]
    public void GetBaselinePath_WithInvalidCharacters_SanitizesPath()
    {
        // Arrange
        const string category = "Core:Rendering";
        const string testName = "Test<>Name";
        const int pageNumber = 0;

        // Act
        var path = _manager.GetBaselinePath(category, testName, pageNumber);

        // Assert
        path.Should().NotContain(":");
        path.Should().NotContain("<");
        path.Should().NotContain(">");
    }

    [Theory]
    [InlineData(null, "TestName", 0)]
    [InlineData("", "TestName", 0)]
    [InlineData("Category", null, 0)]
    [InlineData("Category", "", 0)]
    public void GetBaselinePath_WithNullOrEmptyParameters_ThrowsArgumentException(
        string category,
        string testName,
        int pageNumber)
    {
        // Act & Assert
        var act = () => _manager.GetBaselinePath(category, testName, pageNumber);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetBaselinePath_WithNegativePageNumber_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = () => _manager.GetBaselinePath("Category", "TestName", -1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pageNumber");
    }

    [Fact]
    public void BaselineExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "NonExistent";
        const int pageNumber = 0;

        // Act
        var exists = _manager.BaselineExists(category, testName, pageNumber);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task BaselineExists_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "ExistingTest";
        const int pageNumber = 0;

        var sourcePath = CreateTempImageFile();
        await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Act
        var exists = _manager.BaselineExists(category, testName, pageNumber);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBaselineAsync_WithValidSource_CreatesBaselineFile()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "CreateTest";
        const int pageNumber = 0;

        var sourcePath = CreateTempImageFile();

        // Act
        var result = await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        File.Exists(result.Value).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBaselineAsync_CreatesDirectoryStructure()
    {
        // Arrange
        const string category = "Zoom";
        const string testName = "ZoomLevel150";
        const int pageNumber = 2;

        var sourcePath = CreateTempImageFile();

        // Act
        var result = await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var expectedDir = Path.GetDirectoryName(result.Value);
        expectedDir.Should().NotBeNull();
        Directory.Exists(expectedDir).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBaselineAsync_WithNullSourcePath_ReturnsFailure()
    {
        // Act
        var result = await _manager.CreateBaselineAsync(
            null!,
            "Category",
            "TestName",
            0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("INVALID_SOURCE_PATH");
    }

    [Fact]
    public async Task CreateBaselineAsync_WithNonExistentSource_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempSourceDir, "nonexistent.png");

        // Act
        var result = await _manager.CreateBaselineAsync(
            nonExistentPath,
            "Category",
            "TestName",
            0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("SOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task CreateBaselineAsync_WhenBaselineAlreadyExists_ReturnsFailure()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "DuplicateTest";
        const int pageNumber = 0;

        var sourcePath = CreateTempImageFile();
        await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Act
        var result = await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("BASELINE_ALREADY_EXISTS");
    }

    [Fact]
    public async Task CreateBaselineAsync_CopiesFileContent()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "ContentTest";
        const int pageNumber = 0;

        var sourcePath = CreateTempImageFile();
        var sourceContent = await File.ReadAllBytesAsync(sourcePath);

        // Act
        var result = await _manager.CreateBaselineAsync(sourcePath, category, testName, pageNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var baselineContent = await File.ReadAllBytesAsync(result.Value);
        baselineContent.Should().Equal(sourceContent);
    }

    [Fact]
    public async Task UpdateBaselineAsync_WithExistingBaseline_UpdatesFile()
    {
        // Arrange
        const string category = "CoreRendering";
        const string testName = "UpdateTest";
        const int pageNumber = 0;

        var originalSource = CreateTempImageFile();
        var createResult = await _manager.CreateBaselineAsync(
            originalSource,
            category,
            testName,
            pageNumber);

        var newSource = CreateTempImageFile();
        var newContent = await File.ReadAllBytesAsync(newSource);

        // Act
        var result = await _manager.UpdateBaselineAsync(newSource, category, testName, pageNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(createResult.Value);

        var updatedContent = await File.ReadAllBytesAsync(result.Value);
        updatedContent.Should().Equal(newContent);
    }

    [Fact]
    public async Task UpdateBaselineAsync_WithNonExistentBaseline_ReturnsFailure()
    {
        // Arrange
        var sourcePath = CreateTempImageFile();

        // Act
        var result = await _manager.UpdateBaselineAsync(
            sourcePath,
            "Category",
            "NonExistent",
            0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("BASELINE_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateBaselineAsync_WithNullSourcePath_ReturnsFailure()
    {
        // Act
        var result = await _manager.UpdateBaselineAsync(
            null!,
            "Category",
            "TestName",
            0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("INVALID_SOURCE_PATH");
    }

    [Fact]
    public async Task UpdateBaselineAsync_WithNonExistentSource_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempSourceDir, "nonexistent.png");

        // Act
        var result = await _manager.UpdateBaselineAsync(
            nonExistentPath,
            "Category",
            "TestName",
            0);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("SOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task CreateBaselineAsync_SupportsCancellation()
    {
        // Arrange
        var sourcePath = CreateTempImageFile();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _manager.CreateBaselineAsync(
                sourcePath,
                "Category",
                "TestName",
                0,
                cts.Token));
    }

    [Fact]
    public async Task UpdateBaselineAsync_SupportsCancellation()
    {
        // Arrange
        var sourcePath = CreateTempImageFile();
        await _manager.CreateBaselineAsync(sourcePath, "Category", "TestName", 0);

        var newSource = CreateTempImageFile();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _manager.UpdateBaselineAsync(
                newSource,
                "Category",
                "TestName",
                0,
                cts.Token));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var manager = new BaselineManager(_tempBaselineRoot);

        // Act & Assert
        manager.Dispose();
        manager.Dispose(); // Should not throw
    }

    /// <summary>
    /// Creates a temporary image file for testing.
    /// </summary>
    private string CreateTempImageFile()
    {
        var path = Path.Combine(_tempSourceDir, $"test_image_{Guid.NewGuid()}.png");

        // Create a simple 1x1 PNG file (minimal valid PNG)
        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, // IEND chunk
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        };

        File.WriteAllBytes(path, pngBytes);
        return path;
    }
}
