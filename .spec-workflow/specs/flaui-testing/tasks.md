# Tasks Document

- [x] 1. Add FlaUI packages to FluentPDF.App.Tests
  - File: tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj
  - Add FlaUI.Core and FlaUI.UIA3 NuGet packages
  - Purpose: Enable UI automation testing capabilities
  - _Leverage: existing test project structure_
  - _Requirements: 1.1_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in test infrastructure | Task: Add FlaUI.Core and FlaUI.UIA3 NuGet packages to FluentPDF.App.Tests.csproj following requirement 1.1 | Restrictions: Do not modify existing package references, use latest stable versions | Success: Packages added, project builds successfully | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 2. Create FlaUITestBase class
  - File: tests/FluentPDF.App.Tests/E2E/FlaUITestBase.cs
  - Implement base class with app lifecycle management
  - Add screenshot capture on failure
  - Purpose: Provide common infrastructure for all FlaUI tests
  - _Leverage: tests/FluentPDF.App.Tests/E2E/_
  - _Requirements: 1.2, 1.3_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer with FlaUI expertise | Task: Create FlaUITestBase class implementing app launch, cleanup, and screenshot capture following requirements 1.2 and 1.3 | Restrictions: Must implement IDisposable, use UIA3Automation, handle app not found gracefully | Success: Base class compiles, can launch and close app | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 3. Create MainWindowPage page object
  - File: tests/FluentPDF.App.Tests/PageObjects/MainWindowPage.cs
  - Implement page object for main window interactions
  - Use AutomationId for element discovery
  - Purpose: Encapsulate main window UI interactions
  - _Leverage: tests/FluentPDF.App.Tests/PageObjects/_
  - _Requirements: 2.1, 2.2, 2.3_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer specializing in Page Object Pattern | Task: Create MainWindowPage with methods for file open, navigation following requirements 2.1-2.3 | Restrictions: Use AutomationId selectors only, expose high-level actions not implementation | Success: Page object provides OpenFile, GetCurrentPage methods | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 4. Add AutomationId to key XAML controls
  - File: src/FluentPDF.App/Views/MainWindow.xaml (and related)
  - Add x:AutomationId attributes to testable controls
  - Purpose: Enable FlaUI to locate UI elements reliably
  - _Leverage: src/FluentPDF.App/Views/_
  - _Requirements: 2.3_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI 3 Developer | Task: Add x:AutomationId attributes to key controls (OpenFileButton, PageNavigator, ZoomSlider) following requirement 2.3 | Restrictions: Do not change control functionality, use descriptive IDs | Success: Key controls have AutomationId, app builds and runs | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 5. Create basic UI test for file open workflow
  - File: tests/FluentPDF.App.Tests/E2E/FileOpenTests.cs
  - Implement test that opens a PDF file and verifies display
  - Use page objects for interactions
  - Purpose: Validate core file open functionality via UI
  - _Leverage: tests/FluentPDF.App.Tests/PageObjects/MainWindowPage.cs, tests/FluentPDF.App.Tests/E2E/FlaUITestBase.cs_
  - _Requirements: 3.1, 3.3_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create FileOpenTests with test methods for opening PDF and verifying it displays correctly following requirements 3.1 and 3.3 | Restrictions: Use page objects, capture screenshot on failure, clean up after test | Success: Test passes when run locally, fails gracefully if app not found | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 6. Create navigation UI test
  - File: tests/FluentPDF.App.Tests/E2E/NavigationTests.cs
  - Implement test for page navigation
  - Verify page number updates correctly
  - Purpose: Validate navigation workflow via UI
  - _Leverage: tests/FluentPDF.App.Tests/PageObjects/MainWindowPage.cs_
  - _Requirements: 3.2_
  - _Prompt: Implement the task for spec flaui-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create NavigationTests verifying page navigation works correctly following requirement 3.2 | Restrictions: Use page objects, test next/previous page navigation | Success: Test navigates between pages and verifies page number | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_
