# Design: Architecture Consolidation

## 1. Architecture Overview

### 1.1 Current Architecture (Duplicated)

```
┌─────────────────────────────┐  ┌─────────────────────────────┐
│   FluentPDF.App (WinUI)     │  │   FluentPDF.Avalonia        │
│  ┌──────────────────────┐   │  │  ┌──────────────────────┐   │
│  │ Services (1,180 LOC) │   │  │  │ Services (1,145 LOC) │   │
│  │ - SettingsService    │   │  │  │ - SettingsService    │   │
│  │ - RecentFilesService │   │  │  │ - RecentFilesService │   │
│  │ - AnimationService   │   │  │  │ - AnimationService   │   │
│  │ - ThemeService       │   │  │  │ - ThemeService       │   │
│  └──────────────────────┘   │  │  └──────────────────────┘   │
│  ┌──────────────────────┐   │  │  ┌──────────────────────┐   │
│  │ API (950 LOC)        │   │  │  │ API (950 LOC)        │   │
│  │ - Endpoints          │   │  │  │ - Endpoints          │   │
│  │ - SessionManager     │   │  │  │ - SessionManager     │   │
│  └──────────────────────┘   │  │  └──────────────────────┘   │
│  ┌──────────────────────┐   │  │  ┌──────────────────────┐   │
│  │ ViewModels (2,400 LOC)│  │  │  │ ViewModels (2,345 LOC)│  │
│  │ - MainViewModel      │   │  │  │ - MainViewModel      │   │
│  │ - PdfViewerViewModel │   │  │  │ - PdfViewerViewModel │   │
│  └──────────────────────┘   │  │  └──────────────────────┘   │
└─────────────────────────────┘  └─────────────────────────────┘
              │                              │
              └──────────┬───────────────────┘
                         ▼
              ┌─────────────────────┐
              │  FluentPDF.Core     │
              │  - Interfaces       │
              │  - Some Services    │
              └─────────────────────┘
```

**Problem:** 11,050 lines of duplicate code! Changes must be made twice, bugs fixed twice, features tested twice.

### 1.2 Target Architecture (Consolidated)

```
┌──────────────────┐  ┌──────────────────┐
│ FluentPDF.App    │  │FluentPDF.Avalonia│
│  (Thin UI)       │  │  (Thin UI)       │
│ ┌──────────────┐ │  │ ┌──────────────┐ │
│ │ UI-Specific  │ │  │ │ UI-Specific  │ │
│ │ Views        │ │  │ │ Views        │ │
│ │ Styles       │ │  │ │ Styles       │ │
│ │ Converters   │ │  │ │ Converters   │ │
│ └──────────────┘ │  │ └──────────────┘ │
│ ┌──────────────┐ │  │ ┌──────────────┐ │
│ │ UI ViewModels│ │  │ │ UI ViewModels│ │
│ │ (inherits    │ │  │ │ (inherits    │ │
│ │  from Core)  │ │  │ │  from Core)  │ │
│ └──────────────┘ │  │ └──────────────┘ │
│ ┌──────────────┐ │  │ ┌──────────────┐ │
│ │ Platform     │ │  │ │ Platform     │ │
│ │ Adapters     │ │  │ │ Adapters     │ │
│ │ (100 LOC)    │ │  │ │ (100 LOC)    │ │
│ └──────────────┘ │  │ └──────────────┘ │
└──────────────────┘  └──────────────────┘
         │                     │
         └──────────┬──────────┘
                    ▼
         ┌──────────────────────┐
         │  FluentPDF.Core      │
         │  (Shared Business    │
         │   Logic - SSOT)      │
         │ ┌──────────────────┐ │
         │ │ Services         │ │
         │ │ - Settings       │ │
         │ │ - RecentFiles    │ │
         │ │ - Telemetry      │ │
         │ │ (1,200 LOC)      │ │
         │ └──────────────────┘ │
         │ ┌──────────────────┐ │
         │ │ ViewModels.Base  │ │
         │ │ - MainVMBase     │ │
         │ │ - PdfViewerVMBase│ │
         │ │ (2,000 LOC)      │ │
         │ └──────────────────┘ │
         │ ┌──────────────────┐ │
         │ │ API (Shared)     │ │
         │ │ - Endpoints      │ │
         │ │ - SessionManager │ │
         │ │ (950 LOC)        │ │
         │ └──────────────────┘ │
         │ ┌──────────────────┐ │
         │ │ Abstractions     │ │
         │ │ - INavigation    │ │
         │ │ - IFileDialog    │ │
         │ │ - IThemeService  │ │
         │ └──────────────────┘ │
         └──────────────────────┘
```

