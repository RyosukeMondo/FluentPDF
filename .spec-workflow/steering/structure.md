# Project Structure

## Directory Organization

```
FluentPDF/
├── src/
│   ├── FluentPDF.App/              # Main WinUI 3 application project
│   │   ├── Views/                  # XAML pages and user controls
│   │   ├── ViewModels/             # MVVM view models
│   │   ├── Models/                 # Domain models and data structures
│   │   ├── Services/               # Application services (file I/O, settings, etc.)
│   │   ├── Converters/             # XAML value converters
│   │   ├── Assets/                 # Images, icons, fonts
│   │   ├── Styles/                 # XAML resource dictionaries
│   │   ├── App.xaml                # Application entry point
│   │   └── Package.appxmanifest    # MSIX manifest with capabilities
│   │
│   ├── FluentPDF.Core/             # Core business logic (C# library)
│   │   ├── Pdf/                    # PDF-specific domain logic
│   │   │   ├── Document.cs         # PDF document abstraction
│   │   │   ├── Page.cs             # Page model
│   │   │   └── Metadata.cs         # PDF metadata handling
│   │   ├── Conversion/             # Document conversion logic
│   │   │   ├── DocxToPdf.cs        # Mammoth + WebView2 pipeline
│   │   │   └── ImageToPdf.cs       # Future: image conversion
│   │   └── Editing/                # Document manipulation logic
│   │       ├── Merger.cs           # Combine PDFs
│   │       ├── Splitter.cs         # Split PDFs
│   │       └── Optimizer.cs        # Compression and linearization
│   │
│   ├── FluentPDF.Rendering/        # Rendering engine (C# + P/Invoke)
│   │   ├── Interop/                # P/Invoke declarations
│   │   │   ├── PdfiumNative.cs     # PDFium API bindings
│   │   │   └── QpdfNative.cs       # QPDF API bindings (if not using CLI)
│   │   ├── PdfiumRenderer.cs       # High-level PDFium rendering wrapper
│   │   ├── RenderContext.cs        # Render state management
│   │   ├── TileCache.cs            # Memory-efficient tile caching
│   │   └── BitmapConverter.cs      # BGRA buffer → SoftwareBitmap conversion
│   │
│   └── FluentPDF.Native/           # C++ wrapper projects (optional)
│       ├── PdfiumWrapper/          # C++/CLI or COM wrapper for PDFium
│       └── QpdfWrapper/            # C++/CLI or COM wrapper for QPDF
│
├── tests/
│   ├── FluentPDF.Core.Tests/       # Unit tests for business logic
│   │   ├── Pdf/                    # Domain logic tests
│   │   ├── Editing/                # Document manipulation tests
│   │   └── Conversion/             # Conversion pipeline tests
│   ├── FluentPDF.Rendering.Tests/  # Rendering pipeline tests
│   │   ├── Integration/            # PDFium integration tests
│   │   ├── Performance/            # BenchmarkDotNet benchmarks
│   │   └── Visual/                 # Verify.Xaml snapshot tests
│   ├── FluentPDF.App.Tests/        # UI automation tests (FlaUI)
│   │   ├── PageObjects/            # Page Object Pattern implementations
│   │   └── Scenarios/              # End-to-end user scenarios
│   ├── FluentPDF.Architecture.Tests/  # ArchUnitNET tests
│   │   ├── LayerTests.cs           # Layer dependency rules
│   │   ├── NamingTests.cs          # Naming convention enforcement
│   │   └── DependencyTests.cs      # Interface usage rules
│   └── FluentPDF.Validation.Tests/ # PDF quality validation
│       ├── QpdfTests.cs            # QPDF structural validation
│       ├── VeraPdfTests.cs         # PDF/A compliance checks
│       └── VisualRegressionTests.cs # SSIM-based visual comparison
│
├── libs/                            # vcpkg-built native libraries
│   ├── x64/
│   │   ├── pdfium.dll
│   │   ├── libqpdf.dll
│   │   └── dependencies/           # zlib, freetype, etc.
│   └── arm64/                      # ARM64 builds for Surface Pro X
│       └── [same structure]
│
├── tools/
│   ├── vcpkg/                      # Git submodule or local vcpkg clone
│   ├── build-libs.ps1              # Script to build PDFium/QPDF via vcpkg
│   ├── package-msix.ps1            # MSIX packaging script
│   ├── validation/                 # PDF validation CLI tools
│   │   ├── qpdf.exe                # QPDF command-line tool
│   │   ├── verapdf/                # VeraPDF installation
│   │   └── jhove/                  # JHOVE installation
│   └── ai-quality-agent/           # AI quality assessment scripts
│       ├── analyze-tests.ps1       # TRX analysis orchestrator
│       ├── analyze-logs.ps1        # Log aggregation and analysis
│       ├── generate-report.ps1     # Quality report generator
│       └── schemas/                # JSON Schema for structured outputs
│           └── quality-report.schema.json
│
├── docs/
│   ├── research.md                 # Original research document (Japanese)
│   ├── autonomous_deterministic.md # Quality assurance architecture (Japanese)
│   ├── architecture.md             # Architecture decision records
│   ├── observability.md            # Logging, tracing, metrics guide
│   ├── testing-strategy.md         # Testing pyramid and strategies
│   └── api/                        # Generated API documentation
│
├── .spec-workflow/                 # Spec workflow system
│   ├── steering/                   # Steering documents (this file)
│   ├── templates/                  # Spec templates
│   └── specs/                      # Feature specifications
│
├── .github/
│   └── workflows/
│       ├── build.yml               # CI build validation
│       ├── test.yml                # Automated testing pipeline
│       ├── quality-analysis.yml    # AI quality agent execution
│       ├── visual-regression.yml   # Visual testing with Win2D
│       └── release.yml             # Store package creation
│
├── FluentPDF.sln                   # Visual Studio solution
├── Directory.Build.props           # Shared MSBuild properties
├── .editorconfig                   # Code style rules
└── README.md                       # Project overview and setup instructions
```

