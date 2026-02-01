# WinUI 3 Removal Plan - Phase 5

**Status**: PLANNING PHASE - DO NOT EXECUTE
**Target**: Remove all WinUI 3 (FluentPDF.App) after Phase 3 completes
**Scope**: Complete removal of Windows-specific UI framework
**Estimated Impact**: 39,164 lines of code + dependencies

---

## 1. Source Files to Delete

### Directory Structure: `src/FluentPDF.App/`

Complete removal of the entire directory:

```
src/FluentPDF.App/
├── Api/                                    (15 files, ~2,500 LOC)
│   ├── Endpoints/
│   │   ├── ActionEndpoints.cs
│   │   ├── AnnotationEndpoints.cs
│   │   ├── DocumentEndpoints.cs
│   │   ├── ElementEndpoints.cs
│   │   ├── FormEndpoints.cs
│   │   ├── HealthEndpoints.cs
│   │   ├── RenderEndpoints.cs
│   │   ├── StampEndpoints.cs
│   │   ├── ThemeEndpoints.cs
│   │   └── VerifyEndpoints.cs
│   ├── Models/
│   │   └── ApiModels.cs
│   ├── Services/
│   │   ├── DocumentSessionManager.cs
│   │   ├── HashingService.cs
│   │   └── UiAutomationService.cs
│   └── VerificationApiServer.cs
├── Assets/                                 (7 image files)
│   ├── LockScreenLogo.scale-200.png
│   ├── SplashScreen.scale-200.png
│   ├── Square150x150Logo.scale-200.png
│   ├── Square44x44Logo.scale-200.png
│   ├── Square44x44Logo.targetsize-24_altform-unplated.png
│   ├── StoreLogo.png
│   └── Wide310x150Logo.scale-200.png
├── Controls/                               (18 files, ~4,500 LOC)
│   ├── AnnotationLayer.xaml & .cs
│   ├── BookmarksPanel.xaml & .cs
│   ├── ContinuousScrollViewer.xaml & .cs
│   ├── DiagnosticsPanelControl.xaml & .cs
│   ├── FormFieldControl.xaml & .cs
│   ├── FormFieldControlTemplates.xaml
│   ├── GlassPanel.xaml & .cs
│   ├── ImageManipulationOverlay.xaml & .cs
│   ├── LogViewerControl.xaml & .cs
│   ├── PdfViewerControl.xaml & .cs
│   ├── ShimmerPlaceholder.xaml & .cs
│   ├── ThumbnailsSidebar.xaml & .cs
│   ├── TwoPageViewer.xaml & .cs
│   └── ValidationErrorPanel.xaml & .cs
├── Converters/                             (14 files, ~800 LOC)
│   ├── BoolToVisibilityConverter.cs
│   ├── CountToVisibilityConverter.cs
│   ├── EnumToIntConverter.cs
│   ├── InverseBoolConverter.cs
│   ├── InverseBoolToVisibilityConverter.cs
│   ├── InverseCountToVisibilityConverter.cs
│   ├── InverseNullToVisibilityConverter.cs
│   ├── LogLevelToBackgroundConverter.cs
│   ├── LogLevelToIconConverter.cs
│   ├── MatchCounterConverter.cs
│   ├── NullToVisibilityConverter.cs
│   ├── PercentageConverter.cs
│   ├── StringToAnnotationToolConverter.cs
│   └── ValidationErrorToStringConverter.cs
├── Diagnostics/                            (18 files, ~2,200 LOC)
│   ├── Commands/
│   │   ├── TestAnnotationsCommand.cs
│   │   ├── TestConversionCommand.cs
│   │   ├── TestEncryptCommand.cs
│   │   ├── TestExportImagesCommand.cs
│   │   ├── TestFormsCommand.cs
│   │   ├── TestMergeCommand.cs
│   │   ├── TestOptimizeCommand.cs
│   │   ├── TestSplitCommand.cs
│   │   ├── TestStampCommand.cs
│   │   └── TestWatermarkCommand.cs
│   ├── Models/
│   │   ├── AnnotationsCommandResult.cs
│   │   ├── CommandResult.cs
│   │   ├── ConversionCommandResult.cs
│   │   ├── EncryptCommandResult.cs
│   │   ├── ExportImagesCommandResult.cs
│   │   ├── FormsCommandResult.cs
│   │   ├── MergeCommandResult.cs
│   │   ├── OptimizeCommandResult.cs
│   │   ├── SplitCommandResult.cs
│   │   └── WatermarkCommandResult.cs
│   ├── DiagnosticCommandHandler.cs
│   └── MarshalingValidationService.cs
├── Helpers/                                (1 file, ~150 LOC)
│   └── CoordinateTransformHelper.cs
├── Interfaces/                             (1 file, ~50 LOC)
│   └── IRenderingStrategy.cs
├── Models/                                 (2 files, ~200 LOC)
│   ├── DisposableBitmapImage.cs
│   └── ThumbnailItem.cs
├── Properties/                             (1 file)
│   └── launchSettings.json
├── Services/                               (11 files, ~3,500 LOC)
│   ├── AnimationService.cs
│   ├── CoordinateMapper.cs
│   ├── DiagnosticCommandHandler.cs (duplicate?)
│   ├── IAnimationService.cs
│   ├── INavigationService.cs
│   ├── JumpListService.cs
│   ├── MemoryMonitor.cs
│   ├── NavigationService.cs
│   ├── RecentFilesService.cs
│   ├── RenderingCoordinator.cs
│   ├── RenderingObservabilityService.cs
│   ├── RenderingSettingsService.cs
│   ├── RenderingStrategyFactory.cs
│   ├── RenderingStrategies/
│   │   ├── FileBasedRenderingStrategy.cs
│   │   └── WriteableBitmapRenderingStrategy.cs
│   ├── SettingsService.cs
│   └── UIBindingVerifier.cs
├── Styles/                                 (2 files, ~400 LOC)
│   ├── ButtonStyles.xaml
│   └── ThemeResources.xaml
├── Testing/                                (17 files, ~2,500 LOC)
│   ├── ICliTest.cs
│   ├── Models.cs
│   ├── ResultVerifier.cs
│   ├── TestDiscovery.cs
│   ├── TestExecutor.cs
│   ├── TestRunner.cs
│   ├── Tests/
│   │   ├── AnnotationsAndWatermarkCliTest.cs
│   │   ├── BatchRenderCliTest.cs
│   │   ├── BookmarksCliTest.cs
│   │   ├── DocumentEditingCliTest.cs
│   │   ├── FindReplaceCliTest.cs
│   │   ├── FormFieldRenderCliTest.cs
│   │   ├── FormsCliTest.cs
│   │   ├── MetadataCliTest.cs
│   │   ├── PageOperationsCliTest.cs
│   │   ├── PageRenderCliTest.cs
│   │   ├── RenderCliTest.cs
│   │   ├── SearchCliTest.cs
│   │   ├── TextExtractionCliTest.cs
│   │   └── ThumbnailAllPagesCliTest.cs
│   └── Verification/
│       ├── ExitCodeRule.cs
│       ├── FileExistsRule.cs
│       └── LogContainsRule.cs
├── ViewModels/                             (19 files, ~6,000 LOC)
│   ├── AnnotationToolbarViewModel.cs
│   ├── AnnotationViewModel.cs
│   ├── BookmarksViewModel.cs
│   ├── ConversionViewModel.cs
│   ├── DiagnosticsPanelViewModel.cs
│   ├── EncryptDialogViewModel.cs
│   ├── FormFieldViewModel.cs
│   ├── ImageInsertionViewModel.cs
│   ├── LogViewerViewModel.cs
│   ├── MainToolbarViewModel.cs
│   ├── MainViewModel.cs
│   ├── MainWindowViewModel.cs
│   ├── MergeViewModel.cs
│   ├── PdfViewerViewModel.cs
│   ├── PresentationViewModel.cs
│   ├── SearchPanelViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── SplitViewModel.cs
│   ├── StampGalleryViewModel.cs
│   ├── TabViewModel.cs
│   ├── ThumbnailsViewModel.cs
│   └── WatermarkViewModel.cs
├── Views/                                  (14 files, ~4,500 LOC)
│   ├── ConversionPage.xaml & .cs
│   ├── DeletePagesDialog.xaml & .cs
│   ├── EncryptDialog.xaml & .cs
│   ├── ErrorDialog.xaml & .cs
│   ├── ExportImagesDialog.xaml & .cs
│   ├── MainPage.xaml & .cs
│   ├── MainWindow.xaml & .cs
│   ├── PdfViewerPage.xaml & .cs
│   ├── PresentationWindow.xaml & .cs
│   ├── SaveConfirmationDialog.xaml & .cs
│   ├── SettingsPage.xaml & .cs
│   └── WatermarkDialog.xaml & .cs
├── App.xaml & .xaml.cs                    (~400 LOC)
├── CommandLineOptions.cs                  (~300 LOC)
├── Imports.cs                             (~200 LOC)
├── FluentPDF.App.csproj                   (project file)
├── FluentPDF.App.slnx                     (workspace file)
├── app.manifest                           (application manifest)
├── Directory.Build.targets                (build configuration)
├── Properties/
│   └── launchSettings.json
├── bin/                                   (compiled outputs, ~200MB)
└── obj/                                   (build artifacts, ~500MB+)
```

