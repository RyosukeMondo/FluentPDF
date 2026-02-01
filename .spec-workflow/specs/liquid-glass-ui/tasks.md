# Tasks Document: Liquid Glass UI Enhancement

## Phase 1: Theme Foundation (Week 1-2)

- [ ] 1.1. [NEXT] Create color palette resource dictionary
  **Action Steps**:
  1. Create directory src/FluentPDF.Avalonia/Styles/Theme/ if not exists
  2. Create file src/FluentPDF.Avalonia/Styles/Theme/Colors.axaml
  3. Add Light theme colors:
     - BackgroundPrimary: #FFFFFF
     - BackgroundSecondary: #F3F3F3
     - ForegroundPrimary: #000000
     - AccentPrimary: System accent color (use Windows.UI.ViewManagement.UISettings)
     - Add 15 total semantic colors (Surface, Border, Disabled, etc.)
  4. Add Dark theme colors (same semantic names, dark values)
  5. Add HighContrast theme colors (WCAG AAA compliant)
  6. Add ResourceDictionary.ThemeDictionaries with Light/Dark/HighContrast variants
  7. Build project: dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj
  8. Verify: Check Colors.axaml has 45+ color resources (15 colors × 3 themes)
  - File: `src/FluentPDF.Avalonia/Styles/Theme/Colors.axaml`
  - _Leverage: Existing `ThemeResources.axaml`_
  - _Requirements: 1.1, 1.5_
  - _Prompt: **Role**: UI/UX Designer specializing in color systems and accessibility | **Task**: Create comprehensive color palette for liquid glass UI following requirements 1.1 and 1.5, with Light/Dark/HighContrast variants ensuring WCAG AA compliance. Define colors for background, foreground, accent, acrylic tints, and UI states (hover, pressed, disabled). | **Restrictions**: Must maintain 4.5:1 contrast ratio for text, 3:1 for UI components. Do not hardcode colors in components. Follow Fluent Design color naming conventions. | **_Leverage**: Extend existing ThemeResources.axaml color definitions, use Windows accent color APIs | **Success**: All three theme variants defined, accessibility tested, colors dynamically switch based on system theme_

- [ ] 1.2. Create acrylic brushes and materials
  - File: `src/FluentPDF.Avalonia/Styles/Theme/Brushes.axaml`
  - Implement custom AcrylicMaterial for Avalonia
  - Define gradient brushes for depth effects
  - Purpose: GPU-accelerated glass materials
  - _Leverage: Avalonia `ExperimentalAcrylicMaterial`, compositor shaders_
  - _Requirements: 1.1_
  - _Prompt: **Role**: Graphics programmer with GPU shader and Avalonia rendering expertise | **Task**: Implement custom acrylic materials following requirement 1.1 using Avalonia's compositor for GPU-accelerated backdrop blur (20px radius, 60% tint opacity). Create reusable brush resources for toolbar, panels, and dialogs with varying blur intensities. | **Restrictions**: Must use GPU compositor, not software rendering. Detect GPU availability and fallback to solid colors gracefully. Max 5% CPU overhead for blur effects. | **_Leverage**: Avalonia ExperimentalAcrylicMaterial, Composition API, existing brush definitions | **Success**: Acrylic materials render at 60 FPS, GPU-accelerated blur working, graceful fallback on low-end hardware, < 10MB memory overhead_

- [ ] 1.3. Create typography scale resource dictionary
  - File: `src/FluentPDF.Avalonia/Styles/Theme/Typography.axaml`
  - Define Segoe UI Variable font family with size ramp
  - Set up line heights and font weights
  - Purpose: Consistent typography across app
  - _Leverage: Fluent Design type ramp (10pt, 12pt, 14pt, 18pt, 24pt)_
  - _Requirements: 1.6_
  - _Prompt: **Role**: Typography specialist with Fluent Design System expertise | **Task**: Create comprehensive typography scale following requirement 1.6 using Segoe UI Variable font with Fluent type ramp sizes (Caption: 10pt, Body: 12pt, BodyStrong: 14pt, Subtitle: 18pt, Title: 24pt). Define line heights (1.5x for body, 1.2x for headings) and font weights (Regular: 400, SemiBold: 600, Bold: 700). | **Restrictions**: Use only system-installed fonts, no custom font files. Ensure crisp rendering at all DPI scales (100%-300%). | **_Leverage**: Windows Segoe UI Variable font, Fluent Design typography guidelines | **Success**: All text sizes defined, scales correctly at HiDPI, readable at all sizes, consistent across components_

