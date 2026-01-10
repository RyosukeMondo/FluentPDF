# Tasks Document

## Implementation Tasks

- [x] 1. Update vcpkg build script to include PDFium
  - Files:
    - `tools/build-libs.ps1` (modify)
    - `.gitignore` (update)
  - Modify build-libs.ps1 to install pdfium package via vcpkg
  - Update PDFium DLL copy to libs/x64/bin/
  - Add PDFium headers copy to libs/x64/include/
  - Test script completes successfully and DLLs are copied
  - Purpose: Automate PDFium native library build and deployment
  - _Leverage: Existing build-libs.ps1 script from foundation_
  - _Requirements: 1.1, 1.2_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** DevOps Engineer specializing in native library build automation and vcpkg

    **Task:**
    Update the existing `tools/build-libs.ps1` script to include PDFium in addition to QPDF:

    1. Modify the vcpkg install command to include pdfium:
       - Change: `vcpkg install qpdf:$Triplet`
       - To: `vcpkg install pdfium:$Triplet qpdf:$Triplet`

    2. Ensure DLL copy logic handles PDFium DLLs (pdfium.dll)

    3. Copy PDFium headers from vcpkg:
       - Source: `tools/vcpkg/installed/$Triplet/include/fpdfview.h` and related headers
       - Destination: `libs/$($Triplet -replace '-windows', '')/include/pdfium/`

    4. Update .gitignore to ensure native DLLs and vcpkg are not committed:
       - Add: `tools/vcpkg/`
       - Add: `libs/**/bin/*.dll`
       - Keep: `libs/**/include/` (commit headers for reference)

    5. Test the script:
       - Run: `.\tools\build-libs.ps1 -Triplet x64-windows`
       - Verify pdfium.dll exists in libs/x64/bin/
       - Verify headers exist in libs/x64/include/pdfium/

    **Restrictions:**
    - Do NOT commit vcpkg installation or built DLLs to repo
    - Do NOT modify existing QPDF build logic
    - Keep script idempotent (safe to run multiple times)
    - Maintain support for -Clean and -UseCache parameters
    - Display clear progress messages for PDFium build

    **Success Criteria:**
    - Script successfully installs PDFium via vcpkg
    - pdfium.dll is copied to libs/x64/bin/
    - PDFium headers are copied to libs/x64/include/pdfium/
    - Script completes in < 5 minutes with cache
    - .gitignore prevents committing large binaries
    - `dotnet build` succeeds after running script

    **Instructions:**
    1. Before implementing, read `.spec-workflow/specs/pdf-viewer-core/requirements.md` and design.md
    2. Edit `.spec-workflow/specs/pdf-viewer-core/tasks.md` and change this task's status from `[ ]` to `[-]` (in-progress)
    3. Modify build-libs.ps1 to add PDFium
    4. Update .gitignore
    5. Test by running the script and verifying output
    6. Use the log-implementation tool to record implementation details with artifacts:
       - Document script modifications in artifacts.functions
       - Include files modified (build-libs.ps1, .gitignore)
       - Note PDFium version installed
    7. After successful logging, edit tasks.md and change this task's status from `[-]` to `[x]` (completed)

