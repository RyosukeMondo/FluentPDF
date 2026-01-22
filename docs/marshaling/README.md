# PDFium Marshaling Guide

## Overview

This documentation provides comprehensive guidance for safely marshaling data between .NET and the PDFium native library. PDFium is a C/C++ library, and interacting with it from C# requires careful handling of memory, strings, buffers, and threading to avoid crashes, data corruption, and security vulnerabilities.

**Critical Areas Covered:**
- UTF-16LE string encoding and two-phase buffer allocation
- Bitmap buffer marshaling with stride calculations and overflow prevention
- Annotation geometry marshaling (structs with floating-point data)
- Threading model constraints (Task.Yield vs Task.Run)
- Buffer safety and overflow prevention
- Known workarounds for PDFium bugs

## Quick Start

### 1. Inherit from PdfiumServiceBase

All services that call PDFium **must** inherit from `PdfiumServiceBase`:

```csharp
public class MyPdfService : PdfiumServiceBase
{
    public async Task<Result<Data>> ProcessAsync()
    {
        return await ExecutePdfiumOperationAsync(() =>
        {
            // PDFium calls here - safe on calling thread
            var result = PdfiumInterop.SomeFunction(...);
            return Result.Ok(data);
        });
    }
}
```

**Why?** `ExecutePdfiumOperationAsync` uses `Task.Yield()` instead of `Task.Run()`, ensuring PDFium calls execute on the calling thread. This prevents AccessViolation crashes in .NET 9.0 WinUI 3.

### 2. UTF-16LE String Marshaling (Two-Phase Pattern)

PDFium uses UTF-16LE encoding for strings. Always use the two-phase allocation pattern:

```csharp
// Phase 1: Get required buffer size (includes null terminator)
var length = FPDFBookmark_GetTitle(bookmark, null, 0);
if (length == 0) return "(Untitled)";

// Phase 2: Allocate buffer and get data
var buffer = new byte[length];
FPDFBookmark_GetTitle(bookmark, buffer, length);

// Decode UTF-16LE and trim null terminators
return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
```

**Common pitfalls:** Forgetting null terminator, using wrong encoding (UTF-8 vs UTF-16LE), buffer underallocation.

### 3. Bitmap Buffer Safety

When copying bitmap buffers, always use checked arithmetic to prevent integer overflow:

```csharp
var stride = FPDFBitmap_GetStride(bitmap);
var height = FPDFBitmap_GetHeight(bitmap);

// Use checked arithmetic to detect overflow
var bufferSize = checked(stride * height);
var buffer = new byte[bufferSize];

Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);
```

**Why?** Large images (e.g., 8192x8192 pixels) can cause `stride × height` to overflow, leading to buffer underallocation and crashes.

### 4. Struct Marshaling

For PDFium structs (e.g., `FS_RECTF`, `FS_QUADPOINTSF`), always use `LayoutKind.Sequential`:

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct FS_RECTF
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}
```

**Why?** PDFium expects C-style struct layout with fields in declaration order without padding.

## Documentation Structure

| Document | Description |
|----------|-------------|
| [safe-patterns.md](./safe-patterns.md) | Proven safe patterns for UTF-16, bitmaps, threading, structs |
| [pitfalls.md](./pitfalls.md) | Common mistakes with incorrect vs. correct side-by-side examples |
| [utf16-encoding.md](./utf16-encoding.md) | UTF-16LE encoding rules, BOM, null terminators, edge cases |
| [buffer-safety.md](./buffer-safety.md) | Buffer overflow prevention, Marshal.Copy best practices |
| [threading-model.md](./threading-model.md) | Threading constraints, Task.Yield vs Task.Run, AccessViolation prevention |
| [decision-tree.md](./decision-tree.md) | Flowchart for choosing the correct marshaling approach |

## Validation Tools

FluentPDF includes automated validation tools to verify marshaling correctness:

```bash
# Validate all high-risk marshaling areas
FluentPDF.App.exe --validate-all --junit-output results.xml

# Validate specific areas
FluentPDF.App.exe --validate-utf16-marshalling
FluentPDF.App.exe --validate-bitmap-marshalling
FluentPDF.App.exe --validate-threading-model

# Profile marshaling performance
FluentPDF.App.exe --profile-marshalling --compare-baseline baseline.json