- [x] 1.4. Create spacing and layout grid system
  - File: `src/FluentPDF.Avalonia/Styles/Theme/Spacing.axaml`
  - Define 4px-based grid system (4, 8, 12, 16, 24, 32, 48)
  - Create margin and padding resource keys
  - Purpose: Consistent spacing and alignment
  - _Leverage: Fluent Design 4px grid_
  - _Requirements: 1.6_
  - _Prompt: **Role**: UI layout specialist with grid system design expertise | **Task**: Create 4px-based spacing system following requirement 1.6 with increments (XXS:4px, XS:8px, S:12px, M:16px, L:24px, XL:32px, XXL:48px). Define thickness resources for margins, padding, borders, and corner radii. | **Restrictions**: All spacing must be multiples of 4px. Do not create arbitrary spacing values. Ensure touch targets meet 44x44px minimum. | **_Leverage**: Fluent Design 4px grid, existing spacing conventions | **Success**: All spacing values multiples of 4px, consistent alignment across app, touch targets accessible_

- [x] 1.5. Create ThemeService for runtime theme switching
  - Files: `src/FluentPDF.Avalonia/Services/IThemeService.cs`, `src/FluentPDF.Avalonia/Services/ThemeService.cs`
  - Implement theme switching with hot-reload
  - Add system accent color detection
  - Purpose: Centralized theme management
  - _Leverage: Avalonia `IResourceProvider`, reactive extensions_
  - _Requirements: 1.5_
  - _Prompt: **Role**: C# developer with Avalonia and reactive programming expertise | **Task**: Implement IThemeService and ThemeService following requirement 1.5 for runtime theme switching. Detect system theme changes, provide IObservable<ThemeVariant> stream, support Light/Dark/HighContrast. Use Result pattern for error handling. | **Restrictions**: Must not require app restart for theme changes. Use reactive streams, not polling. Log all theme changes with correlation IDs. | **_Leverage**: Avalonia Application.RequestedThemeVariant, System.Reactive, FluentResults, Serilog | **Success**: Theme switches instantly without restart, system theme changes detected, errors handled gracefully, all changes logged_

- [ ] 1.6. Register ThemeService in dependency injection
  - File: `src/FluentPDF.Avalonia/App.axaml.cs` (modify existing)
  - Add ThemeService registration in ConfigureServices
  - Set up singleton lifetime
  - Purpose: Enable service injection
  - _Leverage: Existing DI configuration in App.axaml.cs_
  - _Requirements: 1.5_
  - _Prompt: **Role**: .NET architect with DI container expertise | **Task**: Register ThemeService following requirement 1.5 in Microsoft.Extensions.DependencyInjection container with singleton lifetime. Inject into MainViewModel and SettingsViewModel. | **Restrictions**: Must use constructor injection, not service locator. Singleton lifetime only. Do not create circular dependencies. | **_Leverage**: Existing DI setup in App.axaml.cs, IServiceCollection | **Success**: ThemeService injectable in all ViewModels, singleton instance reused, no circular dependencies_

## Phase 2: Core Components (Week 3-4)

- [ ] 2.1. Create GlassPanel reusable control
  - Files: `src/FluentPDF.Avalonia/Controls/GlassPanel.axaml`, `src/FluentPDF.Avalonia/Controls/GlassPanel.axaml.cs`
  - Implement frosted glass panel with backdrop blur
  - Add BlurRadius, TintOpacity, Elevation properties
  - Purpose: Reusable glass container for panels and dialogs
  - _Leverage: AcrylicMaterial from task 1.2, Avalonia Panel base class_
  - _Requirements: 1.1, 1.2_
  - _Prompt: **Role**: Avalonia controls developer with custom rendering expertise | **Task**: Create GlassPanel UserControl following requirements 1.1 and 1.2 with properties BlurRadius (20px default), TintOpacity (0.6 default), Elevation (0-32dp shadow), CornerRadius (8px default). Apply acrylic material from Brushes.axaml, render drop shadow based on elevation. | **Restrictions**: Must extend Avalonia.Controls.Panel. Use GPU compositor for blur. File ≤500 lines. | **_Leverage**: Brushes.axaml AcrylicMaterial, Avalonia Panel, DropShadowEffect | **Success**: Panel renders with glass effect, properties work correctly, performs at 60 FPS, file <500 lines_

