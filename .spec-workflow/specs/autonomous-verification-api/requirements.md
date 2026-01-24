# Requirements Document: Autonomous Verification API

## Introduction

This feature provides a REST API embedded in FluentPDF.App that enables autonomous, CLI-driven verification of PDF rendering functionality without requiring User Acceptance Testing (UAT) or manual UI interaction. The API allows agents and CI systems to programmatically verify that PDF pages render correctly, enabling ultrafast iteration cycles.

## Alignment with Product Vision

This directly supports the steering document goals:
- **Verifiable Architecture**: Built-in validation mechanisms ensure every PDF renders correctly
- **AI-Driven Quality Assurance**: Continuous self-assessment system that monitors application health
- **Performance**: Automated verification without human bottlenecks

## Requirements

### REQ-1: Health Check Endpoint

**User Story:** As a CI system, I want to check if the rendering service is operational, so that I can skip tests if the service is unavailable.

#### Acceptance Criteria

1. WHEN GET /api/health is called THEN system SHALL respond with 200 OK and JSON `{"status": "healthy", "version": "X.X.X"}`
2. IF PDFium library is not loaded THEN system SHALL respond with 503 Service Unavailable and `{"status": "unhealthy", "error": "PDFium not initialized"}`
3. WHEN GET /api/health is called THEN system SHALL respond within 100ms

### REQ-2: Document Loading Endpoint

**User Story:** As a test automation tool, I want to load a PDF document via API, so that I can verify the loading pipeline works correctly.

#### Acceptance Criteria

1. WHEN POST /api/document/load with `{"path": "/path/to/file.pdf"}` is called THEN system SHALL load the document and respond with `{"documentId": "uuid", "pageCount": N, "metadata": {...}}`
2. IF file does not exist THEN system SHALL respond with 404 and `{"error": "PDF_FILE_NOT_FOUND", "message": "..."}`
3. IF file is corrupted THEN system SHALL respond with 422 and `{"error": "PDF_CORRUPTED", "message": "..."}`
4. WHEN document requires password AND password not provided THEN system SHALL respond with 401 and `{"error": "PDF_REQUIRES_PASSWORD"}`

### REQ-3: Page Rendering Endpoint

**User Story:** As a verification agent, I want to render a specific page to PNG via API, so that I can verify rendering works without UI.

#### Acceptance Criteria

1. WHEN POST /api/render with `{"documentId": "uuid", "pageIndex": 0, "dpi": 96}` is called THEN system SHALL render the page and respond with PNG binary (Content-Type: image/png)
2. IF documentId is invalid THEN system SHALL respond with 404 and `{"error": "DOCUMENT_NOT_FOUND"}`
3. IF pageIndex is out of range THEN system SHALL respond with 400 and `{"error": "PAGE_OUT_OF_RANGE", "maxPage": N}`
4. WHEN rendering succeeds THEN response headers SHALL include `X-Render-Time-Ms` with actual render time
5. WHEN dpi is omitted THEN system SHALL default to 96 DPI

### REQ-4: Render Verification Endpoint

**User Story:** As a test agent, I want to verify a rendered page against a baseline, so that I can detect visual regressions automatically.

#### Acceptance Criteria

1. WHEN POST /api/verify/render with `{"documentId": "uuid", "pageIndex": 0, "baselineHash": "sha256..."}` is called THEN system SHALL render and compare hash
2. IF hashes match THEN system SHALL respond with 200 and `{"match": true, "ssim": 1.0}`
3. IF hashes differ THEN system SHALL respond with 200 and `{"match": false, "ssim": 0.XX, "diffUrl": "/api/diff/uuid"}`
4. WHEN baseline not provided THEN system SHALL respond with current hash `{"hash": "sha256...", "ssim": null}`

### REQ-5: Document Close Endpoint

**User Story:** As a test tool, I want to close documents after testing, so that memory is released.

#### Acceptance Criteria

1. WHEN DELETE /api/document/{documentId} is called THEN system SHALL close the document and release memory
2. IF documentId is invalid THEN system SHALL respond with 404
3. WHEN document is closed THEN system SHALL respond with 200 and `{"closed": true}`

### REQ-6: Batch Verification Endpoint

**User Story:** As a CI pipeline, I want to verify all pages of a document in one request, so that tests are fast.

#### Acceptance Criteria

1. WHEN POST /api/verify/batch with `{"documentId": "uuid", "baselines": {"0": "hash", "1": "hash"}}` is called THEN system SHALL verify all pages
2. WHEN all pages match THEN system SHALL respond with `{"allMatch": true, "results": [...]}`
3. IF any page fails THEN system SHALL respond with `{"allMatch": false, "failures": [{"pageIndex": 0, "ssim": 0.95}]}`
4. WHEN batch verification runs THEN system SHALL process pages in parallel for performance

### REQ-7: Server Lifecycle Management

**User Story:** As a developer, I want to start/stop the verification server via CLI, so that I can integrate it into my workflow.

#### Acceptance Criteria

1. WHEN `FluentPDF.App.exe --api-server --port 5000` is run THEN system SHALL start REST API server on specified port
2. IF port is in use THEN system SHALL respond with error and suggest alternative port
3. WHEN `--api-server` is combined with `--headless` THEN system SHALL run without UI window
4. WHEN server starts THEN system SHALL log startup URL to console

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: API controllers separate from rendering logic
- **Modular Design**: API layer uses existing PdfRenderingService, no code duplication
- **Dependency Management**: Uses DI for all services
- **Clear Interfaces**: OpenAPI/Swagger documentation auto-generated

### Performance
- Health check response: < 100ms
- Single page render: < 2000ms for standard PDF at 96 DPI
- Batch verification: < 500ms per page when parallelized
- Memory: API server adds < 50MB to base memory footprint

### Security
- Server only binds to localhost by default (127.0.0.1)
- Optional `--bind-address` flag to allow network access
- No authentication required for localhost (trusted)
- Rate limiting: 100 requests/second per client

### Reliability
- Server gracefully handles OOM during rendering (fallback DPI)
- All errors return structured JSON with error codes
- Correlation IDs in all responses for tracing
- Server auto-recovers from PDFium crashes (respawn)

### Usability
- Swagger UI available at /swagger when in development mode
- CLI help shows all API server options
- Exit code 0 on clean shutdown, non-zero on error
