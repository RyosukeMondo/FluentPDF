# Tasks Document

## Implementation Tasks

- [x] 1. Create solution structure and core projects
  - Files:
    - `FluentPDF.sln`
    - `Directory.Build.props`
    - `src/FluentPDF.App/FluentPDF.App.csproj`
    - `src/FluentPDF.Core/FluentPDF.Core.csproj`
    - `src/FluentPDF.Rendering/FluentPDF.Rendering.csproj`
  - Create Visual Studio solution with WinUI 3 app project, Core class library, and Rendering class library
  - Configure Directory.Build.props with shared MSBuild properties (LangVersion, Nullable, TreatWarningsAsErrors)
  - Set up project references (App → Core, App → Rendering, Rendering → Core)
  - Purpose: Establish foundational project structure following steering document organization
  - _Leverage: None (greenfield)_
  - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** .NET Solution Architect specializing in WinUI 3 and multi-project solutions

    **Task:**
    Create a Visual Studio solution named `FluentPDF.sln` with three projects following the exact structure defined in `.spec-workflow/steering/structure.md`:

    1. **FluentPDF.App** (WinUI 3 Application):
       - Location: `src/FluentPDF.App/`
       - Target Framework: `net8.0-windows10.0.19041.0`
       - Project type: WinUI 3 Desktop App
       - Package as MSIX
       - Add project references to FluentPDF.Core and FluentPDF.Rendering

    2. **FluentPDF.Core** (Class Library):
       - Location: `src/FluentPDF.Core/`
       - Target Framework: `net8.0`
       - Must have ZERO UI dependencies (headless testable)
       - Install NuGet: FluentResults, Serilog.Extensions.Logging

    3. **FluentPDF.Rendering** (Class Library):
       - Location: `src/FluentPDF.Rendering/`
       - Target Framework: `net8.0`
       - Add project reference to FluentPDF.Core
       - Will contain P/Invoke code for PDFium (add later)

    4. **Directory.Build.props** (root):
       - Set LangVersion to "latest"
       - Enable Nullable reference types: `<Nullable>enable</Nullable>`
       - TreatWarningsAsErrors: true
       - Add CLAUDE.md code quality metrics as comments (500 line file limit, 50 line method limit)

    **Restrictions:**
    - Do NOT add any business logic yet - this is purely structure setup
    - Do NOT install additional NuGet packages beyond those specified
    - Do NOT modify default WinUI 3 template files yet (App.xaml, MainWindow.xaml)
    - Ensure Core project has ZERO references to WinUI/Windows namespaces
    - Follow exact naming conventions from steering document (FluentPDF.* namespace root)

    **Success Criteria:**
    - Solution opens in Visual Studio 2022 without errors
    - All three projects compile successfully
    - Directory.Build.props applies settings to all projects
    - Project references are correct (App → Core/Rendering, Rendering → Core, no circular dependencies)
    - `dotnet build FluentPDF.sln` succeeds
    - Core project has no UI dependencies (verified by no `using Microsoft.UI.*` statements)

    **Instructions:**
    1. Before implementing, read `.spec-workflow/specs/project-foundation/requirements.md` and `.spec-workflow/specs/project-foundation/design.md` to understand the full context
    2. Edit `.spec-workflow/specs/project-foundation/tasks.md` and change this task's status from `[ ]` to `[-]` (in-progress)
    3. Implement the solution structure as specified
    4. Test by running `dotnet build FluentPDF.sln` and verifying all projects compile
    5. Use the log-implementation tool to record implementation details with artifacts:
       - Include projects created in artifacts.components (name, type, purpose, location)
       - Include filesCreated and filesModified
       - Include statistics (lines added, files changed)
    6. After successful logging, edit tasks.md and change this task's status from `[-]` to `[x]` (completed)

- [x] 2. Set up test projects with xUnit and ArchUnitNET
  - Files:
    - `tests/FluentPDF.Architecture.Tests/FluentPDF.Architecture.Tests.csproj`
    - `tests/FluentPDF.Core.Tests/FluentPDF.Core.Tests.csproj`
    - `tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj`
    - `tests/FluentPDF.Architecture.Tests/ArchitectureTestBase.cs`
  - Create three test projects: Architecture, Core unit tests, and App UI tests
  - Install NuGet packages: xUnit, ArchUnitNET.xUnit, FluentAssertions, Moq
  - Add project references to projects under test
  - Purpose: Establish testing infrastructure for unit, architecture, and UI tests
  - _Leverage: Task 1 (solution structure)_
  - _Requirements: 6.1, 6.2, 7.1, 7.2_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Test Infrastructure Engineer specializing in .NET testing frameworks and CI/CD

    **Task:**
    Create three test projects in the `tests/` directory with proper dependencies and test infrastructure:

    1. **FluentPDF.Architecture.Tests** (xUnit Test Project):
       - Location: `tests/FluentPDF.Architecture.Tests/`
       - Target Framework: net8.0
       - NuGet packages:
         - xUnit (latest)
         - xunit.runner.visualstudio (latest)
         - ArchUnitNET.xUnit (latest)
       - Add project references to ALL src projects (App, Core, Rendering)
       - Create `ArchitectureTestBase.cs` with Architecture object initialization

    2. **FluentPDF.Core.Tests** (xUnit Test Project):
       - Location: `tests/FluentPDF.Core.Tests/`
       - Target Framework: net8.0
       - NuGet packages:
         - xUnit
         - xunit.runner.visualstudio
         - FluentAssertions (latest)
         - Moq (latest)
         - AutoFixture (latest) - for test data generation
       - Add project reference to FluentPDF.Core only
       - Create `TestBase.cs` with common test utilities

    3. **FluentPDF.App.Tests** (xUnit Test Project):
       - Location: `tests/FluentPDF.App.Tests/`
       - Target Framework: net8.0-windows10.0.19041.0
       - NuGet packages:
         - xUnit
         - xunit.runner.visualstudio
         - FlaUI.Core (latest)
         - FlaUI.UIA3 (latest)
         - Verify.Xunit (latest)
       - Add project reference to FluentPDF.App
       - Create `UITestBase.cs` with FlaUI initialization helpers

    **Restrictions:**
    - Do NOT write actual tests yet - only project setup and base classes
    - Do NOT add [Fact] or [Theory] tests in this task
    - Ensure Core.Tests has NO UI dependencies (can run headless)
    - Follow xUnit naming conventions (*.Tests suffix)
    - Add all projects to solution file

    **Success Criteria:**
    - All three test projects compile successfully
    - `dotnet test FluentPDF.sln` runs without errors (even if no tests exist yet)
    - ArchitectureTestBase.cs initializes Architecture object from ArchUnitNET
    - TestBase classes provide useful helpers (mock creation, fixtures)
    - Visual Studio Test Explorer discovers test projects
    - Core.Tests can run without WinUI runtime (headless testable)

    **Instructions:**
    1. Before implementing, read the design document section on "Testing Infrastructure Setup"
    2. Edit tasks.md: change task status to `[-]` (in-progress)
    3. Create test projects with specified NuGet packages
    4. Write base classes with helpful utilities for future tests
    5. Verify by running `dotnet test` (should succeed with 0 tests)
    6. Log implementation with artifacts:
       - Include test projects in artifacts.components
       - Document test infrastructure setup
    7. Edit tasks.md: change task status to `[x]` (completed)

