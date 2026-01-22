# Common Marshaling Pitfalls

This document presents common mistakes when marshaling data with PDFium, showing **incorrect** and **correct** implementations side-by-side. Learn from these examples to avoid crashes, data corruption, and security vulnerabilities.

## Table of Contents

- [UTF-16 String Marshaling](#utf-16-string-marshaling)
- [Bitmap Buffer Marshaling](#bitmap-buffer-marshaling)
- [Threading Mistakes](#threading-mistakes)
- [Struct Marshaling](#struct-marshaling)
- [Handle Management](#handle-management)
- [Buffer Overflow](#buffer-overflow)

---

## UTF-16 String Marshaling

### Pitfall 1: Using UTF-8 Instead of UTF-16LE

PDFium uses UTF-16LE encoding, **not** UTF-8. Using the wrong encoding corrupts non-ASCII characters.

#### ❌ Incorrect

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    // WRONG: UTF-8 decoding of UTF-16LE data
    return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    // Result for "Café": "CafÃ©" (mojibake)
    // Result for "你好": "�ǺÃ" (completely corrupted)
}
```

#### ✅ Correct

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    // CORRECT: UTF-16LE decoding (Encoding.Unicode)
    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result for "Café": "Café" ✓
    // Result for "你好": "你好" ✓
}
```

**Key Lesson:** Always use `Encoding.Unicode` (UTF-16LE) for PDFium strings.

---

### Pitfall 2: Forgetting Null Terminator in Buffer Size

PDFium's length includes the null terminator. Allocating too small a buffer causes truncation.

#### ❌ Incorrect

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    // WRONG: Subtracting 2 bytes (null terminator size)
    var buffer = new byte[length - 2];  // Buffer too small!
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result: Last character truncated or missing
}
```

#### ✅ Correct

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    // CORRECT: Use exact length returned (includes null terminator)
    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result: Complete string with all characters ✓
}
```

**Key Lesson:** Use the exact length returned by PDFium, including null terminator space.

---

### Pitfall 3: Single-Phase Allocation (Guessing Buffer Size)

Guessing buffer size risks overflow or waste. Always use two-phase allocation.

#### ❌ Incorrect

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    // WRONG: Guessing buffer size
    var buffer = new byte[1024];  // What if title is 2000 bytes?
    FPDFBookmark_GetTitle(bookmark, buffer, 1024);

    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result: Truncated titles if longer than 512 characters (1024 bytes / 2)
}
```

#### ✅ Correct

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    // CORRECT: Two-phase allocation
    // Phase 1: Get exact required size
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    // Phase 2: Allocate exact buffer and fill
    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result: Handles any title length correctly ✓
}
```

**Key Lesson:** Always call twice: once to get size, once to get data.

---

### Pitfall 4: Not Trimming Null Terminators

UTF-16LE strings include null terminators that must be removed after decoding.

#### ❌ Incorrect

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    // WRONG: Not trimming null terminator
    return Encoding.Unicode.GetString(buffer);
    // Result: "Chapter 1\0" (visible null character in UI or comparisons)
}
```

#### ✅ Correct

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0) return "";

    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    // CORRECT: Trim null terminators
    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    // Result: "Chapter 1" (clean string) ✓
}
```

**Key Lesson:** Always `TrimEnd('\0')` after decoding UTF-16LE.

---

## Bitmap Buffer Marshaling

### Pitfall 5: Not Using Checked Arithmetic for Buffer Size

Large images can cause `stride × height` to overflow, resulting in incorrect allocation and crashes.

#### ❌ Incorrect

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var stride = FPDFBitmap_GetStride(bitmap);  // 32768 for 8192-wide image
    var height = FPDFBitmap_GetHeight(bitmap);  // 8192

    // WRONG: Unchecked arithmetic - overflow for large images!
    var bufferSize = stride * height;
    // For 8192x8192: 32768 * 8192 = 268,435,456 bytes
    // If this overflows to negative or wraps, allocation is wrong size

    var buffer = new byte[bufferSize];  // May allocate tiny buffer!
    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);
    // CRASH: Copying 268 MB into small buffer

    return buffer;
}
```

#### ✅ Correct

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var stride = FPDFBitmap_GetStride(bitmap);
    var height = FPDFBitmap_GetHeight(bitmap);

    // CORRECT: Checked arithmetic detects overflow
    int bufferSize;
    try
    {
        bufferSize = checked(stride * height);
    }
    catch (OverflowException ex)
    {
        throw new InvalidOperationException(
            $"Bitmap too large: stride={stride}, height={height}", ex);
    }

    var buffer = new byte[bufferSize];
    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);

    return buffer;
    // Result: Safe allocation or clear error ✓
}
```

