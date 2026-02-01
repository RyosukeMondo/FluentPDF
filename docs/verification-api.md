# FluentPDF Verification API

## Overview

The FluentPDF Verification API is a REST API server embedded in the FluentPDF application that enables autonomous testing of both UI elements and document operations. It provides comprehensive endpoints for verifying element properties, layout, user interactions, theme switching, annotations, forms, and document operations.

## Starting the API Server

### Command Line

```bash
# Start with default settings (port 5000, with UI)
FluentPDF.App.exe --api-server

# Start on custom port
FluentPDF.App.exe --api-server --port 8080

# Start in headless mode (no UI window)
FluentPDF.App.exe --api-server --headless

# Start with verbose logging
FluentPDF.App.exe --api-server --verbose
```

### Server Features

- **Swagger UI**: Interactive API documentation at `http://localhost:5000/`
- **CORS Support**: Enabled for localhost origins
- **Correlation IDs**: Automatic request tracing via `X-Correlation-Id` header
- **JSON Responses**: All responses use camelCase property naming

## API Endpoints

### Health & Status

#### `GET /api/health`

Check if the API server is running and PDFium is initialized.

**Response:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "pdfiumLoaded": true
}
```

**Status Codes:**
- `200 OK`: Server is healthy
- `503 Service Unavailable`: PDFium not initialized

#### `GET /api/status`

Get current application state.

**Response:**
```json
{
  "windowOpen": true,
  "documentLoaded": true,
  "documentPath": "C:/test.pdf",
  "currentPage": 3,
  "totalPages": 15,
  "zoomLevel": 100,
  "theme": "dark",
  "sidebars": {
    "thumbnails": true,
    "bookmarks": false
  }
}
```

### UI Element Verification

#### `POST /api/verify/element`

Verify that a UI element exists and has expected properties.

**Request:**
```json
{
  "automationId": "OpenFileButton",
  "expectedProperties": {
    "isEnabled": true,
    "isVisible": true,
    "width": {
      "min": 80,
      "max": 120
    },
    "height": {
      "min": 30,
      "max": 50
    }
  }
}
```

**Response:**
```json
{
  "found": true,
  "passed": true,
  "element": {
    "automationId": "OpenFileButton",
    "name": "Open",
    "isEnabled": true,
    "isVisible": true,
    "width": 100,
    "height": 40,
    "x": 10,
    "y": 10
  },
  "checks": [
    {
      "property": "isEnabled",
      "expected": true,
      "actual": true,
      "passed": true
    }
  ],
  "errors": []
}
```

#### `POST /api/verify/layout`

Verify the layout of multiple UI elements.

**Request:**
```json
{
  "elements": [
    {
      "automationId": "ThumbnailsPanel",
      "expectedPosition": "left",
      "expectedWidth": {
        "min": 150,
        "max": 250
      }
    },
    {
      "automationId": "ContentArea",
      "expectedWidth": {
        "min": 400
      }
    }
  ]
}
```

**Response:**
```json
{
  "passed": true,
  "elements": [
    {
      "automationId": "ThumbnailsPanel",
      "found": true,
      "position": {
        "x": 0,
        "y": 80,
        "width": 200,
        "height": 600
      },
      "checks": {
        "position": "left",
        "width": 200,
        "passed": true
      }
    }
  ],
  "errors": []
}
```

### UI Actions

#### `POST /api/action/click`

Simulate a click on a UI element.

**Request:**
```json
{
  "automationId": "OpenFileButton",
  "waitForDialog": true,
  "dialogAutomationId": "FileOpenDialog"
}
```

**Response:**
```json
{
  "success": true,
  "clicked": true,
  "dialogAppeared": true,
  "durationMs": 150,
  "errors": []
}
```

#### `POST /api/action/input`

Input text into a field.

**Request:**
```json
{
  "automationId": "SearchBox",
  "text": "test search",
  "clearFirst": true,
  "pressEnter": false
}
```

**Response:**
```json
{
  "success": true,
  "valueSet": true,
  "actualValue": "test search",
  "errors": []
}
```

#### `POST /api/action/navigate`

Navigate between pages.

**Request:**
```json
{
  "action": "nextPage",
  "expectedPage": 4
}
```

**Actions:** `nextPage`, `previousPage`, `firstPage`, `lastPage`

**Response:**
```json
{
  "success": true,
  "previousPage": 3,
  "currentPage": 4,
  "expectedPage": 4,
  "passed": true,
  "errors": []
}
```

### Theme Verification

#### `POST /api/verify/theme`

Switch theme and verify element backgrounds.

**Request:**
```json
{
  "setTheme": "dark",
  "verifyElements": [
    {
      "automationId": "MainGrid",
      "expectedBackground": "#1E1E1E"
    },
    {
      "automationId": "Toolbar",
      "expectedBackground": "#2D2D2D"
    }
  ]
}
```

**Themes:** `light`, `dark`, `system`

**Response:**
```json
{
  "themeSet": "dark",
  "passed": true,
  "elements": [
    {
      "automationId": "MainGrid",
      "background": "#1E1E1E",
      "expectedBackground": "#1E1E1E",
      "passed": true
    }
  ],
  "errors": []
}
```

### Annotation Verification

#### `POST /api/verify/annotation`

Create and verify annotations.

**Request:**
```json
{
  "tool": "highlight",
  "action": "create",
  "page": 0,
  "rect": [100, 100, 200, 120],
  "color": "#FFFF00",
  "verifyPresence": true
}
```

**Response:**
```json
{
  "success": true,
  "annotationCreated": true,
  "annotationId": "abc-123",
  "presence": {
    "verified": true,
    "found": true,
    "properties": {
      "type": "highlight",
      "color": "#FFFF00",
      "rect": [100, 100, 200, 120]
    }
  },
  "errors": []
}
```

### Form Verification

#### `POST /api/verify/form`

Fill and validate form fields.

**Request:**
```json
{
  "field": "NameField",
  "action": "fill",
  "value": "John Doe",
  "verifyValidation": true,
  "expectedValid": true
}
```

**Response:**
```json
{
  "success": true,
  "filled": true,
  "value": "John Doe",
  "validation": {
    "valid": true,
    "errors": []
  },
  "passed": true
}
```

### Document Operations

#### `POST /api/verify/merge`

Merge PDF documents and verify the result.

**Request:**
```json
{
  "files": ["file1.pdf", "file2.pdf"],
  "output": "merged.pdf",
  "verifyPageCount": true
}
```

**Response:**
```json
{
  "success": true,
  "merged": true,
  "output": "merged.pdf",
  "verification": {
    "expectedPages": 25,
    "actualPages": 25,
    "passed": true
  },
  "errors": []
}
```

## Usage Examples

### PowerShell

```powershell
# Health check
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/health"
Write-Host "Status: $($response.status)"

