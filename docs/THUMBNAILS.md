# Thumbnails Sidebar

FluentPDF provides a visual navigation sidebar displaying thumbnail previews of all pages in the PDF document. The thumbnails sidebar enables quick visual browsing and page navigation with optimized rendering and memory management.

## Features

### Visual Navigation
- Thumbnail previews for all pages in the document
- Click thumbnail to navigate to specific page
- Current page highlighted with accent border
- Lazy loading of thumbnails as you scroll
- Smooth scrolling with virtualization for large documents

### Performance Optimizations
- Low-resolution rendering (48 DPI, 20% zoom) for fast load times
- LRU cache with automatic memory management (< 50MB for 100 pages)
- Concurrent rendering limit (max 4 simultaneous renders)
- Priority loading: current page neighborhood loaded first
- Cached thumbnails reused to avoid re-rendering

### Keyboard Accessibility
- Full keyboard navigation support
- Tab through thumbnails
- Arrow keys (Up/Down) to navigate between thumbnails
- Enter or Space to navigate to focused thumbnail
- Visible focus indicators (accent border)
- Screen reader announcements for navigation

### Responsive UI
- Toggle sidebar visibility with toolbar button
- Sidebar show/hide animation (200ms)
- Content area resizes smoothly when sidebar toggled
- Thumbnails sidebar default visible on document open

## Usage

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Tab` | Focus next thumbnail |
| `Shift+Tab` | Focus previous thumbnail |
| `↑` (Up Arrow) | Navigate to previous thumbnail |
| `↓` (Down Arrow) | Navigate to next thumbnail |
| `Enter` | Navigate to focused thumbnail page |
| `Space` | Navigate to focused thumbnail page |

### Mouse Interaction

1. **Click Thumbnail**: Navigate to that page in the main viewer
2. **Scroll Sidebar**: View more thumbnails (lazy loads as you scroll)
3. **Toggle Sidebar Button**: Show/hide the thumbnails sidebar

### Visual Indicators

- **Accent Border**: Current page thumbnail highlighted
- **Loading Spinner**: Displayed while thumbnail is rendering
- **Focus Rectangle**: Shows keyboard focus on thumbnails

## Architecture

### Service Layer

#### IThumbnailRenderingService
Interface for rendering low-resolution thumbnail images:

```csharp
public interface IThumbnailRenderingService
{
    Task<Result<Stream>> RenderThumbnailAsync(PdfDocument document, int pageNumber);
}
```

**Implementation**: `ThumbnailRenderingService`
- Wraps `IPdfRenderingService` with optimized thumbnail settings
- DPI: 48 (half of standard 96 DPI)
- Zoom: 0.2 (20% of full size)
- Target size: ~150x200 pixels for letter-sized pages
- Logs render time for performance monitoring

```csharp
public sealed class ThumbnailRenderingService : IThumbnailRenderingService
{
    private const double ThumbnailDpi = 48.0;
    private const double ThumbnailZoom = 0.2;

    public async Task<Result<Stream>> RenderThumbnailAsync(
        PdfDocument document,
        int pageNumber)
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await _renderingService.RenderPageAsync(
            document,
            pageNumber,
            ThumbnailZoom,
            ThumbnailDpi);

        _logger.LogInformation(
            "Thumbnail rendered in {RenderTimeMs}ms for page {PageNumber}",
            stopwatch.ElapsedMilliseconds,
            pageNumber);

        return result;
    }
}
```

### Caching Layer

#### LruCache<TKey, TValue>
Generic LRU (Least Recently Used) cache with automatic disposal:

```csharp
public sealed class LruCache<TKey, TValue> : IDisposable
    where TKey : notnull
    where TValue : IDisposable
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new();

    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
            value = default;
            return false;
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.Count >= _capacity)
            {
                // Evict least recently used item
                var lru = _lruList.Last!;
                _cache.Remove(lru.Value.Key);
                _lruList.RemoveLast();
                lru.Value.Value.Dispose();
            }

            var node = _lruList.AddFirst(new CacheItem(key, value));
            _cache[key] = node;
        }
    }
}
```

**Features**:
- O(1) access time using Dictionary + LinkedList
- Thread-safe with lock-based synchronization
- Automatic disposal of evicted items
- Capacity: 100 items (configurable)

**Usage in Thumbnails**:
```csharp
private readonly LruCache<int, DisposableBitmapImage> _cache;