- [ ] 2.2. Create LiquidButton with ripple effect
  - Files: `src/FluentPDF.Avalonia/Controls/LiquidButton.axaml`, `src/FluentPDF.Avalonia/Controls/LiquidButton.axaml.cs`
  - Implement button with pointer-origin ripple animation
  - Add scale-down press feedback (0.95x)
  - Purpose: Enhanced button with micro-interactions
  - _Leverage: ButtonStyles.axaml, Avalonia Transitions_
  - _Requirements: 1.4_
  - _Prompt: **Role**: UI animation developer with pointer event expertise | **Task**: Create LiquidButton extending Avalonia Button following requirement 1.4. Implement ripple effect originating from pointer position using RadialGradientBrush animation. Add 0.95x scale on press with 150ms cubic-ease-out transition. | **Restrictions**: Use Composition API for animations. Must work with mouse, touch, and pen input. File ≤500 lines. | **_Leverage**: ButtonStyles.axaml base styling, Avalonia Transitions, PointerPressed events | **Success**: Ripple animates from pointer origin, press feedback tactile, works on all input types, 60 FPS maintained_

- [ ] 2.3. Enhance existing SearchPanel with glass styling
  - Files: `src/FluentPDF.Avalonia/Controls/SearchPanel.axaml`, `src/FluentPDF.Avalonia/Controls/SearchPanel.axaml.cs` (modify)
  - Apply GlassPanel as container
  - Add slide-in/out animations (250ms)
  - Purpose: Modernize search with liquid glass aesthetic
  - _Leverage: GlassPanel from task 2.1, existing SearchPanel logic_
  - _Requirements: 1.1, 1.3_
  - _Prompt: **Role**: Avalonia developer with animation and refactoring expertise | **Task**: Refactor SearchPanel following requirements 1.1 and 1.3 to use GlassPanel container with 20px blur. Add slide-in from top (250ms cubic-ease-out) when IsVisible=true, slide-out when false. Preserve all existing search functionality. | **Restrictions**: Must not break existing search logic. Maintain ViewModel separation. File ≤500 lines. | **_Leverage**: GlassPanel, existing SearchPanelViewModel, Avalonia Transitions | **Success**: Panel has glass effect, slides smoothly, search functionality intact, file <500 lines_

- [ ] 2.4. Enhance ThumbnailsSidebar with glass and virtualization
  - Files: `src/FluentPDF.Avalonia/Controls/ThumbnailsSidebar.axaml`, `src/FluentPDF.Avalonia/Controls/ThumbnailsSidebar.axaml.cs` (modify)
  - Apply GlassPanel styling
  - Implement virtualization for 1000+ thumbnails
  - Add shimmer loading placeholders
  - Purpose: Performant thumbnail panel with glass aesthetic
  - _Leverage: GlassPanel from task 2.1, Avalonia VirtualizingStackPanel_
  - _Requirements: 1.1, 1.6.5_
  - _Prompt: **Role**: Performance-focused Avalonia developer with virtualization expertise | **Task**: Refactor ThumbnailsSidebar following requirements 1.1 and 1.6.5 to use GlassPanel and VirtualizingStackPanel for efficient rendering of 1000+ thumbnails. Implement lazy loading with shimmer placeholders for off-screen items. | **Restrictions**: Must maintain 60 FPS scrolling. Memory usage ≤ 10MB additional for 1000 thumbnails. File ≤500 lines. | **_Leverage**: GlassPanel, VirtualizingStackPanel, existing ThumbnailsViewModel | **Success**: Scrolls smoothly with 1000+ pages, only visible thumbnails rendered, shimmer placeholders shown, memory efficient_

