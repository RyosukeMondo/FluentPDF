# Requirements: Architecture Consolidation

## 1. Overview

Eliminate ~20,000 lines of duplicate code between FluentPDF.App (WinUI 3) and FluentPDF.Avalonia by enforcing Single Source of Truth (SSOT), consolidating shared logic in Core/Rendering layers, and making UI projects truly thin presentation layers.

## 2. User Stories

### 2.1 Single Source of Truth for Business Logic
**As a** developer
**I want** business logic in one place (Core/Rendering)
**So that** bugs are fixed once and features work consistently across UIs

**EARS Criteria:**
- **WHEN** any business operation executes
- **THE SYSTEM SHALL** use shared Core/Rendering services
- **AND** UI projects SHALL NOT duplicate logic

### 2.2 Shared ViewModels Base Classes
**As a** UI developer
**I want** common ViewModel logic in shared base classes
**So that** I don't duplicate MVVM patterns across UI frameworks

**EARS Criteria:**
- **WHERE** ViewModels exist in UI projects
- **THE SYSTEM SHALL** inherit from shared base ViewModels in Core
- **EXCEPT** framework-specific UI bindings

### 2.3 Unified API Server
**As a** QA engineer
**I want** one REST API implementation
**So that** testing works identically across both UIs

**EARS Criteria:**
- **WHEN** REST API server runs
- **THE SYSTEM SHALL** use shared API implementation from Core
- **AND** both UI projects SHALL host the same server

### 2.4 Consolidated Service Implementations
**As a** maintainer
**I want** services implemented once
**So that** maintenance cost is reduced and quality improves

**EARS Criteria:**
- **WHERE** services exist (Settings, RecentFiles, Navigation, etc.)
- **THE SYSTEM SHALL** implement once in Core with interfaces
- **AND** UI projects SHALL provide only thin adapters

### 2.5 Shared Diagnostics and Telemetry
**As a** support engineer
**I want** consistent diagnostics across UIs
**So that** debugging and monitoring work the same way

**EARS Criteria:**
- **WHEN** diagnostics or telemetry execute
- **THE SYSTEM SHALL** use shared telemetry service from Core
- **AND** both UIs SHALL report identical metrics

## 3. Functional Requirements

### 3.1 Code Duplication Analysis

**FR-3.1.1:** System SHALL identify all duplicated code:
- Services (SettingsService, RecentFilesService, AnimationService, etc.)
- API endpoints and server implementation
- ViewModels base classes
- Diagnostic commands
- Telemetry and logging configuration

**FR-3.1.2:** System SHALL measure duplication:
- Lines of code duplicated
- Percentage of codebase duplicated
- Maintenance cost estimate

**FR-3.1.3:** System SHALL create consolidation priority:
- High: Services with business logic
- Medium: ViewModels and API
- Low: UI-specific helpers

### 3.2 Shared Core Services

**FR-3.2.1:** System SHALL move services to Core:
- `ISettingsService` with cross-platform implementation
- `IRecentFilesService` with file history
- `ITelemetryService` with metrics collection
- `INavigationService` (abstraction, UI-specific implementation)
- `IFileDialogService` (abstraction, UI-specific implementation)
- `IThemeService` (abstraction, UI-specific theme mapping)

**FR-3.2.2:** Services SHALL be platform-agnostic:
- Use abstractions for platform-specific features
- No WinUI or Avalonia types in Core
- All paths use cross-platform System.IO
- Settings storage uses cross-platform JSON

**FR-3.2.3:** Services SHALL use Dependency Injection:
- Register in DI container
- Constructor injection only
- Interface-based dependencies
- Proper lifetime management (singleton, scoped, transient)

### 3.3 Shared ViewModel Base Classes

**FR-3.3.1:** System SHALL create base ViewModels in FluentPDF.Core.ViewModels:
```
- ViewModelBase (INotifyPropertyChanged, RelayCommand support)
- DocumentViewModelBase (PDF document operations)
- PageViewModelBase (page-level operations)
- ToolbarViewModelBase (toolbar button states)
```

