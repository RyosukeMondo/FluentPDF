# FluentPDF WinUI 3 Feature Inventory

**Analysis Date:** 2026-02-01
**Analyst:** inventory-analyst agent
**Purpose:** Complete inventory of WinUI 3 features for Avalonia migration

---

## ViewModels (22 Total)

### Core PDF Viewer (6 ViewModels)

| ViewModel | Purpose | API Surface | Classification |
|-----------|---------|-------------|----------------|
| **PdfViewerViewModel** | Main PDF viewer orchestration | Properties: CurrentDocument, PageCount, CurrentPageIndex, ZoomLevel, ViewMode (SinglePage/ContinuousScroll/TwoPage), IsSearchPanelVisible, IsThumbnailsVisible, IsBookmarksVisible. Commands: OpenCommand, SaveCommand, PrintCommand, ZoomInCommand, ZoomOutCommand, SetZoomCommand, FitWidthCommand, FitPageCommand, NextPageCommand, PreviousPageCommand, GoToPageCommand, ToggleThumbnailsCommand, ToggleBookmarksCommand, ShowSearchCommand, CloseCommand. Child VMs: BookmarksViewModel, FormFieldViewModel, DiagnosticsPanelViewModel, LogViewerViewModel, AnnotationViewModel, ThumbnailsViewModel, ImageInsertionViewModel, WatermarkViewModel, SearchPanelViewModel | Business Logic + UI Coordination |
| **BookmarksViewModel** | PDF bookmarks/outline tree | Properties: Bookmarks (ObservableCollection), SelectedBookmark, HasBookmarks. Commands: NavigateToBookmarkCommand, ExpandAllCommand, CollapseAllCommand | Business Logic |
| **ThumbnailsViewModel** | Page thumbnails with lazy loading | Properties: Thumbnails (ObservableCollection), SelectedPages, CurrentPage. Commands: NavigateToPageCommand, DeletePagesCommand, RotateRightCommand, RotateLeftCommand, SelectAllCommand, DeselectAllCommand | Business Logic |
| **AnnotationViewModel** | PDF annotations (highlights, notes, stamps) | Properties: Annotations (ObservableCollection), SelectedAnnotation, CurrentTool (None/Highlight/TextBox/Arrow/Rectangle/Circle), AnnotationColor, Opacity. Commands: AddAnnotationCommand, DeleteAnnotationCommand, SelectToolCommand, SaveAnnotationsCommand | Business Logic |
| **AnnotationToolbarViewModel** | Toolbar for annotation tools | Properties: SelectedTool, ToolColor, ToolOpacity, IsEnabled. Commands: SelectToolCommand (for each annotation type) | UI-Specific |
| **SearchPanelViewModel** | Text search and navigation | Properties: SearchQuery, MatchCount, CurrentMatchIndex, IsCaseSensitive, IsWholeWord. Commands: SearchCommand, NextMatchCommand, PreviousMatchCommand, ClearSearchCommand | Business Logic |

### Document Operations (7 ViewModels)

| ViewModel | Purpose | API Surface | Classification |
|-----------|---------|-------------|----------------|
| **ConversionViewModel** | PDF to image/text conversion | Properties: SourcePath, OutputPath, ConversionType (ToPNG/ToJPEG/ToText), Quality, DPI. Commands: SelectSourceCommand, SelectOutputCommand, ConvertCommand | Business Logic |
| **MergeViewModel** | Merge multiple PDFs | Properties: SourceFiles (ObservableCollection), OutputPath, IsProcessing. Commands: AddFilesCommand, RemoveFileCommand, MoveUpCommand, MoveDownCommand, MergeCommand | Business Logic |
| **SplitViewModel** | Split PDF into multiple files | Properties: SourcePath, OutputDirectory, SplitMode (ByPage/ByPageRange/ByBookmarks), PageRanges. Commands: SelectSourceCommand, SelectOutputCommand, SplitCommand | Business Logic |
| **ImageInsertionViewModel** | Insert images into PDF pages | Properties: SelectedImagePath, TargetPage, Position, Scale, Rotation. Commands: SelectImageCommand, InsertCommand, CancelCommand | Business Logic |
| **WatermarkViewModel** | Add text/image watermarks | Properties: WatermarkText, FontSize, FontFamily, TextColor, Opacity, Rotation, Position, ImagePath. Commands: SelectImageCommand, ApplyWatermarkCommand | Business Logic |
| **EncryptDialogViewModel** | PDF encryption/password protection | Properties: UserPassword, OwnerPassword, AllowPrinting, AllowCopying, AllowModification, EncryptionLevel (40-bit/128-bit/256-bit). Commands: EncryptCommand, CancelCommand | Business Logic |
| **FormFieldViewModel** | PDF form field interactions | Properties: FormFields (ObservableCollection), SelectedField, FieldValue. Commands: FillFieldCommand, ClearFieldCommand, ImportFdfCommand, ExportFdfCommand | Business Logic |

