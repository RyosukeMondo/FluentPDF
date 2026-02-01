# FluentPDF Avalonia - UI Quick Start Guide

## Overview

FluentPDF Avalonia now has a complete, polished user interface with search, thumbnails, bookmarks, and settings.

## UI Components

### Main Window Layout

```
┌─────────────────────────────────────────────┐
│ File   Tools                                │ Menu Bar
├─────────────────────────────────────────────┤
│ [Open] [◀] [1 / 10] [▶] [-] [100%] [+] ... │ Toolbar
├─────────────────────────────────────────────┤
│ [🔍 Search...] [◀] [▶] [Aa] [Ab|] [✕]      │ Search Panel (Ctrl+F)
├───────┬─────────────────────────────┬───────┤
│       │                             │       │
│ 📄    │       PDF Content           │  🔖   │
│ Page  │                             │ Book- │
│ Thumb │                             │ marks │
│ nails │                             │       │
│       │                             │       │
└───────┴─────────────────────────────┴───────┘
```

## Keyboard Shortcuts

### Document Navigation
- `Ctrl+O` - Open PDF
- `Ctrl+S` - Save PDF
- `Ctrl+W` - Close tab
- `Page Up/Down` - Navigate pages
- `Home/End` - First/Last page

### Viewing
- `Ctrl+Plus` - Zoom in
- `Ctrl+Minus` - Zoom out
- `Ctrl+0` - Reset zoom
- `Ctrl+Shift+Plus` - Rotate right
- `Ctrl+Shift+Minus` - Rotate left

### Search
- `Ctrl+F` - Open search panel
- `F3` - Next match
- `Shift+F3` - Previous match
- `Esc` - Close search panel

### Sidebars
- `F9` - Toggle thumbnails sidebar
- `F10` - Toggle bookmarks sidebar

## Feature Guide

### 1. Search and Replace

**To search:**
1. Press `Ctrl+F` or click 🔍 in toolbar
2. Type search text
3. Use ◀/▶ buttons to navigate matches
4. Match counter shows "5 of 23"

**Options:**
- **Aa** - Case sensitive toggle
- **Ab|** - Whole word toggle

**To replace:**
1. Search finds matches
2. Replace UI appears automatically
3. Enter replacement text
4. Click "Replace" (single) or "Replace All"

### 2. Thumbnails Sidebar

**Features:**
- Click thumbnail to jump to page
- Selected page highlighted with blue border
- Toolbar buttons:
  - ⟲ Rotate left
  - ⟳ Rotate right
  - 🗑 Delete pages

**Selection:**
- Click thumbnail to select page
- Ctrl+Click for multi-select (future)

### 3. Bookmarks Panel

**Features:**
- Tree view of PDF outline
- Click bookmark to jump to section
- Expand/collapse nested bookmarks
- Hover shows page number

**Empty State:**
- Message shows if PDF has no bookmarks
- Panel auto-hides

### 4. Settings Page

**Access:** Tools → Settings

**Sections:**

#### Appearance
- **Theme:** Light, Dark, or System default

#### Rendering
- **Quality:** Auto (recommended), Low, Medium, High, Ultra
  - Auto: Adapts to display DPI and zoom
  - Low: 75 DPI, minimal memory
  - Medium: 96 DPI, standard quality
  - High: 144 DPI, high-DPI displays
  - Ultra: 192+ DPI, 4K displays (⚠️ high memory)

#### Default Settings
- **Default Zoom:** Zoom level for new documents
- **Scroll Mode:** Vertical, Horizontal, Continuous

#### Privacy
- **Telemetry:** Anonymous usage data
- **Crash Reporting:** Automatic error reports

#### Actions
- **Reset to Defaults:** Restore all settings

### 5. Dialogs

**Message Dialog:**
```csharp
// Show information
await MessageDialog.ShowAsync(window, "Success", "File saved", MessageDialogType.Success);

// Show error
await MessageDialog.ShowAsync(window, "Error", "File not found", MessageDialogType.Error);
```

**Confirm Dialog:**
```csharp
bool confirmed = await ConfirmDialog.ShowAsync(window, "Delete", "Are you sure?");
if (confirmed)
{
    // Perform action
}
```

## Toolbar Reference

### File Operations
- **Open** - Open PDF file (Ctrl+O)

### Navigation
- **◀** - Previous page
- **Page Number** - Current page (editable)
- **▶** - Next page

### Zoom
- **−** - Zoom out
- **100%** - Current zoom level
- **+** - Zoom in

### Rotation
- **⟲** - Rotate counter-clockwise
- **⟳** - Rotate clockwise

### View Options
- **🔍** - Toggle search panel
- **📄** - Toggle thumbnails sidebar
- **🔖** - Toggle bookmarks sidebar

## Tips and Tricks

### Performance

**For Large Documents (100+ pages):**
1. Use Low or Medium rendering quality
2. Thumbnails load lazily (scroll to load)
3. Close sidebars when not needed
4. Enable thumbnail cache (default: 50MB)

