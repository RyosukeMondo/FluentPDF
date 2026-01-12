# Tasks Document

- [x] 1. Add Verify packages to FluentPDF.App.Tests
  - File: tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj
  - Add Verify.Xunit and Verify.WinUI NuGet packages
  - Purpose: Enable snapshot testing capabilities
  - _Leverage: existing test project structure_
  - _Requirements: 1.1_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer specializing in test infrastructure | Task: Add Verify.Xunit and Verify.WinUI packages to FluentPDF.App.Tests.csproj following requirement 1.1 | Restrictions: Do not modify existing package references, use latest stable versions compatible with xUnit 2.x | Success: Packages added, project builds successfully | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 2. Create ModuleInitializer for Verify settings
  - File: tests/FluentPDF.App.Tests/Snapshots/ModuleInitializer.cs
  - Configure Verify settings for WinUI 3
  - Set up snapshot directory and scrubbing
  - Purpose: Global Verify configuration
  - _Leverage: tests/FluentPDF.App.Tests/Snapshots/_
  - _Requirements: 1.2, 2.3_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Developer with Verify framework expertise | Task: Create ModuleInitializer class with [ModuleInitializer] attribute to configure Verify settings following requirements 1.2 and 2.3 | Restrictions: Use VerifierSettings.UseDirectory for snapshots, scrub GUIDs | Success: Verify configured globally, snapshots saved to correct directory | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 3. Create SnapshotTestBase class
  - File: tests/FluentPDF.App.Tests/Snapshots/SnapshotTestBase.cs
  - Implement base class with helper methods for control verification
  - Add [UsesVerify] attribute handling
  - Purpose: Common infrastructure for snapshot tests
  - _Leverage: tests/FluentPDF.App.Tests/Snapshots/ModuleInitializer.cs_
  - _Requirements: 2.1, 2.2_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer with snapshot testing expertise | Task: Create SnapshotTestBase class with VerifyControl helper methods following requirements 2.1 and 2.2 | Restrictions: Use [UsesVerify] attribute, handle initial snapshot creation | Success: Base class provides reusable verification methods | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 4. Create PdfViewerSnapshotTests
  - File: tests/FluentPDF.App.Tests/Snapshots/PdfViewerSnapshotTests.cs
  - Implement snapshot tests for PdfViewerControl
  - Test default state and zoomed state
  - Purpose: Detect visual regressions in PDF viewer
  - _Leverage: tests/FluentPDF.App.Tests/Snapshots/SnapshotTestBase.cs, src/FluentPDF.App/Controls/PdfViewerControl.xaml_
  - _Requirements: 3.1, 3.3_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create PdfViewerSnapshotTests with tests for default and zoomed states following requirements 3.1 and 3.3 | Restrictions: Extend SnapshotTestBase, create meaningful test scenarios | Success: Tests generate and compare snapshots for viewer control | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 5. Create ToolbarSnapshotTests
  - File: tests/FluentPDF.App.Tests/Snapshots/ToolbarSnapshotTests.cs
  - Implement snapshot tests for toolbar controls
  - Test enabled and disabled states
  - Purpose: Detect visual regressions in toolbar
  - _Leverage: tests/FluentPDF.App.Tests/Snapshots/SnapshotTestBase.cs_
  - _Requirements: 3.2, 3.3_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create ToolbarSnapshotTests with tests for toolbar appearance following requirements 3.2 and 3.3 | Restrictions: Extend SnapshotTestBase, test different toolbar states | Success: Tests generate and compare snapshots for toolbar | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 6. Create initial approved snapshots
  - File: tests/FluentPDF.App.Tests/Snapshots/Verified/*.verified.txt
  - Run tests to generate initial snapshots
  - Review and approve baseline snapshots
  - Purpose: Establish baseline for regression detection
  - _Leverage: all snapshot test files_
  - _Requirements: 2.2_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer | Task: Run all snapshot tests to generate initial .received files, review them, and rename to .verified to approve following requirement 2.2 | Restrictions: Only approve correct snapshots, document any issues | Success: All snapshot tests pass with approved baselines | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_