### UI Management (5 ViewModels)

| ViewModel | Purpose | API Surface | Classification |
|-----------|---------|-------------|----------------|
| **MainViewModel** | Main window orchestration, tab management | Properties: Tabs (ObservableCollection<TabViewModel>), ActiveTab, RecentFiles. Commands: NewTabCommand, CloseTabCommand, OpenFileCommand, OpenRecentCommand | UI-Specific |
| **MainWindowViewModel** | Main window state (theme, title) | Properties: Title, CurrentTheme (Light/Dark), IsFullScreen. Commands: ToggleThemeCommand, ToggleFullScreenCommand | UI-Specific |
| **MainToolbarViewModel** | Main toolbar state | Properties: IsFileMenuOpen, IsEditMenuOpen, IsViewMenuOpen, CurrentZoom. Minimal command surface (mostly delegates to PdfViewerViewModel) | UI-Specific |
| **TabViewModel** | Individual tab state | Properties: Header, FilePath, IsModified, IsClosed, ViewerViewModel (PdfViewerViewModel). Commands: CloseCommand | UI-Specific |
| **PresentationViewModel** | Full-screen presentation mode | Properties: CurrentPage, PageCount, IsPlaying, AutoAdvanceInterval. Commands: NextSlideCommand, PreviousSlideCommand, PlayCommand, PauseCommand, ExitCommand | UI-Specific |

### Diagnostics & Settings (4 ViewModels)

| ViewModel | Purpose | API Surface | Classification |
|-----------|---------|-------------|----------------|
| **DiagnosticsPanelViewModel** | Observability metrics (FPS, memory, render time) | Properties: FPS, MemoryUsage, RenderTime, AverageRenderTime, PeakMemory, DiagnosticLogs (ObservableCollection). Commands: ExportLogsCommand, ClearLogsCommand, EnableProfilingCommand | Business Logic |
| **LogViewerViewModel** | Application log viewing | Properties: LogEntries (ObservableCollection), SelectedLevel (Trace/Debug/Info/Warning/Error), SearchQuery. Commands: FilterCommand, ExportLogsCommand, ClearCommand, CopySelectedCommand | Business Logic |
| **SettingsViewModel** | Application settings | Properties: DefaultZoom, DefaultViewMode, Theme, EnableAnimations, RenderingQuality, CacheSize, AutoSaveDrafts. Commands: SaveSettingsCommand, ResetDefaultsCommand | Business Logic |
| **StampGalleryViewModel** | Stamp library for annotations | Properties: Stamps (ObservableCollection<StampItemViewModel>), SelectedStamp, Categories. Commands: AddStampCommand, DeleteStampCommand, ImportStampCommand | Business Logic |

---

## Services (17 Total)

### Rendering & Graphics (7 Services)

