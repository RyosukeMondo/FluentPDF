# FluentPDF Keyboard Shortcuts

Complete reference for all keyboard shortcuts in FluentPDF. Implemented as part of UI/UX Phase 2: Keyboard Navigation.

## Standard Application Shortcuts

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+O` | Open file | Global | Opens file picker to select a PDF |
| `Ctrl+S` | Save document | Global | Saves current document (if modified) |
| `Ctrl+P` | Print dialog | Global | Reserved for future print functionality |
| `Ctrl+,` | Settings | Global | Opens application settings dialog |
| `Ctrl+W` | Close tab | Global | Closes current document tab |
| `Ctrl+F` | Find/Search | PDF Viewer | Opens text search panel |

## Tab Management

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+Tab` | Next tab | Global | Cycles through open tabs forward |
| `Ctrl+Shift+Tab` | Previous tab | Global | Cycles through open tabs backward |
| `Ctrl+W` | Close current tab | Global | Prompts to save if document modified |

## Page Navigation

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Page Up` | Previous page | PDF Viewer | Navigate to previous page |
| `Page Down` | Next page | PDF Viewer | Navigate to next page |
| `Home` | First page | PDF Viewer | Jump to page 1 |
| `End` | Last page | PDF Viewer | Jump to last page |
| `Ctrl+G` | Go to page | PDF Viewer | Opens dialog to enter page number |
| `Left Arrow` | Previous page | PDF Viewer | Alternative to Page Up |
| `Right Arrow` | Next page | PDF Viewer | Alternative to Page Down |

## Zoom Controls

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl++` | Zoom in | PDF Viewer | Increase zoom by 25% |
| `Ctrl+-` | Zoom out | PDF Viewer | Decrease zoom by 25% |
| `Ctrl+0` | Reset zoom | PDF Viewer | Reset zoom to 100% |
| `Ctrl+Add` | Zoom in | PDF Viewer | Numpad plus key |
| `Ctrl+Subtract` | Zoom out | PDF Viewer | Numpad minus key |
| `Ctrl+Scroll` | Zoom in/out | PDF Viewer | Mouse wheel zoom |

## Panels and Sidebars

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+T` | Toggle thumbnails | PDF Viewer | Show/hide thumbnails sidebar |
| `Ctrl+B` | Toggle bookmarks | PDF Viewer | Show/hide bookmarks panel (if available) |
| `Ctrl+F` | Toggle search | PDF Viewer | Show/hide search panel |
| `Escape` | Close panels | PDF Viewer | Closes panels in priority order (see below) |

### Escape Key Priority Order

When pressing `Escape`, panels close in this order:
1. Search panel (if visible)
2. Bookmarks panel (if visible)
3. Thumbnails sidebar (if visible)
4. Diagnostics panel (if visible)

## Text Selection and Copy

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+C` | Copy selected text | PDF Viewer | Copies text to clipboard |
| `Mouse drag` | Select text | PDF Viewer | Click and drag to select text region |

## Annotation Tools

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `H` | Highlight tool | PDF Viewer | Activates highlight annotation tool |
| `U` | Underline tool | PDF Viewer | Activates underline annotation tool |
| `S` | Strikethrough tool | PDF Viewer | Activates strikethrough annotation tool |
| `Escape` | Deselect tool | PDF Viewer | Deactivates current annotation tool |
| `Mouse drag` | Create annotation | PDF Viewer | Select text with active annotation tool |

### Annotation Workflow

1. Press `H`, `U`, or `S` to activate an annotation tool
2. Click and drag to select text on the PDF
3. Annotation is created automatically on mouse release
4. Tool remains active for multiple annotations
5. Press `Escape` to deactivate tool

## Search Operations

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+F` | Open search | PDF Viewer | Opens search panel and focuses input |
| `Enter` | Next match | Search Panel | Navigate to next search result |
| `Shift+Enter` | Previous match | Search Panel | Navigate to previous search result |
| `Escape` | Close search | Search Panel | Closes search panel |

## Form Field Navigation

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Tab` | Next field | Forms | Navigate to next form field |
| `Shift+Tab` | Previous field | Forms | Navigate to previous form field |

## Diagnostic and Developer Shortcuts

| Shortcut | Action | Context | Notes |
|----------|--------|---------|-------|
| `Ctrl+Shift+D` | Toggle diagnostics | PDF Viewer | Show/hide diagnostics panel |
| `Ctrl+Shift+L` | Open log viewer | PDF Viewer | Opens application log viewer |
| `F5` | Presentation mode | PDF Viewer | Enter full-screen presentation |
| `Ctrl+L` | Presentation mode | PDF Viewer | Alternative to F5 |

## Accessibility Features

### Focus Management

FluentPDF implements proper focus management to ensure keyboard-only navigation:

- **Tab Order**: All interactive elements follow logical tab order
- **Focus Indicators**: Visible focus rings on all focusable elements (uses ThemeResources)
- **Escape Key**: Closes panels and returns focus to main content
- **Dialog Focus**: Dialogs automatically focus primary input on open

### Screen Reader Support

- All buttons have proper `AutomationProperties.AutomationId` and labels
- Page navigation announces current page number
- Search results announce match count and position
- Form validation errors announce to screen readers

## Tips for Power Users

1. **Quick Navigation**: Use `Ctrl+G` for fast page jumping in large documents
2. **Panel Management**: Press `Escape` repeatedly to close all open panels
3. **Zoom Workflow**: `Ctrl+0` quickly resets zoom after zooming in/out
4. **Search Workflow**: `Ctrl+F` → type → `Enter` repeatedly to cycle matches
5. **Tab Workflow**: `Ctrl+O` to open, `Ctrl+Tab` to switch, `Ctrl+W` to close

## Command-Line Integration

FluentPDF supports keyboard shortcuts even when launched from CLI:

```powershell
# Open file directly
FluentPDF.App.exe "C:\path\to\document.pdf"

# Then use keyboard shortcuts for all operations
# No mouse required!
```

## Future Enhancements

Planned keyboard shortcuts for future releases:

- `Ctrl+P` - Print dialog (when printing is implemented)
- `Ctrl+Z` / `Ctrl+Y` - Undo/Redo for annotations
- `Ctrl+A` - Select all text on current page
- `Ctrl+N` - New tab/window
- `F11` - Toggle full screen mode

## Customization

> **Note**: Custom keyboard shortcuts are not currently supported but may be added in a future release based on user feedback.

## Platform Notes

- All shortcuts follow Windows conventions
- Keyboard accelerators work in both windowed and full-screen modes
- Shortcuts are disabled when text input fields have focus (except `Escape`)

---

**Version**: Phase 2 - Keyboard Navigation (Tasks 2.1-2.3) + Text Selection & Annotations
**Last Updated**: 2026-01-30
**Related Specs**: `.spec-workflow/specs/ui-implementation.md`, `.spec-workflow/specs/text-selection-annotations/`
