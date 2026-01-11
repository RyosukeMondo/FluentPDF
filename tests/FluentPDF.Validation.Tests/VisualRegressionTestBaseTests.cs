using FluentAssertions;
using FluentPDF.Rendering.Tests.Models;
using FluentPDF.Rendering.Tests.Services;
using FluentPDF.Validation.Tests.Exceptions;
using FluentResults;
using NSubstitute;
using Xunit;

namespace FluentPDF.Validation.Tests;

public sealed class VisualRegressionTestBaseTests : IDisposable
{
    private readonly IHeadlessRenderingService _mockRenderingService;
    private readonly IVisualComparisonService _mockComparisonService;
    private readonly IBaselineManager _mockBaselineManager;
    private readonly string _testDirectory;
    private readonly TestableVisualRegressionTestBase _sut;

    public VisualRegressionTestBaseTests()
    {
        _mockRenderingService = Substitute.For<IHeadlessRenderingService>();
        _mockComparisonService = Substitute.For<IVisualComparisonService>();
        _mockBaselineManager = Substitute.For<IBaselineManager>();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"visual_tests_{Guid.NewGuid()}");
        _sut = new TestableVisualRegressionTestBase(
            _mockRenderingService,
            _mockComparisonService,
            _mockBaselineManager,
            _testDirectory);
    }

    [Fact]
    public void Constructor_WithNullRenderingService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestableVisualRegressionTestBase(
            null!,
            _mockComparisonService,
            _mockBaselineManager,
            _testDirectory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("renderingService");
    }

    [Fact]
    public void Constructor_WithNullComparisonService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestableVisualRegressionTestBase(
            _mockRenderingService,
            null!,
            _mockBaselineManager,
            _testDirectory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("comparisonService");
    }

    [Fact]
    public void Constructor_WithNullBaselineManager_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestableVisualRegressionTestBase(
            _mockRenderingService,
            _mockComparisonService,
            null!,
            _testDirectory);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baselineManager");
    }

    [Fact]
    public void Constructor_WithValidServices_CreatesTestResultsDirectory()
    {
        // Assert
        Directory.Exists(_testDirectory).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssertVisualMatchAsync_WithInvalidPdfPath_ThrowsArgumentException(string? invalidPath)
    {
        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            invalidPath!,
            "TestCategory",
            "TestName");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pdfPath");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssertVisualMatchAsync_WithInvalidCategory_ThrowsArgumentException(string? invalidCategory)
    {
        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            "test.pdf",
            invalidCategory!,
            "TestName");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("category");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssertVisualMatchAsync_WithInvalidTestName_ThrowsArgumentException(string? invalidTestName)
    {
        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            "test.pdf",
            "TestCategory",
            invalidTestName!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("testName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AssertVisualMatchAsync_WithInvalidPageNumber_ThrowsArgumentOutOfRangeException(int invalidPage)
    {
        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            "test.pdf",
            "TestCategory",
            "TestName",
            pageNumber: invalidPage);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("pageNumber");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public async Task AssertVisualMatchAsync_WithInvalidThreshold_ThrowsArgumentOutOfRangeException(double invalidThreshold)
    {
        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            "test.pdf",
            "TestCategory",
            "TestName",
            threshold: invalidThreshold);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("threshold");
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenRenderingFails_ThrowsInvalidOperationException()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(true);
        _mockBaselineManager.GetBaselinePath(category, testName, 0).Returns("baseline.png");
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Rendering failed"));

        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(pdfPath, category, testName);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to render PDF page*");
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenNoBaseline_CreatesBaselineAndPasses()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(false);
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _mockBaselineManager.CreateBaselineAsync(
                Arg.Any<string>(),
                category,
                testName,
                0,
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok("baseline.png"));

        // Act
        await _sut.TestAssertVisualMatchAsync(pdfPath, category, testName);

        // Assert - should complete without exception
        await _mockBaselineManager.Received(1).CreateBaselineAsync(
            Arg.Any<string>(),
            category,
            testName,
            0,
            Arg.Any<CancellationToken>());
        await _mockComparisonService.DidNotReceive().CompareImagesAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenBaselineCreationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(false);
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _mockBaselineManager.CreateBaselineAsync(
                Arg.Any<string>(),
                category,
                testName,
                0,
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("Baseline creation failed"));

        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(pdfPath, category, testName);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create baseline*");
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenComparisonPasses_CompletesSuccessfully()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";
        const string baselinePath = "baseline.png";

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(true);
        _mockBaselineManager.GetBaselinePath(category, testName, 0).Returns(baselinePath);
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var comparisonResult = new ComparisonResult
        {
            SsimScore = 0.98,
            Passed = true,
            Threshold = 0.95,
            BaselinePath = baselinePath,
            ActualPath = "actual.png",
            DifferencePath = "difference.png",
            ComparedAt = DateTime.UtcNow
        };

        _mockComparisonService.CompareImagesAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok(comparisonResult));

        // Act
        await _sut.TestAssertVisualMatchAsync(pdfPath, category, testName);

        // Assert - should complete without exception
        await _mockComparisonService.Received(1).CompareImagesAsync(
            baselinePath,
            Arg.Any<string>(),
            Arg.Any<string>(),
            0.95,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenComparisonFails_ThrowsVisualRegressionException()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";
        const string baselinePath = "baseline.png";
        const string actualPath = "actual.png";
        const string differencePath = "difference.png";
        const double ssimScore = 0.85;
        const double threshold = 0.95;

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(true);
        _mockBaselineManager.GetBaselinePath(category, testName, 0).Returns(baselinePath);
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var comparisonResult = new ComparisonResult
        {
            SsimScore = ssimScore,
            Passed = false,
            Threshold = threshold,
            BaselinePath = baselinePath,
            ActualPath = actualPath,
            DifferencePath = differencePath,
            ComparedAt = DateTime.UtcNow
        };

        _mockComparisonService.CompareImagesAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                threshold,
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok(comparisonResult));

        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(
            pdfPath,
            category,
            testName,
            threshold: threshold);

        // Assert
        var exception = await act.Should().ThrowAsync<VisualRegressionException>();
        exception.Which.Category.Should().Be(category);
        exception.Which.TestName.Should().Be(testName);
        exception.Which.PageNumber.Should().Be(1);
        exception.Which.SsimScore.Should().Be(ssimScore);
        exception.Which.Threshold.Should().Be(threshold);
        exception.Which.BaselinePath.Should().Be(baselinePath);
        exception.Which.ActualPath.Should().Contain("actual");
        exception.Which.DifferencePath.Should().Contain("difference");
    }

    [Fact]
    public async Task AssertVisualMatchAsync_WhenComparisonServiceFails_ThrowsInvalidOperationException()
    {
        // Arrange
        const string pdfPath = "test.pdf";
        const string category = "TestCategory";
        const string testName = "TestName";

        _mockBaselineManager.BaselineExists(category, testName, 0).Returns(true);
        _mockBaselineManager.GetBaselinePath(category, testName, 0).Returns("baseline.png");
        _mockRenderingService.RenderPageToFileAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        _mockComparisonService.CompareImagesAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<double>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail<ComparisonResult>("Comparison failed"));

        // Act
        var act = async () => await _sut.TestAssertVisualMatchAsync(pdfPath, category, testName);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to compare images*");
    }

    [Fact]
    public void Dispose_DisposesAllServices()
    {
        // Act
        _sut.Dispose();

        // Assert
        _mockRenderingService.Received(1).Dispose();
        _mockComparisonService.Received(1).Dispose();
        _mockBaselineManager.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        // Act
        _sut.Dispose();
        _sut.Dispose();
        _sut.Dispose();

        // Assert
        _mockRenderingService.Received(1).Dispose();
        _mockComparisonService.Received(1).Dispose();
        _mockBaselineManager.Received(1).Dispose();
    }

    public void Dispose()
    {
        _sut?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Testable concrete implementation of VisualRegressionTestBase
    /// to allow testing of the abstract base class.
    /// </summary>
    private sealed class TestableVisualRegressionTestBase : VisualRegressionTestBase
    {
        public TestableVisualRegressionTestBase(
            IHeadlessRenderingService renderingService,
            IVisualComparisonService comparisonService,
            IBaselineManager baselineManager,
            string? testResultsDirectory = null)
            : base(renderingService, comparisonService, baselineManager, testResultsDirectory)
        {
        }

        public Task TestAssertVisualMatchAsync(
            string pdfPath,
            string category,
            string testName,
            int pageNumber = 1,
            double threshold = 0.95,
            int dpi = 96,
            CancellationToken cancellationToken = default)
        {
            return AssertVisualMatchAsync(
                pdfPath,
                category,
                testName,
                pageNumber,
                threshold,
                dpi,
                cancellationToken);
        }
    }
}