- [x] 3. Implement Result pattern error handling (PdfError, ErrorCategory, ErrorSeverity)
  - Files:
    - `src/FluentPDF.Core/ErrorHandling/PdfError.cs`
    - `src/FluentPDF.Core/ErrorHandling/ErrorCategory.cs`
    - `src/FluentPDF.Core/ErrorHandling/ErrorSeverity.cs`
    - `tests/FluentPDF.Core.Tests/ErrorHandling/PdfErrorTests.cs`
  - Create structured error types extending FluentResults.Error
  - Implement ErrorCategory and ErrorSeverity enums
  - Add Context dictionary for metadata
  - Write unit tests for error creation and serialization
  - Purpose: Enable type-safe domain error handling with AI-analyzable metadata
  - _Leverage: FluentResults NuGet package_
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** C# Developer specializing in functional error handling and domain-driven design

    **Task:**
    Implement the Result pattern infrastructure in FluentPDF.Core following the exact design from `.spec-workflow/steering/structure.md` Error Handling section:

    1. **PdfError.cs**:
       - Inherit from `FluentResults.Error`
       - Add properties: ErrorCode (string), Category (ErrorCategory enum), Severity (ErrorSeverity enum), Context (Dictionary<string, object>)
       - Constructor should initialize Metadata dictionary for AI analysis
       - Add metadata entries for ErrorCode, Category, Severity
       - Implement WithContext() fluent method to add context data

    2. **ErrorCategory.cs**:
       - Create enum with values: Validation, System, Security, IO, Rendering, Conversion
       - Add XML doc comments explaining each category

    3. **ErrorSeverity.cs**:
       - Create enum with values: Critical, Error, Warning, Info
       - Add XML doc comments explaining each severity level

    4. **PdfErrorTests.cs**:
       - Test error creation with all properties
       - Test Metadata is properly populated
       - Test Context dictionary can be added
       - Test error chaining with InnerError (CausedBy)
       - Use FluentAssertions for readable test assertions

    **Restrictions:**
    - Do NOT add logging in this task (that's a later task)
    - Do NOT create concrete service implementations yet
    - Keep files under 500 lines (CLAUDE.md guideline)
    - Follow structure.md code organization patterns (using directives, namespace, XML docs, class)
    - Use init-only properties for immutability where appropriate

    **Success Criteria:**
    - PdfError can be created with ErrorCode, Category, Severity, and Context
    - Error.Metadata dictionary contains ErrorCode, Category, Severity for AI analysis
    - Context dictionary allows adding arbitrary metadata (file path, page number, etc.)
    - FluentResults Result<T> can wrap PdfError: `Result.Fail(new PdfError(...))`
    - All tests pass: `dotnet test tests/FluentPDF.Core.Tests --filter PdfErrorTests`
    - Code coverage > 80% for error handling classes
    - XML doc comments are present on all public members

    **Instructions:**
    1. Read design.md "Component 3: Result Pattern Error Handling" section carefully
    2. Edit tasks.md: change to `[-]` status
    3. Implement error handling classes in FluentPDF.Core/ErrorHandling/
    4. Write comprehensive unit tests
    5. Run tests and verify all pass
    6. Log implementation with artifacts:
       - Include PdfError class in artifacts.classes (name, purpose, methods)
       - Include ErrorCategory and ErrorSeverity enums
       - Document that this enables AI-analyzable error metadata
    7. Edit tasks.md: change to `[x]` status

- [x] 4. Configure dependency injection with IHost and service registration
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify)
    - `src/FluentPDF.App/Services/INavigationService.cs`
    - `src/FluentPDF.App/Services/NavigationService.cs`
    - `src/FluentPDF.Core/Services/ITelemetryService.cs`
    - `src/FluentPDF.Core/Services/TelemetryService.cs`
  - Set up IHost in App.xaml.cs with Microsoft.Extensions.DependencyInjection
  - Create INavigationService interface and implementation for testable navigation
  - Register ViewModels as Transient services
  - Register infrastructure services as Singletons
  - Purpose: Enable constructor injection and testable architecture
  - _Leverage: Microsoft.Extensions.Hosting, Task 1 (solution structure)_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** .NET Infrastructure Engineer specializing in dependency injection and service architecture

    **Task:**
    Configure the dependency injection container in FluentPDF.App using IHost pattern:

    1. **App.xaml.cs modifications**:
       - Add private field: `IHost _host;`
       - In constructor, create Host with `Host.CreateDefaultBuilder().ConfigureServices(...).Build()`
       - Register services:
         - ViewModels: `services.AddTransient<MainViewModel>()` (when created later)
         - Services: `services.AddSingleton<INavigationService, NavigationService>()`
         - Services: `services.AddSingleton<ITelemetryService, TelemetryService>()`
       - Add public method: `T GetService<T>() => _host.Services.GetRequiredService<T>();`
       - Ensure host is started: `await _host.StartAsync()` in OnLaunched

    2. **INavigationService.cs** (in App/Services/):
       - Define interface with methods:
         - `void NavigateTo(Type pageType, object? parameter = null)`
         - `bool CanGoBack { get; }`
         - `void GoBack()`
       - Add XML doc comments

    3. **NavigationService.cs**:
       - Implement INavigationService
       - Store Frame reference (injected or set via property)
       - Implement navigation using Frame.Navigate()
       - Handle GoBack with Frame.GoBack()

    4. **ITelemetryService.cs** (in Core/Services/):
       - Define interface with methods:
         - `void TrackEvent(string eventName, Dictionary<string, object>? properties = null)`
         - `void TrackException(Exception exception, Dictionary<string, object>? properties = null)`
       - Add XML doc comments

    5. **TelemetryService.cs** (in Core/Services/):
       - Implement ITelemetryService
       - For now, just log to Debug.WriteLine (actual telemetry added later)
       - Store ILogger<TelemetryService> via constructor injection

    **Restrictions:**
    - Do NOT implement actual ViewModels yet (just prepare registration)
    - Do NOT add logging infrastructure yet (next task)
    - Keep NavigationService simple - no complex routing yet
    - Ensure all services use interface contracts (I* pattern)
    - Follow structure.md DI patterns

    **Success Criteria:**
    - App.xaml.cs builds without errors with IHost configured
    - Services can be resolved: `var nav = ((App)Application.Current).GetService<INavigationService>();`
    - INavigationService abstraction allows testing without Frame
    - Services follow interface-based pattern for mockability
    - No circular dependencies exist
    - `dotnet build src/FluentPDF.App` succeeds

    **Instructions:**
    1. Read design.md "Component 4: Dependency Injection Container" carefully
    2. Edit tasks.md: change to `[-]`
    3. Install NuGet: Microsoft.Extensions.Hosting in FluentPDF.App
    4. Modify App.xaml.cs to add IHost
    5. Create service interfaces and implementations
    6. Test by building the solution
    7. Log implementation with artifacts:
       - Include INavigationService and ITelemetryService in artifacts.classes
       - Document DI container setup in App.xaml.cs
       - Note that this enables testable architecture
    8. Edit tasks.md: change to `[x]`