- [x] 2. Implement PDFium P/Invoke declarations with SafeHandle
  - Files:
    - `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs`
    - `src/FluentPDF.Rendering/Interop/SafePdfDocumentHandle.cs`
    - `src/FluentPDF.Rendering/Interop/SafePdfPageHandle.cs`
    - `tests/FluentPDF.Rendering.Tests/Interop/PdfiumInteropTests.cs`
  - Create P/Invoke declarations for PDFium functions (FPDF_InitLibrary, FPDF_LoadDocument, etc.)
  - Implement SafeHandle types for automatic resource cleanup
  - Add error code checking for all PDFium calls
  - Write unit tests for P/Invoke layer
  - Purpose: Provide safe managed wrapper for PDFium native library
  - _Leverage: Existing SafeHandle patterns from .NET, PDFium documentation_
  - _Requirements: 1.3, 1.4, 1.5, 1.6, 1.7_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Native Interop Developer specializing in P/Invoke and unmanaged memory safety

    **Task:**
    Create PDFium P/Invoke declarations with SafeHandle for memory safety:

    1. **PdfiumInterop.cs** (in Rendering/Interop/):
       - Add [DllImport("pdfium.dll")] declarations for:
         - FPDF_InitLibrary()
         - FPDF_DestroyLibrary()
         - FPDF_LoadDocument(string file_path, string password)
         - FPDF_CloseDocument(IntPtr document)
         - FPDF_GetPageCount(SafePdfDocumentHandle document)
         - FPDF_LoadPage(SafePdfDocumentHandle document, int page_index)
         - FPDF_ClosePage(IntPtr page)
         - FPDF_GetPageWidth(SafePdfPageHandle page)
         - FPDF_GetPageHeight(SafePdfPageHandle page)
         - FPDFBitmap_Create(int width, int height, int alpha)
         - FPDFBitmap_Destroy(IntPtr bitmap)
         - FPDF_RenderPageBitmap(IntPtr bitmap, SafePdfPageHandle page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags)
       - Add static methods wrapping DllImport with error checking
       - Add public Initialize() and Shutdown() methods

    2. **SafePdfDocumentHandle.cs**:
       - Inherit from SafeHandleZeroOrMinusOneIsInvalid
       - Override ReleaseHandle() to call FPDF_CloseDocument()
       - Implement IsInvalid check

    3. **SafePdfPageHandle.cs**:
       - Inherit from SafeHandleZeroOrMinusOneIsInvalid
       - Override ReleaseHandle() to call FPDF_ClosePage()
       - Implement IsInvalid check

    4. **PdfiumInteropTests.cs** (in Rendering.Tests/Interop/):
       - Test Initialize() succeeds
       - Test LoadDocument with sample PDF returns valid handle
       - Test GetPageCount returns correct value
       - Test SafeHandles dispose correctly (no leaks)
       - Use sample PDF from test fixtures

    **Restrictions:**
    - Do NOT use raw IntPtr for document/page handles (use SafeHandle only)
    - Do NOT skip error checking on PDFium calls
    - Follow structure.md P/Invoke patterns (CallingConvention, CharSet, etc.)
    - Keep PdfiumInterop class under 500 lines
    - Add XML documentation for all public methods

    **Success Criteria:**
    - All PDFium functions have P/Invoke declarations
    - SafeHandles automatically dispose resources
    - Initialize() and Shutdown() work correctly
    - Tests verify basic PDFium operations work
    - No memory leaks detected in tests
    - ArchUnitNET tests pass (P/Invoke in Rendering namespace only)

    **Instructions:**
    1. Read design.md "Component 1: PdfiumInterop" section
    2. Edit tasks.md: change to `[-]`
    3. Create P/Invoke declarations
    4. Implement SafeHandle types
    5. Write comprehensive tests
    6. Run tests with sample PDF: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfiumInteropTests`
    7. Log implementation with artifacts:
       - Include PdfiumInterop class in artifacts.classes
       - Document all P/Invoke functions
       - Note SafeHandle types for automatic cleanup
    8. Edit tasks.md: change to `[x]`

- [x] 3. Create PDF domain models (PdfDocument, PdfPage)
  - Files:
    - `src/FluentPDF.Core/Models/PdfDocument.cs`
    - `src/FluentPDF.Core/Models/PdfPage.cs`
    - `tests/FluentPDF.Core.Tests/Models/PdfDocumentTests.cs`
  - Implement PdfDocument model with IDisposable pattern
  - Implement PdfPage model with page metadata
  - Add validation and error handling
  - Write unit tests for models
  - Purpose: Provide domain models for PDF entities
  - _Leverage: Existing model patterns from Core project, IDisposable pattern_
  - _Requirements: Design Component 2 and 3_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** C# Developer specializing in domain modeling and resource management

    **Task:**
    Create PDF domain models in FluentPDF.Core with proper resource management:

    1. **PdfDocument.cs** (in Core/Models/):
       - Properties:
         - `string FilePath { get; init; }` (required)
         - `int PageCount { get; init; }` (required)
         - `SafePdfDocumentHandle Handle { get; init; }` (required)
         - `DateTime LoadedAt { get; init; }` (required)
         - `long FileSizeBytes { get; init; }` (required)
       - Implement IDisposable:
         - Dispose() calls Handle?.Dispose()
         - Add GC.SuppressFinalize(this)
       - Add XML doc comments
       - Use required properties (C# 11 feature)

    2. **PdfPage.cs** (in Core/Models/):
       - Properties:
         - `int PageNumber { get; init; }` (1-based)
         - `double Width { get; init; }` (in points)
         - `double Height { get; init; }` (in points)
         - `double AspectRatio => Width / Height;` (calculated property)
       - Add XML doc comments
       - Validate PageNumber > 0 in constructor

    3. **PdfDocumentTests.cs** (in Core.Tests/Models/):
       - Test PdfDocument can be created with required properties
       - Test Dispose() cleans up handle
       - Test PdfPage calculates AspectRatio correctly
       - Test PdfPage validates PageNumber
       - Use FluentAssertions for readable assertions

    **Restrictions:**
    - Do NOT add business logic to models (keep them simple data containers)
    - Do NOT add references to PDFium in Core project (use SafeHandle abstraction)
    - Keep models immutable (init-only properties)
    - Follow structure.md code organization (using, namespace, XML docs, class)
    - Keep files under 500 lines

    **Success Criteria:**
    - Models compile without errors
    - PdfDocument properly disposes SafeHandle
    - PdfPage validates inputs
    - Tests verify model behavior
    - No UI dependencies in Core models
    - XML documentation on all public members

    **Instructions:**
    1. Read design.md "Component 2 and 3" sections
    2. Edit tasks.md: change to `[-]`
    3. Create PdfDocument and PdfPage classes
    4. Implement IDisposable pattern correctly
    5. Write unit tests
    6. Run tests: `dotnet test tests/FluentPDF.Core.Tests --filter PdfDocumentTests`
    7. Log implementation with artifacts:
       - Include PdfDocument and PdfPage in artifacts.classes
       - Note IDisposable pattern for resource cleanup
    8. Edit tasks.md: change to `[x]`

- [x] 4. Implement IPdfDocumentService and PdfDocumentService
  - Files:
    - `src/FluentPDF.Core/Services/IPdfDocumentService.cs`
    - `src/FluentPDF.Rendering/Services/PdfDocumentService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/PdfDocumentServiceTests.cs`
  - Create service interface with LoadDocumentAsync, GetPageInfoAsync, CloseDocument methods
  - Implement service using PdfiumInterop
  - Add comprehensive error handling with PdfError codes
  - Write unit tests with mocked PdfiumInterop
  - Purpose: Provide business logic for PDF document operations
  - _Leverage: PdfiumInterop, PdfError, Result<T>, Serilog logging_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Backend Service Developer specializing in error handling and async operations

    **Task:**
    Implement PDF document loading service with comprehensive error handling:

    1. **IPdfDocumentService.cs** (in Core/Services/):
       - Define interface:
         ```csharp
         public interface IPdfDocumentService
         {
             Task<Result<PdfDocument>> LoadDocumentAsync(string filePath, string? password = null);
             Task<Result<PdfPage>> GetPageInfoAsync(PdfDocument document, int pageNumber);
             Result CloseDocument(PdfDocument document);
         }
         ```
       - Add XML doc comments explaining each method

    2. **PdfDocumentService.cs** (in Rendering/Services/):
       - Implement IPdfDocumentService
       - Constructor: `public PdfDocumentService(PdfiumInterop interop, ILogger<PdfDocumentService> logger)`
       - **LoadDocumentAsync:**
         - Check if file exists: if not, return `Result.Fail(new PdfError("PDF_FILE_NOT_FOUND", ErrorCategory.IO, ErrorSeverity.Error).WithContext("FilePath", filePath))`
         - Call `PdfiumInterop.LoadDocument(filePath, password)` on background thread
         - If null handle returned, determine error:
           - Try loading with empty password: if succeeds, error is "PDF_REQUIRES_PASSWORD"
           - Otherwise, error is "PDF_CORRUPTED" or "PDF_INVALID_FORMAT"
         - If successful, get page count: `PdfiumInterop.GetPageCount(handle)`
         - Get file size: `new FileInfo(filePath).Length`
         - Return `Result.Ok(new PdfDocument { FilePath, PageCount, Handle, LoadedAt = DateTime.UtcNow, FileSizeBytes })`
         - Log all operations with correlation ID
       - **GetPageInfoAsync:**
         - Validate pageNumber >= 1 and <= document.PageCount
         - Load page: `PdfiumInterop.LoadPage(document.Handle, pageNumber - 1)` (0-based)
         - Get dimensions: `PdfiumInterop.GetPageWidth/Height(pageHandle)`
         - Close page immediately (we only need metadata)
         - Return `Result.Ok(new PdfPage { PageNumber, Width, Height })`
       - **CloseDocument:**
         - Call `document.Dispose()`
         - Return `Result.Ok()`
       - Add error codes: PDF_FILE_NOT_FOUND, PDF_INVALID_FORMAT, PDF_CORRUPTED, PDF_REQUIRES_PASSWORD, PDF_LOAD_FAILED

    3. **PdfDocumentServiceTests.cs** (in Rendering.Tests/Services/):
       - Mock PdfiumInterop
       - Test LoadDocumentAsync with valid file returns Result.Ok
       - Test LoadDocumentAsync with non-existent file returns Result.Fail with PDF_FILE_NOT_FOUND
       - Test LoadDocumentAsync with corrupted file returns Result.Fail with appropriate error
       - Test GetPageInfoAsync with valid page returns Result.Ok
       - Test GetPageInfoAsync with invalid page number returns Result.Fail
       - Test Context dictionary includes expected metadata
       - Use FluentAssertions and Moq

    **Restrictions:**
    - Do NOT skip error checking on any operation
    - Do NOT block UI thread (use Task.Run for PDFium calls)
    - Log all operations with structured data
    - Follow Result<T> pattern consistently
    - Keep service methods under 50 lines each

    **Success Criteria:**
    - Service implements interface contract exactly
    - All error scenarios return appropriate PdfError codes
    - Logging includes correlation IDs and context
    - Tests verify error handling and success cases
    - No resource leaks (handles properly disposed)
    - Service is fully async (no blocking calls)

    **Instructions:**
    1. Read design.md "Component 4 and 5" sections
    2. Edit tasks.md: change to `[-]`
    3. Create interface and implementation
    4. Add comprehensive error handling
    5. Write thorough unit tests
    6. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfDocumentServiceTests`
    7. Log implementation with artifacts:
       - Include IPdfDocumentService in artifacts.classes
       - Document all error codes
       - Note async pattern and error handling
    8. Edit tasks.md: change to `[x]`

