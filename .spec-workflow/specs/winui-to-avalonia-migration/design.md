# WinUI 3 to Avalonia Migration - Design

## Architecture Overview

### Current State (Problematic)
```
FluentPDF.App (WinUI 3)          FluentPDF.Avalonia
├── ViewModels (duplicate!)      ├── ViewModels (duplicate!)
├── Services (duplicate!)        ├── Services (duplicate!)
├── Controls                     ├── Controls
└── Views (XAML)                 └── Views (AXAML)
        ↓                               ↓
    FluentPDF.Core                FluentPDF.Core
    FluentPDF.Rendering           FluentPDF.Rendering
```

**Problem**: 11,000+ lines duplicated, features diverge, double maintenance

### Target State (Clean)
```
FluentPDF.Avalonia (ONLY UI)
├── Controls (Avalonia-specific)
├── Views (AXAML)
├── Converters (UI helpers)
└── Services (UI-only: AnimationService)
        ↓
FluentPDF.Core (SHARED LOGIC)
├── ViewModels (ALL business logic)
│   ├── MainViewModel
│   ├── PdfViewerViewModel
│   ├── AnnotationViewModel
│   ├── BookmarksViewModel
│   └── ThumbnailsViewModel
├── Services (Interfaces)
└── Models
        ↓
FluentPDF.Rendering (PDF ENGINE)
├── Services (Implementations)
│   ├── PdfRenderService
│   ├── AnnotationService
│   ├── TextExtractionService
│   ├── FormService
│   └── StampService
└── Interop (PDFium P/Invoke)
```

## Migration Strategy

### Phase 1: Analyze & Inventory (Day 1)
**Goal**: Understand what needs porting

1. **Inventory WinUI 3 Features**
   - List all ViewModels in FluentPDF.App/ViewModels/
   - List all Services in FluentPDF.App/Services/
   - List all Controls in FluentPDF.App/Controls/
   - Identify UI-specific vs business logic

2. **Compare with Avalonia**
   - Identify missing features in Avalonia
   - Identify outdated/broken code
   - List build errors to fix

3. **Dependency Analysis**
   - Map WinUI 3 APIs used (Microsoft.UI.Xaml.*)
   - Find Avalonia equivalents (Avalonia.Controls.*)
   - Identify platform-specific code

**Output**: Feature matrix, gap analysis, porting checklist

### Phase 2: Core Consolidation (Day 2-3)
**Goal**: Move shared logic to Core

1. **Move ViewModels to Core**
   - Create FluentPDF.Core/ViewModels/ directory
   - Move all ViewModels from FluentPDF.App
   - Remove UI framework dependencies
   - Use interfaces for UI-specific services

2. **Update Avalonia Project**
   - Reference ViewModels from Core
   - Delete duplicate ViewModels
   - Update DI registration

3. **Service Interfaces**
   - IAnimationService → Core (Avalonia implements)
   - ICoordinateMapper → Core (Avalonia implements)
   - Keep Rendering services where they are

**Key Principle**: If it's business logic or state management, it goes in Core.

### Phase 3: Avalonia Feature Porting (Day 4-6)
**Goal**: Achieve feature parity

1. **Text Selection & Annotations** (Priority 1)
   - Port CoordinateMapper to Avalonia
   - Port text selection logic to Avalonia views
   - Port annotation tools (H/U/S shortcuts)
   - Test in all view modes

2. **View Modes** (Priority 2)
   - Verify ContinuousScrollViewer works
   - Verify TwoPageViewer works
   - Port any missing functionality

3. **UI Controls** (Priority 3)
   - Port GlassPanel (liquid glass effects)
   - Port ShimmerPlaceholder
   - Port theme system
   - Update all AXAML files

4. **Document Operations** (Priority 4)
   - Verify merge/split/rotate work
   - Port any WinUI-specific implementations
   - Update dialogs to Avalonia

5. **Forms & Watermarks** (Priority 5)
   - Port form filling UI
   - Port watermark dialogs
   - Port stamp gallery

**Testing**: After each feature, run tests and manual UAT

### Phase 4: Fix Build Errors (Day 7)
**Goal**: Clean build on all platforms

1. **Fix Current Errors**
   - AnimationService.cs XML comments (lines 22, 537)
   - MainWindow.axaml.cs ToggleButton missing
   - PdfViewerViewModel missing properties/commands
   - PerformanceMonitor unused field