## Naming Conventions

### Files
- **XAML Views**: `PascalCase` + descriptive suffix
  - Pages: `DocumentViewerPage.xaml`, `SettingsPage.xaml`
  - Controls: `PageThumbnailControl.xaml`, `ToolbarControl.xaml`
- **C# Classes**: `PascalCase.cs`
  - ViewModels: `DocumentViewerViewModel.cs`
  - Services: `FileStorageService.cs`, `SettingsManager.cs`
- **Interop**: `PascalCaseNative.cs` for P/Invoke wrappers
  - Example: `PdfiumNative.cs`, `QpdfNative.cs`
- **Tests**: `[ClassName]Tests.cs`
  - Example: `DocumentTests.cs`, `PdfiumRendererTests.cs`

### Code
- **Classes/Types**: `PascalCase`
  - `DocumentViewModel`, `PdfDocument`, `RenderOptions`
- **Interfaces**: `IPascalCase`
  - `IRenderer`, `IDocumentConverter`, `ICacheStrategy`
- **Methods**: `PascalCase` (C# convention)
  - `RenderPageAsync()`, `MergeDocuments()`, `GetMetadata()`
- **Private fields**: `_camelCase` with underscore prefix
  - `_pdfiumHandle`, `_renderCache`, `_documentPath`
- **Properties**: `PascalCase`
  - `PageCount`, `IsEncrypted`, `CurrentPage`
- **Constants**: `PascalCase` or `UPPER_SNAKE_CASE` for native interop
  - `MaxCacheSize`, `DefaultDpi`
  - `FPDF_ANNOT`, `FPDF_RENDER_NO_SMOOTHTEXT` (matching PDFium API)
- **Enums**: `PascalCase` type, `PascalCase` values
  - `RenderQuality { Low, Medium, High }`

## Import Patterns

### Import Order (C#)
1. System namespaces (`using System;`, `using System.Collections.Generic;`)
2. External dependencies (`using Microsoft.UI.Xaml;`, `using Mammoth;`)
3. Internal project references (`using FluentPDF.Core.Pdf;`)
4. Relative/local imports (rare in C#)

### Namespace Organization
- **Root namespace**: `FluentPDF`
- **Project-based sub-namespaces**:
  - `FluentPDF.App.Views`, `FluentPDF.App.ViewModels`
  - `FluentPDF.Core.Pdf`, `FluentPDF.Core.Editing`
  - `FluentPDF.Rendering.Interop`
- **Absolute references**: All projects reference via project references in solution

## Code Structure Patterns

### File Organization (C# Classes)

**Standard class file structure:**
```csharp
// 1. Using directives (grouped by System, External, Internal)
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using FluentPDF.Core.Pdf;

// 2. Namespace declaration
namespace FluentPDF.Rendering;

// 3. XML doc comment for class
/// <summary>
/// High-performance PDF renderer using PDFium engine.
/// </summary>

// 4. Class declaration with attributes
[Serializable]
public sealed class PdfiumRenderer : IDisposable
{
    // 5. Constants
    private const int MaxCacheSize = 100;

    // 6. Fields (grouped by access level)
    private readonly IntPtr _documentHandle;
    private readonly TileCache _cache;

    // 7. Constructors
    public PdfiumRenderer(string filePath) { ... }

    // 8. Public properties
    public int PageCount { get; }

    // 9. Public methods
    public Task<SoftwareBitmap> RenderPageAsync(int pageIndex) { ... }

    // 10. Private/helper methods
    private void ValidatePageIndex(int index) { ... }

    // 11. IDisposable implementation
    public void Dispose() { ... }
}
```

### XAML Page Organization
```xaml
<!-- 1. Root element with namespaces -->
<Page x:Class="FluentPDF.App.Views.DocumentViewerPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 2. Page resources (styles, converters) -->
    <Page.Resources>
        <local:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
    </Page.Resources>

    <!-- 3. Layout root -->
    <Grid>
        <!-- 4. UI structure (logical top-to-bottom or left-to-right) -->
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- Toolbar -->
            <RowDefinition Height="*"/>    <!-- Content -->
        </Grid.RowDefinitions>

        <!-- 5. Controls (ordered by grid position) -->
        <CommandBar Grid.Row="0"/>
        <ScrollViewer Grid.Row="1">
            <Image x:Name="PageImage"/>
        </ScrollViewer>
    </Grid>
</Page>
```

### P/Invoke Interop Pattern
```csharp
namespace FluentPDF.Rendering.Interop;

/// <summary>
/// P/Invoke declarations for PDFium library (pdfium.dll).
/// </summary>
internal static class PdfiumNative
{
    private const string DllName = "pdfium.dll";

    // Grouped by functionality (Document, Page, Rendering, etc.)

    #region Document Management

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr FPDF_LoadDocument(string file_path, string password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_CloseDocument(IntPtr document);

    #endregion

    #region Rendering

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_RenderPageBitmap(
        IntPtr bitmap,
        IntPtr page,
        int start_x,
        int start_y,
        int size_x,
        int size_y,
        int rotate,
        int flags);

    #endregion
}
```

## Code Organization Principles

1. **Single Responsibility**: Each class should have one clear purpose
   - `PdfiumRenderer` handles rendering only, not document parsing or editing
   - `DocumentViewModel` manages UI state, not business logic
   - `FileStorageService` handles file I/O, not PDF manipulation

2. **Separation of Concerns**:
   - **UI (Views)**: XAML only, minimal code-behind (event wiring only)
   - **Presentation (ViewModels)**: UI state, commands, property change notifications
   - **Business Logic (Core)**: Domain logic independent of UI framework
   - **Rendering (Rendering)**: Low-level interop and performance-critical code

3. **Dependency Injection**:
   - Use constructor injection for services
   - Register services in `App.xaml.cs` using `Microsoft.Extensions.DependencyInjection`
   - Example: `services.AddSingleton<IPdfRenderer, PdfiumRenderer>()`

4. **Async/Await**:
   - All I/O operations must be asynchronous (`Task<T>` return types)
   - Rendering operations use `Task.Run` for CPU-bound work off UI thread
   - Cancel long operations via `CancellationToken`

## Module Boundaries

### Core Boundaries
- **FluentPDF.Core** ← No dependencies on UI (WinUI) or rendering (PDFium)
  - Pure business logic, can be unit tested without UI
- **FluentPDF.Rendering** ← Depends on Core, no dependency on App/UI
  - Can render PDFs without WinUI (useful for testing)
- **FluentPDF.App** ← Depends on Core and Rendering, orchestrates everything

### Interop Isolation
- **Native code isolation**: All P/Invoke in `FluentPDF.Rendering.Interop` namespace
- **Unsafe code limited**: Only in `BitmapConverter` and interop layer
- **Error translation**: Native error codes translated to C# exceptions at interop boundary

### Platform-Specific Code
- **Windows-only APIs** (WinUI, Windows.Storage): Isolated in `FluentPDF.App`
- **Cross-platform potential**: Core and Rendering projects use .NET Standard where possible for future Uno Platform port

## Code Size Guidelines

**Enforced via CLAUDE.md directives:**
- **File size**: Maximum 500 lines (excluding comments/blank lines)
  - If exceeded, split into multiple files or refactor
- **Function/Method size**: Maximum 50 lines
  - Extract helper methods or decompose into smaller functions
- **Class complexity**: Single responsibility, max ~10 public members
  - Large classes indicate violation of SRP—split into multiple classes
- **Nesting depth**: Maximum 3 levels
  - Use early returns and guard clauses to reduce nesting

**Testing Requirements:**
- **Code coverage**: 80% minimum, 90% for critical paths (rendering, file I/O)
- **Test file naming**: `[ClassName]Tests.cs`
- **Test method naming**: `[MethodName]_[Scenario]_[ExpectedBehavior]`
  - Example: `RenderPage_WithInvalidIndex_ThrowsArgumentOutOfRangeException`

## Threading & Concurrency Patterns

### Thread Affinity
- **UI Thread**: All WinUI controls and XAML access
- **Render Threads**: Background `Task.Run` for PDFium rendering (CPU-bound)
- **I/O Threads**: `async`/`await` for file operations (I/O-bound)

### Synchronization
- **PDFium document handle**: Not thread-safe—use `lock` or `SemaphoreSlim` for access
- **Cache updates**: `ConcurrentDictionary` for tile cache
- **DispatcherQueue**: Use `DispatcherQueue.TryEnqueue` to marshal results back to UI thread

### Example Pattern
```csharp
public async Task<SoftwareBitmap> RenderPageAsync(int pageIndex)
{
    // Validate on calling thread
    ValidatePageIndex(pageIndex);

    // CPU-bound work on background thread
    var pixels = await Task.Run(() => RenderToPixels(pageIndex));

    // Marshal back to UI thread for SoftwareBitmap creation
    SoftwareBitmap bitmap = null;
    await _dispatcherQueue.EnqueueAsync(() =>
    {
        bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            pixels, BitmapPixelFormat.Bgra8, width, height);
    });

    return bitmap;
}
```

## Error Handling Standards

### Result Pattern with FluentResults

**Primary approach: Use Result<T> for domain operations, reserve exceptions for truly exceptional conditions.**

```csharp
using FluentResults;

namespace FluentPDF.Core.Editing;

/// <summary>
/// Structured error with rich context for AI analysis.
/// </summary>
public class PdfError : Error
{
    public string ErrorCode { get; init; }
    public ErrorCategory Category { get; init; }
    public ErrorSeverity Severity { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();

    public PdfError(string errorCode, string message, ErrorCategory category, ErrorSeverity severity)
        : base(message)
    {
        ErrorCode = errorCode;
        Category = category;
        Severity = severity;

        // Add metadata for AI analysis
        Metadata.Add("ErrorCode", errorCode);
        Metadata.Add("Category", category.ToString());
        Metadata.Add("Severity", severity.ToString());
    }
}

public enum ErrorCategory
{
    Validation,    // User input or data validation errors
    System,        // Infrastructure failures (file I/O, memory)
    Security,      // Permission or encryption issues
    IO,            // File system operations
    Rendering,     // PDF rendering failures
    Conversion     // Document conversion errors
}

public enum ErrorSeverity
{
    Critical,  // Application cannot continue
    Error,     // Operation failed but app remains stable
    Warning,   // Potential issue, operation succeeded with caveats
    Info       // Informational message
}

/// <summary>
/// Example service using Result pattern.
/// </summary>
public class PdfMergeService
{
    private readonly ILogger<PdfMergeService> _logger;

    public async Task<Result<PdfDocument>> MergeDocumentsAsync(
        IEnumerable<string> filePaths,
        CancellationToken cancellationToken)
    {
        // Generate correlation ID for tracing
        var correlationId = Guid.NewGuid().ToString();
        using var _ = LogContext.PushProperty("CorrelationId", correlationId);

        _logger.Information("Starting merge operation for {FileCount} files", filePaths.Count());

        // Validation
        if (!filePaths.Any())
        {
            var error = new PdfError(
                "PDF_MERGE_NO_FILES",
                "No files provided for merge operation",
                ErrorCategory.Validation,
                ErrorSeverity.Error
            );
            error.Context["CorrelationId"] = correlationId;

            _logger.Warning("Merge validation failed: {ErrorCode}", error.ErrorCode);
            return Result.Fail(error);
        }

        try
        {
            // Actual merge logic using QPDF
            var result = await PerformMergeAsync(filePaths, cancellationToken);

            _logger.Information("Merge completed successfully: {PageCount} pages",
                result.PageCount);

            return Result.Ok(result);
        }
        catch (IOException ex)
        {
            var error = new PdfError(
                "PDF_MERGE_IO_ERROR",
                $"File I/O error during merge: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error
            );
            error.Context["CorrelationId"] = correlationId;
            error.Context["FilePaths"] = filePaths.ToArray();
            error.CausedBy(ex);

            _logger.Error(ex, "Merge I/O error: {ErrorCode}", error.ErrorCode);
            return Result.Fail(error);
        }
    }
}
```

### Exception Hierarchy (for truly exceptional conditions)

**Use exceptions ONLY for:**
- Infrastructure failures (out of memory, stack overflow)
- Programming errors (null reference, index out of range)
- Unrecoverable system errors

```
FluentPDFException (base)
├── PdfLoadException            # Critical failure loading PDF
├── PdfRenderException          # Unrecoverable rendering failure
└── InfrastructureException     # System-level failures
```

### Global Exception Handling (WinUI 3)

```csharp
// App.xaml.cs
public App()
{
    InitializeComponent();

    // UI thread exceptions
    UnhandledException += OnUnhandledException;

    // Background task exceptions
    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

    // Non-UI thread exceptions
    AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
}

private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    var correlationId = Guid.NewGuid().ToString();
    Log.Fatal(e.Exception,
        "Unhandled UI exception [CorrelationId: {CorrelationId}]",
        correlationId);

    // Show user-friendly error dialog
    ShowErrorDialog(e.Exception, correlationId);

    e.Handled = true; // Prevent crash if possible
}
```

### Structured Logging with Correlation IDs

```csharp
// Service layer: Start operation with correlation ID
public async Task<Result> ProcessDocumentAsync(string filePath)
{
    var correlationId = Guid.NewGuid().ToString();
    using var _ = LogContext.PushProperty("CorrelationId", correlationId);
    using var __ = LogContext.PushProperty("FilePath", filePath);

    _logger.Information("Starting document processing");

    var validationResult = await ValidateDocumentAsync(filePath);
    if (validationResult.IsFailed)
    {
        _logger.Warning("Validation failed: {Errors}",
            validationResult.Errors.Select(e => e.Message));
        return validationResult;
    }

    // Correlation ID automatically included in all logs within this scope
    _logger.Information("Document processing complete");
    return Result.Ok();
}
```

### Logging Configuration (Serilog + OpenTelemetry)

```csharp
// Program.cs or App.xaml.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FluentPDF")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Async(a => a.File(
        new JsonFormatter(),
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs", "log-.json"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7))
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://localhost:4317"; // .NET Aspire Dashboard
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "FluentPDF.Desktop",
            ["service.version"] = "1.0.0",
            ["deployment.environment"] = "development"
        };
    })
    .CreateLogger();
```

## Documentation Standards

### XML Documentation
- **All public APIs**: Must have `<summary>`, `<param>`, `<returns>`
- **Complex logic**: Inline comments explaining "why," not "what"
- **P/Invoke methods**: Document PDFium API reference URL

Example:
```csharp
/// <summary>
/// Renders a PDF page to a WinUI SoftwareBitmap with HiDPI scaling.
/// </summary>
/// <param name="pageIndex">Zero-based page index.</param>
/// <param name="scale">Rasterization scale (1.0 = 96 DPI, 2.0 = 192 DPI).</param>
/// <returns>BGRA8 SoftwareBitmap suitable for WinUI Image control.</returns>
/// <exception cref="ArgumentOutOfRangeException">pageIndex is invalid.</exception>
public async Task<SoftwareBitmap> RenderPageAsync(int pageIndex, double scale)
```

### Architecture Documentation
- **docs/architecture.md**: Major architectural decisions (ADRs)
- **README.md per project**: Setup instructions, purpose, dependencies
- **API docs**: Auto-generated via DocFX from XML comments

## Testing Patterns & Quality Assurance

### Architecture Testing with ArchUnitNET

**Purpose: Prevent architectural erosion through automated enforcement of design rules.**

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

public class LayerTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(FluentPDF.Core.Pdf.Document).Assembly,      // Core
            typeof(FluentPDF.Rendering.PdfiumRenderer).Assembly, // Rendering
            typeof(FluentPDF.App.App).Assembly                  // App
        ).Build();

    [Fact]
    public void CoreLayer_ShouldNot_DependOn_UILayer()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Core layer must be UI-agnostic for testability");

        rule.Check(Architecture);
    }

    [Fact]
    public void CoreLayer_ShouldNot_DependOn_RenderingLayer()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering"))
            .Because("Core layer should not depend on rendering implementation");

        rule.Check(Architecture);
    }

    [Fact]
    public void ViewModels_Should_InheritFrom_ObservableObject()
    {
        var rule = Classes()
            .That().HaveNameEndingWith("ViewModel")
            .Should().Inherit(typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableObject))
            .Because("ViewModels must use CommunityToolkit.Mvvm for consistency");

        rule.Check(Architecture);
    }

    [Fact]
    public void Services_Should_UseInterfaces()
    {
        var rule = Classes()
            .That().HaveNameEndingWith("Service")
            .Should().ImplementInterface("I" + ".*Service")
            .Because("Services must be abstracted via interfaces for DI and testing");

        rule.Check(Architecture);
    }
}
```

### Visual Regression Testing with Verify.Xaml

**Purpose: Detect unintended UI changes through snapshot testing.**

```csharp
using FluentPDF.App.Views;
using Microsoft.UI.Xaml.Controls;
using VerifyXunit;
using Xunit;