**Result:**
- ~11,000 lines eliminated from UI projects
- Business logic centralized in Core
- UI projects reduced to ~200-300 lines of platform adapters
- Changes made once, tested once

### 1.3 Project Structure

**FluentPDF.Core** (expanded):
```
FluentPDF.Core/
├── Services/               # Concrete implementations
│   ├── SettingsService.cs
│   ├── RecentFilesService.cs
│   ├── TelemetryService.cs
│   └── ... (other shared services)
├── Services.Abstractions/  # Platform-specific abstractions
│   ├── INavigationService.cs
│   ├── IFileDialogService.cs
│   ├── IThemeService.cs
│   └── IDiagnosticProvider.cs
├── ViewModels/             # Base ViewModels (NEW)
│   ├── ViewModelBase.cs
│   ├── MainViewModelBase.cs
│   ├── PdfViewerViewModelBase.cs
│   ├── AnnotationViewModelBase.cs
│   └── ... (other base VMs)
├── Api/                    # REST API (NEW - moved from UI)
│   ├── Endpoints/
│   │   ├── DocumentEndpoints.cs
│   │   ├── RenderEndpoints.cs
│   │   └── ... (all endpoints)
│   ├── Services/
│   │   ├── DocumentSessionManager.cs
│   │   ├── HashingService.cs
│   │   └── UiAutomationServiceBase.cs
│   └── VerificationApiExtensions.cs
├── Diagnostics/            # Diagnostic commands (moved)
│   ├── Commands/
│   ├── Models/
│   └── DiagnosticCommandHandler.cs
└── Configuration/
    ├── AppSettings.cs
    └── PlatformSettings.cs
```

**FluentPDF.App** (reduced):
```
FluentPDF.App/
├── Views/                  # WinUI-specific XAML
├── ViewModels/             # Inherit from Core base VMs
│   ├── MainViewModel.cs    # : MainViewModelBase (50 LOC)
│   ├── PdfViewerViewModel.cs # : PdfViewerViewModelBase (60 LOC)
│   └── ... (thin wrappers, 400 LOC total)
├── Services/               # Platform adapters only
│   ├── WinUINavigationService.cs   # : INavigationService (50 LOC)
│   ├── WinUIFileDialogService.cs   # : IFileDialogService (40 LOC)
│   ├── WinUIThemeService.cs        # : IThemeService (30 LOC)
│   └── WinUIDiagnosticProvider.cs  # : IDiagnosticProvider (40 LOC)
│   # Total platform adapters: ~200 LOC
├── Converters/             # WinUI-specific converters
├── Styles/                 # WinUI themes
└── Program.cs              # DI configuration
```

**FluentPDF.Avalonia** (reduced):
```
FluentPDF.Avalonia/
├── Views/                  # Avalonia-specific XAML
├── ViewModels/             # Inherit from Core base VMs
│   ├── MainViewModel.cs    # : MainViewModelBase (50 LOC)
│   ├── PdfViewerViewModel.cs # : PdfViewerViewModelBase (60 LOC)
│   └── ... (thin wrappers, 400 LOC total)
├── Services/               # Platform adapters only
│   ├── AvaloniaNavigationService.cs   # : INavigationService (50 LOC)
│   ├── AvaloniaFileDialogService.cs   # : IFileDialogService (40 LOC)
│   ├── AvaloniaThemeService.cs        # : IThemeService (30 LOC)
│   └── AvaloniaDiagnosticProvider.cs  # : IDiagnosticProvider (40 LOC)
│   # Total platform adapters: ~200 LOC
├── Converters/             # Avalonia-specific converters
├── Styles/                 # Avalonia themes
└── Program.cs              # DI configuration
```