2. **Add Missing APIs**
   - Implement missing ViewModel commands
   - Add missing properties
   - Wire up event handlers

3. **Platform Testing**
   - Build on Windows ✓
   - Build on Linux ✓
   - Build on macOS (if available)

### Phase 5: Remove WinUI 3 (Day 8)
**Goal**: Clean removal

1. **Delete FluentPDF.App**
   - Remove project from solution
   - Delete directory
   - Update .gitignore

2. **Update Build Scripts**
   - Remove WinUI 3 build commands
   - Update CI/CD pipelines
   - Update package scripts

3. **Documentation**
   - Remove WinUI 3 references
   - Update README with Avalonia instructions
   - Update CLAUDE.md

4. **Final Cleanup**
   - Remove unused WinUI 3 packages
   - Update solution configurations
   - Clean bin/obj directories

### Phase 6: Testing & Validation (Day 9-10)
**Goal**: Everything works

1. **Automated Tests**
   - Run all unit tests
   - Run integration tests
   - Update E2E tests for Avalonia
   - Achieve 80%+ coverage

2. **Manual UAT**
   - Text selection & annotations
   - All view modes
   - Document operations
   - Forms and watermarks
   - Theme switching
   - Cross-platform testing

3. **Performance Validation**
   - Page rendering <500ms
   - Text extraction <100ms
   - Annotation creation <50ms
   - Startup time <3s

## Technical Details

### ViewModel Migration Pattern

**Before (WinUI 3)**:
```csharp
// FluentPDF.App/ViewModels/PdfViewerViewModel.cs
using Microsoft.UI.Xaml.Input; // WinUI specific!

public class PdfViewerViewModel : ViewModelBase
{
    private readonly IDispatcher _dispatcher; // WinUI dispatcher
}
```

**After (Core)**:
```csharp
// FluentPDF.Core/ViewModels/PdfViewerViewModel.cs
// No UI framework dependencies!

public class PdfViewerViewModel : ViewModelBase
{
    private readonly IDispatcherService _dispatcher; // Abstraction
}
```

**Avalonia Implementation**:
```csharp
// FluentPDF.Avalonia/Services/AvaloniaDispatcherService.cs
public class AvaloniaDispatcherService : IDispatcherService
{
    public void Dispatch(Action action)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(action);
    }
}
```

### Control Porting Pattern

**WinUI 3 → Avalonia Mapping**:
- `Microsoft.UI.Xaml.Controls.Button` → `Avalonia.Controls.Button`
- `Microsoft.UI.Xaml.Input.Pointer` → `Avalonia.Input.Pointer`
- `Microsoft.UI.Xaml.Media.CompositionTarget` → `Avalonia.Rendering.IRenderer`
- `Windows.UI.Color` → `Avalonia.Media.Color`

### Dependency Injection

**WinUI 3 (App.xaml.cs)**:
```csharp
services.AddSingleton<IDispatcher, WinUIDispatcher>();
```

**Avalonia (App.axaml.cs)**:
```csharp
services.AddSingleton<IDispatcherService, AvaloniaDispatcherService>();
services.AddSingleton<IAnimationService, AvaloniaAnimationService>();
```

## Risk Mitigation

### Risk 1: Feature Regressions
**Mitigation**: Comprehensive test suite, UAT checklist, feature matrix tracking

### Risk 2: Performance Degradation
**Mitigation**: Benchmark before/after, performance tests in CI

### Risk 3: Cross-Platform Issues
**Mitigation**: Test on all platforms early, use platform abstractions

### Risk 4: Build Breakage
**Mitigation**: Fix errors incrementally, maintain WinUI 3 until Avalonia stable

## Success Metrics

- ✅ Zero WinUI 3 code remaining
- ✅ All tests passing (≥80% coverage)
- ✅ All features working in Avalonia
- ✅ Clean build on Windows + Linux
- ✅ Performance targets met
- ✅ Code duplication eliminated (11,000+ lines removed)

## Timeline

- **Day 1**: Analysis & Inventory
- **Day 2-3**: Core Consolidation
- **Day 4-6**: Feature Porting
- **Day 7**: Build Fixes
- **Day 8**: WinUI Removal
- **Day 9-10**: Testing & Validation

**Total**: 10 days estimated
