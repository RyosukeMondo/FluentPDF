# FluentPDF Avalonia - Components Reference

## Component Architecture

### Component Hierarchy

```
App.axaml
└── MainWindow.axaml
    ├── Menu Bar
    ├── TabControl
    │   └── PdfViewerPage.axaml (per tab)
    │       ├── Toolbar
    │       ├── SearchPanel.axaml (collapsible)
    │       └── Grid Layout
    │           ├── ThumbnailsSidebar.axaml (left)
    │           ├── PDF Content Area (center)
    │           └── BookmarksPanel.axaml (right)
    └── Empty State Overlay
```

## Component Details

### 1. SearchPanel Control

**File:** `Controls/SearchPanel.axaml`
**ViewModel:** `SearchPanelViewModel`

#### XAML Structure
```xml
<UserControl>
    <Border>  <!-- Container with border -->
        <Grid ColumnDefinitions="Auto,*,Auto,Auto,...">
            <PathIcon />        <!-- Search icon -->
            <TextBox />         <!-- Search input -->
            <TextBlock />       <!-- Match counter -->
            <Button />          <!-- Previous match -->
            <Button />          <!-- Next match -->
            <ToggleButton />    <!-- Case sensitive -->
            <ToggleButton />    <!-- Whole word -->
            <TextBox />         <!-- Replace input -->
            <Button />          <!-- Replace -->
            <Button />          <!-- Replace all -->
            <Button />          <!-- Close -->
        </Grid>
    </Border>
</UserControl>
```

#### Properties
```csharp
public class SearchPanelViewModel
{
    string SearchText { get; set; }          // Search query
    string ReplaceText { get; set; }         // Replacement text
    string SearchResultsText { get; set; }   // "5 of 23"
    bool HasMatches { get; set; }            // Are there matches?
    bool CaseSensitive { get; set; }         // Case-sensitive search
    bool WholeWord { get; set; }             // Whole word only
    bool PreviewMode { get; set; }           // Preview replacements
    int CurrentMatchIndex { get; set; }      // Current match (0-based)
    int TotalMatches { get; set; }           // Total matches
}
```

#### Commands
- `PreviousMatchCommand` - Navigate to previous match
- `NextMatchCommand` - Navigate to next match
- `ReplaceCommand` - Replace current match
- `ReplaceAllCommand` - Replace all matches
- `CloseSearchCommand` - Close search panel

#### Usage
```xml
<controls:SearchPanel DataContext="{Binding SearchPanelViewModel}"
                      IsVisible="{Binding IsSearchVisible}"/>
```

---

### 2. ThumbnailsSidebar Control

**File:** `Controls/ThumbnailsSidebar.axaml`
**ViewModel:** `ThumbnailsViewModel`

#### XAML Structure
```xml
<UserControl>
    <Border>  <!-- Container with right border -->
        <Grid RowDefinitions="Auto,*">
            <!-- Toolbar -->
            <Border>
                <StackPanel>
                    <Button />  <!-- Rotate left -->
                    <Button />  <!-- Rotate right -->
                    <Button />  <!-- Delete pages -->
                </StackPanel>
            </Border>

            <!-- Thumbnails List -->
            <ScrollViewer>
                <ItemsControl>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Button>  <!-- Thumbnail item -->
                                <Grid>
                                    <Border>  <!-- Thumbnail image container -->
                                        <ProgressBar />  <!-- Loading -->
                                        <Image />        <!-- Thumbnail -->
                                    </Border>
                                    <TextBlock />  <!-- Page number -->
                                </Grid>
                            </Button>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Grid>
    </Border>
</UserControl>
```

#### Properties
```csharp
public class ThumbnailsViewModel
{
    ObservableCollection<ThumbnailItem> Thumbnails { get; }  // Thumbnail list
    int SelectedPageNumber { get; set; }                      // Current page
    bool IsVisible { get; set; }                              // Sidebar visible
}

public class ThumbnailItem
{
    int PageNumber { get; set; }           // Page number (1-based)
    Bitmap? Thumbnail { get; set; }        // Thumbnail image
    bool IsSelected { get; set; }          // Is selected
    bool IsLoading { get; set; }           // Is loading
}
```

