# Tasks Document

## Implementation Tasks

- [x] 1. Implement generic LruCache with automatic disposal
  - Files:
    - `src/FluentPDF.Core/Caching/LruCache.cs` (create)
    - `tests/FluentPDF.Core.Tests/Caching/LruCacheTests.cs` (create)
  - Create generic LRU cache using Dictionary + LinkedList
  - Implement TryGet (move to front), Add (evict LRU if full)
  - Implement automatic disposal of evicted items (IDisposable constraint)
  - Add Clear method disposing all items
  - Write comprehensive unit tests (add, get, eviction, disposal)
  - Purpose: Provide memory-efficient caching for thumbnails
  - _Leverage: Generic collections, IDisposable pattern_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_
  - _Prompt: Role: C# Developer with expertise in data structures and caching | Task: Implement generic LruCache<TKey, TValue> where TValue : IDisposable using Dictionary for O(1) lookup and LinkedList for LRU ordering. Implement TryGet (if exists, move node to front and return true), Add (if at capacity, dispose and remove last node, add new node at front). Implement Clear disposing all items. Constructor takes capacity parameter. Write unit tests verifying: adding items, retrieving updates LRU order, eviction when full, disposed items are disposed, Clear disposes all. | Restrictions: Must be thread-safe (use lock), generic type constraint TValue : IDisposable, O(1) access time, automatically dispose on eviction. | Success: Cache works correctly, LRU eviction verified, disposed items counted in tests, all operations O(1), thread-safe._

- [x] 2. Create IThumbnailRenderingService interface and implementation
  - Files:
    - `src/FluentPDF.Core/Services/IThumbnailRenderingService.cs` (create)
    - `src/FluentPDF.Rendering/Services/ThumbnailRenderingService.cs` (create)
    - `tests/FluentPDF.Rendering.Tests/Services/ThumbnailRenderingServiceTests.cs` (create)
  - Create service interface with RenderThumbnailAsync method
  - Implement service calling PdfRenderingService with low DPI (48) and zoom (0.2)
  - Add performance logging (log render time for each thumbnail)
  - Return Result<BitmapImage> with error handling
  - Write unit tests with mocked PdfRenderingService
  - Purpose: Provide optimized thumbnail rendering
  - _Leverage: IPdfRenderingService, Result<T>, Serilog_
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_
  - _Prompt: Role: Graphics Developer with expertise in image rendering optimization | Task: Create IThumbnailRenderingService interface with RenderThumbnailAsync(PdfDocument, pageNumber) returning Result<BitmapImage>. Implement ThumbnailRenderingService with constructor taking IPdfRenderingService and ILogger. Implementation: call _renderingService.RenderPageAsync with zoomLevel=0.2, dpi=48 for low-resolution output (~150x200px). Log render start/completion with page number and elapsed time. Handle errors returning Result.Fail with placeholder. Write unit tests mocking PdfRenderingService, verifying low DPI/zoom parameters, error handling. | Restrictions: Must use existing PdfRenderingService (do not duplicate rendering logic), log performance metrics, handle errors gracefully, maintain aspect ratio. | Success: Service renders thumbnails at low resolution, performance logged, errors handled with placeholders, unit tests verify logic._

- [x] 3. Create ThumbnailItem model and ThumbnailsViewModel
  - Files:
    - `src/FluentPDF.App/Models/ThumbnailItem.cs` (create)
    - `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs` (create)
    - `tests/FluentPDF.App.Tests/ViewModels/ThumbnailsViewModelTests.cs` (create)
  - Create ThumbnailItem model with PageNumber, Thumbnail, IsLoading, IsSelected properties
  - Create ThumbnailsViewModel with Thumbnails collection, cache management
  - Add LoadThumbnailsAsync command (populate collection, load visible thumbnails)
  - Add NavigateToPage command (notify PdfViewerViewModel via Messenger)
  - Integrate LruCache for thumbnail caching
  - Write unit tests for ViewModel logic
  - Purpose: Manage thumbnail state and caching
  - _Leverage: ObservableObject, RelayCommand, LruCache, WeakReferenceMessenger_
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
  - _Prompt: Role: MVVM Developer with expertise in state management | Task: Create ThumbnailItem (inherits ObservableObject) with observable properties: PageNumber (int), Thumbnail (BitmapImage?), IsLoading (bool), IsSelected (bool). Create ThumbnailsViewModel with Thumbnails (ObservableCollection<ThumbnailItem>), SelectedPageNumber (int), LruCache<int, BitmapImage> with capacity 100. Implement LoadThumbnailsAsync: create ThumbnailItem for each page, load visible thumbnails (first 20) asynchronously, cache results. Implement NavigateToPage: send message to PdfViewerViewModel. Constructor takes IThumbnailRenderingService, PdfDocument. Write unit tests mocking service, verifying cache usage, navigation messages. | Restrictions: Load thumbnails asynchronously (Task.Run), limit concurrent renders (max 4 using SemaphoreSlim), cache all rendered thumbnails, dispose cache on ViewModel disposal. | Success: ViewModel manages state correctly, thumbnails load asynchronously, cache works, navigation messages sent, unit tests verify logic._

