Diagnose FluentPDF GUI issues using the REST API endpoints.

## Quick Diagnosis Steps

1. **Check if app is running**: `curl -s --max-time 3 http://localhost:5000/api/health`
   - If connection refused: app not running or crashed
   - If pdfiumLoaded=false: PDFium init failed

2. **Check GUI state**: `curl -s --max-time 3 http://localhost:5000/api/gui/state`
   - Inspect: tabCount, activeTab, viewer (currentPage, zoomLevel, isLoading, statusMessage)
   - If viewer is null: no document loaded
   - If isLoading=true: rendering stuck

3. **Open a test PDF**: `curl -s --max-time 20 -X POST http://localhost:5000/api/gui/open -H "Content-Type: application/json" -d '{"filePath":"C:/Users/ryosu/Downloads/Auction_Invitation_Fixed_Final.pdf"}'`
   - Check success, pageCount, statusMessage
   - If error contains "File not found": path encoding issue (use forward slashes)

4. **Take screenshot**: `curl -s -o screenshot.png http://localhost:5000/api/gui/screenshot`
   - Read the PNG to visually inspect the GUI
   - Check: thumbnails visible? bookmarks loaded? rendering quality?

5. **Test rendering API**: `POST /api/document/load` with `{"path":"..."}`, then `GET /api/render/{sessionId}/0` for page 0 PNG

## Common Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| Open endpoint hangs | Dispatcher deadlock | Use `Post()` + TCS pattern, not `InvokeAsync(async)` |
| App crashes on open | XAML type resolution | Check `x:DataType` and typed bindings like `((vm:Type)DataContext)` |
| Thumbnails empty | DataContext not set | Set `DataContext="{Binding Thumbnails}"` on ThumbnailsSidebar |
| Japanese paths 400 | Encoding issue | Use PowerShell with UTF-8 bytes or use the GUI Open button |
| Low DPI render | CalculateEffectiveDpi returns 96 | Check IDpiDetectionService registration |

## API Endpoints Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| /api/health | GET | Health + PDFium status |
| /api/gui/state | GET | Full UI state JSON |
| /api/gui/open | POST | Open PDF by path |
| /api/gui/navigate | POST | Page navigation |
| /api/gui/zoom | POST | Zoom control |
| /api/gui/close-tab | POST | Close active tab |
| /api/gui/screenshot | GET | PNG screenshot |
| /api/document/load | POST | Load doc for render API |
| /api/render/{id}/{page} | GET | Render page to PNG |
| /api/verify/render | POST | Verify render hash |