- [x] 5. Implement IPdfRenderingService and PdfRenderingService
  - Files:
    - `src/FluentPDF.Core/Services/IPdfRenderingService.cs`
    - `src/FluentPDF.Rendering/Services/PdfRenderingService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/PdfRenderingServiceTests.cs`
  - Create service interface with RenderPageAsync method
  - Implement page rendering to BitmapImage
  - Add performance logging for slow renders
  - Write unit tests with mocked PdfiumInterop
  - Purpose: Provide PDF page rendering to WinUI-compatible images
  - _Leverage: PdfiumInterop, PdfError, Result<T>, Serilog logging, Windows.Graphics.Imaging_
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Graphics Developer specializing in image rendering and bitmap manipulation

    **Task:**
    Implement PDF page rendering service with performance monitoring:

    1. **IPdfRenderingService.cs** (in Core/Services/):
       - Define interface:
         ```csharp
         public interface IPdfRenderingService
         {
             Task<Result<BitmapImage>> RenderPageAsync(PdfDocument document, int pageNumber, double zoomLevel, double dpi = 96);
         }
         ```
       - Add XML doc comments

    2. **PdfRenderingService.cs** (in Rendering/Services/):
       - Implement IPdfRenderingService
       - Constructor: `public PdfRenderingService(PdfiumInterop interop, ILogger<PdfRenderingService> logger)`
       - **RenderPageAsync:**
         - Start stopwatch for performance tracking
         - Validate pageNumber
         - Load page: `PdfiumInterop.LoadPage(document.Handle, pageNumber - 1)`
         - Get page dimensions: `PdfiumInterop.GetPageWidth/Height(pageHandle)`
         - Calculate output size:
           ```csharp
           var scaleFactor = (dpi / 72.0) * zoomLevel;
           var outputWidth = (int)(pageWidth * scaleFactor);
           var outputHeight = (int)(pageHeight * scaleFactor);
           ```
         - Create bitmap: `FPDFBitmap_Create(outputWidth, outputHeight, 1)`
         - Render page: `FPDF_RenderPageBitmap(bitmap, pageHandle, 0, 0, outputWidth, outputHeight, 0, 0)`
         - Convert bitmap to BitmapImage:
           - Get bitmap buffer: `FPDFBitmap_GetBuffer(bitmap)`
           - Copy to byte array
           - Create BitmapImage from bytes using Windows.Graphics.Imaging
         - Clean up: Dispose bitmap, close page
         - Stop stopwatch: if > 2 seconds, log performance warning
         - Return `Result.Ok(bitmapImage)`
       - Add error codes: PDF_PAGE_INVALID, PDF_RENDERING_FAILED, PDF_OUT_OF_MEMORY
       - Log performance metrics: page number, zoom, render time

    3. **PdfRenderingServiceTests.cs** (in Rendering.Tests/Services/):
       - Mock PdfiumInterop
       - Test RenderPageAsync with valid inputs returns BitmapImage
       - Test RenderPageAsync with invalid page returns Result.Fail
       - Test zoom levels calculate correct dimensions
       - Test performance logging triggers for slow renders (mock long delay)
       - Verify bitmap disposal occurs

    **Restrictions:**
    - Do NOT leak bitmap handles (always dispose)
    - Do NOT block UI thread (render on background thread)
    - Log performance metrics for all renders
    - Follow Result<T> pattern
    - Keep rendering pipeline under 50 lines

    **Success Criteria:**
    - Renders pages at correct zoom levels
    - Performance warnings logged for slow renders
    - No memory leaks (bitmaps disposed)
    - Tests verify rendering logic
    - BitmapImage compatible with WinUI Image control
    - Error handling covers all failure scenarios

    **Instructions:**
    1. Read design.md "Component 6 and 7" sections
    2. Edit tasks.md: change to `[-]`
    3. Create interface and implementation
    4. Implement bitmap rendering and conversion
    5. Add performance logging
    6. Write unit tests
    7. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfRenderingServiceTests`
    8. Log implementation with artifacts:
       - Include IPdfRenderingService in artifacts.classes
       - Document rendering pipeline
       - Note performance monitoring
    9. Edit tasks.md: change to `[x]`

- [x] 6. Create PdfViewerViewModel with navigation and zoom commands
  - Files:
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs`
    - `tests/FluentPDF.App.Tests/ViewModels/PdfViewerViewModelTests.cs`
  - Implement ViewModel with CommunityToolkit.Mvvm source generators
  - Add observable properties for state (CurrentPageImage, CurrentPageNumber, etc.)
  - Add relay commands for operations (OpenDocument, NextPage, ZoomIn, etc.)
  - Add command CanExecute logic for button states
  - Write comprehensive headless unit tests
  - Purpose: Provide presentation logic for PDF viewer UI
  - _Leverage: ObservableObject, RelayCommand, IPdfDocumentService, IPdfRenderingService_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.12, 5.1-5.10, 6.1-6.7_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI MVVM Developer specializing in data binding and command patterns

    **Task:**
    Create PdfViewerViewModel with full navigation and zoom functionality:

    1. **PdfViewerViewModel.cs** (in App/ViewModels/):
       - Mark class as `partial` (for source generators)
       - Inherit from `ObservableObject`
       - **Observable Properties:**
         ```csharp
         [ObservableProperty] private BitmapImage? _currentPageImage;
         [ObservableProperty] private int _currentPageNumber = 1;
         [ObservableProperty] private int _totalPages;
         [ObservableProperty] private double _zoomLevel = 1.0;
         [ObservableProperty] private bool _isLoading;
         [ObservableProperty] private string _statusMessage = "Open a PDF file to get started";
         private PdfDocument? _currentDocument;
         ```
       - **Commands:**
         ```csharp
         [RelayCommand]
         private async Task OpenDocumentAsync()
         {
             // Use FileOpenPicker with .pdf filter
             // Call _documentService.LoadDocumentAsync()
             // If success, set _currentDocument, TotalPages, render page 1
             // If fail, show error dialog with PdfError details
         }

         [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
         private async Task GoToPreviousPageAsync()
         {
             CurrentPageNumber--;
             await RenderCurrentPageAsync();
         }
         private bool CanGoToPreviousPage() => CurrentPageNumber > 1 && !IsLoading;

         [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
         private async Task GoToNextPageAsync()
         {
             CurrentPageNumber++;
             await RenderCurrentPageAsync();
         }
         private bool CanGoToNextPage() => CurrentPageNumber < TotalPages && !IsLoading;

         [RelayCommand(CanExecute = nameof(CanZoomIn))]
         private async Task ZoomInAsync()
         {
             // Increase zoom: 1.0 -> 1.25 -> 1.5 -> 1.75 -> 2.0
             await RenderCurrentPageAsync();
         }
         private bool CanZoomIn() => ZoomLevel < 2.0 && !IsLoading;

         [RelayCommand(CanExecute = nameof(CanZoomOut))]
         private async Task ZoomOutAsync()
         {
             // Decrease zoom: 2.0 -> 1.75 -> 1.5 -> 1.25 -> 1.0 -> 0.75 -> 0.5
             await RenderCurrentPageAsync();
         }
         private bool CanZoomOut() => ZoomLevel > 0.5 && !IsLoading;

         [RelayCommand]
         private async Task ResetZoomAsync()
         {
             ZoomLevel = 1.0;
             await RenderCurrentPageAsync();
         }

         [RelayCommand]
         private async Task GoToPageAsync(int pageNumber)
         {
             if (pageNumber >= 1 && pageNumber <= TotalPages)
             {
                 CurrentPageNumber = pageNumber;
                 await RenderCurrentPageAsync();
             }
         }
         ```
       - **Private Methods:**
         ```csharp
         private async Task RenderCurrentPageAsync()
         {
             IsLoading = true;
             StatusMessage = $"Rendering page {CurrentPageNumber}...";
             var result = await _renderingService.RenderPageAsync(_currentDocument!, CurrentPageNumber, ZoomLevel);
             if (result.IsSuccess)
             {
                 CurrentPageImage = result.Value;
                 StatusMessage = $"Page {CurrentPageNumber} of {TotalPages} - {ZoomLevel:P0}";
             }
             else
             {
                 _logger.LogError("Failed to render page: {Error}", result.Errors);
                 StatusMessage = "Failed to render page";
             }
             IsLoading = false;
         }
         ```
       - Constructor: `public PdfViewerViewModel(IPdfDocumentService documentService, IPdfRenderingService renderingService, ILogger<PdfViewerViewModel> logger)`
       - Implement IDisposable to close document on cleanup

    2. **PdfViewerViewModelTests.cs** (in App.Tests/ViewModels/):
       - Mock IPdfDocumentService and IPdfRenderingService
       - Test OpenDocumentCommand loads document and renders page 1
       - Test GoToNextPageCommand advances page
       - Test GoToPreviousPageCommand goes back
       - Test ZoomInCommand increases zoom
       - Test ZoomOutCommand decreases zoom
       - Test CanExecute logic (disabled states)
       - Test property change notifications
       - Use FluentAssertions and Moq

    **Restrictions:**
    - Do NOT add `using Microsoft.UI.Xaml` (ViewModel should be UI-agnostic except BitmapImage)
    - Do NOT directly call PDFium (use services)
    - Keep ViewModel under 500 lines
    - Follow MVVM pattern strictly
    - All commands must have CanExecute logic

    **Success Criteria:**
    - All observable properties fire PropertyChanged events
    - Commands work correctly with CanExecute logic
    - Opening document loads and renders first page
    - Navigation commands advance/retreat pages
    - Zoom commands work correctly
    - Tests run headless without WinUI runtime
    - ArchUnitNET tests pass

    **Instructions:**
    1. Read design.md "Component 8" section
    2. Edit tasks.md: change to `[-]`
    3. Create ViewModel with all properties and commands
    4. Implement async operations correctly
    5. Write comprehensive tests
    6. Run tests: `dotnet test tests/FluentPDF.App.Tests --filter PdfViewerViewModelTests`
    7. Log implementation with artifacts:
       - Include PdfViewerViewModel in artifacts.components
       - Document all commands and properties
       - Note MVVM pattern with source generators
    8. Edit tasks.md: change to `[x]`