| Service | Interface | Purpose | Classification | Platform APIs |
|---------|-----------|---------|----------------|---------------|
| **RenderingCoordinator** | None | Orchestrates PDF rendering with fallback strategies | Business Logic | Returns `ImageSource` (WinUI) |
| **RenderingStrategyFactory** | None | Factory for rendering strategies | Business Logic | None |
| **WriteableBitmapRenderingStrategy** | IRenderingStrategy | Primary rendering: ImageSharp → WriteableBitmap | UI-Specific | `Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap`, `App.MainWindow.DispatcherQueue` |
| **FileBasedRenderingStrategy** | IRenderingStrategy | Fallback: Save to temp file → BitmapImage | UI-Specific | `Microsoft.UI.Xaml.Media.Imaging.BitmapImage`, `Windows.Storage.StorageFile` |
| **RenderingSettingsService** | IRenderingSettingsService | DPI detection, rendering quality settings | Business Logic | None |
| **RenderingObservabilityService** | None | Logs rendering metrics, errors, memory snapshots | Business Logic | None |
| **AnimationService** | IAnimationService | WinUI 3 Composition API animations | UI-Specific | `Microsoft.UI.Composition`, `Windows.UI.ViewManagement.UISettings` |

### UI Services (4 Services)

| Service | Interface | Purpose | Classification | Platform APIs |
|---------|-----------|---------|----------------|---------------|
| **NavigationService** | INavigationService | WinUI Frame-based page navigation | UI-Specific | `Microsoft.UI.Xaml.Controls.Frame` |
| **SettingsService** | ISettingsService (Core) | Persist app settings to ApplicationData | UI-Specific | `Windows.Storage.ApplicationData.Current.LocalSettings` |
| **JumpListService** | None | Windows taskbar Jump List integration | Platform-Specific | `Windows.UI.StartScreen.JumpList` |
| **RecentFilesService** | IRecentFilesService (Core) | Recent file tracking with Jump List sync | Business Logic | Uses JumpListService internally |

### Diagnostics & Validation (6 Services)

| Service | Interface | Purpose | Classification | Platform APIs |
|---------|-----------|---------|----------------|---------------|
| **MemoryMonitor** | None | Memory usage tracking, leak detection | Business Logic | `System.Diagnostics.Process` |
| **UIBindingVerifier** | None | Verifies XAML bindings at runtime | UI-Specific | Reflection on DependencyProperty |
| **DiagnosticCommandHandler** | None | CLI diagnostic commands (--test-render, --diagnostics) | Business Logic | None |
| **UiAutomationService** | IUiAutomationService | WinUI automation IDs for E2E testing | UI-Specific | `Microsoft.UI.Xaml.Automation.AutomationProperties` |
| **CoordinateMapper** | ICoordinateMapper (Core) | PDF coordinate ↔ screen coordinate translation | Business Logic | None |
| **MarshalingValidationService** | None | Validates P/Invoke marshaling for PDFium | Business Logic | None |

---

## Controls (13 Total)

### PDF Viewing Controls (4 Controls)

| Control | Base Class | Purpose | Platform APIs | Complexity |
|---------|-----------|---------|---------------|------------|
| **PdfViewerControl** | UserControl | Wrapper for PdfViewerPage, accepts ViewModel via DependencyProperty | `DependencyProperty`, `Microsoft.UI.Xaml.Controls.UserControl` | Simple |
| **ContinuousScrollViewer** | UserControl | Continuous scroll view with virtualization | `ScrollViewer`, `ItemsRepeater`, `Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap` | Complex (570 lines) |
| **TwoPageViewer** | UserControl | Two-page side-by-side view (book mode) | `Grid`, `Image`, gesture handlers | Medium |
| **AnnotationLayer** | UserControl | Overlay canvas for drawing annotations | `Canvas`, `InkCanvas`, `PointerPressed/Moved/Released` events | Complex (680 lines) |

### Navigation & Sidebars (3 Controls)