- [x] 2.5. Enhance BookmarksPanel with glass and expand animations
  - Files: `src/FluentPDF.App/Controls/BookmarksPanel.xaml`, `src/FluentPDF.App/Controls/BookmarksPanel.xaml.cs` (modified)
  - Applied GlassPanel styling with 8dp elevation and acrylic backdrop
  - Added smooth expand/collapse animations (200ms ease-out with CubicEase) for tree nodes
  - Purpose: Modernized bookmarks with glass aesthetic
  - _Leverage: GlassPanel from task 2.1, existing BookmarksViewModel_
  - _Requirements: 1.1, 1.3_
  - _Completed: XAML 134 lines, C# 106 lines (total 240 lines, well under 500 limit). Preserved bookmark navigation functionality. Implemented Opacity and ScaleY animations for smooth expand/collapse transitions._

- [ ] 2.6. Refactor MainWindow toolbar with acrylic background
  - File: `src/FluentPDF.Avalonia/Views/MainWindow.axaml` (modify)
  - Apply acrylic brush to toolbar background
  - Enhance button styles with LiquidButton
  - Purpose: Modernize main toolbar aesthetic
  - _Leverage: Brushes.axaml acrylic, LiquidButton from task 2.2_
  - _Requirements: 1.1_
  - _Prompt: **Role**: XAML specialist with Avalonia styling expertise | **Task**: Refactor MainWindow toolbar following requirement 1.1 to use AcrylicBrush background (60% tint opacity) and replace standard buttons with LiquidButton controls. Maintain all existing toolbar functionality and command bindings. | **Restrictions**: Must preserve all commands and bindings. File ≤500 lines. No logic changes to MainWindow.axaml.cs. | **_Leverage**: Brushes.axaml, LiquidButton, existing MainViewModel | **Success**: Toolbar has acrylic background, buttons have ripple effects, all commands work, file <500 lines_

## Phase 3: Advanced Effects & Animations (Week 5-6)

- [x] 3.1. Create AnimationService for centralized control
  - Files: `src/FluentPDF.Avalonia/Services/IAnimationService.cs`, `src/FluentPDF.Avalonia/Services/AnimationService.cs`
  - Implement page transition animations (slide, fade, zoom)
  - Detect reduced motion accessibility setting
  - Purpose: Centralized animation orchestration
  - _Leverage: Avalonia Transitions, System accessibility APIs_
  - _Requirements: 1.3, 1.4_
  - _Prompt: **Role**: Animation developer with accessibility and reactive programming expertise | **Task**: Implement IAnimationService and AnimationService following requirements 1.3 and 1.4. Create AnimatePageTransitionAsync (slide 350ms, fade 300ms, zoom 400ms), AnimatePanelSlideAsync (250ms), AnimateFadeAsync. Detect Windows reduced motion setting, disable animations if enabled. Use Task<void> for async orchestration. | **Restrictions**: Must check accessibility settings on startup. All animations via Composition API. Use Result pattern for errors. | **_Leverage**: Avalonia ITransition, Composition API, FluentResults, Serilog | **Success**: Animations smooth at 60 FPS, reduced motion respected, errors logged, async properly implemented_

- [ ] 3.2. Implement page transition animations in PdfViewerViewModel
  - File: `src/FluentPDF.Avalonia/ViewModels/PdfViewerViewModel.cs` (modify)
  - Inject IAnimationService
  - Add slide transitions on page navigation
  - Purpose: Smooth page changes with animations
  - _Leverage: AnimationService from task 3.1_
  - _Requirements: 1.3_
  - _Prompt: **Role**: MVVM developer with animation integration expertise | **Task**: Enhance PdfViewerViewModel following requirement 1.3 to use IAnimationService for page transitions. Inject service via constructor, call AnimatePageTransitionAsync on GoToNextPage/GoToPreviousPage commands. Handle animation cancellation on rapid navigation. | **Restrictions**: Must not block UI thread. Handle rapid page changes gracefully. File ≤500 lines. | **_Leverage**: AnimationService, existing navigation commands | **Success**: Pages slide smoothly, rapid navigation cancels previous animation, UI responsive, file <500 lines_