namespace FluentPDF.Rendering.Tests.Visual;

[UsesVerify]
public class PdfViewerVisualTests
{
    [Fact]
    public async Task PdfViewer_WithSampleDocument_MatchesSnapshot()
    {
        // Arrange: Create control with test data
        var viewer = new PdfViewerControl();
        await viewer.LoadDocumentAsync("TestData/sample.pdf");

        // Act & Assert: Verify against approved snapshot
        await Verifier.Verify(viewer)
            .UseDirectory("Snapshots");
    }

    [Fact]
    public async Task PdfViewer_ZoomIn_MatchesSnapshot()
    {
        var viewer = new PdfViewerControl();
        await viewer.LoadDocumentAsync("TestData/sample.pdf");

        viewer.ZoomLevel = 200; // 200% zoom

        await Verifier.Verify(viewer)
            .UseDirectory("Snapshots")
            .UseFileName("PdfViewer_Zoomed");
    }
}
```

### Visual Regression with SSIM (Headless Rendering)

**Purpose: CI-friendly visual testing without UI dependencies.**

```csharp
using Microsoft.Graphics.Canvas;
using OpenCvSharp;
using Xunit;

namespace FluentPDF.Validation.Tests;

public class VisualRegressionTests
{
    [Theory]
    [InlineData("sample.pdf", 0)] // Page 0
    [InlineData("sample.pdf", 1)] // Page 1
    public async Task RenderPage_ShouldMatch_Baseline(string filename, int pageIndex)
    {
        // Arrange
        var pdfPath = Path.Combine("TestData", filename);
        var baselinePath = Path.Combine("Baselines", $"{filename}_page{pageIndex}.png");

        // Act: Render using Win2D (headless)
        using var device = CanvasDevice.GetSharedDevice();
        using var renderTarget = new CanvasRenderTarget(device, 1920, 1080, 96);

        using (var session = renderTarget.CreateDrawingSession())
        {
            // Render PDF page to CanvasRenderTarget
            await RenderPageToCanvasAsync(pdfPath, pageIndex, session);
        }

        var actualBytes = renderTarget.GetPixelBytes();

        // Assert: Compare using SSIM
        var ssimScore = CompareImagesSSIM(baselinePath, actualBytes);

        Assert.True(ssimScore > 0.99,
            $"Visual regression detected! SSIM: {ssimScore:F4} (threshold: 0.99)");
    }

