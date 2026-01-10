# Tasks Document

## Implementation Tasks

- [x] 1. Extend PdfiumInterop with bookmark P/Invoke declarations
  - Files:
    - `src/FluentPDF.Rendering/Interop/PdfiumInterop.cs` (modify - add bookmark functions)
    - `tests/FluentPDF.Rendering.Tests/Interop/PdfiumBookmarkInteropTests.cs`
  - Add P/Invoke declarations for bookmark API (FPDFBookmark_GetFirstChild, GetNextSibling, GetTitle, GetDest, etc.)
  - Add helper methods for UTF-16LE title decoding
  - Write unit tests for bookmark extraction P/Invoke
  - Purpose: Provide safe managed wrapper for PDFium bookmark API
  - _Leverage: Existing PdfiumInterop patterns, SafeHandle_
  - _Requirements: 1.1, 1.2, 1.3_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Native Interop Developer specializing in P/Invoke and PDFium API

    **Task:**
    Extend PdfiumInterop with bookmark extraction functions:

    1. **PdfiumInterop.cs modifications** (add to existing file):
       - Add bookmark P/Invoke declarations:
         ```csharp
         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern IntPtr FPDFBookmark_GetFirstChild(SafePdfDocumentHandle document, IntPtr parentBookmark);

         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern IntPtr FPDFBookmark_GetNextSibling(SafePdfDocumentHandle document, IntPtr bookmark);

         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern uint FPDFBookmark_GetTitle(IntPtr bookmark, byte[] buffer, uint bufferLength);

         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern IntPtr FPDFBookmark_GetDest(SafePdfDocumentHandle document, IntPtr bookmark);

         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern uint FPDFDest_GetDestPageIndex(SafePdfDocumentHandle document, IntPtr dest);

         [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
         internal static extern bool FPDFDest_GetLocationInPage(IntPtr dest, out int hasX, out int hasY, out int hasZoom, out float x, out float y, out float zoom);
         ```
       - Add helper method:
         ```csharp
         public static string GetBookmarkTitle(IntPtr bookmark)
         {
             // Get title length
             var length = FPDFBookmark_GetTitle(bookmark, null, 0);
             if (length == 0) return "(Untitled)";

             // Get title bytes (UTF-16LE)
             var buffer = new byte[length];
             FPDFBookmark_GetTitle(bookmark, buffer, length);

             // Decode UTF-16LE to string
             return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
         }
         ```

    2. **PdfiumBookmarkInteropTests.cs** (new file):
       - Test GetFirstChild with sample PDF returns valid handle
       - Test GetNextSibling iterates through siblings
       - Test GetBookmarkTitle decodes UTF-16LE correctly
       - Test GetDest returns valid destination handle
       - Test GetDestPageIndex returns correct page number

    **Restrictions:**
    - Do NOT use raw IntPtr for bookmark handles (they're temporary, no SafeHandle needed)
    - Follow existing PdfiumInterop patterns
    - Keep UTF-16LE decoding robust (handle null terminators)
    - Add XML documentation for all public methods

    **Success Criteria:**
    - All P/Invoke declarations compile and link correctly
    - UTF-16LE title decoding works for various character sets (test with Japanese, emoji, etc.)
    - Tests verify basic bookmark operations
    - No memory leaks (bookmarks don't need explicit cleanup)

    **Instructions:**
    1. Read design.md "Component 1: PdfiumBookmarkInterop" section
    2. Edit tasks.md: change to `[-]`
    3. Add P/Invoke declarations to existing PdfiumInterop.cs
    4. Implement helper methods
    5. Write comprehensive tests
    6. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter PdfiumBookmarkInteropTests`
    7. Log implementation with artifacts documenting P/Invoke functions
    8. Edit tasks.md: change to `[x]`

- [ ] 2. Create BookmarkNode domain model
  - Files:
    - `src/FluentPDF.Core/Models/BookmarkNode.cs`
    - `tests/FluentPDF.Core.Tests/Models/BookmarkNodeTests.cs`
  - Implement hierarchical BookmarkNode model
  - Add properties: Title, PageNumber, X, Y, Children
  - Add helper methods: Depth calculation, total node count
  - Write unit tests for model
  - Purpose: Provide domain model for hierarchical bookmark structure
  - _Leverage: Existing model patterns from Core project_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** C# Developer specializing in domain modeling and tree structures

    **Task:**
    Create hierarchical BookmarkNode model:

    1. **BookmarkNode.cs** (in Core/Models/):
       ```csharp
       public class BookmarkNode
       {
           public required string Title { get; init; }
           public int? PageNumber { get; init; }  // 1-based, null if no destination
           public float? X { get; init; }
           public float? Y { get; init; }
           public List<BookmarkNode> Children { get; init; } = new();

           public int GetTotalNodeCount()
           {
               int count = 1;
               foreach (var child in Children)
               {
                   count += child.GetTotalNodeCount();
               }
               return count;
           }
       }
       ```
       - Use init-only properties for immutability
       - Add XML doc comments
       - Validate Title is not null (required keyword)

    2. **BookmarkNodeTests.cs** (in Core.Tests/Models/):
       - Test BookmarkNode can be created with required properties
       - Test Children list is initialized empty
       - Test GetTotalNodeCount for single node returns 1
       - Test GetTotalNodeCount for node with children returns correct total
       - Test hierarchical structure (parent with nested children)
       - Use FluentAssertions for readable assertions

    **Restrictions:**
    - Do NOT add business logic to model (keep it simple)
    - Keep model immutable (init-only properties)
    - Follow structure.md code organization
    - Keep file under 100 lines

    **Success Criteria:**
    - Model compiles without errors
    - Children list properly initialized
    - Helper methods work correctly
    - Tests verify model behavior
    - XML documentation on all public members

    **Instructions:**
    1. Read design.md "Component 2: BookmarkNode Model" section
    2. Edit tasks.md: change to `[-]`
    3. Create BookmarkNode class
    4. Write unit tests
    5. Run tests: `dotnet test tests/FluentPDF.Core.Tests --filter BookmarkNodeTests`
    6. Log implementation with artifacts documenting BookmarkNode class
    7. Edit tasks.md: change to `[x]`

- [ ] 3. Implement IBookmarkService and BookmarkService
  - Files:
    - `src/FluentPDF.Core/Services/IBookmarkService.cs`
    - `src/FluentPDF.Rendering/Services/BookmarkService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/BookmarkServiceTests.cs`
  - Create service interface with ExtractBookmarksAsync method
  - Implement bookmark extraction using PdfiumInterop
  - Add recursive traversal algorithm (iterative, not recursive to avoid stack overflow)
  - Add comprehensive error handling with PdfError codes
  - Write unit tests with mocked PdfiumInterop
  - Purpose: Provide business logic for bookmark extraction
  - _Leverage: PdfiumInterop, PdfError, Result<T>, Serilog logging_
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Backend Service Developer specializing in tree traversal algorithms

    **Task:**
    Implement bookmark extraction service with iterative traversal:

    1. **IBookmarkService.cs** (in Core/Services/):
       ```csharp
       public interface IBookmarkService
       {
           Task<Result<List<BookmarkNode>>> ExtractBookmarksAsync(PdfDocument document);
       }
       ```

    2. **BookmarkService.cs** (in Rendering/Services/):
       - Implement IBookmarkService
       - Constructor: `public BookmarkService(PdfiumInterop interop, ILogger<BookmarkService> logger)`
       - **ExtractBookmarksAsync implementation:**
         - Use iterative algorithm with Stack<(IntPtr handle, BookmarkNode parent, int depth)>
         - Start with root: `FPDFBookmark_GetFirstChild(document, IntPtr.Zero)`
         - For each bookmark:
           - Get title: `PdfiumInterop.GetBookmarkTitle(bookmark)`
           - Get destination: `FPDFBookmark_GetDest(document, bookmark)`
           - Get page index: `FPDFDest_GetDestPageIndex(document, dest)` (convert to 1-based)
           - Get coordinates (optional): `FPDFDest_GetLocationInPage(dest, ...)`
           - Create BookmarkNode
           - Push children to stack: `FPDFBookmark_GetFirstChild(document, bookmark)`
           - Push next sibling to stack: `FPDFBookmark_GetNextSibling(document, bookmark)`
         - Limit depth to 20 to prevent infinite loops
         - Return Result.Ok with root bookmark list
       - Add error handling for extraction failures
       - Log bookmark count and extraction time

    3. **BookmarkServiceTests.cs** (in Rendering.Tests/Services/):
       - Mock PdfiumInterop
       - Test ExtractBookmarksAsync with flat list returns correct bookmarks
       - Test hierarchical extraction (parent with children)
       - Test empty bookmark list (no bookmarks)
       - Test bookmark with no destination returns null PageNumber
       - Test error handling (extraction failure)

    **Restrictions:**
    - Do NOT use recursive calls (use iterative algorithm with Stack)
    - Limit depth to 20 levels maximum
    - Log extraction performance
    - Follow Result<T> pattern
    - Keep service methods under 100 lines each

    **Success Criteria:**
    - Service extracts all bookmarks correctly
    - Hierarchical structure preserved
    - No stack overflow for deep hierarchies
    - Error handling covers all failure scenarios
    - Tests verify extraction logic
    - Performance logged

    **Instructions:**
    1. Read design.md "Component 3 and 4" sections
    2. Edit tasks.md: change to `[-]`
    3. Create interface and implementation
    4. Implement iterative extraction algorithm
    5. Add comprehensive error handling
    6. Write thorough unit tests
    7. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter BookmarkServiceTests`
    8. Log implementation with artifacts documenting extraction algorithm
    9. Edit tasks.md: change to `[x]`

- [ ] 4. Create BookmarksViewModel with panel state management
  - Files:
    - `src/FluentPDF.App/ViewModels/BookmarksViewModel.cs`
    - `tests/FluentPDF.App.Tests/ViewModels/BookmarksViewModelTests.cs`
  - Implement ViewModel with CommunityToolkit.Mvvm
  - Add observable properties for bookmarks, panel state
  - Add commands for loading bookmarks, toggling panel, navigation
  - Add settings persistence (panel visibility, width)
  - Write comprehensive unit tests
  - Purpose: Provide presentation logic for bookmarks panel UI
  - _Leverage: ObservableObject, RelayCommand, IBookmarkService, ApplicationData.LocalSettings_
  - _Requirements: 3.1-3.10, 4.1-4.7, 6.1-6.6_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI MVVM Developer specializing in data binding and state management

    **Task:**
    Create BookmarksViewModel with state persistence:

    1. **BookmarksViewModel.cs** (in App/ViewModels/):
       ```csharp
       public partial class BookmarksViewModel : ObservableObject
       {
           private readonly IBookmarkService _bookmarkService;
           private readonly PdfViewerViewModel _pdfViewerViewModel;
           private readonly ILogger<BookmarksViewModel> _logger;

           [ObservableProperty] private List<BookmarkNode>? _bookmarks;
           [ObservableProperty] private bool _isPanelVisible = true;
           [ObservableProperty] private double _panelWidth = 250;
           [ObservableProperty] private bool _isLoading;
           [ObservableProperty] private string _emptyMessage = "No bookmarks in this document";
           [ObservableProperty] private BookmarkNode? _selectedBookmark;

           [RelayCommand]
           private async Task LoadBookmarksAsync(PdfDocument document)
           {
               IsLoading = true;
               var result = await _bookmarkService.ExtractBookmarksAsync(document);
               if (result.IsSuccess)
               {
                   Bookmarks = result.Value;
                   _logger.LogInformation("Loaded {Count} root bookmarks ({Total} total)",
                       Bookmarks.Count,
                       Bookmarks.Sum(b => b.GetTotalNodeCount()));
               }
               else
               {
                   _logger.LogWarning("Failed to load bookmarks: {Errors}", result.Errors);
                   Bookmarks = new List<BookmarkNode>();
               }
               IsLoading = false;
           }

           [RelayCommand]
           private void TogglePanel()
           {
               IsPanelVisible = !IsPanelVisible;
               SavePanelState();
           }

           [RelayCommand]
           private async Task NavigateToBookmarkAsync(BookmarkNode bookmark)
           {
               if (bookmark.PageNumber.HasValue)
               {
                   await _pdfViewerViewModel.GoToPageAsync(bookmark.PageNumber.Value);
                   SelectedBookmark = bookmark;
               }
           }

           private void SavePanelState()
           {
               var settings = ApplicationData.Current.LocalSettings;
               settings.Values["BookmarksPanelVisible"] = IsPanelVisible;
               settings.Values["BookmarksPanelWidth"] = PanelWidth;
           }

           private void LoadPanelState()
           {
               var settings = ApplicationData.Current.LocalSettings;
               if (settings.Values.TryGetValue("BookmarksPanelVisible", out var visible))
                   IsPanelVisible = (bool)visible;
               if (settings.Values.TryGetValue("BookmarksPanelWidth", out var width))
               {
                   var w = (double)width;
                   PanelWidth = Math.Clamp(w, 150, 600);  // Validate range
               }
           }

           public BookmarksViewModel(IBookmarkService bookmarkService, PdfViewerViewModel pdfViewerViewModel, ILogger<BookmarksViewModel> logger)
           {
               _bookmarkService = bookmarkService;
               _pdfViewerViewModel = pdfViewerViewModel;
               _logger = logger;
               LoadPanelState();
           }
       }
       ```

    2. **BookmarksViewModelTests.cs** (in App.Tests/ViewModels/):
       - Mock IBookmarkService and PdfViewerViewModel
       - Test LoadBookmarksCommand populates Bookmarks
       - Test NavigateToBookmarkCommand calls GoToPageAsync
       - Test TogglePanelCommand changes IsPanelVisible
       - Test SavePanelState/LoadPanelState (mock ApplicationData)
       - Test property change notifications

    **Restrictions:**
    - Do NOT add business logic (use services)
    - Keep ViewModel under 300 lines
    - Validate panel width range
    - All commands must have clear responsibilities

    **Success Criteria:**
    - All observable properties fire PropertyChanged
    - Commands work correctly
    - Panel state persists across sessions
    - Tests run headless without WinUI runtime

    **Instructions:**
    1. Read design.md "Component 5" section
    2. Edit tasks.md: change to `[-]`
    3. Create ViewModel with all properties and commands
    4. Implement state persistence
    5. Write comprehensive tests
    6. Run tests: `dotnet test tests/FluentPDF.App.Tests --filter BookmarksViewModelTests`
    7. Log implementation with artifacts documenting ViewModel
    8. Edit tasks.md: change to `[x]`

- [ ] 5. Create BookmarksPanel UI control
  - Files:
    - `src/FluentPDF.App/Controls/BookmarksPanel.xaml`
    - `src/FluentPDF.App/Controls/BookmarksPanel.xaml.cs`
  - Design XAML layout with TreeView
  - Add hierarchical data template for BookmarkNode
  - Add empty state and loading indicator
  - Bind all controls to ViewModel properties
  - Add keyboard navigation support
  - Purpose: Provide bookmarks UI control
  - _Leverage: WinUI 3 TreeView, BookmarksViewModel_
  - _Requirements: 3.1-3.10, 5.1-5.7_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI Frontend Developer specializing in XAML and TreeView controls

    **Task:**
    Create BookmarksPanel UserControl with TreeView:

    1. **BookmarksPanel.xaml**:
       ```xml
       <UserControl x:Name="RootControl">
           <Grid>
               <!-- Empty state -->
               <TextBlock Text="{Binding EmptyMessage}"
                          HorizontalAlignment="Center"
                          VerticalAlignment="Center"
                          Style="{StaticResource SubtitleTextBlockStyle}">
                   <TextBlock.Visibility>
                       <Binding Path="Bookmarks.Count" Converter="{StaticResource CountToVisibilityConverter}">
                           <Binding.ConverterParameter>0</Binding.ConverterParameter>
                       </Binding>
                   </TextBlock.Visibility>
               </TextBlock>

               <!-- Loading indicator -->
               <ProgressRing IsActive="{Binding IsLoading}" Width="60" Height="60"/>

               <!-- TreeView -->
               <TreeView ItemsSource="{Binding Bookmarks}"
                         SelectedItem="{Binding SelectedBookmark, Mode=TwoWay}">
                   <TreeView.Visibility>
                       <Binding Path="Bookmarks.Count" Converter="{StaticResource InverseCountToVisibilityConverter}">
                           <Binding.ConverterParameter>0</Binding.ConverterParameter>
                       </Binding>
                   </TreeView.Visibility>
                   <TreeView.ItemTemplate>
                       <DataTemplate x:DataType="models:BookmarkNode">
                           <TreeViewItem ItemsSource="{x:Bind Children}">
                               <StackPanel Orientation="Horizontal" Spacing="8">
                                   <FontIcon Glyph="&#xE8A5;" FontSize="14"/>
                                   <TextBlock Text="{x:Bind Title}"
                                             ToolTipService.ToolTip="{x:Bind Title}"
                                             TextTrimming="CharacterEllipsis"
                                             MaxWidth="200"/>
                               </StackPanel>
                               <Interactivity:Interaction.Behaviors>
                                   <Core:EventTriggerBehavior EventName="Tapped">
                                       <Core:InvokeCommandAction Command="{Binding DataContext.NavigateToBookmarkCommand, ElementName=RootControl}"
                                                                CommandParameter="{Binding}"/>
                                   </Core:EventTriggerBehavior>
                               </Interactivity:Interaction.Behaviors>
                           </TreeViewItem>
                       </DataTemplate>
                   </TreeView.ItemTemplate>
               </TreeView>
           </Grid>
       </UserControl>
       ```
       - Add value converters in resources (CountToVisibilityConverter, InverseCountToVisibilityConverter)

    2. **BookmarksPanel.xaml.cs**:
       ```csharp
       public sealed partial class BookmarksPanel : UserControl
       {
           public BookmarksViewModel ViewModel { get; }

           public BookmarksPanel()
           {
               InitializeComponent();
               ViewModel = ((App)Application.Current).GetService<BookmarksViewModel>();
               RootControl.DataContext = ViewModel;
           }
       }
       ```

    **Restrictions:**
    - Do NOT add business logic in code-behind
    - Use data binding for all dynamic content
    - Ensure keyboard accessibility
    - Keep XAML well-formatted

    **Success Criteria:**
    - TreeView renders hierarchical bookmarks correctly
    - Empty state displays when no bookmarks
    - Loading indicator shows during extraction
    - Tapping bookmark navigates
    - Keyboard navigation works (arrow keys, Enter)
    - UI is accessible

    **Instructions:**
    1. Read design.md "Component 6" section
    2. Edit tasks.md: change to `[-]`
    3. Create XAML layout with TreeView
    4. Add data templates and bindings
    5. Implement value converters
    6. Create code-behind with ViewModel initialization
    7. Test UI by running app
    8. Log implementation with artifacts documenting UI control
    9. Edit tasks.md: change to `[x]`

- [ ] 6. Integrate BookmarksPanel into PdfViewerPage
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml` (modify)
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml.cs` (modify)
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (modify - add LoadBookmarksCommand)
  - Replace root Grid with SplitView
  - Add BookmarksPanel to SplitView.Pane
  - Add "Toggle Bookmarks" button to toolbar
  - Wire up bookmark loading when document opens
  - Purpose: Integrate bookmarks panel into main viewer
  - _Leverage: Existing PdfViewerPage layout, PdfViewerViewModel_
  - _Requirements: 3.9, 3.10, 4.1_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI Frontend Developer specializing in layout and integration

    **Task:**
    Integrate BookmarksPanel into PdfViewerPage:

    1. **PdfViewerPage.xaml modifications**:
       - Replace root `<Grid>` with `<SplitView>`
       - Configure SplitView:
         ```xml
         <SplitView IsPaneOpen="{Binding BookmarksViewModel.IsPanelVisible, Mode=TwoWay}"
                    OpenPaneLength="{Binding BookmarksViewModel.PanelWidth, Mode=TwoWay}"
                    DisplayMode="Inline"
                    PaneBackground="{ThemeResource LayerFillColorDefaultBrush}">
             <SplitView.Pane>
                 <controls:BookmarksPanel/>
             </SplitView.Pane>
             <SplitView.Content>
                 <!-- Existing viewer content (toolbar + page display) -->
             </SplitView.Content>
         </SplitView>
         ```
       - Add "Toggle Bookmarks" button to toolbar:
         ```xml
         <AppBarButton Icon="AlignLeft" Label="Bookmarks" Command="{Binding BookmarksViewModel.TogglePanelCommand}">
             <AppBarButton.KeyboardAccelerators>
                 <KeyboardAccelerator Key="B" Modifiers="Control"/>
             </AppBarButton.KeyboardAccelerators>
         </AppBarButton>
         ```

    2. **PdfViewerViewModel.cs modifications**:
       - Add property: `public BookmarksViewModel BookmarksViewModel { get; }`
       - Update constructor to inject BookmarksViewModel
       - In OpenDocumentCommand, after loading document:
         ```csharp
         await BookmarksViewModel.LoadBookmarksAsync(document);
         ```

    3. **PdfViewerPage.xaml.cs**:
       - Ensure BookmarksViewModel is accessible for binding

    **Restrictions:**
    - Do NOT break existing viewer functionality
    - Maintain responsive layout
    - Ensure panel can be resized
    - Follow existing UI patterns

    **Success Criteria:**
    - BookmarksPanel appears on left side
    - Toggle button shows/hides panel
    - Ctrl+B keyboard shortcut works
    - Panel width can be adjusted
    - Bookmarks load when document opens
    - Existing viewer functionality unaffected

    **Instructions:**
    1. Read design.md "Component 7" section
    2. Edit tasks.md: change to `[-]`
    3. Modify PdfViewerPage layout to use SplitView
    4. Add BookmarksPanel to pane
    5. Update PdfViewerViewModel to load bookmarks
    6. Add toolbar button
    7. Test integration by running app
    8. Log implementation with artifacts documenting integration
    9. Edit tasks.md: change to `[x]`

- [ ] 7. Register BookmarkService in DI container
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify)
  - Register IBookmarkService and BookmarkService
  - Register BookmarksViewModel
  - Ensure proper service lifetime
  - Purpose: Wire up bookmark components in application
  - _Leverage: Existing IHost DI container_
  - _Requirements: DI integration_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Application Integration Engineer specializing in dependency injection

    **Task:**
    Register bookmark services in DI container:

    1. **App.xaml.cs modifications (ConfigureServices)**:
       ```csharp
       // Register bookmark service
       services.AddSingleton<IBookmarkService, BookmarkService>();

       // Register ViewModels
       services.AddTransient<BookmarksViewModel>();
       ```
       - Use singleton for service (stateless)
       - Use transient for ViewModel (multiple instances possible)

    **Restrictions:**
    - Follow existing DI registration patterns
    - Do NOT create circular dependencies
    - Maintain service resolution efficiency

    **Success Criteria:**
    - All services registered and resolvable
    - Service lifetimes appropriate
    - No circular dependencies
    - App starts successfully

    **Instructions:**
    1. Read design.md "Dependency Injection Registration" section
    2. Edit tasks.md: change to `[-]`
    3. Modify App.xaml.cs to register services
    4. Test app startup
    5. Log implementation with artifacts documenting DI registrations
    6. Edit tasks.md: change to `[x]`

- [ ] 8. Integration testing with real PDFium and sample PDFs
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Integration/BookmarkIntegrationTests.cs`
    - `tests/Fixtures/bookmarked.pdf` (add sample PDF with bookmarks)
  - Create integration tests using real PDFium
  - Add sample PDF files with various bookmark structures
  - Test full workflow: extract bookmarks, verify structure
  - Test PDFs with no bookmarks
  - Purpose: Verify bookmark extraction works with real PDFium
  - _Leverage: PDFium, BookmarkService_
  - _Requirements: All functional requirements_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** QA Integration Engineer specializing in end-to-end testing

    **Task:**
    Create integration tests for bookmark extraction:

    1. **Setup Test Fixtures:**
       - Add `tests/Fixtures/bookmarked.pdf` (PDF with hierarchical bookmarks)
       - Add `tests/Fixtures/no-bookmarks.pdf` (PDF without bookmarks)

    2. **BookmarkIntegrationTests.cs** (in Rendering.Tests/Integration/):
       - Test: ExtractBookmarks_WithBookmarkedPdf_ReturnsStructure
         - Use real BookmarkService
         - Load bookmarked.pdf
         - Verify Result.IsSuccess
         - Verify bookmark count > 0
         - Verify hierarchical structure (some bookmarks have children)
       - Test: ExtractBookmarks_WithNoBookmarks_ReturnsEmptyList
         - Load no-bookmarks.pdf
         - Verify Result.IsSuccess
         - Verify bookmark list is empty
       - Test: BookmarkTitles_AreDecodedCorrectly
         - Verify bookmark titles are readable strings
         - Test special characters (if available)
       - Test: BookmarkPageNumbers_AreCorrect
         - Verify page numbers are 1-based
         - Verify page numbers within document range

    **Restrictions:**
    - Do NOT mock PDFium (use real library)
    - Ensure test PDFs are small (< 100KB)
    - Tests must clean up resources
    - Add test category: [Trait("Category", "Integration")]

    **Success Criteria:**
    - All integration tests pass with real PDFium
    - Bookmark structure verified
    - Tests run reliably in CI
    - Test coverage includes various bookmark scenarios

    **Instructions:**
    1. Read design.md "Testing Strategy - Integration Testing" section
    2. Edit tasks.md: change to `[-]`
    3. Add sample PDF files
    4. Create integration test class
    5. Implement test scenarios
    6. Run tests: `dotnet test tests/FluentPDF.Rendering.Tests --filter "Category=Integration"`
    7. Log implementation with artifacts documenting test coverage
    8. Edit tasks.md: change to `[x]`

- [ ] 9. Add ArchUnitNET rules for bookmark architecture
  - Files:
    - `tests/FluentPDF.Architecture.Tests/BookmarksArchitectureTests.cs`
  - Create architecture tests for bookmark components
  - Add rule: BookmarkService implements IBookmarkService
  - Add rule: BookmarksViewModel does not reference PDFium
  - Add rule: BookmarkNode has no dependencies
  - Purpose: Enforce architectural rules for bookmark components
  - _Leverage: Existing ArchUnitNET tests_
  - _Requirements: Architecture integrity_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Software Architect specializing in architecture testing

    **Task:**
    Add ArchUnitNET tests for bookmark architecture:

    1. **BookmarksArchitectureTests.cs** (in Architecture.Tests/):
       - Test: BookmarkService_Should_ImplementInterface
       - Test: BookmarksViewModel_ShouldNot_Reference_Pdfium
       - Test: BookmarkNode_Should_HaveNoDependencies
       - Test: IBookmarkService_Should_ResideIn_CoreServices

    **Restrictions:**
    - Ensure tests fail when rules are violated
    - Use descriptive .Because() clauses
    - Add XML doc comments

    **Success Criteria:**
    - All architecture tests pass
    - Tests catch violations
    - Rules enforce clean architecture

    **Instructions:**
    1. Read design.md "Testing Strategy - Architecture Testing" section
    2. Edit tasks.md: change to `[-]`
    3. Create BookmarksArchitectureTests.cs
    4. Implement architecture rules
    5. Run tests: `dotnet test tests/FluentPDF.Architecture.Tests --filter BookmarksArchitectureTests`
    6. Log implementation with artifacts documenting rules
    7. Edit tasks.md: change to `[x]`

- [ ] 10. Final integration testing and documentation
  - Files:
    - `docs/ARCHITECTURE.md` (update with bookmarks section)
    - `README.md` (update with bookmarks feature)
  - Perform end-to-end testing of bookmarks panel
  - Update architecture documentation
  - Update README with usage instructions
  - Verify all requirements met
  - Purpose: Ensure feature is complete and documented
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: |
    Implement the task for spec bookmarks-panel. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Technical Writer and QA Lead performing final validation

    **Task:**
    Perform final testing and update documentation:

    1. **E2E Testing Checklist:**
       - [ ] App starts with bookmarks panel visible
       - [ ] Can load PDF with bookmarks
       - [ ] Bookmarks display in TreeView
       - [ ] Can expand/collapse bookmark nodes
       - [ ] Clicking bookmark navigates to page
       - [ ] Toggle button shows/hides panel
       - [ ] Ctrl+B keyboard shortcut works
       - [ ] Panel width can be resized
       - [ ] Panel state persists across sessions
       - [ ] PDF without bookmarks shows empty state
       - [ ] All unit tests pass
       - [ ] All integration tests pass
       - [ ] All architecture tests pass

    2. **Update ARCHITECTURE.md:**
       - Add section: "Bookmarks Panel Architecture"
       - Document bookmark extraction algorithm
       - Show component diagram

    3. **Update README.md:**
       - Add feature: "PDF Bookmarks Navigation"
       - Add usage instructions

    4. **Validate Requirements:**
       - Go through each requirement
       - Verify all acceptance criteria met

    **Success Criteria:**
    - All E2E tests pass
    - Documentation complete
    - All requirements verified
    - Feature production-ready

    **Instructions:**
    1. Read all requirements
    2. Edit tasks.md: change to `[-]`
    3. Run E2E testing checklist
    4. Update documentation
    5. Validate requirements
    6. Log implementation with verification results
    7. Edit tasks.md: change to `[x]`
    8. Mark spec as complete

## Summary

This spec implements the PDF bookmarks panel:
- Bookmark extraction using PDFium API
- Hierarchical BookmarkNode model
- TreeView-based UI with expand/collapse
- Click-to-navigate functionality
- Panel state persistence
- Keyboard shortcuts (Ctrl+B)
- Comprehensive testing (unit, integration, architecture)

**Next steps after completion:**
- Implement bookmark search/filter (enhancement)
- Implement bookmark creation (requires PDF editing)
