# UI Thread Deadlock Analysis

## Problem Summary

**Symptom**: Avalonia UI thread dispatcher is not processing messages when app runs with `--api-server` flag.

**Evidence**:
```
[REST API] DIAGNOSTIC: Testing if UI thread dispatcher is alive...
[REST API] CRITICAL: UI thread dispatcher is NOT processing messages! Dispatcher may be dead or blocked.
```

## Root Cause Analysis

### 1. Async Void Method Issue

**File**: `src/FluentPDF.Avalonia/App.axaml.cs:194`

```csharp
public override async void OnFrameworkInitializationCompleted()
```

**Problem**: `async void` methods are fire-and-forget. The caller doesn't wait for completion.

### 2. Execution Flow

1. `Program.Main()` calls `StartWithClassicDesktopLifetime(args)` (blocking)
2. Avalonia calls `OnFrameworkInitializationCompleted()` (async void - fire-and-forget)
3. Method starts executing async operations:
   - `await _host.StartAsync()` - Starts DI container
   - `await _apiServer.StartAsync()` - Starts Kestrel HTTP server
   - Creates MainWindow
   - Calls `base.OnFrameworkInitializationCompleted()`
4. Avalonia enters UI message loop
5. **RACE CONDITION**: UI message loop might start BEFORE async init completes

### 3. Potential Deadlock Scenarios

**Scenario A**: ASP.NET Core thread pool exhaustion
- Kestrel server starts and uses thread pool
- UI thread tries to dispatch work to thread pool
- All thread pool threads are busy waiting for UI thread
- **DEADLOCK**

**Scenario B**: Premature dispatcher access
- Async initialization tries to use `Dispatcher.UIThread` before message loop starts
- Dispatched work waits for message loop
- Message loop waits for initialization to signal completion
- **DEADLOCK**

### 4. Evidence from Logs

```
[13:38:43.109] [Info] [Watchdog] OperationWatchdog initialized
[13:38:43.109] [Info] [MainViewModel] MainViewModel initialized
# APP IS RUNNING - API server responds to health checks

[13:38:45.024] [Info] [GuiStateEndpoints] [REST API] Received open-file request
[13:38:45.024] [Info] [GuiStateEndpoints] [REST API] DIAGNOSTIC: Testing if UI thread dispatcher is alive...

# Dispatcher.UIThread.Post() is called but callback NEVER executes

[13:38:46.035] [Error] [GuiStateEndpoints] [REST API] CRITICAL: UI thread dispatcher is NOT processing messages!
```

**Key Findings**:
- App process is running ✓
- API server is responding ✓
- MainViewModel is initialized ✓
- Dispatcher exists ✓
- **Dispatcher message queue is NOT being processed** ❌

## Proposed Solutions

### Solution 1: Synchronous Initialization (Recommended)

Remove `async` from `OnFrameworkInitializationCompleted` and run async operations synchronously:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    try
    {
        // Synchronous initialization
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => { /* ... */ })
            .Build();

        _host.StartAsync().GetAwaiter().GetResult(); // Sync wait

        if (CommandLineOptions.Current?.ApiServer == true)
        {
            _apiServer.StartAsync(port, "localhost")
                .GetAwaiter().GetResult(); // Sync wait
        }

        // ... rest of initialization

        base.OnFrameworkInitializationCompleted();
    }
    catch (Exception ex)
    {
        // Error handling
    }
}
```

**Pros**:
- Ensures UI message loop starts AFTER full initialization
- No race conditions
- Dispatcher ready when first message arrives

**Cons**:
- Blocks UI thread during startup (acceptable for initialization)

### Solution 2: Post-Initialization API Server Start

Start API server AFTER UI message loop is running:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Synchronous init without API server
    _host.StartAsync().GetAwaiter().GetResult();

    // Create UI
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        _window = new MainWindow(...);
        desktop.MainWindow = _window;
        _window.Show();
    }

    base.OnFrameworkInitializationCompleted();

    // Start API server AFTER base call (message loop is now running)
    if (CommandLineOptions.Current?.ApiServer == true)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await _apiServer.StartAsync(port, "localhost");
        });
    }
}
```

**Pros**:
- Dispatcher is running before API server starts
- No deadlock risk

**Cons**:
- Small delay before API server is available

### Solution 3: Dedicated API Server Thread

Run API server on dedicated thread, completely independent of UI:

**Pros**:
- Complete thread isolation
- No UI thread dependency

**Cons**:
- More complex
- Still need mechanism to safely access MainViewModel

## Recommended Action

**Implement Solution 1**: Make initialization synchronous.

This is the cleanest approach and ensures the UI thread message loop is ready before any REST API requests arrive.

## Testing Plan

After fix:
1. Run `pwsh tools/test-fully-autonomous.ps1`
2. Verify diagnostic message: `[REST API] DIAGNOSTIC: UI thread dispatcher IS WORKING!`
3. Verify file load succeeds with 8-step logging from MainViewModel
4. Verify watchdog shows operation completion