- [ ] 3.3. Create PerformanceMonitor service for FPS tracking
  - Files: `src/FluentPDF.Avalonia/Services/IPerformanceMonitor.cs`, `src/FluentPDF.Avalonia/Services/PerformanceMonitor.cs`
  - Track real-time FPS using Avalonia render timer
  - Log performance metrics with Serilog
  - Purpose: Monitor and log animation performance
  - _Leverage: Avalonia IRenderTimer, Serilog_
  - _Requirements: 1.6.2_
  - _Prompt: **Role**: Performance engineer with Avalonia rendering pipeline expertise | **Task**: Implement IPerformanceMonitor following requirement 1.6.2 to track FPS using Avalonia's IRenderTimer. Calculate CurrentFPS, MemoryUsageMB (managed + native), expose IObservable<PerformanceMetrics> stream. Log metrics every 5 seconds with Serilog. | **Restrictions**: Must use IRenderTimer, not manual timers. Calculate FPS over 1-second rolling window. Memory tracking via Process.WorkingSet64. | **_Leverage**: Avalonia IRenderTimer, System.Diagnostics.Process, Serilog, System.Reactive | **Success**: FPS tracked accurately, memory reported correctly, metrics logged, stream emits every 5s_

- [ ] 3.4. Create diagnostics panel UI with real-time metrics
  - Files: `src/FluentPDF.Avalonia/Views/DiagnosticsPanel.axaml`, `src/FluentPDF.Avalonia/Views/DiagnosticsPanel.axaml.cs`, `src/FluentPDF.Avalonia/ViewModels/DiagnosticsPanelViewModel.cs`
  - Display FPS, memory, render timing
  - Toggle visibility with Ctrl+Shift+D
  - Purpose: Developer diagnostics overlay
  - _Leverage: PerformanceMonitor from task 3.3, GlassPanel_
  - _Requirements: 1.4.2_
  - _Prompt: **Role**: UI developer with data visualization and MVVM expertise | **Task**: Create DiagnosticsPanel following requirement 1.4.2 showing real-time FPS (green >60, yellow 30-60, red <30), memory usage MB, and frame timing graph. Use GlassPanel container. Bind to DiagnosticsPanelViewModel subscribing to PerformanceMonitor stream. Toggle with Ctrl+Shift+D KeyBinding. | **Restrictions**: Panel must overlay content (not push). Update UI at max 60Hz. File ≤500 lines each. | **_Leverage**: PerformanceMonitor, GlassPanel, System.Reactive | **Success**: Panel shows live metrics, FPS color-coded, keyboard shortcut works, overlays correctly_

- [ ] 3.5. Implement adaptive quality based on performance
  - File: `src/FluentPDF.Avalonia/Services/AnimationService.cs` (modify)
  - Detect sustained low FPS (<30 for >1 second)
  - Automatically simplify or disable animations
  - Purpose: Maintain responsiveness on low-end hardware
  - _Leverage: PerformanceMonitor from task 3.3_
  - _Requirements: 1.6.2_
  - _Prompt: **Role**: Performance optimization specialist with reactive programming expertise | **Task**: Enhance AnimationService following requirement 1.6.2 to subscribe to PerformanceMonitor metrics. If FPS <30 for >60 consecutive frames (~1 second), disable animations by setting IsMotionEnabled=false. Log performance degradation. Allow manual re-enable. | **Restrictions**: Must use reactive streams, not polling. Hysteresis: require FPS >45 for 5 seconds before re-enabling. | **_Leverage**: PerformanceMonitor IObservable, System.Reactive operators | **Success**: Animations auto-disable on low FPS, auto-resume when performance improves, logged appropriately_

## Phase 4: Polish & Testing (Week 7-8)

- [x] 4.1. Create visual regression tests with Verify.Xaml
  - Files: `tests/FluentPDF.App.Tests/Controls/GlassPanelVisualTests.cs`, `tests/FluentPDF.App.Tests/Controls/LiquidButtonVisualTests.cs`
  - Snapshot test GlassPanel in Light/Dark themes with varying elevation/corner radius
  - Snapshot test LiquidButton states (normal, hover, pressed, disabled) and styles (default, primary, compact, with icon)
  - Purpose: Prevent visual regressions
  - _Leverage: Verify.Xaml, Win2D for headless rendering_
  - _Requirements: 1.3.1_
  - _Completed: Created comprehensive visual regression test suite with 30 test scenarios (14 GlassPanel variants, 16 LiquidButton variants). Implemented Win2D CanvasRenderTarget for headless CI rendering. Added VerifyConfig.cs for global settings with 95% SSIM threshold. Tests cover all elevation levels (0dp, 8dp, 32dp), corner radii (0px, 8px, 16px), button states (Normal, PointerOver, Pressed, Disabled), and both Light/Dark themes. Documentation in README_VISUAL_TESTS.md._

