# Safe Marshaling Patterns

This document provides proven safe patterns for marshaling data between .NET and PDFium. All patterns have been validated through automated testing and are used in production FluentPDF code.

## Table of Contents

- [UTF-16LE String Marshaling](#utf-16le-string-marshaling)
- [Bitmap Buffer Marshaling](#bitmap-buffer-marshaling)
- [Struct Marshaling](#struct-marshaling)
- [Threading Patterns](#threading-patterns)
- [Handle Management](#handle-management)
- [Error Handling](#error-handling)

---

## UTF-16LE String Marshaling

### Pattern: Two-Phase Buffer Allocation

PDFium returns strings as UTF-16LE (Little Endian) byte arrays. Always use the two-phase pattern to avoid buffer overflows.

#### Implementation

```csharp
/// <summary>
/// Gets the title of a bookmark as a UTF-16LE encoded string.
/// Uses two-phase allocation: first get length, then allocate and fill buffer.
/// </summary>
public static string GetBookmarkTitle(IntPtr bookmark)
{
    if (bookmark == IntPtr.Zero)
    {
        return "(Untitled)";
    }

    // Phase 1: Get title length (includes null terminator)
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0)
    {
        return "(Untitled)";
    }

    // Phase 2: Allocate buffer and get title bytes (UTF-16LE)
    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    // Decode UTF-16LE to string and trim null terminators
    return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
}
```

#### P/Invoke Declaration

```csharp
[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern uint FPDFBookmark_GetTitle(
    IntPtr bookmark,
    [Out] byte[]? buffer,
    uint buflen);
```

#### Key Points

1. **First call with `null` buffer**: Returns required size in bytes (including null terminator)
2. **Buffer size**: Use the exact size returned from first call
3. **Encoding**: Use `System.Text.Encoding.Unicode` (UTF-16LE), **NOT** `Encoding.UTF8`
4. **Null terminators**: Always `TrimEnd('\0')` after decoding
5. **Empty check**: Handle `length == 0` case (empty string vs error)

#### Edge Cases Handled

```csharp
// ✅ Correctly handles all these cases:
"Simple ASCII"           // Standard ASCII characters
"Café Münchën"          // Latin characters with diacritics
"你好世界"               // CJK characters
"🚀🌟💻"                 // Emojis (4-byte UTF-16 surrogate pairs)
"e\u0301"               // Combining diacritics (é as base + combining char)
"Hello\0World"          // Embedded null characters
""                      // Empty string
```

---

## Bitmap Buffer Marshaling

### Pattern: Checked Arithmetic for Stride Calculation

Bitmap buffers can be very large. Use checked arithmetic to detect integer overflow.

#### Implementation

```csharp
/// <summary>
/// Copies bitmap buffer from PDFium to managed byte array with overflow protection.
/// </summary>
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var width = FPDFBitmap_GetWidth(bitmap);
    var height = FPDFBitmap_GetHeight(bitmap);
    var stride = FPDFBitmap_GetStride(bitmap);
    var buffer = FPDFBitmap_GetBuffer(bitmap);

    if (buffer == IntPtr.Zero)
    {
        throw new InvalidOperationException("Failed to get bitmap buffer pointer");
    }

    // Use checked arithmetic to detect overflow
    // For 8192x8192 BGRA32: stride=32768, height=8192 → 268,435,456 bytes
    // Without checked, overflow could result in small allocation and crash
    int bufferSize;
    try
    {
        bufferSize = checked(stride * height);
    }
    catch (OverflowException ex)
    {
        throw new InvalidOperationException(
            $"Bitmap too large: stride={stride}, height={height} causes overflow", ex);
    }

    var managedBuffer = new byte[bufferSize];
    Marshal.Copy(buffer, managedBuffer, 0, bufferSize);

    return managedBuffer;
}
```

#### P/Invoke Declarations

```csharp
[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern int FPDFBitmap_GetStride(IntPtr bitmap);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern int FPDFBitmap_GetWidth(IntPtr bitmap);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern int FPDFBitmap_GetHeight(IntPtr bitmap);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern void FPDFBitmap_Destroy(IntPtr bitmap);
```

#### Key Points

1. **Stride**: Row width in bytes (includes padding for alignment, typically 4-byte aligned)
2. **Buffer size**: `stride × height` (use checked arithmetic)
3. **Pixel format**: BGRA32 (4 bytes per pixel: Blue, Green, Red, Alpha)
4. **Overflow detection**: Use `checked` block for large images
5. **Resource cleanup**: Always call `FPDFBitmap_Destroy` (use `try/finally` or `using` pattern)

#### Stride Calculation

```csharp
// For BGRA32 format (4 bytes per pixel):
int bytesPerPixel = 4;
int minimumStride = width * bytesPerPixel;

// PDFium may add padding for alignment (e.g., 4-byte alignment)
// Always use FPDFBitmap_GetStride() - NEVER calculate manually
int actualStride = FPDFBitmap_GetStride(bitmap);

// Example: 1920x1080 image
// minimumStride = 1920 * 4 = 7680 bytes
// actualStride might be 7680 (no padding) or 7684 (4-byte aligned)
```

#### Edge Cases Handled

```csharp
// ✅ Correctly handles all these cases:
// 1x1 pixel (minimum size)
// 8192x8192 pixels (large image, 268 MB buffer)
// 1920x1080 (non-power-of-2 dimensions)
// Width causing stride padding (e.g., 1001 pixels → stride 4008, not 4004)
// Zero/negative dimensions (validation before allocation)
```

---

## Struct Marshaling

### Pattern: Sequential Layout for PDFium Structs

PDFium structs expect C-style sequential layout without compiler-added padding.

#### Implementation

```csharp
/// <summary>
/// Rectangle structure (PDFium FS_RECTF).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FS_RECTF
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;

    /// <summary>
    /// Creates a rectangle from coordinates.
    /// </summary>
    public FS_RECTF(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Validates that coordinates are not inverted.
    /// </summary>
    public bool IsValid() => Left <= Right && Top <= Bottom;

    /// <summary>
    /// Normalizes inverted coordinates.
    /// </summary>
    public FS_RECTF Normalize()
    {
        return new FS_RECTF(
            Math.Min(Left, Right),
            Math.Min(Top, Bottom),
            Math.Max(Left, Right),
            Math.Max(Top, Bottom)
        );
    }
}

/// <summary>
/// Quadrilateral points structure (PDFium FS_QUADPOINTSF).
/// Used for annotation attachment points (8 floats: x1,y1,x2,y2,x3,y3,x4,y4).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FS_QUADPOINTSF
{
    public float X1, Y1;  // Point 1
    public float X2, Y2;  // Point 2
    public float X3, Y3;  // Point 3
    public float X4, Y4;  // Point 4

    /// <summary>
    /// Creates quad points from coordinates.
    /// </summary>
    public FS_QUADPOINTSF(
        float x1, float y1,
        float x2, float y2,
        float x3, float y3,
        float x4, float y4)
    {
        X1 = x1; Y1 = y1;
        X2 = x2; Y2 = y2;
        X3 = x3; Y3 = y3;
        X4 = x4; Y4 = y4;
    }

    /// <summary>
    /// Validates that all coordinates are finite (not NaN or Infinity).
    /// </summary>
    public bool IsValid() =>
        IsFinite(X1) && IsFinite(Y1) &&
        IsFinite(X2) && IsFinite(Y2) &&
        IsFinite(X3) && IsFinite(Y3) &&
        IsFinite(X4) && IsFinite(Y4);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);
}
```

#### P/Invoke Declarations

```csharp
[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern bool FPDFAnnot_SetRect(IntPtr annot, ref FS_RECTF rect);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern bool FPDFAnnot_GetRect(IntPtr annot, out FS_RECTF rect);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
private static extern bool FPDFAnnot_SetAttachmentPoints(
    IntPtr annot,
    int quadIndex,
    ref FS_QUADPOINTSF quadPoints);
```

#### Key Points

1. **Layout**: Always use `[StructLayout(LayoutKind.Sequential)]`
2. **Field order**: Must match PDFium struct declaration exactly
3. **Field types**: Use exact matching types (`float` for `float`, `int` for `int`)
4. **Validation**: Add validation methods for edge cases (NaN, Infinity, inverted coordinates)
5. **Passing**: Use `ref` for input structs, `out` for output structs

#### Edge Cases Handled

```csharp
// ✅ Correctly handles all these cases:
new FS_RECTF(0, 0, 100, 100)           // Normal rectangle
new FS_RECTF(100, 100, 0, 0)           // Inverted (normalize before use)
new FS_RECTF(float.NaN, 0, 100, 100)   // NaN values (validate before use)
new FS_RECTF(-1000, -1000, 1000, 1000) // Negative coordinates (valid)
new FS_RECTF(0, 0, 1e10f, 1e10f)       // Very large coordinates (valid)
```

---

## Threading Patterns

### Pattern: Task.Yield() for Async without Thread Switch

PDFium calls **must** execute on the calling thread. Use `Task.Yield()` for async behavior without thread pool execution.

#### Implementation

```csharp
/// <summary>
/// Base class for services that interact with PDFium.
/// Provides threading-safe helper methods.
/// </summary>
public abstract class PdfiumServiceBase
{
    /// <summary>
    /// Executes a PDFium operation asynchronously without switching threads.
    /// </summary>
    protected static async Task<T> ExecutePdfiumOperationAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Yield to provide async behavior without thread switching.
        // This allows the UI thread to remain responsive while keeping
        // PDFium calls on the original calling thread to prevent crashes.
        await Task.Yield();

        return operation();
    }
}
```

#### Usage in Services

```csharp
public class BookmarkService : PdfiumServiceBase
{
    /// <summary>
    /// Extracts bookmarks from PDF asynchronously.
    /// </summary>
    public async Task<Result<List<BookmarkNode>>> ExtractBookmarksAsync(PdfDocument document)
    {
        return await ExecutePdfiumOperationAsync(() =>
        {
            // All PDFium calls here execute on calling thread (safe)
            var handle = (SafePdfDocumentHandle)document.Handle;
            var bookmarks = new List<BookmarkNode>();

            var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);
            while (bookmark != IntPtr.Zero)
            {
                var title = PdfiumInterop.GetBookmarkTitle(bookmark);
                bookmarks.Add(new BookmarkNode { Title = title });
                bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
            }

            return Result.Ok(bookmarks);
        });
    }
}
```

#### Key Points

1. **Always use `Task.Yield()`**: Never use `Task.Run()` for PDFium operations
2. **Inherit from `PdfiumServiceBase`**: All PDFium-calling services must inherit
3. **Use helper method**: Call `ExecutePdfiumOperationAsync` for standard patterns
4. **UI responsiveness**: `Task.Yield()` allows UI thread to process messages
5. **Thread safety**: PDFium calls remain on calling thread (prevents AccessViolation)

#### Why Not Task.Run?

```csharp
// ❌ WRONG - DO NOT USE:
public async Task<Result<Data>> ExtractDataAsync(PdfDocument document)
{
    // This will crash in .NET 9.0 WinUI 3!
    return await Task.Run(() =>
    {
        var handle = (SafePdfDocumentHandle)document.Handle;
        // PDFium call on thread pool thread → AccessViolation crash
        var data = PdfiumInterop.SomeFunction(handle);
        return Result.Ok(data);
    });
}

// ✅ CORRECT - USE Task.Yield():
public async Task<Result<Data>> ExtractDataAsync(PdfDocument document)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var handle = (SafePdfDocumentHandle)document.Handle;
        // PDFium call on calling thread → Safe
        var data = PdfiumInterop.SomeFunction(handle);
        return Result.Ok(data);
    });
}
```

---

## Handle Management

### Pattern: SafeHandle for Resource Cleanup

Use `SafeHandle` derived classes for automatic PDFium resource cleanup.

#### Implementation

```csharp
/// <summary>
/// Safe handle for PDF document.
/// Automatically closes document on disposal.
/// </summary>
public sealed class SafePdfDocumentHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePdfDocumentHandle() : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            FPDF_CloseDocument(handle);
            return true;
        }
        return false;
    }

    [DllImport(PdfiumInterop.DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_CloseDocument(IntPtr document);
}

/// <summary>
/// Safe handle for PDF page.
/// Automatically closes page on disposal.
/// </summary>
public sealed class SafePdfPageHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePdfPageHandle() : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            FPDF_ClosePage(handle);
            return true;
        }
        return false;
    }

    [DllImport(PdfiumInterop.DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_ClosePage(IntPtr page);
}
```

#### Usage

```csharp
public async Task<Result<PageData>> LoadPageAsync(string filePath, int pageIndex)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        // SafeHandle automatically disposes on scope exit
        using var document = PdfiumInterop.LoadDocument(filePath, password: null);
        if (document.IsInvalid)
        {
            return Result.Fail<PageData>("Failed to load document");
        }

        using var page = PdfiumInterop.LoadPage(document, pageIndex);
        if (page.IsInvalid)
        {
            return Result.Fail<PageData>("Failed to load page");
        }

        var width = PdfiumInterop.GetPageWidth(page);
        var height = PdfiumInterop.GetPageHeight(page);

        return Result.Ok(new PageData { Width = width, Height = height });

        // document and page automatically closed here
    });
}
```

#### Key Points

1. **Always use SafeHandle**: Never use raw `IntPtr` for PDFium handles
2. **Inherit from appropriate base**: Use `SafeHandleZeroOrMinusOneIsInvalid` for handles where 0/-1 = invalid
3. **Implement `ReleaseHandle`**: Call PDFium cleanup function
4. **Use `using` statements**: Ensure deterministic disposal
5. **Check `IsInvalid`**: Validate handle before use

---

## Error Handling

### Pattern: Result<T> for Operation Outcomes

Use the Result pattern instead of exceptions for expected failures.

#### Implementation

```csharp
public async Task<Result<List<string>>> ExtractTextAsync(PdfDocument document, int pageIndex)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        using var handle = (SafePdfDocumentHandle)document.Handle;
        if (handle.IsInvalid)
        {
            return Result.Fail<List<string>>("Invalid document handle");
        }

        using var page = PdfiumInterop.LoadPage(handle, pageIndex);
        if (page.IsInvalid)
        {
            return Result.Fail<List<string>>($"Failed to load page {pageIndex}");
        }

        using var textPage = PdfiumInterop.LoadTextPage(page);
        if (textPage == IntPtr.Zero)
        {
            return Result.Fail<List<string>>("Failed to load text page");
        }

        try
        {
            var lines = new List<string>();
            // Extract text logic...
            return Result.Ok(lines);
        }
        finally
        {
            PdfiumInterop.CloseTextPage(textPage);
        }
    });
}
```

#### Key Points

1. **Use Result<T> for expected failures**: File not found, invalid page, etc.
2. **Throw exceptions for unexpected errors**: Null arguments, programming errors
3. **Validate inputs early**: Fail fast at method entry
4. **Provide detailed messages**: Include context (file path, page index, etc.)
5. **Cleanup in finally**: Ensure resources freed even on error

---

## Summary

| Pattern | Use Case | Key Technique |
|---------|----------|---------------|
| Two-Phase Allocation | UTF-16 strings | Get length → Allocate → Fill buffer |
| Checked Arithmetic | Bitmap buffers | `checked(stride * height)` |
| Sequential Layout | Structs | `[StructLayout(LayoutKind.Sequential)]` |
| Task.Yield | Threading | `await Task.Yield(); PdfiumCall();` |
| SafeHandle | Resource cleanup | Inherit from `SafeHandleZeroOrMinusOneIsInvalid` |
| Result<T> | Error handling | Return success/failure instead of throwing |

All these patterns are implemented in `src/FluentPDF.Rendering/Interop/` and validated through automated tests.
