# UI Production Ready - Requirements

## Overview
Transform FluentPDF Avalonia UI from debug/development mode to production-ready state by removing debug artifacts, fixing critical PDFium rendering connection, and polishing the user experience.

## Critical Issue
**PDFium Rendering Broken**: `PdfViewerViewModel.RenderPageCallback` is never wired up, causing PDFs to load but not render (blank pages).

## Requirements

### FR-1: Fix PDFium Rendering Pipeline
**Priority**: CRITICAL
**Status**: Broken

**Current State**:
- `PdfViewerViewModel.RenderPageCallback` is declared but never assigned
- PDFs load successfully but pages don't render
- Warning logged: "RenderPageCallback not set, cannot render page"

**Required Fixes**:
1. Wire up `RenderPageCallback` in `PdfViewerPage.axaml.cs` or `PdfViewerControl.axaml.cs`
2. Connect to `IPdfRenderingService` via DI
3. Convert Stream → Avalonia Bitmap for display
4. Handle errors gracefully with user-visible messages

**Acceptance Criteria**:
- PDFs open and display page 1 immediately
- Page navigation (next/prev) renders correctly
- Zoom operations trigger re-render
- Error messages displayed if rendering fails

---

### FR-2: Hide Debug Console by Default
**Priority**: HIGH
**Status**: Always visible (250px height)

**Current State**:
- Debug console occupies 250px at bottom of window
- Comment says "ALWAYS VISIBLE"
- Log refresh timer is disabled (commented out)

**Required Fixes**:
1. Set RowDefinition Height to "Auto" or "0" by default
2. Add toggle button to toolbar or View menu
3. Persist visibility state in user settings
4. Keep keyboard shortcut (Ctrl+Shift+L or similar)

**Acceptance Criteria**:
- Debug console hidden on app launch
- Toggle button clearly visible and accessible
- Console animations smooth (slide up/down)
- State persists across app restarts

---

### FR-3: Remove Desktop Debug Log Files
**Priority**: MEDIUM
**Status**: Creates files on Desktop folder

**Current State**:
- `FluentPDF-Hang-Debug-{timestamp}.txt` created on startup
- `FluentPDF-Early-Debug-{timestamp}.txt` created on startup
- Files clutter user's Desktop

**Required Fixes**:
1. Wrap debug file creation in `#if DEBUG` directives
2. OR move to AppData/Logs directory
3. OR make conditional on "--debug" command-line flag

**Acceptance Criteria**:
- No Desktop files in Release builds
- Debug logging still available in Debug builds
- Log path clearly documented if kept

---

### FR-4: Clean Up Console.WriteLine Debugging
**Priority**: MEDIUM
**Status**: 50+ statements throughout code

**Locations**:
- `MainWindow.axaml.cs` constructor
- `MainWindow.axaml.cs` OnWindowLoaded
- `App.axaml.cs` OnFrameworkInitializationCompleted

**Required Fixes**:
1. Replace with conditional ILogger calls
2. Remove ">>>" prefix debugging statements
3. Keep critical error messages via ILogger.LogError
4. Use structured logging with proper log levels

**Acceptance Criteria**:
- No Console.WriteLine in production code paths
- All important events logged via ILogger
- Log output clean and professional

---

### FR-5: Remove Debug Artifacts
**Priority**: LOW
**Status**: TestWindow exists

**Artifacts**:
- `TestWindow.axaml` - Simple "✅ AVALONIA IS WORKING!" window
- Commented-out WinUI 3 dialog code in MainViewModel.cs
- Disabled log refresh timer

**Required Fixes**:
1. Delete `TestWindow.axaml` and `.cs` files
2. Remove commented WinUI 3 code
3. Re-enable log refresh timer OR remove if not needed

**Acceptance Criteria**:
- TestWindow files deleted
- Code comments cleaned up
- Log timer working or removed entirely

---

### FR-6: Production UI Polish
**Priority**: MEDIUM
**Status**: Functional but needs polish

**Required Improvements**:
1. **Empty State**: Clear message when no PDFs open
2. **Loading States**: Show progress for PDF loading/rendering
3. **Error Handling**: User-friendly error messages (not console)
4. **Tooltips**: Ensure all toolbar buttons have tooltips
5. **Status Bar**: Show document info (page X of Y, zoom %, file path)
6. **Keyboard Shortcuts**: Document all shortcuts clearly

**Acceptance Criteria**:
- Empty state with helpful "Open PDF" button
- Loading spinner during PDF operations
- Error dialogs for common failures
- All UI elements have tooltips
- Status bar shows relevant info

---

## Non-Functional Requirements

### NFR-1: Performance
- PDF rendering completes in <500ms for typical pages
- UI remains responsive during rendering
- Memory usage acceptable (<200MB idle, <500MB with PDF)

### NFR-2: Reliability
- No crashes on invalid PDF files
- Graceful degradation if PDFium fails
- Error recovery without app restart

### NFR-3: Usability
- First-time users can open PDF without documentation
- Keyboard shortcuts follow common conventions
- Visual feedback for all operations

### NFR-4: Maintainability
- Debug code clearly separated (#if DEBUG)
- Logging uses structured ILogger
- No hardcoded file paths

---

## Out of Scope
- Multi-window support
- PDF editing features (annotations, forms)
- Advanced rendering options (beyond zoom)
- Theme customization UI

---

## Success Metrics
1. **PDF Rendering Works**: 100% of valid PDFs render correctly
2. **Clean UI**: No debug artifacts visible to end users
3. **Professional**: Desktop not cluttered with log files
4. **Performant**: Renders standard PDF page in <500ms
5. **User-Friendly**: Clear error messages, helpful empty state

---

## Dependencies
- PDFium library (already integrated)
- IPdfRenderingService (already registered in DI)
- SkiaRenderingStrategy (Avalonia-specific, already implemented)
- Avalonia 11.3.9 (already using)

---

## Risk Assessment
| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| RenderPageCallback wiring complex | Low | High | Reference WinUI 3 implementation |
| Bitmap conversion issues | Medium | High | Test with various PDF types |
| Performance regression | Low | Medium | Profile before/after |
| Breaking existing features | Low | Medium | Comprehensive testing |

---

## Timeline Estimate
- **Critical Fix (FR-1)**: 30-60 minutes
- **Debug Cleanup (FR-2,3,4,5)**: 30-45 minutes
- **UI Polish (FR-6)**: 60-90 minutes
- **Testing & Validation**: 30 minutes
- **Total**: 2.5-4 hours

---

## Validation Plan
1. Open sample PDFs (simple, complex, large)
2. Test navigation (next/prev page, jump to page)
3. Test zoom (in/out, fit width, fit page)
4. Test multi-document tabs
5. Verify no debug artifacts visible
6. Check Desktop for log files
7. Review console output for cleanliness
