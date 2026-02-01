using FluentAssertions;
using FluentPDF.App.Testing;
using FluentPDF.App.Testing.Tests;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using System.Text.Json;

namespace FluentPDF.App.Tests.Testing.Tests;

/// <summary>
/// Unit tests for document operations CLI test implementations.
/// Verifies BookmarksCliTest, SearchCliTest, PageRotateCliTest, PageDeleteCliTest,
/// PageReorderCliTest, AnnotationsCliTest, and MetadataCliTest.
/// </summary>
public sealed class DocumentOperationsCliTestsTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IPdfDocumentService> _mockDocumentService;
    private readonly Mock<IBookmarkService> _mockBookmarkService;
    private readonly Mock<ITextSearchService> _mockSearchService;
    private readonly Mock<IPageOperationsService> _mockPageOpsService;
    private readonly Mock<IAnnotationService> _mockAnnotationService;

    public DocumentOperationsCliTestsTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"FluentPDF_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        _mockLogger = new Mock<ILogger>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockDocumentService = new Mock<IPdfDocumentService>();
        _mockBookmarkService = new Mock<IBookmarkService>();
        _mockSearchService = new Mock<ITextSearchService>();
        _mockPageOpsService = new Mock<IPageOperationsService>();
        _mockAnnotationService = new Mock<IAnnotationService>();

        // Setup service provider
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IPdfDocumentService)))
            .Returns(_mockDocumentService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IBookmarkService)))
            .Returns(_mockBookmarkService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ITextSearchService)))
            .Returns(_mockSearchService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IPageOperationsService)))
            .Returns(_mockPageOpsService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAnnotationService)))
            .Returns(_mockAnnotationService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region BookmarksCliTest Tests

    [Fact]
    public void BookmarksCliTest_HasCorrectMetadata()
    {
        var test = new BookmarksCliTest();

        test.Name.Should().Be("bookmarks");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BookmarksCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new BookmarksCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task BookmarksCliTest_RunAsync_WithMissingPdf_ReturnsFailure()
    {
        var test = new BookmarksCliTest();
        var context = CreateTestContext();
        context.Data["TestPdfPath"] = Path.Combine(_tempDirectory, "nonexistent.pdf");

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task BookmarksCliTest_RunAsync_WithBookmarks_ReturnsSuccess()
    {
        var test = new BookmarksCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var bookmarks = new List<BookmarkNode>
        {
            new() { Title = "Chapter 1", PageNumber = 1, Children = new List<BookmarkNode>() },
            new() { Title = "Chapter 2", PageNumber = 2, Children = new List<BookmarkNode>() }
        };
        _mockBookmarkService.Setup(s => s.ExtractBookmarksAsync(mockDocument))
            .ReturnsAsync(Result.Ok(bookmarks));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("RootBookmarkCount");
        result.Outputs.Should().ContainKey("TotalBookmarkCount");
        result.Outputs.Should().ContainKey("BookmarksWithDestinations");
        result.Outputs.Should().ContainKey("InvalidPageNumbers");
        result.Outputs["RootBookmarkCount"].Should().Be(2);
        result.Outputs["TotalBookmarkCount"].Should().Be(2);
        result.Outputs["BookmarksWithDestinations"].Should().Be(2);
        result.Outputs["InvalidPageNumbers"].Should().Be(0);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task BookmarksCliTest_RunAsync_WithInvalidPageNumbers_ReturnsSuccessWithNonZeroExitCode()
    {
        var test = new BookmarksCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var bookmarks = new List<BookmarkNode>
        {
            new() { Title = "Chapter 1", PageNumber = 1, Children = new List<BookmarkNode>() },
            new() { Title = "Chapter 999", PageNumber = 999, Children = new List<BookmarkNode>() }
        };
        _mockBookmarkService.Setup(s => s.ExtractBookmarksAsync(mockDocument))
            .ReturnsAsync(Result.Ok(bookmarks));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs["InvalidPageNumbers"].Should().Be(1);
        result.Outputs["ExitCode"].Should().Be(1);
    }

    [Fact]
    public async Task BookmarksCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new BookmarksCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task BookmarksCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new BookmarksCliTest();
        var outputFile = Path.Combine(_tempDirectory, "bookmarks.json");
        await File.WriteAllTextAsync(outputFile, "[]");

        var result = new CliTestResult
        {
            TestName = "bookmarks",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["RootBookmarkCount"] = 2,
                ["TotalBookmarkCount"] = 2,
                ["BookmarksWithDestinations"] = 2,
                ["InvalidPageNumbers"] = 0,
                ["ExtractionTimeMs"] = 50.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    [Fact]
    public async Task BookmarksCliTest_VerifyAsync_WithInvalidPageNumbers_ReturnsFalse()
    {
        var test = new BookmarksCliTest();
        var outputFile = Path.Combine(_tempDirectory, "bookmarks.json");
        await File.WriteAllTextAsync(outputFile, "[]");

        var result = new CliTestResult
        {
            TestName = "bookmarks",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["RootBookmarkCount"] = 2,
                ["TotalBookmarkCount"] = 2,
                ["InvalidPageNumbers"] = 1
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeFalse();
    }

    #endregion

    #region SearchCliTest Tests

    [Fact]
    public void SearchCliTest_HasCorrectMetadata()
    {
        var test = new SearchCliTest();

        test.Name.Should().Be("search");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new SearchCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task SearchCliTest_RunAsync_WithMatches_ReturnsSuccess()
    {
        var test = new SearchCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["SearchTerm"] = "test";

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var matches = new List<SearchMatch>
        {
            new() { PageNumber = 0, CharIndex = 10, Length = 4, Text = "test", BoundingBox = new BoundingBox { Left = 10, Top = 20, Right = 30, Bottom = 40 } },
            new() { PageNumber = 1, CharIndex = 50, Length = 4, Text = "test", BoundingBox = new BoundingBox { Left = 15, Top = 25, Right = 35, Bottom = 45 } }
        };
        _mockSearchService.Setup(s => s.SearchAsync(mockDocument, "test", It.IsAny<SearchOptions>()))
            .ReturnsAsync(Result.Ok((IReadOnlyList<SearchMatch>)matches));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("JsonOutputFile");
        result.Outputs.Should().ContainKey("SearchTerm");
        result.Outputs.Should().ContainKey("TotalMatches");
        result.Outputs.Should().ContainKey("PagesWithMatches");
        result.Outputs["SearchTerm"].Should().Be("test");
        result.Outputs["TotalMatches"].Should().Be(2);
        result.Outputs["PagesWithMatches"].Should().Be(2);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task SearchCliTest_RunAsync_WithZeroResults_ReturnsSuccess()
    {
        var test = new SearchCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["SearchTerm"] = "nonexistent";

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var matches = new List<SearchMatch>();
        _mockSearchService.Setup(s => s.SearchAsync(mockDocument, "nonexistent", It.IsAny<SearchOptions>()))
            .ReturnsAsync(Result.Ok((IReadOnlyList<SearchMatch>)matches));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs["TotalMatches"].Should().Be(0);
        result.Outputs["PagesWithMatches"].Should().Be(0);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task SearchCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new SearchCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task SearchCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new SearchCliTest();
        var outputFile = Path.Combine(_tempDirectory, "search_results.txt");
        var jsonFile = Path.Combine(_tempDirectory, "search_results.json");
        await File.WriteAllTextAsync(outputFile, "Search results");
        await File.WriteAllTextAsync(jsonFile, "{}");

        var result = new CliTestResult
        {
            TestName = "search",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["JsonOutputFile"] = jsonFile,
                ["SearchTerm"] = "test",
                ["TotalMatches"] = 5,
                ["PagesWithMatches"] = 2,
                ["PageCount"] = 3,
                ["SearchTimeMs"] = 25.0,
                ["MatchesByPage"] = new Dictionary<int, int> { { 0, 3 }, { 1, 2 } }
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    #endregion

    #region PageRotateCliTest Tests

    [Fact]
    public void PageRotateCliTest_HasCorrectMetadata()
    {
        var test = new PageRotateCliTest();

        test.Name.Should().Be("page-rotate");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PageRotateCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new PageRotateCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task PageRotateCliTest_RunAsync_WithSuccessfulRotation_ReturnsSuccess()
    {
        var test = new PageRotateCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 0, 1 };
        context.Data["RotationAngle"] = RotationAngle.Rotate90;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockPageOpsService.Setup(s => s.RotatePagesAsync(mockDocument, new[] { 0, 1 }, RotationAngle.Rotate90))
            .ReturnsAsync(Result.Ok());

        _mockDocumentService.Setup(s => s.SaveDocumentAsync(mockDocument, It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("ReportFile");
        result.Outputs.Should().ContainKey("RotationAngle");
        result.Outputs.Should().ContainKey("OperationTimeMs");
        result.Outputs["RotationAngle"].Should().Be(90);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task PageRotateCliTest_RunAsync_WithInvalidPageIndex_ReturnsFailure()
    {
        var test = new PageRotateCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 999 };

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid page indices");
    }

    [Fact]
    public async Task PageRotateCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new PageRotateCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task PageRotateCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new PageRotateCliTest();
        var outputFile = Path.Combine(_tempDirectory, "rotated.pdf");
        await File.WriteAllBytesAsync(outputFile, new byte[1024]);

        var result = new CliTestResult
        {
            TestName = "page-rotate",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["OperationTimeMs"] = 50.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    #endregion

    #region PageDeleteCliTest Tests

    [Fact]
    public void PageDeleteCliTest_HasCorrectMetadata()
    {
        var test = new PageDeleteCliTest();

        test.Name.Should().Be("page-delete");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PageDeleteCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new PageDeleteCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task PageDeleteCliTest_RunAsync_WithSuccessfulDeletion_ReturnsSuccess()
    {
        var test = new PageDeleteCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 1 };

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockPageOpsService.Setup(s => s.DeletePagesAsync(mockDocument, new[] { 1 }))
            .ReturnsAsync(Result.Ok())
            .Callback(() => mockDocument.Setup(d => d.PageCount).Returns(2));

        _mockDocumentService.Setup(s => s.SaveDocumentAsync(mockDocument, It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("OriginalPageCount");
        result.Outputs.Should().ContainKey("NewPageCount");
        result.Outputs.Should().ContainKey("PagesDeleted");
        result.Outputs["OriginalPageCount"].Should().Be(3);
        result.Outputs["NewPageCount"].Should().Be(2);
        result.Outputs["PagesDeleted"].Should().Be(1);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task PageDeleteCliTest_RunAsync_WithAttemptToDeleteAllPages_ReturnsFailure()
    {
        var test = new PageDeleteCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 0, 1, 2 };

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot delete all pages");
    }

    [Fact]
    public async Task PageDeleteCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new PageDeleteCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task PageDeleteCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new PageDeleteCliTest();
        var outputFile = Path.Combine(_tempDirectory, "deleted.pdf");
        await File.WriteAllBytesAsync(outputFile, new byte[1024]);

        var result = new CliTestResult
        {
            TestName = "page-delete",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["OriginalPageCount"] = 5,
                ["NewPageCount"] = 3,
                ["PagesDeleted"] = 2,
                ["OperationTimeMs"] = 50.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    #endregion

    #region PageReorderCliTest Tests

    [Fact]
    public void PageReorderCliTest_HasCorrectMetadata()
    {
        var test = new PageReorderCliTest();

        test.Name.Should().Be("page-reorder");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PageReorderCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new PageReorderCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task PageReorderCliTest_RunAsync_WithSuccessfulReorder_ReturnsSuccess()
    {
        var test = new PageReorderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 2 };
        context.Data["TargetIndex"] = 0;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockPageOpsService.Setup(s => s.ReorderPagesAsync(mockDocument, new[] { 2 }, 0))
            .ReturnsAsync(Result.Ok());

        _mockDocumentService.Setup(s => s.SaveDocumentAsync(mockDocument, It.IsAny<string>()))
            .ReturnsAsync(Result.Ok());

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("OutputFile");
        result.Outputs.Should().ContainKey("PageCount");
        result.Outputs.Should().ContainKey("TargetIndex");
        result.Outputs["PageCount"].Should().Be(3);
        result.Outputs["TargetIndex"].Should().Be(0);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task PageReorderCliTest_RunAsync_WithInvalidTargetIndex_ReturnsFailure()
    {
        var test = new PageReorderCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;
        context.Data["PageIndices"] = new[] { 0 };
        context.Data["TargetIndex"] = 999;

        var mockDocument = CreateMockDocument(pageCount: 3);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid target index");
    }

    [Fact]
    public async Task PageReorderCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new PageReorderCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task PageReorderCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new PageReorderCliTest();
        var outputFile = Path.Combine(_tempDirectory, "reordered.pdf");
        await File.WriteAllBytesAsync(outputFile, new byte[1024]);

        var result = new CliTestResult
        {
            TestName = "page-reorder",
            Success = true,
            Outputs =
            {
                ["OutputFile"] = outputFile,
                ["PageCount"] = 5,
                ["OperationTimeMs"] = 50.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    #endregion

    #region AnnotationsCliTest Tests

    [Fact]
    public void AnnotationsCliTest_HasCorrectMetadata()
    {
        var test = new AnnotationsCliTest();

        test.Name.Should().Be("annotations");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnnotationsCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new AnnotationsCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task AnnotationsCliTest_RunAsync_WithAnnotations_ReturnsSuccess()
    {
        var test = new AnnotationsCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 2);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var annotations = new List<Annotation>
        {
            new() { Id = "1", Type = AnnotationType.Highlight, Bounds = new BoundingBox { Left = 10, Top = 20, Right = 30, Bottom = 40 } },
            new() { Id = "2", Type = AnnotationType.Text, Bounds = new BoundingBox { Left = 15, Top = 25, Right = 35, Bottom = 45 } }
        };
        _mockAnnotationService.Setup(s => s.GetAnnotationsAsync(mockDocument, 0))
            .ReturnsAsync(Result.Ok((IReadOnlyList<Annotation>)annotations));
        _mockAnnotationService.Setup(s => s.GetAnnotationsAsync(mockDocument, 1))
            .ReturnsAsync(Result.Ok((IReadOnlyList<Annotation>)new List<Annotation>()));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("ReportFile");
        result.Outputs.Should().ContainKey("JsonOutputFile");
        result.Outputs.Should().ContainKey("TotalAnnotations");
        result.Outputs.Should().ContainKey("PagesWithAnnotations");
        result.Outputs["TotalAnnotations"].Should().Be(2);
        result.Outputs["PagesWithAnnotations"].Should().Be(1);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task AnnotationsCliTest_RunAsync_WithNoAnnotations_ReturnsSuccess()
    {
        var test = new AnnotationsCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 2);
        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        _mockAnnotationService.Setup(s => s.GetAnnotationsAsync(mockDocument, It.IsAny<int>()))
            .ReturnsAsync(Result.Ok((IReadOnlyList<Annotation>)new List<Annotation>()));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs["TotalAnnotations"].Should().Be(0);
        result.Outputs["PagesWithAnnotations"].Should().Be(0);
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task AnnotationsCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new AnnotationsCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task AnnotationsCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new AnnotationsCliTest();
        var reportFile = Path.Combine(_tempDirectory, "annotations_report.txt");
        var jsonFile = Path.Combine(_tempDirectory, "annotations.json");
        await File.WriteAllTextAsync(reportFile, "Annotations report");
        await File.WriteAllTextAsync(jsonFile, "{\"totalAnnotations\": 5}");

        var result = new CliTestResult
        {
            TestName = "annotations",
            Success = true,
            Outputs =
            {
                ["ReportFile"] = reportFile,
                ["JsonOutputFile"] = jsonFile,
                ["TotalAnnotations"] = 5,
                ["PagesWithAnnotations"] = 2,
                ["PageCount"] = 3,
                ["AnnotationsByType"] = new Dictionary<string, int> { { "Highlight", 3 }, { "Text", 2 } },
                ["DetectionTimeMs"] = 30.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    [Fact]
    public async Task AnnotationsCliTest_VerifyAsync_WithInconsistentData_ReturnsFalse()
    {
        var test = new AnnotationsCliTest();
        var reportFile = Path.Combine(_tempDirectory, "annotations_report.txt");
        var jsonFile = Path.Combine(_tempDirectory, "annotations.json");
        await File.WriteAllTextAsync(reportFile, "Annotations report");
        await File.WriteAllTextAsync(jsonFile, "{}");

        var result = new CliTestResult
        {
            TestName = "annotations",
            Success = true,
            Outputs =
            {
                ["ReportFile"] = reportFile,
                ["JsonOutputFile"] = jsonFile,
                ["TotalAnnotations"] = 5,
                ["PagesWithAnnotations"] = 0,
                ["AnnotationsByType"] = new Dictionary<string, int> { { "Highlight", 3 } }
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeFalse();
    }

    #endregion

    #region MetadataCliTest Tests

    [Fact]
    public void MetadataCliTest_HasCorrectMetadata()
    {
        var test = new MetadataCliTest();

        test.Name.Should().Be("metadata");
        test.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MetadataCliTest_RunAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var test = new MetadataCliTest();

        var act = async () => await test.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task MetadataCliTest_RunAsync_WithValidPdf_ReturnsSuccess()
    {
        var test = new MetadataCliTest();
        var context = CreateTestContext();
        var testPdfPath = GetTestPdfPath("multi-page.pdf");
        context.Data["TestPdfPath"] = testPdfPath;

        var mockDocument = CreateMockDocument(pageCount: 3);
        mockDocument.Setup(d => d.FilePath).Returns(testPdfPath);
        mockDocument.Setup(d => d.FileSizeBytes).Returns(1024L);
        mockDocument.Setup(d => d.LoadedAt).Returns(DateTime.UtcNow);

        _mockDocumentService.Setup(s => s.LoadDocumentAsync(testPdfPath))
            .ReturnsAsync(Result.Ok(mockDocument));

        var result = await test.RunAsync(context);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("ReportFile");
        result.Outputs.Should().ContainKey("JsonOutputFile");
        result.Outputs.Should().ContainKey("Metadata");
        result.Outputs.Should().ContainKey("SetFields");
        result.Outputs.Should().ContainKey("TotalFields");
        result.Outputs.Should().ContainKey("NotImplementedFields");
        result.Outputs["ExitCode"].Should().Be(0);
    }

    [Fact]
    public async Task MetadataCliTest_VerifyAsync_WithNullResult_ThrowsArgumentNullException()
    {
        var test = new MetadataCliTest();

        var act = async () => await test.VerifyAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task MetadataCliTest_VerifyAsync_WithValidResult_ReturnsTrue()
    {
        var test = new MetadataCliTest();
        var reportFile = Path.Combine(_tempDirectory, "metadata_report.txt");
        var jsonFile = Path.Combine(_tempDirectory, "metadata.json");
        await File.WriteAllTextAsync(reportFile, "Metadata report");
        await File.WriteAllTextAsync(jsonFile, "{}");

        var metadata = new Dictionary<string, object>
        {
            ["FilePath"] = "test.pdf",
            ["FileName"] = "test.pdf",
            ["FileSize"] = "1 KB",
            ["PageCount"] = 5
        };

        var result = new CliTestResult
        {
            TestName = "metadata",
            Success = true,
            Outputs =
            {
                ["ReportFile"] = reportFile,
                ["JsonOutputFile"] = jsonFile,
                ["Metadata"] = metadata,
                ["SetFields"] = 4,
                ["TotalFields"] = 10,
                ["ExtractionTimeMs"] = 20.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeTrue();
    }

    [Fact]
    public async Task MetadataCliTest_VerifyAsync_WithMissingRequiredFields_ReturnsFalse()
    {
        var test = new MetadataCliTest();
        var reportFile = Path.Combine(_tempDirectory, "metadata_report.txt");
        var jsonFile = Path.Combine(_tempDirectory, "metadata.json");
        await File.WriteAllTextAsync(reportFile, "Metadata report");
        await File.WriteAllTextAsync(jsonFile, "{}");

        var metadata = new Dictionary<string, object>
        {
            ["FileName"] = "test.pdf"
        };

        var result = new CliTestResult
        {
            TestName = "metadata",
            Success = true,
            Outputs =
            {
                ["ReportFile"] = reportFile,
                ["JsonOutputFile"] = jsonFile,
                ["Metadata"] = metadata,
                ["SetFields"] = 1,
                ["TotalFields"] = 10,
                ["ExtractionTimeMs"] = 20.0
            }
        };

        var verified = await test.VerifyAsync(result);

        verified.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private CliTestContext CreateTestContext()
    {
        return new CliTestContext(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _tempDirectory
        );
    }

    private PdfDocument CreateMockDocument(int pageCount)
    {
        return new PdfDocument
        {
            PageCount = pageCount,
            FilePath = "test.pdf",
            FileSizeBytes = 1024L,
            LoadedAt = DateTime.UtcNow,
            Handle = Mock.Of<IDisposable>()
        };
        return mockDocument;
    }

    private string GetTestPdfPath(string filename)
    {
        var fixturesPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures",
            filename
        );
        return Path.GetFullPath(fixturesPath);
    }

    #endregion
}