**FR-3.3.2:** Base ViewModels SHALL be framework-agnostic:
- Use CommunityToolkit.Mvvm (supports both WinUI and Avalonia)
- No platform-specific types
- Observable collections use standard types
- Commands use IRelayCommand

**FR-3.3.3:** UI-specific ViewModels SHALL inherit from base:
```csharp
// Core
public abstract class MainViewModelBase : ViewModelBase
{
    protected readonly IPdfDocumentService _documentService;
    public IRelayCommand OpenDocumentCommand { get; }
    // ... shared logic
}

// WinUI
public class MainViewModel : MainViewModelBase
{
    // WinUI-specific bindings only
    public XamlRoot? XamlRoot { get; set; }
}

// Avalonia
public class MainViewModel : MainViewModelBase
{
    // Avalonia-specific bindings only
    public Visual? Visual { get; set; }
}
```

### 3.4 Unified REST API

**FR-3.4.1:** System SHALL consolidate API into FluentPDF.Core.Api:
- Move all endpoints from App.Api and Avalonia.Api
- Shared DocumentSessionManager
- Shared HashingService
- Shared UiAutomationService (platform-abstracted)

**FR-3.4.2:** API SHALL be framework-agnostic:
- Use ASP.NET Core minimal APIs
- No WinUI/Avalonia dependencies
- Platform-specific UI automation via abstraction

**FR-3.4.3:** UI projects SHALL host shared API:
```csharp
// Both projects
app.Services.UseVerificationApi(); // Extension method from Core
```

### 3.5 Shared Diagnostics

**FR-3.5.1:** System SHALL consolidate diagnostic commands into Core:
- Move from App/Diagnostics to Core/Diagnostics
- Platform-agnostic command handlers
- Shared diagnostic models

**FR-3.5.2:** System SHALL provide diagnostic abstraction:
```csharp
public interface IDiagnosticProvider
{
    SystemInfo GetSystemInfo();
    MemoryInfo GetMemoryInfo();
    RenderingInfo GetRenderingInfo();
}
```

### 3.6 Configuration Consolidation

**FR-3.6.1:** System SHALL use unified configuration:
- Single appsettings.json structure
- Shared configuration models
- Platform-specific sections when needed

**FR-3.6.2:** Settings SHALL be cross-platform:
- Use %APPDATA%/FluentPDF on Windows
- Use ~/.config/FluentPDF on Linux/macOS
- JSON format for all settings

## 4. Non-Functional Requirements

### 4.1 Maintainability

**NFR-4.1.1:** Duplicate code SHALL be reduced by &gt;90%

**NFR-4.1.2:** Services SHALL be in Core, not duplicated in UI projects

**NFR-4.1.3:** Code coverage SHALL be &gt;80% for shared Core code

**NFR-4.1.4:** Architecture tests SHALL enforce no duplication

### 4.2 Consistency

**NFR-4.2.1:** Both UIs SHALL behave identically for business logic

**NFR-4.2.2:** Settings SHALL sync across applications (same storage)

**NFR-4.2.3:** Diagnostics SHALL report identical information

### 4.3 Performance

**NFR-4.3.1:** Consolidation SHALL NOT degrade performance

**NFR-4.3.2:** Shared services SHALL be efficient (no extra overhead)

**NFR-4.3.3:** ViewModels SHALL update UI at same speed as before

### 4.4 Testability

**NFR-4.4.1:** Shared services SHALL be 100% unit testable

**NFR-4.4.2:** ViewModels SHALL be testable without UI framework

**NFR-4.4.3:** API SHALL be testable with integration tests

## 5. Constraints

### 5.1 Technical Constraints

**CON-5.1.1:** Must maintain .NET 8 compatibility

**CON-5.1.2:** Must support both WinUI 3 and Avalonia UI

**CON-5.1.3:** Must use CommunityToolkit.Mvvm (works with both frameworks)

