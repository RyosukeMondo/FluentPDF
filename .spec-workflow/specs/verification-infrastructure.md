# Verification Infrastructure Specification

**Status**: Draft
**Priority**: Critical
**Phase**: Foundation
**Dependencies**: None

## Overview

This specification defines the autonomous verification infrastructure for FluentPDF, enabling comprehensive testing of both business logic and UI functionality without human interaction. The infrastructure consists of two main components:

1. **CLI Verification Commands**: Command-line tools for testing business logic
2. **REST API Server**: HTTP endpoints for testing UI layout, behavior, and interactions

## Goals

- **100% Autonomous Verification**: Every feature must be testable without human UAT
- **Deterministic Results**: Tests produce consistent, repeatable results
- **Comprehensive Coverage**: Test business logic, UI layout, user interactions, and edge cases
- **CI/CD Integration**: Exit codes and JSON reports suitable for automated pipelines
- **Developer Productivity**: Fast feedback loops for development

## CLI Verification Commands

### Architecture

```
FluentPDF.App.exe --verify-command [options]
  ↓
  DiagnosticCommandHandler
  ↓
  BusinessLogicVerifier (headless, no UI)
  ↓
  JSON Report + Exit Code
```

### Command Specification

#### Global Options

| Option | Type | Description |
|--------|------|-------------|
| `--verbose` | flag | Enable detailed logging |
| `--output` | path | Output directory for reports (default: current dir) |
| `--format` | enum | Output format: json, junit, trx (default: json) |
| `--timeout` | int | Timeout in seconds (default: 300) |

#### --test-merge

**Purpose**: Verify PDF merge functionality

**Syntax**:
```bash
FluentPDF.App.exe --test-merge --input "file1.pdf;file2.pdf;file3.pdf" --output "merged.pdf"
```

**Options**:
- `--input`: Semicolon-separated list of PDF files to merge
- `--output`: Output merged PDF path
- `--verify-structure`: Validate merged PDF structure with QPDF
- `--verify-pages`: Confirm page count matches sum of inputs

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Merge operation failed
- 3: Verification failed (page count mismatch)
- 4: Structure validation failed

**JSON Report Schema**:
```json
{
  "command": "test-merge",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1234,
  "status": "pass|fail|error",
  "input": {
    "files": ["file1.pdf", "file2.pdf"],
    "totalPages": 25
  },
  "output": {
    "file": "merged.pdf",
    "pages": 25,
    "fileSizeBytes": 1048576
  },
  "validation": {
    "structureValid": true,
    "pageCountMatch": true,
    "qpdfErrors": []
  },
  "errors": []
}
```

#### --test-split

**Purpose**: Verify PDF split functionality

**Syntax**:
```bash
FluentPDF.App.exe --test-split --input "document.pdf" --ranges "1-5;6-10;11-15" --output "output_dir"
```

**Options**:
- `--input`: Input PDF file
- `--ranges`: Semicolon-separated page ranges
- `--output`: Output directory for split files
- `--verify-structure`: Validate each split PDF with QPDF

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Split operation failed
- 3: Invalid page ranges
- 4: Structure validation failed

**JSON Report Schema**:
```json
{
  "command": "test-split",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1234,
  "status": "pass",
  "input": {
    "file": "document.pdf",
    "totalPages": 15
  },
  "ranges": ["1-5", "6-10", "11-15"],
  "output": [
    {
      "file": "output_dir/document_1.pdf",
      "pages": 5,
      "valid": true
    },
    {
      "file": "output_dir/document_2.pdf",
      "pages": 5,
      "valid": true
    },
    {
      "file": "output_dir/document_3.pdf",
      "pages": 5,
      "valid": true
    }
  ],
  "errors": []
}
```

#### --test-optimize

**Purpose**: Verify PDF optimization functionality

**Syntax**:
```bash
FluentPDF.App.exe --test-optimize --input "large.pdf" --output "optimized.pdf"
```

**Options**:
- `--input`: Input PDF file
- `--output`: Output optimized PDF path
- `--min-reduction`: Minimum file size reduction percentage (default: 10)
- `--verify-visual`: Render both PDFs and compare with SSIM (default: false)

**Exit Codes**:
- 0: Success (met reduction threshold)
- 1: Input file not found
- 2: Optimization failed
- 3: Insufficient file size reduction
- 4: Visual regression detected

