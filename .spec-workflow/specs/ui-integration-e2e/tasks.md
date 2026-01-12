# Tasks Document

## Phase 1: E2E Foundation

- [x] 1. Create E2E test project with FlaUI infrastructure
  - File: tests/FluentPDF.E2E.Tests/FluentPDF.E2E.Tests.csproj
  - Create xUnit test project targeting net8.0-windows10.0.19041.0
  - Add FlaUI.Core, FlaUI.UIA3, FluentAssertions dependencies
  - Configure test settings for WinUI 3 automation
  - Purpose: Establish E2E testing foundation for UI automation
  - _Leverage: tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj_
  - _Requirements: US-1_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Test Engineer specializing in UI automation and WinUI 3 | Task: Create E2E test project with FlaUI infrastructure for WinUI 3 app automation, referencing existing test project structure | Restrictions: Must target Windows-only, use FlaUI.UIA3 for WinUI 3 compatibility, do not modify existing test projects | Success: Project builds successfully, FlaUI can attach to WinUI 3 processes | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 2. Create AppLaunchFixture for test lifecycle
  - File: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs
  - Implement IAsyncLifetime for app launch/teardown
  - Add methods to locate and launch FluentPDF.App.exe
  - Implement MainWindow acquisition with timeout
  - Purpose: Provide reusable fixture for launching app in tests
  - _Leverage: src/FluentPDF.App/App.xaml.cs_
  - _Requirements: US-1_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Test Engineer specializing in FlaUI automation | Task: Create AppLaunchFixture that launches FluentPDF.App.exe and acquires MainWindow for automation, handling async lifecycle | Restrictions: Must handle launch timeout gracefully, clean up process on dispose, do not use hardcoded paths | Success: Fixture launches app, acquires window, disposes cleanly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 3. Create LogVerifier for error checking
  - File: tests/FluentPDF.E2E.Tests/Fixtures/LogVerifier.cs
  - Parse Serilog JSON log files from %LocalAppData%
  - Implement AssertNoErrors() to fail on ERROR level entries
  - Add GetLogEntries() for diagnostic output
  - Purpose: Verify app runs without errors during E2E tests
  - _Leverage: src/FluentPDF.Core/Services/Interfaces/ILogExportService.cs_
  - _Requirements: US-1_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in logging and diagnostics | Task: Create LogVerifier that parses Serilog JSON logs and asserts no ERROR level entries during test execution | Restrictions: Must handle missing log files gracefully, parse JSON format correctly, do not modify app logging | Success: LogVerifier correctly identifies error entries, provides useful diagnostic output | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 4. Add AutomationIds to MainWindow and toolbar
  - File: src/FluentPDF.App/Views/MainWindow.xaml
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml
  - Add AutomationProperties.AutomationId to all interactive elements
  - Include: Open, Save, Navigation, Zoom buttons
  - Purpose: Enable FlaUI to locate UI elements for automation
  - _Leverage: Design document AutomationId table_
  - _Requirements: US-1, US-2, US-3, US-4_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer specializing in accessibility and automation | Task: Add AutomationProperties.AutomationId to all interactive elements in MainWindow and PdfViewerPage XAML for FlaUI automation | Restrictions: Do not change element behavior or styling, use consistent naming convention, do not remove existing properties | Success: All buttons, inputs, panels have unique AutomationIds | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 5. Create app launch smoke test
  - File: tests/FluentPDF.E2E.Tests/Tests/AppLaunchTests.cs
  - Test app launches within 10 seconds
  - Test main window is visible and responsive
  - Test no errors in log after launch
  - Test PDFium initialization succeeds (no native load errors)
  - Purpose: Verify basic app launch functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs, tests/FluentPDF.E2E.Tests/Fixtures/LogVerifier.cs_
  - _Requirements: US-1_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in smoke testing | Task: Create AppLaunchTests that verify app launches successfully, displays main window, and logs no errors using AppLaunchFixture and LogVerifier | Restrictions: Must complete in under 30 seconds, do not test feature functionality, clean up app process | Success: Test passes when app launches cleanly, fails on launch error or ERROR log entry | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 2: Core Viewing Tests