- [ ] 5. Implement Serilog + OpenTelemetry logging infrastructure
  - Files:
    - `src/FluentPDF.Core/Logging/SerilogConfiguration.cs`
    - `src/FluentPDF.App/App.xaml.cs` (modify - add logging setup)
    - `tests/FluentPDF.Core.Tests/Logging/SerilogConfigurationTests.cs`
  - Create Serilog configuration with JSON formatter and enrichers
  - Configure file sink to ApplicationData.LocalFolder for MSIX compatibility
  - Set up OpenTelemetry sink for .NET Aspire Dashboard
  - Add LogContext for correlation IDs
  - Write tests for logging configuration
  - Purpose: Enable structured logging with correlation IDs for AI analysis and debugging
  - _Leverage: Serilog, Serilog.Sinks.OpenTelemetry, Task 4 (DI setup)_
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Observability Engineer specializing in structured logging and OpenTelemetry

    **Task:**
    Implement comprehensive logging infrastructure using Serilog with OpenTelemetry integration:

    1. **Install NuGet packages** in FluentPDF.Core:
       - Serilog (latest)
       - Serilog.Sinks.File (latest)
       - Serilog.Sinks.Async (latest)
       - Serilog.Sinks.OpenTelemetry (latest)
       - Serilog.Formatting.Compact (latest)
       - Serilog.Enrichers.Environment (latest)
       - Serilog.Enrichers.Thread (latest)

    2. **SerilogConfiguration.cs** (in Core/Logging/):
       - Create static class with `CreateLogger()` method returning ILogger
       - Configure LoggerConfiguration with:
         - MinimumLevel.Debug()
         - Enrich.FromLogContext() for correlation IDs
         - Enrich.WithProperty("Application", "FluentPDF")
         - Enrich.WithProperty("Version", from assembly version)
         - Enrich.WithMachineName()
         - Enrich.WithEnvironmentName()
       - Add file sink:
         - Use JsonFormatter for structured logs
         - Path: `ApplicationData.Current.LocalFolder.Path + "/logs/log-.json"`
         - RollingInterval.Day
         - RetainedFileCountLimit: 7
         - Wrap in WriteTo.Async() for performance
       - Add OpenTelemetry sink:
         - Endpoint: "http://localhost:4317"
         - Protocol: OtlpProtocol.Grpc
         - ResourceAttributes: service.name = "FluentPDF.Desktop", service.version
       - Handle ApplicationData.Current being null (for tests): fallback to temp path

    3. **Modify App.xaml.cs**:
       - In constructor, before IHost creation: `Log.Logger = SerilogConfiguration.CreateLogger();`
       - In ConfigureServices: `services.AddLogging(builder => builder.AddSerilog(dispose: true));`
       - In OnLaunched error handler: use `Log.Fatal()` for unhandled exceptions
       - Ensure proper disposal: in App.OnExit, call `Log.CloseAndFlush();`

    4. **SerilogConfigurationTests.cs**:
       - Test CreateLogger() returns valid ILogger
       - Test log file is created in expected location
       - Test enrichers add expected properties
       - Mock ApplicationData for headless testing

    **Restrictions:**
    - Do NOT add global exception handlers yet (next task)
    - Do NOT implement actual logging calls in services yet
    - Ensure logs work in MSIX sandbox (ApplicationData.LocalFolder only)
    - Use async file sink to prevent I/O blocking
    - Keep CreateLogger() method under 50 lines

    **Success Criteria:**
    - `Log.Information("Test message")` writes to both file and OTLP endpoint
    - Log files appear in ApplicationData.LocalFolder/logs/
    - JSON log format is valid and includes all enrichers
    - Correlation IDs can be added: `LogContext.PushProperty("CorrelationId", guid)`
    - Tests verify logging configuration without crashing
    - .NET Aspire Dashboard shows logs at http://localhost:4317 (when running)

    **Instructions:**
    1. Read design.md "Component 5: Structured Logging Infrastructure" section
    2. Edit tasks.md: change to `[-]`
    3. Install required NuGet packages
    4. Implement SerilogConfiguration.cs
    5. Integrate into App.xaml.cs
    6. Write configuration tests
    7. Test manually: add `Log.Information("App started")` in App constructor, run app, verify logs created
    8. Log implementation with artifacts:
       - Include SerilogConfiguration class in artifacts.classes
       - Document logging endpoints (file + OTLP)
       - Note MSIX sandbox compatibility
    9. Edit tasks.md: change to `[x]`

