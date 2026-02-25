Run autonomous UAT for FluentPDF via the REST API.

## Prerequisites
- Build: `dotnet build src/FluentPDF.Avalonia -c Debug`
- Kill existing: `taskkill //F //IM FluentPDF.Avalonia.exe` (or dotnet.exe)
- Launch: `start "" "path/to/FluentPDF.Avalonia.exe"` and wait 8 seconds

## API Base URL
`http://localhost:5000`

## Test Sequence
Execute these steps in order. If any step fails, report the error and continue.

1. **Health Check**: `GET /api/health` - Verify pdfiumLoaded=true
2. **Initial State**: `GET /api/gui/state` - Verify tabCount=0
3. **Open PDF**: `POST /api/gui/open` with `{"filePath":"C:/Users/ryosu/Downloads/Auction_Invitation_Fixed_Final.pdf"}` - Verify success=true, pageCount>0
4. **Verify State**: `GET /api/gui/state` - Verify viewer.hasDocument=true, viewer.currentPage=1
5. **Navigate**: `POST /api/gui/navigate` with `{"page":2}` - Verify currentPage=2
6. **Navigate Next**: `POST /api/gui/navigate` with `{"action":"next"}` - Verify page incremented
7. **Navigate Previous**: `POST /api/gui/navigate` with `{"action":"previous"}` - Verify page decremented
8. **Zoom In**: `POST /api/gui/zoom` with `{"action":"in"}` - Verify zoomLevel > 1.0
9. **Set Zoom**: `POST /api/gui/zoom` with `{"level":1.5}` - Verify zoomLevel=1.5
10. **Screenshot**: `GET /api/gui/screenshot` - Save PNG, verify file size > 0
11. **View Screenshot**: Read the saved PNG to visually verify rendering quality, thumbnails, bookmarks
12. **Close Tab**: `POST /api/gui/close-tab` - Verify remainingTabs=0
13. **Final State**: `GET /api/gui/state` - Verify clean state

## Important Notes
- Use forward slashes in file paths: `C:/Users/...` not `C:\Users\...`
- For Japanese filenames, use PowerShell with UTF-8 encoding
- All async GUI endpoints use `Dispatcher.UIThread.Post` pattern
- The API server auto-starts on port 5000 in DEBUG builds
- `curl -s --max-time 20` for open/navigate, `--max-time 5` for state/health