- [ ] 4.2. Create performance benchmarks with BenchmarkDotNet
  - File: `tests/FluentPDF.Avalonia.Benchmarks/AnimationBenchmarks.cs`
  - Benchmark page transition animation frame times
  - Benchmark acrylic blur rendering performance
  - Purpose: Ensure 60 FPS maintained
  - _Leverage: BenchmarkDotNet, MemoryDiagnoser_
  - _Requirements: 1.6.2_
  - _Prompt: **Role**: Performance testing specialist with BenchmarkDotNet expertise | **Task**: Create performance benchmarks following requirement 1.6.2 measuring page transition frame times (must average <16ms for 60 FPS) and acrylic blur overhead (must be <5% CPU). Use [MemoryDiagnoser] to track memory allocations. Fail if FPS <30 or memory increase >10MB. | **Restrictions**: Run with Release configuration. Measure both managed and native memory. Use realistic PDF page rendering scenarios. | **_Leverage**: BenchmarkDotNet, MemoryDiagnoser, NativeMemoryProfiler | **Success**: Benchmarks show >60 FPS maintained, memory usage acceptable, runs in CI, clear performance baselines_

- [x] 4.3. Create FlaUI integration tests for theme switching
  - File: `tests/FluentPDF.App.Tests/IntegrationTests/ThemeSwitchingTests.cs`
  - Test theme toggle button functionality via RadioButton controls
  - Verify visual changes via AutomationPeer (IsSelected property)
  - Purpose: Validate theme switching end-to-end
  - _Leverage: FlaUI, AutomationId attributes, Page Object Pattern_
  - _Requirements: 1.5_
  - _Implemented: Created comprehensive integration tests with 3 test methods (ThemeCycle, ImmediateApplication, Persistence). Implements Page Object Pattern (SettingsPageObject class) with retry strategy (max 3 retries, 1s delay). Tests Light→Dark→System cycle with AutomationPeer verification. Screenshot capture on failures. 325 lines total (within 500 limit)._

- [x] 4.4. Create accessibility audit tests
  - Files: `tests/FluentPDF.E2E.Tests/Tests/AccessibilityTests.cs`, `tests/FluentPDF.E2E.Tests/Fixtures/ColorContrastAnalyzer.cs`
  - Implemented comprehensive WCAG 2.1 Level AA compliance testing with 14 test methods
  - Keyboard navigation: Tab/Shift+Tab focus order, keyboard shortcuts (Ctrl+O, Ctrl+F), Enter/Space activation
  - Screen reader support: AutomationProperties.Name validation, descriptive labels, state announcements
  - High contrast mode: Theme detection, acrylic fallback validation (manual verification required)
  - Color contrast ratios: WCAG AA validation (4.5:1 text, 3:1 UI) with screen capture-based color extraction
  - Purpose: Ensure WCAG 2.1 Level AA compliance
  - _Completed: 607 lines AccessibilityTests.cs + 299 lines ColorContrastAnalyzer.cs. Leveraged FlaUI for UI automation, System.Drawing for screen capture color analysis. Implements relative luminance calculation per WCAG spec. Updated E2E test project to .NET 9 for compatibility._

- [ ] 4.5. Create ArchUnitNET architecture tests
  - File: `tests/FluentPDF.Architecture.Tests/LiquidGlassUiArchitectureTests.cs`
  - Validate ViewModels have no Avalonia dependencies
  - Validate no circular dependencies
  - Validate naming conventions (ViewModel suffix, Service suffix)
  - Purpose: Enforce architecture constraints
  - _Leverage: ArchUnitNET_
  - _Requirements: 1.2.5_
  - _Prompt: **Role**: Software architect with architecture testing and ArchUnitNET expertise | **Task**: Create architecture tests following requirement 1.2.5 using ArchUnitNET to validate: (1) ViewModels do not reference Avalonia namespaces, (2) No circular dependencies in Services layer, (3) All ViewModels end with "ViewModel" suffix, (4) All services implement I{ServiceName} interface. Fail CI build if violated. | **Restrictions**: Tests must run fast (<5s total). Use predicates for flexible rules. Clear violation messages. | **_Leverage**: ArchUnitNET, xUnit | **Success**: All architecture rules enforced, violations fail build, clear error messages, fast execution_