- [ ] 6. Add global exception handlers to App.xaml.cs
  - Files:
    - `src/FluentPDF.App/App.xaml.cs` (modify - add exception handlers)
    - `src/FluentPDF.App/Views/ErrorDialog.xaml`
    - `src/FluentPDF.App/Views/ErrorDialog.xaml.cs`
  - Implement Application.UnhandledException handler for UI thread
  - Implement TaskScheduler.UnobservedTaskException for background tasks
  - Implement AppDomain.UnhandledException for non-UI threads
  - Create user-friendly error dialog showing correlation ID
  - Log all exceptions with structured metadata
  - Purpose: Prevent crashes and ensure all errors are logged for AI analysis
  - _Leverage: Task 5 (Serilog logging), Task 3 (PdfError)_
  - _Requirements: 3.4, 5.6_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Reliability Engineer specializing in error resilience and crash prevention

    **Task:**
    Implement multi-layered global exception handling to prevent crashes and ensure observability:

    1. **Modify App.xaml.cs constructor**:
       - After InitializeComponent(), add three exception handlers:

         a) UI Thread: `UnhandledException += OnUnhandledException;`
            - In handler: generate correlation ID (Guid.NewGuid())
            - Log with `Log.Fatal(e.Exception, "Unhandled UI exception [CorrelationId: {CorrelationId}]", correlationId)`
            - Show ErrorDialog with correlation ID (async)
            - Set `e.Handled = true` to prevent crash if possible

         b) Background Tasks: `TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;`
            - In handler: generate correlation ID
            - Log with `Log.Fatal(e.Exception, "Unobserved task exception [CorrelationId: {CorrelationId}]", correlationId)`
            - Set `e.SetObserved()` to prevent crash

         c) Non-UI Threads: `AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;`
            - In handler: generate correlation ID
            - Log with `Log.Fatal(e.ExceptionObject as Exception, "Domain unhandled exception [CorrelationId: {CorrelationId}]", correlationId)`
            - Note: Cannot prevent crash in this handler, but log is saved

    2. **ErrorDialog.xaml**:
       - Create WinUI 3 ContentDialog
       - Title: "An error occurred"
       - Content: User-friendly message + correlation ID in monospace font
       - PrimaryButton: "Close" or "Restart App"
       - Visual design: Follow Fluent Design with error icon

    3. **ErrorDialog.xaml.cs**:
       - Constructor accepts: string message, string correlationId
       - Display message and correlation ID
       - Log button clicks

    **Restrictions:**
    - Do NOT crash the app unnecessarily - mark handlers as handled where possible
    - Do NOT show technical stack traces to users - only friendly messages
    - Ensure correlation ID is ALWAYS logged for support purposes
    - Keep exception handler methods under 50 lines each
    - Test that logs are written before app terminates

    **Success Criteria:**
    - Throwing an exception in UI thread shows ErrorDialog instead of crashing
    - Unobserved Task exceptions are logged and don't crash app
    - AppDomain exceptions are logged before crash
    - Correlation ID appears in both log and error dialog
    - Logs contain full stack trace and context for debugging
    - App remains stable after handled exceptions

    **Instructions:**
    1. Read design.md "Error Handling" section and structure.md "Global Exception Handling" example
    2. Edit tasks.md: change to `[-]`
    3. Modify App.xaml.cs to add three exception handlers
    4. Create ErrorDialog XAML and code-behind
    5. Test by intentionally throwing exceptions in different contexts
    6. Verify logs are written with correlation IDs
    7. Log implementation with artifacts:
       - Document all three exception handlers in implementation log
       - Include ErrorDialog component
       - Note that this provides three-layer crash protection
    8. Edit tasks.md: change to `[x]`

