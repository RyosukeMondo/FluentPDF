# FluentPDF Avalonia - User Guide

Welcome to FluentPDF, a modern cross-platform PDF viewer built on Avalonia UI.

## Table of Contents

- [Getting Started](#getting-started)
- [Opening PDF Files](#opening-pdf-files)
- [Navigation](#navigation)
- [Viewing Options](#viewing-options)
- [Theme System](#theme-system)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Command Line Usage](#command-line-usage)
- [Tips and Tricks](#tips-and-tricks)
- [Troubleshooting](#troubleshooting)

## Getting Started

### First Launch

When you first launch FluentPDF:

1. The main window opens with a welcome screen
2. Use the "Open" button or menu to load a PDF
3. The theme automatically matches your system settings

### System Requirements

- **Windows:** Windows 10 1809+ or Windows 11
- **macOS:** macOS 10.15 (Catalina) or later
- **Linux:** Ubuntu 20.04+, Fedora 36+, Debian 11+
- **Disk Space:** ~80 MB

## Opening PDF Files

### Method 1: File Menu

1. Click "Open" button in the toolbar
2. Or use menu: File → Open
3. Browse to your PDF file
4. Click "Open"

### Method 2: Command Line

**Windows:**
```powershell
FluentPDF.Avalonia.exe "C:\Documents\report.pdf"
```

**macOS:**
```bash
/Applications/FluentPDF.Avalonia.app/Contents/MacOS/FluentPDF.Avalonia ~/Documents/report.pdf
```

**Linux:**
```bash
FluentPDF.Avalonia ~/Documents/report.pdf
```

### Method 3: File Association

After setting FluentPDF as default PDF viewer:
- Double-click any PDF file
- Right-click → Open with → FluentPDF

### Supported File Types

- `.pdf` - Portable Document Format
- PDF versions: 1.0 through 2.0
- Encrypted PDFs with password protection

## Navigation

### Page Navigation

**Mouse:**
- Scroll up/down to navigate pages
- Click thumbnail to jump to page

**Keyboard:**
- `Page Up` / `Page Down` - Previous/next page
- `Home` - First page
- `End` - Last page
- `Arrow Up` / `Arrow Down` - Scroll within page

**Go To Page:**
1. Use menu: View → Go to Page
2. Enter page number
3. Press Enter

### Zoom Controls

**Mouse:**
- `Ctrl + Mouse Wheel` - Zoom in/out
- Double-click - Reset zoom to fit page

**Keyboard:**
- `Ctrl +` (or `Cmd +`) - Zoom in
- `Ctrl -` (or `Cmd -`) - Zoom out
- `Ctrl 0` (or `Cmd 0`) - Reset zoom to 100%

**Zoom Presets:**
- 50%, 75%, 100%, 125%, 150%, 200%, 400%
- Fit Width - Adjust to window width
- Fit Page - Fit entire page in window

### Pan and Scroll

- Click and drag to pan around zoomed pages
- Use scrollbars for precise positioning
- Middle mouse button drag for quick panning

## Viewing Options

### Display Modes

**Single Page:**
- View one page at a time
- Default mode for most documents

**Continuous Scroll:**
- Pages flow vertically
- Smooth scrolling between pages

**Two Page Spread:**
- View two pages side-by-side
- Ideal for reading books and magazines

**Toggle Modes:**
- Menu: View → Display Mode
- Keyboard: `Ctrl + 1` (Single), `Ctrl + 2` (Continuous), `Ctrl + 3` (Two Page)

### Rotation

Rotate pages for better viewing:
- `Ctrl + ]` - Rotate clockwise 90°
- `Ctrl + [` - Rotate counter-clockwise 90°
- Menu: View → Rotate

### Fullscreen

Distraction-free reading:
- Press `F11` to enter fullscreen
- Press `F11` or `Esc` to exit
- Mouse to top edge reveals menu bar

## Theme System

FluentPDF supports dark and light themes.

### Automatic Theme Detection

**Windows:**
- Respects Settings → Personalization → Colors → "Choose your mode"

**macOS:**
- Respects System Preferences → General → Appearance

**Linux:**
- Respects GTK theme (dark/light variant)

### Manual Theme Toggle

1. **Keyboard:** `Ctrl + T` (or `Cmd + T` on macOS)
2. **Menu:** Settings → Theme → Dark/Light
3. **System:** Settings → Theme → System Default

### Theme Customization

Advanced users can customize themes by editing:
- Windows: `%APPDATA%\FluentPDF\theme.json`
- macOS: `~/Library/Application Support/FluentPDF/theme.json`
- Linux: `~/.local/share/FluentPDF/theme.json`

## Keyboard Shortcuts

### File Operations

| Shortcut | Action |
|----------|--------|
| `Ctrl + O` (`Cmd + O`) | Open file |
| `Ctrl + W` (`Cmd + W`) | Close file |
| `Ctrl + Q` (`Cmd + Q`) | Quit application |
| `Ctrl + R` (`Cmd + R`) | Reload file |

### Navigation

| Shortcut | Action |
|----------|--------|
| `Page Up` | Previous page |
| `Page Down` | Next page |
| `Home` | First page |
| `End` | Last page |
| `Ctrl + G` (`Cmd + G`) | Go to page |

### View

| Shortcut | Action |
|----------|--------|
| `Ctrl + +` (`Cmd + +`) | Zoom in |
| `Ctrl + -` (`Cmd + -`) | Zoom out |
| `Ctrl + 0` (`Cmd + 0`) | Reset zoom |
| `Ctrl + 1` | Single page mode |
| `Ctrl + 2` | Continuous scroll mode |
| `Ctrl + 3` | Two page mode |
| `Ctrl + ]` | Rotate clockwise |
| `Ctrl + [` | Rotate counter-clockwise |
| `F11` | Toggle fullscreen |

### Interface

| Shortcut | Action |
|----------|--------|
| `Ctrl + T` (`Cmd + T`) | Toggle theme |
| `Ctrl + ,` (`Cmd + ,`) | Open settings |
| `F1` | Help |

## Command Line Usage

FluentPDF supports command-line arguments for automation and testing.

### Basic Usage

```bash
FluentPDF.Avalonia [OPTIONS] [FILE]
```

### Options

**Open File:**
```bash
FluentPDF.Avalonia document.pdf
```

**Start with Specific Page:**
```bash
FluentPDF.Avalonia --page 5 document.pdf
```

**Set Zoom Level:**
```bash
FluentPDF.Avalonia --zoom 150 document.pdf
```

**Force Theme:**
```bash
FluentPDF.Avalonia --theme dark document.pdf
FluentPDF.Avalonia --theme light document.pdf
```

**Fullscreen:**
```bash
FluentPDF.Avalonia --fullscreen document.pdf
```

**REST API Server (Experimental):**
```bash
FluentPDF.Avalonia --api-server --port 5000
```

### Examples

**Open presentation in fullscreen:**
```bash
FluentPDF.Avalonia --fullscreen --page 1 presentation.pdf
```

**Batch testing:**
```bash
for file in *.pdf; do
  FluentPDF.Avalonia "$file" --test-render
done
```

**Remote control:**
```bash
# Start API server
FluentPDF.Avalonia --api-server --port 8080 --headless

# In another terminal
curl http://localhost:8080/api/document/load -d '{"path":"test.pdf"}'
```

## Tips and Tricks

### Performance

**Large PDFs:**
- Use continuous scroll mode for smoother experience
- Disable thumbnails for files with >500 pages
- Close other applications to free memory

**Slow Loading:**
- PDFs with embedded fonts load slower
- Encrypted PDFs require decryption overhead
- Network PDFs: copy to local disk first

### Productivity

**Quick Navigation:**
- Create bookmarks for frequently accessed pages
- Use thumbnail sidebar for visual navigation
- Remember: `Ctrl + G` for quick page jump

**Reading Mode:**
- Use fullscreen (`F11`) for focused reading
- Two page mode for books and magazines
- Continuous scroll for reports and documents

**Customization:**
- Set your preferred default zoom level
- Choose default theme (dark/light)
- Configure keyboard shortcuts (future feature)

### Accessibility

**High Contrast:**
- Use dark theme for reduced eye strain
- Increase zoom for better readability

**Screen Readers:**
- FluentPDF supports text extraction
- Use arrow keys for page-by-page reading
- Future: full screen reader integration

## Troubleshooting

### Common Issues

**Application Won't Start:**
1. Check system requirements
2. Verify all files extracted correctly
3. Run from command line to see error messages
4. Check log files (see Installation Guide)

**PDF Won't Open:**
1. Verify file is valid PDF
2. Check file permissions
3. Try opening in another PDF viewer
4. Check for file corruption

**Slow Performance:**
1. Close other applications
2. Reduce zoom level
3. Switch to single page mode
4. Check available RAM
5. Disable thumbnails

**Theme Not Changing:**
1. Check system theme settings
2. Restart application
3. Manually toggle with `Ctrl + T`
4. Check theme configuration file

**Blurry Text:**
1. Reset zoom to 100%
2. Check display scaling settings
3. Update graphics drivers
4. Try different rendering mode

### Error Messages

**"PDFium library not found":**
- Ensure using self-contained build
- Verify pdfium.dll (Windows) / libpdfium.dylib (macOS) / libpdfium.so (Linux) present
- Reinstall application

**"File could not be loaded":**
- Check file exists and is accessible
- Verify file is valid PDF
- Check file permissions
- Try copying to different location

**"Out of memory":**
- Close other applications
- Reduce zoom level
- Split large PDF into smaller files

### Getting Help

**Log Files:**
- Windows: `%APPDATA%\FluentPDF\logs\`
- macOS: `~/Library/Logs/FluentPDF/`
- Linux: `~/.local/share/FluentPDF/logs/`

**Report Issues:**
1. Check existing issues on GitHub
2. Create new issue with:
   - OS and version
   - FluentPDF version
   - Steps to reproduce
   - Error messages
   - Log files (if applicable)

**Community:**
- GitHub Discussions for questions
- GitHub Issues for bugs and features

## Advanced Features

### REST API (Experimental)

For automation and testing:

```bash
# Start API server
FluentPDF.Avalonia --api-server --port 5000

# Health check
curl http://localhost:5000/api/health

# Load document
curl -X POST http://localhost:5000/api/document/load \
  -H "Content-Type: application/json" \
  -d '{"path":"/path/to/document.pdf"}'

# Render page
curl http://localhost:5000/api/render/{session-id}/0 -o page0.png
```

See `docs/verification-api.md` for full API documentation.

### Configuration File

Advanced settings in configuration file:

**Location:**
- Windows: `%APPDATA%\FluentPDF\config.json`
- macOS: `~/Library/Application Support/FluentPDF/config.json`
- Linux: `~/.local/share/FluentPDF/config.json`

**Example:**
```json
{
  "defaultZoom": 100,
  "theme": "system",
  "displayMode": "continuous",
  "showThumbnails": true,
  "maxCacheSize": 512,
  "renderQuality": "high"
}
```

## Feedback

We value your feedback!

**Feature Requests:**
- Open GitHub issue with `enhancement` label
- Describe use case and desired behavior

**Bug Reports:**
- Include reproduction steps
- Attach screenshots if relevant
- Share log files

**General Feedback:**
- GitHub Discussions
- Email: [your-email]

---

**Thank you for using FluentPDF!**

For more information:
- Website: https://github.com/yourusername/FluentPDF
- Documentation: `docs/`
- Release Notes: `RELEASE_NOTES.md`
