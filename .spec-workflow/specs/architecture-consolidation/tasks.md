# Tasks: Architecture Consolidation

## Summary

Eliminate 11,050+ lines of duplicate code between FluentPDF.App (WinUI) and FluentPDF.Avalonia by consolidating shared logic in Core. Target: >90% reduction in duplication.

## Phase 1: Service Consolidation (Weeks 1-2)

- [x] 1.1 Create Core service infrastructure
  **Action Steps**:
  1. Check if src/FluentPDF.Core/Services/ exists:
     - If exists: List subdirectories with `ls src/FluentPDF.Core/Services/`
     - If not exists: Create with `mkdir src/FluentPDF.Core/Services/`
  2. Create subdirectory structure:
     - Create src/FluentPDF.Core/Services/Abstractions/ (for ISettingsService, IRecentFilesService interfaces)
     - Create src/FluentPDF.Core/Services/Implementation/ (for concrete SettingsService, RecentFilesService)
  3. Verify directory structure:
     - Run: `ls -R src/FluentPDF.Core/Services/`
     - Expected: Services/, Services/Abstractions/, Services/Implementation/
  4. Create placeholder .gitkeep files:
     - Create empty file: src/FluentPDF.Core/Services/Abstractions/.gitkeep
     - Create empty file: src/FluentPDF.Core/Services/Implementation/.gitkeep
  5. Build Core project: `dotnet build src/FluentPDF.Core`
  6. Verify: Build succeeds with no errors, directory structure exists
  7. Run spec-workflow command: First run spec-workflow-guide to record this implementation
  - File: src/FluentPDF.Core/Services/ (directory structure)
  - _Leverage: Existing Core project structure_
  - _Requirements: 3.2.1_