    private double CompareImagesSSIM(string baselinePath, byte[] actualBytes)
    {
        using var baseline = Cv2.ImRead(baselinePath);
        using var actual = Mat.FromPixelData(1080, 1920, MatType.CV_8UC4, actualBytes);

        // Convert to grayscale for SSIM
        using var baselineGray = new Mat();
        using var actualGray = new Mat();
        Cv2.CvtColor(baseline, baselineGray, ColorConversionCodes.BGRA2GRAY);
        Cv2.CvtColor(actual, actualGray, ColorConversionCodes.BGRA2GRAY);

        // Calculate SSIM
        var ssim = Cv2.SSIM(baselineGray, actualGray);
        return ssim.Val0; // Return scalar value
    }
}
```

### Performance Benchmarking with BenchmarkDotNet

**Purpose: Measure and track performance of critical paths.**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace FluentPDF.Rendering.Tests.Performance;

[MemoryDiagnoser]
[NativeMemoryProfiler]
public class PdfRenderingBenchmarks
{
    private PdfiumRenderer _renderer;
    private string _testPdfPath;

    [GlobalSetup]
    public void Setup()
    {
        _testPdfPath = Path.Combine("TestData", "sample_100pages.pdf");
        _renderer = new PdfiumRenderer(_testPdfPath);
    }

    [Benchmark]
    [Arguments(0)]   // First page
    [Arguments(50)]  // Middle page
    [Arguments(99)]  // Last page
    public async Task<SoftwareBitmap> RenderPage(int pageIndex)
    {
        return await _renderer.RenderPageAsync(pageIndex, scale: 1.0);
    }

    [Benchmark]
    public async Task RenderAllPages_Sequential()
    {
        for (int i = 0; i < _renderer.PageCount; i++)
        {
            await _renderer.RenderPageAsync(i, scale: 1.0);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _renderer.Dispose();
    }
}

// Run benchmarks:
// dotnet run --project FluentPDF.Rendering.Tests.Performance -c Release
```