- [x] 6. Add test PDF samples to TestData
  - File: tests/FluentPDF.E2E.Tests/TestData/sample.pdf
  - File: tests/FluentPDF.E2E.Tests/TestData/multi-page.pdf
  - File: tests/FluentPDF.E2E.Tests/TestData/form.pdf
  - Copy or create PDF test files with known content
  - Include: single-page, multi-page (10+ pages), form PDF
  - Purpose: Provide test data for document loading tests
  - _Leverage: tests/FluentPDF.Core.Tests/TestData/_
  - _Requirements: US-2, US-3, US-11_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer specializing in test data management | Task: Add or copy PDF test files to E2E TestData folder, including single-page, multi-page, and form PDFs | Restrictions: Use small file sizes for fast tests, ensure files are valid PDFs, do not use copyrighted content | Success: Test files exist, are valid PDFs, have predictable content | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 7. Create document loading E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/DocumentLoadingTests.cs
  - Test Open button invokes file picker
  - Test loading sample.pdf displays first page
  - Test page count displays correctly
  - Test thumbnails sidebar populates
  - Purpose: Verify PDF loading workflow end-to-end
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-2_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in E2E testing | Task: Create DocumentLoadingTests that verify PDF loading workflow including file picker, page display, and thumbnails using FlaUI automation | Restrictions: Must programmatically set file path (bypass picker dialog), verify visible state changes, do not modify app code | Success: Test passes when PDF loads and displays correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 8. Create navigation E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/NavigationTests.cs
  - Test Previous/Next page buttons
  - Test page number input navigation
  - Test thumbnail click navigation
  - Test keyboard navigation (Page Up/Down)
  - Purpose: Verify all page navigation methods
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-3_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in navigation testing | Task: Create NavigationTests that verify all page navigation methods including buttons, input, thumbnails, and keyboard | Restrictions: Load multi-page PDF first, verify page number changes correctly, do not modify app code | Success: All navigation methods work and update current page correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 9. Create zoom E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/ZoomTests.cs
  - Test Zoom In button increases zoom
  - Test Zoom Out button decreases zoom
  - Test Reset Zoom returns to 100%
  - Test zoom slider interaction
  - Purpose: Verify zoom controls
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-4_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in control testing | Task: Create ZoomTests that verify zoom in, zoom out, and reset zoom functionality using FlaUI automation | Restrictions: Load PDF first, verify zoom percentage changes, do not modify app code | Success: All zoom controls work and update zoom level correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 3: Search & Panels Tests

- [x] 10. Create search E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/SearchTests.cs
  - Test search panel toggle
  - Test search query input
  - Test match highlighting on page
  - Test next/previous match navigation
  - Test match count display
  - Purpose: Verify text search functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-5_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in search testing | Task: Create SearchTests that verify text search workflow including panel toggle, query input, highlighting, and match navigation | Restrictions: Use PDF with known text content, verify match count accuracy, do not modify app code | Success: Search finds expected text, navigation works, match count is accurate | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [x] 11. Add AutomationIds to sidebar panels
  - File: src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml (bookmarks panel section)
  - Add AutomationIds to thumbnails, bookmark items
  - Include: sidebar container, individual thumbnails, bookmark nodes
  - Purpose: Enable sidebar automation testing
  - _Leverage: Design document AutomationId table_
  - _Requirements: US-2, US-3_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer specializing in accessibility | Task: Add AutomationProperties.AutomationId to ThumbnailsSidebar and BookmarksPanel elements for FlaUI automation | Restrictions: Do not change behavior or styling, use consistent naming, handle dynamic item generation | Success: Sidebar elements are locatable by FlaUI | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 4: Editing Operations Tests

- [x] 12. Create merge E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/MergeTests.cs
  - Test Merge button opens file picker
  - Test merging two PDFs
  - Test merged document displays combined pages
  - Test save preserves merged content
  - Purpose: Verify document merge functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-6_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in document operations | Task: Create MergeTests that verify PDF merge workflow including file selection, merge execution, and result verification | Restrictions: Use known test PDFs, verify page count after merge, do not modify app code | Success: Merge combines PDFs correctly, page count is sum of inputs | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 13. Create split E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/SplitTests.cs
  - Test Split button opens dialog
  - Test page range input accepts format
  - Test split creates output file(s)
  - Test split output has correct pages
  - Purpose: Verify document split functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-7_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in document operations | Task: Create SplitTests that verify PDF split workflow including dialog, page range input, and output verification | Restrictions: Use multi-page test PDF, verify output page count, do not modify app code | Success: Split creates files with correct page ranges | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 14. Add AutomationIds to dialogs
  - File: src/FluentPDF.App/Views/WatermarkDialog.xaml
  - File: src/FluentPDF.App/Views/DeletePagesDialog.xaml
  - File: src/FluentPDF.App/Views/SaveConfirmationDialog.xaml
  - Add AutomationIds to dialog inputs, buttons
  - Purpose: Enable dialog automation testing
  - _Leverage: Design document AutomationId table_
  - _Requirements: US-7, US-9_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer specializing in accessibility | Task: Add AutomationProperties.AutomationId to all dialog inputs and buttons for FlaUI automation | Restrictions: Do not change dialog behavior, use consistent naming, include all interactive elements | Success: All dialog elements are locatable by FlaUI | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 5: Content Creation Tests