#### Commands
- `NavigateToPageCommand` - Navigate to clicked page
- `RotateLeftCommand` - Rotate selected pages left
- `RotateRightCommand` - Rotate selected pages right
- `Rotate180Command` - Rotate selected pages 180°
- `DeletePagesCommand` - Delete selected pages
- `SelectAllCommand` - Select all pages

#### Usage
```xml
<controls:ThumbnailsSidebar DataContext="{Binding ThumbnailsViewModel}"
                            IsVisible="{Binding IsVisible}"
                            Width="180"/>
```

#### Lazy Loading
```csharp
// Load visible thumbnails when user scrolls
await viewModel.LoadVisibleThumbnailsAsync(startIndex: 0, endIndex: 20);

// Load single thumbnail
await viewModel.LoadThumbnailAsync(thumbnailItem);
```

---

### 3. BookmarksPanel Control

**File:** `Controls/BookmarksPanel.axaml`
**ViewModel:** `BookmarksViewModel`

#### XAML Structure
```xml
<UserControl>
    <Border>  <!-- Container with right border -->
        <Grid RowDefinitions="Auto,*">
            <!-- Header -->
            <Border>
                <TextBlock Text="Bookmarks" />
            </Border>

            <!-- Content -->
            <Grid>
                <!-- Loading Indicator -->
                <StackPanel IsVisible="{Binding IsLoading}">
                    <ProgressBar />
                    <TextBlock />
                </StackPanel>

                <!-- Empty State -->
                <StackPanel IsVisible="{Binding !HasBookmarks}">
                    <PathIcon />  <!-- Book icon -->
                    <TextBlock Text="No bookmarks" />
                </StackPanel>

                <!-- Bookmarks Tree -->
                <ScrollViewer IsVisible="{Binding HasBookmarks}">
                    <TreeView ItemsSource="{Binding Bookmarks}">
                        <TreeDataTemplate>
                            <Button Command="{Binding NavigateCommand}">
                                <TextBlock Text="{Binding Title}" />
                            </Button>
                        </TreeDataTemplate>
                    </TreeView>
                </ScrollViewer>
            </Grid>
        </Grid>
    </Border>
</UserControl>
```

#### Properties
```csharp
public class BookmarksViewModel
{
    List<BookmarkNode>? Bookmarks { get; set; }       // Root bookmarks
    bool IsPanelVisible { get; set; }                 // Panel visible
    double PanelWidth { get; set; }                   // Panel width (150-600px)
    bool IsLoading { get; set; }                      // Loading bookmarks
    bool IsVisible { get; set; }                      // Sidebar visible
    string EmptyMessage { get; set; }                 // Empty state message
    BookmarkNode? SelectedBookmark { get; set; }      // Selected bookmark
    bool HasBookmarks { get; }                        // Has any bookmarks
}

public class BookmarkNode
{
    string Title { get; set; }                        // Bookmark title
    int? PageNumber { get; set; }                     // Target page (1-based)
    List<BookmarkNode> Children { get; set; }         // Child bookmarks
}
```

#### Commands
- `LoadBookmarksCommand` - Load bookmarks from PDF
- `TogglePanelCommand` - Show/hide panel
- `NavigateToBookmarkCommand` - Navigate to bookmark page

#### Usage
```xml
<controls:BookmarksPanel DataContext="{Binding BookmarksViewModel}"
                         IsVisible="{Binding IsVisible}"
                         Width="250"/>
```

---

### 4. SettingsPage View

**File:** `Views/SettingsPage.axaml`
**ViewModel:** `SettingsViewModel`

