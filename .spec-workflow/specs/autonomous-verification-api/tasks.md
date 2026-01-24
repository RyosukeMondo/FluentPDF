# Tasks Document: Autonomous Verification API

- [ ] 1. Create API server bootstrap infrastructure
  - File: src/FluentPDF.App/Api/VerificationApiServer.cs
  - Implement IVerificationApiServer interface with Start/Stop methods
  - Use Microsoft.AspNetCore.Builder minimal APIs
  - Configure Kestrel for localhost-only binding by default
  - Purpose: Enable embedding REST API server in WinUI app
  - _Leverage: Microsoft.AspNetCore.Builder, existing DI container in App.xaml.cs_
  - _Requirements: REQ-7_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Backend Developer specializing in ASP.NET Core minimal APIs | Task: Create VerificationApiServer class that bootstraps Kestrel embedded in WinUI app, implementing IVerificationApiServer interface with StartAsync/StopAsync methods. Configure for localhost-only by default. | Restrictions: Do not block UI thread, use async patterns, reuse existing DI services | _Leverage: Microsoft.AspNetCore.Builder minimal APIs | _Requirements: REQ-7 | Success: Server starts on specified port, gracefully stops, integrates with existing DI container. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 2. Add CLI argument parsing for API server mode
  - File: src/FluentPDF.App/Program.cs (modify existing)
  - Add `--api-server`, `--port`, `--headless`, `--bind-address` flags
  - Parse arguments before WinUI initialization
  - Purpose: Enable CLI-driven server startup
  - _Leverage: Existing CLI argument parsing in Program.cs_
  - _Requirements: REQ-7_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with CLI experience | Task: Extend Program.cs to parse --api-server, --port (default 5000), --headless, --bind-address flags. When --api-server is present, start VerificationApiServer instead of or alongside WinUI. | Restrictions: Must not break existing CLI commands (--render-test, --diagnostics), maintain backward compatibility | _Leverage: Existing CLI parsing in Program.cs | _Requirements: REQ-7 | Success: CLI flags parsed correctly, server starts when --api-server provided, --headless skips UI. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 3. Create request/response DTO models
  - File: src/FluentPDF.App/Api/Models/ApiModels.cs
  - Define LoadDocumentRequest, LoadDocumentResponse, RenderRequest, VerifyRequest, VerifyResponse, ErrorResponse records
  - Use C# records for immutability
  - Purpose: Type-safe API contracts
  - _Leverage: Existing models in FluentPDF.Core/Models_
  - _Requirements: REQ-2, REQ-3, REQ-4_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer specializing in API design | Task: Create ApiModels.cs with all request/response DTOs as C# records. Include LoadDocumentRequest, LoadDocumentResponse, RenderRequest, VerifyRequest, VerifyResponse, BatchVerifyRequest, BatchVerifyResponse, ErrorResponse. | Restrictions: Use records for immutability, include XML documentation, use nullable reference types | _Leverage: FluentPDF.Core/Models patterns | _Requirements: REQ-2, REQ-3, REQ-4 | Success: All DTOs defined with proper nullability, JSON serialization works correctly. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 4. Implement DocumentSessionManager for tracking loaded documents
  - File: src/FluentPDF.App/Api/Services/DocumentSessionManager.cs
  - Use ConcurrentDictionary for thread-safe session storage
  - Generate UUID session IDs
  - Implement CreateSession, GetDocument, CloseSession, CloseAllSessions
  - Purpose: Track documents across API requests
  - _Leverage: ConcurrentDictionary, existing PdfDocument model_
  - _Requirements: REQ-2, REQ-5_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer with concurrency experience | Task: Create DocumentSessionManager with ConcurrentDictionary<string, PdfDocument> storage. Implement IDocumentSessionManager interface with CreateSession (returns UUID), GetDocument, CloseSession, CloseAllSessions methods. | Restrictions: Must be thread-safe, dispose documents on close, log session lifecycle | _Leverage: ConcurrentDictionary, existing PdfDocument | _Requirements: REQ-2, REQ-5 | Success: Sessions created/retrieved/closed correctly, thread-safe under concurrent access. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 5. Implement Health endpoint
  - File: src/FluentPDF.App/Api/Endpoints/HealthEndpoints.cs
  - GET /api/health returns status, version, PDFium state
  - Check PDFium initialization via IPdfDocumentService
  - Purpose: Enable health checks for CI systems
  - _Leverage: IPdfDocumentService, Assembly version_
  - _Requirements: REQ-1_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Developer | Task: Create HealthEndpoints.cs with GET /api/health endpoint using minimal APIs. Return {"status": "healthy/unhealthy", "version": "X.X.X", "pdfiumLoaded": true/false}. Check PDFium state via IPdfDocumentService. | Restrictions: Response must be < 100ms, return 503 if unhealthy | _Leverage: IPdfDocumentService, Assembly.GetExecutingAssembly().GetName().Version | _Requirements: REQ-1 | Success: Health check returns correct status, version included, PDFium state detected. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 6. Implement Document endpoints (load/info/close)
  - File: src/FluentPDF.App/Api/Endpoints/DocumentEndpoints.cs
  - POST /api/document/load - load document, return session ID
  - GET /api/document/{id} - get document info
  - DELETE /api/document/{id} - close document
  - Purpose: Document lifecycle management via API
  - _Leverage: IPdfDocumentService, IDocumentSessionManager_
  - _Requirements: REQ-2, REQ-5_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Developer | Task: Create DocumentEndpoints.cs with POST /api/document/load (loads PDF, returns sessionId + metadata), GET /api/document/{id} (returns document info), DELETE /api/document/{id} (closes document). Map PdfError codes to HTTP status codes. | Restrictions: Validate file paths, handle password-protected PDFs, return structured errors | _Leverage: IPdfDocumentService, IDocumentSessionManager | _Requirements: REQ-2, REQ-5 | Success: Documents load/query/close via API, errors return proper HTTP codes. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 7. Implement Render endpoint
  - File: src/FluentPDF.App/Api/Endpoints/RenderEndpoints.cs
  - POST /api/render - render page to PNG
  - GET /api/render/{documentId}/{pageIndex} - convenience GET endpoint
  - Return PNG binary with Content-Type: image/png
  - Add X-Render-Time-Ms header
  - Purpose: Enable programmatic page rendering
  - _Leverage: IPdfRenderingService_
  - _Requirements: REQ-3_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Developer | Task: Create RenderEndpoints.cs with POST /api/render accepting RenderRequest, returning PNG binary. Also GET /api/render/{documentId}/{pageIndex}?dpi=96 for convenience. Add X-Render-Time-Ms response header. Stream PNG directly to response. | Restrictions: Default DPI to 96, validate pageIndex, use Results<T> pattern | _Leverage: IPdfRenderingService.RenderPageAsync | _Requirements: REQ-3 | Success: Pages render to PNG via API, render time header included, errors handled gracefully. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 8. Implement HashingService for render verification
  - File: src/FluentPDF.App/Api/Services/HashingService.cs
  - Compute SHA256 hash of rendered PNG
  - Optional: SSIM comparison if baseline provided
  - Purpose: Enable render verification without visual inspection
  - _Leverage: System.Security.Cryptography.SHA256_
  - _Requirements: REQ-4_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: C# Developer | Task: Create HashingService implementing IHashingService with ComputeHash(Stream) returning SHA256 hex string. Add optional CompareSSIM(byte[] rendered, byte[] baseline) using SixLabors.ImageSharp for basic perceptual comparison. | Restrictions: Use SHA256 managed, handle large images efficiently | _Leverage: System.Security.Cryptography.SHA256 | _Requirements: REQ-4 | Success: Hashes computed correctly, optional SSIM comparison works. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 9. Implement Verify endpoints (single and batch)
  - File: src/FluentPDF.App/Api/Endpoints/VerifyEndpoints.cs
  - POST /api/verify/render - render and verify against baseline hash
  - POST /api/verify/batch - verify multiple pages in parallel
  - Purpose: Enable automated visual regression testing
  - _Leverage: IPdfRenderingService, IHashingService_
  - _Requirements: REQ-4, REQ-6_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Developer | Task: Create VerifyEndpoints.cs with POST /api/verify/render (renders page, computes hash, compares to baseline if provided) and POST /api/verify/batch (verifies multiple pages in parallel using Task.WhenAll). Return match status, current hash, optional SSIM score. | Restrictions: Batch verification must be parallel, return partial results on failure | _Leverage: IPdfRenderingService, IHashingService, Task.WhenAll | _Requirements: REQ-4, REQ-6 | Success: Single and batch verification works, parallel processing improves performance. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 10. Add middleware for correlation IDs and error handling
  - File: src/FluentPDF.App/Api/Middleware/ApiMiddleware.cs
  - CorrelationIdMiddleware: Add X-Correlation-Id to all requests/responses
  - ErrorHandlingMiddleware: Catch exceptions, return structured ErrorResponse
  - Purpose: Observability and consistent error handling
  - _Leverage: Serilog LogContext, existing PdfError_
  - _Requirements: All_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Middleware Developer | Task: Create ApiMiddleware.cs with CorrelationIdMiddleware (generates/propagates X-Correlation-Id, pushes to Serilog LogContext) and ErrorHandlingMiddleware (catches exceptions, maps to ErrorResponse JSON, logs with correlation). | Restrictions: Must not swallow exceptions silently, include stack trace in development only | _Leverage: Serilog LogContext.PushProperty, PdfError codes | _Requirements: All | Success: All responses have correlation IDs, exceptions return structured JSON errors. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 11. Register API services in DI container
  - File: src/FluentPDF.App/App.xaml.cs (modify existing)
  - Register IVerificationApiServer, IDocumentSessionManager, IHashingService
  - Conditional registration when API mode enabled
  - Purpose: Enable DI for API components
  - _Leverage: Existing DI setup in App.xaml.cs_
  - _Requirements: All_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET DI Specialist | Task: Modify App.xaml.cs to register API services: IVerificationApiServer as singleton, IDocumentSessionManager as singleton, IHashingService as singleton. Only register when API mode is enabled (check CLI flag). | Restrictions: Do not break existing registrations, use singleton for server/session manager | _Leverage: Existing DI setup, Microsoft.Extensions.DependencyInjection | _Requirements: All | Success: API services resolve correctly, existing services unaffected. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 12. Create API integration tests
  - File: tests/FluentPDF.Api.Tests/ApiIntegrationTests.cs
  - Test health, document load, render, verify endpoints
  - Use WebApplicationFactory or direct HttpClient
  - Purpose: Verify API works end-to-end
  - _Leverage: xUnit, Microsoft.AspNetCore.Mvc.Testing_
  - _Requirements: All_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Test Engineer | Task: Create ApiIntegrationTests.cs with tests for all endpoints: health check, document load (valid/invalid paths), render (valid/invalid pages), verify (hash match/mismatch), batch verify. Use in-memory test server. | Restrictions: Use sample PDF from test fixtures, clean up sessions after tests | _Leverage: xUnit, WebApplicationFactory or TestServer, tests/Fixtures/ | _Requirements: All | Success: All endpoints tested, happy and error paths covered. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 13. Create CLI verification script
  - File: tools/verify-rendering.ps1
  - PowerShell script that starts API server, runs verification, reports results
  - Exit code 0 on success, non-zero on failure
  - Purpose: One-command verification for CI
  - _Leverage: curl/Invoke-WebRequest, FluentPDF.App.exe --api-server_
  - _Requirements: All_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer | Task: Create verify-rendering.ps1 that: 1) Starts FluentPDF.App.exe --api-server --headless, 2) Waits for health check, 3) Loads test PDF, 4) Renders and verifies pages against baseline hashes, 5) Reports results, 6) Stops server. Exit 0 on success, 1 on failure. | Restrictions: Handle server startup timeout, clean up on failure | _Leverage: Start-Process, Invoke-RestMethod | _Requirements: All | Success: Script runs autonomously in CI, reports pass/fail correctly. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 14. Add Swagger/OpenAPI documentation
  - File: src/FluentPDF.App/Api/VerificationApiServer.cs (modify)
  - Enable Swagger UI in development mode
  - Generate OpenAPI spec at /swagger/v1/swagger.json
  - Purpose: API discoverability and documentation
  - _Leverage: Swashbuckle.AspNetCore_
  - _Requirements: Non-functional_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Developer | Task: Add Swashbuckle.AspNetCore to enable Swagger UI at /swagger and OpenAPI spec at /swagger/v1/swagger.json. Only enable in development mode. Add XML documentation to endpoints. | Restrictions: Disable in production, document all request/response types | _Leverage: Swashbuckle.AspNetCore NuGet package | _Requirements: Non-functional | Success: Swagger UI accessible, all endpoints documented with examples. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._

- [ ] 15. Update CLAUDE.md with API server documentation
  - File: CLAUDE.md (modify existing)
  - Add API server CLI commands section
  - Document endpoints and usage examples
  - Purpose: Developer discoverability
  - _Leverage: Existing CLAUDE.md structure_
  - _Requirements: Non-functional_
  - _Prompt: Implement the task for spec autonomous-verification-api, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Technical Writer | Task: Add "Verification API Server" section to CLAUDE.md with CLI usage (--api-server, --port, --headless), endpoint summary, example curl commands, and CI integration notes. | Restrictions: Follow existing CLAUDE.md format, keep concise | _Leverage: Existing CLAUDE.md structure | _Requirements: Non-functional | Success: Developers can discover and use API from CLAUDE.md. Mark task in progress in tasks.md before implementing, log implementation with log-implementation tool, mark complete when done._
