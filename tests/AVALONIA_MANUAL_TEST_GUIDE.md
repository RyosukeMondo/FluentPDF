# FluentPDF Avalonia Manual Test Guide

**Date:** 2026-01-28
**Platform:** Windows x64
**Build:** Debug

---

## Prerequisites

1. **Build Status:** ⚠️ XAML compilation error detected in `ThumbnailsSidebar.axaml`
   - Error: Multiple children in Border control (line 74)
   - **Fix Required:** Wrap ProgressBar and Image in a Grid/Panel

2. **Existing Executable:** ✅ Available at `src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe`
   - Last built: 2026-01-28 18:56:40
   - Size: 148.5 KB

3. **Test PDFs:** ✅ 13 PDFs available in `tests\Fixtures\`

---

## Manual Test Checklist

### Phase 1: Application Launch

#### Test 1.1: Launch and Window Visibility
**Steps:**
1. Navigate to `src\FluentPDF.Avalonia\bin\Debug\net8.0\`
2. Double-click `FluentPDF.Avalonia.exe`
3. Observe window appearance

**Expected:**
- [ ] Application launches without crash
- [ ] Main window appears on screen
- [ ] Window title shows "FluentPDF"
- [ ] Window is properly sized (1200x800)
- [ ] Window is responsive to mouse/keyboard

**Actual:** ___________________________________________

---

#### Test 1.2: Empty State Display
**Steps:**
1. With no PDFs open, observe the main content area

**Expected:**
- [ ] Empty state overlay visible
- [ ] Document icon displayed (centered)
- [ ] Text "No PDFs open" displayed
- [ ] "Open File" button visible
- [ ] Background properly themed

**Actual:** ___________________________________________

---

#### Test 1.3: Menu Bar
**Steps:**
1. Inspect menu bar at top of window

**Expected:**
- [ ] "File" menu item visible
- [ ] "Tools" menu item visible
- [ ] Menu items respond to hover
- [ ] Menu items respond to clicks

**Actual:** ___________________________________________

---

### Phase 2: File Menu Operations

#### Test 2.1: File Menu - Structure
**Steps:**
1. Click "File" menu

**Expected:**
- [ ] Menu drops down
- [ ] "Open..." item visible (Ctrl+O)
- [ ] Separator line
- [ ] "Save" item visible (Ctrl+S)
- [ ] "Save As..." item visible (Ctrl+Shift+S)
- [ ] "Recent Files" submenu
- [ ] "Clear Recent Files..." item
- [ ] "Exit" item

**Actual:** ___________________________________________

---

#### Test 2.2: Open File Dialog
**Steps:**
1. Click "File" → "Open..." (or press Ctrl+O)
2. Observe file dialog

**Expected:**
- [ ] File dialog opens
- [ ] Dialog is native Windows file picker
- [ ] Can navigate folders
- [ ] PDF files are visible/selectable
- [ ] Dialog has "Open" and "Cancel" buttons

**Actions:**
- [ ] Navigate to `tests\Fixtures\`
- [ ] Select `sample-with-text.pdf`
- [ ] Click "Open"

**Actual:** ___________________________________________

---

#### Test 2.3: PDF Loading
**Steps:**
1. After selecting PDF from Test 2.2

**Expected:**
- [ ] File dialog closes
- [ ] PDF loads in viewer
- [ ] Tab appears with filename
- [ ] Empty state overlay disappears
- [ ] PDF content visible
- [ ] No error messages

**Actual:** ___________________________________________

---

### Phase 3: PDF Viewer Functionality

#### Test 3.1: PDF Rendering
**Steps:**
1. With PDF loaded, inspect the viewer

**Expected:**
- [ ] PDF page(s) render correctly
- [ ] Text is readable
- [ ] Images/graphics display properly
- [ ] No visual artifacts
- [ ] No missing content

**Actual:** ___________________________________________

---

#### Test 3.2: Navigation Controls
**Steps:**
1. Look for navigation controls (if multi-page PDF)
2. Test next/previous page buttons

**Expected:**
- [ ] Page indicator visible (e.g., "Page 1 of 3")
- [ ] Next/Previous buttons visible
- [ ] Next button navigates forward
- [ ] Previous button navigates backward
- [ ] Page transitions smooth

**Actual:** ___________________________________________

---

#### Test 3.3: Zoom Controls
**Steps:**
1. Look for zoom controls
2. Test zoom in/out/reset

**Expected:**
- [ ] Zoom controls visible
- [ ] Zoom in button increases size
- [ ] Zoom out button decreases size
- [ ] Reset/fit button restores default
- [ ] Zoom percentage displayed

**Actual:** ___________________________________________

---

### Phase 4: Tab Management

#### Test 4.1: Multiple Tabs
**Steps:**
1. Open another PDF (File → Open)
2. Select different PDF from `tests\Fixtures\`

**Expected:**
- [ ] New tab created
- [ ] Tab shows filename
- [ ] Can switch between tabs
- [ ] Each tab shows correct PDF
- [ ] Tabs are visually distinct

**Actual:** ___________________________________________

---

#### Test 4.2: Tab Switching
**Steps:**
1. With 2+ tabs open
2. Click on different tabs
3. Try Ctrl+Tab (if supported)

**Expected:**
- [ ] Click switches active tab
- [ ] Active tab is highlighted
- [ ] Content updates to match tab
- [ ] Keyboard shortcut works (if implemented)

**Actual:** ___________________________________________

---

#### Test 4.3: Tab Closing
**Steps:**
1. With multiple tabs open
2. Look for close button on tab or use Ctrl+W

**Expected:**
- [ ] Tab close button visible (if implemented)
- [ ] Close button removes tab
- [ ] Other tabs remain open
- [ ] If last tab closed, empty state returns

**Actual:** ___________________________________________

---

### Phase 5: Keyboard Shortcuts

#### Test 5.1: File Operations
**Test each shortcut:**
- [ ] **Ctrl+O** - Opens file dialog
- [ ] **Ctrl+S** - Saves current file (if implemented)
- [ ] **Ctrl+Shift+S** - Opens Save As dialog (if implemented)
- [ ] **Ctrl+W** - Closes current tab (if implemented)

**Actual:** ___________________________________________

---

#### Test 5.2: Tab Navigation
**Test shortcuts:**
- [ ] **Ctrl+Tab** - Next tab (if implemented)
- [ ] **Ctrl+Shift+Tab** - Previous tab (if implemented)

**Actual:** ___________________________________________

---

### Phase 6: Tools Menu

#### Test 6.1: Settings Dialog
**Steps:**
1. Click "Tools" → "Settings..."
2. Observe settings dialog

**Expected:**
- [ ] Settings dialog opens
- [ ] Dialog is properly sized
- [ ] Settings categories visible
- [ ] Can modify settings (if implemented)
- [ ] OK/Cancel buttons work

**Actual:** ___________________________________________

---

### Phase 7: Theme and Visual Quality

#### Test 7.1: Theme System
**Steps:**
1. Observe overall application appearance

**Expected:**
- [ ] Theme resources loaded correctly
- [ ] Consistent color scheme
- [ ] Proper contrast (readable text)
- [ ] Buttons are styled
- [ ] No missing resources (no pink squares)

**Actual:** ___________________________________________

---

#### Test 7.2: UI Responsiveness
**Steps:**
1. Resize window by dragging edges
2. Minimize and restore window
3. Move window around screen

**Expected:**
- [ ] Window resizes smoothly
- [ ] Content scales appropriately
- [ ] No UI elements overlap
- [ ] Minimize/restore works
- [ ] Window position remembered

**Actual:** ___________________________________________

---

### Phase 8: Error Handling

#### Test 8.1: Invalid File
**Steps:**
1. Open file dialog
2. Try to open a non-PDF file (e.g., .txt, .jpg)

**Expected:**
- [ ] Error message displayed
- [ ] Message is user-friendly
- [ ] Application doesn't crash
- [ ] Can continue using app

**Actual:** ___________________________________________

---

#### Test 8.2: Corrupted PDF
**Steps:**
1. Open `tests\Fixtures\corrupted.pdf`

**Expected:**
- [ ] Error message displayed
- [ ] Message explains the issue
- [ ] Application remains stable
- [ ] Can open other files afterward

**Actual:** ___________________________________________

---

#### Test 8.3: Cancel Dialog
**Steps:**
1. Open file dialog (Ctrl+O)
2. Click "Cancel"

**Expected:**
- [ ] Dialog closes
- [ ] No error occurs
- [ ] App returns to previous state
- [ ] Can open dialog again

**Actual:** ___________________________________________

---

### Phase 9: Memory and Performance

#### Test 9.1: Startup Time
**Steps:**
1. Close application
2. Launch again
3. Time from click to window visible

**Expected:**
- [ ] Launch time < 5 seconds
- [ ] No long "white screen" period
- [ ] Splash screen (if implemented)

**Actual Timing:** ___________________________________________

---

#### Test 9.2: Memory Usage
**Steps:**
1. Open Task Manager
2. Find FluentPDF.Avalonia.exe process
3. Note memory usage with 1 PDF open
4. Open 5 more PDFs
5. Note memory increase

**Results:**
- Initial memory (1 PDF): ____________ MB
- Memory with 6 PDFs: ____________ MB
- Memory increase per PDF: ____________ MB

**Expected:** < 100 MB per PDF

---

#### Test 9.3: PDF Load Time
**Steps:**
1. Open a large PDF (e.g., `complex-layout.pdf`)
2. Time from selection to render

**Expected:**
- [ ] Load time < 3 seconds for small PDFs
- [ ] Progress indicator shown (if implemented)
- [ ] UI remains responsive during load

**Actual:** ___________________________________________

---

### Phase 10: Advanced Features

#### Test 10.1: Bookmarks Panel (if implemented)
**Steps:**
1. Open `bookmarked.pdf`
2. Look for bookmarks panel/sidebar

**Expected:**
- [ ] Bookmarks panel visible
- [ ] Bookmarks listed hierarchically
- [ ] Click bookmark navigates to page

**Actual:** ___________________________________________

---

#### Test 10.2: Thumbnails Sidebar (if implemented)
**Steps:**
1. Open multi-page PDF
2. Look for thumbnails sidebar

**Expected:**
- [ ] Thumbnails panel visible
- [ ] Thumbnails render correctly
- [ ] Click thumbnail navigates to page
- [ ] Current page highlighted

**Actual:** ___________________________________________

⚠️ **Known Issue:** Thumbnails sidebar has XAML compilation error - may not work until fixed

---

#### Test 10.3: Search Functionality (if implemented)
**Steps:**
1. Open PDF with text
2. Look for search box/button (Ctrl+F)
3. Search for keyword

**Expected:**
- [ ] Search UI appears
- [ ] Can enter search term
- [ ] Results highlighted in PDF
- [ ] Can navigate between results

**Actual:** ___________________________________________

---

## Test Summary

### Statistics
- **Total Tests:** 30+
- **Tests Passed:** ______ / ______
- **Tests Failed:** ______ / ______
- **Tests Skipped:** ______ / ______

### Critical Issues Found
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

### Minor Issues Found
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

### Recommendations
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

---

## Known Build Issues

### Critical
1. **ThumbnailsSidebar.axaml (Line 74)** - XAML Compilation Error
   - **Issue:** Border control has multiple children (ProgressBar and Image)
   - **Fix:** Wrap children in Grid or Panel
   - **Impact:** Thumbnails sidebar likely non-functional

   ```xml
   <!-- Current (broken) -->
   <Border>
       <ProgressBar ... />
       <Image ... />
   </Border>

   <!-- Fix -->
   <Border>
       <Grid>
           <ProgressBar ... />
           <Image ... />
       </Grid>
   </Border>
   ```

### Warnings
1. **Avalonia.DesktopRuntime.dll** - Missing dependency
   - May not be required if using single-file publish
   - Monitor for runtime issues

---

## Diagnostic Logs

Check desktop for diagnostic logs:
- **Pattern:** `FluentPDF_Avalonia_Diagnostic_YYYYMMDD_HHMMSS.txt`
- **Location:** Desktop
- **Contains:** Detailed startup, initialization, and error logs

If application crashes:
1. Find latest diagnostic log on desktop
2. Check for "FATAL" or "EXCEPTION" entries
3. Note stack traces
4. Include in bug report

---

## Test Environment

- **OS:** Windows 10.0.26200
- **PowerShell:** 7.5.4
- **.NET SDK:** 9.0.308
- **Avalonia Version:** 11.3.9
- **Build Configuration:** Debug
- **Platform:** x64

---

## Next Steps

1. **Fix XAML Error:** Resolve ThumbnailsSidebar.axaml compilation issue
2. **Rebuild:** Run `dotnet build src/FluentPDF.Avalonia`
3. **Retest:** Execute automated test suite
4. **Full Manual Test:** Complete all checklist items above
5. **Performance Test:** Extended memory leak test (10+ minute session)
6. **Cross-Platform:** Test on macOS/Linux (if supported)

---

**Test Conducted By:** ___________________________________________

**Date:** ___________________________________________

**Duration:** ___________________________________________

**Overall Assessment:** ☐ Pass ☐ Fail ☐ Needs Work

---

*Manual test guide generated by FluentPDF QA Agent*