**JSON Report Schema**:
```json
{
  "command": "test-optimize",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 2345,
  "status": "pass",
  "input": {
    "file": "large.pdf",
    "fileSizeBytes": 5242880,
    "pages": 50
  },
  "output": {
    "file": "optimized.pdf",
    "fileSizeBytes": 2097152,
    "pages": 50
  },
  "metrics": {
    "reductionPercent": 60.0,
    "minReductionThreshold": 10.0,
    "meetsThreshold": true
  },
  "visualRegression": {
    "enabled": false,
    "ssimScore": null
  },
  "errors": []
}
```

#### --test-watermark

**Purpose**: Verify watermark application

**Syntax**:
```bash
FluentPDF.App.exe --test-watermark --input "doc.pdf" --text "CONFIDENTIAL" --output "watermarked.pdf"
```

**Options**:
- `--input`: Input PDF file
- `--output`: Output watermarked PDF path
- `--text`: Watermark text
- `--image`: Watermark image path (alternative to --text)
- `--position`: center|tl|tr|bl|br (default: center)
- `--opacity`: 0-100 (default: 50)
- `--pages`: all|range (default: all)
- `--verify-visual`: Render and verify watermark present

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Watermark operation failed
- 3: Verification failed (watermark not detected)

**JSON Report Schema**:
```json
{
  "command": "test-watermark",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1500,
  "status": "pass",
  "input": {
    "file": "doc.pdf",
    "pages": 10
  },
  "watermark": {
    "type": "text",
    "content": "CONFIDENTIAL",
    "position": "center",
    "opacity": 50,
    "appliedToPages": "all"
  },
  "output": {
    "file": "watermarked.pdf",
    "pages": 10
  },
  "verification": {
    "visualCheckEnabled": true,
    "watermarkDetected": true
  },
  "errors": []
}
```

#### --test-annotations

**Purpose**: Verify annotation persistence

**Syntax**:
```bash
FluentPDF.App.exe --test-annotations --input "doc.pdf" --annotations "annotations.json" --output "annotated.pdf"
```

**Options**:
- `--input`: Input PDF file
- `--annotations`: JSON file with annotation definitions
- `--output`: Output PDF with annotations
- `--verify-persistence`: Reopen PDF and verify annotations present

**Annotation JSON Format**:
```json
{
  "annotations": [
    {
      "type": "highlight",
      "page": 0,
      "rect": [100, 100, 200, 120],
      "color": "#FFFF00",
      "opacity": 0.5
    },
    {
      "type": "text",
      "page": 1,
      "rect": [50, 50, 150, 100],
      "content": "Review this section"
    }
  ]
}
```

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Annotation application failed
- 3: Persistence verification failed

**JSON Report Schema**:
```json
{
  "command": "test-annotations",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1800,
  "status": "pass",
  "input": {
    "file": "doc.pdf",
    "pages": 5
  },
  "annotations": {
    "count": 2,
    "types": ["highlight", "text"]
  },
  "output": {
    "file": "annotated.pdf"
  },
  "persistence": {
    "verified": true,
    "annotationsRecovered": 2
  },
  "errors": []
}
```

#### --test-forms

**Purpose**: Verify form filling and validation

**Syntax**:
```bash
FluentPDF.App.exe --test-forms --input "form.pdf" --data "form-data.json" --output "filled.pdf"
```

**Options**:
- `--input`: Input PDF with form fields
- `--data`: JSON file with form field values
- `--output`: Output filled PDF
- `--verify-validation`: Test validation rules
- `--verify-persistence`: Reopen and verify field values

**Form Data JSON Format**:
```json
{
  "fields": {
    "name": "John Doe",
    "email": "john@example.com",
    "subscribe": true,
    "gender": "male"
  }
}
```

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Form fill failed
- 3: Validation failed
- 4: Persistence verification failed

**JSON Report Schema**:
```json
{
  "command": "test-forms",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1200,
  "status": "pass",
  "input": {
    "file": "form.pdf",
    "fieldCount": 4
  },
  "data": {
    "fieldsProvided": 4,
    "fieldsFilled": 4
  },
  "validation": {
    "enabled": true,
    "passed": true,
    "errors": []
  },
  "persistence": {
    "verified": true,
    "fieldsRecovered": 4,
    "valuesMatch": true
  },
  "errors": []
}
```

#### --test-conversion

**Purpose**: Verify DOCX to PDF conversion

**Syntax**:
```bash
FluentPDF.App.exe --test-conversion --input "document.docx" --output "converted.pdf"
```

**Options**:
- `--input`: Input DOCX file
- `--output`: Output PDF path
- `--verify-pages`: Minimum expected page count
- `--verify-structure`: Validate PDF structure with QPDF

**Exit Codes**:
- 0: Success
- 1: Input file not found
- 2: Conversion failed
- 3: Verification failed

