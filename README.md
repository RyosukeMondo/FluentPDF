# FluentPDF

High-quality, ethically-designed PDF application for Windows built on WinUI 3.

[![Build Status](https://github.com/rmondo/FluentPDF/actions/workflows/build.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/build.yml)
[![Tests](https://github.com/rmondo/FluentPDF/actions/workflows/test.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/test.yml)
[![Quality Analysis](https://github.com/rmondo/FluentPDF/actions/workflows/quality-analysis.yml/badge.svg)](https://github.com/rmondo/FluentPDF/actions/workflows/quality-analysis.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

### PDF Viewing
- **Multi-Tab Interface**: Work with multiple PDF files simultaneously using WinUI 3 TabView
- **Recent Files**: Quick access to recently opened files via File menu and Windows Jump List
- **High-Quality Rendering**: View PDF documents using Google's PDFium rendering engine
- **HiDPI Display Scaling**: Automatic crisp rendering on high-resolution displays (4K, Surface devices)
  - Automatic DPI detection (100%-300% scaling)
  - Dynamic quality adjustment when moving between monitors
  - User-controlled quality settings (Auto, Low, Medium, High, Ultra)
  - Optimized performance for modern displays
- **Text Extraction and Search**: Extract text from PDF pages and search within documents with visual highlighting
- **Bookmark Navigation**: Hierarchical bookmark panel with TreeView for quick document navigation
- **Page Navigation**: Navigate through documents with previous/next buttons or arrow keys
- **Zoom Controls**: Zoom in/out with preset levels (50% to 200%) or keyboard shortcuts
- **File Picker Integration**: Open PDF files with native Windows file picker
- **Loading Indicators**: Visual feedback during document loading and page rendering
- **Error Handling**: Graceful error handling for corrupted or invalid PDF files

### PDF Form Filling
- **Interactive Form Fields**: Fill PDF forms with text fields, checkboxes, and radio buttons
- **Overlay Controls**: Form fields rendered as WinUI controls overlaid on PDF pages
- **Keyboard Navigation**: Tab through form fields in document-defined tab order
- **Real-Time Validation**: Validate required fields, max length, and format masks as you type
- **Visual Feedback**: Clear visual states for hover, focus, error, and read-only fields
- **Form Data Persistence**: Save filled form data back to PDF using PDFium's native save API
- **Error Display**: InfoBar shows validation errors with clear, actionable messages

### Office Document Conversion
- **DOCX to PDF Conversion**: Convert Microsoft Word documents to PDF with high quality
- **Semantic Parsing**: Uses Mammoth.NET to preserve document structure and formatting
- **Chromium Rendering**: WebView2-based PDF generation for professional-quality output
- **Quality Validation**: Optional SSIM-based comparison against LibreOffice baseline
- **Progress Tracking**: Real-time conversion progress with status indicators
- **Error Recovery**: Comprehensive error handling with clear user feedback
- **Batch Support**: Queue multiple conversions for efficient processing

### PDF Validation
- **PDF/A Compliance**: VeraPDF integration for archival standard validation (PDF/A-1, PDF/A-2, PDF/A-3)
- **Format Validation**: JHOVE integration for PDF format characterization and metadata extraction
- **Structural Validation**: QPDF integration for cross-reference table and corruption detection
- **Flexible Profiles**: Quick (QPDF), Standard (QPDF+JHOVE), Full (all tools) validation modes
- **Parallel Execution**: Multiple validation tools run concurrently for performance
- **Comprehensive Reports**: JSON-serializable validation reports with detailed error information
- **CI/CD Integration**: Automated validation in GitHub Actions workflows

### Architecture & Quality
- **Modern MVVM architecture** with CommunityToolkit.Mvvm source generators
- **Verifiable quality** with ArchUnitNET automated architecture tests
- **Comprehensive observability** with Serilog + OpenTelemetry
- **Type-safe error handling** using FluentResults Result pattern
- **Testable architecture** with dependency injection and interface-based design
- **Memory-safe P/Invoke** with SafeHandle pattern for native interop

## Performance

FluentPDF is designed for high performance and low memory usage:

- **Fast Application Launch**: Cold start < 2 seconds (P99)
- **Responsive Rendering**: Page render < 1 second (P99) for text-heavy documents at 100% zoom
- **Efficient Memory Usage**: < 200MB for single document, no memory leaks in sustained operation
- **Smooth Navigation**: Page navigation < 1 second (P99), zoom changes < 2 seconds (P99)

**Performance Monitoring**:
- Comprehensive benchmark suite using BenchmarkDotNet
- Automated regression detection in CI (fails build if > 20% slower)
- Baseline tracking for historical performance trends
- Detailed performance reports with P50/P95/P99 latencies

See [PERFORMANCE.md](docs/PERFORMANCE.md) for detailed performance characteristics and [Benchmarks README](tests/FluentPDF.Benchmarks/README.md) for running benchmarks locally.

## Prerequisites

- **Windows 10 (version 1809 or later)** or **Windows 11**
- **Visual Studio 2022** with the following workloads:
  - .NET desktop development
  - Desktop development with C++ (for vcpkg)
  - Windows application development (for WinUI 3)
- **Git** (for cloning and vcpkg)
- **PowerShell 5.1+** (included with Windows)
- **WebView2 Runtime** (for DOCX conversion - usually pre-installed on Windows 11)
  - Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703
- **LibreOffice** (optional, for quality validation)
  - Download: https://www.libreoffice.org/download/
- **Java Runtime Environment** (for JHOVE validation tool)
  - Download: https://adoptium.net/
  - Minimum version: Java 8

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/FluentPDF.git
cd FluentPDF
```

### 2. Build Native Dependencies

Build PDFium and QPDF native libraries using the automated vcpkg script:

```powershell
.\tools\build-libs.ps1
```

**Note:** First-time build takes approximately 30-60 minutes as vcpkg compiles PDFium and QPDF from source.

**For ARM64 Windows:**
```powershell
.\tools\build-libs.ps1 -Triplet arm64-windows
```

**For faster subsequent builds (with binary caching):**
```powershell
.\tools\build-libs.ps1 -UseCache
```

**To force a clean rebuild:**
```powershell
.\tools\build-libs.ps1 -Clean
```

### 3. Install PDF Validation Tools (Optional)

For PDF validation functionality, install the validation tools:

```powershell
pwsh .\tools\validation\install-tools.ps1
```

This installs:
- **VeraPDF** 1.26.1 - PDF/A compliance validator
- **JHOVE** 1.30.1 - PDF format characterization (requires Java)
- **QPDF** 11.9.1 - Structural validation tool

See [tools/validation/README.md](tools/validation/README.md) for detailed installation instructions.

### 4. Open and Build the Solution

1. Open `FluentPDF.sln` in Visual Studio 2022
2. Restore NuGet packages (automatic on first build)
3. Build the solution: **Ctrl+Shift+B**
4. Run the application: **F5**

**Command-line build:**
```bash
dotnet restore FluentPDF.sln
dotnet build FluentPDF.sln
```

## Usage

### Opening a PDF

1. Click the **Open** button in the toolbar (or press **Ctrl+O**)
2. Select a PDF file from the file picker
3. The first page will be displayed automatically in a new tab

### Working with Multiple PDFs (Tabs)

FluentPDF supports opening multiple PDF files simultaneously using tabs:

- **Open in New Tab**: Each PDF opens in its own tab, allowing you to switch between documents
- **Tab Switching**: Click on any tab to switch to that document
- **Close Tab**: Click the X button on a tab to close that document
- **Prevent Duplicates**: Opening the same file again activates the existing tab instead of creating a duplicate
- **Active Tab**: The currently selected tab is highlighted and shows its content

### Recent Files and Jump List

FluentPDF tracks your recently opened files for quick access:

- **Recent Files Menu**: Click **File** → **Recent Files** to see up to 10 recently opened PDFs
- **Windows Jump List**: Right-click the FluentPDF taskbar icon to see recent files in the Jump List
- **Quick Open**: Click any recent file to open it immediately (or activate if already open)
- **Clear Recent**: Click **File** → **Clear Recent Files** to remove all recent files from the list
- **Automatic Cleanup**: Deleted or moved files are automatically removed from recent files list

**Recent Files Features**:
- Most Recently Used (MRU) ordering - most recent files appear first
- Persistent across application restarts
- Maximum 10 items per Windows guidelines
- Integrated with Windows taskbar for native OS experience

### Navigating Pages

- **Next Page**: Click the **Next** button or press **Right Arrow**
- **Previous Page**: Click the **Previous** button or press **Left Arrow**
- **Page Indicator**: The toolbar shows your current position (e.g., "Page 2 of 10")

### Using Bookmarks

- **Toggle Panel**: Click the **Bookmarks** button in the toolbar or press **Ctrl+B**
- **Navigate**: Click any bookmark to jump directly to that page
- **Expand/Collapse**: Click the arrow icon to expand or collapse nested bookmarks
- **Resize Panel**: Drag the panel edge to adjust width (150-600px)
- **Panel State**: Visibility and width are saved automatically and restored on next launch
- **Empty State**: If a PDF has no bookmarks, the panel displays "No bookmarks in this document"

### Text Extraction and Search

FluentPDF provides powerful text extraction and search capabilities using PDFium's text APIs:

**Opening Search**:
1. Press **Ctrl+F** to open the search panel
2. Type your search query in the search box
3. Search automatically begins after you stop typing (300ms delay)

**Navigating Matches**:
- **Next Match**: Press **F3** or click the down arrow button
- **Previous Match**: Press **Shift+F3** or click the up arrow button
- **Match Counter**: Shows "X of Y matches" in the search panel
- **Auto-Scroll**: Automatically scrolls to show the current match

**Search Options**:
- **Case Sensitive**: Check the "Aa" checkbox to match exact case
- **Close Search**: Press **Escape** or click the X button to close

**Visual Highlighting**:
- All matches are highlighted in semi-transparent blue
- Current match is highlighted in yellow
- Highlights update automatically when zooming or changing pages

**Text Selection and Copy** (coming soon):
- Click and drag to select text on PDF pages
- Press **Ctrl+C** to copy selected text to clipboard
- Right-click for context menu with Copy option

**Performance**:
- Text extraction: < 500ms per page
- Full document search: < 5 seconds for 100-page documents
- Real-time highlighting with no frame rate impact

See [TEXT-SEARCH.md](docs/TEXT-SEARCH.md) for detailed text extraction and search documentation.

### Zoom Controls

- **Zoom In**: Click the **Zoom In** button or press **Ctrl+Plus** (increases by 25%)
- **Zoom Out**: Click the **Zoom Out** button or press **Ctrl+Minus** (decreases by 25%)
- **Reset Zoom**: Click the **Reset Zoom** button or press **Ctrl+0** (returns to 100%)
- **Zoom Levels**: 50%, 75%, 100%, 125%, 150%, 175%, 200%
- **Current Zoom**: Displayed in toolbar (e.g., "150%")

### Filling PDF Forms

When you open a PDF with form fields, FluentPDF automatically detects and displays interactive overlay controls:

1. **Automatic Detection**: Form fields appear as interactive controls overlaid on the PDF
2. **Fill Fields**: Click on a field or tab to it, then type your input
3. **Tab Navigation**: Press **Tab** to move to the next field, **Shift+Tab** for previous field
4. **Validation**: Real-time validation shows errors immediately (required fields, max length, format)
5. **Fix Errors**: Red borders indicate validation errors - fix them before saving
6. **Save Form**: Click **Save** or press **Ctrl+S** to save filled form data

**Supported Field Types**:
- **Text Fields**: Single-line and multi-line text input with max length validation
- **Checkboxes**: Check/uncheck with mouse or spacebar
- **Radio Buttons**: Select one option from a group

**Validation**:
- Required fields must be filled before saving
- Max length enforced for text fields
- Format masks validate patterns (phone numbers, dates, etc.)
- Validation errors shown in InfoBar at top of page

**Tips**:
- Use Tab key for faster navigation through fields
- Validation happens as you type - fix errors immediately
- Save frequently to avoid losing your work
- Original PDF remains unchanged - saves to new file

### HiDPI Display Scaling

FluentPDF automatically detects and adapts to high-resolution displays for crisp, pixel-perfect PDF rendering:

**Automatic Adaptation**:
- Detects display scaling (100%, 125%, 150%, 200%, 300%)
- Renders PDFs at optimal DPI for your display
- Automatically re-renders when moving between monitors with different DPI
- Shows "Adjusting quality..." overlay during DPI changes

**Quality Settings**:
Navigate to Settings to manually control rendering quality:

1. Open **Settings** page
2. Find **Rendering Quality** section
3. Choose quality level:
   - **Auto** (Recommended): Matches your display automatically
   - **Low** (96 DPI): Fastest rendering, lower quality
   - **Medium** (144 DPI): Balanced quality and performance
   - **High** (192 DPI): Sharp rendering for most displays
   - **Ultra** (288 DPI): Maximum quality (may be slow on large documents)

**Multi-Monitor Support**:
- Seamlessly adapts when moving app between monitors
- Each monitor can have different DPI (e.g., laptop screen + 4K external)
- Set manual quality for consistent rendering across all displays

**Performance**:
- Standard document at 2x DPI (192 DPI): < 1 second render time
- Memory usage scales with quality (2x DPI = 4x memory)
- Automatic fallback to lower DPI on out-of-memory errors

**See [HIDPI.md](docs/HIDPI.md) for comprehensive HiDPI documentation.**

### Observability and Diagnostics

FluentPDF includes comprehensive observability features for development and production debugging:

**In-App Diagnostics Panel** (`Ctrl+Shift+D`):
- Real-time performance metrics overlay (FPS, memory usage, render times)
- Color-coded performance levels (Green: Good, Yellow: Warning, Red: Critical)
- Export metrics to JSON/CSV for analysis
- Acrylic background for minimal visual intrusion

**Structured Log Viewer** (`Ctrl+Shift+L`):
- In-app browser for Serilog JSON logs
- Advanced filtering: severity, correlation ID, component, time range, search
- Correlation ID tracing for end-to-end operation debugging
- Export filtered logs to JSON

**Development-Time Monitoring**:
- .NET Aspire Dashboard integration via OpenTelemetry (OTLP)
- Real-time metrics: FPS, memory (managed/native), render time histograms
- Distributed tracing: Complete rendering pipeline visibility
- Structured logs with full context

See [OBSERVABILITY.md](docs/OBSERVABILITY.md) for complete observability guide.

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+O** | Open PDF file in new tab |
| **Ctrl+W** | Close current tab |
| **Ctrl+S** | Save filled form data |
| **Ctrl+B** | Toggle bookmarks panel |
| **Ctrl+F** | Open search panel |
| **F3** | Next search match |
| **Shift+F3** | Previous search match |
| **Escape** | Close search panel |
| **Right Arrow** | Next page |
| **Left Arrow** | Previous page |
| **Ctrl+Plus** | Zoom in |
| **Ctrl+Minus** | Zoom out |
| **Ctrl+0** | Reset zoom to 100% |
| **Tab** | Next form field |
| **Shift+Tab** | Previous form field |
| **Ctrl+Shift+C** | Convert DOCX to PDF |
| **Ctrl+Shift+D** | Toggle diagnostics panel |
| **Ctrl+Shift+L** | Open log viewer |
| **Ctrl+,** | Open settings |

### Settings and Preferences

FluentPDF provides a comprehensive settings system to customize your PDF viewing experience. Access settings by clicking **File** → **Settings** or pressing **Ctrl+,**.

**Viewing Preferences**:
- **Default Zoom Level**: Choose the initial zoom level for newly opened documents
  - Options: 50%, 75%, 100% (default), 125%, 150%, 175%, 200%, Fit Width, Fit Page
- **Scroll Mode**: Set the default scroll behavior
  - Vertical: Traditional top-to-bottom scrolling (default)
  - Horizontal: Left-to-right scrolling for wide documents
  - Fit Page: Single page view with page-by-page navigation

**Appearance**:
- **Theme**: Choose your preferred application theme
  - Light: Bright theme with light backgrounds
  - Dark: Dark theme for reduced eye strain
  - Use System (default): Automatically matches Windows theme preference

**Privacy**:
- **Anonymous Telemetry**: Opt-in to share anonymous usage data (default: disabled)
- **Crash Reporting**: Opt-in to share crash reports for debugging (default: disabled)

**Settings Features**:
- **Automatic Persistence**: Settings are saved automatically to `ApplicationData.LocalFolder/settings.json`
- **Instant Apply**: Changes take effect immediately without restart
- **Debounced Saves**: Rapid changes are batched to reduce I/O operations
- **Corrupt File Recovery**: If settings file is corrupted, defaults are restored automatically
- **Reset to Defaults**: Click **Reset to Defaults** button to restore all settings to factory values

**Settings Behavior**:
- Theme changes apply immediately to the entire application
- Zoom and scroll mode preferences apply to newly opened documents
- Existing open documents retain their current settings
- All settings persist across application restarts

### Converting DOCX to PDF

1. Navigate to **Convert DOCX to PDF** page
2. Click **Browse** to select a .docx file
3. Choose output location for the PDF
4. (Optional) Enable **Validate Quality** for LibreOffice comparison
5. Click **Convert** or press **Ctrl+Shift+C**
6. Wait for conversion to complete (progress bar shows status)
7. Click **Open PDF** to view the converted document

**Quality Validation**: When enabled, the converter compares output against LibreOffice using SSIM (Structural Similarity Index) metrics. A score above 0.85 indicates good quality. Comparison images are saved if quality is below threshold.

**See [CONVERSION.md](docs/CONVERSION.md) for detailed conversion documentation.**

### Validating PDFs

FluentPDF integrates industry-standard validation tools to verify PDF quality, compliance, and structural integrity:

```csharp
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Models;

// Create validation service
var validationService = new PdfValidationService(
    new QpdfWrapper(),
    new JhoveWrapper(),
    new VeraPdfWrapper()
);

// Validate with Quick profile (fast, QPDF only)
var result = await validationService.ValidateAsync(
    "document.pdf",
    ValidationProfile.Quick
);

if (result.Value.OverallStatus == ValidationStatus.Pass)
{
    Console.WriteLine("PDF is structurally valid");
}
```

**Validation Profiles**:

| Profile   | Tools Used              | Use Case                        | Speed  |
|-----------|------------------------|----------------------------------|--------|
| Quick     | QPDF                   | Fast structural validation       | ~0.5s  |
| Standard  | QPDF + JHOVE           | Format + metadata extraction     | ~2s    |
| Full      | QPDF + JHOVE + VeraPDF | PDF/A compliance + comprehensive | ~4s    |

**What Gets Validated**:
- **Structural integrity** (cross-reference tables, object streams)
- **PDF format compliance** (version detection, well-formedness)
- **PDF/A archival standards** (PDF/A-1, PDF/A-2, PDF/A-3)
- **Metadata extraction** (title, author, page count, creation date)

**See [VALIDATION.md](docs/VALIDATION.md) for comprehensive validation documentation and [FluentPDF.Validation README](src/FluentPDF.Validation/README.md) for API reference.**

## Architecture

```
FluentPDF
├── FluentPDF.App          # WinUI 3 presentation layer (MVVM)
│   ├── ViewModels/        # CommunityToolkit.Mvvm ViewModels
│   ├── Views/             # XAML pages and controls
│   └── Services/          # UI-specific services (navigation)
│
├── FluentPDF.Core         # Business logic (UI-agnostic, headless testable)
│   ├── ErrorHandling/     # FluentResults error types
│   ├── Logging/           # Serilog configuration
│   └── Services/          # Domain services and interfaces
│
├── FluentPDF.Rendering    # PDF rendering infrastructure
│   └── P/Invoke/          # PDFium and QPDF native interop
│
└── FluentPDF.Validation   # PDF validation (VeraPDF, JHOVE, QPDF)
    ├── Services/          # Validation orchestration
    ├── Wrappers/          # CLI tool integration
    └── Models/            # Validation reports and results
```

### Architectural Principles

- **Core is UI-agnostic**: `FluentPDF.Core` has zero UI dependencies and can run headless
- **Dependency flow**: `App` → `Core` + `Rendering`, `Rendering` → `Core`
- **Interface-based design**: All services implement `I*Service` interfaces for testability
- **MVVM pattern**: ViewModels use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Error handling**: FluentResults `Result<T>` pattern instead of exceptions for expected failures
- **Observability**: Structured logging with Serilog (JSON format) + OpenTelemetry

## Testing

### Run All Tests

```bash
dotnet test FluentPDF.sln
```

### Run Specific Test Projects

```bash
# Architecture tests (enforces layering rules)
dotnet test tests/FluentPDF.Architecture.Tests

# Core unit tests (headless, no UI dependencies)
dotnet test tests/FluentPDF.Core.Tests

# UI tests (requires Windows runtime)
dotnet test tests/FluentPDF.App.Tests
```

### Code Quality Standards

- **ArchUnitNET** enforces architectural rules automatically:
  - Core cannot depend on App or Rendering
  - ViewModels must inherit from `ObservableObject`
  - Services must implement `I*Service` interfaces
- **File size limit**: Maximum 500 lines per file
- **Method size limit**: Maximum 50 lines per method
- **Test coverage**: Minimum 80% (90% for critical paths)

### Visual Test Explorer

Open **Test Explorer** in Visual Studio:
- **View** → **Test Explorer** (Ctrl+E, T)
- Run, debug, and view results for all tests

## Observability

FluentPDF provides three-layer observability for comprehensive monitoring and debugging:

### 1. In-App Diagnostics Panel

Press **Ctrl+Shift+D** to toggle the real-time diagnostics overlay:

- **FPS (Frames Per Second)**: Color-coded (Green ≥30, Yellow 15-30, Red <15)
- **Memory Usage**: Managed + Native memory tracking
- **Render Time**: Last page render duration
- **Current Page**: Active page number
- **Export**: Save metrics to JSON/CSV for analysis

**Performance Levels**:
- 🟢 **Good**: FPS ≥ 30 AND Memory < 500MB
- 🟡 **Warning**: FPS 15-30 OR Memory 500-1000MB
- 🔴 **Critical**: FPS < 15 OR Memory > 1000MB

### 2. Structured Log Viewer

Press **Ctrl+Shift+L** to open the in-app log browser:

**Filter Options**:
- **Severity**: Minimum log level (Trace, Debug, Info, Warning, Error, Critical)
- **Correlation ID**: Exact match for operation tracing
- **Component**: Namespace filter (e.g., "FluentPDF.Rendering")
- **Time Range**: Start and end timestamps
- **Search**: Case-insensitive message search

**Features**:
- LRU cache for fast log access (10,000 entries)
- Virtualized ListView for performance
- Export filtered logs to JSON
- Copy correlation IDs for Aspire Dashboard filtering

### 3. Development-Time Monitoring (.NET Aspire Dashboard)

During development, telemetry is sent to .NET Aspire Dashboard via OpenTelemetry:

```bash
# Start Aspire Dashboard
docker-compose -f tools/docker-compose-aspire.yml up -d

# Access dashboard
http://localhost:18888
```

**Dashboard Features**:
- **Metrics**: Real-time FPS, memory, render time histograms
- **Traces**: Distributed traces showing RenderPage → LoadPage → RenderBitmap → ConvertToImage
- **Logs**: Structured logs with correlation IDs, severity filtering

**Graceful Fallback**: If Aspire not running, app continues normally with file-based logging.

### Structured Logging

Logs are written in JSON format to enable AI-powered analysis:

- **Log location**: `%LOCALAPPDATA%\Packages\FluentPDF_*\LocalState\logs\`
- **Format**: Serilog JSON (newline-delimited)
- **Enrichers**: Machine name, environment, thread ID, correlation IDs
- **Rolling**: Daily log files, 7-day retention
- **OTLP Export**: Logs sent to Aspire Dashboard when running

**Example Log Entry**:
```json
{
  "@t": "2026-01-11T14:32:15.1234567Z",
  "@l": "Information",
  "@mt": "Rendering page {PageNumber} at zoom {ZoomLevel}",
  "PageNumber": 42,
  "ZoomLevel": 1.5,
  "CorrelationId": "3f7b8c9d-e21a-4f5d-a6c8-1b2e3d4a5f6g"
}
```

### Correlation ID Tracing

Track operations end-to-end using correlation IDs:

1. **Generation**: Unique GUID created per rendering operation
2. **Propagation**: Flows through OpenTelemetry spans, Serilog logs, metrics tags
3. **Filtering**: Use correlation ID in log viewer or Aspire Dashboard to see complete trace
4. **Exception Handling**: All unhandled exceptions include correlation ID in error dialog

**Workflow**:
- Render a page → Correlation ID generated
- View logs in app → Filter by correlation ID
- Copy correlation ID → Paste in Aspire Dashboard
- See complete distributed trace with all spans and logs

See [OBSERVABILITY.md](docs/OBSERVABILITY.md) for comprehensive guide.

## Error Handling

FluentPDF uses a multi-layered error handling approach:

1. **Result Pattern** (FluentResults): Type-safe error handling for expected failures
2. **Global UI Exception Handler**: Catches unhandled UI thread exceptions
3. **Task Exception Handler**: Catches unobserved background task exceptions
4. **AppDomain Handler**: Final safety net for non-UI thread exceptions

All exceptions are logged with structured metadata for AI analysis.

## Building Native Dependencies

The `build-libs.ps1` script automates native library compilation:

1. **Clones vcpkg** from Microsoft's official repository
2. **Bootstraps vcpkg** (compiles vcpkg itself)
3. **Installs PDFium and QPDF** using vcpkg's package manager
4. **Copies binaries** to `libs/{arch}/bin/` directory
5. **Copies headers** to `libs/{arch}/include/` for P/Invoke development

### Script Options

```powershell
# Basic usage
.\tools\build-libs.ps1

# Advanced options
.\tools\build-libs.ps1 `
    -Triplet arm64-windows `  # Target architecture
    -Clean `                  # Force rebuild
    -UseCache                 # Enable binary caching
```

### Troubleshooting vcpkg

If builds fail:

1. **Check Visual Studio installation**: Ensure C++ desktop development workload is installed
2. **Clean rebuild**: Run with `-Clean` flag
3. **Check vcpkg logs**: `tools/vcpkg/buildtrees/{package}/build-*.log`
4. **Update vcpkg**: Delete `tools/vcpkg/` and re-run script

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Ensure all tests pass: `dotnet test`
5. Ensure architecture tests pass (validates layering rules)
6. Submit a pull request

### Code Style

- Follow `.editorconfig` settings (enforced by Visual Studio)
- Use CommunityToolkit.Mvvm source generators for ViewModels
- Use `I*Service` interface pattern for all services
- Keep files under 500 lines, methods under 50 lines
- Write XML documentation for public APIs

## Project Structure

```
FluentPDF/
├── .spec-workflow/          # Specification workflow documents
│   └── specs/
│       └── project-foundation/
│           ├── requirements.md
│           ├── design.md
│           └── tasks.md
├── docs/                    # Additional documentation
│   ├── ARCHITECTURE.md      # Architectural decision records
│   └── TESTING.md           # Testing strategy
├── src/                     # Source code
│   ├── FluentPDF.App/       # WinUI 3 application
│   ├── FluentPDF.Core/      # Business logic
│   └── FluentPDF.Rendering/ # PDF rendering
├── tests/                   # Test projects
│   ├── FluentPDF.Architecture.Tests/
│   ├── FluentPDF.Core.Tests/
│   └── FluentPDF.App.Tests/
├── tools/                   # Build scripts
│   └── build-libs.ps1       # vcpkg build automation
├── libs/                    # Native libraries (generated by build-libs.ps1)
│   ├── x64/
│   │   ├── bin/             # DLL files
│   │   └── include/         # Header files
│   └── arm64/
├── Directory.Build.props    # Shared MSBuild properties
└── FluentPDF.sln            # Visual Studio solution
```

## License

[MIT License](LICENSE)

## Acknowledgments

- **PDFium**: Google's PDF rendering engine
- **QPDF**: PDF transformation library by Jay Berkenbilt
- **VeraPDF**: PDF/A validation reference implementation
- **JHOVE**: PDF format characterization and validation
- **Mammoth.NET**: Semantic DOCX to HTML converter
- **WebView2**: Microsoft's Chromium-based rendering engine
- **vcpkg**: Microsoft's C/C++ package manager
- **WinUI 3**: Microsoft's native Windows UI framework
- **CommunityToolkit.Mvvm**: .NET Community MVVM toolkit
- **FluentResults**: Functional error handling library
- **Serilog**: Structured logging framework
- **ArchUnitNET**: Architecture testing framework
- **OpenCvSharp**: Computer vision library for SSIM calculations
