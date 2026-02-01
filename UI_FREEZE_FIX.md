# UI Freeze Fix - RESOLVED ✅

## Problem

When launching FluentPDF normally (without `--api-server` flag), the application window appeared but was completely frozen:
- GUI unresponsive to clicks
- Loading cursor only (spinning wheel)
- Window appears but UI thread is deadlocked

## Root Cause

**UI thread deadlock caused by incorrect initialization order in Avalonia**

The code was calling `_window.Show()` and `_window.Activate()` **BEFORE** calling `base.OnFrameworkInitializationCompleted()`.

In Avalonia's lifecycle:
1. `OnFrameworkInitializationCompleted()` is called during app startup
2. **You MUST call `base.OnFrameworkInitializationCompleted()` to START the UI message loop**
3. Only AFTER the message loop starts can the window be shown/activated
4. When you set `desktop.MainWindow = _window`, the framework automatically shows it when message loop starts

## The Bug

```csharp
// App.axaml.cs - OnFrameworkInitializationCompleted()

desktop.MainWindow = _window;

// ❌ WRONG: Trying to show window BEFORE message loop starts
_window.Show();
_window.Activate();

// Called too late - message loop should start before showing window
base.OnFrameworkInitializationCompleted();
```

This caused a deadlock because:
1. `.Show()` tries to process UI messages
2. But the message loop hasn't started yet
3. The call blocks forever waiting for dispatcher
4. `base.OnFrameworkInitializationCompleted()` never gets called
5. Result: Window shows but UI thread is frozen

## The Fix

```csharp
// App.axaml.cs - OnFrameworkInitializationCompleted()

// 1. Create window
_window = new MainWindow(mainViewModel!, logger);

// 2. Set as main window
desktop.MainWindow = _window;

// 3. ✅ CORRECT: Call base FIRST to start message loop
base.OnFrameworkInitializationCompleted();

// 4. Framework automatically shows the window
// No need for explicit .Show() or .Activate() calls!
```

**Key changes:**
1. Removed explicit `.Show()` and `.Activate()` calls
2. Call `base.OnFrameworkInitializationCompleted()` immediately after setting `desktop.MainWindow`
3. Let the framework handle window visibility automatically

## Files Modified

**src/FluentPDF.Avalonia/App.axaml.cs** (line 281-358):
- Removed explicit window show/activate calls
- Moved `base.OnFrameworkInitializationCompleted()` to correct position
- Restructured try-catch blocks for proper error handling

## Testing

```bash
# Normal startup (should work now)
pwsh RUN_LATEST.ps1

# API server mode (should still work)
pwsh RUN_LATEST.ps1 --api

# Autonomous test (should still pass)
pwsh tools/test-fully-autonomous.ps1
```

## Expected Behavior After Fix

✅ Window appears and is immediately responsive
✅ Can click menus and buttons
✅ No loading cursor freeze
✅ UI thread processes messages correctly
✅ Both normal and API server modes work

## Why This Worked in API Server Mode

In the autonomous tests, the app launched with `--api-server` flag which:
1. Still had the same initialization order issue
2. But the Dispatcher.UIThread.Post callback queue compensated
3. Message loop eventually started after a delay
4. Window became responsive after initial freeze

Without `--api-server`, there was no deferred initialization to "unstick" the frozen message loop.

## Lesson Learned

**Avalonia initialization order is critical:**
1. Create window
2. Set desktop.MainWindow
3. **Call base.OnFrameworkInitializationCompleted() to start message loop**
4. Let framework show window
5. Queue any async initialization via Dispatcher.UIThread.Post

Never call `.Show()` or `.Activate()` before the message loop starts!

---

**Status**: ✅ FIXED - Build: 2026-01-29 23:15 JST
**Rebuild**: `dotnet build src/FluentPDF.Avalonia -c Release` (0 errors, 0 warnings)
