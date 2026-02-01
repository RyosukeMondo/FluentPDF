# Phase 3B to Phase 4 Handoff Document

**From**: feature-porter-2 (Phase 3B - UI Controls & View Modes)
**To**: Next agent (Phase 4 - Build Fixes & Integration)
**Date**: 2026-02-02

## What Was Completed

### ✅ Controls Ported (971 lines)
1. **ContinuousScrollViewer** - Virtual scrolling PDF viewer
2. **TwoPageViewer** - Side-by-side book layout
3. **ShimmerPlaceholder** - Loading animation
4. **ThumbnailsSidebar** - Enhanced with lazy loading & keyboard nav

### ✅ Verified Working
- GlassPanel (liquid glass effects)
- Theme system (Dark/Light switching)
- All AXAML resource dictionaries

## What Needs Fixing (Priority Order)

### 🔴 CRITICAL: Build Errors (38 errors)

#### 1. Missing AnnotationViewModel Property (10 errors)
**File**: `src/FluentPDF.Avalonia/Views/PdfViewerPage.axaml.cs`
**Lines**: 106, 116, 118, 125, 127, 134, 136, 159, 314, 320
**Issue**: `PdfViewerViewModel` doesn't have `AnnotationViewModel` property
**Fix**: Add property to PdfViewerViewModel or inject separately

```csharp
// Add to PdfViewerViewModel
public AnnotationViewModel? AnnotationViewModel { get; set; }
```

#### 2. Missing AnnotationTool Enum (6 errors)
**File**: `src/FluentPDF.Avalonia/Views/PdfViewerPage.axaml.cs`
**Lines**: 159, 160, 325, 326, 327, 328, 329, 330
**Issue**: `AnnotationTool` enum not in scope
**Fix**: Add using directive or move enum to Core

```csharp
using FluentPDF.Core.Models; // If AnnotationTool is here
// OR
using FluentPDF.Avalonia.Models; // If it's Avalonia-specific
```