**Key Lesson:** Use `checked(stride * height)` to detect overflow for large images.

---

### Pitfall 6: Calculating Stride Manually Instead of Using PDFium

Stride may include padding for alignment. Never calculate it manually.

#### ❌ Incorrect

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var width = FPDFBitmap_GetWidth(bitmap);   // 1001 pixels
    var height = FPDFBitmap_GetHeight(bitmap); // 1000 pixels

    // WRONG: Calculating stride manually
    var stride = width * 4;  // 1001 * 4 = 4004 bytes
    // Actual stride might be 4008 (4-byte aligned)

    var bufferSize = checked(stride * height);
    var buffer = new byte[bufferSize];  // Too small!

    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);
    // CRASH: Reading past buffer end

    return buffer;
}
```

#### ✅ Correct

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var width = FPDFBitmap_GetWidth(bitmap);
    var height = FPDFBitmap_GetHeight(bitmap);

    // CORRECT: Always get stride from PDFium
    var stride = FPDFBitmap_GetStride(bitmap);  // 4008 (with padding)

    var bufferSize = checked(stride * height);
    var buffer = new byte[bufferSize];

    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);

    return buffer;
    // Result: Correct buffer size including padding ✓
}
```

**Key Lesson:** Always use `FPDFBitmap_GetStride()`, never calculate stride manually.

---

### Pitfall 7: Not Disposing Bitmap Handles

PDFium bitmaps allocate unmanaged memory. Forgetting to destroy them causes memory leaks.

#### ❌ Incorrect

```csharp
public static byte[] RenderPage(SafePdfPageHandle page, int width, int height)
{
    var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

    PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 0);

    var buffer = CopyBitmapBuffer(bitmap);

    // WRONG: Not destroying bitmap - memory leak!
    return buffer;
    // Each call leaks ~width*height*4 bytes
    // Rendering 100 pages at 1920x1080 leaks ~790 MB
}
```

#### ✅ Correct

```csharp
public static byte[] RenderPage(SafePdfPageHandle page, int width, int height)
{
    var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);
    try
    {
        PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 0);

        return CopyBitmapBuffer(bitmap);
    }
    finally
    {
        // CORRECT: Always destroy bitmap
        FPDFBitmap_Destroy(bitmap);
    }
    // Result: No memory leaks ✓
}
```

**Key Lesson:** Always call `FPDFBitmap_Destroy` in `finally` block or use wrapper with `IDisposable`.

---

## Threading Mistakes

### Pitfall 8: Using Task.Run for PDFium Operations

PDFium calls on thread pool threads cause AccessViolation crashes in .NET 9.0 WinUI 3.

#### ❌ Incorrect

```csharp
public async Task<Result<List<string>>> ExtractBookmarksAsync(PdfDocument document)
{
    // WRONG: Task.Run switches to thread pool
    return await Task.Run(() =>
    {
        var handle = (SafePdfDocumentHandle)document.Handle;
        var bookmarks = new List<string>();

        // CRASH: PDFium call on thread pool thread → AccessViolation (0xC0000005)
        var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);

        while (bookmark != IntPtr.Zero)
        {
            bookmarks.Add(PdfiumInterop.GetBookmarkTitle(bookmark));
            bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
        }

        return Result.Ok(bookmarks);
    });
    // Application terminates immediately with no exception
}
```

#### ✅ Correct

```csharp
public async Task<Result<List<string>>> ExtractBookmarksAsync(PdfDocument document)
{
    // CORRECT: ExecutePdfiumOperationAsync uses Task.Yield
    return await ExecutePdfiumOperationAsync(() =>
    {
        var handle = (SafePdfDocumentHandle)document.Handle;
        var bookmarks = new List<string>();

        // Safe: PDFium call on calling thread
        var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);

        while (bookmark != IntPtr.Zero)
        {
            bookmarks.Add(PdfiumInterop.GetBookmarkTitle(bookmark));
            bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
        }

        return Result.Ok(bookmarks);
    });
    // Result: Async operation without crashes ✓
}
```

**Key Lesson:** Use `ExecutePdfiumOperationAsync` with `Task.Yield()`, **never** `Task.Run()`.

---

### Pitfall 9: Using Parallel.For for Batch Operations

