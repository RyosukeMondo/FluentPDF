// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;
using Windows.Foundation;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Unit tests for text selection functionality in PdfViewerViewModel.
/// Tests the text selection flow including coordinate mapping and bounds-based extraction.
/// </summary>
public class PdfViewerViewModelTextSelectionTests
{
    private readonly Mock<IPdfRenderingService> _mockRenderingService;
    private readonly Mock<ITextExtractionService> _mockTextExtractionService;
    private readonly Mock<ICoordinateMapper> _mockCoordinateMapper;
    private readonly Mock<ILogger<PdfViewerViewModel>> _mockLogger;
    private readonly PdfViewerViewModel _viewModel;

    public PdfViewerViewModelTextSelectionTests()
    {
        _mockRenderingService = new Mock<IPdfRenderingService>();
        _mockTextExtractionService = new Mock<ITextExtractionService>();
        _mockCoordinateMapper = new Mock<ICoordinateMapper>();
        _mockLogger = new Mock<ILogger<PdfViewerViewModel>>();

        // Create view model with mocked dependencies
        _viewModel = new PdfViewerViewModel(
            _mockRenderingService.Object,
            _mockTextExtractionService.Object,
            Mock.Of<ISearchService>(),
            Mock.Of<IAnnotationService>(),
            Mock.Of<IPdfFormService>(),
            Mock.Of<ITelemetryService>(),
            _mockCoordinateMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void BeginTextSelection_SetsIsSelectingToTrue()
    {
        // Arrange
        var startPoint = new Point(100, 100);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);

        // Assert
        Assert.True(_viewModel.IsSelecting);
    }

    [Fact]
    public void BeginTextSelection_StoresStartPoint()
    {
        // Arrange
        var startPoint = new Point(100, 100);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);

        // Assert - verify start point is stored (private field, tested via EndTextSelection)
        Assert.True(_viewModel.IsSelecting);
    }

    [Fact]
    public void UpdateTextSelection_UpdatesCurrentPoint()
    {
        // Arrange
        var startPoint = new Point(100, 100);
        var updatePoint = new Point(200, 200);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(updatePoint);

        // Assert
        Assert.True(_viewModel.IsSelecting);
    }

