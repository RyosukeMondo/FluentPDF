# FluentPDF Features

This document describes the features and capabilities of FluentPDF.

## Table of Contents

- [PDF Viewing](#pdf-viewing)
- [PDF Form Filling](#pdf-form-filling)
- [Document Navigation](#document-navigation)
- [Office Document Conversion](#office-document-conversion)

## PDF Viewing

### Core Viewing Capabilities

- **High-Quality Rendering**: Powered by Google's PDFium engine
- **Multi-Page Support**: Navigate through documents of any size
- **Zoom Controls**: Zoom in/out with mouse wheel or toolbar buttons
  - Zoom range: 50% to 200%
  - Smooth zoom transitions
- **Page Navigation**:
  - Next/Previous page buttons
  - Direct page number input
  - Keyboard shortcuts (Page Up/Down)

### Display Features

- **Anti-Aliasing**: Smooth text and graphics rendering
- **Responsive UI**: Async rendering prevents UI blocking
- **Memory Efficient**: Automatic bitmap cleanup and resource management
- **Error Handling**: Graceful handling of corrupted or invalid PDFs

### Keyboard Shortcuts

- **Ctrl+O**: Open PDF document
- **Page Up/Down**: Navigate pages
- **Ctrl++**: Zoom in
- **Ctrl+-**: Zoom out
- **Ctrl+0**: Reset zoom to 100%

## PDF Form Filling

FluentPDF provides comprehensive PDF form filling capabilities with interactive overlay controls and keyboard navigation.

### Supported Form Field Types

1. **Text Fields**
   - Single-line text input
   - Multi-line text areas
   - Max length validation
   - Format mask support (e.g., phone numbers, dates)

2. **Checkboxes**
   - Single checkbox fields
   - Required field validation
   - Read-only protection

3. **Radio Buttons**
   - Grouped radio button sets
   - Single selection enforcement
   - Tab navigation through groups

### Form Interaction Features

#### Interactive Overlay Controls

- **Positioned Overlays**: Form fields rendered as WinUI controls overlaid on PDF pages
- **Zoom-Aware**: Field positions automatically adjust with zoom level
- **Visual States**: Clear visual feedback for:
  - Normal state
  - Hover (mouse over)
  - Focused (keyboard focus)
  - Error (validation failed)
  - Read-only (disabled input)

#### Keyboard Navigation

- **Tab Navigation**: Press Tab to move to next field in tab order
- **Shift+Tab Navigation**: Press Shift+Tab to move to previous field
- **Automatic Focus**: First field automatically focused when form loads
- **Tab Order**: Fields follow PDF-defined tab order for logical navigation

#### Form Validation

Real-time validation with clear error messages:

1. **Required Fields**: Ensures required fields are not empty
2. **Max Length**: Validates text length against field limits
3. **Format Masks**: Validates input format using regex patterns
4. **Read-Only Protection**: Prevents modification of read-only fields

**Validation Error Display**:
- Errors shown in InfoBar at top of page
- Field borders highlighted in red
- Clear, actionable error messages
- Per-field validation on input
- Form-wide validation on save

#### Form Data Persistence

- **Save Form Data**: Save filled form data back to PDF
- **Preserves Original**: Saves to new file, original unchanged
- **Native Format**: Uses PDFium's native save API (FPDF_SaveAsCopy)
- **Reload Verification**: Saved data persists when reopening PDF

### Form Filling Workflow

```
1. Open PDF with form fields
   ↓
2. Form fields detected automatically
   ↓
3. Interactive overlays appear on form fields
   ↓
4. Fill fields using keyboard or mouse
   ↓
5. Tab through fields in order
   ↓
6. Real-time validation as you type
   ↓
7. Save form when complete
   ↓
8. Validation check before save
   ↓
9. Fix any validation errors
   ↓
10. Form saved successfully
```

### Technical Implementation

- **PDFium Form API**: Native form field detection and manipulation
- **WinUI Custom Controls**: FormFieldControl with type-specific templates
- **MVVM Architecture**: Clean separation with FormFieldViewModel
- **Result Pattern**: Structured error handling with detailed context
- **SafeHandle**: Automatic resource cleanup for form environment
- **Structured Logging**: All form operations logged with correlation IDs

### Limitations and Future Enhancements

**Current Limitations**:
- Combo boxes and list boxes not yet supported
- Digital signatures display only (not editable)
- No JavaScript formula evaluation
- No rich text formatting in text fields

**Planned Enhancements**:
1. Auto-fill from user profile
2. Form templates (FDF format)
3. Combo box and list box support
4. Digital signature creation
5. Field calculation formulas
6. File attachment fields

### Form Filling Best Practices

1. **Save Frequently**: Save your work regularly to avoid data loss
2. **Check Validation**: Address validation errors before saving
3. **Use Tab Navigation**: Faster than clicking each field
4. **Review Before Saving**: Double-check all fields are complete
5. **Keep Backups**: Original PDF remains unchanged

## Document Navigation

### Bookmarks Panel

- **Hierarchical Bookmarks**: View nested bookmark structure in TreeView
- **Click to Navigate**: Click bookmark to jump to page
- **Keyboard Shortcuts**:
  - **Ctrl+B**: Toggle bookmarks panel
- **Panel Persistence**: Panel visibility and width saved across sessions
- **Resizable Panel**: Drag to adjust panel width (150-600px)
- **Empty State**: Clear message when PDF has no bookmarks
- **UTF-16 Support**: Properly displays international characters in bookmark titles

### Page Thumbnails

*(Planned Feature)*

- Visual page previews in sidebar
- Quick navigation to any page
- Thumbnail caching for performance

## Office Document Conversion

### Supported Formats

- **Microsoft Word**: .docx files (Office Open XML)

### Conversion Features

#### High-Quality Conversion

- **Semantic HTML Conversion**: Uses Mammoth.NET for structure-preserving conversion
- **Chromium Rendering**: WebView2 for PDF generation with native browser quality
- **Image Embedding**: Images embedded as base64 data URIs
- **Formatting Preservation**:
  - Bold, italic, underline
  - Headings (H1-H6)
  - Lists (bulleted and numbered)
  - Tables
  - Page breaks

#### Conversion Options

- **Custom Margins**: Configurable page margins (default: 0.5 inch)
- **Page Size**: US Letter (8.5" x 11") - customizable in future
- **Background Printing**: CSS backgrounds included in output
- **Scale Factor**: 100% scale (configurable)

#### Quality Validation (Optional)

- **LibreOffice Baseline**: Compare against LibreOffice conversion
- **SSIM Metrics**: Structural Similarity Index for quality measurement
- **Comparison Images**: Visual diff images saved for manual review
- **Quality Thresholds**:
  - Excellent: 0.95-1.0
  - Good: 0.85-0.95
  - Fair: 0.70-0.85
  - Poor: < 0.70

#### Conversion Workflow

```
1. Select DOCX file
   ↓
2. Optionally enable quality validation
   ↓
3. Click Convert button
   ↓
4. Parse DOCX to HTML (Mammoth.NET)
   ↓
5. Render HTML to PDF (WebView2)
   ↓
6. Optionally validate quality (LibreOffice + SSIM)
   ↓
7. Display conversion results
   ↓
8. Open converted PDF in viewer
```

### Conversion Performance

**Typical Times** (Intel i7, 16GB RAM):
- Simple text (10 pages): 2-3 seconds
- With images (10 pages): 3-5 seconds
- Complex formatting (10 pages): 5-8 seconds
- Large document (100 pages): 30-60 seconds

**Memory Usage**:
- Baseline: < 50MB when idle
- During conversion: 200-500MB (typical)
- Peak: Up to 1GB for image-heavy documents

### Requirements

- **WebView2 Runtime**: Required for HTML-to-PDF conversion
  - Usually pre-installed on Windows 10/11
  - Automatic installation prompt if missing
- **LibreOffice** (Optional): For quality validation
  - Not required for basic conversion
  - Download from https://www.libreoffice.org/

### Conversion Limitations

**Not Supported**:
- Password-protected DOCX files
- Documents > 100MB
- Macros and VBA code
- SmartArt graphics (converted to static images)
- Embedded videos/audio
- Track changes and comments

**Future Format Support**:
- Excel (.xlsx)
- PowerPoint (.pptx)
- HTML files
- Markdown files

## Error Handling

### User-Friendly Error Messages

- **Correlation IDs**: Unique ID for each error for support tracking
- **Contextual Information**: Clear description of what went wrong
- **Actionable Guidance**: Suggestions for how to fix the issue
- **Structured Logging**: All errors logged to JSON files for analysis

### Global Exception Handling

Three-layer safety net:
1. **UI Thread Handler**: Catches unhandled UI exceptions, shows error dialog
2. **Background Task Handler**: Catches async task exceptions
3. **AppDomain Handler**: Final safety net for non-UI threads

### Error Recovery

- **Automatic Retry**: Some operations automatically retry on transient failures
- **Graceful Degradation**: App continues running after recoverable errors
- **Resource Cleanup**: Guaranteed cleanup even during error conditions

## Logging and Observability

### Structured Logging (Serilog)

- **JSON Format**: Machine-parsable log files
- **Correlation IDs**: Link related operations across log entries
- **Performance Metrics**: Log operation durations and performance warnings
- **Error Context**: Full context with stack traces for debugging

### Log Storage

- **Location**: `%LocalAppData%\FluentPDF\logs\`
- **Format**: Daily rolling JSON files (`log-YYYYMMDD.json`)
- **Retention**: 7 days (automatic cleanup)
- **Privacy**: No sensitive data logged (PII, passwords, etc.)

### OpenTelemetry Integration (Development)

- **Live Dashboard**: Real-time telemetry via .NET Aspire Dashboard
- **Distributed Tracing**: Span instrumentation for operation tracking
- **Metrics Export**: OTLP/gRPC to localhost:4317

## Future Features

### Planned Enhancements

1. **Annotation Tools**
   - Highlight text
   - Add comments
   - Draw shapes
   - Sticky notes

2. **Text Selection and Copy**
   - Select text with mouse
   - Copy to clipboard
   - Search within document

3. **Print Support**
   - Print to physical printer
   - Print to PDF (flatten forms)
   - Page range selection

4. **PDF Manipulation**
   - Merge multiple PDFs
   - Split PDF into multiple files
   - Extract pages
   - Rotate pages
   - Reorder pages

5. **Cloud Integration**
   - OneDrive sync
   - SharePoint integration
   - Auto-save to cloud

6. **Accessibility**
   - Screen reader support
   - High contrast themes
   - Keyboard-only navigation
   - Text-to-speech

7. **Advanced Form Features**
   - Digital signatures
   - Form templates (FDF)
   - Auto-fill from profile
   - Form field calculations

## System Requirements

### Minimum Requirements

- **OS**: Windows 10 version 1809 (Build 17763) or later
- **RAM**: 4GB
- **Disk Space**: 500MB
- **Display**: 1366x768

### Recommended Requirements

- **OS**: Windows 11 version 22H2 or later
- **RAM**: 8GB or more
- **Disk Space**: 1GB (for caching and logs)
- **Display**: 1920x1080 or higher
- **WebView2 Runtime**: Pre-installed on Windows 11

### Architecture Support

- **x64**: Fully supported
- **ARM64**: Supported (native ARM64 build available)

## References

- [Architecture Documentation](./ARCHITECTURE.md)
- [Testing Strategy](./TESTING.md)
- [Form Filling Specification](../.spec-workflow/specs/form-filling/)
- [Bookmarks Panel Specification](../.spec-workflow/specs/bookmarks-panel/)
