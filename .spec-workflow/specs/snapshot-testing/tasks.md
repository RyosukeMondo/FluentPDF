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

- [x] 5. Create ToolbarSnapshotTests
  - File: tests/FluentPDF.App.Tests/Snapshots/ToolbarSnapshotTests.cs
  - Implement snapshot tests for toolbar controls
  - Test enabled and disabled states
  - Purpose: Detect visual regressions in toolbar
  - _Leverage: tests/FluentPDF.App.Tests/Snapshots/SnapshotTestBase.cs_
  - _Requirements: 3.2, 3.3_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Automation Engineer | Task: Create ToolbarSnapshotTests with tests for toolbar appearance following requirements 3.2 and 3.3 | Restrictions: Extend SnapshotTestBase, test different toolbar states | Success: Tests generate and compare snapshots for toolbar | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 6. Create initial approved snapshots - **BLOCKED**
  - File: tests/FluentPDF.App.Tests/Snapshots/Verified/*.verified.txt
  - Run tests to generate initial snapshots
  - Review and approve baseline snapshots
  - Purpose: Establish baseline for regression detection
  - **BLOCKER**: Windows build environment has XAML compiler failure (MSB3073: XamlCompiler.exe exits with code 1)
    - FluentPDF.App.csproj fails to build on Windows due to XAML compilation error
    - Error occurs in Microsoft.UI.Xaml.Markup.Compiler.interop.targets during MarkupCompilePass1
    - XamlCompiler.exe crashes silently with no diagnostic output (known WinUI 3 bug)
    - All source files synced correctly, converters and ViewModels present
    - Requires Windows environment investigation/fix before snapshot tests can run
  - **ATTEMPTED FIXES** (2026-01-12):
    - ✅ Validated all XAML files with `validate-xaml-windows.ps1` - all checks passed
    - ✅ Added `global.json` to lock .NET SDK to 9.0.308 (was using preview 10.0.101)
    - ✅ Updated WindowsAppSDK from 1.5.240428000 to 1.6.241114003, then to 1.8.251106002
    - ✅ Cleared NuGet cache and restored packages
    - ✅ Created minimal WinUI 3 project - **BUILDS SUCCESSFULLY** (proves environment is fine)
    - ✅ Tried DisableXbfGeneration property - didn't prevent XamlCompiler execution
    - ❌ Issue persists across all attempted fixes
    - 📝 XamlCompiler.exe crashes before creating output.json (crash in process, not validation error)
    - **KEY FINDING**: Issue is project-specific, not environment-specific (minimal WinUI 3 app compiles fine)
  - **TROUBLESHOOTING RESOURCES**:
    - Diagnostic build script: `build-diagnostics-windows.ps1` (generates detailed build logs)
    - XAML validation script: `validate-xaml-windows.ps1` (validates XAML structure)
    - Known issue: [XamlCompiler.exe needs better logs #9813](https://github.com/microsoft/microsoft-ui-xaml/issues/9813)
    - Related: [Can't get error output from XamlCompiler.exe #10027](https://github.com/microsoft/microsoft-ui-xaml/issues/10027)
  - **NEXT INVESTIGATION STEPS**:
    1. ~~Check Windows Event Viewer for application crash details~~ (checked - no XamlCompiler crashes logged)
    2. Use Process Monitor to trace XamlCompiler.exe file/registry access patterns
    3. ~~Try building a minimal WinUI 3 project~~ (DONE - builds successfully, isolates issue to FluentPDF.App)
    4. Binary search XAML files: Copy FluentPDF XAML files to minimal project one-by-one to identify problematic file
    5. Examine input.json for FluentPDF.App vs minimal project to identify differences
    6. Check if specific NuGet package combinations trigger XamlCompiler bug (CommunityToolkit.Mvvm, Mammoth, etc.)
    7. Try building FluentPDF.App in Visual Studio 2022 IDE for better error diagnostics
    8. Consider temporarily removing XAML files to isolate the problematic one
  - _Leverage: all snapshot test files_
  - _Requirements: 2.2_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer | Task: Run all snapshot tests to generate initial .received files, review them, and rename to .verified to approve following requirement 2.2 | Restrictions: Only approve correct snapshots, document any issues | Success: All snapshot tests pass with approved baselines | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_
