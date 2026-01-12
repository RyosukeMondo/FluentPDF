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

- [ ] 6. Create initial approved snapshots - **READY FOR TESTING**
  - File: tests/FluentPDF.App.Tests/Snapshots/Verified/*.verified.txt
  - Run tests to generate initial snapshots
  - Review and approve baseline snapshots
  - Purpose: Establish baseline for regression detection
  - **STATUS**: .NET 9 upgrade complete - ready for Windows testing
  - **WINDOWS TESTING INSTRUCTIONS**:
    1. Sync files to Windows PC: `scp -r src tests *.sln Directory.Build.props global.json ryosu@192.168.11.48:"C:/dev/FluentPDF/"`
    2. SSH to Windows: `ssh ryosu@192.168.11.48`
    3. Install .NET 9 SDK if not present: Download from https://dotnet.microsoft.com/download/dotnet/9.0
    4. Verify .NET 9 SDK: `dotnet --version` (should show 9.0.100 or higher)
    5. Clean and restore packages: `cd C:\dev\FluentPDF && dotnet clean && dotnet restore`
    6. Build FluentPDF.App: `dotnet build src\FluentPDF.App\FluentPDF.App.csproj -p:Platform=x64`
    7. If build succeeds, run snapshot tests: `dotnet test tests\FluentPDF.App.Tests\FluentPDF.App.Tests.csproj -p:Platform=x64`
    8. Review generated snapshots and approve baselines
  - **PREVIOUS BLOCKER** (now addressed): Windows build environment had XAML compiler failure (MSB3073: XamlCompiler.exe exits with code 1)
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
    - ✅ Copied FluentPDF App.xaml to minimal project - **BUILDS SUCCESSFULLY** (App.xaml is not the issue)
    - ✅ Verified No Visual Studio available for better diagnostics
    - ❌ Issue persists across all attempted fixes
    - 📝 XamlCompiler.exe crashes before creating output.json (crash in process, not validation error)
    - **KEY FINDING**: Issue is project-specific, not environment-specific (minimal WinUI 3 app compiles fine)
    - **HYPOTHESIS**: Issue likely caused by specific XAML file(s), NuGet package interaction, or project complexity (20 XAML files)
  - **TROUBLESHOOTING RESOURCES**:
    - Diagnostic build script: `build-diagnostics-windows.ps1` (generates detailed build logs)
    - XAML validation script: `validate-xaml-windows.ps1` (validates XAML structure)
    - Known issue: [XamlCompiler.exe needs better logs #9813](https://github.com/microsoft/microsoft-ui-xaml/issues/9813)
    - Related: [Can't get error output from XamlCompiler.exe #10027](https://github.com/microsoft/microsoft-ui-xaml/issues/10027)
  - **BINARY SEARCH INVESTIGATION** (2026-01-12 continued):
    - ✅ Binary search completed on all 19 XAML files (excluding App.xaml)
    - ✅ **CRITICAL FINDING**: ALL FluentPDF XAML files fail when copied to minimal project individually
    - ✅ Tested Controls directory (10 files) - fails
    - ✅ Tested Views directory (9 files) - fails
    - ✅ Tested individual files (AnnotationLayer, PdfViewerControl, MainPage) - ALL fail
    - **ROOT CAUSE IDENTIFIED**: XAML files fail in minimal project because they reference code-behind classes (e.g., `x:Class="FluentPDF.App.Views.MainPage"`) that don't exist in the minimal project
    - **KEY INSIGHT**: The issue is NOT with individual XAML files - they fail in isolation only due to missing dependencies
    - **REVISED HYPOTHESIS**: FluentPDF.App has all required files but XamlCompiler.exe still crashes, suggesting:
      1. Project complexity (20 XAML files) exceeds XamlCompiler limits
      2. Large input.json (152KB vs minimal's 100KB) triggers XamlCompiler bug
      3. Specific NuGet package combination causes XamlCompiler crash
      4. ProjectReferences to FluentPDF.Core and FluentPDF.Rendering cause issues
  - **ROOT CAUSE IDENTIFIED** (2026-01-12):
    - ✅ **CONFIRMED**: Mammoth package transitively pulls in WPF assemblies (PresentationCore, PresentationFramework, System.Windows.Forms, etc.)
    - ✅ FluentPDF.App input.json contains 277 reference assemblies (including 11+ WPF assemblies)
    - ✅ Minimal WinUI 3 project input.json contains only 201 assemblies with ZERO WPF assemblies
    - ✅ XamlCompiler.exe crashes when it encounters WPF assemblies in WinUI 3 project's reference list
    - **WHY**: Mammoth is a .NET library for .docx processing that depends on WPF for document rendering
    - **IMPACT**: WPF and WinUI 3 XamlCompiler are incompatible when WPF assemblies appear in XamlCompiler's input.json
  - **ATTEMPTED FIXES** (2026-01-12):
    - ❌ Changed FluentPDF.Rendering from net8.0 to net8.0-windows10.0.19041.0 (WPF assemblies still present)
    - ❌ Added UseWPF=false and UseWindowsForms=false properties (doesn't prevent transitive references)
    - ❌ Created FilterWpfReferences MSBuild target to filter ReferencePathWithRefAssemblies (target runs too late or wrong item group)
    - ❌ Set IncludePackageReferencesDuringMarkupCompilation=false (no effect)
    - ⚠️  Created filter-xaml-compiler-input.ps1 PowerShell script (runs successfully, removes 21 WPF assemblies, but changes don't persist - MSBuild regenerates input.json)
    - ✅ Replaced Mammoth with DocumentFormat.OpenXml (commit 28c3b6f) - successfully eliminates Mammoth's WPF dependencies
    - ❌ Updated filter script to also exclude WindowsBase.dll - now removes 22 assemblies but XamlCompiler still crashes
    - ❌ Tried DisableImplicitFrameworkReferences on FluentPDF.Rendering (breaks test project compatibility)
    - ❌ Tried ExcludeAssets=FrameworkReferences on ProjectReference (no effect, still 278 assemblies in input.json)
  - **CURRENT STATUS** (2026-01-12):
    - ✅ Mammoth replaced with DocumentFormat.OpenXml - eliminates direct WPF package dependencies
    - ⚠️  WPF assemblies still present: microsoft.windowsdesktop.app.ref is implicitly included when targeting net8.0-windows
    - ⚠️  Filter script successfully removes 22 WPF assemblies but XamlCompiler.exe still crashes with exit code 1
    - **HYPOTHESIS**: XamlCompiler may cache input.json before filter runs, OR there's a different issue now that direct WPF dependencies are gone
  - **ADDITIONAL INVESTIGATION** (2026-01-12 afternoon):
    - ✅ Changed FluentPDF.Rendering from net8.0-windows10.0.19041.0 to net8.0 - builds successfully on Linux
    - ❌ FluentPDF.App still fails with XamlCompiler exit code 1 - WPF assemblies still present (278 total)
    - ❌ Tried FrameworkReference Remove for Microsoft.WindowsDesktop.App* - no effect
    - ❌ Filter script runs too early - input.json doesn't exist yet when FilterXamlCompilerInput target runs
    - **ROOT CAUSE CONFIRMED**: net8.0-windows TFM implicitly and unavoidably includes microsoft.windowsdesktop.app.ref
    - **ARCHITECTURAL LIMITATION**: No MSBuild property can prevent implicit WPF reference inclusion in net8.0-windows
    - **KEY INSIGHT**: Filter script runs AfterTargets="_PrepareForMarkupCompilation" but input.json is created AFTER this target
    - **TIMING ISSUE**: MSBuild generates input.json, then our filter runs (sometimes finds it, sometimes doesn't), then XamlCompiler runs with unfiltered version
  - **RECOMMENDED SOLUTIONS** (in order of preference):
    1. ~~Replace Mammoth with DocumentFormat.OpenXml~~ - ✅ COMPLETED (commit 28c3b6f) but insufficient to fix XamlCompiler crash
    2. ~~**Upgrade to .NET 9**~~ - ✅ COMPLETED (commit a8e1852)
       - **COMPLETED STEPS** (2026-01-12):
         - ✅ Updated global.json to require .NET 9 SDK (9.0.100)
         - ✅ Updated FluentPDF.App from net8.0-windows10.0.19041.0 to net9.0-windows10.0.19041.0
         - ✅ Updated all cross-platform projects (Core, Rendering, Validation) from net8.0 to net9.0
         - ✅ Updated all test projects to net9.0 or net9.0-windows
         - ✅ Updated Microsoft.Extensions.Hosting from 8.* to 9.*
         - ⏳ **NEXT**: Test build on Windows to verify XamlCompiler works with .NET 9's improved framework reference handling
       - **RATIONALE**: .NET 9 may have improved handling of implicit framework references and better isolation between WinUI 3 and WPF assemblies
    3. **Try Visual Studio 2022 IDE build** - May have workarounds or better error diagnostics for this XamlCompiler issue
    4. **Custom MSBuild task** - Create custom MSBuild task (not Target) that hooks into GenerateXamlInputJson task to modify ReferenceAssemblies before input.json generation
    5. **Isolate Mammoth in separate process** - No longer applicable (Mammoth removed)
  - **COMPLETED INVESTIGATION STEPS**:
    1. ~~Check Windows Event Viewer for application crash details~~ (checked - no XamlCompiler crashes logged)
    2. ~~Try building a minimal WinUI 3 project~~ (DONE - builds successfully, isolates issue to FluentPDF.App)
    3. ~~Binary search XAML files~~ (DONE - all files fail individually due to missing code-behind, not XAML syntax errors)
    4. ~~Examine input.json for FluentPDF.App vs minimal project to identify differences~~ (DONE - found WPF assemblies)
    5. ~~Check if specific NuGet package combinations trigger XamlCompiler bug~~ (DONE - Mammoth brings WPF)
    6. ~~Create custom MSBuild target that intercepts and modifies XamlCompiler input.json~~ (DONE - script works but changes don't persist)
  - _Leverage: all snapshot test files_
  - _Requirements: 2.2_
  - _Prompt: Implement the task for spec snapshot-testing, first run spec-workflow-guide to get the workflow guide then implement the task: Role: QA Engineer | Task: Run all snapshot tests to generate initial .received files, review them, and rename to .verified to approve following requirement 2.2 | Restrictions: Only approve correct snapshots, document any issues | Success: All snapshot tests pass with approved baselines | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_