### UI Automation with FlaUI (Page Object Pattern)

**Purpose: Stable, maintainable UI tests using Page Object Pattern.**

```csharp
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace FluentPDF.App.Tests.PageObjects;

public class MainWindowPage
{
    private readonly Window _window;

    public MainWindowPage(Window window)
    {
        _window = window;
    }

    public Button OpenFileButton =>
        _window.FindFirstDescendant(cf => cf.ByAutomationId("OpenFileButton"))
               .AsButton();

    public TextBox FilePathTextBox =>
        _window.FindFirstDescendant(cf => cf.ByAutomationId("FilePathTextBox"))
               .AsTextBox();

    public void OpenFile(string filePath)
    {
        OpenFileButton.Click();
        // Handle file picker dialog...
    }
}

public class DocumentViewerTests : IDisposable
{
    private readonly Application _app;
    private readonly UIA3Automation _automation;
    private MainWindowPage _mainWindow;

    public DocumentViewerTests()
    {
        _automation = new UIA3Automation();
        _app = Application.Launch("FluentPDF.App.exe");
        _mainWindow = new MainWindowPage(_app.GetMainWindow(_automation));
    }

    [Fact]
    public void OpenFile_ValidPdf_DisplaysDocument()
    {
        // Arrange
        var testPdfPath = Path.Combine("TestData", "sample.pdf");

        // Act
        _mainWindow.OpenFile(testPdfPath);

        // Assert
        var filePathText = _mainWindow.FilePathTextBox.Text;
        Assert.Equal(testPdfPath, filePathText);
    }

    public void Dispose()
    {
        _app?.Close();
        _automation?.Dispose();
    }
}
```