## 2. Service Consolidation

### 2.1 Settings Service (Example)

**Before (Duplicated):**
```csharp
// FluentPDF.App/Services/SettingsService.cs (350 lines)
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluentPDF", "settings.json");

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_settingsPath, json);
    }
    // ... 340 more lines
}

// FluentPDF.Avalonia/Services/AvaloniaSettingsService.cs (340 lines)
public class AvaloniaSettingsService : ISettingsService
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FluentPDF", "settings.json");

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_settingsPath, json);
    }
    // ... 330 more lines - IDENTICAL!
}
```

**After (Consolidated):**
```csharp
// FluentPDF.Core/Services/SettingsService.cs (350 lines)
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FluentPDF", "settings.json");
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_settingsPath, json, ct);
        _logger.LogInformation("Settings saved to {Path}", _settingsPath);
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_settingsPath))
            return AppSettings.Default;

        var json = await File.ReadAllTextAsync(_settingsPath, ct);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.Default;
    }
    // ... rest of implementation (ONE PLACE!)
}

// Both UI projects: DELETE their SettingsService, use Core's version
// DI Configuration in both:
services.AddSingleton<ISettingsService, SettingsService>();
```

**Savings:** 340 lines eliminated, one source of truth!

### 2.2 Platform-Specific Services (Abstraction Pattern)

Some services need platform-specific implementations (file dialogs, navigation, theme). Use abstraction in Core, implementation in UI.

**Core Abstraction:**
```csharp
// FluentPDF.Core/Services.Abstractions/IFileDialogService.cs
public interface IFileDialogService
{
    Task<string?> OpenFileAsync(FileDialogOptions options);
    Task<string?> SaveFileAsync(FileDialogOptions options);
    Task<IReadOnlyList<string>> OpenMultipleFilesAsync(FileDialogOptions options);
}

public class FileDialogOptions
{
    public string? Title { get; init; }
    public IReadOnlyList<FileFilter>? Filters { get; init; }
    public string? InitialDirectory { get; init; }
}

public class FileFilter
{
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Extensions { get; init; } = Array.Empty<string>();
}
```

**WinUI Implementation (Thin Adapter):**
```csharp
// FluentPDF.App/Services/WinUIFileDialogService.cs (40 lines)
public class WinUIFileDialogService : IFileDialogService
{
    private readonly Window _window;

    public WinUIFileDialogService(Window window)
    {
        _window = window;
    }

    public async Task<string?> OpenFileAsync(FileDialogOptions options)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        if (options.Filters != null)
        {
            foreach (var filter in options.Filters)
            {
                foreach (var ext in filter.Extensions)
                    picker.FileTypeFilter.Add(ext);
            }
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    // SaveFileAsync, OpenMultipleFilesAsync (~30 more lines)
}
```

**Avalonia Implementation (Thin Adapter):**
```csharp
// FluentPDF.Avalonia/Services/AvaloniaFileDialogService.cs (40 lines)
public class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Window _window;

    public AvaloniaFileDialogService(Window window)
    {
        _window = window;
    }

    public async Task<string?> OpenFileAsync(FileDialogOptions options)
    {
        var dialog = new OpenFileDialog
        {
            Title = options.Title,
            Directory = options.InitialDirectory
        };

        if (options.Filters != null)
        {
            dialog.Filters = options.Filters
                .Select(f => new Avalonia.Controls.FileDialogFilter
                {
                    Name = f.Name,
                    Extensions = f.Extensions.ToList()
                })
                .ToList();
        }

        var result = await dialog.ShowAsync(_window);
        return result?.FirstOrDefault();
    }

    // SaveFileAsync, OpenMultipleFilesAsync (~30 more lines)
}
```