- [ ] 7. Create ArchUnitNET layer and naming tests
  - Files:
    - `tests/FluentPDF.Architecture.Tests/LayerTests.cs`
    - `tests/FluentPDF.Architecture.Tests/NamingTests.cs`
    - `tests/FluentPDF.Architecture.Tests/InterfaceTests.cs`
  - Implement layering rules: Core must not depend on App or Rendering
  - Implement naming rules: ViewModels must end with "ViewModel" and inherit ObservableObject
  - Implement interface rules: Services must implement I* interface pattern
  - Write tests that fail when architectural rules are violated
  - Purpose: Automated enforcement of architectural integrity
  - _Leverage: ArchUnitNET.xUnit, Task 2 (test projects)_
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Software Architect specializing in architectural governance and automated testing

    **Task:**
    Implement comprehensive ArchUnitNET tests to enforce architectural rules and prevent erosion:

    1. **LayerTests.cs**:
       - Create Architecture object loading all assemblies:
         ```csharp
         private static readonly Architecture Architecture =
             new ArchLoader().LoadAssemblies(
                 typeof(FluentPDF.Core.ErrorHandling.PdfError).Assembly,
                 typeof(FluentPDF.App.App).Assembly,
                 typeof(FluentPDF.Rendering.PdfiumRenderer).Assembly // add when exists
             ).Build();
         ```
       - Test: CoreLayer_ShouldNot_DependOn_AppLayer
         - Verify Core namespace doesn't depend on App namespace
         - Reason: "Core must be UI-agnostic for testability"
       - Test: CoreLayer_ShouldNot_DependOn_RenderingLayer
         - Verify Core doesn't depend on Rendering
         - Reason: "Core is business logic, Rendering is infrastructure"
       - Test: AppLayer_Can_DependOn_CoreAndRendering
         - Verify App CAN depend on both (positive test)

    2. **NamingTests.cs**:
       - Test: ViewModels_Should_EndWith_ViewModel
         - Use: Classes().That().HaveNameEndingWith("ViewModel")
         - Should: Have "ViewModel" suffix
         - Reason: "Consistent naming for ViewModels"
       - Test: ViewModels_Should_InheritFrom_ObservableObject
         - Verify ViewModels extend CommunityToolkit.Mvvm.ComponentModel.ObservableObject
         - Reason: "ViewModels must use CommunityToolkit.Mvvm"
       - Test: Services_Should_EndWith_Service
         - Classes ending with "Service" should have consistent naming
       - Test: Interfaces_Should_StartWith_I
         - All interfaces should have "I" prefix

    3. **InterfaceTests.cs**:
       - Test: Services_Should_ImplementInterfaces
         - Classes ending with "Service" should implement corresponding I*Service interface
         - Use regex: `Should().ImplementInterface("I.*Service")`
         - Reason: "Services must be abstracted for DI and testing"
       - Test: Interfaces_Should_BeInSameNamespace_AsImplementations
         - INavigationService and NavigationService in same namespace
       - Test: CoreInterfaces_Should_NotReference_UITypes
         - Interfaces in Core should not use WinUI types in signatures

    **Restrictions:**
    - Do NOT skip tests because implementations don't exist yet - tests should PASS when they do
    - Use descriptive .Because() clauses for all rules
    - Keep test classes under 500 lines
    - Add [Fact] attribute to each test method
    - Follow structure.md ArchUnitNET patterns exactly

    **Success Criteria:**
    - All architecture tests pass: `dotnet test tests/FluentPDF.Architecture.Tests`
    - Tests fail if layer boundaries are violated (verify by intentionally breaking a rule)
    - Test output clearly explains which rule was violated
    - Tests run in < 5 seconds (fast feedback)
    - CI pipeline can run these tests (no UI dependencies)

    **Instructions:**
    1. Read design.md "Component 6: ArchUnitNET Architecture Tests" and structure.md "Architecture Testing" section
    2. Edit tasks.md: change to `[-]`
    3. Implement LayerTests.cs with dependency rules
    4. Implement NamingTests.cs with naming conventions
    5. Implement InterfaceTests.cs with interface patterns
    6. Run tests and verify all pass
    7. Intentionally violate a rule (e.g., add Core→App reference) and verify test fails
    8. Log implementation with artifacts:
       - Document all architecture rules in implementation log
       - Note that this prevents architectural erosion automatically
       - Include test classes in artifacts
    9. Edit tasks.md: change to `[x]`

- [ ] 8. Set up CommunityToolkit.Mvvm with sample ViewModel
  - Files:
    - `src/FluentPDF.App/ViewModels/MainViewModel.cs`
    - `src/FluentPDF.App/MainWindow.xaml` (modify - add ViewModel binding)
    - `tests/FluentPDF.Core.Tests/ViewModels/MainViewModelTests.cs`
  - Install CommunityToolkit.Mvvm NuGet package
  - Create MainViewModel with [ObservableProperty] and [RelayCommand] examples
  - Configure ViewModel in DI container
  - Bind MainWindow to MainViewModel
  - Write headless unit tests for ViewModel
  - Purpose: Demonstrate modern MVVM pattern with source generators
  - _Leverage: CommunityToolkit.Mvvm, Task 4 (DI setup)_
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** WinUI 3 Frontend Developer specializing in MVVM and data binding

    **Task:**
    Implement a sample ViewModel using CommunityToolkit.Mvvm to demonstrate the pattern:

    1. **Install NuGet** in FluentPDF.App:
       - CommunityToolkit.Mvvm (latest version, 8.x)

    2. **MainViewModel.cs** (in App/ViewModels/):
       - Mark class as `partial` (required for source generators)
       - Inherit from `ObservableObject`
       - Add observable properties using attributes:
         ```csharp
         [ObservableProperty]
         private string _title = "FluentPDF";

         [ObservableProperty]
         private string _statusMessage = "Ready";

         [ObservableProperty]
         private bool _isLoading;
         ```
       - Add commands using attributes:
         ```csharp
         [RelayCommand]
         private async Task LoadDocumentAsync()
         {
             IsLoading = true;
             StatusMessage = "Loading...";
             await Task.Delay(1000); // Simulate work
             StatusMessage = "Ready";
             IsLoading = false;
         }

         [RelayCommand(CanExecute = nameof(CanSave))]
         private void Save()
         {
             StatusMessage = "Saved!";
         }

         private bool CanSave() => !IsLoading;
         ```
       - Inject ILogger<MainViewModel> via constructor
       - Log property changes in OnPropertyChanged override

    3. **Register in DI** (App.xaml.cs):
       - Add: `services.AddTransient<MainViewModel>();`

    4. **Modify MainWindow.xaml**:
       - Add x:Name="RootWindow" to Window
       - In code-behind constructor:
         ```csharp
         var vm = ((App)Application.Current).GetService<MainViewModel>();
         RootWindow.DataContext = vm;
         ```
       - Add simple UI demonstrating binding:
         - TextBlock bound to Title
         - TextBlock bound to StatusMessage
         - Button bound to LoadDocumentCommand
         - Button bound to SaveCommand (disabled when loading)

    5. **MainViewModelTests.cs**:
       - Test property change notifications (PropertyChanged event)
       - Test commands can execute and update properties
       - Test async command handles concurrent execution
       - Use FluentAssertions: `vm.Title.Should().Be("FluentPDF");`
       - Mock ILogger to verify logging
       - Ensure tests run WITHOUT WinUI runtime (headless)

    **Restrictions:**
    - Do NOT add complex business logic yet - this is a pattern demonstration
    - Do NOT create multiple ViewModels yet - just MainViewModel
    - Ensure ViewModel has ZERO `using Microsoft.UI.Xaml` statements
    - Keep ViewModel under 500 lines
    - Follow structure.md ViewModel organization pattern

    **Success Criteria:**
    - Source generators create Title and StatusMessage properties automatically
    - Property changes trigger INotifyPropertyChanged events
    - Commands are correctly generated with CanExecute logic
    - UI bindings work: clicking button updates StatusMessage
    - Tests run headless: `dotnet test --filter MainViewModelTests` succeeds without UI
    - ArchUnitNET ViewModels_Should_InheritFrom_ObservableObject test passes

    **Instructions:**
    1. Read design.md "Component 2: MVVM Foundation" section carefully
    2. Edit tasks.md: change to `[-]`
    3. Install CommunityToolkit.Mvvm
    4. Create MainViewModel with attributes
    5. Wire up to MainWindow
    6. Write comprehensive headless tests
    7. Run app and verify bindings work
    8. Log implementation with artifacts:
       - Include MainViewModel in artifacts.components
       - Document MVVM pattern with source generators
       - Note headless testability
    9. Edit tasks.md: change to `[x]`

