# Design: PDFium Backend Abstraction

## 1. Architecture Overview

### 1.1 Layered Architecture

```
┌─────────────────────────────────────────────┐
│          Application Layer (UI)             │
│  FluentPDF.App / FluentPDF.Avalonia         │
└────────────────┬────────────────────────────┘
                 │
┌────────────────▼────────────────────────────┐
│         Service Layer (Business Logic)      │
│            FluentPDF.Core                   │
│  - IPdfDocumentService                      │
│  - IPdfRenderingService                     │
│  - ITextExtractionService (etc.)            │
└────────────────┬────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────┐
│        PDF Backend Abstraction              │
│     FluentPDF.Rendering.Abstractions        │
│  - IPdfBackend                              │
│  - IPdfDocument, IPdfPage                   │
│  - PdfRect, PdfMatrix, PdfColor             │
└────────────┬───────────────────┬────────────┘
             │                   │
   ┌─────────▼──────┐   ┌───────▼────────┐
   │  PdfiumBackend │   │ InMemoryBackend│
   │ (Production)   │   │  (Testing)     │
   └────────────────┘   └────────────────┘
```

### 1.2 Project Structure

**New Projects:**
- `FluentPDF.Rendering.Abstractions` - Core PDF abstractions
- `FluentPDF.Rendering.Pdfium` - PDFium implementation
- `FluentPDF.Rendering.InMemory` - Test mock implementation

**Modified Projects:**
- `FluentPDF.Core` - Services depend on abstractions
- `FluentPDF.Rendering` - Delegates to backend implementations

### 1.3 Namespace Organization

```
FluentPDF.Rendering.Abstractions
├── IPdfBackend.cs
├── IPdfDocument.cs
├── IPdfPage.cs
├── IDocument
│   ├── IPageCollection.cs
│   ├── IFormFieldCollection.cs
│   └── IAnnotationCollection.cs
├── Models
│   ├── PdfRect.cs (struct)
│   ├── PdfMatrix.cs (struct)
│   ├── PdfColor.cs (struct)
│   ├── PdfSize.cs (struct)
│   └── RenderOptions.cs
└── Exceptions
    ├── PdfBackendException.cs
    ├── PdfDocumentException.cs
    └── PdfRenderingException.cs

FluentPDF.Rendering.Pdfium
├── PdfiumBackend.cs
├── PdfiumDocument.cs
├── PdfiumPage.cs
├── PdfiumFormField.cs
├── PdfiumAnnotation.cs
├── Interop
│   ├── PdfiumInterop.cs (P/Invoke)
│   ├── PdfiumFormInterop.cs
│   ├── PdfiumTextInterop.cs
│   └── SafeHandles
│       ├── SafePdfDocumentHandle.cs
│       ├── SafePdfPageHandle.cs
│       └── SafePdfTextPageHandle.cs
└── Collections
    ├── PdfiumPageCollection.cs
    └── PdfiumFormFieldCollection.cs

FluentPDF.Rendering.InMemory
├── InMemoryBackend.cs
├── InMemoryDocument.cs
├── InMemoryPage.cs
└── Builders
    ├── PdfDocumentBuilder.cs
    └── PdfPageBuilder.cs
```

## 2. Core Abstractions

### 2.1 IPdfBackend Interface

```csharp
namespace FluentPDF.Rendering.Abstractions;

/// <summary>
/// Abstraction for PDF rendering backend (PDFium, MuPDF, etc.)
/// </summary>
public interface IPdfBackend : IDisposable
{
    /// <summary>
    /// Initialize the backend (called once per application)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Open a PDF document from file path
    /// </summary>
    Task<IPdfDocument> OpenDocumentAsync(string path, string? password = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Open a PDF document from stream
    /// </summary>
    Task<IPdfDocument> OpenDocumentAsync(Stream stream, string? password = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new blank PDF document
    /// </summary>
    IPdfDocument CreateDocument();

    /// <summary>
    /// Get backend name and version info
    /// </summary>
    PdfBackendInfo GetInfo();
}

public sealed record PdfBackendInfo(
    string Name,
    string Version,
    bool SupportsEditing,
    bool SupportsAnnotations,
    bool SupportsForms
);
```