- [ ] 15. Create annotation E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/AnnotationTests.cs
  - Test annotation toolbar tool selection
  - Test highlight creation on text
  - Test shape (rectangle, circle) creation
  - Test freehand drawing
  - Test annotation persistence on save
  - Purpose: Verify annotation creation functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-8_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in drawing/annotation testing | Task: Create AnnotationTests that verify annotation tools including highlight, shapes, and freehand using FlaUI automation | Restrictions: Simulate mouse interactions for drawing, verify annotations persist, do not modify app code | Success: Annotations created via each tool, saved and reloaded correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 16. Create watermark E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/WatermarkTests.cs
  - Test Watermark button opens dialog
  - Test text watermark configuration
  - Test image watermark configuration
  - Test preview display
  - Test watermark application
  - Purpose: Verify watermark functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-9_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in dialog workflow testing | Task: Create WatermarkTests that verify watermark dialog workflow including text/image configuration and application | Restrictions: Verify watermark visible on page after apply, do not modify app code | Success: Watermark configured, previewed, and applied correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 17. Create image insertion E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/ImageInsertionTests.cs
  - Test Insert Image button opens file picker
  - Test image displays on page
  - Test image resize handles
  - Test image move operation
  - Test image persistence on save
  - Purpose: Verify image insertion functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-10_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in image manipulation testing | Task: Create ImageInsertionTests that verify image insertion workflow including placement, resize, move, and save | Restrictions: Use test image file, verify image bounds change on manipulation, do not modify app code | Success: Image inserted, manipulated, and persisted correctly | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 6: Advanced Features Tests

- [ ] 18. Create form filling E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/FormFillingTests.cs
  - Test form fields are detected
  - Test text field input
  - Test checkbox toggle
  - Test radio button selection
  - Test form data persistence
  - Purpose: Verify form filling functionality
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-11_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in form testing | Task: Create FormFillingTests that verify PDF form filling including text, checkbox, and radio inputs using FlaUI | Restrictions: Use test PDF with form fields, verify field values change, do not modify app code | Success: All form field types work, data persists on save | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 19. Add DOCX test file
  - File: tests/FluentPDF.E2E.Tests/TestData/sample.docx
  - Create simple DOCX with known text content
  - Include: headings, paragraphs, basic formatting
  - Purpose: Provide test data for DOCX conversion
  - _Leverage: tests/FluentPDF.Core.Tests/TestData/_
  - _Requirements: US-12_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer specializing in test data | Task: Create simple DOCX test file with known content for conversion testing | Restrictions: Keep file small and simple, use basic formatting only, avoid complex features | Success: DOCX file exists, has predictable content for verification | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 20. Create DOCX conversion E2E test
  - File: tests/FluentPDF.E2E.Tests/Tests/ConversionTests.cs
  - Test Convert DOCX button opens file picker
  - Test conversion progress displays
  - Test converted PDF opens in viewer
  - Test converted content is accurate
  - Purpose: Verify DOCX to PDF conversion
  - _Leverage: tests/FluentPDF.E2E.Tests/Fixtures/AppLaunchFixture.cs_
  - _Requirements: US-12_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in conversion testing | Task: Create ConversionTests that verify DOCX to PDF conversion workflow including file selection, progress, and result | Restrictions: Verify conversion completes successfully, output PDF is valid, do not modify app code | Success: DOCX converts to PDF, displays in viewer, content is preserved | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

## Phase 7: Integration Suite

- [ ] 21. Create E2E test runner script
  - File: tests/run-e2e-tests.ps1
  - Build FluentPDF.App in Release mode
  - Run E2E test suite with xUnit
  - Collect and output test results
  - Include timeout handling
  - Purpose: Automate E2E test execution
  - _Leverage: RunFluentPDF.bat_
  - _Requirements: All_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer specializing in test automation | Task: Create PowerShell script to build app and run E2E test suite with result collection | Restrictions: Handle build failures gracefully, include timeout for hung tests, output results in standard format | Success: Script builds app, runs tests, reports pass/fail status | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_

- [ ] 22. Create comprehensive E2E workflow test
  - File: tests/FluentPDF.E2E.Tests/Tests/FullWorkflowTests.cs
  - Test complete user workflow: open, annotate, save, reopen
  - Test multi-document workflow with tabs
  - Test error recovery scenarios
  - Purpose: Verify end-to-end user experience
  - _Leverage: All previous E2E tests_
  - _Requirements: All_
  - _Prompt: Implement the task for spec ui-integration-e2e, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in integration testing | Task: Create FullWorkflowTests that verify complete user journeys spanning multiple features | Restrictions: Build on existing test fixtures, cover real user scenarios, include cleanup | Success: Full workflows complete successfully, app state is consistent throughout | Mark task [ ] to [-] in tasks.md before starting, use log-implementation tool after completion, then mark [-] to [x]_