**Result:**
- Interface in Core (20 lines)
- WinUI adapter (40 lines)
- Avalonia adapter (40 lines)
- **Total:** 100 lines vs previous 600 lines duplicated!

## 3. ViewModel Consolidation

### 3.1 Base ViewModel Pattern

**Core Base ViewModel:**
```csharp
// FluentPDF.Core/ViewModels/ViewModelBase.cs
public abstract class ViewModelBase : ObservableObject
{
    protected readonly ILogger Logger;

    protected ViewModelBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Executes async command with error handling
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string operationName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during {Operation}", operationName);
            OnError(ex, operationName);
        }
    }

    /// <summary>
    /// Override to handle errors (show dialog, toast, etc.)
    /// </summary>
    protected virtual void OnError(Exception ex, string operationName)
    {
        // Platform-specific error display
    }
}
```

**Document ViewModel Base:**
```csharp
// FluentPDF.Core/ViewModels/MainViewModelBase.cs
public abstract class MainViewModelBase : ViewModelBase
{
    protected readonly IPdfDocumentService _documentService;
    protected readonly ISettingsService _settingsService;
    protected readonly IRecentFilesService _recentFilesService;
    protected readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private bool _isDocumentOpen;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private int _currentPageIndex;

    [ObservableProperty]
    private int _totalPages;

    public IRelayCommand OpenDocumentCommand { get; }
    public IRelayCommand CloseDocumentCommand { get; }
    public IRelayCommand SaveDocumentCommand { get; }

    protected MainViewModelBase(
        IPdfDocumentService documentService,
        ISettingsService settingsService,
        IRecentFilesService recentFilesService,
        IFileDialogService fileDialogService,
        ILogger<MainViewModelBase> logger)
        : base(logger)
    {
        _documentService = documentService;
        _settingsService = settingsService;
        _recentFilesService = recentFilesService;
        _fileDialogService = fileDialogService;

        OpenDocumentCommand = new AsyncRelayCommand(OpenDocumentAsync);
        CloseDocumentCommand = new RelayCommand(CloseDocument);
        SaveDocumentCommand = new AsyncRelayCommand(SaveDocumentAsync);
    }

    protected virtual async Task OpenDocumentAsync()
    {
        var path = await _fileDialogService.OpenFileAsync(new FileDialogOptions
        {
            Title = "Open PDF",
            Filters = new[] { new FileFilter { Name = "PDF Files", Extensions = new[] { ".pdf" } } }
        });

        if (path == null) return;

        await ExecuteAsync(async () =>
        {
            var document = await _documentService.OpenAsync(path);
            CurrentFilePath = path;
            TotalPages = document.PageCount;
            CurrentPageIndex = 0;
            IsDocumentOpen = true;

            await _recentFilesService.AddRecentFileAsync(path);
        }, "OpenDocument");
    }

    protected virtual void CloseDocument()
    {
        _documentService.CloseDocument();
        IsDocumentOpen = false;
        CurrentFilePath = null;
    }

    protected virtual async Task SaveDocumentAsync()
    {
        if (CurrentFilePath == null) return;

        await ExecuteAsync(async () =>
        {
            await _documentService.SaveAsync(CurrentFilePath);
        }, "SaveDocument");
    }

    // ... other shared methods (600 lines total)
}
```

**WinUI ViewModel (Thin Wrapper):**
```csharp
// FluentPDF.App/ViewModels/MainViewModel.cs (50 lines)
public class MainViewModel : MainViewModelBase
{
    private readonly XamlRoot? _xamlRoot;

    public MainViewModel(
        IPdfDocumentService documentService,
        ISettingsService settingsService,
        IRecentFilesService recentFilesService,
        IFileDialogService fileDialogService,
        ILogger<MainViewModel> logger)
        : base(documentService, settingsService, recentFilesService, fileDialogService, logger)
    {
    }

    // WinUI-specific: Set XamlRoot for dialogs
    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    // Override error handling for WinUI ContentDialog
    protected override void OnError(Exception ex, string operationName)
    {
        if (_xamlRoot != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = _xamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }

    // Any other WinUI-specific properties/methods (minimal)
}
```

