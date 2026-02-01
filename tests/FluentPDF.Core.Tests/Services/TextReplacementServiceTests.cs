using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentPDF.Core.Tests.Services;

/// <summary>
/// Unit tests for TextReplacementService.
/// Tests text replacement functionality with mock dependencies.
/// </summary>
public class TextReplacementServiceTests
{
    private readonly Mock<ITextSearchService> _mockSearchService;
    private readonly Mock<IAnnotationService> _mockAnnotationService;
    private readonly Mock<ILogger<TextReplacementService>> _mockLogger;
    private readonly TextReplacementService _service;

    public TextReplacementServiceTests()
    {
        _mockSearchService = new Mock<ITextSearchService>();
        _mockAnnotationService = new Mock<IAnnotationService>();
        _mockLogger = new Mock<ILogger<TextReplacementService>>();
        _service = new TextReplacementService(
            _mockSearchService.Object,
            _mockAnnotationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ReplaceAsync_WithValidMatch_ReturnsSuccessResult()
    {
        // Arrange
        var document = CreateMockDocument();
        var match = new SearchMatch(
            PageNumber: 1,
            CharIndex: 10,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(10, 10, 50, 20)
        );
        var replacementText = "world";

        _mockAnnotationService
            .Setup(x => x.CreateAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<Annotation>()))
            .ReturnsAsync(FluentResults.Result.Ok(new Annotation
            {
                PageNumber = 0,
                Type = AnnotationType.Square,
                Bounds = match.BoundingBox,
                FillColor = System.Drawing.Color.White,
                Contents = string.Empty,
                Author = "FluentPDF",
                Opacity = 1.0,
                StrokeWidth = 0
            }));

        _mockAnnotationService
            .Setup(x => x.GetAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>()))
            .ReturnsAsync(FluentResults.Result.Ok(new List<Annotation>()));

        // Act
        var result = await _service.ReplaceAsync(document, match, replacementText, preview: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(replacementText, result.Value.ReplacementText);
        Assert.Equal(match.Text, result.Value.OriginalText);
        Assert.Equal(0, result.Value.PageNumber); // 0-based
    }

    [Fact]
    public async Task ReplaceAsync_WithPreviewMode_DoesNotCreateAnnotations()
    {
        // Arrange
        var document = CreateMockDocument();
        var match = new SearchMatch(
            PageNumber: 1,
            CharIndex: 10,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(10, 10, 50, 20)
        );
        var replacementText = "world";

        // Act
        var result = await _service.ReplaceAsync(document, match, replacementText, preview: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-1, result.Value.AnnotationIndex); // Not created in preview
        _mockAnnotationService.Verify(
            x => x.CreateAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<Annotation>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceAsync_WithInvalidMatch_ReturnsFailure()
    {
        // Arrange
        var document = CreateMockDocument();
        var invalidMatch = new SearchMatch(
            PageNumber: -1, // Invalid page number
            CharIndex: 10,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(10, 10, 50, 20)
        );
        var replacementText = "world";

        // Act
        var result = await _service.ReplaceAsync(document, invalidMatch, replacementText);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ReplaceAllAsync_WithMatches_ReplacesAllOccurrences()
    {
        // Arrange
        var document = CreateMockDocument();
        var findText = "hello";
        var replaceText = "world";
        var searchOptions = new SearchOptions { CaseSensitive = false };

        var matches = new List<SearchMatch>
        {
            new SearchMatch(1, 10, 5, "hello", new PdfRectangle(10, 10, 50, 20)),
            new SearchMatch(1, 100, 5, "hello", new PdfRectangle(100, 10, 140, 20)),
            new SearchMatch(2, 50, 5, "hello", new PdfRectangle(50, 10, 90, 20))
        };

        _mockSearchService
            .Setup(x => x.SearchAsync(
                It.IsAny<PdfDocument>(),
                findText,
                searchOptions,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FluentResults.Result.Ok(matches));

        _mockAnnotationService
            .Setup(x => x.CreateAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<Annotation>()))
            .ReturnsAsync(FluentResults.Result.Ok(new Annotation
            {
                PageNumber = 0,
                Type = AnnotationType.Square,
                Bounds = new PdfRectangle(10, 10, 50, 20),
                FillColor = System.Drawing.Color.White,
                Contents = string.Empty,
                Author = "FluentPDF",
                Opacity = 1.0,
                StrokeWidth = 0
            }));

        _mockAnnotationService
            .Setup(x => x.GetAnnotationsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>()))
            .ReturnsAsync(FluentResults.Result.Ok(new List<Annotation>()));

        // Act
        var result = await _service.ReplaceAllAsync(
            document,
            findText,
            replaceText,
            searchOptions,
            preview: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.All(result.Value, r => Assert.Equal(replaceText, r.ReplacementText));
    }

    [Fact]
    public async Task ReplaceAllAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var document = CreateMockDocument();
        var findText = "notfound";
        var replaceText = "world";

        _mockSearchService
            .Setup(x => x.SearchAsync(
                It.IsAny<PdfDocument>(),
                findText,
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FluentResults.Result.Ok(new List<SearchMatch>()));

        // Act
        var result = await _service.ReplaceAllAsync(document, findText, replaceText);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task UndoReplacementsAsync_WithValidReplacements_DeletesAnnotations()
    {
        // Arrange
        var document = CreateMockDocument();
        var replacements = new List<TextReplacement>
        {
            new TextReplacement(
                Match: new SearchMatch(1, 10, 5, "hello", new PdfRectangle(10, 10, 50, 20)),
                ReplacementText: "world",
                PageNumber: 0,
                AnnotationIndex: 5,
                Timestamp: DateTime.UtcNow
            ),
            new TextReplacement(
                Match: new SearchMatch(1, 100, 5, "hello", new PdfRectangle(100, 10, 140, 20)),
                ReplacementText: "world",
                PageNumber: 0,
                AnnotationIndex: 3,
                Timestamp: DateTime.UtcNow
            )
        };

        _mockAnnotationService
            .Setup(x => x.DeleteAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(FluentResults.Result.Ok());

        // Act
        var result = await _service.UndoReplacementsAsync(document, replacements);

        // Assert
        Assert.True(result.IsSuccess);
        // Each replacement has 2 annotations (cover + text), should delete 4 total
        _mockAnnotationService.Verify(
            x => x.DeleteAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task UndoReplacementsAsync_WithPreviewReplacements_SkipsDeletion()
    {
        // Arrange
        var document = CreateMockDocument();
        var previewReplacements = new List<TextReplacement>
        {
            new TextReplacement(
                Match: new SearchMatch(1, 10, 5, "hello", new PdfRectangle(10, 10, 50, 20)),
                ReplacementText: "world",
                PageNumber: 0,
                AnnotationIndex: -1, // Preview mode annotation index
                Timestamp: DateTime.UtcNow
            )
        };

        // Act
        var result = await _service.UndoReplacementsAsync(document, previewReplacements);

        // Assert
        Assert.True(result.IsSuccess);
        _mockAnnotationService.Verify(
            x => x.DeleteAnnotationAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    private static PdfDocument CreateMockDocument()
    {
        return new PdfDocument
        {
            Handle = Mock.Of<IDisposable>(),
            FilePath = "test.pdf",
            PageCount = 10,
            LoadedAt = DateTime.UtcNow,
            FileSizeBytes = 1024
        };
    }
}
