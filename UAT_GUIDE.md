# FluentPDF User Acceptance Testing (UAT) Guide

## Quick Start Commands

### 1. Build the Application (First Time)

```powershell
# Navigate to project directory
cd C:\Users\ryosu\repos\FluentPDF

# Build the application
dotnet build src\FluentPDF.App -p:Platform=x64
```

### 2. Launch FluentPDF

```powershell
# Option A: Use the launch script (RECOMMENDED)
pwsh tools\launch-uat.ps1

# Option B: Direct launch with test PDF
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe tests\Fixtures\sample.pdf

# Option C: Launch without PDF
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe
```

### 3. Launch with API Server (for Autonomous Testing)

```powershell
# Start API server
pwsh tools\launch-uat.ps1 -ApiServer

# In another terminal, run tests
pwsh tools\run-autonomous-tests.ps1
```

### 4. Build and Launch in One Command

```powershell
pwsh tools\launch-uat.ps1 -BuildFirst
```

### 5. Run Complete Autonomous Tests

```powershell
pwsh tools\launch-uat.ps1 -RunTests
```

---

## Manual UAT Test Cases

### Test Case 1: Basic Document Loading

**Steps:**
1. Launch FluentPDF: `pwsh tools\launch-uat.ps1`
2. Click "Open" button (or Ctrl+O)
3. Select `tests\Fixtures\sample.pdf`
4. Document should load and display

**Expected:**
- ✅ Document loads without errors
- ✅ First page displays
- ✅ Page counter shows "Page 1 of N"
- ✅ Thumbnails sidebar shows page previews

---

### Test Case 2: Navigation

**Steps:**
1. Open any PDF document
2. Click "Next Page" button (or press Right Arrow)
3. Click "Previous Page" button (or press Left Arrow)
4. Enter page number in page box and press Enter

**Expected:**
- ✅ Navigation works smoothly
- ✅ Current page updates correctly
- ✅ Thumbnail highlights current page
- ✅ Page number box shows correct value

---

### Test Case 3: Zoom Controls

**Steps:**
1. Open a PDF document
2. Click "Zoom In" button (or Ctrl++)
3. Click "Zoom Out" button (or Ctrl+-)
4. Select zoom level from dropdown (100%, 150%, etc.)
5. Click "Reset Zoom" (or Ctrl+0)

**Expected:**
- ✅ Zoom changes are smooth
- ✅ Content scales correctly
- ✅ Reset returns to 100%
- ✅ Fit Width/Fit Page work correctly

---

### Test Case 4: View Modes

**Steps:**
1. Open a PDF document
2. Click view mode dropdown
3. Select "Continuous Scroll"
4. Scroll vertically through pages
5. Select "Two Page"
6. Verify two pages display side-by-side

**Expected:**
- ✅ Continuous scroll shows all pages vertically
- ✅ Two-page mode shows pages side-by-side
- ✅ Single page mode shows one page at a time

---

### Test Case 5: Search Functionality

**Steps:**
1. Open a PDF with text content
2. Click Search button (or Ctrl+F)
3. Enter search text
4. Click "Next" and "Previous" buttons
5. Toggle "Case Sensitive" checkbox
6. Close search panel (Esc)

**Expected:**
- ✅ Search finds matches
- ✅ Match counter shows "X of Y"
- ✅ Navigation highlights matches
- ✅ Case sensitivity works
- ✅ Matches are highlighted on page

---

### Test Case 6: Text Selection and Annotations

**Steps:**
1. Open a PDF document with text content (e.g., `tests\Fixtures\sample-with-text.pdf`)
2. Test basic text selection:
   - Click and drag to select text
   - Verify selection rectangle appears
   - Copy selected text with Ctrl+C
   - Paste into notepad to verify text was copied correctly
3. Test highlight annotation:
   - Press `H` to activate highlight tool
   - Verify cursor changes to crosshair
   - Verify status bar shows "Highlight tool active"
   - Click and drag to select text
   - Release mouse - highlight annotation should appear in yellow
   - Select more text - second highlight should appear (tool stays active)