**Avalonia ViewModel (Thin Wrapper):**
```csharp
// FluentPDF.Avalonia/ViewModels/MainViewModel.cs (50 lines)
public class MainViewModel : MainViewModelBase
{
    private readonly Interaction<string, Unit> _showErrorInteraction = new();

    public Interaction<string, Unit> ShowErrorInteraction => _showErrorInteraction;

    public MainViewModel(
        IPdfDocumentService documentService,
        ISettingsService settingsService,
        IRecentFilesService recentFilesService,
        IFileDialogService fileDialogService,
        ILogger<MainViewModel> logger)
        : base(documentService, settingsService, recentFilesService, fileDialogService, logger)
    {
    }

    // Override error handling for Avalonia interaction
    protected override void OnError(Exception ex, string operationName)
    {
        _ = _showErrorInteraction.Handle(ex.Message);
    }

    // Any other Avalonia-specific properties/methods (minimal)
}
```

**Result:**
- Base ViewModel: 600 lines (shared)
- WinUI ViewModel: 50 lines (adapter)
- Avalonia ViewModel: 50 lines (adapter)
- **Savings:** 550 lines eliminated (was 800 + 780 = 1,580 duplicated)

## 4. API Consolidation

### 4.1 Shared API in Core

**Before:** API duplicated in both UI projects (950 lines each = 1,900 total)

**After:** API in Core (950 lines), both UIs host it

```csharp
// FluentPDF.Core/Api/VerificationApiExtensions.cs
public static class VerificationApiExtensions
{
    public static IServiceCollection AddVerificationApi(this IServiceCollection services)
    {
        services.AddSingleton<DocumentSessionManager>();
        services.AddSingleton<HashingService>();
        services.AddTransient<IUiAutomationService, UiAutomationService>();
        return services;
    }

    public static WebApplication UseVerificationApi(this WebApplication app)
    {
        app.MapDocumentEndpoints();
        app.MapRenderEndpoints();
        app.MapVerifyEndpoints();
        app.MapHealthEndpoints();
        // ... other endpoints
        return app;
    }
}

// Both UI projects Program.cs:
builder.Services.AddVerificationApi();
// ...
app.UseVerificationApi();
```

**Endpoints stay in Core:**
```csharp
// FluentPDF.Core/Api/Endpoints/DocumentEndpoints.cs
public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/document/load", async (
            LoadDocumentRequest request,
            DocumentSessionManager sessionManager) =>
        {
            var sessionId = await sessionManager.LoadDocumentAsync(request.Path);
            return Results.Ok(new { sessionId });
        });

        // ... other document endpoints
    }
}
```

**UI Automation Abstraction:**
```csharp
// FluentPDF.Core/Api/Services/IUiAutomationService.cs
public interface IUiAutomationService
{
    Task<AutomationElement?> FindElementAsync(string automationId);
    Task<bool> ClickElementAsync(string automationId);
    Task<string?> GetElementTextAsync(string automationId);
}

// Platform implementations inject platform-specific automation
// WinUI: Use UIAutomation framework
// Avalonia: Use Avalonia's automation APIs
```

## 5. Migration Strategy

### 5.1 Phase 1: Services (Weeks 1-2)

1. **Move SettingsService to Core**
   - Copy to Core/Services/SettingsService.cs
   - Test with both UIs
   - Delete from App and Avalonia

2. **Move RecentFilesService to Core**
   - Same process

3. **Move TelemetryService to Core**
   - Already partially in Core, complete migration