### Summary by Category

| Category | Files | Est. LOC | Priority |
|----------|-------|---------|----------|
| Views (XAML + CodeBehind) | 28 | 4,500 | HIGH |
| ViewModels | 19 | 6,000 | HIGH |
| Controls (XAML + CodeBehind) | 18 | 4,500 | HIGH |
| Services | 11 | 3,500 | MEDIUM |
| Testing Framework | 17 | 2,500 | MEDIUM |
| API Endpoints | 15 | 2,500 | LOW (can move to Avalonia) |
| Diagnostics | 18 | 2,200 | LOW |
| Converters | 14 | 800 | MEDIUM |
| Assets (images) | 7 | N/A | LOW |
| Models/Helpers/Interfaces | 4 | 400 | MEDIUM |
| **TOTAL** | **154** | **39,164** | |

### Unique/Non-Duplicated Files

These files exist in WinUI but may NOT have direct equivalents in Avalonia yet:

- **MUST MIGRATE**: Api/VerificationApiServer.cs (REST API for E2E testing)
- **MUST MIGRATE**: Services/RenderingCoordinator.cs, RenderingObservabilityService.cs
- **MUST MIGRATE**: Testing/* (CLI diagnostic test framework)
- **CONSIDER REUSE**: Converters/* (some converters can be ported)
- **CAN DELETE**: ViewModels/*, Views/*, Controls/* (Avalonia has equivalents)
- **CAN DELETE**: Styles/* (Avalonia uses different theming)

---

## 2. Solution File Changes

### File: `FluentPDF.sln`

**Changes required:**

1. Remove Project entry for FluentPDF.App:
   ```xml
   <!-- REMOVE: Lines 12-13 -->
   Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "FluentPDF.App", ...
   EndProject
   ```

2. Remove ProjectConfigurationPlatforms for FluentPDF.App:
   ```xml
   <!-- REMOVE: Lines 68-79 (Debug/Release x86/x64/Any CPU mappings) -->
   {FDB9EEC9-3C86-4609-A5E9-D73AEF03A855}.*
   ```

3. Remove NestedProjects entry:
   ```xml
   <!-- REMOVE: Line 195 -->
   {FDB9EEC9-3C86-4609-A5E9-D73AEF03A855} = {24867AF2-E34D-49AC-9EB3-80D0EC9618F3}
   ```

4. Remove platform configurations that only apply to WinUI (x86, x64):
   - Consider keeping only `Any CPU` for cross-platform builds

**Impact**: 12 lines removed from solution file

---

## 3. Build Scripts to Update

### PowerShell Scripts Referencing FluentPDF.App

| Script | Changes Required |
|--------|------------------|
| `run.ps1` | Remove WinUI launch paths; replace with Avalonia |
| `run-avalonia.ps1` | No changes (already Avalonia-focused) |
| `build-production.ps1` | Remove x64 platform requirement; update to Avalonia release build |
| `launch-fluentpdf.ps1` | Redirect to Avalonia executable |
| `RUN_LATEST.ps1` | Update paths to Avalonia output |
| `test-*.ps1` (all test scripts) | Update binary paths from App to Avalonia |
| `test-api-responsive.ps1` | Update to point to Avalonia API server |
| `test-avalonia-app.ps1` | No changes needed |
| `auto-diagnose-hang.ps1` | Update process names from WinUI to Avalonia |
| `launch-with-console.ps1` | Update executable path |
| `test-winui3.ps1` | DELETE entirely (WinUI-specific) |
| `test-minimal.ps1` | Update to use Avalonia |
| `capture-crash.ps1` | Update process name |

**Scripts to DELETE:**
- `test-winui3.ps1` (only relevant for WinUI)

**Scripts to significantly refactor:**
- `run.ps1` (primary entry point)
- `build-production.ps1` (release builds)
- `launch-fluentpdf.ps1` (app launch)

---

## 4. Documentation to Update

### Root-level Markdown Files (75 total)

**Files DEFINITELY referencing WinUI/FluentPDF.App:**

| File | Type | Action |
|------|------|--------|
| `CLAUDE.md` | Instructions | Update build commands (remove x64 platform, WinUI references) |
| `README.md` | Project overview | Remove WinUI UI framework mention; update to Avalonia |
| `UAT_GUIDE.md` | Testing guide | Update for Avalonia app instead of WinUI |
| `CLI-QUICKSTART.md` | User guide | Update executable paths |
| `COMMANDS.md` | CLI reference | Verify still accurate for Avalonia |
| `QUICK_START.md` | Getting started | Update build/run instructions |
| `BUILD_QUICK_REFERENCE.md` | Build guide | Remove x64 platform setup |
| `WHERE_IS_THE_APP.md` | Location guide | Update to Avalonia output path |
| `XAML_COMPILER_WORKAROUND.md` | Technical note | DELETE (XAML is WinUI-specific) |
| `LAUNCH*.md` | Various launch guides | Update paths/instructions |

**Files to REVIEW (may reference both):**
- AVALONIA_*.md files (verify no incorrect WinUI references remain)
- PHASE_2_*.md, TASK_*.md (historical docs, may keep as-is)
- Most other docs (verify but likely can stay)

**Estimate**: 10-15 files need significant updates; 50+ can remain as historical documentation

---

## 5. CI/CD Workflow Changes

### File: `.github/workflows/`

| Workflow | Changes |
|----------|---------|
| `build.yml` | Remove WinUI build job; keep Avalonia |
| `test.yml` | Remove FluentPDF.App.Tests; keep other tests |
| `release.yml` | Remove WinUI release artifacts; publish only Avalonia |
| `autonomous-tests.yml` | Update to use Avalonia API server |
| `benchmark.yml` | Review; likely remove WinUI-specific benchmarks |
| `marshaling-validation.yml` | Remove references to FluentPDF.App |
| `prototype-build.yml` | Keep as-is (unrelated) |
| `quality-analysis.yml` | Remove FluentPDF.App from analysis scope |
| `visual-regression.yml` | Remove WinUI visual tests |
| `pdfium-verification.yml` | Update paths if references App binaries |

**Jobs to DELETE:**
- WinUI build jobs
- WinUI test jobs
- WinUI packaging jobs

**Jobs to UPDATE:**
- Artifact publishing (remove .msix, .exe from App folder)
- API server tests (verify using Avalonia)

---

## 6. NuGet Dependencies to Remove

### From FluentPDF.App.csproj

**WinUI 3 Specific:**
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.*" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.*" />
```

**Framework References (remove Windows-only):**
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<!-- Change to: -->
<TargetFramework>net8.0</TargetFramework>
```

**Remove x64 Platform Requirements:**
```xml
<Platforms>x86;x64;ARM64</Platforms>
<RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<!-- All removed in Avalonia -->
```

**Keep/Migrate:**
- `Microsoft.Web.WebView2` (might be used by Avalonia)
- `Microsoft.Extensions.Hosting`
- `Serilog.*` (cross-platform, keep)
- `CommunityToolkit.Mvvm` (cross-platform, keep)
- `OpenTelemetry.*` (cross-platform, keep)
- `Swashbuckle.AspNetCore` (cross-platform, keep)
- `System.Reactive` (cross-platform, keep)

---

## 7. Test Projects to Update/Remove

### Test Projects and Changes

| Test Project | Action | Reason |
|--------------|--------|--------|
| `FluentPDF.App.Tests` | **DELETE** | WinUI-specific tests; no Avalonia equivalent yet |
| `FluentPDF.Architecture.Tests` | **UPDATE** | Remove commented-out FluentPDF.App reference; remove WinUI architecture rules |
| `FluentPDF.E2E.Tests` | **UPDATE** | Remove FluentPDF.App project reference; update to use Avalonia |
| `FluentPDF.Rendering.Tests` | **REVIEW** | Verify no WinUI dependencies |
| `FluentPDF.Core.Tests` | **KEEP** | Core tests (framework-agnostic) |
| `FluentPDF.Validation.Tests` | **KEEP** | Validation tests (framework-agnostic) |
| `FluentPDF.Verification.*.Tests` | **KEEP** | Verification tests (framework-agnostic) |
| `FluentPDF.Integration.Tests` | **REVIEW** | May reference App binaries |
| `FluentPDF.Benchmarks` | **UPDATE** | Remove WinUI benchmarks |

### Files Affected in Tests

**FluentPDF.App.Tests/** (entire directory ~500+ files):
```
tests/FluentPDF.App.Tests/
├── Controls/
│   ├── GlassPanelVisualTests.cs
│   ├── LiquidButtonVisualTests.cs
│   └── IMPLEMENTATION_SUMMARY.md
├── IntegrationTests/
├── Services/
│   └── CoordinateMapperTests.cs
├── ViewModels/
│   └── PdfViewerViewModelTextSelectionTests.cs
├── VerifyConfig.cs
└── FluentPDF.App.Tests.csproj

Action: DELETE ENTIRE DIRECTORY
```

---

## 8. Bin/Obj Directories

### Cleanup

**Files to delete (auto-generated, can be regenerated):**
- `src/FluentPDF.App/bin/` (~200MB)
- `src/FluentPDF.App/obj/` (~500MB+)
- All compiled .dll, .exe, .pdb files

These are auto-generated during build; safe to delete before removal.

---

## 9. Verification API Server - CRITICAL NOTE

### Action: DO NOT DELETE API ENDPOINTS

The API server code (Api/VerificationApiServer.cs and all endpoints) must be **migrated to Avalonia** before deletion:

- **Location**: `src/FluentPDF.App/Api/VerificationApiServer.cs`
- **Dependent Code**:
  - `Api/Endpoints/*.cs` (10 endpoint files)
  - `Api/Services/*.cs` (DocumentSessionManager, HashingService, etc.)
- **Purpose**: REST API for autonomous E2E verification
- **Status**: MUST BE PORTED TO AVALONIA BEFORE PHASE 5

**Checklist before deletion:**
- [ ] All API endpoints migrated to FluentPDF.Avalonia
- [ ] VerificationApiServer running in Avalonia
- [ ] All automation scripts tested with Avalonia API
- [ ] E2E test pipeline validated

---

## 10. Execution Checklist (DO NOT EXECUTE NOW)

### Pre-Deletion Phase

- [ ] Verify Phase 3 (Avalonia port) is 100% complete
- [ ] Verify Avalonia app has all features from WinUI
- [ ] Run full test suite with Avalonia (not WinUI)
- [ ] Migrate API server to Avalonia
- [ ] Update all automation scripts to use Avalonia paths
- [ ] Update CI/CD workflows
- [ ] Verify documentation is updated

### Deletion Phase (Sequential, Non-Reversible)

1. **Backup** (required):
   - [ ] Create git tag: `v-before-winui-removal`
   - [ ] Backup branch: `git checkout -b backup/winui-before-removal`

2. **Solution File**:
   - [ ] Remove FluentPDF.App project from FluentPDF.sln
   - [ ] Remove all platform-specific configurations
   - [ ] Verify solution builds with Avalonia only

3. **Source Directories**:
   - [ ] Delete entire `src/FluentPDF.App/` directory
   - [ ] Delete `src/FluentPDF.App.xml` (documentation)

4. **Test Projects**:
   - [ ] Delete `tests/FluentPDF.App.Tests/`
   - [ ] Update `tests/FluentPDF.Architecture.Tests/` (remove App reference)
   - [ ] Update `tests/FluentPDF.E2E.Tests/` (update dependencies)

5. **Build Scripts**:
   - [ ] Update all *.ps1 scripts
   - [ ] Delete `test-winui3.ps1`
   - [ ] Verify `run.ps1` launches Avalonia

6. **CI/CD Workflows**:
   - [ ] Update `.github/workflows/*.yml` files
   - [ ] Remove WinUI build jobs
   - [ ] Verify workflows execute successfully

7. **Solution Configuration**:
   - [ ] Remove x64, x86, ARM64 platform configurations (keep Any CPU only)
   - [ ] Update `Directory.Build.props` if needed

8. **Clean Build Verification**:
   - [ ] `dotnet clean`
   - [ ] `dotnet build` (Avalonia + Core + Rendering)
   - [ ] `dotnet test` (all cross-platform tests)
   - [ ] Verify Avalonia app builds and runs

9. **Commit**:
   - [ ] Single commit: "chore: remove WinUI 3 (Phase 5 cleanup)"
   - [ ] Include summary of deleted files

10. **Post-Removal Verification**:
    - [ ] Run full test suite
    - [ ] Verify API server endpoints work
    - [ ] Test CLI commands
    - [ ] Verify package builds successfully

---

## 11. File Counts and Statistics

### Summary Table

| Category | Count | LOC | Size (MB) | Status |
|----------|-------|-----|-----------|--------|
| Source Files (.cs) | 115 | 28,000 | ~8 | DELETE |
| XAML Files (.xaml) | 39 | 11,164 | ~3 | DELETE |
| Image Assets | 7 | N/A | ~1 | DELETE |
| Config/Manifest | 3 | N/A | <1 | DELETE |
| bin/ (compiled) | many | N/A | ~200 | DELETE |
| obj/ (artifacts) | many | N/A | ~500 | DELETE |
| **TOTAL** | **154+** | **39,164** | **~712** | |

### Dependencies to Remove

**NuGet packages (WinUI-specific):**
- Microsoft.WindowsAppSDK (v1.7.*)
- Microsoft.Windows.SDK.BuildTools (v10.*)

**Framework targets:**
- net8.0-windows10.0.19041.0 (replace with net8.0 in Avalonia)

---

## 12. Documentation Update Checklist

| File | Update | Priority |
|------|--------|----------|
| `CLAUDE.md` | Update build commands | HIGH |
| `README.md` | Remove WinUI references | HIGH |
| `UAT_GUIDE.md` | Update test procedures | HIGH |
| `BUILD_QUICK_REFERENCE.md` | Remove x64 platform | HIGH |
| `WHERE_IS_THE_APP.md` | Update path | MEDIUM |
| `XAML_COMPILER_WORKAROUND.md` | DELETE | LOW |
| All AVALONIA_*.md | Review for accuracy | LOW |
| Historical docs | Keep as-is | N/A |

---

## 13. Risk Assessment

### Low Risk
- Deleting WinUI-specific XAML files
- Removing NuGet packages from App.csproj
- Deleting Views/Controls/ViewModels (equivalents exist in Avalonia)

### Medium Risk
- Removing API endpoints (must migrate first)
- Updating CI/CD workflows (needs testing)
- Removing test projects (ensure equivalents exist)

### High Risk
- **None** if Phase 3 is complete and tested

---

## 14. Rollback Plan

If Phase 5 removal causes issues:

1. Revert to backup branch: `git checkout backup/winui-before-removal`
2. Restore tag: `git checkout v-before-winui-removal`
3. Analyze root cause
4. Plan targeted removal instead of full removal

---

## 15. Success Criteria

Phase 5 is complete when:

- [ ] `dotnet build` succeeds without WinUI project
- [ ] `dotnet test` passes (all remaining tests)
- [ ] Avalonia application launches and functions
- [ ] All REST API endpoints work
- [ ] CLI commands execute successfully
- [ ] No broken references in solution
- [ ] No WinUI code remains in repository
- [ ] Documentation updated and accurate

---

## Notes

- **DO NOT DELETE YET**: This is a planning document
- **Phase 3 must complete first**: All Avalonia features must be ported and tested
- **API server is critical**: Must be running in Avalonia before removal
- **Backup before deletion**: This operation is non-reversible without git history
- **Execute after full Phase 3 validation**: Do not skip testing