Parallel execution of PDFium calls causes crashes. Always use sequential execution.

#### ❌ Incorrect

```csharp
public async Task<List<byte[]>> RenderAllPagesAsync(PdfDocument document)
{
    var pageCount = document.PageCount;
    var renderings = new byte[pageCount][];

    // WRONG: Parallel.For executes on thread pool
    Parallel.For(0, pageCount, i =>
    {
        using var page = PdfiumInterop.LoadPage(document.Handle, i);

        // CRASH: PDFium calls on multiple thread pool threads
        var bitmap = PdfiumInterop.CreateBitmap(1920, 1080, hasAlpha: true);
        try
        {
            PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, 1920, 1080, 0, 0);
            renderings[i] = CopyBitmapBuffer(bitmap);
        }
        finally
        {
            FPDFBitmap_Destroy(bitmap);
        }
    });

    return renderings.ToList();
    // Random crashes or data corruption
}
```

#### ✅ Correct

```csharp
public async Task<List<byte[]>> RenderAllPagesAsync(PdfDocument document)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var pageCount = document.PageCount;
        var renderings = new List<byte[]>();

        // CORRECT: Sequential execution on calling thread
        for (int i = 0; i < pageCount; i++)
        {
            using var page = PdfiumInterop.LoadPage(document.Handle, i);

            var bitmap = PdfiumInterop.CreateBitmap(1920, 1080, hasAlpha: true);
            try
            {
                PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, 1920, 1080, 0, 0);
                renderings.Add(CopyBitmapBuffer(bitmap));
            }
            finally
            {
                FPDFBitmap_Destroy(bitmap);
            }
        }

        return renderings;
    });
    // Result: Safe sequential rendering ✓
}
```

**Key Lesson:** Always use sequential `for` loops, never `Parallel.For` or `Task.Run`.

---

## Struct Marshaling

### Pitfall 10: Using LayoutKind.Auto for PDFium Structs

.NET may reorder fields with `LayoutKind.Auto`, breaking compatibility with C structs.

#### ❌ Incorrect

```csharp
// WRONG: LayoutKind.Auto allows field reordering
[StructLayout(LayoutKind.Auto)]
public struct FS_RECTF
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}

// PDFium expects: Left, Top, Right, Bottom (in that order)
// .NET might reorder to: Bottom, Left, Right, Top
// Result: Coordinates completely wrong, annotations in wrong places
```

#### ✅ Correct

```csharp
// CORRECT: LayoutKind.Sequential preserves field order
[StructLayout(LayoutKind.Sequential)]
public struct FS_RECTF
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}

// PDFium receives: Left, Top, Right, Bottom (correct order)
// Result: Coordinates correct ✓
```

**Key Lesson:** Always use `[StructLayout(LayoutKind.Sequential)]` for PDFium structs.

---

### Pitfall 11: Not Validating Floating-Point Values

PDFium structs may contain NaN or Infinity values from corrupted PDFs. Always validate.

#### ❌ Incorrect

```csharp
public void SetAnnotationRect(IntPtr annotation, FS_RECTF rect)
{
    // WRONG: Not validating values
    FPDFAnnot_SetRect(annotation, ref rect);
    // If rect contains NaN: undefined behavior or crash
}
```

#### ✅ Correct

```csharp
public void SetAnnotationRect(IntPtr annotation, FS_RECTF rect)
{
    // CORRECT: Validate before use
    if (!IsValidRect(rect))
    {
        throw new ArgumentException(
            $"Invalid rectangle: Left={rect.Left}, Top={rect.Top}, " +
            $"Right={rect.Right}, Bottom={rect.Bottom}");
    }

    FPDFAnnot_SetRect(annotation, ref rect);
}

private static bool IsValidRect(FS_RECTF rect)
{
    return !float.IsNaN(rect.Left) && !float.IsInfinity(rect.Left) &&
           !float.IsNaN(rect.Top) && !float.IsInfinity(rect.Top) &&
           !float.IsNaN(rect.Right) && !float.IsInfinity(rect.Right) &&
           !float.IsNaN(rect.Bottom) && !float.IsInfinity(rect.Bottom) &&
           rect.Left <= rect.Right && rect.Top <= rect.Bottom;
}
```

**Key Lesson:** Validate floating-point struct values for NaN, Infinity, and logical constraints.

---

## Handle Management

### Pitfall 12: Using Raw IntPtr Instead of SafeHandle