- [x] 7. Create PdfViewerPage UI with toolbar and content area
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml`
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml.cs`
  - Design XAML layout with CommandBar toolbar
  - Add navigation buttons (Previous, Next, Page indicator)
  - Add zoom controls (Zoom In, Zoom Out, Reset)
  - Add keyboard accelerators (Ctrl+O, Arrow keys, Ctrl+Plus/Minus/0)
  - Bind all controls to ViewModel commands and properties
  - Purpose: Provide PDF viewer UI following Fluent Design
  - _Leverage: WinUI 3 controls, PdfViewerViewModel, existing App styling_
  - _Requirements: 4.1-4.12, 5.1-5.10_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI Frontend Developer specializing in XAML and Fluent Design

    **Task:**
    Create the PDF viewer page UI with toolbar and content area:

    1. **PdfViewerPage.xaml**:
       - Root: `<Page x:Name="RootPage">`
       - Layout: Grid with two rows (toolbar + content)
       - **Toolbar (Row 0):**
         ```xml
         <CommandBar>
             <AppBarButton Icon="OpenFile" Label="Open" Command="{Binding OpenDocumentCommand}">
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="O" Modifiers="Control"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
             <AppBarSeparator/>
             <AppBarButton Icon="Previous" Label="Previous" Command="{Binding GoToPreviousPageCommand}">
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="Left"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
             <TextBlock VerticalAlignment="Center" Margin="12,0">
                 <Run Text="Page "/>
                 <Run Text="{Binding CurrentPageNumber}"/>
                 <Run Text=" of "/>
                 <Run Text="{Binding TotalPages}"/>
             </TextBlock>
             <AppBarButton Icon="Next" Label="Next" Command="{Binding GoToNextPageCommand}">
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="Right"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
             <AppBarSeparator/>
             <AppBarButton Label="Zoom Out" Command="{Binding ZoomOutCommand}">
                 <AppBarButton.Icon>
                     <FontIcon Glyph="&#xE71F;"/>
                 </AppBarButton.Icon>
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="Subtract" Modifiers="Control"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
             <TextBlock Text="{Binding ZoomLevel, Converter={StaticResource PercentageConverter}}" VerticalAlignment="Center" Margin="12,0"/>
             <AppBarButton Label="Zoom In" Command="{Binding ZoomInCommand}">
                 <AppBarButton.Icon>
                     <FontIcon Glyph="&#xE71E;"/>
                 </AppBarButton.Icon>
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="Add" Modifiers="Control"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
             <AppBarButton Label="Reset Zoom" Command="{Binding ResetZoomCommand}">
                 <AppBarButton.Icon>
                     <FontIcon Glyph="&#xE8A0;"/>
                 </AppBarButton.Icon>
                 <AppBarButton.KeyboardAccelerators>
                     <KeyboardAccelerator Key="Number0" Modifiers="Control"/>
                 </AppBarButton.KeyboardAccelerators>
             </AppBarButton>
         </CommandBar>
         ```
       - **Content Area (Row 1):**
         ```xml
         <Grid>
             <!-- Page viewer with scrolling -->
             <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Auto">
                 <Image Source="{Binding CurrentPageImage}"
                        Visibility="{Binding CurrentPageImage, Converter={StaticResource NullToVisibilityConverter}}"
                        Stretch="None"/>
             </ScrollViewer>

             <!-- Loading indicator -->
             <ProgressRing IsActive="{Binding IsLoading}" Width="60" Height="60"/>

             <!-- Empty state message -->
             <TextBlock Text="{Binding StatusMessage}"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center"
                        Visibility="{Binding CurrentPageImage, Converter={StaticResource InverseNullToVisibilityConverter}}"
                        Style="{StaticResource SubtitleTextBlockStyle}"/>
         </Grid>
         ```
       - Add value converters in page resources:
         - PercentageConverter (double to "150%" string)
         - NullToVisibilityConverter
         - InverseNullToVisibilityConverter

    2. **PdfViewerPage.xaml.cs**:
       - Constructor: Get PdfViewerViewModel from DI, set as DataContext
         ```csharp
         public PdfViewerPage()
         {
             InitializeComponent();
             var vm = ((App)Application.Current).GetService<PdfViewerViewModel>();
             RootPage.DataContext = vm;
         }
         ```
       - Implement IDisposable to dispose ViewModel

    **Restrictions:**
    - Do NOT add business logic in code-behind (only view logic)
    - Use data binding for all dynamic content
    - Follow Fluent Design guidelines
    - Ensure keyboard accessibility
    - Keep XAML well-formatted and readable

    **Success Criteria:**
    - UI renders correctly with all toolbar buttons
    - All commands bound to ViewModel
    - Keyboard shortcuts work
    - Page navigation works smoothly
    - Zoom controls update display
    - Loading indicator shows during operations
    - Empty state message displays when no document loaded
    - UI is responsive and accessible

    **Instructions:**
    1. Read design.md "Component 9" section
    2. Edit tasks.md: change to `[-]`
    3. Create XAML layout with CommandBar
    4. Add all buttons and bindings
    5. Implement value converters
    6. Create code-behind with ViewModel initialization
    7. Test UI by running app: `dotnet run --project src/FluentPDF.App`
    8. Log implementation with artifacts:
       - Include PdfViewerPage in artifacts.components
       - Document UI controls and bindings
       - Note keyboard shortcuts
    9. Edit tasks.md: change to `[x]`