- [ ] 9. Create vcpkg build script (build-libs.ps1) for PDFium and QPDF
  - Files:
    - `tools/build-libs.ps1`
    - `.gitmodules` (add vcpkg submodule)
    - `README.md` (add build instructions)
  - Create PowerShell script to bootstrap vcpkg
  - Install PDFium and QPDF via vcpkg with x64-windows triplet
  - Copy built DLLs to libs/x64/ directory
  - Add error handling and progress reporting
  - Document usage in README
  - Purpose: Automate native library build process for reproducible builds
  - _Leverage: vcpkg, Task 1 (solution structure)_
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** DevOps Engineer specializing in build automation and native dependency management

    **Task:**
    Create an automated build script for PDFium and QPDF native libraries using vcpkg:

    1. **build-libs.ps1** (in tools/):
       - Parameters:
         - `[string]$Triplet = "x64-windows"` (default x64, support arm64-windows)
         - `[switch]$Clean` (force rebuild)
         - `[switch]$UseCache` (use vcpkg binary cache)
       - Script logic:
         a) Check if vcpkg exists at tools/vcpkg/
            - If not: `git clone https://github.com/microsoft/vcpkg.git tools/vcpkg`
         b) Bootstrap vcpkg if vcpkg.exe doesn't exist:
            - Run: `.\tools\vcpkg\bootstrap-vcpkg.bat`
         c) If $UseCache, set VCPKG_BINARY_SOURCES environment variable
         d) Install libraries:
            - `.\tools\vcpkg\vcpkg install pdfium:$Triplet qpdf:$Triplet`
         e) Copy DLLs:
            - From: `tools/vcpkg/installed/$Triplet/bin/*.dll`
            - To: `libs/$($Triplet -replace '-windows', '')/*.dll`
            - Create target directory if it doesn't exist
         f) Copy include headers (for future P/Invoke):
            - From: `tools/vcpkg/installed/$Triplet/include/`
            - To: `libs/$($Triplet -replace '-windows', '')/include/`
         g) Display summary:
            - "✓ Built PDFium and QPDF for $Triplet"
            - "✓ DLLs copied to libs/ directory"
            - "Next step: Add DLLs as Content items in FluentPDF.App.csproj"

    2. **.gitmodules** (add if vcpkg as submodule preferred):
       - Alternatively, document that vcpkg will be cloned by script

    3. **README.md** (add section):
       - "Building Native Dependencies"
       - Prerequisites: Visual Studio 2022, Git
       - Steps:
         1. Run `.\tools\build-libs.ps1`
         2. Wait for vcpkg to build (first time: ~30 minutes)
         3. Verify DLLs in libs/x64/
       - For ARM64: `.\tools\build-libs.ps1 -Triplet arm64-windows`

    **Restrictions:**
    - Do NOT commit vcpkg installation to repo (add tools/vcpkg/ to .gitignore)
    - Do NOT commit built DLLs to repo yet (large binary files)
    - Do commit include headers and build scripts
    - Handle errors gracefully (vcpkg not found, build failures)
    - Display progress to console for user feedback

    **Success Criteria:**
    - Running `.\tools\build-libs.ps1` successfully builds PDFium and QPDF
    - DLLs appear in libs/x64/ directory
    - Script works on clean clone (no vcpkg pre-installed)
    - Build is reproducible (same DLLs on different machines)
    - Script completes in < 5 minutes after vcpkg bootstrap (using cache)
    - README clearly documents the build process

    **Instructions:**
    1. Read design.md "Component 8: vcpkg Build Infrastructure" section
    2. Edit tasks.md: change to `[-]`
    3. Create build-libs.ps1 with parameter handling and error checks
    4. Test on clean environment (delete tools/vcpkg if exists)
    5. Verify DLLs are built and copied correctly
    6. Update README.md with build instructions
    7. Add tools/vcpkg/ to .gitignore
    8. Log implementation with artifacts:
       - Document build automation script
       - Note vcpkg integration
       - Include paths where DLLs are placed
    9. Edit tasks.md: change to `[x]`

