# PDFium Threading Model Constraints

This document explains the critical threading constraints when using PDFium in .NET 9.0 WinUI 3 applications, including the **Task.Yield() workaround** and why `Task.Run()` causes crashes.

## Table of Contents

- [The Problem: AccessViolation with Task.Run](#the-problem-accessviolation-with-taskrun)
- [The Solution: Task.Yield() Pattern](#the-solution-taskyield-pattern)
- [Why This Happens](#why-this-happens)
- [Implementation Guide](#implementation-guide)
- [Do's and Don'ts](#dos-and-donts)
- [Testing Threading Safety](#testing-threading-safety)

---

## The Problem: AccessViolation with Task.Run

### Crash Scenario

PDFium native library calls **cannot** be executed from `Task.Run` thread pool threads in .NET 9.0 WinUI 3 self-contained deployments.

```csharp
// ❌ THIS WILL CRASH:
public async Task<List<string>> ExtractBookmarksAsync(PdfDocument document)
{
    return await Task.Run(() =>
    {
        var handle = (SafePdfDocumentHandle)document.Handle;

        // CRASH: AccessViolation (0xC0000005)
        var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);

        // Application terminates immediately - no exception caught
        return new List<string>();
    });
}
```

### What Happens

1. `Task.Run()` schedules work on **thread pool thread**
2. PDFium P/Invoke call executes on **thread pool thread**
3. **AccessViolation exception** (0xC0000005) occurs
4. Application **terminates immediately** (no exception handling)

### Exit Code

```
Process terminated with code 0xC0000005 (ACCESS_VIOLATION)
```

This is a **fatal error** - the CLR cannot recover, and `try/catch` doesn't help.

---

## The Solution: Task.Yield() Pattern

### Safe Async Pattern

Use `Task.Yield()` to provide async behavior **without switching threads**.

```csharp
// ✅ THIS IS SAFE:
public async Task<List<string>> ExtractBookmarksAsync(PdfDocument document)
{
    // Yield to allow async behavior without thread switch
    await Task.Yield();

    var handle = (SafePdfDocumentHandle)document.Handle;

    // Safe: PDFium call executes on calling thread
    var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);
    var bookmarks = new List<string>();

    while (bookmark != IntPtr.Zero)
    {
        bookmarks.Add(PdfiumInterop.GetBookmarkTitle(bookmark));
        bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
    }

    return bookmarks;
}
```

### How Task.Yield() Works

```csharp
await Task.Yield();
```

1. **Yields control** back to the scheduler
2. **Queues continuation** to run later on the **same thread context**
3. **Does NOT switch** to thread pool
4. **Allows UI responsiveness** while keeping code on calling thread

### Comparison

| Approach | Thread Switch | PDFium Safe? | UI Responsive? |
|----------|---------------|--------------|----------------|
| Synchronous (no await) | No | ✅ Yes | ❌ No (blocks UI) |
| `Task.Run()` | Yes (thread pool) | ❌ No (crashes) | ✅ Yes |
| `Task.Yield()` | No (same thread) | ✅ Yes | ✅ Yes |

**Winner:** `Task.Yield()` - both safe and responsive.

---

## Why This Happens

### Root Cause Analysis

The exact root cause is not fully documented, but likely involves:

1. **Native thread-local storage (TLS)**: PDFium may use TLS for state
2. **Thread affinity**: Some native resources tied to specific threads
3. **.NET 9.0 self-contained deployment**: Different runtime hosting model
4. **WinUI 3 threading**: Specific thread requirements for UI interop

### Reproduction Conditions

This issue occurs specifically with:
- ✅ .NET 9.0 (or later)
- ✅ WinUI 3 applications
- ✅ Self-contained deployment
- ✅ `Task.Run()` or thread pool threads
- ✅ PDFium P/Invoke calls

### Why Task.Yield() Works

`Task.Yield()` provides:
- **Async continuation** on the **same SynchronizationContext**
- **No thread pool scheduling** (critical for PDFium safety)
- **UI thread message pump interaction** (allows responsiveness)

When called from UI thread:
```
UI Thread → await Task.Yield() → UI Thread (after yield)
```

When called from background thread:
```
Thread X → await Task.Yield() → Thread X (after yield)
```

---

## Implementation Guide

### Base Class Pattern

Create a base class for all PDFium-calling services:

```csharp
/// <summary>
/// Base class for services that interact with PDFium.
/// Ensures thread-safe PDFium operations using Task.Yield().
/// </summary>
public abstract class PdfiumServiceBase
{
    /// <summary>
    /// Executes a PDFium operation asynchronously without switching threads.
    /// </summary>
    /// <remarks>
    /// Uses Task.Yield() to provide async behavior while keeping PDFium calls
    /// on the calling thread to prevent AccessViolation crashes.
    /// </remarks>
    protected static async Task<T> ExecutePdfiumOperationAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Yield to provide async behavior without thread switching
        await Task.Yield();

        return operation();
    }

    /// <summary>
    /// Executes a PDFium operation asynchronously without switching threads.
    /// Overload for operations that don't return a value.
    /// </summary>
    protected static async Task ExecutePdfiumOperationAsync(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await Task.Yield();

        operation();
    }
}
```

### Service Implementation

Inherit from `PdfiumServiceBase` and use the helper:

```csharp
public class BookmarkService : PdfiumServiceBase
{
    /// <summary>
    /// Extracts bookmarks from a PDF document asynchronously.
    /// </summary>
    public async Task<Result<List<BookmarkNode>>> ExtractBookmarksAsync(
        PdfDocument document)
    {
        return await ExecutePdfiumOperationAsync(() =>
        {
            var handle = (SafePdfDocumentHandle)document.Handle;
            if (handle.IsInvalid)
            {
                return Result.Fail<List<BookmarkNode>>("Invalid document handle");
            }

            var bookmarks = new List<BookmarkNode>();
            var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);

            while (bookmark != IntPtr.Zero)
            {
                var title = PdfiumInterop.GetBookmarkTitle(bookmark);
                var dest = PdfiumInterop.GetBookmarkDest(handle, bookmark);

                bookmarks.Add(new BookmarkNode
                {
                    Title = title,
                    Destination = dest
                });

                bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
            }

            return Result.Ok(bookmarks);
        });
    }
}
```

### Direct Pattern (Without Base Class)

If you cannot inherit from base class:

```csharp
public async Task<List<string>> GetPageTextAsync(SafePdfPageHandle page)
{
    // Explicit Task.Yield() before PDFium calls
    await Task.Yield();

    var textPage = PdfiumInterop.LoadTextPage(page);
    try
    {
        var charCount = PdfiumInterop.CountChars(textPage);
        var lines = new List<string>();

        for (int i = 0; i < charCount; i++)
        {
            var c = PdfiumInterop.GetChar(textPage, i);
            // Process character...
        }

        return lines;
    }
    finally
    {
        PdfiumInterop.CloseTextPage(textPage);
    }
}
```

---

## Do's and Don'ts

### ✅ DO

**1. Inherit from PdfiumServiceBase**
```csharp
public class MyPdfService : PdfiumServiceBase
{
    public async Task ProcessAsync()
    {
        return await ExecutePdfiumOperationAsync(() =>
        {
            // PDFium calls here
        });
    }
}
```

**2. Use Task.Yield() for async behavior**
```csharp
public async Task RenderAsync()
{
    await Task.Yield();
    var bitmap = PdfiumInterop.CreateBitmap(...);
    // Safe: on calling thread
}
```

**3. Use sequential loops for batch operations**
```csharp
public async Task<List<byte[]>> RenderAllPagesAsync(PdfDocument doc)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var results = new List<byte[]>();
        for (int i = 0; i < doc.PageCount; i++)
        {
            // Sequential rendering - safe
            var bitmap = RenderPage(doc, i);
            results.Add(bitmap);
        }
        return results;
    });
}
```

**4. Add code comments explaining threading**
```csharp
/// <summary>
/// Renders PDF page asynchronously.
/// </summary>
/// <remarks>
/// Uses Task.Yield() instead of Task.Run() because PDFium calls
/// must execute on the calling thread to prevent AccessViolation.
/// </remarks>
public async Task<byte[]> RenderPageAsync(...)
{
    await Task.Yield();
    // ...
}
```

### ❌ DON'T

**1. Don't use Task.Run for PDFium operations**
```csharp
// ❌ WRONG - WILL CRASH:
public async Task ProcessAsync()
{
    return await Task.Run(() =>
    {
        var result = PdfiumInterop.SomeFunction(...);  // CRASH!
        return result;
    });
}
```

**2. Don't use Parallel.For or PLINQ**
```csharp
// ❌ WRONG - WILL CRASH:
Parallel.For(0, pageCount, i =>
{
    var page = PdfiumInterop.LoadPage(doc, i);  // CRASH!
});

// ❌ WRONG - WILL CRASH:
var pages = Enumerable.Range(0, pageCount)
    .AsParallel()
    .Select(i => PdfiumInterop.LoadPage(doc, i))  // CRASH!
    .ToList();
```

**3. Don't assume thread pool is safe**
```csharp
// ❌ WRONG:
ThreadPool.QueueUserWorkItem(_ =>
{
    var doc = PdfiumInterop.LoadDocument(...);  // CRASH!
});
```

**4. Don't use Task.Factory.StartNew with default options**
```csharp
// ❌ WRONG:
await Task.Factory.StartNew(() =>
{
    var page = PdfiumInterop.LoadPage(...);  // CRASH!
});
```

**5. Don't nest PDFium operations in background tasks**
```csharp
// ❌ WRONG:
public async Task ProcessAsync()
{
    var data = await GetDataAsync();  // Switches to thread pool

    // If this runs on thread pool thread: CRASH!
    var doc = PdfiumInterop.LoadDocument(...);
}
```

---

## Testing Threading Safety

### Unit Test: Verify Task.Yield() Safety

```csharp
[Fact]
public async Task PdfiumOperation_WithTaskYield_DoesNotCrash()
{
    // Arrange
    var document = LoadTestDocument();

    // Act - should not crash
    var bookmarks = await ExtractBookmarksAsync(document);

    // Assert
    Assert.NotNull(bookmarks);
}

private async Task<List<string>> ExtractBookmarksAsync(PdfDocument doc)
{
    await Task.Yield();  // Safe pattern

    var bookmarks = new List<string>();
    var bookmark = PdfiumInterop.GetFirstBookmark(doc.Handle, IntPtr.Zero);

    while (bookmark != IntPtr.Zero)
    {
        bookmarks.Add(PdfiumInterop.GetBookmarkTitle(bookmark));
        bookmark = PdfiumInterop.GetNextBookmark(doc.Handle, bookmark);
    }

    return bookmarks;
}
```

### Integration Test: Detect Task.Run Crash

**⚠️ Warning:** This test must run in **isolated process** to prevent crashing test runner.

```csharp
[Fact]
public void PdfiumOperation_WithTaskRun_CrashesInIsolatedProcess()
{
    // Run in separate process to detect crash without killing test runner
    var startInfo = new ProcessStartInfo
    {
        FileName = "FluentPDF.App.exe",
        Arguments = "--test-threading-crash",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var process = Process.Start(startInfo);
    process.WaitForExit(timeout: 5000);

    // Assert: Process crashed with AccessViolation
    Assert.Equal(unchecked((int)0xC0000005), process.ExitCode);
}
```

### Stress Test: Concurrent Operations with Task.Yield()

```csharp
[Fact]
public async Task PdfiumOperations_ConcurrentWithTaskYield_AllSucceed()
{
    var document = LoadTestDocument();
    var tasks = new List<Task<List<string>>>();

    // Create 100 concurrent operations
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(ExtractBookmarksAsync(document));
    }

    // All should succeed without crashes
    var results = await Task.WhenAll(tasks);

    Assert.Equal(100, results.Length);
    Assert.All(results, r => Assert.NotNull(r));
}
```

### Automated Validation

```bash
# Run threading model validator
FluentPDF.App.exe --validate-threading-model

# Output:
# ✓ Task.Yield() prevents crashes
# ✓ Sequential operations succeed
# ✓ Concurrent operations with Task.Yield() succeed
# ✗ Task.Run() causes AccessViolation (expected in isolated process)
# Result: Threading model validation passed
```

---

## Performance Considerations

### Is Task.Yield() Slower?

**No.** `Task.Yield()` has minimal overhead:

```csharp
// Benchmark results (approximate):
// Task.Yield():      ~5 microseconds
// Task.Run() spawn:  ~50 microseconds
// Context switch:    ~1-10 microseconds
```

For typical PDFium operations (milliseconds to seconds), the yield overhead is **negligible**.

### UI Responsiveness

`Task.Yield()` **maintains UI responsiveness** by yielding to the message pump:

```csharp
// UI remains responsive during long operation
public async Task<List<byte[]>> RenderAllPagesAsync(int pageCount)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var results = new List<byte[]>();
        for (int i = 0; i < pageCount; i++)
        {
            // Long-running operation
            var bitmap = RenderPage(i);
            results.Add(bitmap);

            // Periodically yield to allow UI updates
            if (i % 10 == 0)
            {
                await Task.Yield();
            }
        }
        return results;
    });
}
```

---

## Summary

| Aspect | Guideline |
|--------|-----------|
| **Safe pattern** | `await Task.Yield()` before PDFium calls |
| **Unsafe pattern** | `await Task.Run()` wrapping PDFium calls |
| **Base class** | Inherit from `PdfiumServiceBase` |
| **Helper method** | Use `ExecutePdfiumOperationAsync<T>()` |
| **Batch operations** | Use sequential `for`, not `Parallel.For` |
| **Crash exit code** | `0xC0000005` (AccessViolation) |
| **Testing** | Run crash tests in isolated process |
| **Performance** | Task.Yield() overhead is negligible |

### Quick Reference

```csharp
// ❌ WRONG:
await Task.Run(() => PdfiumInterop.Function(...));

// ✅ CORRECT:
await Task.Yield();
PdfiumInterop.Function(...);

// ✅ BEST:
await ExecutePdfiumOperationAsync(() =>
{
    PdfiumInterop.Function(...);
});
```

**Remember:** PDFium calls must stay on the calling thread. Use `Task.Yield()`, never `Task.Run()`.
