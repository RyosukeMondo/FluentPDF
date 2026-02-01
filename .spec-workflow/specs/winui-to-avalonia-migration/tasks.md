# WinUI 3 to Avalonia Migration - Tasks

## Phase 1: Analysis & Inventory

### Task 1.1: Inventory WinUI 3 Features
- [ ] List all ViewModels in FluentPDF.App/ViewModels/
- [ ] List all Services in FluentPDF.App/Services/
- [ ] List all Controls in FluentPDF.App/Controls/
- [ ] Document API surface of each component
- [ ] Identify business logic vs UI-specific code
**Files**: Analysis report in scratchpad
**Output**: `feature-inventory.md` with complete feature list

### Task 1.2: Avalonia Gap Analysis
- [ ] Compare Avalonia ViewModels with WinUI 3
- [ ] List missing features in Avalonia
- [ ] Document current build errors (26 errors catalogued)
- [ ] Identify broken/outdated code in Avalonia
**Files**: `gap-analysis.md`
**Output**: Gap analysis report with prioritized fix list

### Task 1.3: Dependency Mapping
- [ ] Map all WinUI 3 API usage (Microsoft.UI.Xaml.*)
- [ ] Find Avalonia equivalents (Avalonia.Controls.*)
- [ ] Document platform-specific code
- [ ] Create API translation table
**Files**: `api-mapping.md`
**Output**: WinUI → Avalonia API mapping reference

## Phase 2: Core Consolidation

### Task 2.1: Move ViewModels to Core
- [ ] Create FluentPDF.Core/ViewModels/ directory
- [ ] Move MainViewModel to Core
- [ ] Move PdfViewerViewModel to Core
- [ ] Move AnnotationViewModel to Core
- [ ] Move BookmarksViewModel to Core
- [ ] Move ThumbnailsViewModel to Core
- [ ] Move SearchPanelViewModel to Core
- [ ] Move AnnotationToolbarViewModel to Core
- [ ] Remove all UI framework dependencies from ViewModels
- [ ] Update namespaces to FluentPDF.Core.ViewModels
**Files**: `src/FluentPDF.Core/ViewModels/*.cs`

### Task 2.2: Create Service Abstractions
- [ ] Create IDispatcherService interface in Core
- [ ] Create IAnimationService interface in Core
- [ ] Create ICoordinateMapper interface (already exists, verify)
- [ ] Create IDialogService interface for platform dialogs
**Files**: `src/FluentPDF.Core/Services/*.cs`

### Task 2.3: Update Avalonia to Use Core ViewModels
- [ ] Update Avalonia project references
- [ ] Delete duplicate ViewModels from Avalonia
- [ ] Update using statements in Views
- [ ] Update DI container registration
**Files**: `src/FluentPDF.Avalonia/App.axaml.cs`, Views

### Task 2.4: Implement Avalonia Services
- [ ] Implement AvaloniaDispatcherService
- [ ] Implement AvaloniaAnimationService
- [ ] Implement AvaloniaCoordinateMapper
- [ ] Implement AvaloniaDialogService
- [ ] Register services in DI container
**Files**: `src/FluentPDF.Avalonia/Services/*.cs`

## Phase 3: Feature Porting

### Task 3.1: Text Selection & Annotations (PRIORITY 1)
- [ ] Port CoordinateMapper to Avalonia implementation
- [ ] Port text selection pointer events to Avalonia views
- [ ] Port BeginTextSelection/EndTextSelection logic
- [ ] Port annotation creation keyboard shortcuts (H/U/S)
- [ ] Port annotation tool visual feedback (crosshair cursor)
- [ ] Test text selection in single-page mode
- [ ] Test text selection in continuous scroll mode
- [ ] Test text selection in two-page mode
- [ ] Port TextSelection model (verify already in Core)
- [ ] Port annotation from selection workflow
**Files**: `src/FluentPDF.Avalonia/Views/PdfViewerPage.axaml.cs`, `src/FluentPDF.Avalonia/Controls/ContinuousScrollViewer.axaml.cs`

### Task 3.2: View Modes (PRIORITY 2)
- [ ] Verify ContinuousScrollViewer builds and works
- [ ] Verify TwoPageViewer builds and works
- [ ] Port view mode switching logic
- [ ] Port zoom handling in all modes
- [ ] Port page navigation in all modes
- [ ] Test rendering performance in each mode
**Files**: `src/FluentPDF.Avalonia/Controls/*.axaml.cs`