- [ ] 10. Create GitHub Actions CI/CD workflows (build, test, quality-analysis)
  - Files:
    - `.github/workflows/build.yml`
    - `.github/workflows/test.yml`
    - `.github/workflows/quality-analysis.yml`
    - `.github/workflows/visual-regression.yml`
  - Create build workflow for Windows Server 2022
  - Create test workflow running all test projects
  - Create quality analysis workflow for TRX analysis (placeholder for AI agent)
  - Configure vcpkg binary caching for CI
  - Purpose: Automated CI/CD pipeline with quality gates
  - _Leverage: GitHub Actions, Task 9 (build-libs.ps1), Task 2 (tests)_
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** DevOps/CI Engineer specializing in GitHub Actions and automated pipelines

    **Task:**
    Create comprehensive GitHub Actions workflows for CI/CD:

    1. **build.yml**:
       - Trigger: push, pull_request to main
       - Runs on: windows-2022
       - Steps:
         1. Checkout code: `actions/checkout@v4`
         2. Setup MSBuild: `microsoft/setup-msbuild@v2`
         3. Setup .NET: `actions/setup-dotnet@v4` with .NET 8
         4. Restore vcpkg cache: `actions/cache@v4` with key based on vcpkg-manifest
         5. Build native libraries: `.\tools\build-libs.ps1 -UseCache`
         6. Restore NuGet: `dotnet restore FluentPDF.sln`
         7. Build solution: `msbuild FluentPDF.sln /p:Configuration=Release /p:Platform="Any CPU"`
         8. Upload build artifacts: `actions/upload-artifact@v4` with FluentPDF.App output

    2. **test.yml**:
       - Depends on: build.yml (download artifacts)
       - Runs on: windows-2022
       - Steps:
         1. Checkout and setup (same as build)
         2. Run architecture tests: `dotnet test tests/FluentPDF.Architecture.Tests --logger "trx;LogFileName=architecture-tests.trx"`
         3. Run unit tests: `dotnet test tests/FluentPDF.Core.Tests --logger "trx;LogFileName=core-tests.trx"`
         4. Upload TRX files: `actions/upload-artifact@v4` with name "test-results"
         5. Publish test results: `dorny/test-reporter@v1` (shows results in PR)

    3. **quality-analysis.yml**:
       - Depends on: test.yml
       - Runs on: ubuntu-latest (placeholder for AI agent)
       - Steps:
         1. Download test results: `actions/download-artifact@v4`
         2. Placeholder: "AI quality analysis will be implemented here"
         3. Comment: Add TODO to implement AI agent TRX analysis script
         4. For now, just echo summary of test results

    4. **visual-regression.yml**:
       - Runs on: windows-2022
       - Steps:
         1. Setup (same as build and test)
         2. Placeholder: "Visual regression tests with Win2D headless rendering"
         3. Comment: Will be implemented when UI tests exist

    **Restrictions:**
    - Do NOT add actual AI analysis scripts yet (placeholder only)
    - Do NOT run App.Tests yet if they don't exist (conditional)
    - Keep workflows simple and fast (use caching aggressively)
    - Ensure workflows fail fast if critical errors occur
    - Add status badges to README for build and test status

    **Success Criteria:**
    - Push to GitHub triggers all workflows
    - Build workflow compiles solution successfully
    - Test workflow runs architecture and unit tests
    - TRX files are uploaded as artifacts
    - Test results appear in PR checks
    - Workflows complete in < 10 minutes total
    - vcpkg cache reduces build time significantly (90% faster)

    **Instructions:**
    1. Read design.md "Component 9: CI/CD Pipeline Configuration" section
    2. Edit tasks.md: change to `[-]`
    3. Create .github/workflows/ directory
    4. Implement build.yml with vcpkg caching
    5. Implement test.yml with TRX upload
    6. Implement placeholder quality-analysis.yml
    7. Implement placeholder visual-regression.yml
    8. Push to GitHub and verify workflows run
    9. Log implementation with artifacts:
       - Document all CI/CD workflows
       - Note vcpkg caching strategy
       - Include workflow files in implementation log
    10. Edit tasks.md: change to `[x]`

- [ ] 11. Create comprehensive README.md with setup and architecture documentation
  - Files:
    - `README.md` (create/update)
    - `.editorconfig` (create)
    - `.gitignore` (update)
  - Write comprehensive project README with:
    - Project overview and goals
    - Prerequisites and setup instructions
    - Build instructions (native libs + solution)
    - Architecture overview diagram
    - Testing guide
    - Contributing guidelines
  - Create .editorconfig for code style enforcement
  - Update .gitignore for project artifacts
  - Purpose: Comprehensive documentation for developers
  - _Leverage: All previous tasks_
  - _Requirements: 1.5, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Technical Writer and Developer Experience Engineer

    **Task:**
    Create comprehensive project documentation and development environment configuration:

    1. **README.md** (root of repo):
       - Sections:

         ## FluentPDF
         High-quality, ethically-designed PDF application for Windows built on WinUI 3
         [Add badges: Build status, Test status, License]

         ## Features
         - Enterprise-grade PDF rendering with PDFium
         - Modern MVVM architecture with CommunityToolkit
         - Verifiable quality with ArchUnitNET
         - Comprehensive observability with Serilog + OpenTelemetry

         ## Prerequisites
         - Windows 10 (1809+) or Windows 11
         - Visual Studio 2022 with workloads:
           - .NET desktop development
           - Desktop development with C++
         - Git
         - PowerShell 5.1+

         ## Getting Started
         1. Clone the repository: `git clone https://github.com/you/FluentPDF.git`
         2. Build native libraries: `.\tools\build-libs.ps1` (first time: ~30 min)
         3. Open `FluentPDF.sln` in Visual Studio
         4. Build and run (F5)

         ## Architecture
         [Include diagram from design.md showing layers]
         - **FluentPDF.App**: WinUI 3 presentation layer
         - **FluentPDF.Core**: Business logic (UI-agnostic)
         - **FluentPDF.Rendering**: PDF rendering with PDFium P/Invoke

         ## Testing
         - Architecture tests: `dotnet test tests/FluentPDF.Architecture.Tests`
         - Unit tests: `dotnet test tests/FluentPDF.Core.Tests`
         - All tests: `dotnet test FluentPDF.sln`

         ## Code Quality
         - ArchUnitNET enforces architectural rules
         - Max 500 lines per file, 50 lines per method
         - 80% test coverage minimum (90% for critical paths)

         ## Observability
         - Logs: ApplicationData/LocalFolder/logs/ (JSON format)
         - .NET Aspire Dashboard: http://localhost:4317 (development)

         ## Contributing
         1. Fork the repository
         2. Create a feature branch
         3. Ensure all tests pass
         4. Submit a pull request

         ## License
         [Add license information]

    2. **.editorconfig** (root):
       - Configure for C# and C++:
         ```ini
         root = true

         [*]
         charset = utf-8
         insert_final_newline = true
         trim_trailing_whitespace = true

         [*.cs]
         indent_style = space
         indent_size = 4

         # Code style rules (follow steering/structure.md)
         dotnet_sort_system_directives_first = true
         csharp_new_line_before_open_brace = all
         csharp_prefer_braces = true:warning

         # Naming conventions
         dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
         dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
         dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i

         [*.cpp]
         indent_style = space
         indent_size = 2
         ```

    3. **.gitignore** (update):
       - Add:
         - tools/vcpkg/
         - libs/**/*.dll (large binaries, built locally)
         - .vs/
         - bin/
         - obj/
         - *.user
         - *.suo
         - TestResults/

    **Restrictions:**
    - Do NOT include sensitive information (API keys, secrets)
    - Keep README concise but comprehensive
    - Use relative links for internal documentation
    - Add diagrams using Mermaid markdown where appropriate
    - Ensure all commands are copy-pasteable and work

    **Success Criteria:**
    - New developer can clone and build following README only
    - All commands in README execute successfully
    - .editorconfig enforces consistent code style
    - GitHub renders README nicely with working badges
    - Architecture diagram clearly explains project structure
    - .gitignore prevents committing build artifacts

    **Instructions:**
    1. Read all steering documents for context
    2. Edit tasks.md: change to `[-]`
    3. Create comprehensive README.md
    4. Create .editorconfig with C# style rules
    5. Update .gitignore with all build artifacts
    6. Add GitHub badges (build status, tests)
    7. Verify all commands in README work
    8. Log implementation with artifacts:
       - Note comprehensive documentation created
       - Include .editorconfig configuration
    9. Edit tasks.md: change to `[x]`