### PDF Validation Testing

**Purpose: Ensure generated PDFs meet ISO standards and compliance requirements.**

```csharp
using System.Diagnostics;
using Xunit;

namespace FluentPDF.Validation.Tests;

public class QpdfValidationTests
{
    [Theory]
    [InlineData("output_merged.pdf")]
    [InlineData("output_optimized.pdf")]
    public async Task GeneratedPdf_ShouldPass_QpdfValidation(string filename)
    {
        // Arrange
        var pdfPath = Path.Combine("Output", filename);

        // Act: Run QPDF validation
        var result = await RunQpdfCheckAsync(pdfPath);

        // Assert
        Assert.True(result.IsSuccess,
            $"QPDF validation failed: {result.ErrorMessage}");
        Assert.Equal(0, result.ExitCode);
    }

    private async Task<ValidationResult> RunQpdfCheckAsync(string pdfPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "qpdf",
                Arguments = $"--check \"{pdfPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ValidationResult
        {
            IsSuccess = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            Output = output,
            ErrorMessage = error
        };
    }
}
```

## AI Quality Agent Integration

### Quality Report Schema

**Purpose: Structured outputs for AI analysis that can be consumed by dashboards and CI systems.**

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "FluentPDF Quality Report",
  "type": "object",
  "required": ["summary", "overallScore", "status", "analysis"],
  "properties": {
    "summary": {
      "type": "string",
      "description": "High-level summary of quality assessment"
    },
    "overallScore": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "Overall quality score (0-100)"
    },
    "status": {
      "type": "string",
      "enum": ["Pass", "Warning", "Fail"],
      "description": "Overall status of the build"
    },
    "analysis": {
      "type": "object",
      "properties": {
        "validity": {
          "type": "object",
          "properties": {
            "status": { "type": "string", "enum": ["Pass", "Fail"] },
            "details": { "type": "string" }
          }
        },
        "visual": {
          "type": "object",
          "properties": {
            "status": { "type": "string", "enum": ["Pass", "Warning", "Fail"] },
            "details": { "type": "string" },
            "ssimScores": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "testName": { "type": "string" },
                  "score": { "type": "number" }
                }
              }
            }
          }
        },
        "logs": {
          "type": "object",
          "properties": {
            "criticalErrors": { "type": "integer" },
            "warnings": { "type": "integer" },
            "patterns": {
              "type": "array",
              "items": { "type": "string" }
            }
          }
        }
      }
    },
    "rootCauseHypothesis": {
      "type": "string",
      "description": "AI-generated hypothesis about root causes of failures"
    },
    "recommendations": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Actionable recommendations for improvement"
    }
  }
}
```
