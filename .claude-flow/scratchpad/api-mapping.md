# WinUI 3 → Avalonia API Translation Table

**Analysis Date:** 2026-02-01
**Analyst:** inventory-analyst agent
**Purpose:** Complete mapping of WinUI 3 APIs to Avalonia equivalents

---

## Table of Contents
1. [UI Controls](#ui-controls)
2. [Layout & Containers](#layout--containers)
3. [Data Binding](#data-binding)
4. [Events](#events)
5. [Imaging & Graphics](#imaging--graphics)
6. [Composition & Animations](#composition--animations)
7. [Threading & Dispatch](#threading--dispatch)
8. [File Pickers & Dialogs](#file-pickers--dialogs)
9. [Clipboard & Data Transfer](#clipboard--data-transfer)
10. [Input & Keyboard](#input--keyboard)
11. [Platform-Specific APIs](#platform-specific-apis)
12. [Styling & Theming](#styling--theming)

---

## UI Controls

### Basic Controls

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Controls.Button` | `Avalonia.Controls.Button` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.TextBox` | `Avalonia.Controls.TextBox` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.TextBlock` | `Avalonia.Controls.TextBlock` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.CheckBox` | `Avalonia.Controls.CheckBox` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.RadioButton` | `Avalonia.Controls.RadioButton` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ComboBox` | `Avalonia.Controls.ComboBox` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ListBox` | `Avalonia.Controls.ListBox` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ListView` | `Avalonia.Controls.ListBox` | Use ListBox + styling |
| `Microsoft.UI.Xaml.Controls.Slider` | `Avalonia.Controls.Slider` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ProgressBar` | `Avalonia.Controls.ProgressBar` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ToggleButton` | `Avalonia.Controls.Primitives.ToggleButton` | **Different namespace** - add `using Avalonia.Controls.Primitives;` |
| `Microsoft.UI.Xaml.Controls.ToggleSwitch` | `Avalonia.Controls.ToggleSwitch` | Direct equivalent |

### Advanced Controls

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Controls.TreeView` | `Avalonia.Controls.TreeView` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.TabView` | `Avalonia.Controls.TabControl` | Similar - use TabControl + styling |
| `Microsoft.UI.Xaml.Controls.Frame` | `Avalonia.Controls.ContentControl` | No direct Frame equivalent - use ContentControl for navigation |
| `Microsoft.UI.Xaml.Controls.ItemsRepeater` | `Avalonia.Controls.ItemsRepeater` | Direct equivalent (requires Avalonia.Labs.ItemsRepeater package) |
| `Microsoft.UI.Xaml.Controls.ScrollViewer` | `Avalonia.Controls.ScrollViewer` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.WebView2` | `Avalonia.Controls.WebView` | Different implementation - cross-platform web view |
| `Microsoft.UI.Xaml.Controls.MenuBar` | `Avalonia.Controls.Menu` | Use Menu for menu bars |
| `Microsoft.UI.Xaml.Controls.ColorPicker` | Custom control | No built-in ColorPicker - use 3rd party or create custom |

### User Control Base Classes

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Controls.UserControl` | `Avalonia.Controls.UserControl` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.Page` | `Avalonia.Controls.UserControl` | No Page concept - use UserControl |
| `Microsoft.UI.Xaml.Window` | `Avalonia.Controls.Window` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.ContentControl` | `Avalonia.Controls.ContentControl` | Direct equivalent |

---

## Layout & Containers

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Controls.Grid` | `Avalonia.Controls.Grid` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.StackPanel` | `Avalonia.Controls.StackPanel` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.Canvas` | `Avalonia.Controls.Canvas` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.Border` | `Avalonia.Controls.Border` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.Viewbox` | `Avalonia.Controls.Viewbox` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.WrapPanel` | `Avalonia.Controls.WrapPanel` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.RelativePanel` | `Avalonia.Controls.RelativePanel` | Direct equivalent |
| `Microsoft.UI.Xaml.Controls.SplitView` | `Avalonia.Controls.SplitView` | Direct equivalent |

---

## Data Binding

### Dependency Properties

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `DependencyProperty` | `StyledProperty<T>` or `DirectProperty<TOwner, TValue>` | **Different system** - Avalonia uses strongly-typed properties |
| `DependencyProperty.Register()` | `AvaloniaProperty.Register<TOwner, TValue>()` | Register with generic types |
| `GetValue(DependencyProperty)` | `GetValue(AvaloniaProperty)` | Similar pattern |
| `SetValue(DependencyProperty, value)` | `SetValue(AvaloniaProperty, value)` | Similar pattern |
| `PropertyMetadata` | `PropertyMetadata<T>` | Generic version |

**Example Conversion:**

WinUI 3:
```csharp
public static readonly DependencyProperty ViewerViewModelProperty =
    DependencyProperty.Register(
        nameof(ViewerViewModel),
        typeof(PdfViewerViewModel),
        typeof(PdfViewerControl),
        new PropertyMetadata(null, OnViewerViewModelChanged));

public PdfViewerViewModel? ViewerViewModel
{
    get => (PdfViewerViewModel?)GetValue(ViewerViewModelProperty);
    set => SetValue(ViewerViewModelProperty, value);
}
```

Avalonia:
```csharp
public static readonly StyledProperty<PdfViewerViewModel?> ViewerViewModelProperty =
    AvaloniaProperty.Register<PdfViewerControl, PdfViewerViewModel?>(
        nameof(ViewerViewModel),
        defaultValue: null,
        notifying: OnViewerViewModelChanged);

public PdfViewerViewModel? ViewerViewModel
{
    get => GetValue(ViewerViewModelProperty);
    set => SetValue(ViewerViewModelProperty, value);
}
```

### Binding

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `{Binding Property}` | `{Binding Property}` | Same syntax in XAML |
| `{x:Bind Property}` | Not supported | Use `{Binding}` or `{CompiledBinding}` (Avalonia 11+) |
| `Mode=OneWay/TwoWay` | `Mode=OneWay/TwoWay` | Same |
| `UpdateSourceTrigger` | Not needed | Avalonia auto-detects |

---

## Events

### Event Arguments

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `RoutedEventArgs` | `Avalonia.Interactivity.RoutedEventArgs` | Direct equivalent |
| `PointerEventArgs` | `Avalonia.Input.PointerEventArgs` | Direct equivalent |
| `KeyRoutedEventArgs` | `Avalonia.Input.KeyEventArgs` | **Different name** |
| `ManipulationDeltaEventArgs` | `Avalonia.Input.PointerEventArgs` | No direct equivalent - use PointerMoved/Delta |
| `Tapped` | `PointerPressed` or `Tapped` | Avalonia has both |

### Event Handlers

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Loaded` event | `Loaded` event | Same |
| `Unloaded` event | `Unloaded` event | Same |
| `SizeChanged` event | `SizeChanged` event | Same |
| `PointerPressed` | `PointerPressed` | Same |
| `PointerMoved` | `PointerMoved` | Same |
| `PointerReleased` | `PointerReleased` | Same |
| `KeyDown` | `KeyDown` | Same |
| `KeyUp` | `KeyUp` | Same |

---

## Imaging & Graphics

### Image Types

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Media.ImageSource` | `Avalonia.Media.IImage` | **Different base type** |
| `Microsoft.UI.Xaml.Media.Imaging.BitmapImage` | `Avalonia.Media.Imaging.Bitmap` | **Different API** - use `new Bitmap(stream)` |
| `Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap` | `Avalonia.Media.Imaging.WriteableBitmap` | Direct equivalent |
| `Windows.Graphics.Imaging.SoftwareBitmap` | Not needed | Use `Avalonia.Media.Imaging.Bitmap` |

**Critical Change - Rendering Strategy:**

WinUI 3:
```csharp
public async Task<ImageSource?> TryRenderAsync(Stream pngStream, RenderContext context)
{
    // Uses ImageSharp to decode, then WriteableBitmap
    using var image = await SixLabors.ImageSharp.Image.LoadAsync<Bgra32>(pngStream);
    var pixelData = new byte[image.Width * image.Height * 4];
    image.CopyPixelDataTo(pixelData);

    // MUST dispatch to UI thread for WriteableBitmap
    var tcs = new TaskCompletionSource<WriteableBitmap?>();
    App.MainWindow.DispatcherQueue.TryEnqueue(() => {
        var bitmap = new WriteableBitmap(width, height);
        using (var buffer = bitmap.PixelBuffer.AsStream())
        {
            buffer.Write(pixelData, 0, pixelData.Length);
        }
        tcs.SetResult(bitmap);
    });
    return await tcs.Task;
}
```

Avalonia:
```csharp
public async Task<IImage?> TryRenderAsync(Stream pngStream, RenderContext context)
{
    // Avalonia.Media.Imaging.Bitmap can decode directly, no UI thread required
    pngStream.Seek(0, SeekOrigin.Begin);
    var bitmap = await Task.Run(() => new Bitmap(pngStream));
    return bitmap;
}
```

**Key Difference:** Avalonia Bitmap construction is thread-safe, no dispatcher required.

### Brushes

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Xaml.Media.SolidColorBrush` | `Avalonia.Media.SolidColorBrush` | Direct equivalent |
| `Microsoft.UI.Xaml.Media.LinearGradientBrush` | `Avalonia.Media.LinearGradientBrush` | Direct equivalent |
| `Microsoft.UI.Xaml.Media.RadialGradientBrush` | `Avalonia.Media.RadialGradientBrush` | Direct equivalent |
| `Microsoft.UI.Xaml.Media.ImageBrush` | `Avalonia.Media.ImageBrush` | Direct equivalent |
| `Microsoft.UI.Xaml.Media.AcrylicBrush` | `Avalonia.Media.ExperimentalAcrylicMaterial` | **Different API** - experimental in Avalonia |

### Colors

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Windows.UI.Color` | `Avalonia.Media.Color` | **Different namespace** |
| `Windows.UI.Color.FromArgb(a, r, g, b)` | `Avalonia.Media.Color.FromArgb(a, r, g, b)` | Same method |

**Example Conversion:**

WinUI 3:
```csharp
using Windows.UI;
var color = Color.FromArgb(255, 128, 64, 32);
```

Avalonia:
```csharp
using Avalonia.Media;
var color = Color.FromArgb(255, 128, 64, 32);
```

---

## Composition & Animations

### WinUI 3 Composition API

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Microsoft.UI.Composition.Compositor` | Not directly available | Use Avalonia Animations or RenderTransform |
| `Microsoft.UI.Composition.CompositionAnimation` | `Avalonia.Animation.Animation` | Different API |
| `ElementCompositionPreview.GetElementVisual()` | Not available | Use RenderTransform or Transitions |
| `CompositionPropertySet` | Not available | Use attached properties |

**WinUI 3 AnimationService (GPU-accelerated):**
```csharp
public async Task AnimatePageTransitionAsync(UIElement target, PageTransitionDirection direction, CancellationToken ct)
{
    var visual = ElementCompositionPreview.GetElementVisual(target);
    var compositor = visual.Compositor;

    var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
    offsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
    offsetAnimation.InsertKeyFrame(0.0f, new Vector3(direction == PageTransitionDirection.Forward ? 100 : -100, 0, 0));
    offsetAnimation.InsertKeyFrame(1.0f, Vector3.Zero);

    visual.StartAnimation("Offset", offsetAnimation);
    await Task.Delay(300, ct);
}
```

**Avalonia Equivalent (RenderTransform):**
```csharp
public async Task AnimatePageTransitionAsync(Control target, PageTransitionDirection direction, CancellationToken ct)
{
    var offsetX = direction == PageTransitionDirection.Forward ? 100 : -100;
    target.RenderTransform = new TranslateTransform(offsetX, 0);

    var animation = new Animation
    {
        Duration = TimeSpan.FromMilliseconds(300),
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(TranslateTransform.XProperty, offsetX) }
            },
            new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(TranslateTransform.XProperty, 0.0) }
            }
        }
    };

    await animation.RunAsync(target, ct);
}
```

**Note:** WinUI 3 Composition API is GPU-accelerated. Avalonia animations are also GPU-accelerated via Skia.

### Transitions

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `EntranceThemeTransition` | `Avalonia.Animation.Transitions` | Use `Transitions` collection |
| `ContentThemeTransition` | `Avalonia.Animation.CrossFade` | Use CrossFade transition |

---

## Threading & Dispatch

### Dispatcher

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `DispatcherQueue` | `Avalonia.Threading.Dispatcher` | **Different API** |
| `DispatcherQueue.TryEnqueue(action)` | `Dispatcher.UIThread.Post(action)` or `Dispatcher.UIThread.InvokeAsync(action)` | **Critical change** |
| `DispatcherQueuePriority` | `DispatcherPriority` | Similar enum |

**Example Conversion:**

WinUI 3:
```csharp
App.MainWindow.DispatcherQueue.TryEnqueue(() =>
{
    // UI update code
    textBlock.Text = "Updated";
});

// Async version
await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
{
    await SomeAsyncOperation();
});
```

Avalonia:
```csharp
Dispatcher.UIThread.Post(() =>
{
    // UI update code
    textBlock.Text = "Updated";
});

// Async version
await Dispatcher.UIThread.InvokeAsync(async () =>
{
    await SomeAsyncOperation();
});
```

**Note:** Avalonia Bitmap creation is thread-safe, so dispatcher is often not needed for image operations.

---

## File Pickers & Dialogs

### File Pickers

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Windows.Storage.Pickers.FileOpenPicker` | `Avalonia.Controls.OpenFileDialog` | **Different API** |
| `Windows.Storage.Pickers.FileSavePicker` | `Avalonia.Controls.SaveFileDialog` | **Different API** |
| `Windows.Storage.Pickers.FolderPicker` | `Avalonia.Controls.OpenFolderDialog` | **Different API** |

**Example Conversion:**

WinUI 3:
```csharp
var picker = new FileOpenPicker
{
    ViewMode = PickerViewMode.List,
    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
};
picker.FileTypeFilter.Add(".pdf");

var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSingleFileAsync();
if (file != null)
{
    var path = file.Path;
}
```

Avalonia:
```csharp
var dialog = new OpenFileDialog
{
    Title = "Open PDF File",
    Filters = new List<FileDialogFilter>
    {
        new FileDialogFilter { Name = "PDF Files", Extensions = new List<string> { "pdf" } }
    }
};

var result = await dialog.ShowAsync(parentWindow);
if (result != null && result.Length > 0)
{
    var path = result[0];
}
```

**Key Differences:**
- Avalonia dialogs are simpler (no hwnd required)
- Returns string paths directly, not StorageFile objects
- Must pass parent window reference

### Message Dialogs

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `ContentDialog` | Custom UserControl + Window | No built-in ContentDialog - create custom |
| `MessageDialog` | `MessageBoxManager` (3rd party) | Use MessageBox.Avalonia package or custom |

---

## Clipboard & Data Transfer

### Clipboard

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Windows.ApplicationModel.DataTransfer.Clipboard` | `Avalonia.Input.Clipboard` | **Different API** |
| `Clipboard.SetContent(dataPackage)` | `await clipboard.SetTextAsync(text)` | **Async in Avalonia** |
| `DataPackage` | Not needed | Use SetTextAsync/GetTextAsync directly |

**Example Conversion:**

WinUI 3:
```csharp
var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
dataPackage.SetText(selectedText);
Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
```

Avalonia:
```csharp
var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
if (clipboard != null)
{
    await clipboard.SetTextAsync(selectedText);
}
```

### Drag & Drop

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `DragEventArgs` | `Avalonia.Input.DragEventArgs` | Similar |
| `AllowDrop` property | `DragDrop.AllowDrop` attached property | Use `DragDrop.AllowDrop="True"` |
| `DragStarting` event | Custom implementation | No built-in DragStarting - use PointerPressed + DragDrop.DoDragDrop() |

---

## Input & Keyboard

### Keyboard State

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `Windows.System.VirtualKey` | `Avalonia.Input.Key` | **Different enum** |
| `InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)` | `KeyModifiers.Control` (from KeyEventArgs) | **Different approach** |
| `CoreVirtualKeyStates.Down` flag | Check `e.KeyModifiers.HasFlag(KeyModifiers.Control)` | **Different pattern** |

**Example Conversion:**

WinUI 3:
```csharp
private void OnKeyDown(object sender, KeyRoutedEventArgs e)
{
    var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource
        .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
        .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    if (e.Key == Windows.System.VirtualKey.S && ctrlPressed)
    {
        // Ctrl+S
    }
}
```

Avalonia:
```csharp
private void OnKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
{
    if (e.Key == Avalonia.Input.Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        // Ctrl+S
    }
}
```

### Key Enum Mapping

| WinUI VirtualKey | Avalonia Key | Notes |
|------------------|--------------|-------|
| `VirtualKey.Control` | `Key.LeftCtrl` or `Key.RightCtrl` | Separate left/right in Avalonia |
| `VirtualKey.Shift` | `Key.LeftShift` or `Key.RightShift` | Separate left/right in Avalonia |
| `VirtualKey.Delete` | `Key.Delete` | Same |
| `VirtualKey.A` through `VirtualKey.Z` | `Key.A` through `Key.Z` | Same |
| `VirtualKey.Up/Down/Left/Right` | `Key.Up/Down/Left/Right` | Same |

---

## Platform-Specific APIs

### Windows-Only Features (No Avalonia Equivalent)

| WinUI 3 API | Purpose | Avalonia Alternative |
|-------------|---------|---------------------|
| `Windows.UI.StartScreen.JumpList` | Windows taskbar Jump List | Skip - Windows-only feature |
| `Windows.Storage.ApplicationData` | App settings storage | Use cross-platform settings (Preferences.xaml, JSON file, SQLite) |
| `Windows.UI.ViewManagement.UISettings` | Theme change detection | Use `Application.Current.ActualThemeVariant` |
| `Windows.Storage.StorageFile` | File abstraction | Use `System.IO.FileInfo` or string paths |

### Settings Storage Replacement

WinUI 3:
```csharp
public void SaveSetting(string key, string value)
{
    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
    localSettings.Values[key] = value;
}

public string? GetSetting(string key)
{
    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
    return localSettings.Values[key] as string;
}
```

Avalonia (cross-platform):
```csharp
// Option 1: JSON file
public void SaveSetting(string key, string value)
{
    var settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluentPDF", "settings.json");

    var settings = File.Exists(settingsPath)
        ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(settingsPath))
        : new Dictionary<string, string>();

    settings[key] = value;
    File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings));
}

// Option 2: Use Avalonia.Labs.Preferences (if available)
```

---

## Styling & Theming

### Theme Detection

WinUI 3:
```csharp
private readonly Windows.UI.ViewManagement.UISettings _uiSettings;

_uiSettings = new Windows.UI.ViewManagement.UISettings();
_uiSettings.ColorValuesChanged += OnSystemThemeChanged;

private void OnSystemThemeChanged(Windows.UI.ViewManagement.UISettings sender, object args)
{
    // System theme changed
    var theme = Application.Current.RequestedTheme;
}
```

Avalonia:
```csharp
// Subscribe to theme changes
Application.Current.PropertyChanged += (sender, e) =>
{
    if (e.PropertyName == nameof(Application.ActualThemeVariant))
    {
        var theme = Application.Current.ActualThemeVariant;
        // theme is ThemeVariant.Light or ThemeVariant.Dark
    }
};
```

### Resource Dictionaries

| WinUI 3 | Avalonia | Notes |
|---------|----------|-------|
| `ResourceDictionary` in `App.xaml` | `Styles` in `App.axaml` | Different structure |
| `<ResourceDictionary Source="..."/>` | `<StyleInclude Source="..."/>` | Different tag |
| `ThemeDictionaries` | `<Style Selector="^:light">` or `<Style Selector="^:dark">` | Theme-specific styles |

**Example - App.xaml vs App.axaml:**

WinUI 3 App.xaml:
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Styles/Colors.xaml"/>
        </ResourceDictionary.MergedDictionaries>

        <SolidColorBrush x:Key="PrimaryBrush" Color="#0078D4"/>
    </ResourceDictionary>
</Application.Resources>
```

Avalonia App.axaml:
```xml
<Application.Styles>
    <StyleInclude Source="avares://FluentPDF.Avalonia/Styles/Colors.axaml"/>

    <Style Selector=":is(Control)">
        <Style.Resources>
            <SolidColorBrush x:Key="PrimaryBrush" Color="#0078D4"/>
        </Style.Resources>
    </Style>
</Application.Styles>
```

---

## Common Pitfalls & Solutions

### 1. ToggleButton Missing

**Error:** `CS0246: Type 'ToggleButton' not found`

**Cause:** ToggleButton is in different namespace in Avalonia

**Solution:**
```csharp
// Add this using directive
using Avalonia.Controls.Primitives;
```

---

### 2. ImageSource Return Type

**Error:** Method returns `ImageSource` but expects `IImage`

**Solution:**
```csharp
// WinUI 3
public async Task<ImageSource?> RenderAsync() { ... }

// Avalonia
public async Task<IImage?> RenderAsync() { ... }
```

---

### 3. Dispatcher Thread Blocking

**Error:** UI freezes when using `DispatcherQueue.TryEnqueue()`

**Solution:**
```csharp
// WinUI 3 - sync dispatch
App.MainWindow.DispatcherQueue.TryEnqueue(() => { ... });

// Avalonia - use Post (non-blocking) or InvokeAsync (awaitable)
Dispatcher.UIThread.Post(() => { ... });  // Non-blocking
await Dispatcher.UIThread.InvokeAsync(() => { ... });  // Awaitable
```

---

### 4. Frame Navigation

**Error:** No `Frame.Navigate()` method

**Solution:**
```csharp
// WinUI 3
_frame.Navigate(typeof(PdfViewerPage), parameter);

// Avalonia - use ContentControl
_contentControl.Content = new PdfViewerPage { DataContext = parameter };
```

---

### 5. File Picker HWND Requirement

**Error:** FileOpenPicker requires window handle in WinUI

**Solution:**
```csharp
// WinUI 3 - complex setup
var picker = new FileOpenPicker();
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
var file = await picker.PickSingleFileAsync();

// Avalonia - simpler
var dialog = new OpenFileDialog();
var result = await dialog.ShowAsync(window);
```

---

## Summary of Critical Changes

| Category | WinUI 3 | Avalonia | Migration Complexity |
|----------|---------|----------|---------------------|
| **UI Controls** | Microsoft.UI.Xaml.Controls.* | Avalonia.Controls.* | LOW (mostly 1:1) |
| **Dependency Properties** | DependencyProperty | StyledProperty<T> | MEDIUM (different registration) |
| **Imaging** | BitmapImage, WriteableBitmap | Bitmap (thread-safe) | MEDIUM (different API) |
| **Threading** | DispatcherQueue.TryEnqueue() | Dispatcher.UIThread.Post() | MEDIUM (different pattern) |
| **Composition Animations** | Compositor, Visual | RenderTransform, Animations | HIGH (completely different) |
| **File Pickers** | FileOpenPicker (complex) | OpenFileDialog (simple) | LOW (easier in Avalonia) |
| **Keyboard Input** | VirtualKey, GetKeyStateForCurrentThread() | Key enum, KeyModifiers | MEDIUM (different approach) |
| **Settings Storage** | ApplicationData.LocalSettings | JSON file or SQLite | MEDIUM (cross-platform approach) |
| **Jump List** | JumpList API | Not applicable | N/A (skip feature) |

**Total Migration Effort:** MEDIUM - Most APIs have direct equivalents, but some require architectural changes (animations, dispatcher, file storage).