- [ ] 12. Final integration testing and documentation review
  - Files:
    - `docs/ARCHITECTURE.md` (create)
    - `docs/TESTING.md` (create)
    - Integration verification checklist
  - Run full solution build and all tests
  - Verify all DI registrations resolve correctly
  - Verify logging works end-to-end (file + OTLP)
  - Verify architecture tests pass
  - Create architecture decision records (ADR)
  - Document testing strategy
  - Validate against all requirements
  - Purpose: Final verification that foundation is complete and production-ready
  - _Leverage: All previous tasks_
  - _Requirements: All requirements_
  - _Prompt: |
    Implement the task for spec project-foundation. First run spec-workflow-guide to get the workflow guide, then implement the task:

    **Role:** Senior Software Engineer and Technical Lead performing final integration validation

    **Task:**
    Perform comprehensive integration testing and create final documentation:

    1. **Integration Testing Checklist**:
       - [ ] Solution builds without errors: `dotnet build FluentPDF.sln`
       - [ ] All tests pass: `dotnet test FluentPDF.sln`
       - [ ] Architecture tests enforce layering rules
       - [ ] App starts without exceptions
       - [ ] DI container resolves all services successfully
       - [ ] Logging writes to file and OTLP endpoint
       - [ ] Global exception handlers catch and log errors
       - [ ] ViewModel bindings work in MainWindow
       - [ ] Result pattern errors include all metadata
       - [ ] vcpkg script builds native libraries
       - [ ] CI workflows run successfully on GitHub

    2. **docs/ARCHITECTURE.md**:
       - Create comprehensive architecture document:
         - System overview with diagram
         - Layer descriptions (App, Core, Rendering)
         - Dependency flow rules
         - Error handling strategy (Result pattern + global handlers)
         - Observability infrastructure (Serilog + OTLP)
         - MVVM pattern with CommunityToolkit
         - DI container configuration
       - Link to steering documents for rationale
       - Include decision log (why FluentResults over exceptions, etc.)

    3. **docs/TESTING.md**:
       - Testing strategy document:
         - Testing pyramid (unit → architecture → integration → UI)
         - How to run tests
         - How to write new tests
         - ArchUnitNET rules and how to add new ones
         - Visual regression testing with Win2D (when implemented)
         - Performance testing with BenchmarkDotNet
       - Coverage requirements (80% minimum)
       - CI/CD integration

    4. **Validation Against Requirements**:
       - Go through each requirement in requirements.md
       - Verify acceptance criteria are met
       - Document any deviations or future work
       - Create issues for any gaps

    5. **Final Smoke Tests**:
       - Clone repo to clean directory
       - Follow README.md setup instructions
       - Build and run app
       - Trigger exceptions and verify logging
       - Check log files in ApplicationData folder
       - Verify .NET Aspire Dashboard shows logs

    **Restrictions:**
    - Do NOT skip any acceptance criteria validation
    - Do NOT approve if critical features are missing
    - Ensure documentation is complete and accurate
    - All links in docs must work

    **Success Criteria:**
    - All requirements have acceptance criteria met
    - Full solution builds and runs on clean clone
    - All tests pass (architecture, unit, integration)
    - Documentation is comprehensive and accurate
    - Project is ready for feature development
    - No critical bugs or missing infrastructure

    **Instructions:**
    1. Read all requirements and verify each acceptance criteria
    2. Edit tasks.md: change to `[-]`
    3. Run complete integration testing checklist
    4. Create ARCHITECTURE.md documenting system design
    5. Create TESTING.md documenting test strategy
    6. Perform clean clone smoke test
    7. Fix any issues found during validation
    8. Log implementation with artifacts:
       - Include final verification results
       - Document all created documentation files
       - Confirm project foundation is complete
    9. Edit tasks.md: change to `[x]`
    10. Mark spec as complete: all tasks should show `[x]`

## Summary

This spec establishes the complete project foundation including:
- ✓ Solution structure (App, Core, Rendering, Tests)
- ✓ MVVM with CommunityToolkit source generators
- ✓ Result pattern error handling with FluentResults
- ✓ Dependency injection with IHost
- ✓ Structured logging with Serilog + OpenTelemetry
- ✓ Global exception handlers (3-layer protection)
- ✓ ArchUnitNET architecture enforcement
- ✓ Testing infrastructure (xUnit, FluentAssertions, Moq, FlaUI)
- ✓ vcpkg build automation for native libraries
- ✓ GitHub Actions CI/CD pipelines
- ✓ Comprehensive documentation

**Next steps after completion:**
- Implement PDF rendering with PDFium (new spec)
- Implement document operations with QPDF (new spec)
- Implement UI for document viewer (new spec)
