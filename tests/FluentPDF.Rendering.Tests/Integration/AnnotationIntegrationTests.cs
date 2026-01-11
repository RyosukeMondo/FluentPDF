using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using System.Drawing;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for annotation operations using real PDFium library.
/// These tests verify the complete workflow: create, save, reload, and verify annotations.
/// Tests all annotation types and ensures lossless persistence.
/// NOTE: These tests require PDFium native library and will only run on Windows.
/// On Linux/macOS, PDFium initialization will fail and tests will be skipped gracefully.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnnotationIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IAnnotationService _annotationService;
    private readonly string _fixturesPath;
    private readonly string _tempPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private readonly List<string> _filesToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public AnnotationIntegrationTests()
    {
        // Initialize PDFium once for all tests
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

        // Setup services
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();
        var annotationLogger = new LoggerFactory().CreateLogger<AnnotationService>();

        _documentService = new PdfDocumentService(documentLogger);
        _annotationService = new AnnotationService(annotationLogger);

        // Setup paths
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _tempPath = Path.Combine(Path.GetTempPath(), "FluentPDF_AnnotationTests");
        Directory.CreateDirectory(_tempPath);

        _documentsToCleanup = new List<PdfDocument>();
        _filesToCleanup = new List<string>();
    }

    public void Dispose()
    {
        // Clean up documents
        foreach (var doc in _documentsToCleanup)
        {
            try
            {
                _documentService.CloseDocument(doc);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up temp files
        foreach (var file in _filesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                var backupFile = file + ".bak";
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up temp directory
        try
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Highlight Annotation Tests

    [Fact]
    public async Task CreateHighlightAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"highlight_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create a highlight annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 300, Bottom = 120 },
            FillColor = Color.Yellow,
            Opacity = 0.5,
            Contents = "Test highlight",
            Author = "Integration Test"
        };

        // Act - Create annotation
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.Should().NotBeNull();

        // Save annotations
        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        // Close and reload
        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert - Verify annotation persisted
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);

        var savedAnnotation = annotationsResult.Value[0];
        savedAnnotation.Type.Should().Be(AnnotationType.Highlight);
        savedAnnotation.PageNumber.Should().Be(0);
        savedAnnotation.Contents.Should().Be("Test highlight");
    }

    #endregion

    #region Underline Annotation Tests

    [Fact]
    public async Task CreateUnderlineAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"underline_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create an underline annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Underline,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 200, Right = 300, Bottom = 215 },
            StrokeColor = Color.Blue,
            Contents = "Underline test",
            Author = "Integration Test"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        annotationsResult.Value[0].Type.Should().Be(AnnotationType.Underline);
    }

    #endregion

    #region StrikeOut Annotation Tests

    [Fact]
    public async Task CreateStrikeOutAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"strikeout_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create a strikeout annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.StrikeOut,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 300, Right = 300, Bottom = 315 },
            StrokeColor = Color.Red,
            Contents = "Strikeout test"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        annotationsResult.Value[0].Type.Should().Be(AnnotationType.StrikeOut);
    }

    #endregion

    #region Text (Comment) Annotation Tests

    [Fact]
    public async Task CreateTextAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"text_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create a text annotation (comment/sticky note)
        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 50, Top = 50, Right = 70, Bottom = 70 },
            FillColor = Color.LightYellow,
            Contents = "This is a comment note",
            Author = "Integration Test"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        var savedAnnotation = annotationsResult.Value[0];
        savedAnnotation.Type.Should().Be(AnnotationType.Text);
        savedAnnotation.Contents.Should().Be("This is a comment note");
    }

    #endregion

    #region Square Annotation Tests

    [Fact]
    public async Task CreateSquareAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"square_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create a square (rectangle) annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Square,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 150, Top = 150, Right = 350, Bottom = 250 },
            StrokeColor = Color.Red,
            StrokeWidth = 2.0,
            FillColor = Color.FromArgb(50, 255, 0, 0), // Semi-transparent red
            Contents = "Rectangle annotation"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        annotationsResult.Value[0].Type.Should().Be(AnnotationType.Square);
    }

    #endregion

    #region Circle Annotation Tests

    [Fact]
    public async Task CreateCircleAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"circle_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create a circle annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Circle,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 200, Top = 200, Right = 300, Bottom = 300 },
            StrokeColor = Color.Blue,
            StrokeWidth = 3.0,
            FillColor = Color.FromArgb(50, 0, 0, 255), // Semi-transparent blue
            Contents = "Circle annotation"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        annotationsResult.Value[0].Type.Should().Be(AnnotationType.Circle);
    }

    #endregion

    #region Ink (Freehand) Annotation Tests

    [Fact]
    public async Task CreateInkAnnotation_SaveAndReload_PersistsCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"ink_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create an ink annotation (freehand drawing)
        var inkPoints = new List<PointF>
        {
            new PointF(100, 400),
            new PointF(150, 420),
            new PointF(200, 400),
            new PointF(250, 420),
            new PointF(300, 400)
        };

        var annotation = new Annotation
        {
            Type = AnnotationType.Ink,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 400, Right = 300, Bottom = 420 },
            StrokeColor = Color.Green,
            StrokeWidth = 2.5,
            InkPoints = inkPoints,
            Contents = "Freehand drawing"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        var savedAnnotation = annotationsResult.Value[0];
        savedAnnotation.Type.Should().Be(AnnotationType.Ink);
        savedAnnotation.InkPoints.Should().NotBeEmpty();
    }

    #endregion

    #region Multiple Annotations Tests

    [Fact]
    public async Task CreateMultipleAnnotations_SaveAndReload_AllPersistCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"multiple_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create multiple different annotations
        var annotations = new List<Annotation>
        {
            new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50, Top = 50, Right = 150, Bottom = 70 },
                FillColor = Color.Yellow
            },
            new Annotation
            {
                Type = AnnotationType.Text,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 200, Top = 50, Right = 220, Bottom = 70 },
                Contents = "Note 1"
            },
            new Annotation
            {
                Type = AnnotationType.Square,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 150 },
                StrokeColor = Color.Red
            },
            new Annotation
            {
                Type = AnnotationType.Circle,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 250, Top = 100, Right = 350, Bottom = 200 },
                StrokeColor = Color.Blue
            }
        };

        // Act - Create all annotations
        foreach (var annotation in annotations)
        {
            var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
            createResult.IsSuccess.Should().BeTrue();
        }

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(4);

        // Verify all types are present
        var types = annotationsResult.Value.Select(a => a.Type).ToList();
        types.Should().Contain(AnnotationType.Highlight);
        types.Should().Contain(AnnotationType.Text);
        types.Should().Contain(AnnotationType.Square);
        types.Should().Contain(AnnotationType.Circle);
    }

    #endregion

    #region Annotation Update Tests

    [Fact]
    public async Task UpdateAnnotation_SaveAndReload_ChangesPersistedCorrectly()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"update_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create initial annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 120, Bottom = 120 },
            Contents = "Original content"
        };

        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();
        var createdAnnotation = createResult.Value;

        // Act - Update annotation
        createdAnnotation.Contents = "Updated content";
        var updateResult = await _annotationService.UpdateAnnotationAsync(document, createdAnnotation);
        updateResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().HaveCount(1);
        annotationsResult.Value[0].Contents.Should().Be("Updated content");
    }

    #endregion

    #region Annotation Deletion Tests

    [Fact]
    public async Task DeleteAnnotation_SaveAndReload_AnnotationRemoved()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"delete_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 120 },
            FillColor = Color.Yellow
        };

        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        // Act - Delete annotation
        var deleteResult = await _annotationService.DeleteAnnotationAsync(document, 0, 0);
        deleteResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert
        var annotationsResult = await _annotationService.GetAnnotationsAsync(reloadedDoc, 0);
        annotationsResult.IsSuccess.Should().BeTrue();
        annotationsResult.Value.Should().BeEmpty();
    }

    #endregion

    #region Backup and Restore Tests

    [Fact]
    public async Task SaveAnnotations_WithBackup_CreatesBackupFile()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"backup_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Create annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 120 },
            FillColor = Color.Yellow
        };

        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        // Act - Save with backup
        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf, createBackup: true);
        saveResult.IsSuccess.Should().BeTrue();

        // Assert
        var backupFile = testPdf + ".bak";
        File.Exists(backupFile).Should().BeTrue("Backup file should be created");
        _filesToCleanup.Add(backupFile);
    }

    #endregion

    #region Lossless Save Tests

    [Fact]
    public async Task SaveAnnotations_PreservesOriginalContent_LosslessSave()
    {
        // Arrange
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"lossless_test_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);

        var originalSize = new FileInfo(testPdf).Length;

        var loadResult = await _documentService.LoadDocumentAsync(testPdf);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);

        // Get original page count
        var originalPageCount = document.PageCount;

        // Create annotation
        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 50, Top = 50, Right = 70, Bottom = 70 },
            Contents = "Test note"
        };

        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        // Act - Save
        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        _documentService.CloseDocument(document);
        _documentsToCleanup.Remove(document);

        // Reload
        var reloadResult = await _documentService.LoadDocumentAsync(testPdf);
        reloadResult.IsSuccess.Should().BeTrue();
        var reloadedDoc = reloadResult.Value;
        _documentsToCleanup.Add(reloadedDoc);

        // Assert - Document integrity preserved
        reloadedDoc.PageCount.Should().Be(originalPageCount, "Page count should be preserved");

        // File size should increase (annotation added) but not decrease
        var newSize = new FileInfo(testPdf).Length;
        newSize.Should().BeGreaterOrEqualTo(originalSize, "File should not lose content");
    }

    #endregion
}