// Check cache before rendering
if (_cache.TryGet(pageNumber, out var cachedImage))
{
    item.Thumbnail = cachedImage.Image;
    return;
}

// Render and cache
var bitmap = await RenderThumbnailAsync(pageNumber);
_cache.Add(pageNumber, new DisposableBitmapImage(bitmap));
```

### ViewModel Layer

#### ThumbnailsViewModel
Manages thumbnail state, loading, and navigation:

```csharp
public partial class ThumbnailsViewModel : ObservableObject, IDisposable
{
    public ObservableCollection<ThumbnailItem> Thumbnails { get; }

    [ObservableProperty]
    private int _selectedPageNumber = 1;

    public async Task LoadThumbnailsAsync(PdfDocument document)
    {
        // Create thumbnail items for all pages
        for (int i = 1; i <= document.PageCount; i++)
        {
            Thumbnails.Add(new ThumbnailItem(i));
        }

        // Load first 20 thumbnails with priority
        await LoadPriorityThumbnailsAsync(1, Math.Min(20, document.PageCount));
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToPage))]
    private void NavigateToPage(int pageNumber)
    {
        SelectedPageNumber = pageNumber;

        // Update selection state
        foreach (var item in Thumbnails)
        {
            item.IsSelected = item.PageNumber == pageNumber;
        }

        // Send message to PdfViewerViewModel
        WeakReferenceMessenger.Default.Send(
            new NavigateToPageMessage(pageNumber));
    }
}
```

**Key Features**:
- `LoadThumbnailsAsync`: Creates thumbnail items and loads initial visible thumbnails
- `NavigateToPage`: Sends navigation message and updates selection state
- `UpdateSelectedPage`: Updates selection from external navigation (prevents loops)
- `LoadVisibleThumbnailsAsync`: Lazy loads thumbnails as user scrolls

#### ThumbnailItem Model
Observable model for individual thumbnail state:

```csharp
public partial class ThumbnailItem : ObservableObject
{
    public int PageNumber { get; }

    [ObservableProperty]
    private BitmapImage? _thumbnail;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSelected;
}
```

### View Layer

#### ThumbnailsSidebar UserControl
WinUI 3 control displaying the thumbnail list:

**XAML Structure**:
```xml
<UserControl>
    <ScrollViewer ViewChanged="ThumbnailsScrollViewer_ViewChanged">
        <ItemsRepeater ItemsSource="{x:Bind ViewModel.Thumbnails}">
            <ItemTemplate>
                <Button Click="ThumbnailButton_Click">
                    <Grid Width="150" Height="220">
                        <!-- Selected border -->
                        <Border BorderBrush="AccentFillColorDefaultBrush"
                                BorderThickness="3"
                                Visibility="{x:Bind IsSelected}"/>

                        <!-- Thumbnail image -->
                        <Image Source="{x:Bind Thumbnail}"
                               Visibility="{x:Bind Thumbnail, Converter=NullToVisibility}"/>

                        <!-- Loading placeholder -->
                        <ProgressRing IsActive="{x:Bind IsLoading}"
                                      Visibility="{x:Bind IsLoading, Converter=BoolToVisibility}"/>

                        <!-- Page number -->
                        <TextBlock Text="{x:Bind PageNumber}"/>
                    </Grid>
                </Button>
            </ItemTemplate>
        </ItemsRepeater>
    </ScrollViewer>
</UserControl>
```

**Key Implementation Details**:
- `ItemsRepeater`: Used for virtualization (better performance than ListView)
- `ScrollViewer.ViewChanged`: Triggers lazy loading when scrolling
- `AutomationProperties.Name`: Set to "Page {N} thumbnail" for screen readers
- `KeyboardAccelerator`: Enter/Space keys trigger navigation
- `FocusVisualPrimaryBrush`: Accent color border for keyboard focus

### Messaging and Synchronization

#### NavigateToPageMessage
Message sent when user clicks a thumbnail:

```csharp
public record NavigateToPageMessage(int PageNumber);