### 2.2 IPdfDocument Interface

```csharp
namespace FluentPDF.Rendering.Abstractions;

/// <summary>
/// Represents an open PDF document
/// </summary>
public interface IPdfDocument : IDisposable
{
    /// <summary>
    /// Document unique identifier for this session
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Source path or null if from stream
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    int PageCount { get; }

    /// <summary>
    /// PDF version (e.g., "1.7")
    /// </summary>
    string PdfVersion { get; }

    /// <summary>
    /// Is password-protected
    /// </summary>
    bool IsEncrypted { get; }

    /// <summary>
    /// Document metadata
    /// </summary>
    PdfMetadata Metadata { get; }

    /// <summary>
    /// Page collection
    /// </summary>
    IPageCollection Pages { get; }

    /// <summary>
    /// Form fields (if document has AcroForms)
    /// </summary>
    IFormFieldCollection? FormFields { get; }

    /// <summary>
    /// Save document to path
    /// </summary>
    Task SaveAsync(string path, PdfSaveOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save document to stream
    /// </summary>
    Task SaveAsync(Stream stream, PdfSaveOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed record PdfMetadata(
    string Title,
    string Author,
    string Subject,
    string Creator,
    DateTime? CreationDate,
    DateTime? ModificationDate
);
```

### 2.3 IPdfPage Interface

```csharp
namespace FluentPDF.Rendering.Abstractions;

/// <summary>
/// Represents a single page in a PDF document
/// </summary>
public interface IPdfPage : IDisposable
{
    /// <summary>
    /// Zero-based page index
    /// </summary>
    int PageIndex { get; }

    /// <summary>
    /// Page size in points (1/72 inch)
    /// </summary>
    PdfSize Size { get; }

    /// <summary>
    /// Page rotation (0, 90, 180, 270)
    /// </summary>
    int Rotation { get; }

    /// <summary>
    /// Render page to bitmap
    /// </summary>
    Task<byte[]> RenderAsync(RenderOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Render page to existing buffer (zero-copy)
    /// </summary>
    Task RenderToBufferAsync(Memory<byte> buffer, RenderOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from page
    /// </summary>
    Task<string> ExtractTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text with bounding rectangles
    /// </summary>
    Task<IReadOnlyList<PdfTextSegment>> ExtractTextWithBoundsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get annotations on this page
    /// </summary>
    Task<IReadOnlyList<IPdfAnnotation>> GetAnnotationsAsync(
        CancellationToken cancellationToken = default);
}

public sealed record RenderOptions(
    int Dpi = 96,
    int Rotation = 0,
    bool RenderAnnotations = true,
    bool RenderFormFields = true,
    bool UseTransparency = false,
    PdfRect? ClipRect = null
);

public readonly struct PdfTextSegment
{
    public string Text { get; init; }
    public PdfRect Bounds { get; init; }
    public int CharIndex { get; init; }
}
```

### 2.4 Value Objects

```csharp
namespace FluentPDF.Rendering.Abstractions;

/// <summary>
/// Immutable rectangle in PDF coordinate space
/// </summary>
public readonly struct PdfRect : IEquatable<PdfRect>
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Right { get; init; }
    public double Bottom { get; init; }

    public double Width => Right - Left;
    public double Height => Bottom - Top;

    public PdfRect(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public bool Contains(PdfRect other) => /* ... */;
    public bool Intersects(PdfRect other) => /* ... */;
    public PdfRect Intersection(PdfRect other) => /* ... */;
}

/// <summary>
/// Page size in points (1/72 inch)
/// </summary>
public readonly struct PdfSize : IEquatable<PdfSize>
{
    public double Width { get; init; }
    public double Height { get; init; }

    public PdfSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    // Common sizes
    public static PdfSize A4 => new(595.28, 841.89);
    public static PdfSize Letter => new(612, 792);
}

/// <summary>
/// Color representation in PDF
/// </summary>
public readonly struct PdfColor : IEquatable<PdfColor>
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }

    public PdfColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static PdfColor Black => new(0, 0, 0);
    public static PdfColor White => new(255, 255, 255);
}
```