**JSON Report Schema**:
```json
{
  "command": "test-conversion",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 3000,
  "status": "pass",
  "input": {
    "file": "document.docx",
    "fileSizeBytes": 102400
  },
  "output": {
    "file": "converted.pdf",
    "pages": 5,
    "fileSizeBytes": 204800
  },
  "verification": {
    "structureValid": true
  },
  "errors": []
}
```

## REST API Server

### Architecture

```
HTTP Request
  ↓
REST API Controller
  ↓
UI Automation (via Dispatcher)
  ↓
Element Inspector / Action Executor
  ↓
JSON Response
```

### Server Configuration

**Start Command**:
```bash
FluentPDF.App.exe --api-server --port 5000 --headless
```

**Options**:
- `--port`: HTTP port (default: 5000)
- `--headless`: Run without visible UI (default: false)
- `--allow-origins`: CORS allowed origins (default: *)
- `--timeout`: Request timeout in seconds (default: 30)

### Endpoint Specification

#### Health & Status

##### GET /api/health

**Purpose**: Verify API server is running

**Response**:
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "pdfium": "v129.0.6668.0",
  "uptime": 3600,
  "timestamp": "2026-01-25T10:30:00Z"
}
```

##### GET /api/status

**Purpose**: Get application state

**Response**:
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

#### UI Element Verification

##### POST /api/verify/element

**Purpose**: Verify UI element exists and has expected properties

**Request**:
```json
{
  "automationId": "OpenFileButton",
  "expectedProperties": {
    "isEnabled": true,
    "isVisible": true,
    "width": { "min": 80, "max": 120 },
    "height": { "min": 30, "max": 50 }
  }
}
```

**Response**:
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
    },
    {
      "property": "isVisible",
      "expected": true,
      "actual": true,
      "passed": true
    },
    {
      "property": "width",
      "expected": { "min": 80, "max": 120 },
      "actual": 100,
      "passed": true
    }
  ],
  "errors": []
}
```

##### POST /api/verify/layout

**Purpose**: Verify multiple elements' positions and layout

**Request**:
```json
{
  "elements": [
    {
      "automationId": "ThumbnailsPanel",
      "expectedPosition": "left",
      "expectedWidth": { "min": 150, "max": 250 }
    },
    {
      "automationId": "BookmarksPanel",
      "expectedPosition": "left-of:ContentArea"
    },
    {
      "automationId": "ContentArea",
      "expectedWidth": { "min": 400 }
    }
  ]
}
```

**Response**:
```json
{
  "passed": true,
  "elements": [
    {
      "automationId": "ThumbnailsPanel",
      "found": true,
      "position": { "x": 0, "y": 80, "width": 200, "height": 600 },
      "checks": {
        "position": "left",
        "width": 200,
        "passed": true
      }
    },
    {
      "automationId": "BookmarksPanel",
      "found": true,
      "position": { "x": 200, "y": 80, "width": 250, "height": 600 },
      "checks": {
        "relativeTo": "ContentArea",
        "relation": "left-of",
        "passed": true
      }
    }
  ],
  "errors": []
}
```

#### UI Interaction Testing

##### POST /api/action/click

**Purpose**: Click a UI element and verify result

**Request**:
```json
{
  "automationId": "OpenFileButton",
  "waitForDialog": true,
  "dialogAutomationId": "FileOpenDialog"
}
```

**Response**:
```json
{
  "success": true,
  "clicked": true,
  "dialogAppeared": true,
  "durationMs": 150,
  "errors": []
}
```

##### POST /api/action/input

**Purpose**: Input text into a field

**Request**:
```json
{
  "automationId": "SearchBox",
  "text": "test search",
  "clearFirst": true,
  "pressEnter": false
}
```

**Response**:
```json
{
  "success": true,
  "valueSet": true,
  "actualValue": "test search",
  "errors": []
}
```

##### POST /api/action/navigate

**Purpose**: Navigate pages and verify

**Request**:
```json
{
  "action": "nextPage",
  "expectedPage": 4
}
```

**Response**:
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

#### Theme & Appearance Testing

##### POST /api/verify/theme

**Purpose**: Verify theme switching

**Request**:
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

**Response**:
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

#### Annotation Testing

##### POST /api/verify/annotation

**Purpose**: Verify annotation tool behavior

**Request**:
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

**Response**:
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

#### Form Testing

##### POST /api/verify/form

**Purpose**: Verify form field interaction

**Request**:
```json
{
  "field": "NameField",
  "action": "fill",
  "value": "John Doe",
  "verifyValidation": true,
  "expectedValid": true
}
```