4. Test underline annotation:
   - Press `U` to activate underline tool
   - Verify cursor changes to crosshair
   - Verify status bar shows "Underline tool active"
   - Select text to create underline annotation
   - Verify underline appears below selected text
5. Test strikethrough annotation:
   - Press `S` to activate strikethrough tool
   - Verify cursor changes to crosshair
   - Verify status bar shows "Strikethrough tool active"
   - Select text to create strikethrough annotation
   - Verify strikethrough line appears through selected text
6. Test tool deactivation:
   - Press `Escape` to deactivate annotation tools
   - Verify cursor returns to normal
   - Verify status bar clears tool message
7. Test in continuous scroll mode:
   - Switch to continuous scroll mode (View menu → Continuous Scroll)
   - Press `H` to activate highlight tool
   - Scroll to page 2 and select text
   - Verify highlight annotation created on correct page
   - Switch back to single page mode
   - Verify annotation persists on page 2
8. Test annotation persistence:
   - Save document (Ctrl+S)
   - Close and reopen document
   - Verify all annotations are still present
   - Verify annotations on correct pages with correct text bounds

**Expected:**
- ✅ Text selection works with mouse drag (selection rectangle visible)
- ✅ Selected text can be copied to clipboard (Ctrl+C)
- ✅ Highlight tool creates yellow highlights from text selection
- ✅ Underline creates underline annotations from text selection
- ✅ Strikethrough creates strikethrough annotations from text selection
- ✅ Keyboard shortcuts (H, U, S) activate annotation tools
- ✅ Cursor changes to crosshair when tool is active
- ✅ Status bar shows active tool name (e.g., "Highlight tool active")
- ✅ Tool stays active after creating annotation (can create multiple)
- ✅ Escape key deactivates tool and returns cursor to normal
- ✅ Annotations work in all view modes (single page, continuous scroll, two-page)
- ✅ Annotations created on correct page in continuous scroll mode
- ✅ Annotations match exact text selection bounds (multi-line selections supported)
- ✅ Annotations save with document
- ✅ Annotations reload correctly after close/reopen

---

### Test Case 7: Form Filling

**Steps:**
1. Open a PDF with form fields
2. Click in a text field and type
3. Check/uncheck checkboxes
4. Select radio buttons
5. Save document (Ctrl+S)
6. Reopen and verify form data persisted

**Expected:**
- ✅ Text input works
- ✅ Checkboxes toggle
- ✅ Radio buttons work correctly
- ✅ Form data saves
- ✅ Validation errors display (if applicable)

---

### Test Case 8: Export as Images

**Steps:**
1. Open a PDF document
2. Click "Export Images" (needs toolbar button)
3. Select format (PNG or JPG)
4. Select DPI (150 or 300)
5. Choose output folder
6. Click "Export"
7. Verify images are created

**Expected:**
- ✅ Export dialog opens
- ✅ Progress bar shows during export
- ✅ Images created in output folder
- ✅ Image quality matches DPI setting

---

### Test Case 9: Find & Replace

**Steps:**
1. Open a PDF document
2. Open search panel (Ctrl+F)
3. Enter text to find
4. Click "Replace" tab
5. Enter replacement text
6. Click "Replace" or "Replace All"
7. Verify text is replaced

**Expected:**
- ✅ Find locates matches
- ✅ Replace updates text
- ✅ Replace All updates all matches
- ✅ Preview mode works
- ✅ Undo works

---

### Test Case 10: Watermark

**Steps:**
1. Open a PDF document
2. Click "Watermark" button
3. Enter watermark text ("DRAFT")
4. Select position (Center)
5. Adjust opacity (50%)
6. Click "Apply"
7. Verify watermark appears

**Expected:**
- ✅ Watermark dialog opens
- ✅ Preview updates in real-time
- ✅ Position grid works
- ✅ Opacity slider works
- ✅ Watermark applies to pages

---