### Task 3.3: UI Controls & Theme (PRIORITY 3)
- [ ] Port GlassPanel control
- [ ] Port ShimmerPlaceholder control
- [ ] Port theme system (Dark/Light/Auto)
- [ ] Port liquid glass effects
- [ ] Update all AXAML resource dictionaries
- [ ] Port animation services
**Files**: `src/FluentPDF.Avalonia/Controls/GlassPanel.axaml`, `src/FluentPDF.Avalonia/Styles/*.axaml`

### Task 3.4: Panels & Navigation (PRIORITY 3)
- [ ] Port ThumbnailsSidebar control
- [ ] Port BookmarksPanel control
- [ ] Port thumbnail generation and caching
- [ ] Port bookmark navigation
- [ ] Test sidebar toggle behavior
**Files**: `src/FluentPDF.Avalonia/Controls/ThumbnailsSidebar.axaml`, `BookmarksPanel.axaml`

### Task 3.5: Document Operations (PRIORITY 4)
- [ ] Verify merge dialog works
- [ ] Verify split dialog works
- [ ] Verify rotate page works
- [ ] Verify delete pages works
- [ ] Port any WinUI-specific dialog implementations
**Files**: `src/FluentPDF.Avalonia/Views/*Dialog.axaml`

### Task 3.6: Forms & Watermarks (PRIORITY 5)
- [ ] Port form filling UI
- [ ] Port form field controls
- [ ] Port watermark dialog
- [ ] Port stamp gallery
- [ ] Test form data persistence
**Files**: `src/FluentPDF.Avalonia/Controls/FormFieldControl.axaml`, `Views/WatermarkDialog.axaml`

## Phase 4: Fix Build Errors

### Task 4.1: Fix AnimationService XML Errors
- [ ] Fix line 22 XML comment in AnimationService.cs
- [ ] Fix line 537 XML comment in AnimationService.cs
- [ ] Verify documentation builds correctly
**Files**: `src/FluentPDF.Avalonia/Services/AnimationService.cs:22,537`

### Task 4.2: Fix MainWindow.axaml.cs Errors
- [ ] Add missing `using Avalonia.Controls;` for ToggleButton
- [ ] Fix ToggleButton reference at line 258
- [ ] Fix ToggleButton reference at line 262
- [ ] Fix ToggleButton reference at line 1177
- [ ] Fix ToggleButton reference at line 1189
**Files**: `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs:258,262,1177,1189`

### Task 4.3: Fix PdfViewerViewModel Missing Members
- [ ] Add CurrentPageIndex property (or use CurrentPageNumber)
- [ ] Add SetZoomCommand or fix references
- [ ] Add FitWidthCommand or fix references
- [ ] Add FitPageCommand or fix references
- [ ] Add ToggleThumbnailsCommand or fix references
- [ ] Add ToggleBookmarksCommand or fix references
- [ ] Add ShowSearchCommand or fix references
- [ ] Add PageCount property
- [ ] Verify all commands are wired correctly
**Files**: `src/FluentPDF.Core/ViewModels/PdfViewerViewModel.cs` (after moving to Core)

### Task 4.4: Fix PerformanceMonitor Warning
- [ ] Remove unused _renderSubscription field or implement usage
- [ ] Mark as readonly if appropriate
**Files**: `src/FluentPDF.Avalonia/Services/PerformanceMonitor.cs:24`

### Task 4.5: Verify Clean Build
- [ ] Build FluentPDF.Avalonia on Windows (Debug)
- [ ] Build FluentPDF.Avalonia on Windows (Release)
- [ ] Build on Linux if available
- [ ] Verify zero errors, zero warnings
**Output**: Clean build log

## Phase 5: Remove WinUI 3

### Task 5.1: Delete FluentPDF.App Project
- [ ] Remove FluentPDF.App from solution file
- [ ] Delete src/FluentPDF.App directory
- [ ] Remove FluentPDF.App.Tests if exists
- [ ] Update .gitignore
**Files**: `FluentPDF.sln`, directory deletion

### Task 5.2: Update Build Scripts
- [ ] Remove WinUI 3 builds from run.ps1
- [ ] Remove WinUI 3 builds from build scripts
- [ ] Update launch scripts for Avalonia only
- [ ] Remove x64 platform requirements
**Files**: `run.ps1`, `launch-fluentpdf.ps1`, `build-*.ps1`

