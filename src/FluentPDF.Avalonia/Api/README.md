# FluentPDF Avalonia Verification API

## Status: ✅ IMPLEMENTED

Complete REST API server for autonomous testing and verification of PDF document operations.

## Architecture

```
FluentPDF.Avalonia.exe
├── VerificationApiServer (ASP.NET Core)
│   ├── Kestrel HTTP Server
│   └── JSON API
├── Services
│   ├── DocumentSessionManager (Thread-safe session management)
│   └── HashingService (SHA256 hashing)
└── Endpoints
    ├── HealthEndpoints (/api/health)
    ├── DocumentEndpoints (/api/document/*)
    ├── RenderEndpoints (/api/render)
    └── VerifyEndpoints (/api/verify/*)
```

## Starting the Server

```bash
# Default (port 5000, with UI)
FluentPDF.Avalonia.exe --api-server

# Custom port
FluentPDF.Avalonia.exe --api-server --port 8080

# Headless mode (no UI)
FluentPDF.Avalonia.exe --api-server --headless

# With verbose logging
FluentPDF.Avalonia.exe --api-server --verbose
```

## API Endpoints

### Health Check

**GET /api/health**

Returns server health status.

Response:
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "pdfiumLoaded": true,
  "activeSessions": 0,
  "timestamp": "2025-01-28T10:00:00Z"
}
```

### Document Management

**POST /api/document/load**

Load a PDF document and create a session.

Request:
```json
{
  "path": "C:/path/to/document.pdf",
  "password": "optional"
}
```

Response:
```json
{
  "documentId": "abc123...",
  "pageCount": 10,
  "metadata": {
    "title": null,
    "author": null,
    "width": 612.0,
    "height": 792.0
  }
}
```

**GET /api/document/{documentId}**

Get information about a loaded document.

**DELETE /api/document/{documentId}**

Close a document session and release resources.

### Rendering

**POST /api/render**

Render a page to PNG.

Request:
```json
{
  "documentId": "abc123...",
  "pageIndex": 0,
  "dpi": 96,
  "zoom": 1.0
}
```

Response: PNG image (image/png)

**GET /api/render/{documentId}/{pageIndex}?dpi=96&zoom=1.0**

Convenience endpoint for rendering a page.

### Verification

**POST /api/verify/render**

Verify a rendered page against a baseline hash.

Request:
```json
{
  "documentId": "abc123...",
  "pageIndex": 0,
  "baselineHash": "optional-sha256-hash",
  "dpi": 96
}
```

Response:
```json
{
  "match": true,
  "hash": "abc123...",
  "ssim": 1.0,
  "diffUrl": null
}
```

**POST /api/verify/batch**

Verify multiple pages at once.

## Error Responses

All endpoints return structured error responses:

```json
{
  "error": "ERROR_CODE",
  "message": "Human-readable message",
  "correlationId": "trace-id",
  "details": { }
}
```

## Testing

```powershell
# Run comprehensive API tests
.\tools\test-avalonia-api.ps1
```

## Implementation Files

- `VerificationApiServer.cs` - Main API server
- `Models/ApiModels.cs` - Request/response DTOs
- `Services/DocumentSessionManager.cs` - Session management
- `Services/HashingService.cs` - SHA256 hashing
- `Endpoints/HealthEndpoints.cs` - Health check
- `Endpoints/DocumentEndpoints.cs` - Document operations
- `Endpoints/RenderEndpoints.cs` - Page rendering
- `Endpoints/VerifyEndpoints.cs` - Hash verification

## Features

✅ Thread-safe document sessions
✅ SHA256 hash verification
✅ Parallel batch verification
✅ Correlation ID tracking
✅ Performance headers
✅ Structured error responses
✅ Headless mode
✅ Custom port binding

## Differences from WinUI 3 API

**Included:**
- ✅ Health check
- ✅ Document loading/closing
- ✅ Page rendering
- ✅ Hash verification
- ✅ Batch verification

**Not Included:**
- ❌ UI element verification (no UI automation)
- ❌ Theme verification
- ❌ Annotation verification
- ❌ Form verification
- ❌ Swagger UI

The Avalonia API focuses on document operations only. For UI verification, use the WinUI 3 version.