- [ ] 8. Register services in DI container and update navigation
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify)
    - `src/FluentPDF.App/MainWindow.xaml` (modify - add navigation to PdfViewerPage)
  - Register IPdfDocumentService and IPdfRenderingService in DI container
  - Initialize PDFium library on app startup
  - Add navigation from MainWindow to PdfViewerPage
  - Handle PDFium initialization failures gracefully
  - Purpose: Wire up all components in the application
  - _Leverage: Existing IHost DI container, INavigationService_
  - _Requirements: 1.3, 1.6, 1.7_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Application Integration Engineer specializing in dependency injection and app lifecycle

    **Task:**
    Register all PDF services in DI container and set up navigation:

    1. **App.xaml.cs modifications (ConfigureServices)**:
       - Add singleton registrations:
         ```csharp
         // Initialize PDFium (singleton ensures single initialization)
         services.AddSingleton<PdfiumInterop>(sp =>
         {
             var logger = sp.GetRequiredService<ILogger<App>>();
             var interop = new PdfiumInterop();
             if (!interop.Initialize())
             {
                 logger.LogCritical("Failed to initialize PDFium library");
                 throw new InvalidOperationException("Failed to initialize PDFium. Please reinstall the application.");
             }
             logger.LogInformation("PDFium initialized successfully");
             return interop;
         });

         // Register services
         services.AddSingleton<IPdfDocumentService, PdfDocumentService>();
         services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

         // Register ViewModels
         services.AddTransient<PdfViewerViewModel>();
         ```
       - In OnLaunched, navigate to PdfViewerPage instead of MainWindow:
         ```csharp
         var window = new Window();
         var frame = new Frame();
         frame.Navigate(typeof(PdfViewerPage));
         window.Content = frame;
         window.Activate();
         ```
       - Add cleanup in Application.OnExit:
         ```csharp
         protected override void OnExit(ExitEventArgs e)
         {
             var interop = _host.Services.GetService<PdfiumInterop>();
             interop?.Shutdown();
             Log.Information("PDFium shutdown");
             Log.CloseAndFlush();
             base.OnExit(e);
         }
         ```

    2. **Alternative: Update MainWindow.xaml** (if keeping MainWindow):
       - Add button: "Open PDF Viewer"
       - In click handler, navigate to PdfViewerPage:
         ```csharp
         var navService = ((App)Application.Current).GetService<INavigationService>();
         navService.NavigateTo(typeof(PdfViewerPage));
         ```

    **Restrictions:**
    - Do NOT skip PDFium initialization check
    - Do NOT allow app to start if PDFium fails to initialize
    - Log all initialization steps
    - Ensure proper shutdown and cleanup
    - Follow existing DI registration patterns

    **Success Criteria:**
    - All services registered and resolvable
    - PDFium initializes successfully on app startup
    - Navigation to PdfViewerPage works
    - PDFium shuts down cleanly on app exit
    - Initialization failure shows clear error message
    - Logs show initialization and shutdown events

    **Instructions:**
    1. Read design.md "Dependency Injection Registration" section
    2. Edit tasks.md: change to `[-]`
    3. Modify App.xaml.cs to register services
    4. Add PDFium initialization with error handling
    5. Update navigation to PdfViewerPage
    6. Add cleanup in OnExit
    7. Test app startup: `dotnet run --project src/FluentPDF.App`
    8. Log implementation with artifacts:
       - Document DI registrations
       - Note PDFium initialization and shutdown
       - Include navigation setup
    9. Edit tasks.md: change to `[x]`