### Task 5.3: Update Documentation
- [ ] Remove WinUI 3 references from README.md
- [ ] Update CLAUDE.md with Avalonia build instructions
- [ ] Update UAT_GUIDE.md for Avalonia
- [ ] Update KEYBOARD_SHORTCUTS.md if needed
**Files**: `README.md`, `CLAUDE.md`, `UAT_GUIDE.md`

### Task 5.4: Update CI/CD
- [ ] Remove WinUI 3 workflows from GitHub Actions
- [ ] Add Avalonia workflows
- [ ] Update package/release scripts
- [ ] Test CI pipeline
**Files**: `.github/workflows/*.yml`

### Task 5.5: Final Cleanup
- [ ] Remove unused WinUI 3 NuGet packages
- [ ] Clean all bin/obj directories
- [ ] Remove WinUI 3 runtime dependencies
- [ ] Verify solution loads correctly
**Output**: Clean solution

## Phase 6: Testing & Validation

### Task 6.1: Unit Tests
- [ ] Run all FluentPDF.Core.Tests
- [ ] Run all FluentPDF.Rendering.Tests
- [ ] Update Avalonia-specific tests
- [ ] Verify 80%+ code coverage
- [ ] Fix any failing tests
**Output**: Test results, coverage report

### Task 6.2: Integration Tests
- [ ] Update integration tests for Avalonia
- [ ] Run TextSelectionAnnotationIntegrationTests
- [ ] Run document operation integration tests
- [ ] Verify all tests pass
**Files**: `tests/FluentPDF.Integration.Tests/*.cs`

### Task 6.3: Manual UAT - Text Selection & Annotations
- [ ] Test H/U/S keyboard shortcuts
- [ ] Test text selection in single-page mode
- [ ] Test text selection in continuous scroll mode
- [ ] Test text selection in two-page mode
- [ ] Test annotation creation and persistence
- [ ] Test zoom with annotations
**Reference**: `.spec-workflow/specs/text-selection-annotations/tasks.md` UAT checklist

### Task 6.4: Manual UAT - Document Operations
- [ ] Test PDF loading
- [ ] Test page navigation
- [ ] Test merge/split/rotate
- [ ] Test thumbnails panel
- [ ] Test bookmarks panel
- [ ] Test search functionality
**Output**: UAT results

### Task 6.5: Manual UAT - Forms & Watermarks
- [ ] Test form filling
- [ ] Test form data save/load
- [ ] Test watermark application
- [ ] Test stamp gallery
**Output**: UAT results

### Task 6.6: Performance Validation
- [ ] Measure page rendering time (<500ms target)
- [ ] Measure text extraction time (<100ms target)
- [ ] Measure annotation creation time (<50ms target)
- [ ] Measure startup time (<3s target)
- [ ] Compare with WinUI 3 baseline
**Output**: Performance benchmark report

### Task 6.7: Cross-Platform Testing
- [ ] Test on Windows 10
- [ ] Test on Windows 11
- [ ] Test on Linux (Ubuntu/Debian)
- [ ] Test on macOS (if available)
- [ ] Document platform-specific issues
**Output**: Platform compatibility report

## Progress Tracking

- **Phase 1**: 0/3 tasks
- **Phase 2**: 0/4 tasks
- **Phase 3**: 0/6 tasks
- **Phase 4**: 0/5 tasks
- **Phase 5**: 0/5 tasks
- **Phase 6**: 0/7 tasks
- **Overall**: 0/30 tasks (0% complete)

## Dependencies

- Phase 2 depends on Phase 1 (need inventory before consolidation)
- Phase 3 depends on Phase 2 (need Core ViewModels before porting)
- Phase 4 can run parallel with Phase 3 (build fixes independent)
- Phase 5 depends on Phases 3-4 (need Avalonia working before removing WinUI)
- Phase 6 depends on Phase 5 (test after migration complete)

## Success Criteria

- ✅ All 30 tasks completed
- ✅ Zero build errors in Avalonia project
- ✅ All tests passing (unit, integration, E2E)
- ✅ FluentPDF.App directory deleted
- ✅ Feature parity achieved (all WinUI features in Avalonia)
- ✅ Performance targets met
- ✅ Cross-platform build successful
- ✅ Documentation updated