// Sender (ThumbnailsViewModel)
WeakReferenceMessenger.Default.Send(new NavigateToPageMessage(pageNumber));

// Receiver (PdfViewerViewModel)
WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this, (r, m) =>
{
    CurrentPageNumber = m.PageNumber;
});
```

#### Bidirectional Synchronization
- **Thumbnail → Viewer**: Click thumbnail sends `NavigateToPageMessage`
- **Viewer → Thumbnail**: `PdfViewerViewModel.CurrentPageNumber` change calls `ThumbnailsViewModel.UpdateSelectedPage`
- **Loop Prevention**: `UpdateSelectedPage` checks if page already selected before updating

## Performance Characteristics

### Rendering Performance
- **Target P99 Latency**: < 200ms per thumbnail
- **Actual Performance**: Varies by page complexity, typically 50-150ms
- **Concurrent Rendering**: Limited to 4 simultaneous renders via `SemaphoreSlim`
- **Priority Loading**: Current page ± 5 pages loaded first

### Memory Management
- **Cache Capacity**: 100 thumbnails (configurable)
- **Target Memory**: < 50MB for 100 pages
- **Estimated Size**: ~120KB per thumbnail (150x200 pixels, 4 bytes per pixel)
- **Automatic Eviction**: LRU cache evicts oldest items when capacity exceeded
- **Disposal**: Evicted items automatically disposed

### Lazy Loading Strategy
1. **Initial Load**: First 20 thumbnails rendered on document open
2. **Scroll Detection**: `ScrollViewer.ViewChanged` calculates visible range
3. **Buffer Zone**: Loads items 2 above and 3 below viewport
4. **Priority Loading**: Pages near current selection loaded before distant pages
5. **Cache Hit**: Already cached thumbnails reused instantly

## Accessibility Features

### Screen Reader Support
- **Thumbnail Names**: Each thumbnail announced as "Page {N} thumbnail"
- **Navigation Announcements**: Page navigation announced to screen readers
- **Sidebar Toggle**: Visibility changes announced via `AutomationPeer`
- **Loading State**: Screen readers informed when thumbnails loading

### Keyboard Navigation
- **Tab Order**: Logical top-to-bottom through thumbnails
- **Arrow Keys**: Up/Down navigation between adjacent thumbnails
- **Enter/Space**: Activate focused thumbnail to navigate
- **Focus Indicators**: Visible 3px accent border around focused thumbnail
- **Skip Navigation**: Can tab past sidebar to main content

### Visual Focus Indicators
```xml
<Button FocusVisualPrimaryBrush="{ThemeResource AccentFillColorDefaultBrush}"
        FocusVisualPrimaryThickness="3"
        FocusVisualSecondaryBrush="Transparent">
```

## Integration with PdfViewerPage

The thumbnails sidebar is integrated into the main PDF viewer layout:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200" /> <!-- Thumbnails -->
        <ColumnDefinition Width="*" />   <!-- Content -->
    </Grid.ColumnDefinitions>

    <controls:ThumbnailsSidebar Grid.Column="0"
                                Visibility="{x:Bind ViewModel.IsSidebarVisible}" />

    <Grid Grid.Column="1">
        <!-- PDF Viewer Content -->
    </Grid>
</Grid>
```

### Toggle Sidebar
```csharp
[ObservableProperty]
private bool _isSidebarVisible = true;

[RelayCommand]
private void ToggleSidebar()
{
    IsSidebarVisible = !IsSidebarVisible;

    // Announce to screen readers
    var message = IsSidebarVisible
        ? "Thumbnails sidebar shown"
        : "Thumbnails sidebar hidden";
    WeakReferenceMessenger.Default.Send(
        new AccessibilityNotificationMessage(message));
}
```

## Testing