- [ ] 9. Add ArchUnitNET rules for P/Invoke and rendering layer
  - Files:
    - `tests/FluentPDF.Architecture.Tests/PdfRenderingArchitectureTests.cs`
  - Create new architecture test file for PDF rendering rules
  - Add rule: P/Invoke methods must be in Rendering namespace
  - Add rule: P/Invoke must use SafeHandle for pointer parameters
  - Add rule: ViewModels must not reference PDFium directly
  - Add rule: Core services must use interfaces
  - Purpose: Enforce architectural rules for PDF rendering components
  - _Leverage: Existing ArchUnitNET tests from foundation_
  - _Requirements: Architecture integrity_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Software Architect specializing in architecture testing and governance

    **Task:**
    Add ArchUnitNET tests to enforce PDF rendering architecture rules:

    1. **PdfRenderingArchitectureTests.cs** (in Architecture.Tests/):
       - Test: PInvoke_ShouldOnly_ExistIn_RenderingNamespace
         ```csharp
         var rule = Methods()
             .That().HaveAttribute<DllImportAttribute>()
             .Should().ResideInNamespace("FluentPDF.Rendering.Interop")
             .Because("P/Invoke declarations must be isolated in Rendering.Interop namespace");
         ```
       - Test: PInvoke_Should_UseSafeHandle_ForPointers
         ```csharp
         // Check that P/Invoke methods don't use raw IntPtr for document/page handles
         // Should use SafePdfDocumentHandle and SafePdfPageHandle
         ```
       - Test: ViewModels_ShouldNot_Reference_Pdfium
         ```csharp
         var rule = Classes()
             .That().HaveNameEndingWith("ViewModel")
             .Should().NotDependOnAny(Classes().That().ResideInNamespace("FluentPDF.Rendering.Interop"))
             .Because("ViewModels should use service interfaces, not direct PDFium access");
         ```
       - Test: RenderingServices_Should_ImplementInterfaces
         ```csharp
         var rule = Classes()
             .That().HaveNameEndingWith("Service")
             .And().ResideInNamespace("FluentPDF.Rendering.Services")
             .Should().ImplementInterface("FluentPDF.Core.Services.I.*")
             .Because("All services must implement interface contracts");
         ```
       - Test: CoreLayer_ShouldNot_Reference_PdfiumInterop
         ```csharp
         var rule = Classes()
             .That().ResideInNamespace("FluentPDF.Core")
             .Should().NotDependOnAny(Classes().That().ResideInNamespace("FluentPDF.Rendering.Interop"))
             .Because("Core must remain independent of Rendering infrastructure");
         ```

    **Restrictions:**
    - Do NOT skip any architecture rules
    - Ensure tests fail when rules are violated (verify with intentional violation)
    - Use descriptive .Because() clauses
    - Keep test file under 500 lines
    - Add XML doc comments

    **Success Criteria:**
    - All architecture tests pass
    - Tests catch violations (verify by breaking a rule)
    - Test output clearly explains violations
    - Tests run in < 5 seconds
    - Rules enforce clean architecture boundaries

    **Instructions:**
    1. Read design.md "Testing Strategy - Architecture Testing" section
    2. Edit tasks.md: change to `[-]`
    3. Create PdfRenderingArchitectureTests.cs
    4. Implement all architecture rules
    5. Run tests: `dotnet test tests/FluentPDF.Architecture.Tests --filter PdfRenderingArchitectureTests`
    6. Verify tests catch violations (intentionally break a rule, verify test fails)
    7. Log implementation with artifacts:
       - Document all architecture rules
       - Note enforcement of clean architecture
    8. Edit tasks.md: change to `[x]`