#### XAML Structure
```xml
<UserControl>
    <ScrollViewer>
        <StackPanel>
            <!-- Page Title -->
            <TextBlock Text="Settings" />

            <!-- Appearance Section -->
            <Border>
                <StackPanel>
                    <TextBlock Text="Appearance" />
                    <RadioButton Content="Light" />
                    <RadioButton Content="Dark" />
                    <RadioButton Content="Use system" />
                </StackPanel>
            </Border>

            <!-- Rendering Section -->
            <Border>
                <StackPanel>
                    <TextBlock Text="Rendering" />
                    <ComboBox />  <!-- Quality selector -->
                    <TextBlock />  <!-- Description -->
                    <Border />  <!-- Ultra warning -->
                    <Button />  <!-- Apply button -->
                </StackPanel>
            </Border>

            <!-- Default Settings Section -->
            <Border>
                <StackPanel>
                    <TextBlock Text="Default Settings" />
                    <ComboBox />  <!-- Default zoom -->
                    <ComboBox />  <!-- Scroll mode -->
                </StackPanel>
            </Border>

            <!-- Privacy Section -->
            <Border>
                <StackPanel>
                    <TextBlock Text="Privacy" />
                    <CheckBox />  <!-- Telemetry -->
                    <CheckBox />  <!-- Crash reporting -->
                </StackPanel>
            </Border>

            <!-- About Section -->
            <StackPanel>
                <TextBlock Text="About" />
                <TextBlock Text="FluentPDF" />
                <TextBlock Text="Version 1.0.0" />
                <Button />  <!-- Reset to defaults -->
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

#### Properties
```csharp
public class SettingsViewModel
{
    // Quality
    QualityOption? SelectedQualityOption { get; set; }
    IReadOnlyList<QualityOption> QualityOptions { get; }
    bool IsUltraQualitySelected { get; }

    // Defaults
    ZoomLevel DefaultZoom { get; set; }
    ScrollMode ScrollMode { get; set; }
    IReadOnlyList<ZoomLevel> ZoomLevels { get; }
    IReadOnlyList<ScrollMode> ScrollModes { get; }

    // Theme
    AppTheme Theme { get; set; }
    bool IsLightTheme { get; set; }
    bool IsDarkTheme { get; set; }
    bool IsSystemTheme { get; set; }

    // Privacy
    bool TelemetryEnabled { get; set; }
    bool CrashReportingEnabled { get; set; }
}

public record QualityOption(
    RenderingQuality Quality,
    string DisplayName,
    string Description
);

public enum RenderingQuality
{
    Auto,    // Automatic (96-192 DPI based on display)
    Low,     // 75 DPI
    Medium,  // 96 DPI
    High,    // 144 DPI
    Ultra    // 192+ DPI
}
```

#### Commands
- `ApplyQualityCommand` - Apply rendering quality
- `ResetToDefaultsCommand` - Reset all settings

#### Usage
```csharp
// Navigate to settings
navigationService.NavigateTo<SettingsPage>();
```

---

### 5. MessageDialog Window

**File:** `Views/MessageDialog.axaml`

#### XAML Structure
```xml
<Window>
    <Grid RowDefinitions="*,Auto">
        <!-- Content Area -->
        <StackPanel>
            <PathIcon />    <!-- Dialog icon -->
            <TextBlock />   <!-- Title -->
            <TextBlock />   <!-- Message -->
        </StackPanel>

        <!-- Button Area -->
        <Border>
            <Button Content="OK" />
        </Border>
    </Grid>
</Window>
```

#### Dialog Types
```csharp
public enum MessageDialogType
{
    Information,  // Blue info icon
    Warning,      // Orange warning icon
    Error,        // Red error icon
    Success       // Green success icon
}
```

#### Usage
```csharp
// Show information
await MessageDialog.ShowAsync(
    owner: this,
    title: "Information",
    message: "Operation completed successfully",
    dialogType: MessageDialogType.Information
);

// Show error
await MessageDialog.ShowAsync(
    owner: this,
    title: "Error",
    message: "Failed to load file",
    dialogType: MessageDialogType.Error
);
```

---

### 6. ConfirmDialog Window

**File:** `Views/ConfirmDialog.axaml`

#### XAML Structure
```xml
<Window>
    <Grid RowDefinitions="*,Auto">
        <!-- Content Area -->
        <StackPanel>
            <PathIcon />    <!-- Question icon -->
            <TextBlock />   <!-- Title -->
            <TextBlock />   <!-- Message -->
        </StackPanel>

        <!-- Button Area -->
        <Border>
            <StackPanel Orientation="Horizontal">
                <Button Content="Yes" />
                <Button Content="No" />
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