4. **Create Platform Abstractions**
   - IFileDialogService → WinUI/Avalonia adapters
   - INavigationService → WinUI/Avalonia adapters
   - IThemeService → WinUI/Avalonia adapters

### 5.2 Phase 2: ViewModels (Weeks 3-4)

1. **Create Core/ViewModels/ViewModelBase.cs**
   - Base class with common MVVM patterns

2. **Create Core/ViewModels/MainViewModelBase.cs**
   - Extract common logic from both MainViewModels
   - Test with both UIs

3. **Update UI ViewModels to inherit from base**
   - Reduce to thin wrappers

4. **Repeat for all ViewModels**
   - PdfViewerViewModelBase
   - AnnotationViewModelBase
   - etc.

### 5.3 Phase 3: API (Week 5)

1. **Move API to Core/Api**
   - Copy endpoints to Core
   - Create VerificationApiExtensions
   - Test with both UIs

2. **Delete duplicated API from UI projects**
   - Verify tests still pass

### 5.4 Phase 4: Cleanup (Week 6)

1. **Remove all duplicated code from UI projects**
2. **Add architecture tests to prevent future duplication**
3. **Update documentation**
4. **Measure savings**

## 6. Dependency Injection Configuration

### 6.1 Shared Services Registration

**Extract common DI to Core:**
```csharp
// FluentPDF.Core/DependencyInjection/CoreServiceExtensions.cs
public static class CoreServiceExtensions
{
    public static IServiceCollection AddFluentPdfCore(
        this IServiceCollection services,
        Action<CoreServicesOptions>? configure = null)
    {
        var options = new CoreServicesOptions();
        configure?.Invoke(options);

        // Register shared services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<ITelemetryService, TelemetryService>();

        // Register PDF services
        services.AddSingleton<IPdfBackend, PdfiumBackend>();
        services.AddScoped<IPdfDocumentService, PdfDocumentService>();
        services.AddScoped<IPdfRenderingService, PdfRenderingService>();

        // Platform abstractions (registered by UI)
        // services.AddSingleton<IFileDialogService, ???>(); // UI provides
        // services.AddSingleton<INavigationService, ???>(); // UI provides

        return services;
    }
}

public class CoreServicesOptions
{
    public string? SettingsPath { get; set; }
    public bool EnableTelemetry { get; set; } = true;
}
```

**UI Project Configuration:**
```csharp
// FluentPDF.App/Program.cs (WinUI)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluentPdfCore(options =>
{
    options.EnableTelemetry = true;
});

// Register platform-specific services
builder.Services.AddSingleton<IFileDialogService, WinUIFileDialogService>();
builder.Services.AddSingleton<INavigationService, WinUINavigationService>();
builder.Services.AddSingleton<IThemeService, WinUIThemeService>();
builder.Services.AddSingleton<IDiagnosticProvider, WinUIDiagnosticProvider>();

// Register ViewModels
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<PdfViewerViewModel>();

// ... rest of configuration
```

```csharp
// FluentPDF.Avalonia/Program.cs (Avalonia)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluentPdfCore(options =>
{
    options.EnableTelemetry = true;
});

// Register platform-specific services (Avalonia implementations)
builder.Services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
builder.Services.AddSingleton<INavigationService, AvaloniaNavigationService>();
builder.Services.AddSingleton<IThemeService, AvaloniaThemeService>();
builder.Services.AddSingleton<IDiagnosticProvider, AvaloniaDiagnosticProvider>();

// Register ViewModels
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<PdfViewerViewModel>();

// ... rest of configuration
```

## 7. Testing Strategy

### 7.1 Shared Service Tests

**Test once in Core.Tests:**
```csharp
// tests/FluentPDF.Core.Tests/Services/SettingsServiceTests.cs
public class SettingsServiceTests
{
    [Fact]
    public async Task SaveSettings_CreatesFile()
    {
        // Arrange
        var service = new SettingsService(NullLogger<SettingsService>.Instance);
        var settings = new AppSettings { Theme = "Dark" };

        // Act
        await service.SaveSettingsAsync(settings);

        // Assert
        var loaded = await service.LoadSettingsAsync();
        Assert.Equal("Dark", loaded.Theme);
    }
}
```

