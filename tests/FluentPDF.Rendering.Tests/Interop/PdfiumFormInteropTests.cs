using FluentAssertions;
using FluentPDF.Rendering.Interop;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop;

/// <summary>
/// Unit tests for PDFium form P/Invoke layer.
/// These tests verify basic form API functionality with real PDFium.
/// </summary>
public class PdfiumFormInteropTests : IDisposable
{
    private bool _isInitialized;
    private readonly string _samplePdfPath;

    public PdfiumFormInteropTests()
    {
        _isInitialized = PdfiumInterop.Initialize();
        _samplePdfPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures", "sample.pdf");
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            PdfiumInterop.Shutdown();
        }
    }

    [Fact]
    public void InitFormFillEnvironment_WithValidDocument_ShouldReturnValidHandle()
    {
        // Arrange
        if (!_isInitialized)
        {
            // Skip test if PDFium is not available
            return;
        }

        using var document = PdfiumInterop.LoadDocument(_samplePdfPath);
        document.IsInvalid.Should().BeFalse("sample PDF should load successfully");

        // Act
        using var formHandle = PdfiumFormInterop.InitFormFillEnvironment(document, IntPtr.Zero);

        // Assert
        formHandle.Should().NotBeNull();
        formHandle.IsInvalid.Should().BeFalse("form environment should initialize successfully");
    }

    [Fact]
    public void InitFormFillEnvironment_WithInvalidDocument_ShouldThrowException()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();

        // Act
        Action act = () => PdfiumFormInterop.InitFormFillEnvironment(invalidDocument, IntPtr.Zero);

        // Assert
        act.Should().Throw<ArgumentException>("invalid document should throw exception");
    }

    [Fact]
    public void SafePdfFormHandle_ShouldDisposeAutomatically()
    {
        // Arrange
        if (!_isInitialized)
        {
            // Skip test if PDFium is not available
            return;
        }

        using var document = PdfiumInterop.LoadDocument(_samplePdfPath);
        SafePdfFormHandle? formHandle = null;

        // Act
        using (formHandle = PdfiumFormInterop.InitFormFillEnvironment(document, IntPtr.Zero))
        {
            formHandle.IsInvalid.Should().BeFalse();
        }

        // Assert - handle should be released after using block
        formHandle.IsClosed.Should().BeTrue("SafeHandle should be closed after disposal");
    }

    [Fact]
    public void GetAnnotationCount_WithValidPage_ShouldReturnNonNegative()
    {
        // Arrange
        if (!_isInitialized)
        {
            // Skip test if PDFium is not available
            return;
        }

        using var document = PdfiumInterop.LoadDocument(_samplePdfPath);
        using var page = PdfiumInterop.LoadPage(document, 0);
        page.IsInvalid.Should().BeFalse();

        // Act
        var count = PdfiumFormInterop.GetAnnotationCount(page);

        // Assert
        count.Should().BeGreaterThanOrEqualTo(0, "annotation count should be non-negative");
    }

    [Fact]
    public void GetAnnotationCount_WithInvalidPage_ShouldReturnNegative()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();

        // Act
        var count = PdfiumFormInterop.GetAnnotationCount(invalidPage);

        // Assert
        count.Should().Be(-1, "invalid page should return -1");
    }

    [Fact]
    public void GetAnnotation_WithValidPageAndIndex_ShouldReturnHandle()
    {
        // Arrange
        if (!_isInitialized)
        {
            // Skip test if PDFium is not available
            return;
        }

        using var document = PdfiumInterop.LoadDocument(_samplePdfPath);
        using var page = PdfiumInterop.LoadPage(document, 0);
        var annotCount = PdfiumFormInterop.GetAnnotationCount(page);

        if (annotCount == 0)
        {
            // Skip test if no annotations
            return;
        }

        // Act
        var annot = PdfiumFormInterop.GetAnnotation(page, 0);

        try
        {
            // Assert
            annot.Should().NotBe(IntPtr.Zero, "first annotation should return valid handle");
        }
        finally
        {
            if (annot != IntPtr.Zero)
            {
                PdfiumFormInterop.CloseAnnotation(annot);
            }
        }
    }

    [Fact]
    public void GetAnnotation_WithInvalidPage_ShouldReturnZero()
    {
        // Arrange
        using var invalidPage = new SafePdfPageHandle();

        // Act
        var annot = PdfiumFormInterop.GetAnnotation(invalidPage, 0);

        // Assert
        annot.Should().Be(IntPtr.Zero, "invalid page should return null handle");
    }

    [Fact]
    public void GetAnnotationSubtype_WithInvalidAnnotation_ShouldReturnUnknown()
    {
        // Arrange
        var invalidAnnot = IntPtr.Zero;

        // Act
        var subtype = PdfiumFormInterop.GetAnnotationSubtype(invalidAnnot);

        // Assert
        subtype.Should().Be(PdfiumFormInterop.AnnotationSubtype.Unknown, "invalid annotation should return Unknown");
    }

    [Fact]
    public void GetFormFieldTypeAtPoint_WithValidFormHandle_ShouldReturnFieldType()
    {
        // Arrange
        if (!_isInitialized)
        {
            // Skip test if PDFium is not available
            return;
        }

        using var document = PdfiumInterop.LoadDocument(_samplePdfPath);
        using var formHandle = PdfiumFormInterop.InitFormFillEnvironment(document, IntPtr.Zero);
        using var page = PdfiumInterop.LoadPage(document, 0);

        // Act - check center of page
        var fieldType = PdfiumFormInterop.GetFormFieldTypeAtPoint(formHandle, page, 100, 100);

        // Assert - should return a valid field type (even if Unknown for no field at point)
        fieldType.Should().BeGreaterThanOrEqualTo(0, "field type should be non-negative");
    }

    [Fact]
    public void GetFormFieldTypeAtPoint_WithInvalidFormHandle_ShouldReturnUnknown()
    {
        // Arrange
        using var invalidFormHandle = new SafePdfFormHandle();
        using var invalidPage = new SafePdfPageHandle();

        // Act
        var fieldType = PdfiumFormInterop.GetFormFieldTypeAtPoint(invalidFormHandle, invalidPage, 0, 0);

        // Assert
        fieldType.Should().Be(PdfiumFormInterop.FieldType.Unknown, "invalid handles should return Unknown");
    }

    [Fact]
    public void GetFormFieldFlags_WithInvalidHandles_ShouldReturnZero()
    {
        // Arrange
        using var invalidFormHandle = new SafePdfFormHandle();
        var invalidAnnot = IntPtr.Zero;

        // Act
        var flags = PdfiumFormInterop.GetFormFieldFlags(invalidFormHandle, invalidAnnot);

        // Assert
        flags.Should().Be(0, "invalid handles should return 0 flags");
    }

    [Fact]
    public void GetFormFieldValue_WithInvalidHandles_ShouldReturnEmptyString()
    {
        // Arrange
        using var invalidFormHandle = new SafePdfFormHandle();
        var invalidAnnot = IntPtr.Zero;

        // Act
        var value = PdfiumFormInterop.GetFormFieldValue(invalidFormHandle, invalidAnnot);

        // Assert
        value.Should().BeEmpty("invalid handles should return empty string");
    }

    [Fact]
    public void GetFormFieldName_WithInvalidAnnotation_ShouldReturnEmptyString()
    {
        // Arrange
        var invalidAnnot = IntPtr.Zero;

        // Act
        var name = PdfiumFormInterop.GetFormFieldName(invalidAnnot);

        // Assert
        name.Should().BeEmpty("invalid annotation should return empty string");
    }

    [Fact]
    public void IsFormFieldChecked_WithInvalidHandles_ShouldReturnFalse()
    {
        // Arrange
        using var invalidFormHandle = new SafePdfFormHandle();
        var invalidAnnot = IntPtr.Zero;

        // Act
        var isChecked = PdfiumFormInterop.IsFormFieldChecked(invalidFormHandle, invalidAnnot);

        // Assert
        isChecked.Should().BeFalse("invalid handles should return false");
    }

    [Fact]
    public void GetAnnotationRect_WithInvalidAnnotation_ShouldReturnFalse()
    {
        // Arrange
        var invalidAnnot = IntPtr.Zero;

        // Act
        var result = PdfiumFormInterop.GetAnnotationRect(invalidAnnot, out var left, out var bottom, out var right, out var top);

        // Assert
        result.Should().BeFalse("invalid annotation should return false");
        left.Should().Be(0);
        bottom.Should().Be(0);
        right.Should().Be(0);
        top.Should().Be(0);
    }

    [Fact]
    public void SetFormFieldValue_WithInvalidHandles_ShouldReturnFalse()
    {
        // Arrange
        using var invalidFormHandle = new SafePdfFormHandle();
        var invalidAnnot = IntPtr.Zero;

        // Act
        var result = PdfiumFormInterop.SetFormFieldValue(invalidFormHandle, invalidAnnot, "test");

        // Assert
        result.Should().BeFalse("invalid handles should return false");
    }

    [Fact]
    public void SetFormFieldChecked_WithInvalidHandles_ShouldReturnFalse()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();
        var invalidAnnot = IntPtr.Zero;

        // Act
        var result = PdfiumFormInterop.SetFormFieldChecked(invalidDocument, invalidAnnot, true);

        // Assert
        result.Should().BeFalse("invalid handles should return false");
    }

    [Fact]
    public void SaveDocument_WithInvalidHandles_ShouldReturnFalse()
    {
        // Arrange
        using var invalidDocument = new SafePdfDocumentHandle();
        var invalidWriter = IntPtr.Zero;

        // Act
        var result = PdfiumFormInterop.SaveDocument(invalidDocument, invalidWriter, 0);

        // Assert
        result.Should().BeFalse("invalid handles should return false");
    }

    [Fact]
    public void FieldTypeConstants_ShouldHaveCorrectValues()
    {
        // Assert - verify field type constants match PDFium spec
        PdfiumFormInterop.FieldType.Unknown.Should().Be(0);
        PdfiumFormInterop.FieldType.PushButton.Should().Be(1);
        PdfiumFormInterop.FieldType.CheckBox.Should().Be(2);
        PdfiumFormInterop.FieldType.RadioButton.Should().Be(3);
        PdfiumFormInterop.FieldType.ComboBox.Should().Be(4);
        PdfiumFormInterop.FieldType.ListBox.Should().Be(5);
        PdfiumFormInterop.FieldType.TextField.Should().Be(6);
        PdfiumFormInterop.FieldType.Signature.Should().Be(7);
    }

    [Fact]
    public void FieldFlagsConstants_ShouldHaveCorrectValues()
    {
        // Assert - verify field flag constants
        PdfiumFormInterop.FieldFlags.ReadOnly.Should().Be(1);
        PdfiumFormInterop.FieldFlags.Required.Should().Be(2);
        PdfiumFormInterop.FieldFlags.NoExport.Should().Be(4);
    }

    [Fact]
    public void SaveFlagsConstants_ShouldHaveCorrectValues()
    {
        // Assert - verify save flag constants
        PdfiumFormInterop.SaveFlags.Incremental.Should().Be(1);
        PdfiumFormInterop.SaveFlags.NoIncremental.Should().Be(2);
        PdfiumFormInterop.SaveFlags.RemoveSecurity.Should().Be(3);
    }
}
