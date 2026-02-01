# GUI File Loading Issue - Diagnostic Report

## Executive Summary

**Problem**: When selecting a PDF file through the GUI (File > Open), nothing happens - no tab is created, no document loads.

**Root Cause**: The file dialog opens and file selection works, but the document loading logic in MainViewModel fails silently.

**Impact**: Users cannot open PDFs through the GUI despite the application appearing to run normally.

## Test Results

### ✅ PASSING: PDF Rendering Engine
The underlying PDF rendering engine works perfectly:

```
Test 1: Loading PDF via REST API
  [OK] Document loaded via API
    Session ID: 1fae71fc73b34996b2f127e6456f926d
    Pages: 5
  [OK] Page rendered (16901 bytes)
```

**Verification**: The REST API successfully:
- Loads PDF documents
- Renders pages to PNG
- Handles multi-page documents
- Integrates with PDFium correctly

### ❌ FAILING: GUI File Loading
The GUI file loading mechanism fails:

```
Test 2: Loading PDF via GUI API
  Tab Created: False
  Document Loaded: False
  Page Count: 0
  [FAIL] Document NOT loaded in GUI
  Message: Document load initiated but not confirmed
```

**Evidence**:
1. `OpenRecentFileCommand.ExecuteAsync(filePath)` is called
2. No tab is created in the MainViewModel.Tabs collection
3. No error dialog is shown to the user
4. The failure is silent - exceptions are caught and logged but not surfaced

## Code Flow Analysis

### Working Path (REST API)
```
POST /api/document/load
  âCheckmark† PdfDocumentService.LoadDocumentAsync()
  âCheckmark† Document loads from file system
  âCheckmark† Session created
  âCheckmark† Page metadata extracted
  âCheckmark† Returns success response
```

### Broken Path (GUI)
```
File > Open Menu Click
  âCheckmark† OnOpenFileClick() called
  âCheckmark† File dialog opens
  âCheckmark† User selects file
  âCheckmark† OpenRecentFileCommand.ExecuteAsync(filePath) called
  âCheckmark† OpenRecentFileAsync(filePath) executes
  âCheckmark† OpenFileInTabAsync(filePath) called
  ❌ FAILS HERE - No tab created
  ❌ No exception thrown to user
  ❌ Silent failure
```

## Technical Details

### File Locations
- **MainViewModel**: `src/FluentPDF.Avalonia/ViewModels/MainViewModel.cs`
  - `OpenRecentFileAsync()` - Line 171
  - `OpenFileInTabAsync()` - Line 100

- **MainWindow**: `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs`
  - `OnOpenFileClick()` - Line 373
  - `OnOpenFileClickAsync()` - Line 381

### Critical Code Section
```csharp
private async Task OpenFileInTabAsync(string filePath)
{
    // Line 124: Create PdfViewerViewModel
    var viewerViewModel = _serviceProvider.GetRequiredService<PdfViewerViewModel>();

    // Line 128: Create TabViewModel
    var tabViewModel = new TabViewModel(filePath, viewerViewModel, tabLogger);

    // Line 131: Add tab
    Tabs.Add(tabViewModel);
    ActivateTab(tabViewModel);

    // Line 137: Load document - THIS IS WHERE IT LIKELY FAILS
    await viewerViewModel.LoadDocumentFromPathAsync(filePath);
}
```