## 3. PDFium Implementation

### 3.1 PdfiumBackend

```csharp
namespace FluentPDF.Rendering.Pdfium;

public sealed class PdfiumBackend : IPdfBackend
{
    private static readonly object _initLock = new();
    private static bool _initialized;
    private readonly ILogger<PdfiumBackend> _logger;

    public PdfiumBackend(ILogger<PdfiumBackend> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        lock (_initLock)
        {
            if (_initialized) return;

            PdfiumInterop.FPDF_InitLibrary();
            _initialized = true;
            _logger.LogInformation("PDFium library initialized");
        }
    }

    public async Task<IPdfDocument> OpenDocumentAsync(string path,
        string? password = null, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("Backend not initialized");

        // Load document asynchronously
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var handle = PdfiumInterop.FPDF_LoadMemDocument(bytes, password);

        if (handle.IsInvalid)
        {
            var error = PdfiumInterop.FPDF_GetLastError();
            throw new PdfDocumentException($"Failed to open PDF: {error}", path);
        }

        return new PdfiumDocument(handle, path, _logger);
    }

    public void Dispose()
    {
        lock (_initLock)
        {
            if (_initialized)
            {
                PdfiumInterop.FPDF_DestroyLibrary();
                _initialized = false;
            }
        }
    }
}
```

### 3.2 PdfiumDocument

```csharp
namespace FluentPDF.Rendering.Pdfium;

internal sealed class PdfiumDocument : IPdfDocument
{
    private readonly SafePdfDocumentHandle _handle;
    private readonly ILogger _logger;
    private readonly Lazy<IPageCollection> _pages;
    private bool _disposed;

    public Guid SessionId { get; } = Guid.NewGuid();
    public string? FilePath { get; }
    public int PageCount { get; }

    internal PdfiumDocument(SafePdfDocumentHandle handle, string? path, ILogger logger)
    {
        _handle = handle;
        FilePath = path;
        _logger = logger;

        PageCount = PdfiumInterop.FPDF_GetPageCount(_handle);
        _pages = new Lazy<IPageCollection>(() => new PdfiumPageCollection(this, _handle));
    }

    public IPageCollection Pages => _pages.Value;

    public PdfMetadata Metadata
    {
        get
        {
            var title = GetMetadataString("Title");
            var author = GetMetadataString("Author");
            // ... extract other metadata
            return new PdfMetadata(title, author, /* ... */);
        }
    }

    private string GetMetadataString(string key)
    {
        var length = PdfiumInterop.FPDF_GetMetaText(_handle, key, null, 0);
        if (length <= 0) return string.Empty;

        var buffer = new byte[length];
        PdfiumInterop.FPDF_GetMetaText(_handle, key, buffer, length);
        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _handle?.Dispose();
        _logger.LogDebug("PDF document {SessionId} disposed", SessionId);
    }
}
```

### 3.3 PdfiumPage