- [x] 4. Create ThumbnailsSidebar XAML control
  - Files:
    - `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml` (create)
    - `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml.cs` (create)
  - Design XAML layout with ScrollViewer and ItemsRepeater
  - Create item template showing thumbnail image, page number, loading placeholder
  - Add click handler for thumbnail navigation
  - Style selected thumbnail with border highlight
  - Implement lazy loading (load thumbnails as user scrolls)
  - Purpose: Display thumbnail list in sidebar
  - _Leverage: ItemsRepeater, ScrollViewer, data binding_
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_
  - _Prompt: Role: WinUI Frontend Developer with expertise in XAML and Fluent Design | Task: Create ThumbnailsSidebar UserControl with ScrollViewer containing ItemsRepeater. ItemsRepeater ItemsSource bound to ThumbnailsViewModel.Thumbnails. ItemTemplate: Grid 150x220, Image (Source=Thumbnail, visible if IsLoaded), Rectangle (Fill=Gray, visible if IsLoading), TextBlock (Text=PageNumber, centered below), Border (highlight if IsSelected). Wrap in Button for click navigation. Add ScrollViewer.ViewChanged handler to detect visible range and load thumbnails lazily. Style selected thumbnail with ThemeResource border. | Restrictions: Use ItemsRepeater (not ListView) for performance, implement virtualization for large lists, smooth scrolling, use Fluent Design styling. | Success: Sidebar displays thumbnails correctly, lazy loading works, click navigation functional, selected thumbnail highlighted, smooth scrolling._

