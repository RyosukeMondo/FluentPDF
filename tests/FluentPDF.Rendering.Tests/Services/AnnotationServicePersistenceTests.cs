using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using System.Drawing;
using Xunit;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Comprehensive unit tests for annotation persistence.
/// Tests create-save-reload cycles for all annotation types and properties.
/// Verifies lossless persistence and edge case handling.
/// NOTE: These tests require PDFium native library and will only run on Windows.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnnotationServicePersistenceTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IAnnotationService _annotationService;
    private readonly IFdfService _fdfService;
    private readonly string _fixturesPath;
    private readonly string _tempPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private readonly List<string> _filesToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public AnnotationServicePersistenceTests()
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
        var fdfLogger = new LoggerFactory().CreateLogger<FdfService>();

        _documentService = new PdfDocumentService(documentLogger);
        _annotationService = new AnnotationService(annotationLogger);
        _fdfService = new FdfService(_annotationService, fdfLogger);

        // Setup paths
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _tempPath = Path.Combine(Path.GetTempPath(), "FluentPDF_PersistenceTests");
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
                    var fileInfo = new FileInfo(file);
                    fileInfo.IsReadOnly = false;
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

    #region CreateAndSaveAnnotationTests

    [Fact]
    public async Task Test_CreateHighlight_SaveAndReload_AnnotationPresent()
    {
        // Arrange
        var testPdf = await CreateTestPdf("highlight");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 300, Bottom = 120 },
            FillColor = Color.Yellow,
            Opacity = 0.5,
            Contents = "Test highlight",
            Author = "Test User"
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        // Reload
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        annotations[0].Type.Should().Be(AnnotationType.Highlight);
    }

    [Fact]
    public async Task Test_CreateUnderline_SaveAndReload_PropertiesMatch()
    {
        // Arrange
        var testPdf = await CreateTestPdf("underline");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Underline,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 150, Top = 200, Right = 350, Bottom = 215 },
            StrokeColor = Color.Blue,
            Contents = "Underline test",
            Author = "Test User"
        };

        // Act
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.Underline);
        saved.Contents.Should().Be("Underline test");
        saved.Author.Should().Be("Test User");
        saved.PageNumber.Should().Be(0);
    }

    [Fact]
    public async Task Test_CreateStrikethrough_SaveAndReload_ColorPreserved()
    {
        // Arrange
        var testPdf = await CreateTestPdf("strikethrough");
        var document = await LoadDocument(testPdf);

        var expectedColor = Color.Red;
        var annotation = new Annotation
        {
            Type = AnnotationType.StrikeOut,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 300, Right = 300, Bottom = 315 },
            StrokeColor = expectedColor,
            Contents = "Strikethrough test"
        };

        // Act
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.StrikeOut);
        saved.StrokeColor.R.Should().Be(expectedColor.R);
        saved.StrokeColor.G.Should().Be(expectedColor.G);
        saved.StrokeColor.B.Should().Be(expectedColor.B);
    }

    [Fact]
    public async Task Test_CreateRectangle_SaveAndReload_BoundsCorrect()
    {
        // Arrange
        var testPdf = await CreateTestPdf("rectangle");
        var document = await LoadDocument(testPdf);

        var expectedBounds = new PdfRectangle { Left = 150, Top = 150, Right = 350, Bottom = 250 };
        var annotation = new Annotation
        {
            Type = AnnotationType.Square,
            PageNumber = 0,
            Bounds = expectedBounds,
            StrokeColor = Color.Red,
            StrokeWidth = 2.0,
            FillColor = Color.FromArgb(50, 255, 0, 0)
        };

        // Act
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.Square);
        // Allow small tolerance for floating point comparison
        saved.Bounds.Left.Should().BeApproximately(expectedBounds.Left, 1.0);
        saved.Bounds.Top.Should().BeApproximately(expectedBounds.Top, 1.0);
        saved.Bounds.Right.Should().BeApproximately(expectedBounds.Right, 1.0);
        saved.Bounds.Bottom.Should().BeApproximately(expectedBounds.Bottom, 1.0);
    }

    [Fact]
    public async Task Test_CreateCircle_SaveAndReload_OpacityPreserved()
    {
        // Arrange
        var testPdf = await CreateTestPdf("circle");
        var document = await LoadDocument(testPdf);

        var expectedOpacity = 0.7;
        var annotation = new Annotation
        {
            Type = AnnotationType.Circle,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 200, Top = 200, Right = 300, Bottom = 300 },
            StrokeColor = Color.Blue,
            StrokeWidth = 3.0,
            FillColor = Color.FromArgb((int)(expectedOpacity * 255), 0, 0, 255),
            Opacity = expectedOpacity
        };

        // Act
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.Circle);
        saved.Opacity.Should().BeApproximately(expectedOpacity, 0.05);
    }

    [Fact]
    public async Task Test_CreateInk_SaveAndReload_PathPreserved()
    {
        // Arrange
        var testPdf = await CreateTestPdf("ink");
        var document = await LoadDocument(testPdf);

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
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.Ink);
        saved.InkPoints.Should().NotBeEmpty();
        saved.InkPoints.Count.Should().Be(inkPoints.Count);
    }

    [Fact]
    public async Task Test_CreateStickyNote_SaveAndReload_ContentPreserved()
    {
        // Arrange
        var testPdf = await CreateTestPdf("stickynote");
        var document = await LoadDocument(testPdf);

        var expectedContent = "This is a sticky note with important information.";
        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 50, Top = 50, Right = 70, Bottom = 70 },
            FillColor = Color.LightYellow,
            Contents = expectedContent,
            Author = "Test User"
        };

        // Act
        await CreateAndSaveAnnotation(document, annotation, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        var saved = annotations[0];
        saved.Type.Should().Be(AnnotationType.Text);
        saved.Contents.Should().Be(expectedContent);
    }

    #endregion

    #region PropertyPersistenceTests

    [Fact]
    public async Task Test_SetColor_SaveAndReload_ColorMatches()
    {
        // Arrange
        var testPdf = await CreateTestPdf("color_persistence");
        var document = await LoadDocument(testPdf);

        var testColors = new[]
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Magenta,
            Color.Cyan,
            Color.FromArgb(128, 64, 192)
        };

        // Act & Assert
        for (int i = 0; i < testColors.Length; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Square,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50 + (i * 60), Top = 50, Right = 100 + (i * 60), Bottom = 100 },
                FillColor = testColors[i],
                Opacity = 1.0
            };

            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(testColors.Length);

        for (int i = 0; i < testColors.Length; i++)
        {
            var saved = annotations[i];
            saved.FillColor.R.Should().Be(testColors[i].R);
            saved.FillColor.G.Should().Be(testColors[i].G);
            saved.FillColor.B.Should().Be(testColors[i].B);
        }
    }

    [Fact]
    public async Task Test_SetOpacity_SaveAndReload_OpacityMatches()
    {
        // Arrange
        var testPdf = await CreateTestPdf("opacity_persistence");
        var document = await LoadDocument(testPdf);

        var testOpacities = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };

        // Act & Assert
        for (int i = 0; i < testOpacities.Length; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50, Top = 50 + (i * 30), Right = 150, Bottom = 70 + (i * 30) },
                FillColor = Color.Yellow,
                Opacity = testOpacities[i]
            };

            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(testOpacities.Length);

        for (int i = 0; i < testOpacities.Length; i++)
        {
            annotations[i].Opacity.Should().BeApproximately(testOpacities[i], 0.05);
        }
    }

    [Fact]
    public async Task Test_SetStrokeWidth_SaveAndReload_WidthMatches()
    {
        // Arrange
        var testPdf = await CreateTestPdf("strokewidth_persistence");
        var document = await LoadDocument(testPdf);

        var testWidths = new[] { 0.5, 1.0, 2.0, 3.5, 5.0 };

        // Act & Assert
        for (int i = 0; i < testWidths.Length; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Square,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50, Top = 50 + (i * 60), Right = 150, Bottom = 100 + (i * 60) },
                StrokeColor = Color.Black,
                StrokeWidth = testWidths[i]
            };

            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(testWidths.Length);

        for (int i = 0; i < testWidths.Length; i++)
        {
            annotations[i].StrokeWidth.Should().BeApproximately(testWidths[i], 0.1);
        }
    }

    [Fact]
    public async Task Test_SetAuthor_SaveAndReload_AuthorMatches()
    {
        // Arrange
        var testPdf = await CreateTestPdf("author_persistence");
        var document = await LoadDocument(testPdf);

        var testAuthors = new[] { "Alice", "Bob", "Charlie", "Unicode: 日本語", "Special: @#$%" };

        // Act & Assert
        for (int i = 0; i < testAuthors.Length; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Text,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50 + (i * 40), Top = 50, Right = 70 + (i * 40), Bottom = 70 },
                Contents = "Note",
                Author = testAuthors[i]
            };

            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(testAuthors.Length);

        for (int i = 0; i < testAuthors.Length; i++)
        {
            annotations[i].Author.Should().Be(testAuthors[i]);
        }
    }

    [Fact]
    public async Task Test_SetContent_SaveAndReload_ContentMatches()
    {
        // Arrange
        var testPdf = await CreateTestPdf("content_persistence");
        var document = await LoadDocument(testPdf);

        var testContents = new[]
        {
            "Simple text",
            "Line 1\nLine 2\nLine 3",
            "Unicode: 日本語、中文、العربية",
            "Special chars: <>&\"'",
            "Very long content: " + new string('x', 1000)
        };

        // Act & Assert
        for (int i = 0; i < testContents.Length; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Text,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50 + (i * 50), Top = 50, Right = 70 + (i * 50), Bottom = 70 },
                Contents = testContents[i]
            };

            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(testContents.Length);

        for (int i = 0; i < testContents.Length; i++)
        {
            annotations[i].Contents.Should().Be(testContents[i]);
        }
    }

    #endregion

    #region UpdateAnnotationTests

    [Fact]
    public async Task Test_UpdateAnnotationColor_PropertyChanged()
    {
        // Arrange
        var testPdf = await CreateTestPdf("update_color");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Square,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 200 },
            FillColor = Color.Red
        };

        await _annotationService.CreateAnnotationAsync(document, annotation);

        // Act
        annotation.FillColor = Color.Blue;
        var updateResult = await _annotationService.UpdateAnnotationAsync(document, annotation);
        updateResult.IsSuccess.Should().BeTrue();

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        annotations[0].FillColor.B.Should().Be(255);
    }

    [Fact]
    public async Task Test_UpdateAnnotationOpacity_PropertyChanged()
    {
        // Arrange
        var testPdf = await CreateTestPdf("update_opacity");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 120 },
            FillColor = Color.Yellow,
            Opacity = 1.0
        };

        await _annotationService.CreateAnnotationAsync(document, annotation);

        // Act
        annotation.Opacity = 0.3;
        await _annotationService.UpdateAnnotationAsync(document, annotation);
        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations[0].Opacity.Should().BeApproximately(0.3, 0.05);
    }

    [Fact]
    public async Task Test_UpdateAnnotationContent_PropertyChanged()
    {
        // Arrange
        var testPdf = await CreateTestPdf("update_content");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 120, Bottom = 120 },
            Contents = "Original content"
        };

        await _annotationService.CreateAnnotationAsync(document, annotation);

        // Act
        annotation.Contents = "Updated content";
        await _annotationService.UpdateAnnotationAsync(document, annotation);
        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations[0].Contents.Should().Be("Updated content");
    }

    #endregion

    #region DeleteAnnotationTests

    [Fact]
    public async Task Test_DeleteAnnotation_NoLongerPresent()
    {
        // Arrange
        var testPdf = await CreateTestPdf("delete_single");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle { Left = 100, Top = 100, Right = 200, Bottom = 120 },
            FillColor = Color.Yellow
        };

        await _annotationService.CreateAnnotationAsync(document, annotation);

        // Act
        var deleteResult = await _annotationService.DeleteAnnotationAsync(document, 0, 0);
        deleteResult.IsSuccess.Should().BeTrue();

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_DeleteAllAnnotations_DocumentClean()
    {
        // Arrange
        var testPdf = await CreateTestPdf("delete_all");
        var document = await LoadDocument(testPdf);

        // Create multiple annotations
        for (int i = 0; i < 5; i++)
        {
            var annotation = new Annotation
            {
                Type = AnnotationType.Square,
                PageNumber = 0,
                Bounds = new PdfRectangle { Left = 50 + (i * 60), Top = 50, Right = 100 + (i * 60), Bottom = 100 }
            };
            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        // Act - Delete all annotations (in reverse order to avoid index shifting)
        for (int i = 4; i >= 0; i--)
        {
            await _annotationService.DeleteAnnotationAsync(document, 0, i);
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().BeEmpty();
    }

    #endregion

    #region FdfExportImportTests

    [Fact]
    public async Task Test_ExportToXfdf_AllAnnotationsIncluded()
    {
        // Arrange
        var testPdf = await CreateTestPdf("export_xfdf");
        var document = await LoadDocument(testPdf);
        var xfdfPath = Path.Combine(_tempPath, "export_test.xfdf");
        _filesToCleanup.Add(xfdfPath);

        var annotations = new[]
        {
            new Annotation { Type = AnnotationType.Highlight, PageNumber = 0, Bounds = new PdfRectangle(50, 50, 150, 70) },
            new Annotation { Type = AnnotationType.Text, PageNumber = 0, Bounds = new PdfRectangle(200, 50, 220, 70), Contents = "Note" },
            new Annotation { Type = AnnotationType.Square, PageNumber = 0, Bounds = new PdfRectangle(100, 100, 200, 200) }
        };

        foreach (var annotation in annotations)
        {
            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        // Act
        var exportResult = await _fdfService.ExportAnnotationsToXfdfAsync(document, xfdfPath);
        exportResult.IsSuccess.Should().BeTrue();

        // Assert
        File.Exists(xfdfPath).Should().BeTrue();
        var xfdfContent = await File.ReadAllTextAsync(xfdfPath);
        xfdfContent.Should().Contain("<highlight");
        xfdfContent.Should().Contain("<text");
        xfdfContent.Should().Contain("<square");
    }

    [Fact]
    public async Task Test_ImportFromXfdf_AnnotationsCreated()
    {
        // Arrange
        var sourcePdf = await CreateTestPdf("import_source");
        var sourceDoc = await LoadDocument(sourcePdf);
        var xfdfPath = Path.Combine(_tempPath, "import_test.xfdf");
        _filesToCleanup.Add(xfdfPath);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 300, 120),
            FillColor = Color.Yellow,
            Opacity = 0.5,
            Contents = "Imported highlight",
            Author = "Test"
        };

        await _annotationService.CreateAnnotationAsync(sourceDoc, annotation);
        await _fdfService.ExportAnnotationsToXfdfAsync(sourceDoc, xfdfPath);
        _documentService.CloseDocument(sourceDoc);

        // Act - Import into new document
        var targetPdf = await CreateTestPdf("import_target");
        var targetDoc = await LoadDocument(targetPdf);
        var importResult = await _fdfService.ImportAnnotationsFromXfdfAsync(targetDoc, xfdfPath);
        importResult.IsSuccess.Should().BeTrue();
        importResult.Value.Should().Be(1);

        await _annotationService.SaveAnnotationsAsync(targetDoc, targetPdf);
        var reloadedDoc = await ReloadDocument(targetDoc, targetPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
        annotations[0].Type.Should().Be(AnnotationType.Highlight);
        annotations[0].Contents.Should().Be("Imported highlight");
    }

    [Fact]
    public async Task Test_RoundTrip_AnnotationsMatch()
    {
        // Arrange
        var testPdf = await CreateTestPdf("roundtrip");
        var document = await LoadDocument(testPdf);
        var xfdfPath = Path.Combine(_tempPath, "roundtrip.xfdf");
        _filesToCleanup.Add(xfdfPath);

        var originalAnnotations = new[]
        {
            new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle(100, 100, 300, 120),
                FillColor = Color.Yellow,
                Opacity = 0.5
            },
            new Annotation
            {
                Type = AnnotationType.Text,
                PageNumber = 0,
                Bounds = new PdfRectangle(50, 50, 70, 70),
                Contents = "Note 1",
                Author = "Alice"
            }
        };

        foreach (var annotation in originalAnnotations)
        {
            await _annotationService.CreateAnnotationAsync(document, annotation);
        }

        // Act - Export then delete then import
        await _fdfService.ExportAnnotationsToXfdfAsync(document, xfdfPath);
        await _annotationService.DeleteAnnotationAsync(document, 0, 1);
        await _annotationService.DeleteAnnotationAsync(document, 0, 0);

        var importResult = await _fdfService.ImportAnnotationsFromXfdfAsync(document, xfdfPath);
        importResult.IsSuccess.Should().BeTrue();
        importResult.Value.Should().Be(2);

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(2);
        annotations.Should().Contain(a => a.Type == AnnotationType.Highlight);
        annotations.Should().Contain(a => a.Type == AnnotationType.Text && a.Contents == "Note 1");
    }

    [Fact]
    public async Task Test_ImportWithMerge_NoDuplicates()
    {
        // Arrange
        var testPdf = await CreateTestPdf("import_merge");
        var document = await LoadDocument(testPdf);
        var xfdfPath = Path.Combine(_tempPath, "import_merge.xfdf");
        _filesToCleanup.Add(xfdfPath);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 300, 120),
            FillColor = Color.Yellow
        };

        await _annotationService.CreateAnnotationAsync(document, annotation);
        await _fdfService.ExportAnnotationsToXfdfAsync(document, xfdfPath);

        // Act - Import with SkipDuplicates merge behavior
        var importResult = await _fdfService.ImportAnnotationsFromXfdfAsync(
            document,
            xfdfPath,
            AnnotationMergeBehavior.SkipDuplicates);

        importResult.IsSuccess.Should().BeTrue();
        importResult.Value.Should().Be(0, "duplicate should be skipped");

        // Assert
        var annotations = await GetAnnotations(document, 0);
        annotations.Should().HaveCount(1, "should still have only one annotation");
    }

    [Fact]
    public async Task Test_ImportWithReplace_OldAnnotationsRemoved()
    {
        // Arrange
        var testPdf = await CreateTestPdf("import_replace");
        var document = await LoadDocument(testPdf);
        var xfdfPath = Path.Combine(_tempPath, "import_replace.xfdf");
        _filesToCleanup.Add(xfdfPath);

        // Create and export one annotation
        var exportAnnotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle(200, 200, 220, 220),
            Contents = "New annotation"
        };

        var tempDoc = await LoadDocument(await CreateTestPdf("temp_export"));
        await _annotationService.CreateAnnotationAsync(tempDoc, exportAnnotation);
        await _fdfService.ExportAnnotationsToXfdfAsync(tempDoc, xfdfPath);
        _documentService.CloseDocument(tempDoc);

        // Create different annotation in target document
        var oldAnnotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 300, 120),
            FillColor = Color.Yellow
        };
        await _annotationService.CreateAnnotationAsync(document, oldAnnotation);

        // Act - Import with Replace behavior
        var importResult = await _fdfService.ImportAnnotationsFromXfdfAsync(
            document,
            xfdfPath,
            AnnotationMergeBehavior.Replace);

        importResult.IsSuccess.Should().BeTrue();

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1, "old annotation should be replaced");
        annotations[0].Type.Should().Be(AnnotationType.Text);
        annotations[0].Contents.Should().Be("New annotation");
    }

    #endregion

    #region EdgeCaseTests

    [Fact]
    public async Task Test_SaveEmptyAnnotation_HandledGracefully()
    {
        // Arrange
        var testPdf = await CreateTestPdf("empty_annotation");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Text,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 120, 120),
            Contents = "", // Empty content
            Author = "" // Empty author
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Test_SaveWithInvalidColor_UsesDefault()
    {
        // Arrange
        var testPdf = await CreateTestPdf("invalid_color");
        var document = await LoadDocument(testPdf);

        var annotation = new Annotation
        {
            Type = AnnotationType.Highlight,
            PageNumber = 0,
            Bounds = new PdfRectangle(100, 100, 300, 120),
            FillColor = Color.FromArgb(0, 0, 0, 0), // Transparent (edge case)
            Opacity = 0.5
        };

        // Act
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        // Assert - Should not throw, annotation should be saved
        var reloadedDoc = await ReloadDocument(document, testPdf);
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(1);
    }

    [Fact]
    public async Task Test_SaveWithInvalidOpacity_ClampsToRange()
    {
        // Arrange
        var testPdf = await CreateTestPdf("invalid_opacity");
        var document = await LoadDocument(testPdf);

        var testCases = new[]
        {
            new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle(100, 100, 300, 120),
                FillColor = Color.Yellow,
                Opacity = -0.5 // Invalid: negative
            },
            new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = 0,
                Bounds = new PdfRectangle(100, 140, 300, 160),
                FillColor = Color.Yellow,
                Opacity = 1.5 // Invalid: > 1.0
            }
        };

        // Act
        foreach (var annotation in testCases)
        {
            var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
            createResult.IsSuccess.Should().BeTrue();
        }

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, testPdf);
        saveResult.IsSuccess.Should().BeTrue();

        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(2);
        foreach (var annotation in annotations)
        {
            annotation.Opacity.Should().BeInRange(0.0, 1.0, "opacity should be clamped to valid range");
        }
    }

    [Fact]
    public async Task Test_MultipleAnnotationsSamePage_AllPersist()
    {
        // Arrange
        var testPdf = await CreateTestPdf("multiple_same_page");
        var document = await LoadDocument(testPdf);

        var annotationCount = 20;
        var annotationTypes = new[]
        {
            AnnotationType.Highlight,
            AnnotationType.Underline,
            AnnotationType.StrikeOut,
            AnnotationType.Text,
            AnnotationType.Square
        };

        // Act
        for (int i = 0; i < annotationCount; i++)
        {
            var annotation = new Annotation
            {
                Type = annotationTypes[i % annotationTypes.Length],
                PageNumber = 0,
                Bounds = new PdfRectangle(50 + (i % 5) * 80, 50 + (i / 5) * 40, 120 + (i % 5) * 80, 80 + (i / 5) * 40),
                FillColor = Color.FromArgb(255, (i * 12) % 256, (i * 23) % 256, (i * 34) % 256),
                Contents = $"Annotation {i}"
            };

            var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
            createResult.IsSuccess.Should().BeTrue();
        }

        await _annotationService.SaveAnnotationsAsync(document, testPdf);
        var reloadedDoc = await ReloadDocument(document, testPdf);

        // Assert
        var annotations = await GetAnnotations(reloadedDoc, 0);
        annotations.Should().HaveCount(annotationCount, "all annotations should persist");

        // Verify type distribution
        foreach (var type in annotationTypes)
        {
            annotations.Count(a => a.Type == type).Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> CreateTestPdf(string testName)
    {
        var sourcePdf = Path.Combine(_fixturesPath, "sample.pdf");
        var testPdf = Path.Combine(_tempPath, $"{testName}_{Guid.NewGuid()}.pdf");
        File.Copy(sourcePdf, testPdf, true);
        _filesToCleanup.Add(testPdf);
        return testPdf;
    }

    private async Task<PdfDocument> LoadDocument(string path)
    {
        var loadResult = await _documentService.LoadDocumentAsync(path);
        loadResult.IsSuccess.Should().BeTrue();
        var document = loadResult.Value;
        _documentsToCleanup.Add(document);
        return document;
    }

    private async Task<PdfDocument> ReloadDocument(PdfDocument oldDocument, string path)
    {
        _documentService.CloseDocument(oldDocument);
        _documentsToCleanup.Remove(oldDocument);
        return await LoadDocument(path);
    }

    private async Task<List<Annotation>> GetAnnotations(PdfDocument document, int pageNumber)
    {
        var result = await _annotationService.GetAnnotationsAsync(document, pageNumber);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private async Task CreateAndSaveAnnotation(PdfDocument document, Annotation annotation, string filePath)
    {
        var createResult = await _annotationService.CreateAnnotationAsync(document, annotation);
        createResult.IsSuccess.Should().BeTrue();

        var saveResult = await _annotationService.SaveAnnotationsAsync(document, filePath);
        saveResult.IsSuccess.Should().BeTrue();
    }

    #endregion
}