```csharp
namespace FluentPDF.Rendering.Pdfium;

internal sealed class PdfiumPage : IPdfPage
{
    private readonly SafePdfDocumentHandle _document;
    private SafePdfPageHandle? _pageHandle;
    private readonly ILogger _logger;

    public int PageIndex { get; }
    public PdfSize Size { get; }
    public int Rotation { get; }

    internal PdfiumPage(SafePdfDocumentHandle document, int pageIndex, ILogger logger)
    {
        _document = document;
        PageIndex = pageIndex;
        _logger = logger;

        // Lazy-load page handle
        var handle = GetOrLoadPage();
        Size = new PdfSize(
            PdfiumInterop.FPDF_GetPageWidth(handle),
            PdfiumInterop.FPDF_GetPageHeight(handle)
        );
        Rotation = PdfiumInterop.FPDFPage_GetRotation(handle);
    }

    private SafePdfPageHandle GetOrLoadPage()
    {
        return _pageHandle ??= PdfiumInterop.FPDF_LoadPage(_document, PageIndex);
    }

    public async Task<byte[]> RenderAsync(RenderOptions options,
        CancellationToken cancellationToken = default)
    {
        var handle = GetOrLoadPage();

        // Calculate dimensions
        var width = (int)(Size.Width * options.Dpi / 72.0);
        var height = (int)(Size.Height * options.Dpi / 72.0);
        var stride = width * 4; // BGRA
        var buffer = new byte[stride * height];

        await RenderToBufferAsync(buffer, options, cancellationToken);
        return buffer;
    }

    public Task RenderToBufferAsync(Memory<byte> buffer, RenderOptions options,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var handle = GetOrLoadPage();
            var width = (int)(Size.Width * options.Dpi / 72.0);
            var height = (int)(Size.Height * options.Dpi / 72.0);

            using var bitmap = PdfiumInterop.FPDFBitmap_CreateEx(
                width, height, 4 /* BGRA */, buffer, width * 4);

            // Fill white background
            PdfiumInterop.FPDFBitmap_FillRect(bitmap, 0, 0, width, height, 0xFFFFFFFF);

            // Render page
            PdfiumInterop.FPDF_RenderPageBitmap(
                bitmap, handle, 0, 0, width, height,
                options.Rotation, GetRenderFlags(options));

        }, cancellationToken);
    }

    private static int GetRenderFlags(RenderOptions options)
    {
        int flags = 0;
        if (options.RenderAnnotations) flags |= 0x01; // FPDF_ANNOT
        if (options.RenderFormFields) flags |= 0x02; // FPDF_LCD_TEXT
        // ... other flags
        return flags;
    }

    public void Dispose()
    {
        _pageHandle?.Dispose();
        _pageHandle = null;
    }
}
```

## 4. In-Memory Test Backend

### 4.1 InMemoryBackend

```csharp
namespace FluentPDF.Rendering.InMemory;

/// <summary>
/// Fast in-memory PDF backend for unit testing
/// </summary>
public sealed class InMemoryBackend : IPdfBackend
{
    private bool _initialized;

    public void Initialize()
    {
        _initialized = true;
    }

    public Task<IPdfDocument> OpenDocumentAsync(string path, string? password = null,
        CancellationToken cancellationToken = default)
    {
        // Return pre-configured test document
        var doc = new InMemoryDocument(path);
        return Task.FromResult<IPdfDocument>(doc);
    }

    public Task<IPdfDocument> OpenDocumentAsync(Stream stream, string? password = null,
        CancellationToken cancellationToken = default)
    {
        var doc = new InMemoryDocument(null);
        return Task.FromResult<IPdfDocument>(doc);
    }

    public IPdfDocument CreateDocument()
    {
        return new InMemoryDocument(null);
    }

    public PdfBackendInfo GetInfo() => new(
        "InMemory", "1.0.0",
        SupportsEditing: true,
        SupportsAnnotations: true,
        SupportsForms: true
    );

    public void Dispose() { }
}
```

### 4.2 PdfDocumentBuilder (Test Utility)