### Exception Handling
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "CRITICAL ERROR: Failed to open file in tab...");

    // Tab is removed if it fails
    if (tabViewModel != null && Tabs.Contains(tabViewModel))
    {
        Tabs.Remove(tabViewModel);
        tabViewModel.Dispose();
    }

    // Error dialog is commented out for testing
    // await ShowErrorDialogAsync("Error Opening File", ...);
}
```

## Root Cause Hypothesis

Based on code analysis, the most likely causes are:

1. **PdfViewerViewModel.LoadDocumentFromPathAsync() throws exception**
   - Exception is caught in OpenFileInTabAsync
   - Tab is removed from collection
   - No error shown to user (commented out)
   - Logs error but user sees nothing

2. **Dependency injection failure**
   - `_serviceProvider.GetRequiredService<PdfViewerViewModel>()` might throw
   - Services not registered correctly in GUI mode
   - Works in API mode because different service scope

3. **File path format issue**
   - Avalonia IStorageProvider returns different path format
   - Path might need normalization
   - Windows path vs URI path mismatch

## Diagnostic Steps Taken

1. ✅ Verified REST API works (document load + render)
2. ✅ Verified GUI launches and shows window
3. ✅ Verified API server starts with GUI
4. ✅ Created GUI state inspection endpoints
5. ✅ Confirmed file dialog opens and returns path
6. ✅ Confirmed OpenRecentFileCommand is called
7. ❌ Document does not load in GUI
8. ❌ No tab is created
9. ❌ No error shown to user

## Recommended Fixes

### Immediate Fix: Enable Error Dialogs
```csharp
// In MainViewModel.OpenFileInTabAsync(), line 158-160
// UNCOMMENT THIS:
await ShowErrorDialogAsync("Error Opening File",
    $"Failed to open {Path.GetFileName(filePath)}:\n{ex.Message}");
```

### Debug Logging
Add verbose logging at each step:
```csharp
_logger.LogInformation("Step 1: Creating ViewerViewModel...");
var viewerViewModel = _serviceProvider.GetRequiredService<PdfViewerViewModel>();

_logger.LogInformation("Step 2: Creating TabViewModel...");
var tabViewModel = new TabViewModel(filePath, viewerViewModel, tabLogger);

_logger.LogInformation("Step 3: Adding tab to collection...");
Tabs.Add(tabViewModel);

_logger.LogInformation("Step 4: Loading document from path: {Path}", filePath);
await viewerViewModel.LoadDocumentFromPathAsync(filePath);

_logger.LogInformation("Step 5: Document loaded successfully");
```

### Path Normalization
```csharp
// Before loading, normalize the path
filePath = Path.GetFullPath(filePath);
_logger.LogInformation("Normalized path: {Path}", filePath);
```

### Check Logs
Look for errors in:
- Serilog logs (if configured to write to file)
- Windows Event Viewer > Application
- Console output (if running from terminal)

## API Extensions Created

To debug this issue, I created new REST API endpoints:

### GUI State Endpoints
- `GET /api/gui/state` - Overall GUI state (tabs, active tab)
- `GET /api/gui/viewer/state` - Active viewer state (document, page, image)
- `GET /api/gui/verify/document-loaded` - Verify document is loaded
- `GET /api/gui/verify/page-rendered` - Verify page is rendered
- `POST /api/gui/action/open-file` - Trigger GUI file open programmatically

### Test Script
- `tools/test-e2e-gui.ps1` - Comprehensive E2E test script
- Tests both API and GUI loading
- Verifies rendering and document state
- Provides detailed pass/fail output

## Next Steps

1. **Enable error dialogs** to see actual exception messages
2. **Add verbose logging** throughout the loading pipeline
3. **Check Serilog output** for existing error logs
4. **Test with debugger** attached to catch exceptions in real-time
5. **Verify service registration** - ensure all dependencies are registered
6. **Test file path** - print the exact path being passed to loader

## Files Modified

- `src/FluentPDF.Avalonia/Api/Endpoints/GuiStateEndpoints.cs` (NEW)
- `src/FluentPDF.Avalonia/Api/VerificationApiServer.cs` (MODIFIED)
- `tools/test-e2e-gui.ps1` (NEW)

## How to Reproduce

1. Run: `FluentPDF.Avalonia.exe --api-server`
2. Click File > Open
3. Select any PDF file
4. **Expected**: PDF opens in new tab
5. **Actual**: Nothing happens, no error shown

## How to Test

```powershell
# Run E2E test
cd C:\Users\ryosu\repos\FluentPDF
pwsh tools/test-e2e-gui.ps1

# Expected output:
#   Test 1 (API): [OK] - Proves rendering works
#   Test 2 (GUI): [FAIL] - Proves GUI loading broken
#   Test 3 (Verify): [FAIL] - No active viewer
```