#### Usage
```csharp
bool confirmed = await ConfirmDialog.ShowAsync(
    owner: this,
    title: "Confirm Delete",
    message: "Delete 5 pages? This action cannot be undone."
);

if (confirmed)
{
    // User clicked Yes
    await DeletePagesAsync();
}
else
{
    // User clicked No
    // Do nothing
}
```

#### Return Value
- `true` - User clicked Yes
- `false` - User clicked No or closed dialog

---

## Value Converters

### BoolToBorderBrushConverter
**File:** `Converters/BoolToBorderBrushConverter.cs`

Converts boolean selection state to border brush color.

```csharp
// true  → Blue border (#0078D4)
// false → Transparent border
```

**Usage:**
```xml
<Button BorderBrush="{Binding IsSelected, Converter={StaticResource BoolToBorderBrushConverter}}" />
```

### BoolToVisibilityConverter
**File:** `Converters/BoolToVisibilityConverter.cs`

```csharp
// true  → Visible
// false → Collapsed
```

### InverseBoolToVisibilityConverter
**File:** `Converters/InverseBoolToVisibilityConverter.cs`

```csharp
// true  → Collapsed
// false → Visible
```

### CountToVisibilityConverter
**File:** `Converters/CountToVisibilityConverter.cs`

```csharp
// count > 0  → Visible
// count == 0 → Collapsed
```

**Usage:**
```xml
<Grid IsVisible="{Binding Tabs.Count, Converter={StaticResource CountToVisibilityConverter}}">
```

---

## Styling Guidelines

### Colors

**Light Theme:**
```
- Accent Blue: #0078D4
- Background: #F3F3F3
- Surface: #FFFFFF
- Border: #E1E1E1
- Text: #000000
- Text Secondary: #606060
```

**Dark Theme:**
```
- Accent Blue: #0078D4
- Background: #1E1E1E
- Surface: #2D2D2D
- Border: #3E3E3E
- Text: #FFFFFF
- Text Secondary: #CCCCCC
```

### Spacing
```
- Small:  4px
- Medium: 8px
- Large:  12px
- XLarge: 16px
- Section: 24px
```

### Font Sizes
```
- Small:  12px
- Normal: 14px
- Medium: 16px
- Large:  18px
- Title:  20px
- Header: 28px
```

### Border Radius
```
- Small:  2px
- Medium: 4px
- Large:  8px
```

---

## Performance Considerations

### Lazy Loading
- Thumbnails load on-demand (scroll to load)
- LRU cache evicts old thumbnails (50MB limit)
- Concurrent render limit: 4 simultaneous renders

### Memory Management
- Dispose bitmaps when evicted from cache
- Unsubscribe from events in Dispose()
- Weak references for messaging

### Async Operations
- All I/O operations are async
- UI thread marshaling via Dispatcher.UIThread.InvokeAsync()
- Cancellation token support for long operations

---

## Testing

### Unit Test ViewModels
```csharp
[Fact]
public async Task SearchPanel_FindsMatches()
{
    var vm = new SearchPanelViewModel(searchService, replacementService, logger);
    vm.SetDocument(testDocument);
    vm.SearchText = "example";

    // Trigger search
    await Task.Delay(100);  // Wait for debounce

    Assert.True(vm.HasMatches);
    Assert.Equal(5, vm.TotalMatches);
}
```

### UI Test Controls
```csharp
[AvaloniaFact]
public async Task ThumbnailsSidebar_RendersItems()
{
    var window = new Window();
    var vm = new ThumbnailsViewModel(thumbnailService, pageOpsService, logger);
    var control = new ThumbnailsSidebar { DataContext = vm };

    window.Content = control;
    window.Show();

    await vm.LoadThumbnailsAsync(testDocument);

    Assert.Equal(10, vm.Thumbnails.Count);
}
```

---

**Reference Version:** 1.0
**Last Updated:** January 28, 2026
**See Also:** `AVALONIA_UI_IMPLEMENTATION_COMPLETE.md`, `AVALONIA_UI_QUICK_START.md`