```csharp
namespace FluentPDF.Rendering.InMemory.Builders;

/// <summary>
/// Fluent builder for creating test PDF documents
/// </summary>
public sealed class PdfDocumentBuilder
{
    private readonly List<InMemoryPage> _pages = new();
    private PdfMetadata _metadata = new("Test Document", "Test Author", "", "", null, null);

    public PdfDocumentBuilder WithPage(Action<PdfPageBuilder> configure)
    {
        var pageBuilder = new PdfPageBuilder();
        configure(pageBuilder);
        _pages.Add(pageBuilder.Build());
        return this;
    }

    public PdfDocumentBuilder WithMetadata(PdfMetadata metadata)
    {
        _metadata = metadata;
        return this;
    }

    public InMemoryDocument Build()
    {
        return new InMemoryDocument(null)
        {
            Pages = _pages,
            Metadata = _metadata
        };
    }

    // Convenience methods
    public static InMemoryDocument CreateSinglePageA4() =>
        new PdfDocumentBuilder()
            .WithPage(p => p.WithSize(PdfSize.A4))
            .Build();
}

public sealed class PdfPageBuilder
{
    private PdfSize _size = PdfSize.A4;
    private string _text = "";

    public PdfPageBuilder WithSize(PdfSize size)
    {
        _size = size;
        return this;
    }

    public PdfPageBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    internal InMemoryPage Build() => new(_size, _text);
}
```

## 5. Service Integration

### 5.1 Dependency Injection Setup

```csharp
// Production configuration
services.AddSingleton<IPdfBackend, PdfiumBackend>();

// Test configuration
services.AddSingleton<IPdfBackend, InMemoryBackend>();
```

### 5.2 Service Migration Example

**Before (tight coupling):**
```csharp
public class PdfDocumentService : IPdfDocumentService
{
    public async Task<PdfDocument> OpenAsync(string path)
    {
        // Direct PDFium P/Invoke
        var handle = PdfiumInterop.FPDF_LoadDocument(path, null);
        return new PdfDocument(handle);
    }
}
```

**After (abstracted):**
```csharp
public class PdfDocumentService : IPdfDocumentService
{
    private readonly IPdfBackend _backend;

    public PdfDocumentService(IPdfBackend backend)
    {
        _backend = backend;
    }

    public async Task<PdfDocument> OpenAsync(string path, CancellationToken ct = default)
    {
        var doc = await _backend.OpenDocumentAsync(path, cancellationToken: ct);
        return new PdfDocument(doc); // Wrap in domain model
    }
}
```

## 6. Testing Strategy

### 6.1 Unit Tests (Fast, No PDFium)

```csharp
public class PdfDocumentServiceTests
{
    private readonly IPdfBackend _backend = new InMemoryBackend();

    [Fact]
    public async Task OpenDocument_ReturnsValidDocument()
    {
        // Arrange
        var service = new PdfDocumentService(_backend);

        // Act
        var doc = await service.OpenAsync("test.pdf");

        // Assert
        Assert.NotNull(doc);
        Assert.True(doc.PageCount > 0);
    }
}
```

### 6.2 Integration Tests (Real PDFium)

```csharp
public class PdfiumIntegrationTests
{
    private readonly IPdfBackend _backend;

    public PdfiumIntegrationTests()
    {
        _backend = new PdfiumBackend(NullLogger<PdfiumBackend>.Instance);
        _backend.Initialize();
    }

    [Fact]
    public async Task RenderPage_ProducesValidBitmap()
    {
        // Use real PDF file
        var doc = await _backend.OpenDocumentAsync("sample.pdf");
        var page = await doc.Pages.GetPageAsync(0);

        var bitmap = await page.RenderAsync(new RenderOptions(Dpi: 150));

        Assert.NotEmpty(bitmap);
    }
}
```

## 7. Migration Plan

### Phase 1: Create Abstractions
1. Create FluentPDF.Rendering.Abstractions project
2. Define all interfaces
3. Define value objects
4. Add to solution

### Phase 2: Implement PDFium Backend
1. Create FluentPDF.Rendering.Pdfium project
2. Move existing interop code
3. Implement PdfiumBackend, PdfiumDocument, PdfiumPage
4. Add integration tests

### Phase 3: Implement Test Backend
1. Create FluentPDF.Rendering.InMemory project
2. Implement InMemoryBackend
3. Add builders for test documents
4. Verify unit tests work without PDFium

### Phase 4: Migrate Services
1. Update PdfDocumentService
2. Update PdfRenderingService
3. Update TextExtractionService
4. Continue with remaining services

### Phase 5: Clean Up
1. Remove direct PDFium references from Core
2. Add architecture tests
3. Update documentation
4. Benchmark performance