- [ ] 4.6. Create comprehensive documentation
  - Files: `docs/liquid-glass-ui/README.md`, `docs/liquid-glass-ui/COMPONENTS.md`, `docs/liquid-glass-ui/THEMING.md`, `docs/liquid-glass-ui/ANIMATIONS.md`
  - Document component usage with examples
  - Document theming system and customization
  - Document animation configuration
  - Purpose: Enable future development and maintenance
  - _Leverage: XML doc comments, DocFX_
  - _Requirements: All_
  - _Prompt: **Role**: Technical writer with software development and documentation expertise | **Task**: Create comprehensive documentation covering all requirements. Write README.md (overview, quick start, architecture), COMPONENTS.md (GlassPanel, LiquidButton usage examples), THEMING.md (color customization, creating new themes), ANIMATIONS.md (animation service usage, performance optimization). Include code examples, screenshots, and diagrams. | **Restrictions**: Use Markdown with Mermaid diagrams. Include runnable code examples. Screenshot quality 2x DPI. | **_Leverage**: XML doc comments, Mermaid, MarkdownSnippets | **Success**: All components documented, usage examples clear, theming guide complete, animations explained_

- [ ] 4.7. Final integration and polish
  - Files: Multiple (code cleanup and refinement)
  - Remove debug logging from production builds
  - Optimize resource dictionary loading
  - Clean up unused code and resources
  - Purpose: Production-ready release
  - _Leverage: Roslyn analyzers, EditorConfig_
  - _Requirements: All_
  - _Prompt: **Role**: Senior developer with code quality and cleanup expertise | **Task**: Perform final integration polish covering all requirements. Remove/reduce debug logging (only keep Info+). Optimize resource loading (merge small dictionaries, defer non-critical). Run Roslyn analyzers, fix all warnings. Remove unused using statements, dead code. Verify all files ≤500 lines. Run full test suite. | **Restrictions**: Do not break existing functionality. All tests must pass. No new warnings introduced. | **_Leverage**: Roslyn analyzers, EditorConfig, ReSharper cleanup | **Success**: Zero warnings, all tests pass, resource loading optimized, code clean and maintainable_

## Implementation Guidelines

- **File Size Limit**: Each file MUST be ≤500 lines (excluding comments/blank lines)
- **Function Size Limit**: Each function MUST be ≤50 lines
- **Test Coverage**: Aim for ≥80% overall, ≥90% for ViewModels and services
- **Performance**: All animations MUST maintain 60 FPS; fail PR if <30 FPS sustained
- **Accessibility**: All interactive elements MUST have AutomationProperties.Name
- **Error Handling**: Use FluentResults, never throw exceptions for expected failures
- **Logging**: Use Serilog with correlation IDs for all operations

## Task Execution Order

**Critical Path** (must complete in order):
1. Phase 1 (1.1 → 1.6) - Theme foundation required by all
2. Phase 2 (2.1 → 2.6) - Components depend on theme
3. Phase 3 (3.1 → 3.5) - Animations depend on components
4. Phase 4 (4.1 → 4.7) - Testing and polish

**Parallelizable Tasks**:
- 1.1, 1.2, 1.3, 1.4 can run in parallel (different files)
- 2.1, 2.2 can run in parallel (different controls)
- 2.3, 2.4, 2.5, 2.6 can run in parallel (different files, depend on 2.1)
- 4.1, 4.2, 4.3, 4.4, 4.5 can run in parallel (different test types)

## Swarm Coordination Strategy

Launch agents in parallel for maximum speed:
- **Theme Team** (Tasks 1.1-1.4): 4 agents creating color/brush/typography/spacing
- **Service Team** (Tasks 1.5-1.6, 3.1, 3.3): 4 agents for services
- **Component Team** (Tasks 2.1-2.6): 6 agents for controls/views
- **Animation Team** (Tasks 3.2, 3.4, 3.5): 3 agents for animation integration
- **Testing Team** (Tasks 4.1-4.5): 5 agents for comprehensive testing
- **Documentation Team** (Task 4.6): 1 agent for docs
- **Polish Team** (Task 4.7): 1 agent for final cleanup

**Total: 24 parallel agents** executing tasks simultaneously for rapid completion