### Test Case 11: Merge PDFs

**Steps:**
1. Click "Merge" button
2. Add 2-3 PDF files
3. Reorder files (drag-drop or buttons)
4. Enter output filename
5. Click "Merge"
6. Verify merged PDF is created

**Expected:**
- ✅ Files can be added
- ✅ Reordering works
- ✅ Total page count updates
- ✅ Merge creates valid PDF
- ✅ Page order is correct

---

### Test Case 12: Split PDF

**Steps:**
1. Open a PDF document
2. Click "Split" button
3. Select "By Page Ranges"
4. Enter ranges (e.g., "1-5", "6-10")
5. Choose output folder
6. Click "Split"
7. Verify split PDFs are created

**Expected:**
- ✅ Split dialog opens
- ✅ Page ranges can be added
- ✅ Preview shows output files
- ✅ Split creates multiple PDFs
- ✅ Page ranges are correct

---

### Test Case 13: Optimize PDF

**Steps:**
1. Open a large PDF document
2. Click "Optimize" button
3. Select optimization options
4. Choose output file
5. Click "Optimize"
6. Verify file size is reduced

**Expected:**
- ✅ Optimize dialog opens
- ✅ File size reduction shown
- ✅ Optimized PDF opens correctly
- ✅ Quality is acceptable

---

### Test Case 14: Presentation Mode

**Steps:**
1. Open a PDF document
2. Press F5 or Ctrl+L
3. Navigate with arrow keys
4. Move mouse to see controls
5. Press Esc to exit

**Expected:**
- ✅ Full-screen mode activates
- ✅ Black background
- ✅ Controls auto-hide
- ✅ Arrow keys navigate
- ✅ Esc exits presentation

---

### Test Case 15: Stamps

**Steps:**
1. Open a PDF document
2. Click "Stamp" button in annotation toolbar
3. Select "APPROVED" stamp
4. Click on page to place stamp
5. Adjust rotation and opacity
6. Save document

**Expected:**
- ✅ Stamp gallery opens
- ✅ Stamp preview updates
- ✅ Stamp places on click
- ✅ Rotation/opacity work
- ✅ Stamp saves with document

---

### Test Case 16: Theme Switching

**Steps:**
1. Open FluentPDF
2. Open Settings (needs menu item)
3. Select Light theme
4. Verify UI changes
5. Select Dark theme
6. Verify UI changes
7. Select System theme

**Expected:**
- ✅ Light theme: white backgrounds
- ✅ Dark theme: dark backgrounds
- ✅ System theme: matches Windows setting
- ✅ All controls visible in both themes

---

### Test Case 17: Sidebar Toggling

**Steps:**
1. Open a PDF document
2. Click "Thumbnails" toggle (Ctrl+T)
3. Verify sidebar shows/hides
4. Click "Bookmarks" toggle (Ctrl+B)
5. Verify sidebar shows/hides
6. Resize sidebar with gripper

**Expected:**
- ✅ Thumbnails toggle works
- ✅ Bookmarks toggle works
- ✅ Sidebar resize works (150-600px)
- ✅ State persists across sessions

---

### Test Case 18: Encryption (Requires QPDF)

**Steps:**
1. Open a PDF document
2. Click "Encrypt Document"
3. Enter owner password
4. Set permissions (uncheck "Print")
5. Select 256-bit AES
6. Click "Encrypt"
7. Try to print encrypted PDF

**Expected:**
- ✅ Encrypt dialog opens
- ✅ Password validation works
- ✅ Encrypted PDF created
- ✅ Permissions enforced
- ✅ Can open with password

---

## CLI Testing Commands

### Test Rendering
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-render "tests\Fixtures\sample.pdf"
```

### Test Merge
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-merge "file1.pdf;file2.pdf" --output "merged.pdf"
```

### Test Export Images
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-export-images "tests\Fixtures\sample.pdf" --output "C:\temp\export" --format png --dpi 150
```

### Test Stamps
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-stamp "tests\Fixtures\sample.pdf" --stamp APPROVED --output "stamped.pdf"
```