## 8. Performance Optimization

### 8.1 Object Pooling

```csharp
public class BitmapBufferPool
{
    private readonly ConcurrentBag<byte[]> _pool = new();

    public byte[] Rent(int size)
    {
        if (_pool.TryTake(out var buffer) && buffer.Length >= size)
            return buffer;

        return new byte[size];
    }

    public void Return(byte[] buffer)
    {
        if (buffer.Length <= 10_000_000) // 10MB max
            _pool.Add(buffer);
    }
}
```

### 8.2 Lazy Loading

- Pages loaded on-demand
- Metadata cached after first access
- Form fields lazy-initialized

### 8.3 Zero-Copy Rendering

```csharp
// Render directly into caller's buffer (no allocation)
var buffer = ArrayPool<byte>.Shared.Rent(width * height * 4);
await page.RenderToBufferAsync(buffer, options);
// Use buffer...
ArrayPool<byte>.Shared.Return(buffer);
```

## 9. Error Handling

### 9.1 Exception Hierarchy

```csharp
public class PdfBackendException : Exception { }
public class PdfDocumentException : PdfBackendException { }
public class PdfRenderingException : PdfBackendException { }
public class PdfPasswordRequiredException : PdfDocumentException { }
```

### 9.2 PDFium Error Mapping

```csharp
private static PdfBackendException MapPdfiumError(int errorCode, string? context = null)
{
    return errorCode switch
    {
        1 => new PdfDocumentException("File not found", context),
        2 => new PdfDocumentException("Invalid format", context),
        3 => new PdfPasswordRequiredException("Password required", context),
        4 => new PdfDocumentException("Security error", context),
        5 => new PdfDocumentException("Page not found", context),
        _ => new PdfBackendException($"Unknown error: {errorCode}", context)
    };
}
```

## 10. Architectural Decision Records

### ADR-001: Abstract PDFium Behind Interfaces

**Context:** Direct PDFium coupling makes testing hard and backend swapping impossible.

**Decision:** Create IPdfBackend abstraction with implementations for PDFium (production) and InMemory (testing).

**Consequences:**
- ✅ Services testable without PDFium
- ✅ Future backend swapping possible
- ✅ Clear separation of concerns
- ⚠️ Small performance overhead (~3-5%)
- ⚠️ Additional abstraction layer to maintain

### ADR-002: Use Value Objects for PDF Types

**Context:** Need immutable, efficient types for PDF primitives.

**Decision:** Use readonly structs for PdfRect, PdfSize, PdfColor, PdfMatrix.

**Consequences:**
- ✅ Zero allocation for common operations
- ✅ Immutability prevents bugs
- ✅ Struct equality by value
- ⚠️ Structs can be copied (not shared)

### ADR-003: Async-First API

**Context:** PDF operations can be slow (file I/O, rendering).

**Decision:** All I/O and rendering operations are async.

**Consequences:**
- ✅ Non-blocking UI
- ✅ Cancellation support
- ✅ Better scalability
- ⚠️ Slightly more complex API

## 11. Documentation

### 11.1 Backend Implementation Guide

Document for implementing alternative backends:
1. Implement IPdfBackend, IPdfDocument, IPdfPage
2. Handle resource disposal correctly
3. Map errors to standard exceptions
4. Write integration tests
5. Register in DI container

### 11.2 Migration Guide for Services

Steps for migrating existing services:
1. Add IPdfBackend dependency
2. Replace direct PDFium calls with backend methods
3. Update tests to use InMemoryBackend
4. Verify integration tests still pass
5. Remove PDFium references from service project

## 12. Success Metrics

- **Abstraction Coverage:** 100% of PDF operations behind interfaces
- **Test Speed:** Unit tests run in &lt;1s (no PDFium)
- **Performance:** &lt;5% overhead vs direct PDFium
- **Code Quality:** Zero direct PDFium references outside Pdfium project
- **Test Coverage:** &gt;90% for abstraction layer