**No need to test in both UI projects anymore!**

### 7.2 ViewModel Tests

**Test base ViewModels in Core.Tests:**
```csharp
// tests/FluentPDF.Core.Tests/ViewModels/MainViewModelBaseTests.cs
public class MainViewModelBaseTests
{
    [Fact]
    public async Task OpenDocument_SetsIsDocumentOpen()
    {
        // Arrange
        var documentService = new Mock<IPdfDocumentService>();
        var fileDialogService = new Mock<IFileDialogService>();
        fileDialogService.Setup(x => x.OpenFileAsync(It.IsAny<FileDialogOptions>()))
            .ReturnsAsync("test.pdf");

        var vm = new TestMainViewModel(
            documentService.Object,
            Mock.Of<ISettingsService>(),
            Mock.Of<IRecentFilesService>(),
            fileDialogService.Object,
            NullLogger<TestMainViewModel>.Instance);

        // Act
        await vm.OpenDocumentCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.IsDocumentOpen);
        Assert.Equal("test.pdf", vm.CurrentFilePath);
    }
}

// Test implementation of abstract base
class TestMainViewModel : MainViewModelBase
{
    public TestMainViewModel(/* params */) : base(/* params */) { }
    protected override void OnError(Exception ex, string operationName) { }
}
```

### 7.3 Architecture Tests

**Enforce no duplication:**
```csharp
// tests/FluentPDF.Architecture.Tests/ConsolidationTests.cs
public class ConsolidationTests
{
    [Fact]
    public void App_ShouldNotHaveDuplicateServices()
    {
        // Arrange
        var assembly = typeof(FluentPDF.App.Program).Assembly;

        // Act & Assert
        var services = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && !t.IsAbstract)
            .ToList();

        // Only platform adapters allowed
        var allowed = new[] { "WinUIFileDialogService", "WinUINavigationService",
            "WinUIThemeService", "WinUIDiagnosticProvider" };

        foreach (var service in services)
        {
            Assert.Contains(service.Name, allowed);
        }
    }

    [Fact]
    public void Core_ViewModels_ShouldBeAbstract()
    {
        var assembly = typeof(MainViewModelBase).Assembly;
        var viewModels = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("ViewModelBase"))
            .ToList();

        Assert.All(viewModels, vm => Assert.True(vm.IsAbstract));
    }
}
```

## 8. Success Metrics

### 8.1 Code Reduction

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| Total LOC in UI projects | 11,050 duplicated | ~600 adapters | 10,450 LOC |
| Service LOC | 2,325 duplicated | 1,200 + 200 adapters | 925 LOC |
| ViewModel LOC | 4,745 duplicated | 2,000 + 200 adapters | 2,545 LOC |
| API LOC | 1,900 duplicated | 950 shared | 950 LOC |
| Duplication % | 50% | &lt;5% | 90% reduction |

### 8.2 Maintenance Impact

**Before:**
- Bug fix requires changes in 2 places
- Feature requires implementation in 2 places
- Tests must pass in 2 projects

**After:**
- Bug fix in Core fixes both UIs
- Feature implemented once, works everywhere
- Tests written once in Core.Tests

## 9. Risks and Mitigation

### 9.1 Breaking Changes

**Risk:** Consolidation breaks existing code

**Mitigation:**
- Incremental migration (service by service)
- Comprehensive test coverage before migration
- Keep both UIs working during entire process

### 9.2 Performance

**Risk:** Abstraction adds overhead

**Mitigation:**
- Benchmark before/after
- Keep abstractions minimal (thin adapters)
- No unnecessary indirection

### 9.3 Platform Differences

**Risk:** Shared code doesn't handle platform differences

**Mitigation:**
- Use abstractions for platform-specific features
- Test on both platforms continuously
- Platform adapters handle all differences