### Test Encryption (Requires QPDF)
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-encrypt "tests\Fixtures\sample.pdf" --owner-password "secret123" --strength 256 --output "encrypted.pdf"
```

---

## REST API Testing

### Start API Server
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --api-server --port 5000
```

### Test Health Endpoint
```powershell
curl http://localhost:5000/api/health
```

### Test Status Endpoint
```powershell
curl http://localhost:5000/api/status
```

### Test Element Verification
```powershell
curl -X POST http://localhost:5000/api/verify/element `
  -H "Content-Type: application/json" `
  -d '{"automationId":"OpenFileButton","expectedProperties":{"isEnabled":true}}'
```

---

## Autonomous Test Suite

### Run Complete Test Suite
```powershell
pwsh tools\run-autonomous-tests.ps1
```

### View HTML Report
```powershell
start tests\reports\test-report.html
```

### View JSON Results
```powershell
cat tests\reports\test-results.json | ConvertFrom-Json | Format-List
```

---

## Known Issues / Workarounds

1. **QPDF Not Found** - Encryption features require QPDF installation
   - Download: https://github.com/qpdf/qpdf/releases
   - Install to PATH or `C:\Program Files\qpdf\bin\`

2. **Build Errors** - Some pre-existing build warnings in unrelated code
   - These don't affect UAT testing
   - Focus on runtime behavior

3. **Find & Replace** - Uses text overlays, not true content editing
   - Limitations: Font matching not perfect
   - Workaround: Preview before applying

4. **Line Annotations** - Canvas interaction needs completion
   - UI elements exist but interactive drawing not wired up yet

---

## UAT Checklist

Print this and check off during testing:

### Core Features
- [ ] Document loading
- [ ] Page navigation
- [ ] Zoom controls
- [ ] Search functionality
- [ ] Thumbnails sidebar
- [ ] Bookmarks sidebar

### View Modes
- [ ] Single page mode
- [ ] Continuous scroll mode
- [ ] Two-page mode
- [ ] Presentation mode (F5)

### Annotations
- [ ] Highlight
- [ ] Underline
- [ ] Strikethrough
- [ ] Rectangle
- [ ] Circle
- [ ] Freehand
- [ ] Sticky notes
- [ ] Stamps

### Forms
- [ ] Text fields
- [ ] Checkboxes
- [ ] Radio buttons
- [ ] Form validation
- [ ] Form persistence

### Document Operations
- [ ] Merge PDFs
- [ ] Split PDF
- [ ] Optimize PDF
- [ ] Watermark
- [ ] Export images

### Advanced Features
- [ ] Find & replace
- [ ] Encryption (with QPDF)
- [ ] Theme switching
- [ ] Keyboard shortcuts

### Autonomous Testing
- [ ] CLI commands work
- [ ] REST API responds
- [ ] Test suite passes
- [ ] Reports generated

---

## Bug Reporting Template

If you find issues during UAT, report them with:

```
**Title**: Brief description
**Severity**: Critical / High / Medium / Low
**Steps to Reproduce**:
1. Step 1
2. Step 2
3. Step 3

**Expected Behavior**: What should happen
**Actual Behavior**: What actually happened
**Screenshots**: (if applicable)
**Error Messages**: (copy/paste any errors)
**Environment**: Windows version, .NET version
```

---

## Success Criteria

UAT is successful if:
- ✅ All core features work without crashes
- ✅ Autonomous test suite passes (>90% tests pass)
- ✅ UI is responsive and theme-aware
- ✅ Document operations produce valid PDFs
- ✅ Annotations and forms persist correctly
- ✅ No data loss during save operations

---

## Next Steps After UAT

1. **Fix Critical Bugs** - Address any blocking issues
2. **Performance Tuning** - Optimize slow operations
3. **Documentation** - Update user guides
4. **Store Submission** - Prepare for Microsoft Store
5. **Release Planning** - Version numbers, changelog