**For High-DPI Displays:**
1. Use High or Ultra rendering quality
2. Increase cache size in settings (future)
3. Consider memory usage with large PDFs

### Workflow Tips

**Quick Navigation:**
1. Use thumbnails for visual page scanning
2. Use bookmarks for logical section navigation
3. Use search for finding specific text

**Editing Workflow:**
1. Open search panel with Ctrl+F
2. Find and replace text across document
3. Use thumbnails to rotate/delete pages
4. Save with Ctrl+S

### Accessibility

**Screen Reader Support:**
- All buttons have descriptive labels
- Keyboard navigation fully supported
- Focus indicators on all controls

**High Contrast:**
- Uses dynamic theme resources
- Adapts to system high contrast mode
- Clear visual indicators

## Troubleshooting

### Search Not Finding Text
**Problem:** Search returns no matches for visible text

**Solution:** PDF may be image-based (scanned document). Only searchable text is indexed.

### Thumbnails Not Loading
**Problem:** Thumbnails show loading spinner forever

**Solution:**
1. Check if PDF is corrupted
2. Try opening in another viewer
3. Check logs for PDFium errors

### Settings Not Saving
**Problem:** Settings reset on app restart

**Solution:**
1. Check write permissions to app data folder
2. Look for `settings.json` in `%LOCALAPPDATA%/FluentPDF`
3. Check logs for save errors

### High Memory Usage
**Problem:** App using excessive memory

**Solution:**
1. Lower rendering quality (Settings → Rendering)
2. Close unused tabs
3. Reduce thumbnail cache size
4. Avoid Ultra quality for large documents

## Developer Integration

### Using Dialog Windows

```csharp
using FluentPDF.Avalonia.Views;

// Information dialog
await MessageDialog.ShowAsync(
    owner: this,
    title: "Information",
    message: "Document loaded successfully",
    dialogType: MessageDialogType.Information
);

// Warning dialog
await MessageDialog.ShowAsync(
    owner: this,
    title: "Warning",
    message: "This action cannot be undone",
    dialogType: MessageDialogType.Warning
);

// Error dialog
await MessageDialog.ShowAsync(
    owner: this,
    title: "Error",
    message: "Failed to save file",
    dialogType: MessageDialogType.Error
);

// Success dialog
await MessageDialog.ShowAsync(
    owner: this,
    title: "Success",
    message: "Operation completed",
    dialogType: MessageDialogType.Success
);

// Confirmation dialog
bool result = await ConfirmDialog.ShowAsync(
    owner: this,
    title: "Confirm Delete",
    message: "Delete 5 pages?"
);

if (result)
{
    // User clicked Yes
}
else
{
    // User clicked No
}
```

### Accessing ViewModels

```csharp
// From PdfViewerPage code-behind
var viewModel = DataContext as PdfViewerViewModel;

// Access search functionality
viewModel.SearchPanelViewModel.SearchText = "example";
viewModel.SearchPanelViewModel.PerformSearch();

// Access thumbnails
await viewModel.ThumbnailsViewModel.LoadThumbnailsAsync(document);

// Access bookmarks
viewModel.BookmarksViewModel.LoadBookmarksCommand.Execute(document);
```

### Custom Styling

```xml
<!-- Override theme colors in App.axaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <FluentTheme />
        </ResourceDictionary.MergedDictionaries>

        <!-- Custom colors -->
        <SolidColorBrush x:Key="SystemAccentColor">#0078D4</SolidColorBrush>
        <SolidColorBrush x:Key="ApplicationPageBackgroundBrush">#F3F3F3</SolidColorBrush>
    </ResourceDictionary>
</Application.Resources>
```

## File Locations

### Configuration
- Settings: `%LOCALAPPDATA%/FluentPDF/settings.json`
- Recent files: `%LOCALAPPDATA%/FluentPDF/recent.json`
- Logs: `%LOCALAPPDATA%/FluentPDF/logs/`

### Source Code
- Controls: `src/FluentPDF.Avalonia/Controls/`
- Views: `src/FluentPDF.Avalonia/Views/`
- ViewModels: `src/FluentPDF.Avalonia/ViewModels/`
- Converters: `src/FluentPDF.Avalonia/Converters/`

## Support

### Logs
Enable verbose logging:
```bash
dotnet run --project src/FluentPDF.Avalonia -- --verbose
```

### Diagnostics
Display system info:
```bash
dotnet run --project src/FluentPDF.Avalonia -- --diagnostics
```

### Testing
Test rendering without UI:
```bash
dotnet run --project src/FluentPDF.Avalonia -- --test-render "path/to/test.pdf"
```

---

**Quick Start Version:** 1.0
**Last Updated:** January 28, 2026
**For Full Documentation:** See `AVALONIA_UI_IMPLEMENTATION_COMPLETE.md`