- [ ] 10. Integration testing with real PDFium and sample PDFs
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Integration/PdfViewerIntegrationTests.cs`
    - `tests/Fixtures/sample.pdf` (add sample PDF file)
  - Create integration tests using real PDFium (not mocked)
  - Add sample PDF files to test fixtures
  - Test full workflow: load document, render pages, navigate, zoom
  - Verify memory cleanup (no handle leaks)
  - Test error scenarios with corrupted PDFs
  - Purpose: Verify all components work together with real PDFium
  - _Leverage: PDFium, PdfDocumentService, PdfRenderingService_
  - _Requirements: All functional requirements_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** QA Integration Engineer specializing in end-to-end testing

    **Task:**
    Create integration tests using real PDFium library:

    1. **Setup Test Fixtures:**
       - Add `tests/Fixtures/sample.pdf` (3-5 page valid PDF)
       - Add `tests/Fixtures/corrupted.pdf` (invalid PDF file)
       - Add `tests/Fixtures/password-protected.pdf` (if available)

    2. **PdfViewerIntegrationTests.cs** (in Rendering.Tests/Integration/):
       - Test: LoadDocument_WithValidPdf_Succeeds
         - Use real PdfDocumentService (not mocked)
         - Load sample.pdf
         - Verify Result.IsSuccess
         - Verify PageCount > 0
         - Verify Handle is valid
         - Dispose document
       - Test: RenderPage_WithValidDocument_ReturnsImage
         - Load sample.pdf
         - Render page 1 at 100% zoom
         - Verify BitmapImage is not null
         - Verify image dimensions match expected size
       - Test: RenderAllPages_Succeeds
         - Load sample.pdf
         - Render all pages in sequence
         - Verify all renders succeed
         - Verify no errors logged
       - Test: ZoomLevels_RenderCorrectly
         - Load sample.pdf
         - Render page 1 at 50%, 100%, 150%, 200%
         - Verify image sizes scale correctly
       - Test: LoadCorruptedPdf_ReturnsError
         - Load corrupted.pdf
         - Verify Result.IsFailed
         - Verify error code is PDF_CORRUPTED or PDF_INVALID_FORMAT
       - Test: MemoryCleanup_NoLeaks
         - Load and dispose multiple documents
         - Verify SafeHandles are disposed
         - Check for handle leaks (use reflection if needed)

    **Restrictions:**
    - Do NOT mock PDFium in integration tests (use real library)
    - Ensure test PDFs are committed to repo (small files only)
    - Tests must clean up resources (no leaks)
    - Tests should run in CI (Windows agents only)
    - Add test category: [Trait("Category", "Integration")]

    **Success Criteria:**
    - All integration tests pass with real PDFium
    - Sample PDFs load and render successfully
    - Error scenarios handled correctly
    - No resource leaks detected
    - Tests run reliably in CI
    - Test coverage includes happy path and error cases

    **Instructions:**
    1. Read design.md "Testing Strategy - Integration Testing" section
    2. Edit tasks.md: change to `[-]`
    3. Add sample PDF files to test fixtures
    4. Create integration test class
    5. Implement all test scenarios
    6. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter "Category=Integration"`
    7. Verify tests pass with real PDFium
    8. Log implementation with artifacts:
       - Document integration test coverage
       - Note use of real PDFium library
       - Include sample PDF fixtures
    9. Edit tasks.md: change to `[x]`