### Unit Tests
- `ThumbnailsViewModelTests`: ViewModel logic, navigation, selection state
- `LruCacheTests`: Cache operations, eviction, disposal, thread safety
- `ThumbnailRenderingServiceTests`: Service calls, parameter validation

### Integration Tests
- `ThumbnailsIntegrationTests`: Full workflow from loading to navigation
- Document loading creates thumbnails for all pages
- Thumbnail click sends navigation message
- Main viewer navigation updates thumbnail selection
- Cache verification (no re-rendering of cached items)
- Concurrent rendering limit enforcement
- Navigation disabled during loading state
- Resource cleanup on disposal

### Performance Tests
- `ThumbnailBenchmarks`: P99 latency benchmarks with BenchmarkDotNet
- Memory usage monitoring for 100-page documents
- Concurrent rendering performance under load

## Code Examples

### Basic Usage

```csharp
// In PdfViewerViewModel constructor
_thumbnailsViewModel = serviceProvider.GetRequiredService<ThumbnailsViewModel>();

// When document loaded
await _thumbnailsViewModel.LoadThumbnailsAsync(document);

// Subscribe to navigation messages
WeakReferenceMessenger.Default.Register<NavigateToPageMessage>(this,
    (r, m) => CurrentPageNumber = m.PageNumber);
```

### Synchronizing Selection

```csharp
partial void OnCurrentPageNumberChanged(int value)
{
    // Update thumbnail selection when viewer page changes
    _thumbnailsViewModel.UpdateSelectedPage(value);
}
```

### Custom Cache Capacity

```csharp
// Adjust cache capacity based on available memory
var cache = new LruCache<int, DisposableBitmapImage>(
    capacity: availableMemoryMB > 1024 ? 200 : 100);
```

## Future Enhancements

The following features are planned for future releases:

1. **Thumbnail Context Menu**
   - Delete page
   - Rotate page
   - Extract page
   - Insert blank page

2. **Drag-and-Drop Reordering**
   - Drag thumbnail to reorder pages
   - Visual feedback during drag
   - Undo/redo support

3. **Annotation Indicators**
   - Show badge on thumbnails with annotations
   - Click to jump to first annotation
   - Filter to show only annotated pages

4. **Search Integration**
   - Highlight thumbnails with search matches
   - Click highlighted thumbnail to view matches
   - Match count badge on thumbnails

5. **Thumbnail Export**
   - Export thumbnails as images
   - Batch export all pages
   - Custom resolution settings

## Troubleshooting

### Thumbnails Not Loading
- **Check Document**: Ensure PDF loaded successfully
- **Check Logs**: Look for rendering errors in diagnostics
- **Memory**: Verify sufficient memory available (< 50MB needed)
- **PDFium**: Ensure PDFium binaries deployed correctly

### Slow Thumbnail Rendering
- **Page Complexity**: Complex pages with many vectors render slower
- **Concurrent Limit**: Only 4 thumbnails render simultaneously
- **DPI Setting**: Verify using 48 DPI (not higher resolution)
- **Profiling**: Check logs for render times > 200ms

### Memory Issues
- **Cache Capacity**: Reduce capacity from 100 to 50
- **Large Documents**: For 1000+ page docs, consider on-demand rendering only
- **Disposal**: Ensure ViewModels properly disposed when closing documents
- **Monitoring**: Use diagnostics panel to track memory usage

### Keyboard Navigation Not Working
- **Focus**: Ensure thumbnail has keyboard focus (Tab to sidebar)
- **Loaded State**: Some thumbnails may not be realized until scrolled into view
- **Accessibility**: Verify AutomationProperties set correctly
- **Testing**: Use Narrator to verify screen reader announcements

## Performance Tips

1. **Initial Load**: Only first 20 thumbnails loaded automatically
2. **Lazy Loading**: Scroll to load more thumbnails on demand
3. **Priority**: Pages near current selection load first
4. **Cache**: Cached thumbnails reused instantly when revisiting pages
5. **Memory**: Close large documents to free memory if experiencing issues
6. **Concurrency**: 4 concurrent renders optimal for most systems
