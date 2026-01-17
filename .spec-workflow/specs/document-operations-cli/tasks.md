# Tasks Document

- [ ] 1. Comply with tasks-template structure
  - Purpose: Ensure tasks document follows spec-workflow template standards
  - _Requirements: N/A_
  - _Prompt: Role: Spec Workflow Compliance Officer | Task: Verify this tasks.md complies with template | Restrictions: Verify only | Success: All required fields present_

- [x] 2. Create BookmarksCliTest in FluentPDF.App/Testing/Tests/BookmarksCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/BookmarksCliTest.cs
  - Implement ICliTest for bookmark extraction verification
  - Extract bookmarks, verify structure, save to JSON
  - _Leverage: IBookmarkService, ICliTest interface, System.Text.Json_
  - _Requirements: 1_
  - _Prompt: Role: PDF Document Test Developer | Task: Implement BookmarksCliTest verifying bookmark extraction from requirement 1 | Restrictions: Verify bookmark count, titles, page destinations, save JSON output | Success: Bookmarks extracted, structure verified, JSON output valid_

- [ ] 3. Create SearchCliTest in FluentPDF.App/Testing/Tests/SearchCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/SearchCliTest.cs
  - Implement ICliTest for search functionality verification
  - Search for term, verify results, report metrics
  - _Leverage: ISearchService_
  - _Requirements: 2_
  - _Prompt: Role: Search Functionality Test Developer | Task: Implement SearchCliTest verifying PDF search from requirement 2 | Restrictions: Verify match count, page numbers, positions, handle zero results gracefully | Success: Search works, results verified, metrics reported_

- [ ] 4. Create PageOperationsCliTests in FluentPDF.App/Testing/Tests/PageOperationsCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/PageOperationsCliTest.cs
  - Implement ICliTest for page rotate, delete, reorder
  - Create separate test methods or test classes
  - _Leverage: IPageOperationsService_
  - _Requirements: 3_
  - _Prompt: Role: Page Manipulation Test Developer | Task: Implement page operations tests (rotate, delete, reorder) from requirement 3 | Restrictions: Verify operations modify PDF correctly, save output for verification, cleanup temps | Success: All page operations work, results verified_

- [ ] 5. Create AnnotationsCliTest in FluentPDF.App/Testing/Tests/AnnotationsCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/AnnotationsCliTest.cs
  - Implement ICliTest for annotation detection
  - Detect annotations, verify metadata, report
  - _Leverage: IAnnotationService_
  - _Requirements: 4_
  - _Prompt: Role: Annotation Test Developer | Task: Implement AnnotationsCliTest verifying annotation detection from requirement 4 | Restrictions: Verify count, types, positions, handle no-annotation PDFs gracefully | Success: Annotations detected, metadata verified, reports correctly_

- [ ] 6. Create MetadataCliTest in FluentPDF.App/Testing/Tests/MetadataCliTest.cs
  - File: src/FluentPDF.App/Testing/Tests/MetadataCliTest.cs
  - Implement ICliTest for metadata extraction
  - Extract metadata, display all fields
  - _Leverage: IPdfDocumentService_
  - _Requirements: 5_
  - _Prompt: Role: Document Metadata Test Developer | Task: Implement MetadataCliTest verifying metadata extraction from requirement 5 | Restrictions: Extract all standard fields, handle missing fields, format output clearly | Success: Metadata extracted, all fields reported, handles missing gracefully_

- [ ] 7. Add document operations CLI commands
  - Files: src/FluentPDF.App/CommandLineOptions.cs, src/FluentPDF.App/Services/DiagnosticCommandHandler.cs
  - Add --test-bookmarks, --test-search, --test-page-rotate, --test-page-delete, --test-page-reorder, --test-annotations, --test-metadata
  - Integrate with test framework
  - _Leverage: ICliTest implementations, TestRunner_
  - _Requirements: All_
  - _Prompt: Role: CLI Integration Developer | Task: Add document operations CLI commands covering all requirements | Restrictions: Follow CLI patterns, proper exit codes, support arguments like search term | Success: All commands work, documented in --help_

- [ ] 8. Create document operations tests unit tests
  - Files: tests/FluentPDF.App.Tests/Testing/Tests/DocumentOperationsCliTestsTests.cs
  - Test each document operation CLI test
  - Use test fixtures with known content
  - _Leverage: xUnit, test fixtures_
  - _Requirements: All_
  - _Prompt: Role: QA Engineer | Task: Create unit tests for document operations CLI tests covering all requirements | Restrictions: Use PDFs with known content, verify both success and failure, edge cases | Success: All tests tested, edge cases covered_

- [ ] 9. Document document operations tests in docs/cli-testing.md
  - File: docs/cli-testing.md (update)
  - Add section on document operations testing
  - Provide examples for each test
  - _Leverage: Existing docs_
  - _Requirements: All_
  - _Prompt: Role: Technical Writer | Task: Document document operations CLI tests with examples covering all requirements | Restrictions: Clear examples, explain verification criteria | Success: All operations documented with examples_