#### 3. Missing ViewModels (7 errors)
**File**: `src/FluentPDF.Avalonia/App.axaml.cs`
**Lines**: 168, 170, 171, 172, 174, 175, 176, 177
**Issue**: These ViewModels don't exist:
- ConversionViewModel
- FormFieldViewModel
- AnnotationViewModel (duplicate of #1)
- SettingsViewModel
- ImageInsertionViewModel
- WatermarkViewModel
- DiagnosticsPanelViewModel
- LogViewerViewModel

**Fix**: Either:
- Create stub ViewModels temporarily
- Comment out registrations
- Port from WinUI (if they exist there)

#### 4. GuiStateEndpoints PixelSize Issues (4 errors)
**File**: `src/FluentPDF.Avalonia/Api/Endpoints/GuiStateEndpoints.cs`
**Lines**: 70, 71, 145, 146
**Issue**: `object.PixelSize` doesn't exist
**Fix**: Cast to proper Avalonia type

```csharp
// Before
width = window.PixelSize.Width;

// After
width = ((Window)window).ClientSize.Width;
```

#### 5. TabViewModel Missing FileName (2 errors)
**File**: `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs`
**Lines**: 405, 553
**Issue**: `TabViewModel` doesn't have `FileName` property
**Fix**: Add property or use different accessor

```csharp
// Add to TabViewModel
public string? FileName { get; set; }
```

#### 6. PdfViewerViewModel Missing SaveAsCommand (1 error)
**File**: `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs`
**Line**: 611
**Issue**: Command doesn't exist
**Fix**: Add command to ViewModel

```csharp
// Add to PdfViewerViewModel
public ICommand SaveAsCommand { get; }
```

#### 7. PresentationViewModel Cast Issues (2 errors)
**File**: `src/FluentPDF.Avalonia/ViewModels/PresentationViewModel.cs`
**Lines**: 170, 186
**Issue**: Can't cast `object` to `Bitmap`
**Fix**: Explicit cast

```csharp
// Before
bitmap = result.Value;

// After
bitmap = (Bitmap)result.Value;
```

#### 8. ShimmerPlaceholder Rectangle Type (1 error)
**File**: `src/FluentPDF.Avalonia/Controls/ShimmerPlaceholder.axaml.cs`
**Line**: 34
**Issue**: Missing `using` for Rectangle shape
**Fix**: Add using directive

```csharp
using Avalonia.Controls.Shapes;
```

#### 9. ShimmerPlaceholder Animation Dispose (1 error)
**File**: `src/FluentPDF.Avalonia/Controls/ShimmerPlaceholder.axaml.cs`
**Line**: 73
**Issue**: `Animation` doesn't have `Dispose()`
**Fix**: Remove dispose call or use cancellation token

```csharp
// Before
_shimmerAnimation?.Dispose();

// After
// Just set to null, Avalonia animations clean up automatically
_shimmerAnimation = null;
```

#### 10. Async Method Warning (1 warning as error)
**File**: `src/FluentPDF.Avalonia/Views/PdfViewerPage.axaml.cs`
**Line**: 201
**Issue**: Async method with no await
**Fix**: Add await or remove async

### 🟡 MEDIUM: Missing Features

1. **Context Menus**: ThumbnailsSidebar has no right-click menus
2. **Drag-and-Drop**: Can't reorder pages in thumbnails
3. **Text Selection**: Pointer events not wired in ContinuousScrollViewer
4. **BookmarksPanel**: Minimal implementation (needs enhancement)

### 🟢 LOW: Document Dialogs (Tasks 3.5-3.6)

Port these from WinUI:
- MergeDialog
- SplitDialog
- RotateDialog
- DeletePagesDialog
- FormFieldControl
- WatermarkDialog
- StampGallery

## How to Start Phase 4

### Step 1: Fix Build Errors (2-3 hours)

```bash
# 1. Fix ShimmerPlaceholder (easiest)
cd src/FluentPDF.Avalonia/Controls
# Add: using Avalonia.Controls.Shapes;
# Remove: _shimmerAnimation?.Dispose();

# 2. Fix PresentationViewModel casts
cd ../ViewModels
# Add explicit (Bitmap) casts

# 3. Comment out missing ViewModels temporarily
cd ..
# In App.axaml.cs, comment out lines 168-177

# 4. Fix GuiStateEndpoints
cd Api/Endpoints
# Cast window to (Window) before accessing ClientSize

# 5. Add missing ViewModel properties
# This is the bulk of the work - see details above
```

### Step 2: Test New Controls (1 hour)

```bash
# Build Avalonia project
dotnet build src/FluentPDF.Avalonia

# Run manual tests:
# - Load multi-page PDF
# - Test continuous scroll
# - Test two-page view
# - Test thumbnails sidebar
# - Switch themes
```

### Step 3: Add Missing Features (4-6 hours)

Priority order:
1. Wire up text selection in ContinuousScrollViewer
2. Add context menus to ThumbnailsSidebar
3. Implement drag-and-drop page reordering
4. Enhance BookmarksPanel

### Step 4: Port Dialogs (6-8 hours)

Use pattern from completed controls:
1. Create AXAML view
2. Translate WinUI → Avalonia APIs
3. Create code-behind
4. Wire up ViewModels
5. Test integration

## Files to Review

### New Controls (Study These)
- `src/FluentPDF.Avalonia/Controls/ContinuousScrollViewer.axaml.cs` - Pattern for view modes
- `src/FluentPDF.Avalonia/Controls/ThumbnailsSidebar.axaml.cs` - Pattern for keyboard nav
- `src/FluentPDF.Avalonia/Controls/ShimmerPlaceholder.axaml.cs` - Pattern for animations

### Files with Errors (Fix These)
- `src/FluentPDF.Avalonia/Views/PdfViewerPage.axaml.cs` (10 errors)
- `src/FluentPDF.Avalonia/App.axaml.cs` (7 errors)
- `src/FluentPDF.Avalonia/Api/Endpoints/GuiStateEndpoints.cs` (4 errors)
- `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs` (3 errors)
- `src/FluentPDF.Avalonia/ViewModels/PresentationViewModel.cs` (2 errors)
- `src/FluentPDF.Avalonia/Controls/ShimmerPlaceholder.axaml.cs` (2 errors)

### WinUI Source (Port From These)
- `src/FluentPDF.App/Controls/` - All WinUI controls
- `src/FluentPDF.App/ViewModels/` - Missing ViewModels
- `src/FluentPDF.App/Views/` - Dialog implementations

## API Translation Quick Reference

```csharp
// WinUI → Avalonia

// Controls
ItemsRepeater → ItemsControl
ProgressRing → ProgressBar (IsIndeterminate=True)
MenuFlyout → ContextMenu

// Properties
Visibility.Visible/Collapsed → IsVisible = true/false
Element.Visibility → Element.IsVisible

// Events
Loaded/Unloaded → AttachedToVisualTree/DetachedFromVisualTree
ViewChanged → ScrollChanged

// Threading
DispatcherQueue.TryEnqueue() → Dispatcher.UIThread.InvokeAsync()

// Images
BitmapImage → Bitmap
SetSourceAsync(stream) → new Bitmap(stream)

// Animation
Storyboard → Animation
DoubleAnimation → KeyFrame with setters
RepeatBehavior.Forever → IterationCount.Infinite

// Drag & Drop
DragStarting → PointerPressed + PointerMoved
DragOver → DragOver (same event name but different args)
Drop → Drop (same)
```

## Testing Checklist

After fixes, verify:
- [ ] Clean build (0 errors, 0 warnings)
- [ ] Continuous scroll loads only visible pages
- [ ] Two-page view displays correctly
- [ ] Thumbnails sidebar lazy loads on scroll
- [ ] Keyboard shortcuts work (Delete, Ctrl+R, Ctrl+A)
- [ ] Theme switching updates all controls
- [ ] Shimmer animation plays during loading
- [ ] Page navigation works in all view modes
- [ ] Zoom updates re-render pages

## Questions? Check These

1. **API differences**: See `design.md` section "Technical Details"
2. **Build issues**: See `PHASE_3B_IMPLEMENTATION_SUMMARY.md` section "Build Status"
3. **Missing features**: See `tasks.md` Tasks 3.5-3.6
4. **WinUI patterns**: Look at `src/FluentPDF.App/Controls/` for reference

## Success Criteria for Phase 4

- ✅ Zero build errors
- ✅ Zero build warnings
- ✅ All new controls tested manually
- ✅ Text selection working
- ✅ Context menus working
- ✅ At least 2 dialogs ported (Merge + Split recommended)
- ✅ Integration tests passing

## Estimated Effort

| Task | Effort | Priority |
|------|--------|----------|
| Fix build errors | 2-3 hours | 🔴 Critical |
| Test new controls | 1 hour | 🔴 Critical |
| Wire text selection | 2-3 hours | 🟡 High |
| Add context menus | 2 hours | 🟡 High |
| Implement drag-drop | 4 hours | 🟡 Medium |
| Enhance BookmarksPanel | 3 hours | 🟡 Medium |
| Port 2 dialogs | 6 hours | 🟢 Low |
| **Total** | **20-24 hours** | |

## Contact

If you need clarification on any Phase 3B work:
- Read `PHASE_3B_IMPLEMENTATION_SUMMARY.md` first
- Check WinUI source files for original implementation
- Review Avalonia docs for API differences

**Good luck with Phase 4!** 🚀