- [x] 5. Integrate ThumbnailsSidebar into PdfViewerPage
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml` (modify)
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (modify)
  - Add ThumbnailsSidebar control to left column of PdfViewerPage
  - Add IsSidebarVisible property to PdfViewerViewModel
  - Add ToggleSidebar command and toolbar button
  - Wire thumbnail selection to PdfViewerViewModel.CurrentPageNumber
  - Implement sidebar show/hide animation
  - Purpose: Integrate thumbnails into main viewer UI
  - _Leverage: Grid layout, property binding, animation_
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_
  - _Prompt: Role: WinUI Developer with expertise in layout and animations | Task: Modify PdfViewerPage.xaml: change root Grid to two columns (thumbnail sidebar width=200, content area width=*), add ThumbnailsSidebar in column 0, move existing content to column 1. Add IsSidebarVisible (bool) property to PdfViewerViewModel with [ObservableProperty]. Add ToggleSidebar [RelayCommand] toggling visibility. Add toolbar button binding to ToggleSidebarCommand. Bind sidebar Visibility to IsSidebarVisible converter. Subscribe to NavigateToPageMessage in PdfViewerViewModel to sync CurrentPageNumber with thumbnail selection. Add DoubleAnimation for sidebar width on show/hide. | Restrictions: Sidebar default visible, animation duration 200ms, ensure content area resizes smoothly, use Grid.Column attached properties, wire message passing correctly. | Success: Sidebar integrates into viewer, toggle button works, sidebar shows/hides with animation, thumbnail selection navigates main viewer, current page highlights in sidebar._

- [x] 6. Implement thumbnail click navigation and synchronization
  - Files:
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (modify)
    - `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs` (modify)
  - Add NavigateToPageMessage for ViewModel communication
  - Subscribe to message in PdfViewerViewModel to change CurrentPageNumber
  - Update ThumbnailsViewModel.SelectedPageNumber when PdfViewerViewModel.CurrentPageNumber changes
  - Add visual feedback (brief highlight animation) on thumbnail click
  - Prevent navigation during loading (disable click)
  - Purpose: Enable bidirectional navigation synchronization
  - _Leverage: WeakReferenceMessenger, observable properties_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_
  - _Prompt: Role: MVVM Developer with expertise in inter-ViewModel communication | Task: Create NavigateToPageMessage class. In ThumbnailsViewModel.NavigateToPage, send message via _messenger.Send(new NavigateToPageMessage(pageNumber)). In PdfViewerViewModel constructor, register message handler: _messenger.Register<NavigateToPageMessage>(this, (r, m) => { CurrentPageNumber = m.PageNumber; }). In ThumbnailsViewModel, observe PdfViewerViewModel.CurrentPageNumber changes (subscribe to PropertyChanged) and update SelectedPageNumber + thumbnail IsSelected flags. Disable NavigateToPage command when IsLoading=true. | Restrictions: Use WeakReferenceMessenger for loose coupling, unregister message handlers on disposal, prevent navigation loops (check if page already selected), disable during loading. | Success: Thumbnail click navigates viewer, viewer page change updates selected thumbnail, no navigation loops, loading state prevents clicks._

- [x] 7. Add performance optimizations and memory management
  - Files:
    - `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs` (modify)
    - `tests/FluentPDF.Benchmarks/Suites/ThumbnailBenchmarks.cs` (create)
  - Limit concurrent thumbnail renders (max 4 using SemaphoreSlim)
  - Implement lazy loading (load only visible thumbnails on scroll)
  - Add priority queue (load current page neighborhood first)
  - Monitor cache memory usage and adjust capacity dynamically
  - Add thumbnail rendering benchmarks (P99 latency < 200ms)
  - Purpose: Optimize performance and memory usage
  - _Leverage: SemaphoreSlim, BenchmarkDotNet, MemoryDiagnoser_
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_
  - _Prompt: Role: Performance Engineer with expertise in async optimization | Task: In ThumbnailsViewModel, add SemaphoreSlim(4) to limit concurrent renders. In LoadThumbnailsAsync, await semaphore before rendering, release after. Implement GetVisibleRange() calculating visible thumbnail indices from ScrollViewer position, load only visible range first. Implement priority loading: load current page ± 5 pages before distant pages. Add MemoryMonitor checking cache size (sum of BitmapImage sizes), reduce capacity if exceeds 50MB. Create ThumbnailBenchmarks in Benchmarks project: benchmark RenderThumbnailAsync P99 latency, verify < 200ms. | Restrictions: Do not block UI thread, limit parallel renders, prioritize visible thumbnails, monitor memory continuously, benchmarks run with real PDFium. | Success: Concurrent renders limited to 4, lazy loading works, priority loading implemented, memory stays under 50MB, P99 latency < 200ms verified in benchmarks._

- [x] 8. Add keyboard navigation and accessibility
  - Files:
    - `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml` (modify)
    - `src/FluentPDF.App/ViewModels/ThumbnailsViewModel.cs` (modify)
  - Add keyboard navigation (Tab, Arrow keys) for thumbnail focus
  - Add Enter key handler to navigate to focused thumbnail
  - Add accessible names for thumbnails ("Page 1 thumbnail")
  - Add screen reader announcements for sidebar visibility changes
  - Implement visual focus indicator (border around focused thumbnail)
  - Purpose: Ensure sidebar is accessible to all users
  - _Leverage: AutomationProperties, KeyboardAccelerator, FocusVisualStyle_
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  - _Prompt: Role: Accessibility Engineer with expertise in WinUI and screen readers | Task: In ThumbnailsSidebar.xaml, add AutomationProperties.Name="Page {PageNumber} thumbnail" to thumbnail Grid. Add KeyboardAccelerator (Key=Enter) to Button for navigation. Set IsTabStop=True on thumbnails, configure tab order. Add FocusVisualPrimaryBrush for focus indicator (ThemeResource border). In ThumbnailsViewModel, add ArrowKey handlers (Up/Down) moving focus. In ToggleSidebar command, announce visibility to screen readers (use AutomationPeer.RaiseNotificationEvent). Test with Narrator for proper announcements. | Restrictions: All interactive elements must be keyboard accessible, focus order logical (top to bottom), screen reader announcements clear, visible focus indicators required. | Success: Thumbnails keyboard navigable with Tab/Arrows, Enter navigates, focus indicators visible, screen reader announces correctly, passes accessibility validation._

- [x] 9. Integration testing with full document workflow
  - Files:
    - `tests/FluentPDF.App.Tests/Integration/ThumbnailsIntegrationTests.cs` (create)
  - Create integration tests loading document with thumbnails enabled
  - Test: Load document, verify thumbnails render for all pages
  - Test: Click thumbnail, verify main viewer navigates
  - Test: Navigate main viewer, verify thumbnail selection updates
  - Test: Toggle sidebar, verify show/hide works
  - Test: Cache memory usage stays under 50MB for 100-page document
  - Purpose: Verify thumbnails work end-to-end
  - _Leverage: FlaUI for UI automation, sample PDFs_
  - _Requirements: All functional requirements_
  - _Prompt: Role: QA Integration Engineer | Task: Create ThumbnailsIntegrationTests using FlaUI. Test: Launch app, load 100-page PDF, verify thumbnails appear in sidebar (wait for first 20 to load). Test: Click thumbnail for page 10, verify main viewer shows page 10 (check page indicator). Test: Navigate to page 20 in main viewer (Next button), verify thumbnail 20 is selected in sidebar. Test: Click ToggleSidebar button, verify sidebar hidden, click again, verify shown. Test: Monitor memory usage (Performance Counter), verify < 50MB for thumbnails. Use [Trait("Category", "Integration")]. | Restrictions: Tests require real PDFium and sample PDFs, run on Windows only, handle async loading (wait for IsLoading=false), verify memory via diagnostic tools. | Success: All integration tests pass, thumbnails load correctly, navigation works bidirectionally, sidebar toggles, memory under 50MB verified._

- [ ] 10. Documentation and final verification
  - Files:
    - `docs/THUMBNAILS.md` (create)
    - `README.md` (update - add thumbnails feature)
  - Document thumbnail sidebar architecture and design decisions
  - Document LRU cache implementation and memory management
  - Document keyboard shortcuts and accessibility features
  - Add thumbnails feature to main README
  - Verify all requirements met with end-to-end testing
  - Purpose: Ensure thumbnails feature is documented and complete
  - _Leverage: Existing documentation structure_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer | Task: Create docs/THUMBNAILS.md documenting: thumbnails sidebar feature overview, architecture (ThumbnailRenderingService, LruCache, ThumbnailsViewModel), keyboard navigation (Tab, Arrows, Enter), accessibility features (screen reader support, focus indicators), performance characteristics (P99 latency < 200ms, memory < 50MB). Update main README.md adding "Thumbnails Sidebar" to features list with description and screenshot. Verify end-to-end: open PDF with thumbnails, test navigation, toggle sidebar, check keyboard nav, verify memory usage. | Restrictions: Documentation must be clear and comprehensive, include code examples where relevant, screenshots encouraged, verify all features work. | Success: THUMBNAILS.md documents architecture and usage, README updated, end-to-end verification complete, all features working._

## Summary

This spec implements thumbnails sidebar for visual PDF navigation:
- Generic LRU cache with automatic disposal for memory efficiency
- ThumbnailRenderingService optimized for low-resolution rendering
- ThumbnailsViewModel managing state and caching
- ThumbnailsSidebar WinUI control with lazy loading and virtualization
- Bidirectional navigation synchronization (thumbnail click ↔ page navigation)
- Performance optimizations (concurrent render limits, priority loading)
- Full keyboard accessibility (Tab, Arrows, Enter, focus indicators)
- Memory management (cache < 50MB for 100 pages)

**Next steps after completion:**
- Add thumbnail context menu for page operations
- Implement drag-and-drop page reordering via thumbnails
- Show annotation indicators on thumbnails
- Integrate with search (highlight matching thumbnails)