| Control | Base Class | Purpose | Platform APIs | Complexity |
|---------|-----------|---------|---------------|------------|
| **ThumbnailsSidebar** | UserControl | Page thumbnails with keyboard nav, drag-drop | `ListView`, `ItemsRepeater`, `Windows.ApplicationModel.DataTransfer`, keyboard events (`VirtualKey`) | Complex (320 lines) |
| **BookmarksPanel** | UserControl | Tree view of PDF bookmarks | `TreeView`, keyboard shortcuts | Medium |
| **ImageManipulationOverlay** | UserControl | Resize/rotate/move images on PDF pages | Canvas, `ManipulationDelta` events, keyboard events | Complex (750 lines) |

### Form & Validation (2 Controls)

| Control | Base Class | Purpose | Platform APIs | Complexity |
|---------|-----------|---------|---------------|------------|
| **FormFieldControl** | UserControl | Render PDF form fields (TextBox, ComboBox, CheckBox, RadioButton) | `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, templating | Medium |
| **ValidationErrorPanel** | UserControl | Display validation errors | `ItemsControl`, data binding | Simple |

### Diagnostics & UI Polish (4 Controls)

| Control | Base Class | Purpose | Platform APIs | Complexity |
|---------|-----------|---------|---------------|------------|
| **DiagnosticsPanelControl** | UserControl | Display FPS, memory, render times | `TextBlock`, data binding | Simple |
| **LogViewerControl** | UserControl | Scrollable log viewer with filtering | `ListView`, `ItemsRepeater`, virtualization | Medium |
| **GlassPanel** | UserControl | Liquid Glass UI acrylic effect | `Microsoft.UI.Composition`, `AcrylicBrush` | Medium |
| **ShimmerPlaceholder** | UserControl | Loading shimmer effect | `Microsoft.UI.Composition`, gradient animations | Medium |

---

## Key Dependencies Summary

### WinUI 3 Specific APIs
- **UI Controls:** `Microsoft.UI.Xaml.Controls.*` (Frame, ListView, TreeView, ItemsRepeater, etc.)
- **Data Binding:** `DependencyProperty`, `PropertyMetadata`
- **Events:** `RoutedEventArgs`, `PointerEventArgs`, `KeyRoutedEventArgs`, `ManipulationDeltaEventArgs`
- **Imaging:** `WriteableBitmap`, `BitmapImage`, `ImageSource`
- **Composition API:** `Microsoft.UI.Composition.*` for animations
- **Threading:** `DispatcherQueue`, `DispatcherQueue.TryEnqueue()`
- **File Pickers:** `Windows.Storage.Pickers.*` (FileOpenPicker, FileSavePicker, FolderPicker)
- **Clipboard:** `Windows.ApplicationModel.DataTransfer.Clipboard`
- **Keyboard:** `Windows.System.VirtualKey`, `InputKeyboardSource.GetKeyStateForCurrentThread()`

### Windows-Specific APIs
- **Jump List:** `Windows.UI.StartScreen.JumpList`
- **Settings:** `Windows.Storage.ApplicationData.Current.LocalSettings`
- **Color:** `Windows.UI.Color`
- **Theme Detection:** `Windows.UI.ViewManagement.UISettings`

### Cross-Platform APIs (Already Compatible)
- **MVVM:** CommunityToolkit.Mvvm (ObservableObject, RelayCommand, Messenger)
- **Logging:** Microsoft.Extensions.Logging
- **DI:** Microsoft.Extensions.DependencyInjection
- **PDF Rendering:** FluentPDF.Core, FluentPDF.Rendering (PDFium wrapper)

---

## Classification Summary

| Category | Count | Business Logic | UI-Specific | Platform-Specific |
|----------|-------|----------------|-------------|-------------------|
| **ViewModels** | 22 | 15 (68%) | 7 (32%) | 0 (0%) |
| **Services** | 17 | 9 (53%) | 6 (35%) | 2 (12%) |
| **Controls** | 13 | 0 (0%) | 13 (100%) | 0 (0%) |
| **TOTAL** | 52 | 24 (46%) | 26 (50%) | 2 (4%) |

**Key Insight:** Nearly half the codebase is business logic that can be reused. The migration primarily involves replacing UI-specific WinUI APIs with Avalonia equivalents.
