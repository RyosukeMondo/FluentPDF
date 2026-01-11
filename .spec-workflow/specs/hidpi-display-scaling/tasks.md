# Tasks Document

## Implementation Tasks

- [x] 1. Create DisplayInfo and RenderingQuality models
  - Files:
    - `src/FluentPDF.Core/Models/DisplayInfo.cs`
    - `src/FluentPDF.Core/Models/RenderingQuality.cs`
    - `tests/FluentPDF.Core.Tests/Models/DisplayInfoTests.cs`
  - Create DisplayInfo model with RasterizationScale, EffectiveDpi, IsHighDpi properties
  - Create RenderingQuality enum with Auto, Low, Medium, High, Ultra values
  - Add validation and helper methods
  - Write unit tests
  - Purpose: Provide domain models for HiDPI functionality
  - _Leverage: Existing model patterns_
  - _Requirements: 1.1-1.7, 6.1-6.9_
  - _Prompt: Role: Domain Modeling Developer | Task: Create DisplayInfo and RenderingQuality models for HiDPI support | Restrictions: Keep models immutable, add validation | Success: Models represent display configuration and quality settings_

- [x] 2. Implement IDpiDetectionService and DpiDetectionService
  - Files:
    - `src/FluentPDF.Core/Services/IDpiDetectionService.cs`
    - `src/FluentPDF.Rendering/Services/DpiDetectionService.cs`
    - `tests/FluentPDF.Rendering.Tests/Services/DpiDetectionServiceTests.cs`
  - Create IDpiDetectionService interface
  - Implement GetCurrentDisplayInfo using XamlRoot.RasterizationScale
  - Implement MonitorDpiChanges with Observable pattern and throttling
  - Implement CalculateEffectiveDpi with quality and zoom support
  - Add DPI clamping (50-576 DPI)
  - Write comprehensive unit tests
  - Purpose: Detect and monitor display DPI dynamically
  - _Leverage: XamlRoot API, System.Reactive, Result<T> pattern_
  - _Requirements: 1.1-1.7, 3.1-3.7, 5.1-5.7_
  - _Prompt: Role: Windows Platform Developer | Task: Implement DPI detection service using WinUI XamlRoot with observable pattern | Restrictions: Must throttle events, clamp DPI bounds, handle null XamlRoot | Success: Can detect DPI and monitor changes with proper throttling_

- [x] 3. Create IRenderingSettingsService and implementation
  - Files:
    - `src/FluentPDF.Core/Services/IRenderingSettingsService.cs`
    - `src/FluentPDF.App/Services/RenderingSettingsService.cs`
    - `tests/FluentPDF.App.Tests/Services/RenderingSettingsServiceTests.cs`
  - Create IRenderingSettingsService interface
  - Implement settings persistence using ApplicationData.LocalSettings
  - Implement observable pattern for quality changes
  - Add default value handling (Auto quality)
  - Write unit tests for persistence and notifications
  - Purpose: Manage rendering quality settings
  - _Leverage: ApplicationData, Subject<T>, Result<T> pattern_
  - _Requirements: 6.1-6.9_
  - _Prompt: Role: Settings Management Developer | Task: Implement rendering settings service with persistence and observable changes | Restrictions: Must persist settings, provide observable updates, handle defaults | Success: Quality settings persist and notify observers of changes_

- [x] 4. Extend PdfRenderingService to use dynamic DPI
  - Files:
    - `src/FluentPDF.Rendering/Services/PdfRenderingService.cs` (modify)
    - `tests/FluentPDF.Rendering.Tests/Services/PdfRenderingServiceTests.cs` (extend)
  - Modify RenderPageAsync to properly use effectiveDpi parameter
  - Add performance logging for high-DPI renders
  - Add out-of-memory handling with fallback to lower DPI
  - Update tests to verify DPI-based scaling
  - Purpose: Enable PDFium rendering at custom DPI
  - _Leverage: Existing PdfRenderingService, PDFium scaling_
  - _Requirements: 2.1-2.7, 4.1-4.7_
  - _Prompt: Role: Graphics Rendering Developer | Task: Extend PdfRenderingService to properly handle dynamic DPI with error recovery | Restrictions: Must handle OOM errors, log performance, scale correctly | Success: Can render at any DPI with proper error handling_

