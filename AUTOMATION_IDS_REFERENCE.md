# FluentPDF AutomationId Reference

Complete list of AutomationIds for REST API verification and UI automation testing.

## Main Window Structure

### Window & Grids
- `MainWindow` - Main application window
- `MainGrid` - Root grid
- `TitleBar` - Title bar grid
- `ContentGrid` - Content area grid (row 4)

## Main Toolbar (Row 1)

### AutomationId: `MainToolbar`

#### File Operations
- `OpenFileButton` - Open file dialog

#### Navigation
- `PreviousPageButton` - Navigate to previous page
- `PageNumberBox` - Current page number input (NumberBox)
- `NextPageButton` - Navigate to next page

#### View Controls
- `ToggleThumbnailsButton` - Show/hide thumbnails sidebar
- `ToggleBookmarksButton` - Show/hide bookmarks sidebar
- `ToggleSearchButton` - Show/hide search panel

#### Zoom Controls
- `ZoomOutButton` - Decrease zoom level
- `ZoomLevelComboBox` - Zoom level selector (50%-400%, Fit Width, Fit Page)
- `ZoomInButton` - Increase zoom level
- `ResetZoomButton` - Reset zoom to 100%
- `ViewModeComboBox` - View mode selector (Single Page, Continuous Scroll, Two Pages)

#### Document Operations
- `MergeButton` - Open merge PDFs dialog
- `SplitButton` - Open split PDF dialog
- `OptimizeButton` - Open optimize dialog

#### Image & Media
- `InsertImageButton` - Insert image into PDF
- `WatermarkButton` - Add watermark dialog

#### Conversion
- `ConvertDocxButton` - Convert DOCX to PDF

## Annotation Toolbar (Row 2)

### AutomationId: `AnnotationToolbar`

#### Text Markup Tools
- `HighlightButton` - Highlight text mode
- `UnderlineButton` - Underline text mode
- `StrikethroughButton` - Strikethrough text mode

#### Comment Tools
- `CommentButton` - Add comment/sticky note mode

#### Shape Tools
- `RectangleButton` - Draw rectangle mode
- `CircleButton` - Draw circle mode
- `FreehandButton` - Freehand drawing mode

#### Properties
- `AnnotationColorPicker` - Annotation color picker with alpha

## Search Panel (Row 3)

### AutomationId: `SearchPanel`

#### Search Controls
- `SearchTextBox` - Search query input
- `SearchResultsText` - Match count display (TextBlock)
- `PreviousMatchButton` - Navigate to previous match
- `NextMatchButton` - Navigate to next match
- `CaseSensitiveCheckBox` - Case-sensitive search toggle
- `CloseSearchButton` - Close search panel

## Thumbnails Sidebar (Column 0)

### AutomationId: `ThumbnailsSidebar`

#### Container
- `ThumbnailsGrid` - Sidebar grid container
- `ThumbnailsScrollViewer` - Scrollable thumbnail list
- `ThumbnailsRepeater` - ItemsRepeater for thumbnails
- `ThumbnailsResizeGripper` - Resize gripper (GridSplitter)

#### Individual Thumbnails
- Pattern: `Thumbnail_{PageIndex}Button` (e.g., `Thumbnail_0Button`, `Thumbnail_1Button`)
- Requires: `ThumbnailItem.AutomationId` property in model

## Bookmarks Sidebar (Column 1)

### AutomationId: `BookmarksSidebar`

#### Container
- `BookmarksGrid` - Sidebar grid container
- `BookmarksTreeView` - TreeView for hierarchical bookmarks
- `BookmarksResizeGripper` - Resize gripper (GridSplitter)

#### Individual Bookmarks
- Pattern: Defined by `BookmarkNode.AutomationId` property
- Requires: AutomationId in bookmark data model

## PDF Content Area (Column 2)

### AutomationId: `PdfContentArea`

#### Container
- `ContentArea` - Main content grid
- `PdfScrollViewer` - Zoomable scroll viewer

#### Canvas & Layers
- `PdfCanvas` - Main canvas container
- `PdfPageImage` - PDF page rendering (Image)
- `AnnotationLayer` - Annotation overlay (Canvas)
- `FormFieldLayer` - Form field overlay (Canvas)
- `SelectionLayer` - Text selection overlay (Canvas)

#### Error Display
- `ValidationErrorBar` - Validation error display grid
- `ValidationErrorText` - Error message text (TextBlock)

## Menu Bar

### File Menu
- `FileMenuBarItem` - File menu
- `OpenMenuItem` - Open file
- `SaveMenuItem` - Save file
- `SaveAsMenuItem` - Save as
- `RecentFilesSubMenu` - Recent files submenu
- `ClearRecentFilesMenuItem` - Clear recent files
- `ExitMenuItem` - Exit application

### Tools Menu
- `ToolsMenuBarItem` - Tools menu
- `SettingsMenuItem` - Settings dialog

## REST API Verification Examples

### Check Element Exists
```bash
curl http://localhost:5000/api/ui/element/MainToolbar
```

