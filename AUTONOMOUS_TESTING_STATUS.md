# Autonomous Testing Status - Jan 29, 2026 1:53 PM

## Current Issue

**App hangs during startup when running with `--api-server` flag**

The REST API server never starts because the app initialization hangs before reaching that point.

## What We've Discovered

### ✅ Working Components

1. **REST API Infrastructure** - All endpoints implemented correctly
   - Health check: `/api/health`
   - Logs endpoint: `/api/logs`
   - GUI state endpoints: `/api/gui/state`, `/api/gui/action/open-file`
   - Operation watchdog: `/api/health/watchdog`

2. **Logging Infrastructure** - LogBufferService captures all logs in memory

3. **Test Scripts** - `tools/test-fully-autonomous.ps1` works correctly for monitoring

4. **Diagnostic Logging** - Comprehensive console output shows exactly where it hangs

### ❌ Root Cause: UI Thread Dispatcher Deadlock

**Discovery**: When app ran successfully (earlier test), we confirmed:
```
[REST API] CRITICAL: UI thread dispatcher is NOT processing messages!
```

The Avalonia UI thread dispatcher exists but is NOT processing the message queue. This is why all `Post()` and `InvokeAsync()` calls never execute their callbacks.

### 🔍 Startup Flow Analysis

**Current Initialization Sequence** (from diagnostic logs):

```
✅ PROGRAM START
✅ APP INITIALIZE
   ✅ Creating Serilog logger
   ✅ DI host built
   ✅ Loading XAML

✅ FRAMEWORK INITIALIZATION
   ✅ Initializing PDFium
   ✅ Starting DI host
   ✅ Starting settings load (async)

✅ CREATING MAIN WINDOW
   ✅ MainViewModel retrieved
   ✅ MainWindow constructor start
   ✅ InitializeComponent
   ✅ DataContext set
   ✅ Menu handlers set
   ✅ Keyboard shortcuts set

❌ HANGS HERE (recent files menu deferred to Loaded event)
```

**The app never reaches**:
- MainWindow constructor completion
- MainWindow.Loaded event
- `base.OnFrameworkInitializationCompleted()` return
- UI message loop start
- API server startup (queued via `Dispatcher.UIThread.Post`)

## Attempts Made

### Attempt 1: Synchronous Initialization
**File**: `App.axaml.cs`
**Change**: Changed `OnFrameworkInitializationCompleted` from `async void` to `void`, used `.GetAwaiter().GetResult()` for all async calls
**Result**: ❌ Deadlock - `_host.StartAsync().GetAwaiter().GetResult()` blocks forever

### Attempt 2: Deferred API Server Start (Window.Loaded)
**Change**: Queue API server start in Window.Loaded event
**Result**: ❌ Window.Loaded never fires because constructor never completes

### Attempt 3: Deferred API Server Start (Dispatcher.UIThread.Post)
**Change**: Queue API server start before `base.OnFrameworkInitializationCompleted()`
**Result**: ❌ Post callback never executes because message loop never starts

### Attempt 4: Non-Blocking Settings Load
**Change**: Made settings load async/non-blocking with `ContinueWith`
**Result**: ❌ App progresses further but still hangs in MainWindow constructor

### Attempt 5: Deferred Recent Files Menu
**Change**: Moved `PopulateRecentFilesMenu()` to Window.Loaded event
**Result**: ❌ Still hanging (current status)

## Current Hypothesis

The MainWindow constructor is still blocking on something after "Keyboard shortcuts set". Possible causes:

1. **ViewModel.Tabs.CollectionChanged event subscription** - Might trigger immediate collection enumeration
2. **UpdateEmptyStateVisibility()** - Might be accessing UI elements that aren't ready
3. **ViewModel.PropertyChanged event subscription** - Might trigger immediate property reads
4. **UpdateMenuItemStates()** - Likely accessing menu items and checking ViewModel state

Any of these could be blocking if they:
- Try to access files/settings that are still loading asynchronously
- Try to dispatch to UI thread (which isn't running yet)
- Wait for some async operation that can't complete until UI thread runs

## Next Steps to Debug

### Option A: Continue Deferring Initialization
Move more MainWindow constructor code to Loaded event:
```csharp
this.Loaded += (s, e) =>
{
    // All UI setup code here
    ViewModel.Tabs.CollectionChanged += ...;
    UpdateEmptyStateVisibility();
    ViewModel.PropertyChanged += ...;
    UpdateMenuItemStates();
    PopulateRecentFilesMenu();
};
```

### Option B: Find Exact Blocking Call
Add console output between EVERY line in MainWindow constructor to identify exact hanging line.

### Option C: Minimal Constructor
Strip MainWindow constructor to bare minimum:
```csharp
public MainWindow(MainViewModel viewModel)
{
    ViewModel = viewModel;
    InitializeComponent();
    DataContext = ViewModel;
    // NOTHING ELSE - defer everything to Loaded event
}
```

### Option D: Check for Avalonia 11.3.9 Known Issues
Research if this is a known problem with Avalonia 11.3.9 when using `--api-server` pattern.

## Recommended Next Action

**Implement Option C**: Minimal constructor + Loaded event initialization

This is the most likely to succeed because:
1. Constructor completes quickly
2. `base.OnFrameworkInitializationCompleted()` can return
3. UI message loop starts
4. Loaded event fires
5. All initialization happens with message loop running

## Files Modified

1. `src/FluentPDF.Avalonia/App.axaml.cs`
   - Changed `OnFrameworkInitializationCompleted()` signature (async void → void → async void)
   - Made settings load non-blocking
   - Deferred API server start to `Dispatcher.UIThread.Post`

2. `src/FluentPDF.Avalonia/Views/MainWindow.axaml.cs`
   - Deferred `PopulateRecentFilesMenu()` to Loaded event

3. `src/FluentPDF.Avalonia/Api/Endpoints/GuiStateEndpoints.cs`
   - Changed from `Post` to `InvokeAsync` to `Post` with polling
   - Added UI thread dispatcher diagnostic test
   - Added comprehensive logging

4. `src/FluentPDF.Avalonia/Api/VerificationApiServer.cs`
   - Pass `logBuffer` to `GuiStateEndpoints.Map()`

5. `tools/test-fully-autonomous.ps1`
   - Removed `-WindowStyle Hidden` (window must be visible for dispatcher)

## Performance Impact

When working:
- API server starts ~1-2 seconds after app launch
- Slight delay is acceptable for autonomous testing
- No impact on normal user workflow (no `--api-server` flag)