**Response**:
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

#### Document Operations

##### POST /api/verify/merge

**Purpose**: Verify merge operation via UI

**Request**:
```json
{
  "files": ["file1.pdf", "file2.pdf"],
  "output": "merged.pdf",
  "verifyPageCount": true
}
```

**Response**:
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

## Implementation Details

### Technology Stack

**CLI Commands**:
- C# Console handlers in FluentPDF.App/Diagnostics/
- Headless operation (no UI dependencies)
- JSON serialization with System.Text.Json
- Exit code handling

**REST API**:
- ASP.NET Core minimal APIs
- Hosted in WinUI 3 app via Microsoft.Extensions.Hosting
- UI Automation via DispatcherQueue
- CORS enabled for localhost testing
- Swagger/OpenAPI documentation

### Directory Structure

```
src/FluentPDF.App/
├── Diagnostics/
│   ├── DiagnosticCommandHandler.cs          # Entry point
│   ├── Commands/
│   │   ├── TestMergeCommand.cs
│   │   ├── TestSplitCommand.cs
│   │   ├── TestOptimizeCommand.cs
│   │   ├── TestWatermarkCommand.cs
│   │   ├── TestAnnotationsCommand.cs
│   │   ├── TestFormsCommand.cs
│   │   └── TestConversionCommand.cs
│   └── Models/
│       ├── CommandResult.cs
│       └── VerificationReport.cs
├── Api/
│   ├── VerificationApiServer.cs             # ASP.NET Core host
│   ├── Controllers/
│   │   ├── HealthController.cs
│   │   ├── StatusController.cs
│   │   ├── ElementController.cs
│   │   ├── ActionController.cs
│   │   ├── ThemeController.cs
│   │   ├── AnnotationController.cs
│   │   ├── FormController.cs
│   │   └── DocumentController.cs
│   ├── Services/
│   │   ├── UiAutomationService.cs          # UI inspection
│   │   └── ElementVerificationService.cs   # Property checks
│   └── Models/
│       ├── ElementVerificationRequest.cs
│       ├── LayoutVerificationRequest.cs
│       └── VerificationResponse.cs
```

### Error Handling

**CLI Commands**:
- All exceptions caught and converted to error exit codes
- Structured error messages in JSON reports
- Correlation IDs for debugging
- Log files in output directory

**REST API**:
- Global exception middleware
- Standardized error responses
- 500 for server errors, 400 for validation errors
- Detailed error messages in response body

### Performance Requirements

| Operation | Max Duration |
|-----------|--------------|
| CLI command execution | 5 minutes |
| API request processing | 30 seconds |
| UI element lookup | 5 seconds |
| UI action execution | 10 seconds |

### Security Considerations

**CLI**:
- Input validation for file paths
- No shell command injection
- Sandboxed file operations

**REST API**:
- CORS restricted to localhost by default
- No authentication (local-only use)
- Rate limiting (100 requests/minute)
- Input sanitization

## Testing the Verification Infrastructure

### Self-Test Commands

```bash
# Test CLI infrastructure
FluentPDF.App.exe --test-cli-infrastructure

# Test REST API infrastructure
FluentPDF.App.exe --test-api-infrastructure
```

### Integration Tests

```csharp
[Fact]
public async Task TestMergeCommand_WithValidInputs_ReturnsSuccess()
{
    var result = await DiagnosticCommandHandler.ExecuteAsync(
        "test-merge",
        new[] { "--input", "file1.pdf;file2.pdf", "--output", "merged.pdf" }
    );

    Assert.Equal(0, result.ExitCode);
    Assert.Equal("pass", result.Report.Status);
}

[Fact]
public async Task VerifyElement_WithValidAutomationId_ReturnsElement()
{
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    var request = new ElementVerificationRequest
    {
        AutomationId = "OpenFileButton",
        ExpectedProperties = new { IsEnabled = true }
    };

    var response = await client.PostAsJsonAsync("/api/verify/element", request);
    var result = await response.Content.ReadFromJsonAsync<ElementVerificationResponse>();

    Assert.True(result.Found);
    Assert.True(result.Passed);
}
```

## Future Enhancements

1. **Video Recording**: Record UI interactions for failed tests
2. **Screenshot Capture**: Automatic screenshots on failures
3. **Performance Profiling**: Built-in performance measurement endpoints
4. **Parallel Execution**: Run multiple tests concurrently
5. **Test Replay**: Save and replay UI interaction sequences
6. **Visual Regression**: SSIM-based visual comparison in API