**CON-5.1.4:** Cannot change external APIs or user experience

### 5.2 Business Constraints

**CON-5.2.1:** Migration must be incremental (service by service)

**CON-5.2.2:** Both UIs must continue working during migration

**CON-5.2.3:** No user-facing changes

## 6. Acceptance Criteria

### 6.1 Code Quality

**AC-6.1.1:** Zero duplicate service implementations

**AC-6.1.2:** &gt;90% reduction in duplicate lines of code

**AC-6.1.3:** Architecture tests enforce SSOT

**AC-6.1.4:** All shared code in Core/Rendering projects

### 6.2 Functionality

**AC-6.2.1:** All features work identically in both UIs

**AC-6.2.2:** Settings sync between WinUI and Avalonia apps

**AC-6.2.3:** API works the same in both hosting scenarios

### 6.3 Testing

**AC-6.3.1:** Shared services have &gt;80% test coverage

**AC-6.3.2:** ViewModels testable without UI frameworks

**AC-6.3.3:** Integration tests verify both UI projects

### 6.4 Documentation

**AC-6.4.1:** Architecture diagram shows consolidation

**AC-6.4.2:** Migration guide documents process

**AC-6.4.3:** Service documentation explains platform abstractions

## 7. Current Duplication Analysis

### 7.1 Duplicated Services

| Service | App Lines | Avalonia Lines | Total Waste |
|---------|-----------|----------------|-------------|
| SettingsService | 350 | 340 | 690 |
| RecentFilesService | 280 | 275 | 555 |
| AnimationService | 180 | 175 | 355 |
| ThemeService | 220 | 210 | 430 |
| NavigationService | 150 | 145 | 295 |
| **Total Services** | **1,180** | **1,145** | **2,325** |

### 7.2 Duplicated API

| Component | App Lines | Avalonia Lines | Total Waste |
|-----------|-----------|----------------|-------------|
| API Endpoints | 450 | 450 | 900 |
| DocumentSessionManager | 180 | 180 | 360 |
| HashingService | 120 | 120 | 240 |
| UiAutomationService | 200 | 200 | 400 |
| **Total API** | **950** | **950** | **1,900** |

### 7.3 Duplicated ViewModels

| ViewModel | App Lines | Avalonia Lines | Total Waste |
|-----------|-----------|----------------|-------------|
| MainViewModel | 800 | 780 | 1,580 |
| PdfViewerViewModel | 650 | 640 | 1,290 |
| AnnotationViewModel | 400 | 390 | 790 |
| BookmarksViewModel | 300 | 290 | 590 |
| SettingsViewModel | 250 | 245 | 495 |
| **Total ViewModels** | **2,400** | **2,345** | **4,745** |

### 7.4 Other Duplication

| Component | App Lines | Avalonia Lines | Total Waste |
|-----------|-----------|----------------|-------------|
| Diagnostics | 500 | 490 | 990 |
| Converters | 300 | 295 | 595 |
| Helpers | 250 | 245 | 495 |
| **Total Other** | **1,050** | **1,030** | **2,080** |

### 7.5 Total Duplication Summary

| Category | Duplicate Lines |
|----------|----------------|
| Services | 2,325 |
| API | 1,900 |
| ViewModels | 4,745 |
| Other | 2,080 |
| **TOTAL** | **11,050** |

**Impact:** 11,050 lines of duplicated code equals:
- 2x maintenance cost
- 2x bug surface area
- Inconsistent behavior between UIs
- Slower feature development

## 8. Dependencies

- FluentPDF.Core (consolidation target)
- FluentPDF.Rendering (consolidation target)
- FluentPDF.App (source of duplication)
- FluentPDF.Avalonia (source of duplication)
- CommunityToolkit.Mvvm (shared MVVM)

## 9. Out of Scope

- Merging WinUI and Avalonia into one UI (they remain separate)
- Changing user-facing features or UI design
- Performance optimization (beyond maintaining current performance)
- Adding new features (pure consolidation)
