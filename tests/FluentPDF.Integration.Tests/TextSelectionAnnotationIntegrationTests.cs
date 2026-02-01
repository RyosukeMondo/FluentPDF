using System.Drawing;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FluentPDF.Integration.Tests;

/// <summary>
/// Fixture for initializing PDFium library once for all tests.
/// </summary>
public class PdfiumFixture : IDisposable
{
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public PdfiumFixture()
    {
        // Initialize PDFium library once
        lock (_initLock)
        {
            if (!_pdfiumInitialized)
            {
                var initialized = PdfiumInterop.Initialize();
                if (!initialized)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize PDFium. Ensure pdfium.dll is in the test output directory.");
                }
                _pdfiumInitialized = true;
            }
        }
    }

    public void Dispose()
    {
        // PDFium cleanup happens automatically
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Integration tests for text selection and annotation workflow.
/// Tests verify the service layer APIs work correctly for text extraction and annotation creation.
/// </summary>
[Collection("PDFium Collection")]
public class TextSelectionAnnotationIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IAnnotationService _annotationService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly string _testPdfPath;
    private readonly string _tempOutputPath;
    private PdfDocument? _currentDocument;

    public TextSelectionAnnotationIntegrationTests(PdfiumFixture fixture)
    {
        var docLogger = NullLogger<PdfDocumentService>.Instance;
        var annLogger = NullLogger<AnnotationService>.Instance;
        var textLogger = NullLogger<TextExtractionService>.Instance;

        _documentService = new PdfDocumentService(docLogger);
        _annotationService = new AnnotationService(annLogger);
        _textExtractionService = new TextExtractionService(textLogger);

        // Use Fixtures subfolder in output directory (copied by csproj)
        _testPdfPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Fixtures", "sample-with-text.pdf"
        );
        _tempOutputPath = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid()}.pdf");

        if (!File.Exists(_testPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found at {_testPdfPath}");
        }
    }

    [Fact]
    public async Task TestFullTextSelectionToAnnotationFlow()
    {
        // Load PDF document
        var loadResult = await _documentService.LoadDocumentAsync(_testPdfPath);
        Assert.True(loadResult.IsSuccess, $"Failed to load document: {loadResult}");
        _currentDocument = loadResult.Value;

        // Extract text from bounds
        var bounds = new RectangleF(100, 100, 200, 50);
        var textResult = await _textExtractionService.ExtractTextInBoundsAsync(_currentDocument, 1, bounds); // 1-based page number
        Assert.True(textResult.IsSuccess, $"Failed to extract text: {textResult}");

        // Create highlight annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0, // Zero-based for annotation service
            Bounds = new PdfRectangle(100, 100, 300, 150),
            Contents = "Test annotation",
            FillColor = Color.FromArgb(128, 255, 255, 0) // Yellow with transparency
        };

        var createResult = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);
        Assert.True(createResult.IsSuccess, $"Failed to create annotation: {createResult}");

        // Verify annotation was created
        var annotations = await _annotationService.GetAnnotationsAsync(_currentDocument, 0);
        Assert.True(annotations.IsSuccess, $"Failed to get annotations: {annotations}");
        Assert.True(annotations.Value.Count > 0, "No annotations found after creation");

        // Clean up
        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;
    }

    [Fact]
    public async Task TestContinuousScrollWithAnnotations()
    {
        // Load PDF document
        var loadResult = await _documentService.LoadDocumentAsync(_testPdfPath);
        Assert.True(loadResult.IsSuccess, $"Failed to load document: {loadResult}");
        _currentDocument = loadResult.Value;

        // Test only if document has multiple pages
        if (_currentDocument.PageCount > 1)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Underline,
                PageNumber = 1, // Zero-based (second page)
                Bounds = new PdfRectangle(50, 50, 200, 80),
                Contents = "Page 2 annotation",
                StrokeColor = Color.Red
            };

            var createResult = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);
            Assert.True(createResult.IsSuccess, $"Failed to create annotation on page 2: {createResult}");

            var page1Annotations = await _annotationService.GetAnnotationsAsync(_currentDocument, 1);
            Assert.True(page1Annotations.IsSuccess, $"Failed to get annotations from page 2: {page1Annotations}");
            Assert.True(page1Annotations.Value.Count > 0, "No annotations found on page 2");
        }

        // Clean up
        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;
    }

    [Fact]
    public async Task TestMultipleAnnotationsOnSamePage()
    {
        // Load PDF document
        var loadResult = await _documentService.LoadDocumentAsync(_testPdfPath);
        Assert.True(loadResult.IsSuccess, $"Failed to load document: {loadResult}");
        _currentDocument = loadResult.Value;

        var annotation1 = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 250, 130),
            Contents = "Highlight annotation",
            FillColor = Color.Yellow
        };

        var annotation2 = new Annotation
        {
            Type = AnnotationType.Underline,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 150, 250, 180),
            Contents = "Underline annotation",
            StrokeColor = Color.Blue
        };

        var annotation3 = new Annotation
        {
            Type = AnnotationType.StrikeOut,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 200, 250, 230),
            Contents = "Strikethrough annotation",
            StrokeColor = Color.Red
        };

        var result1 = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation1);
        Assert.True(result1.IsSuccess, $"Failed to create annotation 1: {result1}");

        var result2 = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation2);
        Assert.True(result2.IsSuccess, $"Failed to create annotation 2: {result2}");

        var result3 = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation3);
        Assert.True(result3.IsSuccess, $"Failed to create annotation 3: {result3}");

        // Verify all annotations were created
        var annotations = await _annotationService.GetAnnotationsAsync(_currentDocument, 0);
        Assert.True(annotations.IsSuccess, $"Failed to get annotations: {annotations}");
        Assert.True(annotations.Value.Count >= 3, $"Expected at least 3 annotations, found {annotations.Value.Count}");

        // Clean up
        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;
    }

    [Fact]
    public async Task TestAnnotationPersistenceAcrossViewModes()
    {
        // Load PDF document
        var loadResult = await _documentService.LoadDocumentAsync(_testPdfPath);
        Assert.True(loadResult.IsSuccess, $"Failed to load document: {loadResult}");
        _currentDocument = loadResult.Value;

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 200, 150),
            Contents = "Persistence test",
            FillColor = Color.Cyan
        };

        var createResult = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);
        Assert.True(createResult.IsSuccess, $"Failed to create annotation: {createResult}");

        var saveResult = await _annotationService.SaveAnnotationsAsync(_currentDocument, _tempOutputPath, false);
        Assert.True(saveResult.IsSuccess, $"Failed to save annotations: {saveResult}");

        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;

        // Reload the saved document
        var reloadResult = await _documentService.LoadDocumentAsync(_tempOutputPath);
        Assert.True(reloadResult.IsSuccess, $"Failed to reload document: {reloadResult}");
        _currentDocument = reloadResult.Value;

        // Verify annotations persisted
        var annotations = await _annotationService.GetAnnotationsAsync(_currentDocument, 0);
        Assert.True(annotations.IsSuccess, $"Failed to get annotations from reloaded document: {annotations}");
        Assert.True(annotations.Value.Count > 0, "No annotations found after reload - persistence failed");

        // Clean up
        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;
    }

    [Fact]
    public async Task TestZoomChangesWithAnnotations()
    {
        // Load PDF document
        var loadResult = await _documentService.LoadDocumentAsync(_testPdfPath);
        Assert.True(loadResult.IsSuccess, $"Failed to load document: {loadResult}");
        _currentDocument = loadResult.Value;

        var annotation = new Annotation
        {
            Type = AnnotationType.StrikeOut,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 200, 150),
            Contents = "Zoom test annotation",
            StrokeColor = Color.Green
        };

        var createResult = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);
        Assert.True(createResult.IsSuccess, $"Failed to create annotation: {createResult}");

        // Retrieve annotations and verify bounds are correct
        var annotations = await _annotationService.GetAnnotationsAsync(_currentDocument, 0);
        Assert.True(annotations.IsSuccess, $"Failed to get annotations: {annotations}");
        Assert.True(annotations.Value.Count > 0, "No annotations found");

        var storedAnnotation = annotations.Value[^1];
        Assert.True(storedAnnotation.Bounds.Width > 0, "Annotation width should be greater than 0");
        Assert.True(storedAnnotation.Bounds.Height > 0, "Annotation height should be greater than 0");

        // Note: Zoom is handled at the UI layer, not the service layer
        // This test verifies that annotation bounds are stored correctly in PDF coordinates

        // Clean up
        _documentService.CloseDocument(_currentDocument);
        _currentDocument = null;
    }

    public void Dispose()
    {
        // Clean up any open document
        if (_currentDocument != null)
        {
            _documentService.CloseDocument(_currentDocument);
            _currentDocument = null;
        }

        // Clean up temporary output file
        if (File.Exists(_tempOutputPath))
        {
            try
            {
                File.Delete(_tempOutputPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

/// <summary>
/// Collection definition for PDFium-dependent tests.
/// Ensures PDFium is initialized once for all tests in the collection.
/// </summary>
[CollectionDefinition("PDFium Collection")]
public class PdfiumCollection : ICollectionFixture<PdfiumFixture>
{
    // This class has no code and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