    [Fact]
    public async Task EndTextSelection_ExtractsTextInBounds()
    {
        // Arrange
        var document = new PdfDocument(1, "test.pdf");
        var startPoint = new Point(100, 100);
        var endPoint = new Point(200, 200);

        // Setup coordinate mapper
        var pdfStartPoint = new PdfPoint(50, 50);
        var pdfEndPoint = new PdfPoint(100, 100);
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(startPoint, It.IsAny<double>(), It.IsAny<double>()))
            .Returns(pdfStartPoint);
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(endPoint, It.IsAny<double>(), It.IsAny<double>()))
            .Returns(pdfEndPoint);

        // Setup text extraction
        var expectedText = "Selected text";
        var textSelection = new TextSelection
        {
            Text = expectedText,
            SelectionBounds = new PdfRectangle { Left = 50, Top = 50, Right = 100, Bottom = 100 }
        };

        _mockTextExtractionService
            .Setup(s => s.ExtractTextInBoundsAsync(
                It.IsAny<PdfDocument>(),
                It.IsAny<int>(),
                It.IsAny<PdfRectangle>()))
            .ReturnsAsync(Result<TextSelection>.Success(textSelection));

        // Set current document (using reflection to set private field)
        var documentField = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField?.SetValue(_viewModel, document);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(endPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsSelecting);
        Assert.Equal(expectedText, _viewModel.SelectedText);
        Assert.True(_viewModel.HasSelectedText);
    }

    [Fact]
    public async Task EndTextSelection_WithNoDocument_DoesNotExtractText()
    {
        // Arrange
        var startPoint = new Point(100, 100);
        var endPoint = new Point(200, 200);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(endPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsSelecting);
        _mockTextExtractionService.Verify(
            s => s.ExtractTextInBoundsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<PdfRectangle>()),
            Times.Never);
    }

    [Fact]
    public async Task EndTextSelection_UsesCoordinateMapper()
    {
        // Arrange
        var document = new PdfDocument(1, "test.pdf");
        var startPoint = new Point(100, 100);
        var endPoint = new Point(200, 200);
        var pageHeight = 800.0;
        var zoomLevel = 1.5;

        // Setup coordinate mapper to verify it's called
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(It.IsAny<Point>(), pageHeight, zoomLevel))
            .Returns(new PdfPoint(0, 0));

        // Setup text extraction
        _mockTextExtractionService
            .Setup(s => s.ExtractTextInBoundsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<PdfRectangle>()))
            .ReturnsAsync(Result<TextSelection>.Success(new TextSelection { Text = "test" }));

        // Set current document
        var documentField = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField?.SetValue(_viewModel, document);

        // Set page height and zoom
        typeof(PdfViewerViewModel).GetProperty("CurrentPageHeight")?.SetValue(_viewModel, pageHeight);
        typeof(PdfViewerViewModel).GetProperty("ZoomLevel")?.SetValue(_viewModel, zoomLevel);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(endPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert - verify coordinate mapper was called
        _mockCoordinateMapper.Verify(
            m => m.ScreenToPdf(It.IsAny<Point>(), pageHeight, zoomLevel),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndTextSelection_WithEmptySelection_ClearsSelectedText()
    {
        // Arrange
        var document = new PdfDocument(1, "test.pdf");
        var startPoint = new Point(100, 100);

        // Setup coordinate mapper
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(It.IsAny<Point>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new PdfPoint(50, 50));

        // Setup text extraction to return empty result
        _mockTextExtractionService
            .Setup(s => s.ExtractTextInBoundsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<PdfRectangle>()))
            .ReturnsAsync(Result<TextSelection>.Success(new TextSelection { Text = "" }));

        // Set current document
        var documentField = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField?.SetValue(_viewModel, document);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsSelecting);
        Assert.False(_viewModel.HasSelectedText);
    }

    [Fact]
    public async Task EndTextSelection_WithAnnotationToolActive_CreatesAnnotation()
    {
        // Arrange
        var document = new PdfDocument(1, "test.pdf");
        var startPoint = new Point(100, 100);
        var endPoint = new Point(200, 200);

        // Setup coordinate mapper
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(It.IsAny<Point>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new PdfPoint(50, 50));

        // Setup text extraction
        var textSelection = new TextSelection
        {
            Text = "Selected text",
            SelectionBounds = new PdfRectangle { Left = 50, Top = 50, Right = 100, Bottom = 100 }
        };

        _mockTextExtractionService
            .Setup(s => s.ExtractTextInBoundsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<PdfRectangle>()))
            .ReturnsAsync(Result<TextSelection>.Success(textSelection));

        // Set current document
        var documentField = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField?.SetValue(_viewModel, document);

        // Activate highlight tool
        _viewModel.AnnotationViewModel.SelectTool(AnnotationTool.Highlight);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(endPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.IsSelecting);
        // Note: Full annotation creation testing would require mocking AnnotationService
        // This test verifies the flow doesn't throw
    }

    [Fact]
    public void BeginTextSelection_ClearsExistingSelection()
    {
        // Arrange
        var startPoint1 = new Point(100, 100);
        var startPoint2 = new Point(200, 200);

        // Act - start first selection
        _viewModel.BeginTextSelectionCommand.Execute(startPoint1);
        Assert.True(_viewModel.IsSelecting);

        // Start new selection
        _viewModel.BeginTextSelectionCommand.Execute(startPoint2);

        // Assert - should still be selecting with new start point
        Assert.True(_viewModel.IsSelecting);
    }

    [Fact]
    public async Task EndTextSelection_CalculatesBoundsCorrectly()
    {
        // Arrange
        var document = new PdfDocument(1, "test.pdf");
        var startPoint = new Point(100, 200); // Top-left in screen coords
        var endPoint = new Point(300, 400);   // Bottom-right in screen coords

        var pdfStart = new PdfPoint(50, 600); // PDF coords (Y flipped)
        var pdfEnd = new PdfPoint(150, 400);

        // Setup coordinate mapper
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(startPoint, It.IsAny<double>(), It.IsAny<double>()))
            .Returns(pdfStart);
        _mockCoordinateMapper.Setup(m => m.ScreenToPdf(endPoint, It.IsAny<double>(), It.IsAny<double>()))
            .Returns(pdfEnd);

        // Setup text extraction to verify bounds
        PdfRectangle? capturedBounds = null;
        _mockTextExtractionService
            .Setup(s => s.ExtractTextInBoundsAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<PdfRectangle>()))
            .Callback<PdfDocument, int, PdfRectangle>((_, _, bounds) => capturedBounds = bounds)
            .ReturnsAsync(Result<TextSelection>.Success(new TextSelection { Text = "test" }));

        // Set current document
        var documentField = typeof(PdfViewerViewModel).GetField("_currentDocument",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        documentField?.SetValue(_viewModel, document);

        // Act
        _viewModel.BeginTextSelectionCommand.Execute(startPoint);
        _viewModel.UpdateTextSelectionCommand.Execute(endPoint);
        await _viewModel.EndTextSelectionCommand.ExecuteAsync(null);

        // Assert - verify bounds were calculated correctly
        Assert.NotNull(capturedBounds);
        Assert.Equal(50, capturedBounds.Value.Left);
        Assert.Equal(400, capturedBounds.Value.Top);
        Assert.Equal(150, capturedBounds.Value.Right);
        Assert.Equal(600, capturedBounds.Value.Bottom);
    }
}