- [x] 5. Extend PdfViewerViewModel with DPI monitoring
  - Files:
    - `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (extend)
    - `tests/FluentPDF.App.Tests/ViewModels/PdfViewerViewModelDpiTests.cs`
  - Add DisplayInfo and RenderingQuality observable properties
  - Implement StartDpiMonitoring method subscribing to DPI changes
  - Modify RenderCurrentPageAsync to use CalculateEffectiveDpi
  - Add automatic re-rendering on DPI changes (> 10% threshold)
  - Add IsAdjustingQuality flag for UI feedback
  - Dispose DPI subscription properly
  - Write unit tests with mocked DPI service
  - Purpose: Integrate DPI detection with viewer
  - _Leverage: Existing PdfViewerViewModel, IDpiDetectionService_
  - _Requirements: 2.1-2.7, 3.1-3.7, 5.1-5.7_
  - _Prompt: Role: WinUI MVVM Developer | Task: Extend PdfViewerViewModel with DPI monitoring and automatic quality adjustment | Restrictions: Must subscribe/unsubscribe properly, re-render only on significant changes | Success: ViewModel monitors DPI and re-renders pages when DPI changes_

- [x] 6. Initialize DPI monitoring in PdfViewerPage
  - Files:
    - `src/FluentPDF.App/Views/PdfViewerPage.xaml.cs` (modify)
  - Call ViewModel.StartDpiMonitoring(this.XamlRoot) in Loaded event
  - Add "Adjusting quality..." overlay when IsAdjustingQuality is true
  - Handle XamlRoot becoming null on unload
  - Purpose: Connect DPI monitoring to UI lifecycle
  - _Leverage: Existing PdfViewerPage, ViewModel DPI methods_
  - _Requirements: 3.1-3.7, 5.1-5.7_
  - _Prompt: Role: WinUI UI Developer | Task: Initialize DPI monitoring in PdfViewerPage Loaded event | Restrictions: Must handle lifecycle properly, show quality adjustment feedback | Success: DPI monitoring starts when page loads and stops on unload_

- [x] 7. Create Settings Page quality UI
  - Files:
    - `src/FluentPDF.App/Views/SettingsPage.xaml` (create or extend)
    - `src/FluentPDF.App/ViewModels/SettingsViewModel.cs` (create or extend)
  - Add "Rendering Quality" section with ComboBox
  - Populate ComboBox with RenderingQuality options and descriptions
  - Bind to RenderingSettingsService
  - Show performance warning for Ultra on low-end devices
  - Add "Apply" button that triggers re-render
  - Purpose: Provide UI for quality settings
  - _Leverage: WinUI 3 controls, IRenderingSettingsService_
  - _Requirements: 6.1-6.9_
  - _Prompt: Role: WinUI Settings UI Developer | Task: Create settings UI for rendering quality selection | Restrictions: Must show descriptions, warn about performance, apply immediately | Success: Users can select quality and see changes applied_

- [ ] 8. Register DPI and settings services in DI container
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify)
  - Register IDpiDetectionService and implementation
  - Register IRenderingSettingsService and implementation
  - Verify dependencies resolve correctly
  - Purpose: Wire up DPI and settings services
  - _Leverage: Existing IHost DI container_
  - _Requirements: All integration_
  - _Prompt: Role: Application Integration Engineer | Task: Register DPI and rendering settings services in DI container | Restrictions: Follow existing DI patterns, use appropriate lifetimes | Success: All services registered and resolvable_

- [ ] 9. Add integration tests for HiDPI rendering
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Integration/HiDpiRenderingIntegrationTests.cs`
  - Test rendering at various DPI levels (96, 144, 192, 288)
  - Verify output image dimensions match expected size
  - Test memory usage at different DPI levels
  - Test fallback on out-of-memory
  - Verify no performance degradation
  - Purpose: Verify HiDPI rendering with real PDFium
  - _Leverage: Real PDFium, sample PDFs_
  - _Requirements: 2.1-2.7, 4.1-4.7, 7.1-7.7_
  - _Prompt: Role: QA Integration Engineer | Task: Create integration tests for HiDPI rendering at various DPI levels | Restrictions: Must test real rendering, verify dimensions, check memory | Success: Integration tests verify correct HiDPI rendering_

- [ ] 10. Add performance benchmarks for HiDPI rendering
  - Files:
    - `tests/FluentPDF.Rendering.Tests/Performance/HiDpiPerformanceBenchmarks.cs`
  - Create BenchmarkDotNet benchmarks for rendering at different DPIs
  - Measure render time and memory for 96, 144, 192, 288 DPI
  - Document baseline performance metrics
  - Verify < 2s render time at 2x DPI
  - Purpose: Measure and document HiDPI performance
  - _Leverage: BenchmarkDotNet, real PDFium_
  - _Requirements: 4.1-4.7, 7.1-7.7_
  - _Prompt: Role: Performance Engineer | Task: Create performance benchmarks for HiDPI rendering | Restrictions: Must use BenchmarkDotNet, test multiple DPI levels, document results | Success: Benchmarks show performance at various DPI levels_

- [ ] 11. Manual testing on multiple display configurations
  - Devices:
    - Standard 1080p monitor (100% scaling)
    - 4K monitor (150% scaling)
    - Surface Pro (200% scaling)
    - Multi-monitor setup
  - Test matrix: All quality levels on all devices
  - Verify smooth DPI transitions when moving windows
  - Document any issues or limitations
  - Purpose: Verify HiDPI on real hardware
  - _Requirements: 7.1-7.7_
  - _Prompt: Role: QA Manual Tester | Task: Perform manual testing on multiple display configurations | Restrictions: Must test all scaling levels, document issues | Success: HiDPI works correctly on all tested devices_

- [ ] 12. Final testing and documentation
  - Files:
    - `docs/ARCHITECTURE.md` (update)
    - `docs/HIDPI.md` (new)
    - `README.md` (update)
  - Update architecture documentation with DPI components
  - Create HIDPI.md documenting scaling support and quality settings
  - Update README with HiDPI feature description
  - Document performance characteristics at different DPI levels
  - Verify all requirements met
  - Purpose: Ensure feature is complete and documented
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: Role: Technical Writer and QA Lead | Task: Complete final validation and documentation for HiDPI support | Restrictions: Must verify all requirements, document performance | Success: Feature is production-ready with complete documentation_

## Summary

This spec implements HiDPI display scaling:
- Automatic DPI detection using XamlRoot
- Dynamic quality adjustment on display changes
- Quality settings with Auto, Low, Medium, High, Ultra
- Performance optimization and OOM handling
- Comprehensive testing on multiple displays
- Integration with existing rendering pipeline
