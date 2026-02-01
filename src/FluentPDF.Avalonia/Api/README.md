# FluentPDF Verification API

## Overview

The FluentPDF Verification API is a comprehensive REST API server embedded within the FluentPDF WinUI 3 application. It enables autonomous testing and verification of both UI elements and document operations without requiring manual user interaction.

## Architecture

```
FluentPDF.App.exe (WinUI 3)
├── VerificationApiServer (ASP.NET Core)
│   ├── Kestrel HTTP Server
│   ├── Swagger/OpenAPI Documentation
│   └── CORS Support
├── Services
│   ├── UiAutomationService (DispatcherQueue-based UI inspection)
│   ├── DocumentSessionManager
│   └── HashingService
└── Endpoints
    ├── HealthEndpoints (/api/health, /api/status)
    ├── ElementEndpoints (/api/verify/element, /api/verify/layout)
    ├── ActionEndpoints (/api/action/click, /api/action/input, /api/action/navigate)
    ├── ThemeEndpoints (/api/verify/theme)
    ├── AnnotationEndpoints (/api/verify/annotation)
    ├── FormEndpoints (/api/verify/form)
    ├── DocumentEndpoints (/api/document/*, /api/verify/merge)
    ├── RenderEndpoints (/api/render)
    └── VerifyEndpoints (/api/verify/render, /api/verify/batch)
```

## Key Components

### 1. VerificationApiServer.cs

Main API server implementation that:
- Hosts ASP.NET Core minimal APIs within WinUI 3 application
- Configures Kestrel to listen on specified port
- Registers all services and endpoints
- Provides Swagger UI at root URL
- Enables CORS for localhost testing
- Adds correlation ID middleware for request tracing

### 2. UiAutomationService.cs

Service for UI element inspection and interaction:
- Finds elements by AutomationId using visual tree traversal
- Verifies element properties (enabled, visible, dimensions)
- Validates layout and positioning
- Retrieves application status
- Gets element background colors for theme verification
- All operations marshaled to UI thread via DispatcherQueue

### 3. Endpoint Controllers

#### HealthEndpoints
- `GET /api/health` - Server health check
- `GET /api/status` - Application state (document loaded, current page, theme, etc.)

#### ElementEndpoints
- `POST /api/verify/element` - Verify element exists with expected properties
- `POST /api/verify/layout` - Verify layout of multiple elements

#### ActionEndpoints
- `POST /api/action/click` - Simulate click on element
- `POST /api/action/input` - Input text into field
- `POST /api/action/navigate` - Navigate pages (next, previous, first, last)

#### ThemeEndpoints
- `POST /api/verify/theme` - Switch theme and verify element backgrounds

#### AnnotationEndpoints
- `POST /api/verify/annotation` - Create annotation and verify presence

#### FormEndpoints
- `POST /api/verify/form` - Fill form field and validate

#### DocumentEndpoints
- `POST /api/document/load` - Load PDF document
- `GET /api/document/{id}` - Get document info
- `DELETE /api/document/{id}` - Close document
- `POST /api/verify/merge` - Merge PDFs and verify result

#### RenderEndpoints
- `POST /api/render` - Render page to PNG
- `GET /api/render/{id}/{page}` - Render specific page

#### VerifyEndpoints
- `POST /api/verify/render` - Verify page rendering with hash
- `POST /api/verify/batch` - Batch verify multiple pages

### 4. Models (ApiModels.cs)

Complete set of request/response DTOs:
- Element verification models
- Layout verification models
- Action models (click, input, navigate)
- Theme verification models
- Annotation verification models
- Form verification models
- Document operation models
- All models use record types for immutability

## Starting the Server

```bash
# Default (port 5000, with UI)
FluentPDF.App.exe --api-server

# Custom port
FluentPDF.App.exe --api-server --port 8080

# Headless mode (no UI)
FluentPDF.App.exe --api-server --headless

# With verbose logging
FluentPDF.App.exe --api-server --verbose
```

## Features

### Swagger UI
Interactive API documentation at `http://localhost:5000/`:
- Full endpoint documentation
- Request/response schemas
- "Try it out" functionality
- Example requests

### CORS Support
Configured for localhost origins to enable testing from:
- Browser-based testing tools
- Node.js test scripts
- Python/PowerShell automation scripts

### Correlation IDs
Automatic request tracing:
- Generated for each request if not provided
- Included in response headers
- Logged with all operations
- Useful for debugging failures

### Thread Safety
All UI operations properly marshaled:
- UI element inspection via DispatcherQueue
- ViewModel access on UI thread
- Document operations synchronized
- No cross-thread access violations

## Testing

### PowerShell Script
```powershell
.\tools\verify-ui.ps1
```

Runs comprehensive UI verification tests:
- Health check
- Application status
- Element verification
- Layout verification
- Navigation testing
- Theme switching

### Manual Testing
```bash
# Health check
curl http://localhost:5000/api/health

# Verify element
curl -X POST http://localhost:5000/api/verify/element \
  -H "Content-Type: application/json" \
  -d '{"automationId":"OpenFileButton","expectedProperties":{"isEnabled":true}}'
```

## Implementation Notes

### Design Decisions

1. **ASP.NET Core over Custom HTTP Server**
   - Leverages mature, well-tested framework
   - Built-in JSON serialization
   - Middleware pipeline support
   - Swagger/OpenAPI integration

2. **DispatcherQueue for UI Access**
   - Required for WinUI 3 thread affinity
   - TaskCompletionSource pattern for async/await
   - Prevents cross-thread exceptions

3. **Minimal APIs over Controllers**
   - Simpler, more concise code
   - Better performance
   - Easier to maintain
   - Less boilerplate

4. **Record Types for DTOs**
   - Immutable by default
   - Value-based equality
   - Concise syntax
   - Better for serialization

### Limitations

1. **Element Lookup**
   - Only finds elements by AutomationId or Name
   - Requires visual tree traversal (slower for deep trees)
   - Cannot find collapsed elements

2. **Action Simulation**
   - Click/input actions simplified in current implementation
   - Would need UI Automation framework for full simulation
   - Some actions may not trigger all event handlers

3. **Theme Verification**
   - Background color detection limited to Panel/Control types
   - May not work for all element types
   - Requires solid color brushes

4. **Single Window**
   - Assumes single main window
   - Dialogs must be found via visual tree
   - No support for multiple top-level windows

## Future Enhancements

1. **Video Recording**: Capture UI interactions for failed tests
2. **Screenshot Capture**: Automatic screenshots on failures
3. **Performance Profiling**: Built-in performance measurement
4. **Test Replay**: Save and replay interaction sequences
5. **Visual Regression**: SSIM-based visual comparison
6. **Parallel Testing**: Run multiple tests concurrently
7. **Extended Automation**: Full UI Automation framework integration
8. **Multi-Window Support**: Handle dialogs and child windows

## Dependencies

- `Microsoft.AspNetCore.App` (framework reference)
- `Swashbuckle.AspNetCore` (Swagger/OpenAPI)
- `Microsoft.Extensions.Hosting`
- `Microsoft.UI.Xaml` (WinUI 3)

## Documentation

- [Verification API Guide](../../../docs/verification-api.md) - Complete API reference
- [Verification Infrastructure Spec](../../../.spec-workflow/specs/verification-infrastructure.md) - Requirements