# Test known workarounds for PDFium bugs
FluentPDF.App.exe --test-workarounds
```

Exit codes:
- `0`: All validations passed
- `1`: One or more validations failed
- `2`: Critical error (e.g., PDFium not initialized)

## Key Principles

### ✅ DO
- Inherit from `PdfiumServiceBase` for all PDFium-calling services
- Use `ExecutePdfiumOperationAsync` for async operations
- Use two-phase allocation for UTF-16 strings (get length, allocate, fill)
- Use checked arithmetic for stride × height calculations
- Use `LayoutKind.Sequential` for all PDFium structs
- Trim null terminators from UTF-16 strings after decoding
- Validate buffer sizes before `Marshal.Copy`
- Test with edge cases (emojis, null chars, large images, negative coordinates)

### ❌ DON'T
- Use `Task.Run()` to wrap PDFium calls (causes AccessViolation)
- Use `Parallel.For` or thread pool threads for PDFium operations
- Assume UTF-8 encoding (PDFium uses UTF-16LE)
- Forget to include null terminator size in buffer allocation
- Use unchecked arithmetic for buffer size calculations
- Modify PDFium structs to use `LayoutKind.Auto` or add padding
- Forget to dispose PDFium handles (use `SafeHandle` pattern)

## Real-World Examples

### Example 1: Extract All Bookmark Titles (UTF-16)

```csharp
public async Task<Result<List<string>>> GetAllBookmarkTitlesAsync(PdfDocument document)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var titles = new List<string>();
        var handle = (SafePdfDocumentHandle)document.Handle;
        var bookmark = PdfiumInterop.GetFirstBookmark(handle, IntPtr.Zero);

        while (bookmark != IntPtr.Zero)
        {
            // Two-phase UTF-16 allocation
            var length = FPDFBookmark_GetTitle(bookmark, null, 0);
            if (length > 0)
            {
                var buffer = new byte[length];
                FPDFBookmark_GetTitle(bookmark, buffer, length);
                titles.Add(Encoding.Unicode.GetString(buffer).TrimEnd('\0'));
            }

            bookmark = PdfiumInterop.GetNextBookmark(handle, bookmark);
        }

        return Result.Ok(titles);
    });
}
```

### Example 2: Render Page to Bitmap (Buffer Safety)

```csharp
public async Task<Result<byte[]>> RenderPageToBitmapAsync(SafePdfPageHandle page, int width, int height)
{
    return await ExecutePdfiumOperationAsync(() =>
    {
        var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);
        try
        {
            PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 0);

            var stride = FPDFBitmap_GetStride(bitmap);
            var bufferSize = checked(stride * height); // Overflow detection
            var buffer = new byte[bufferSize];

            Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);
            return Result.Ok(buffer);
        }
        finally
        {
            FPDFBitmap_Destroy(bitmap);
        }
    });
}
```

## Testing Your Marshaling Code

### Unit Tests

Test with edge cases to ensure robustness:

```csharp
[Theory]
[InlineData("Simple")]                          // ASCII
[InlineData("Café")]                            // Latin with diacritics
[InlineData("你好")]                            // CJK
[InlineData("🚀")]                              // Emoji (4-byte surrogate pair)
[InlineData("e\u0301")]                         // Combining diacritic (é as 2 chars)
[InlineData("Hello\0World")]                    // Embedded null
[InlineData("")]                                // Empty string
public async Task BookmarkTitle_HandlesUtf16EdgeCases(string title)
{
    // Test implementation
}
```

### Automated Validation

Run automated validators in CI/CD:

```yaml
# .github/workflows/marshaling-validation.yml
- name: Validate PDFium Marshaling
  run: FluentPDF.App.exe --validate-all --junit-output validation.xml

- name: Publish Results
  uses: EnricoMi/publish-unit-test-result-action@v2
  with:
    files: validation.xml
```

## When to Use This Guide

- **Adding new PDFium P/Invoke declarations** → Read [safe-patterns.md](./safe-patterns.md)
- **Debugging crashes or data corruption** → Check [pitfalls.md](./pitfalls.md)
- **Implementing string extraction** → See [utf16-encoding.md](./utf16-encoding.md)
- **Rendering images** → See [buffer-safety.md](./buffer-safety.md)
- **Making async operations** → See [threading-model.md](./threading-model.md)
- **Choosing marshaling approach** → Use [decision-tree.md](./decision-tree.md)

## Getting Help

If you encounter marshaling issues:

1. **Check [pitfalls.md](./pitfalls.md)** for common mistakes
2. **Run validators**: `FluentPDF.App.exe --validate-all`
3. **Review existing code**: Look for similar patterns in `PdfiumInterop.cs`
4. **Test with edge cases**: Emojis, large buffers, null characters
5. **Use SafeHandle**: Ensure proper resource cleanup

## Contributing

When adding new PDFium functionality:

1. Inherit from `PdfiumServiceBase`
2. Use `ExecutePdfiumOperationAsync` for async operations
3. Follow existing marshaling patterns from this guide
4. Add unit tests with edge cases
5. Run validators before submitting PR
6. Update documentation if introducing new patterns

## References

- [PDFium API Documentation](https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/)
- [Microsoft P/Invoke Documentation](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [UTF-16 Encoding Standard](https://en.wikipedia.org/wiki/UTF-16)
- FluentPDF validation tools (see [Validation Tools](#validation-tools))
