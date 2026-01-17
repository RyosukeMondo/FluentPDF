# Tasks Document

- [x] 1. Comply with tasks-template structure
  - Purpose: Ensure tasks document follows spec-workflow template standards
  - _Requirements: N/A_
  - _Prompt: Role: Spec Workflow Compliance Officer | Task: Verify this tasks.md complies with template | Restrictions: Verify only, don't modify | Success: All required fields present_

- [x] 2. Create PageRenderCliTest in FluentPDF.App/Testing/Tests/PageRenderCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/PageRenderCliTest.cs
  - Implement ICliTest for page rendering verification
  - Render page, check dimensions, file size
  - _Leverage: IPdfRenderingService, ICliTest interface, ImageSharp_
  - _Requirements: 1_
  - _Prompt: Role: PDF Rendering Test Developer | Task: Implement PageRenderCliTest verifying single page rendering from requirement 1 | Restrictions: Must verify image dimensions match PDF page size, check file size >1KB, cleanup temp files | Success: Test renders page, verifies output correctness, reports metrics_

- [x] 3. Create ThumbnailAllPagesCliTest in FluentPDF.App/Testing/Tests/ThumbnailAllPagesCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/ThumbnailAllPagesCliTest.cs
  - Implement ICliTest for all-page thumbnail verification
  - Generate thumbnails for all pages, verify count and dimensions
  - _Leverage: IThumbnailRenderingService, existing --test-thumbnails code_
  - _Requirements: 2_
  - _Prompt: Role: Thumbnail Testing Specialist | Task: Implement ThumbnailAllPagesCliTest verifying all-page thumbnails from requirement 2 | Restrictions: Reuse logic from existing --test-thumbnails, verify dimensions, report performance | Success: All thumbnails generated, count correct, dimensions verified_

- [x] 4. Create TextExtractionCliTest in FluentPDF.App/Testing/Tests/TextExtractionCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/TextExtractionCliTest.cs
  - Implement ICliTest for text extraction verification
  - Extract text, verify character count, save to file
  - _Leverage: ITextExtractionService_
  - _Requirements: 3_
  - _Prompt: Role: Text Processing Test Developer | Task: Implement TextExtractionCliTest verifying text extraction from requirement 3 | Restrictions: Verify minimum characters extracted, detect encoding errors, save output for manual check | Success: Text extracted, verified, saved correctly_

- [x] 5. Create FormFieldRenderCliTest in FluentPDF.App/Testing/Tests/FormFieldRenderCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/FormFieldRenderCliTest.cs
  - Implement ICliTest for form field rendering
  - Detect forms, render with forms, verify
  - _Leverage: IPdfFormService, IPdfRenderingService_
  - _Requirements: 4_
  - _Prompt: Role: Interactive PDF Test Developer | Task: Implement FormFieldRenderCliTest verifying form rendering from requirement 4 | Restrictions: Handle PDFs without forms gracefully, verify field count and types, check rendering | Success: Forms detected and verified, renders correctly, handles no-form case_

- [x] 6. Create BatchRenderCliTest in FluentPDF.App/Testing/Tests/BatchRenderCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/BatchRenderCliTest.cs
  - Implement ICliTest for batch rendering all pages
  - Render all pages, report progress, aggregate metrics
  - _Leverage: IPdfRenderingService, parallel processing_
  - _Requirements: 5_
  - _Prompt: Role: Performance Test Developer | Task: Implement BatchRenderCliTest for batch rendering from requirement 5 | Restrictions: Report progress, handle failures gracefully, calculate performance metrics | Success: All pages rendered, performance reported, failures handled_

- [x] 7. Add rendering test CLI commands in CommandLineOptions.cs and DiagnosticCommandHandler.cs
  - Files: src/FluentPDF.App/CommandLineOptions.cs, src/FluentPDF.App/Services/DiagnosticCommandHandler.cs
  - Add --test-page-render, --test-text-extract, --test-form-fields, --render-all-pages
  - Integrate with test framework or direct execution
  - _Leverage: ICliTest implementations, TestRunner_
  - _Requirements: All_
  - _Prompt: Role: CLI Integration Developer | Task: Add rendering verification CLI commands covering all requirements | Restrictions: Follow existing CLI patterns, return proper exit codes, support --output for file paths | Success: All commands work, integrated with test framework, documented in --help_

- [x] 8. Create rendering tests unit tests in FluentPDF.App.Tests/Testing/Tests/
  - Files: tests/FluentPDF.App.Tests/Testing/Tests/RenderingCliTestsTests.cs
  - Test each rendering CLI test implementation
  - Verify with test fixtures
  - _Leverage: xUnit, test fixtures from tests/Fixtures_
  - _Requirements: All_
  - _Prompt: Role: QA Engineer | Task: Create unit tests for all rendering CLI tests covering all requirements | Restrictions: Use actual test PDFs, mock services where needed, verify both success and failure paths | Success: All rendering tests tested, edge cases covered_

- [ ] 9. Document rendering CLI tests in docs/cli-testing.md
  - File: docs/cli-testing.md (update)
  - Add section on rendering verification tests
  - Provide examples for each test type
  - _Leverage: Existing docs_
  - _Requirements: All_
  - _Prompt: Role: Technical Writer | Task: Document rendering CLI tests in cli-testing.md with examples covering all requirements | Restrictions: Clear examples, explain verification criteria, troubleshooting guide | Success: All rendering tests documented with working examples_