# Verify element
$body = @{
    automationId = "OpenFileButton"
    expectedProperties = @{
        isEnabled = $true
    }
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/verify/element" `
    -Method Post -Body $body -ContentType "application/json"

if ($response.passed) {
    Write-Host "✓ Element verification passed"
}
```

### Python

```python
import requests

# Health check
response = requests.get("http://localhost:5000/api/health")
print(f"Status: {response.json()['status']}")

# Verify element
payload = {
    "automationId": "OpenFileButton",
    "expectedProperties": {
        "isEnabled": True
    }
}

response = requests.post(
    "http://localhost:5000/api/verify/element",
    json=payload
)

if response.json()["passed"]:
    print("✓ Element verification passed")
```

### cURL

```bash
# Health check
curl http://localhost:5000/api/health

# Verify element
curl -X POST http://localhost:5000/api/verify/element \
  -H "Content-Type: application/json" \
  -d '{
    "automationId": "OpenFileButton",
    "expectedProperties": {
      "isEnabled": true
    }
  }'
```

## Error Handling

All endpoints return structured error responses with correlation IDs for tracing:

```json
{
  "error": "ELEMENT_NOT_FOUND",
  "message": "Element 'InvalidButton' not found",
  "correlationId": "abc123def456",
  "details": {}
}
```

### Common Error Codes

- `INVALID_REQUEST`: Invalid request parameters
- `ELEMENT_NOT_FOUND`: UI element not found by AutomationId
- `DOCUMENT_NOT_LOADED`: No document currently loaded
- `OPERATION_FAILED`: Operation failed to execute

## Automation Script

Use the included PowerShell script for automated UI testing:

```powershell
# Run all UI verification tests
.\tools\verify-ui.ps1

# Run against custom port
.\tools\verify-ui.ps1 -Port 8080
```

## Best Practices

1. **Start API Server First**: Always start the API server before running tests
2. **Use Correlation IDs**: Include `X-Correlation-Id` header for request tracing
3. **Check Status**: Verify application state with `/api/status` before tests
4. **AutomationIds**: Ensure all UI elements have AutomationIds set in XAML
5. **Error Handling**: Always check the `success` or `passed` fields in responses
6. **Cleanup**: Close document sessions when done to free resources

## Swagger UI

Access interactive API documentation at `http://localhost:5000/` when the server is running. The Swagger UI provides:

- Full API endpoint documentation
- Request/response schemas
- Interactive "Try it out" functionality
- Example requests and responses

## Performance

| Operation | Typical Duration |
|-----------|------------------|
| Health check | < 10ms |
| Element lookup | < 100ms |
| Layout verification | < 500ms |
| Navigation action | < 200ms |
| Theme switching | < 1000ms |
| Document merge | Variable (depends on file size) |

## Troubleshooting

### API Server Won't Start

- Check if port is already in use
- Verify PDFium DLLs are present in output directory
- Check logs in `%LocalAppData%/FluentPDF_Debug.log`

### Element Not Found

- Verify AutomationId is set in XAML
- Check element is visible (not collapsed)
- Ensure document is loaded if required
- Use Swagger UI to test requests interactively

### Theme Verification Fails

- Theme switching requires main window to be initialized
- Background colors may vary based on actual theme implementation
- Some elements may inherit backgrounds from parent containers