- [ ] 1.2 Move SettingsService to Core
  - Files: src/FluentPDF.Core/Services/SettingsService.cs, delete from App/Avalonia
  - Move SettingsService from UI projects to Core
  - Make platform-agnostic (cross-platform paths)
  - Use ILogger for logging
  - Test with both UIs
  - _Leverage: Existing SettingsService implementations in App and Avalonia_
  - _Requirements: 3.2.1, 3.2.2, NFR-4.1.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Service Consolidation Specialist | Task: Consolidate SettingsService from both UI projects into single implementation in Core. Make platform-agnostic using cross-platform paths and JSON storage. Inject ILogger. Test with both UIs. | Restrictions: Must work on Windows/Linux/macOS, preserve all functionality, use proper DI, no hardcoded paths, maintain settings compatibility | Success: Single SettingsService in Core, both UIs use it, settings load/save correctly, platform-agnostic, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.3 Move RecentFilesService to Core
  - Files: src/FluentPDF.Core/Services/RecentFilesService.cs, delete from App/Avalonia
  - Consolidate RecentFilesService into Core
  - Use cross-platform file paths
  - Implement max recent files limit
  - _Leverage: Migrated SettingsService pattern_
  - _Requirements: 3.2.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Service Migration Engineer | Task: Consolidate RecentFilesService following SettingsService pattern. Implement in Core with cross-platform paths, max limit configuration, proper persistence. | Restrictions: Must preserve recent files data, cross-platform paths, configurable limit, use DI | Success: RecentFilesService in Core, both UIs use it, recent files persist correctly. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.4 Enhance TelemetryService in Core
  - File: src/FluentPDF.Core/Services/TelemetryService.cs (extend existing)
  - TelemetryService already in Core, enhance with missing features
  - Add metrics collection for both UIs
  - Remove UI-specific code from UI projects
  - _Leverage: Existing TelemetryService in Core_
  - _Requirements: 3.2.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Telemetry Engineer | Task: Enhance existing TelemetryService in Core with comprehensive metrics collection supporting both UIs. Remove duplicated telemetry code from UI projects. | Restrictions: Must support both WinUI and Avalonia metrics, preserve existing telemetry, no UI-specific dependencies in Core | Success: TelemetryService handles all metrics, UI projects use shared service, telemetry consistent across UIs. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.5 Create platform abstraction interfaces
  - File: src/FluentPDF.Core/Services.Abstractions/I*.cs
  - Create IFileDialogService interface
  - Create INavigationService interface
  - Create IThemeService interface
  - Create IDiagnosticProvider interface
  - _Leverage: Existing interface patterns in Core/Services_
  - _Requirements: 3.2.1, 3.2.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Platform Abstraction Designer | Task: Design platform abstraction interfaces (IFileDialogService, INavigationService, IThemeService, IDiagnosticProvider) that work for both WinUI and Avalonia. Use platform-agnostic types. | Restrictions: No WinUI/Avalonia types in interfaces, must support both platforms, use cross-platform types, clear contracts | Success: Interfaces defined, platform-agnostic, support both UI frameworks, compile successfully. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.6 Implement WinUI platform adapters
  - Files: src/FluentPDF.App/Services/WinUI*Service.cs
  - Create WinUIFileDialogService implementing IFileDialogService
  - Create WinUINavigationService implementing INavigationService
  - Create WinUIThemeService implementing IThemeService
  - Create WinUIDiagnosticProvider implementing IDiagnosticProvider
  - Delete old service implementations
  - _Leverage: Platform abstraction interfaces_
  - _Requirements: 3.2.2, 3.2.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI Platform Developer | Task: Implement thin WinUI platform adapters for file dialogs, navigation, theme, and diagnostics following abstraction interfaces. Target ~40 LOC per adapter. | Restrictions: Must implement interfaces exactly, WinUI-specific code only, no business logic, thin adapters, proper async patterns | Success: All adapters implemented, register in DI, tests pass, existing features work. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.7 Implement Avalonia platform adapters
  - Files: src/FluentPDF.Avalonia/Services/Avalonia*Service.cs
  - Create AvaloniaFileDialogService implementing IFileDialogService
  - Create AvaloniaNavigationService implementing INavigationService
  - Create AvaloniaThemeService implementing IThemeService
  - Create AvaloniaDiagnosticProvider implementing IDiagnosticProvider
  - Delete old service implementations
  - _Leverage: WinUI adapter implementation patterns_
  - _Requirements: 3.2.2, 3.2.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Avalonia Platform Developer | Task: Implement thin Avalonia platform adapters following WinUI adapter patterns. Target ~40 LOC per adapter. | Restrictions: Must implement interfaces exactly, Avalonia-specific code only, no business logic, thin adapters, proper async patterns | Success: All adapters implemented, register in DI, tests pass, existing features work. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.8 Update DI configuration for services
  - Files: src/FluentPDF.App/Program.cs, src/FluentPDF.Avalonia/Program.cs
  - Register Core services in both UI projects
  - Register platform adapters
  - Remove registrations for deleted services
  - _Leverage: Existing DI configuration_
  - _Requirements: 3.2.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Dependency Injection Engineer | Task: Update DI configuration in both UI projects to register Core services and platform adapters. Remove old service registrations. | Restrictions: Must register all services, proper lifetimes (singleton/scoped/transient), both UIs configured identically for shared services | Success: DI configured correctly, services resolve, both UIs work, no runtime errors. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.9 Test service consolidation
  - Files: tests/FluentPDF.Core.Tests/Services/*Tests.cs
  - Write unit tests for consolidated services
  - Test with both mock and real dependencies
  - Verify platform adapters work correctly
  - _Leverage: Existing test patterns_
  - _Requirements: NFR-4.4.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Service Testing Engineer | Task: Create comprehensive unit tests for consolidated services in Core. Test SettingsService, RecentFilesService, TelemetryService. Mock platform dependencies. | Restrictions: Must test all service methods, use mocking for dependencies, test error cases, >80% coverage | Success: All services tested, >80% coverage, tests pass, mocking works correctly. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 1.10 Delete duplicate service code
  - Files: Delete src/FluentPDF.App/Services/SettingsService.cs, RecentFilesService.cs, etc.
  - Delete duplicate services from WinUI project
  - Delete duplicate services from Avalonia project
  - Verify both UIs still work
  - Run all tests
  - _Leverage: Consolidated services in Core_
  - _Requirements: AC-6.1.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Code Cleanup Specialist | Task: Delete all duplicate service implementations from UI projects after verifying Core services work correctly. | Restrictions: Must verify Core services work first, run all tests before deletion, both UIs must work after deletion, no breaking changes | Success: Duplicate services deleted, both UIs work, tests pass, no regressions. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Phase 2: ViewModel Consolidation (Weeks 3-4)

- [ ] 2.1 Create Core ViewModels namespace
  - File: src/FluentPDF.Core/ViewModels/ (directory structure)
  - Create ViewModels directory in Core
  - Set up namespace organization
  - Add CommunityToolkit.Mvvm reference if needed
  - _Leverage: Core project structure_
  - _Requirements: 3.3.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: .NET Project Organizer | Task: Create ViewModels directory in Core for base ViewModels. Ensure CommunityToolkit.Mvvm is referenced for MVVM support in both frameworks. | Restrictions: Must use CommunityToolkit.Mvvm (works with both WinUI and Avalonia), proper namespace, matches Core structure | Success: Directory created, namespace organized, Mvvm toolkit referenced, compiles. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.2 Create ViewModelBase class
  - File: src/FluentPDF.Core/ViewModels/ViewModelBase.cs
  - Create base ViewModel with ObservableObject
  - Add common error handling (ExecuteAsync pattern)
  - Add ILogger injection
  - Abstract OnError for platform-specific error display
  - _Leverage: CommunityToolkit.Mvvm.ComponentModel.ObservableObject_
  - _Requirements: 3.3.1, 3.3.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: MVVM Framework Developer | Task: Create ViewModelBase inheriting from ObservableObject with common patterns (ExecuteAsync for error handling, logging, abstract OnError). Framework-agnostic. | Restrictions: Must inherit ObservableObject, no UI framework dependencies, abstract error display, use ILogger, support both frameworks | Success: ViewModelBase created, framework-agnostic, error handling pattern, compiles. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.3 Create MainViewModelBase class
  - File: src/FluentPDF.Core/ViewModels/MainViewModelBase.cs
  - Extract common logic from both MainViewModel implementations
  - Add properties: IsDocumentOpen, CurrentFilePath, CurrentPageIndex, TotalPages
  - Add commands: OpenDocumentCommand, CloseDocumentCommand, SaveDocumentCommand
  - Inject services via constructor (IPdfDocumentService, ISettingsService, etc.)
  - _Leverage: Existing MainViewModel in App and Avalonia_
  - _Requirements: 3.3.1, 3.3.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Architect | Task: Create MainViewModelBase extracting all common logic from WinUI and Avalonia MainViewModels. Target ~600 LOC of shared logic. Use [ObservableProperty] for properties, IRelayCommand for commands. | Restrictions: Must be abstract, no UI framework types, use services via DI, all business logic here, platform-agnostic error handling | Success: MainViewModelBase created, contains shared logic, compiles, ready for UI inheritance. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.4 Update WinUI MainViewModel to inherit base
  - File: src/FluentPDF.App/ViewModels/MainViewModel.cs
  - Change to inherit from MainViewModelBase
  - Remove duplicate logic (now in base)
  - Add only WinUI-specific code (XamlRoot, ContentDialog error display)
  - Target ~50 LOC (thin wrapper)
  - _Leverage: MainViewModelBase_
  - _Requirements: 3.3.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: WinUI ViewModel Developer | Task: Refactor WinUI MainViewModel to inherit from MainViewModelBase. Remove all duplicate logic. Add only WinUI-specific code (XamlRoot, error dialogs). Target ~50 LOC. | Restrictions: Must inherit from base correctly, only WinUI-specific code, override OnError for ContentDialog, no business logic duplication | Success: WinUI MainViewModel inherits base, reduced to ~50 LOC, all features work, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.5 Update Avalonia MainViewModel to inherit base
  - File: src/FluentPDF.Avalonia/ViewModels/MainViewModel.cs
  - Change to inherit from MainViewModelBase
  - Remove duplicate logic (now in base)
  - Add only Avalonia-specific code (Interaction for error display)
  - Target ~50 LOC (thin wrapper)
  - _Leverage: MainViewModelBase, WinUI migration pattern_
  - _Requirements: 3.3.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Avalonia ViewModel Developer | Task: Refactor Avalonia MainViewModel to inherit from MainViewModelBase following WinUI pattern. Target ~50 LOC. | Restrictions: Must inherit from base correctly, only Avalonia-specific code, use Interaction for errors, no business logic duplication | Success: Avalonia MainViewModel inherits base, reduced to ~50 LOC, all features work, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.6 Create PdfViewerViewModelBase class
  - File: src/FluentPDF.Core/ViewModels/PdfViewerViewModelBase.cs
  - Extract common logic from PdfViewerViewModel implementations
  - Add zoom, rotation, page navigation logic
  - Add rendering commands
  - _Leverage: MainViewModelBase pattern_
  - _Requirements: 3.3.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Architect | Task: Create PdfViewerViewModelBase following MainViewModelBase pattern. Extract shared logic for zoom, rotation, navigation, rendering. Target ~650 LOC. | Restrictions: Must be abstract, no UI framework types, all business logic, use services via DI | Success: PdfViewerViewModelBase created, shared logic extracted, compiles. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.7 Update UI PdfViewerViewModels to inherit base
  - Files: src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs, src/FluentPDF.Avalonia/ViewModels/PdfViewerViewModel.cs
  - Both UI ViewModels inherit from PdfViewerViewModelBase
  - Remove duplicate logic
  - Add only UI-specific code
  - Target ~60 LOC each
  - _Leverage: PdfViewerViewModelBase, MainViewModel migration pattern_
  - _Requirements: 3.3.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Migration Specialist | Task: Refactor both UI PdfViewerViewModels to inherit from PdfViewerViewModelBase. Target ~60 LOC each. | Restrictions: Must inherit correctly, only UI-specific code, no duplicate logic | Success: Both ViewModels inherit base, reduced to ~60 LOC each, features work. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.8 Create remaining base ViewModels
  - Files: src/FluentPDF.Core/ViewModels/*ViewModelBase.cs
  - Create AnnotationViewModelBase (~400 LOC shared)
  - Create BookmarksViewModelBase (~300 LOC shared)
  - Create SettingsViewModelBase (~250 LOC shared)
  - Create other base ViewModels as needed
  - _Leverage: Established base ViewModel pattern_
  - _Requirements: 3.3.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Architect | Task: Create remaining base ViewModels (Annotation, Bookmarks, Settings) following established pattern. Extract shared logic from UI implementations. | Restrictions: Must follow base ViewModel pattern, abstract classes, no UI dependencies, all business logic | Success: All base ViewModels created, shared logic extracted, compile successfully. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.9 Update all UI ViewModels to inherit from base
  - Files: All ViewModels in src/FluentPDF.App/ViewModels/, src/FluentPDF.Avalonia/ViewModels/
  - Refactor all remaining UI ViewModels
  - Inherit from appropriate base classes
  - Remove duplicate logic
  - Target thin wrappers (~50-60 LOC each)
  - _Leverage: Completed ViewModel migrations_
  - _Requirements: 3.3.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Migration Lead | Task: Complete migration of all remaining ViewModels in both UI projects to inherit from Core base ViewModels. Target ~50-60 LOC per ViewModel. | Restrictions: All ViewModels must inherit base, remove duplicate logic, only UI-specific code, maintain all features | Success: All ViewModels migrated, thin wrappers, all features work, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 2.10 Create ViewModel unit tests
  - Files: tests/FluentPDF.Core.Tests/ViewModels/*Tests.cs
  - Write unit tests for base ViewModels
  - Test without UI frameworks (mock dependencies)
  - Verify commands, properties, business logic
  - _Leverage: Service testing patterns_
  - _Requirements: NFR-4.4.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: ViewModel Testing Engineer | Task: Create comprehensive unit tests for base ViewModels. Test all commands, properties, and business logic without UI frameworks. Mock all service dependencies. | Restrictions: No UI framework dependencies in tests, mock all services, test success and error paths, >80% coverage | Success: All base ViewModels tested, no UI required, >80% coverage, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Phase 3: API Consolidation (Week 5)

- [ ] 3.1 Create Core API namespace
  - File: src/FluentPDF.Core/Api/ (directory structure)
  - Create Api directory in Core
  - Create subdirectories: Endpoints, Services, Models
  - Organize namespace structure
  - _Leverage: Core project structure_
  - _Requirements: 3.4.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Project Organizer | Task: Create API namespace in Core for consolidating REST API from both UI projects. Organize into Endpoints, Services, Models. | Restrictions: Proper namespace organization, matches Core structure, no UI dependencies | Success: Directory structure created, namespaces organized, compiles. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.2 Move API endpoints to Core
  - Files: src/FluentPDF.Core/Api/Endpoints/*.cs (moved from UI projects)
  - Move DocumentEndpoints.cs from App/Avalonia to Core
  - Move RenderEndpoints.cs
  - Move VerifyEndpoints.cs
  - Move HealthEndpoints.cs
  - Move all other endpoints
  - _Leverage: Existing endpoint implementations_
  - _Requirements: 3.4.1, 3.4.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Migration Specialist | Task: Consolidate all API endpoints from both UI projects into single implementation in Core/Api/Endpoints. Remove UI dependencies. | Restrictions: Framework-agnostic (ASP.NET Core minimal APIs), no WinUI/Avalonia types, identical functionality, all endpoints migrated | Success: All endpoints in Core, framework-agnostic, compile successfully. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.3 Move API services to Core
  - Files: src/FluentPDF.Core/Api/Services/*.cs
  - Move DocumentSessionManager from App/Avalonia to Core
  - Move HashingService to Core
  - Abstract UiAutomationService (create IUiAutomationService)
  - _Leverage: Existing API service implementations_
  - _Requirements: 3.4.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Service Engineer | Task: Consolidate API services (DocumentSessionManager, HashingService) into Core. Abstract UiAutomationService for platform-specific implementations. | Restrictions: Framework-agnostic services, no UI dependencies, platform abstraction for UI automation | Success: API services in Core, abstraction for UI automation, compiles. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.4 Create VerificationApiExtensions
  - File: src/FluentPDF.Core/Api/VerificationApiExtensions.cs
  - Create extension methods for API registration
  - AddVerificationApi() for DI registration
  - UseVerificationApi() for endpoint mapping
  - _Leverage: ASP.NET Core extension patterns_
  - _Requirements: 3.4.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Configuration Specialist | Task: Create VerificationApiExtensions with AddVerificationApi() and UseVerificationApi() extension methods for easy API registration in both UI projects. | Restrictions: Follow ASP.NET Core extension patterns, register all services/endpoints, clean API for UI projects | Success: Extension methods created, easy API registration, both UIs can use it. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.5 Update UI projects to use shared API
  - Files: src/FluentPDF.App/Program.cs, src/FluentPDF.Avalonia/Program.cs
  - Call builder.Services.AddVerificationApi()
  - Call app.UseVerificationApi()
  - Remove old API registration code
  - _Leverage: VerificationApiExtensions_
  - _Requirements: 3.4.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Integration Engineer | Task: Update both UI projects to use shared API via VerificationApiExtensions. Remove old API code. | Restrictions: Use extension methods, remove duplicated API code, both UIs configured identically | Success: Both UIs use shared API, old code removed, API works in both projects. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.6 Test consolidated API
  - Files: tests/FluentPDF.Core.Tests/Api/*Tests.cs
  - Write integration tests for API endpoints
  - Use WebApplicationFactory<T> for testing
  - Test with TestServer (no real HTTP)
  - Verify all endpoints work
  - _Leverage: ASP.NET Core testing patterns_
  - _Requirements: NFR-4.4.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Testing Engineer | Task: Create integration tests for consolidated API using WebApplicationFactory and TestServer. Test all endpoints without real HTTP. | Restrictions: Use TestServer, test all endpoints, verify request/response, error handling, >80% coverage | Success: All API endpoints tested, integration tests pass, >80% coverage. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 3.7 Delete duplicate API code
  - Files: Delete src/FluentPDF.App/Api/, src/FluentPDF.Avalonia/Api/
  - Delete all duplicate API code from UI projects
  - Verify API still works in both UIs
  - Run all API tests
  - _Leverage: Consolidated API in Core_
  - _Requirements: AC-6.1.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Code Cleanup Specialist | Task: Delete all duplicate API code from UI projects after verifying shared API works correctly. | Restrictions: Verify API works first, run tests before deletion, both UIs must work, no breaking changes | Success: Duplicate API deleted, both UIs use shared API, tests pass, no regressions. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Phase 4: Diagnostics and Configuration Consolidation (Week 6)

- [ ] 4.1 Move diagnostics to Core
  - Files: src/FluentPDF.Core/Diagnostics/ (moved from App/Avalonia)
  - Move diagnostic commands from UI projects to Core
  - Create IDiagnosticProvider abstraction (already created in Phase 1)
  - Platform-specific providers in UI projects
  - _Leverage: Existing diagnostic code, IDiagnosticProvider interface_
  - _Requirements: 3.5.1, 3.5.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Diagnostics Migration Specialist | Task: Consolidate diagnostic commands into Core/Diagnostics. Use IDiagnosticProvider abstraction for platform-specific info. | Restrictions: Framework-agnostic commands, platform info via provider, both UIs supported | Success: Diagnostics in Core, platform providers in UIs, commands work in both. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 4.2 Consolidate configuration models
  - Files: src/FluentPDF.Core/Configuration/*.cs
  - Create unified AppSettings model
  - Create RenderingSettings model
  - Create UISettings model
  - Support appsettings.json loading
  - _Leverage: Existing settings models_
  - _Requirements: 3.6.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Configuration Architect | Task: Create unified configuration models in Core supporting both UIs. Use IOptions pattern for configuration. | Restrictions: Platform-agnostic models, support appsettings.json, IOptions pattern, both UIs use same models | Success: Configuration models in Core, both UIs use them, settings load correctly. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 4.3 Create Core DI extensions
  - File: src/FluentPDF.Core/DependencyInjection/CoreServiceExtensions.cs
  - Create AddFluentPdfCore() extension method
  - Register all Core services
  - Configure options
  - _Leverage: DI extension patterns_
  - _Requirements: 3.2.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DI Configuration Architect | Task: Create AddFluentPdfCore() extension method that registers all Core services, configuration, and options. Simplify UI project DI setup. | Restrictions: Register all Core services, proper lifetimes, configure options, clean API for UIs | Success: Extension method created, registers all services, UIs can call one method. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 4.4 Update UI DI configuration
  - Files: src/FluentPDF.App/Program.cs, src/FluentPDF.Avalonia/Program.cs
  - Simplify DI configuration using AddFluentPdfCore()
  - Register only platform-specific services
  - Remove duplicate registrations
  - _Leverage: CoreServiceExtensions_
  - _Requirements: 3.2.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DI Integration Engineer | Task: Update both UI projects to use AddFluentPdfCore(). Register only platform-specific services (file dialog, navigation, theme, diagnostics). | Restrictions: Use Core extension method, register platform services only, clean configuration, both UIs identical for shared services | Success: DI simplified, both UIs use Core extension, platform services registered, apps work. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Phase 5: Architecture Testing and Validation (Week 6)

- [ ] 5.1 Create architecture tests for SSOT
  - File: tests/FluentPDF.Architecture.Tests/ConsolidationTests.cs
  - Test: No duplicate services in UI projects
  - Test: All ViewModels inherit from Core base
  - Test: No API code in UI projects
  - Test: Only platform adapters allowed in UI Services/
  - _Leverage: NetArchTest.Rules_
  - _Requirements: AC-6.1.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Architecture Test Engineer | Task: Create comprehensive architecture tests enforcing SSOT using NetArchTest.Rules. Verify no duplicate services, ViewModels inherit base, no API duplication, only platform adapters in UIs. | Restrictions: Must use NetArchTest, fail build on violations, comprehensive rules, verify all constraints | Success: Architecture tests created, enforce SSOT, fail on violations, pass currently. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 5.2 Measure code reduction
  - File: docs/consolidation-metrics.md
  - Count lines before/after consolidation
  - Calculate duplication reduction percentage
  - Document savings per component
  - Create metrics report
  - _Leverage: Code analysis tools_
  - _Requirements: AC-6.1.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Code Metrics Analyst | Task: Measure code reduction from consolidation. Count LOC before/after for Services, ViewModels, API, etc. Calculate savings. Document in metrics report. | Restrictions: Accurate line counts, categorize by component, calculate percentages, document methodology | Success: Metrics report created, >90% reduction verified, documented per component. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 5.3 End-to-end testing
  - Files: tests/FluentPDF.E2E.Tests/*Tests.cs
  - Test both UIs with consolidated code
  - Verify identical behavior
  - Test all major workflows
  - Settings sync verification
  - _Leverage: Existing E2E test patterns_
  - _Requirements: AC-6.2.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: E2E Test Engineer | Task: Create end-to-end tests verifying both UIs work correctly with consolidated code. Test major workflows (open, edit, save), settings sync, identical behavior. | Restrictions: Test both WinUI and Avalonia, verify identical behavior, all workflows tested, settings sync | Success: E2E tests pass, both UIs work, behavior identical, settings sync verified. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 5.4 Performance verification
  - Files: tests/FluentPDF.Benchmarks/ConsolidationBenchmarks.cs
  - Benchmark before/after consolidation
  - Verify no performance regression
  - Measure memory usage
  - Document results
  - _Leverage: BenchmarkDotNet_
  - _Requirements: NFR-4.3.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Performance Engineer | Task: Create benchmarks comparing performance before/after consolidation. Verify no regression in execution time or memory usage. | Restrictions: Use BenchmarkDotNet, measure key operations, compare before/after, document results | Success: Benchmarks run, no performance regression, memory usage similar or better. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Phase 6: Documentation and Cleanup (Week 6)

- [ ] 6.1 Write consolidation architecture doc
  - File: docs/architecture/consolidation-architecture.md
  - Document new architecture (layering diagram)
  - Explain platform abstraction pattern
  - Document ViewModel inheritance hierarchy
  - Service consolidation strategy
  - _Leverage: Completed consolidation work_
  - _Requirements: AC-6.4.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Technical Documentation Writer | Task: Create comprehensive architecture documentation explaining consolidation strategy, layering, platform abstractions, ViewModel inheritance, service organization. Include diagrams. | Restrictions: Clear diagrams, explain rationale, document patterns, actionable guidance | Success: Documentation complete, diagrams clear, patterns explained, architecture understood. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 6.2 Write migration guide
  - File: docs/consolidation-migration-guide.md
  - Document consolidation process
  - Lessons learned
  - Common pitfalls and solutions
  - Guidelines for future development
  - _Leverage: Migration experience_
  - _Requirements: AC-6.4.2_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Migration Guide Writer | Task: Write migration guide documenting consolidation process, lessons learned, pitfalls, best practices for future development to maintain SSOT. | Restrictions: Actionable steps, lessons learned, pitfalls documented, preventive guidance | Success: Migration guide complete, comprehensive, helps future developers. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 6.3 Update API documentation
  - File: docs/api-documentation.md
  - Document consolidated REST API
  - Update endpoint documentation
  - Platform abstraction API docs
  - Service documentation
  - _Leverage: Consolidated APIs_
  - _Requirements: AC-6.4.3_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: API Documentation Specialist | Task: Update API documentation reflecting consolidated API in Core. Document all endpoints, platform abstractions, service interfaces. | Restrictions: Complete API coverage, examples included, platform abstractions explained | Success: API documentation complete, all endpoints documented, examples clear. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 6.4 Final code cleanup
  - Files: Various files throughout codebase
  - Remove all commented-out duplicate code
  - Clean up namespaces
  - Remove unused using statements
  - Format code consistently
  - _Leverage: Code cleanup tools_
  - _Requirements: Clean codebase_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: Code Cleanup Specialist | Task: Perform final cleanup of codebase after consolidation. Remove commented code, clean namespaces, remove unused usings, format consistently. | Restrictions: No functional changes, only cleanup, run tests after cleanup, maintain working state | Success: Codebase clean, no commented code, consistent formatting, tests pass. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

- [ ] 6.5 Update README and getting started
  - Files: README.md, docs/getting-started.md
  - Update README with new architecture
  - Update getting started guide
  - Document Core/UI separation
  - Explain platform abstraction usage
  - _Leverage: Completed consolidation_
  - _Requirements: AC-6.4.1_
  - _Prompt: Implement the task for spec architecture-consolidation, first run spec-workflow-guide to get the workflow guide then implement the task: Role: User Documentation Writer | Task: Update README and getting started documentation reflecting new consolidated architecture. Explain Core/UI separation, platform abstractions, updated structure. | Restrictions: User-friendly, explain benefits, clear setup instructions, architecture benefits highlighted | Success: Documentation updated, architecture explained, setup clear, benefits communicated. After completing this task, mark it as [-] in tasks.md, use log-implementation tool to record the implementation with detailed artifacts, then mark as [x] when logged.

## Success Criteria

✅ **Code Reduction**:
- Duplicate code reduced from 11,050 to <600 LOC (>90% reduction)
- Services: 2,325 → ~200 LOC (platform adapters)
- ViewModels: 4,745 → ~400 LOC (thin wrappers)
- API: 1,900 → 0 (fully consolidated)

✅ **Architecture**:
- All business logic in Core
- Zero duplicate service implementations
- All ViewModels inherit from Core base classes
- Platform abstractions for UI-specific features

✅ **Testing**:
- >80% test coverage for Core code
- Architecture tests enforce SSOT
- Both UIs tested and working

✅ **Documentation**:
- Architecture documentation complete
- Migration guide written
- API documentation updated
- README reflects new structure

✅ **Functionality**:
- Both UIs work identically
- All features preserved
- Settings sync between apps
- No performance regression