Raw `IntPtr` doesn't auto-cleanup, leading to resource leaks and use-after-free bugs.

#### ❌ Incorrect

```csharp
public class PdfDocument
{
    private IntPtr _handle;  // WRONG: Raw pointer

    public PdfDocument(string filePath)
    {
        _handle = FPDF_LoadDocument(filePath, null);
        if (_handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load document");
        }
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            FPDF_CloseDocument(_handle);
            _handle = IntPtr.Zero;
        }
    }
    // Problem: If exception thrown, Dispose() never called → memory leak
    // Problem: If Dispose() called twice → double-free crash
}
```

#### ✅ Correct

```csharp
public class PdfDocument : IDisposable
{
    private readonly SafePdfDocumentHandle _handle;  // CORRECT: SafeHandle

    public PdfDocument(string filePath)
    {
        _handle = FPDF_LoadDocument(filePath, null);
        if (_handle.IsInvalid)
        {
            throw new Exception("Failed to load document");
        }
    }

    public void Dispose()
    {
        _handle?.Dispose();  // SafeHandle prevents double-free
    }
    // Result: Auto-cleanup even on exception, no double-free ✓
}

public sealed class SafePdfDocumentHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePdfDocumentHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        FPDF_CloseDocument(handle);
        return true;
    }
}
```

**Key Lesson:** Always use `SafeHandle` derived classes, never raw `IntPtr` for PDFium handles.

---

## Buffer Overflow

### Pitfall 13: Not Validating Buffer Bounds in Marshal.Copy

Passing wrong size to `Marshal.Copy` causes crashes or memory corruption.

#### ❌ Incorrect

```csharp
public static byte[] GetTextBuffer(IntPtr textPage)
{
    var charCount = FPDFText_CountChars(textPage);

    // WRONG: Assuming 2 bytes per character (UTF-16), but what if API changed?
    var bufferSize = charCount * 2;
    var buffer = new byte[bufferSize];

    // If actual API uses 4 bytes per char: buffer overflow
    Marshal.Copy(FPDFText_GetBuffer(textPage), buffer, 0, bufferSize);

    return buffer;
}
```

#### ✅ Correct

```csharp
public static byte[] GetTextBuffer(IntPtr textPage)
{
    // CORRECT: Get exact buffer size from API
    var bufferSize = FPDFText_GetBufferSize(textPage);
    if (bufferSize <= 0)
    {
        return Array.Empty<byte>();
    }

    var buffer = new byte[bufferSize];

    // Validate buffer bounds
    var actualSize = FPDFText_GetBuffer(textPage, buffer, bufferSize);
    if (actualSize != bufferSize)
    {
        throw new InvalidOperationException(
            $"Buffer size mismatch: expected {bufferSize}, got {actualSize}");
    }

    return buffer;
}
```

**Key Lesson:** Always get buffer size from API, never calculate or assume it.

---

## Summary Table

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| UTF-8 instead of UTF-16LE | Mojibake (garbled text) | Use `Encoding.Unicode` |
| Missing null terminator space | Truncated strings | Use exact length from API |
| Single-phase allocation | Buffer overflow/truncation | Two-phase: get length, then allocate |
| Not trimming null terminators | Strings end with `\0` | Call `.TrimEnd('\0')` |
| Unchecked arithmetic | Crash on large images | Use `checked(stride * height)` |
| Manual stride calculation | Wrong buffer size | Use `FPDFBitmap_GetStride()` |
| Not disposing bitmaps | Memory leak | Use `try/finally` with `Destroy` |
| Task.Run for PDFium calls | AccessViolation crash | Use `Task.Yield()` |
| Parallel.For for PDFium | Random crashes | Use sequential `for` loop |
| LayoutKind.Auto | Wrong struct layout | Use `LayoutKind.Sequential` |
| Not validating floats | NaN/Infinity bugs | Validate before use |
| Raw IntPtr | Resource leaks | Use `SafeHandle` |
| Wrong Marshal.Copy size | Buffer overflow | Get size from API |

---

## Testing for These Pitfalls

Use automated validators to detect these issues:

```bash
# Validate all marshaling patterns
FluentPDF.App.exe --validate-all

# Specific validators
FluentPDF.App.exe --validate-utf16-marshalling
FluentPDF.App.exe --validate-bitmap-marshalling
FluentPDF.App.exe --validate-threading-model
FluentPDF.App.exe --validate-buffer-safety
```

See [safe-patterns.md](./safe-patterns.md) for correct implementations of all these patterns.