### Check Element Visible
```bash
curl http://localhost:5000/api/ui/element/AnnotationToolbar/visible
```

### Click Button
```bash
curl -X POST http://localhost:5000/api/ui/element/OpenFileButton/click
```

### Set Text
```bash
curl -X POST http://localhost:5000/api/ui/element/SearchTextBox/set-text \
  -H "Content-Type: application/json" \
  -d '{"text":"search query"}'
```

### Get Element Property
```bash
curl http://localhost:5000/api/ui/element/PageNumberBox/value
```

### Toggle Element
```bash
curl -X POST http://localhost:5000/api/ui/element/ToggleThumbnailsButton/toggle
```

## Keyboard Shortcuts

| Shortcut | AutomationId | Action |
|----------|-------------|--------|
| Ctrl+O | OpenFileButton | Open file |
| Ctrl+S | SaveMenuItem | Save |
| Ctrl+Shift+S | SaveAsMenuItem | Save as |
| Ctrl+T | ToggleThumbnailsButton | Toggle thumbnails |
| Ctrl+B | ToggleBookmarksButton | Toggle bookmarks |
| Ctrl+F | ToggleSearchButton | Toggle search |
| Ctrl+0 | ResetZoomButton | Reset zoom |
| F3 | NextMatchButton | Next search match |
| Shift+F3 | PreviousMatchButton | Previous search match |
| Esc | CloseSearchButton | Close search |

## Conditional Visibility

Elements that may not be visible:

| AutomationId | Visibility Binding | Default |
|--------------|-------------------|---------|
| AnnotationToolbar | ShowAnnotationToolbar | Hidden |
| SearchPanel | ShowSearchPanel | Hidden |
| ThumbnailsSidebar | ShowThumbnails | Visible |
| BookmarksSidebar | ShowBookmarks | Hidden |

## Testing Scenarios

### Scenario 1: Open File
1. Click `OpenFileButton`
2. Verify file dialog opens
3. Select file
4. Verify `ThumbnailsRepeater` populated
5. Verify `PageNumberBox` shows 1
6. Verify `TotalPages` updated

### Scenario 2: Search
1. Click `ToggleSearchButton`
2. Verify `SearchPanel` visible
3. Set `SearchTextBox` to "test"
4. Verify `SearchResultsText` updated
5. Click `NextMatchButton`
6. Verify current match highlighted
7. Click `CloseSearchButton`
8. Verify `SearchPanel` hidden

### Scenario 3: Annotation
1. Click `ToggleAnnotationToolbarButton` (if exists)
2. Verify `AnnotationToolbar` visible
3. Click `HighlightButton`
4. Verify `HighlightButton` checked
5. Set `AnnotationColorPicker` to yellow
6. Click `RectangleButton`
7. Verify `HighlightButton` unchecked (mutual exclusion)
8. Verify `RectangleButton` checked

### Scenario 4: Navigation
1. Verify `PreviousPageButton` disabled (page 1)
2. Click `NextPageButton`
3. Verify `PageNumberBox` shows 2
4. Verify `PreviousPageButton` enabled
5. Set `PageNumberBox` to 5
6. Verify current page is 5

### Scenario 5: Zoom
1. Click `ZoomInButton` 3 times
2. Verify zoom level increased
3. Select "Fit Width" in `ZoomLevelComboBox`
4. Verify content fitted to width
5. Click `ResetZoomButton`
6. Verify zoom at 100%

## AutomationId Naming Convention

**Pattern**: `{ComponentName}{ElementType}`

**Element Types**:
- `Button` - Buttons and ToggleButtons
- `TextBox` - Text input fields
- `ComboBox` - Dropdown selectors
- `CheckBox` - Checkboxes
- `RadioButton` - Radio buttons
- `Grid` / `Panel` - Layout containers
- `ScrollViewer` - Scroll containers
- `TreeView` - Tree controls
- `Repeater` - ItemsRepeater
- `ColorPicker` - Color pickers
- `Gripper` - Resize grippers
- `Text` - TextBlock displays
- `Canvas` / `Layer` - Canvas elements
- `Image` - Image elements

**Examples**:
- ✅ `OpenFileButton` - Button to open file
- ✅ `SearchTextBox` - TextBox for search
- ✅ `ThumbnailsScrollViewer` - ScrollViewer for thumbnails
- ❌ `OpenButton` - Too generic
- ❌ `Search` - Missing element type
- ❌ `thumbnails_scroll_viewer` - Wrong casing (use PascalCase)

## Coverage Status

✅ **Complete**: All main UI components have AutomationIds
⚠️ **Partial**: Thumbnail and Bookmark items need model AutomationId property
⏳ **Pending**: Dialog AutomationIds (WatermarkDialog, MergeDialog, SplitDialog, SettingsPage)

## Future Additions

Dialogs to be implemented:
- WatermarkDialog AutomationIds
- MergeDialog AutomationIds
- SplitDialog AutomationIds
- SettingsPage AutomationIds (task #22 in progress)
- ErrorDialog AutomationIds
- SaveConfirmationDialog AutomationIds
- DeletePagesDialog AutomationIds