- [ ] 11. Update CI/CD workflows to copy PDFium DLLs for tests
  - Files:
    - `.github/workflows/build.yml` (modify)
    - `.github/workflows/test.yml` (modify)
  - Modify build workflow to include PDFium DLLs in artifact
  - Modify test workflow to copy DLLs to test output directory
  - Ensure tests can find pdfium.dll at runtime
  - Add Windows-specific test runner
  - Purpose: Enable CI/CD to run PDF tests with PDFium
  - _Leverage: Existing GitHub Actions workflows_
  - _Requirements: CI/CD infrastructure_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** DevOps Engineer specializing in CI/CD and build automation

    **Task:**
    Update GitHub Actions workflows to support PDFium integration tests:

    1. **build.yml modifications:**
       - After building native libraries, copy PDFium DLLs to artifact:
         ```yaml
         - name: Copy PDFium DLLs to output
           run: |
             Copy-Item "libs/x64/bin/pdfium.dll" "src/FluentPDF.App/bin/Release/net8.0-windows10.0.19041.0/"
             Copy-Item "libs/x64/bin/pdfium.dll" "tests/FluentPDF.Rendering.Tests/bin/Release/net8.0/"
         ```
       - Include DLLs in artifact upload

    2. **test.yml modifications:**
       - Ensure DLLs are available before running tests:
         ```yaml
         - name: Ensure PDFium DLLs are in test directories
           run: |
             Copy-Item "libs/x64/bin/pdfium.dll" "tests/FluentPDF.Rendering.Tests/bin/Release/net8.0/" -Force
             Copy-Item "libs/x64/bin/pdfium.dll" "tests/FluentPDF.App.Tests/bin/Release/net8.0-windows10.0.19041.0/" -Force
         ```
       - Run integration tests separately:
         ```yaml
         - name: Run Integration Tests
           run: dotnet test --no-build --filter "Category=Integration" --logger "trx;LogFileName=integration-tests.trx"
         ```

    **Restrictions:**
    - Do NOT skip DLL copy steps
    - Ensure tests can find PDFium at runtime
    - Keep workflows efficient (use caching)
    - Add error handling for missing DLLs
    - Document any Windows-specific requirements

    **Success Criteria:**
    - Build workflow produces artifact with PDFium DLLs
    - Test workflow successfully runs integration tests
    - Tests find pdfium.dll at runtime
    - No DLL load errors in CI logs
    - Workflows complete successfully on GitHub

    **Instructions:**
    1. Edit tasks.md: change to `[-]`
    2. Modify build.yml to copy PDFium DLLs
    3. Modify test.yml to ensure DLLs available
    4. Push changes to GitHub and verify workflows run
    5. Check workflow logs for DLL loading success
    6. Log implementation with artifacts:
       - Document workflow modifications
       - Note PDFium DLL deployment strategy
    7. Edit tasks.md: change to `[x]`

- [ ] 12. Final integration testing and documentation
  - Files:
    - `docs/ARCHITECTURE.md` (update)
    - `docs/TESTING.md` (update)
    - `README.md` (update with PDF viewer features)
  - Perform end-to-end testing of PDF viewer
  - Update architecture documentation with PDF rendering components
  - Update testing documentation with integration test info
  - Update README with usage instructions
  - Verify all requirements met
  - Purpose: Ensure feature is complete and documented
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: |
    Implement the task for spec pdf-viewer-core. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Technical Writer and QA Lead performing final validation

    **Task:**
    Perform final integration testing and update documentation:

    1. **End-to-End Testing Checklist:**
       - [ ] App starts successfully
       - [ ] PDFium initializes without errors
       - [ ] "Open File" button works
       - [ ] Can load sample PDF
       - [ ] Page renders correctly
       - [ ] "Next Page" advances to page 2
       - [ ] "Previous Page" returns to page 1
       - [ ] Zoom In increases zoom (UI updates)
       - [ ] Zoom Out decreases zoom (UI updates)
       - [ ] Keyboard shortcuts work (Ctrl+O, arrows, Ctrl+Plus/Minus/0)
       - [ ] Page indicator shows correct page numbers
       - [ ] Loading spinner shows during render
       - [ ] Error handling works (try loading invalid file)
       - [ ] Memory cleanup (close document, check handles)
       - [ ] All architecture tests pass
       - [ ] All unit tests pass
       - [ ] All integration tests pass

    2. **Update ARCHITECTURE.md:**
       - Add section: "PDF Rendering Architecture"
       - Document PDFium integration
       - Explain P/Invoke layer design
       - Show component diagram with PdfDocumentService and PdfRenderingService
       - Document SafeHandle pattern for memory safety

    3. **Update TESTING.md:**
       - Add section: "PDF Rendering Tests"
       - Document integration testing approach
       - Explain sample PDF fixtures
       - List architecture rules for rendering layer

    4. **Update README.md:**
       - Add features: "PDF Document Viewing", "Page Navigation", "Zoom Controls"
       - Add usage instructions:
         - How to open a PDF
         - How to navigate pages
         - Keyboard shortcuts list
       - Add screenshot (optional, if available)

    5. **Validate Requirements:**
       - Go through each requirement in requirements.md
       - Verify all acceptance criteria met
       - Document any deviations or known issues
       - Create GitHub issues for any gaps

    **Restrictions:**
    - Do NOT approve if critical features missing
    - Ensure documentation is accurate
    - All links in docs must work
    - No broken functionality

    **Success Criteria:**
    - All E2E tests pass
    - Documentation is comprehensive and accurate
    - README clearly explains PDF viewer features
    - All requirements verified
    - Feature is production-ready

    **Instructions:**
    1. Read all requirements and verify each acceptance criteria
    2. Edit tasks.md: change to `[-]`
    3. Run complete E2E testing checklist
    4. Update ARCHITECTURE.md with PDF rendering
    5. Update TESTING.md with test strategy
    6. Update README.md with features and usage
    7. Validate all requirements
    8. Log implementation with artifacts:
       - Include final verification results
       - Document all updated files
       - Confirm PDF viewer core is complete
    9. Edit tasks.md: change to `[x]`
    10. Mark spec as complete: all tasks should show `[x]`

## Summary

This spec implements the core PDF viewing functionality:
- ✓ PDFium P/Invoke integration with SafeHandle
- ✓ PDF document loading with error handling
- ✓ PDF page rendering at multiple zoom levels
- ✓ Navigation controls (previous, next, go to page)
- ✓ Zoom controls (in, out, reset)
- ✓ Keyboard shortcuts
- ✓ MVVM architecture with CommunityToolkit
- ✓ Result pattern error handling
- ✓ Structured logging with performance metrics
- ✓ Comprehensive testing (unit, integration, architecture)
- ✓ CI/CD pipeline support

**Next steps after completion:**
- Implement text selection and copy (new spec)
- Implement PDF search functionality (new spec)
- Implement thumbnails sidebar (new spec)
- Implement PDF annotations (new spec)
