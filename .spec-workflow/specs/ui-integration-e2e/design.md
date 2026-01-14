# Design: UI Integration E2E

## Architecture Overview

The WinUI 3 app has all ViewModels and services already implemented. This design focuses on:
1. E2E test infrastructure using FlaUI
2. Diagnostic logging to verify app health
3. Incremental feature wiring validation

## Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     E2E Test Infrastructure                      │
├─────────────────────────────────────────────────────────────────┤
│  FlaUI Automation  │  App Launch Fixture  │  Log Verification   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     FluentPDF.App (WinUI 3)                      │
├─────────────────────────────────────────────────────────────────┤
│  MainWindow (TabView)  │  Navigation  │  DI Container            │
├─────────────────────────────────────────────────────────────────┤
│  PdfViewerPage         │  ConversionPage  │  SettingsPage        │
├─────────────────────────────────────────────────────────────────┤
│  ViewModels (CommunityToolkit.Mvvm)                              │
├─────────────────────────────────────────────────────────────────┤
│  FluentPDF.Core Services  │  FluentPDF.Rendering                 │
└─────────────────────────────────────────────────────────────────┘
```

## E2E Test Strategy

### Test Framework
- **FlaUI** - Windows UI Automation wrapper for WinUI 3
- **xUnit** - Test runner
- **Serilog** - Structured logging with JSON format

### Test Categories
1. **Smoke Tests** - App launches, no errors
2. **Feature Tests** - Each feature wired correctly
3. **Integration Tests** - Multi-feature workflows

### Log Verification
- Parse `%LocalAppData%\FluentPDF_Debug.log`
- Assert no ERROR level entries during test
- Capture timing metrics

## Feature Wiring Strategy

### Phase 1: Foundation
1. App launch verification
2. PDFium native library loading
3. DI container health check
4. Main window display

### Phase 2: Core Viewing
1. PDF document loading
2. Page rendering
3. Navigation (prev/next/goto)
4. Zoom controls

### Phase 3: Search & Panels
1. Text search
2. Thumbnails sidebar
3. Bookmarks panel

### Phase 4: Editing Operations
1. Merge documents
2. Split documents
3. Page operations (rotate, delete, reorder)

### Phase 5: Content Creation
1. Annotations
2. Watermarks
3. Image insertion

### Phase 6: Advanced Features
1. Form filling
2. DOCX conversion
3. Settings & preferences

## Automation IDs

All UI elements must have `AutomationProperties.AutomationId` for FlaUI:

| Element | AutomationId |
|---------|-------------|
| Open Button | `OpenDocumentButton` |
| Save Button | `SaveButton` |
| Prev Page | `PreviousPageButton` |
| Next Page | `NextPageButton` |
| Page Number | `PageNumberTextBox` |
| Zoom In | `ZoomInButton` |
| Zoom Out | `ZoomOutButton` |
| Search Toggle | `SearchPanelToggle` |
| Search Input | `SearchTextBox` |
| Merge | `MergeButton` |
| Split | `SplitButton` |
| Watermark | `WatermarkButton` |
| Insert Image | `InsertImageButton` |
| Annotation Tools | `AnnotationToolbar` |
| Thumbnails | `ThumbnailsSidebar` |
| Bookmarks | `BookmarksPanel` |

## Test Fixtures

### AppLaunchFixture
```csharp
public class AppLaunchFixture : IAsyncLifetime
{
    public Application? App { get; private set; }
    public Window? MainWindow { get; private set; }

    public async Task InitializeAsync()
    {
        var appPath = GetAppExecutablePath();
        App = Application.Launch(appPath);
        MainWindow = App.GetMainWindow(Automation);
    }

    public async Task DisposeAsync()
    {
        App?.Close();
    }
}
```

### LogVerifier
```csharp
public class LogVerifier
{
    public void AssertNoErrors(string logPath)
    {
        var lines = File.ReadAllLines(logPath);
        var errors = lines.Where(l => l.Contains("\"Level\":\"Error\""));
        Assert.Empty(errors);
    }
}
```

## File Structure

```
tests/
├── FluentPDF.E2E.Tests/
│   ├── FluentPDF.E2E.Tests.csproj
│   ├── Fixtures/
│   │   ├── AppLaunchFixture.cs
│   │   └── LogVerifier.cs
│   ├── Tests/
│   │   ├── AppLaunchTests.cs
│   │   ├── DocumentLoadingTests.cs
│   │   ├── NavigationTests.cs
│   │   ├── ZoomTests.cs
│   │   ├── SearchTests.cs
│   │   ├── MergeTests.cs
│   │   ├── SplitTests.cs
│   │   ├── AnnotationTests.cs
│   │   ├── WatermarkTests.cs
│   │   ├── ImageInsertionTests.cs
│   │   ├── FormFillingTests.cs
│   │   └── ConversionTests.cs
│   └── TestData/
│       ├── sample.pdf
│       ├── multi-page.pdf
│       ├── form.pdf
│       └── sample.docx
```

## Dependencies

- FlaUI.Core
- FlaUI.UIA3
- xUnit
- Microsoft.NET.Test.Sdk
- FluentAssertions
