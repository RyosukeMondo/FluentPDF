# Buffer Safety Guidelines

This document provides comprehensive guidelines for safe buffer handling when marshaling data with PDFium, focusing on preventing buffer overflows, integer overflows, and memory corruption.

## Table of Contents

- [Buffer Overflow Fundamentals](#buffer-overflow-fundamentals)
- [Integer Overflow Detection](#integer-overflow-detection)
- [Marshal.Copy Best Practices](#marshalcopy-best-practices)
- [Stride Calculation for Bitmaps](#stride-calculation-for-bitmaps)
- [Two-Phase Allocation Pattern](#two-phase-allocation-pattern)
- [Memory Safety Checklist](#memory-safety-checklist)

---

## Buffer Overflow Fundamentals

### What is a Buffer Overflow?

A **buffer overflow** occurs when data is written beyond the allocated buffer boundary, causing memory corruption, crashes, or security vulnerabilities.

```csharp
// Example of buffer overflow
var buffer = new byte[10];  // 10 bytes allocated
Marshal.Copy(source, buffer, 0, 20);  // Writing 20 bytes - OVERFLOW!
// Result: Memory corruption, crash, or security breach
```

### Common Causes in PDFium Marshaling

1. **Incorrect buffer size calculation**: `width * height` instead of `stride * height`
2. **Integer overflow**: Large dimensions cause size calculation to wrap to negative or small value
3. **Missing null terminator space**: Allocating `length - 2` instead of `length`
4. **Wrong unit conversion**: Counting characters instead of bytes for UTF-16

---

## Integer Overflow Detection

### The Problem

Multiplying large integers can cause overflow, wrapping to a negative or small value.

```csharp
// Example: 8192x8192 BGRA32 image
int width = 8192;
int stride = width * 4;  // 32,768 bytes per row
int height = 8192;

// WITHOUT checked arithmetic:
int bufferSize = stride * height;
// 32,768 × 8,192 = 268,435,456 bytes (256 MB)
// This is within int.MaxValue (2,147,483,647) so it works

// But for 16384x16384:
width = 16384;
stride = width * 4;  // 65,536 bytes per row
height = 16384;
bufferSize = stride * height;
// 65,536 × 16,384 = 1,073,741,824 bytes
// This is within int.MaxValue, but if dimensions are larger...

// For 32768x32768:
width = 32768;
stride = width * 4;  // 131,072 bytes per row
height = 32768;
bufferSize = stride * height;
// 131,072 × 32,768 = 4,294,967,296 bytes
// This OVERFLOWS int.MaxValue → wraps to 0 or negative!
// Result: Tiny buffer allocated, massive overflow
```

### Solution: Checked Arithmetic

Use `checked` blocks or expressions to detect overflow at runtime.

#### Checked Block

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var stride = FPDFBitmap_GetStride(bitmap);
    var height = FPDFBitmap_GetHeight(bitmap);

    int bufferSize;
    try
    {
        checked
        {
            bufferSize = stride * height;
        }
    }
    catch (OverflowException ex)
    {
        throw new InvalidOperationException(
            $"Bitmap dimensions too large: stride={stride}, height={height}. " +
            $"Buffer size would overflow int.MaxValue.", ex);
    }

    var buffer = new byte[bufferSize];
    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);

    return buffer;
}
```

#### Checked Expression

```csharp
public static byte[] CopyBitmapBuffer(IntPtr bitmap)
{
    var stride = FPDFBitmap_GetStride(bitmap);
    var height = FPDFBitmap_GetHeight(bitmap);

    // Single-line checked expression
    int bufferSize = checked(stride * height);

    var buffer = new byte[bufferSize];
    Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), buffer, 0, bufferSize);

    return buffer;
}
```

### When to Use Checked Arithmetic

| Operation | Use Checked? | Reason |
|-----------|--------------|--------|
| `stride * height` | ✅ Yes | Large images can overflow |
| `width * 4` | ✅ Yes | Large widths can overflow |
| `length / 2` | ❌ No | Division doesn't overflow |
| `count + 1` | ❌ No | Small increment unlikely to overflow |
| `array.Length` | ❌ No | Array length already validated |

**Rule of Thumb:** Use checked arithmetic for **multiplications involving user-controlled dimensions** (width, height, length).

---

## Marshal.Copy Best Practices

### Overview

`Marshal.Copy` copies data between managed and unmanaged memory. Passing the wrong size causes crashes.

```csharp
public static void Copy(
    IntPtr source,        // Unmanaged memory pointer
    byte[] destination,   // Managed array
    int startIndex,       // Index in destination to start writing
    int length            // Number of bytes to copy
);
```

### Rule 1: Validate Source Pointer

Always check that the source pointer is not null.

```csharp
// ❌ WRONG: No validation
var buffer = FPDFBitmap_GetBuffer(bitmap);
Marshal.Copy(buffer, destination, 0, length);  // Crash if buffer == IntPtr.Zero

// ✅ CORRECT: Validate pointer
var buffer = FPDFBitmap_GetBuffer(bitmap);
if (buffer == IntPtr.Zero)
{
    throw new InvalidOperationException("Failed to get bitmap buffer pointer");
}
Marshal.Copy(buffer, destination, 0, length);
```

### Rule 2: Validate Destination Capacity

Ensure the destination array has enough space.

```csharp
// ❌ WRONG: No capacity check
var destination = new byte[100];
Marshal.Copy(source, destination, 0, 200);  // Overflow!

// ✅ CORRECT: Validate capacity
var destination = new byte[100];
int bytesToCopy = 200;
if (bytesToCopy > destination.Length)
{
    throw new ArgumentException(
        $"Destination too small: {destination.Length} bytes, need {bytesToCopy}");
}
Marshal.Copy(source, destination, 0, bytesToCopy);
```

### Rule 3: Use Exact Length from API

Never guess or calculate buffer size - always get it from PDFium API.

```csharp
// ❌ WRONG: Calculating size manually
var width = FPDFBitmap_GetWidth(bitmap);
var height = FPDFBitmap_GetHeight(bitmap);
var bufferSize = width * height * 4;  // Assumes BGRA32, ignores stride!

// ✅ CORRECT: Get stride from API
var stride = FPDFBitmap_GetStride(bitmap);
var height = FPDFBitmap_GetHeight(bitmap);
var bufferSize = checked(stride * height);  // Includes padding
```

### Rule 4: Validate Start Index and Length

Ensure `startIndex + length` doesn't exceed array bounds.

```csharp
// ❌ WRONG: No bounds checking
Marshal.Copy(source, destination, startIndex, length);

// ✅ CORRECT: Validate bounds
if (startIndex < 0 || startIndex >= destination.Length)
{
    throw new ArgumentOutOfRangeException(nameof(startIndex));
}
if (length < 0 || startIndex + length > destination.Length)
{
    throw new ArgumentOutOfRangeException(nameof(length),
        $"startIndex ({startIndex}) + length ({length}) exceeds array bounds ({destination.Length})");
}
Marshal.Copy(source, destination, startIndex, length);
```

### Complete Safe Marshal.Copy Pattern

```csharp
public static void SafeMarshalCopy(
    IntPtr source,
    byte[] destination,
    int startIndex,
    int length)
{
    // Validate source
    if (source == IntPtr.Zero)
    {
        throw new ArgumentNullException(nameof(source), "Source pointer is null");
    }

    // Validate destination
    ArgumentNullException.ThrowIfNull(destination);

    // Validate startIndex
    if (startIndex < 0 || startIndex >= destination.Length)
    {
        throw new ArgumentOutOfRangeException(nameof(startIndex),
            $"Start index ({startIndex}) out of range [0, {destination.Length})");
    }

    // Validate length
    if (length < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(length),
            "Length cannot be negative");
    }

    // Validate bounds
    if (startIndex + length > destination.Length)
    {
        throw new ArgumentOutOfRangeException(nameof(length),
            $"startIndex ({startIndex}) + length ({length}) = {startIndex + length} " +
            $"exceeds destination.Length ({destination.Length})");
    }

    // Safe to copy
    Marshal.Copy(source, destination, startIndex, length);
}
```

---

## Stride Calculation for Bitmaps

### What is Stride?

**Stride** (also called "pitch") is the number of bytes per row of pixels, including padding for memory alignment.

```
For BGRA32 format (4 bytes per pixel):

Minimum stride = width × 4
Actual stride  = minimum stride + padding (for alignment)

Example: 1001-pixel wide image
Minimum stride: 1001 × 4 = 4004 bytes
Actual stride:  4008 bytes (padded to 4-byte alignment)
```

### Why Padding Exists

Many graphics APIs require row data to be aligned to specific byte boundaries (4, 8, or 16 bytes) for performance.

```
Row 0: [pixel data: 4004 bytes] [padding: 4 bytes]
Row 1: [pixel data: 4004 bytes] [padding: 4 bytes]
Row 2: [pixel data: 4004 bytes] [padding: 4 bytes]
...
```

### NEVER Calculate Stride Manually

PDFium handles stride calculation internally. Always use `FPDFBitmap_GetStride()`.

```csharp
// ❌ WRONG: Manual calculation
var width = FPDFBitmap_GetWidth(bitmap);
var stride = width * 4;  // Ignores padding!

// ✅ CORRECT: Get from API
var stride = FPDFBitmap_GetStride(bitmap);
```

### Buffer Size Formula

```csharp
// Correct buffer size calculation
int stride = FPDFBitmap_GetStride(bitmap);  // Bytes per row (with padding)
int height = FPDFBitmap_GetHeight(bitmap);  // Number of rows
int bufferSize = checked(stride * height);  // Total bytes (with overflow check)
```

### Example: Different Image Sizes

| Width | Height | Bytes/Pixel | Min Stride | Actual Stride | Buffer Size |
|-------|--------|-------------|------------|---------------|-------------|
| 1 | 1 | 4 | 4 | 4 | 4 bytes |
| 100 | 100 | 4 | 400 | 400 | 40,000 bytes |
| 1001 | 1000 | 4 | 4004 | 4008 | 4,008,000 bytes |
| 1920 | 1080 | 4 | 7680 | 7680 | 8,294,400 bytes |
| 8192 | 8192 | 4 | 32768 | 32768 | 268,435,456 bytes |

---

## Two-Phase Allocation Pattern

### Why Two Phases?

PDFium returns variable-length data (strings, buffers). Two-phase allocation prevents overflow and waste.

**Phase 1:** Get required size
**Phase 2:** Allocate and fill buffer

### Pattern for UTF-16 Strings

```csharp
public static string GetBookmarkTitle(IntPtr bookmark)
{
    // Phase 1: Get buffer size (includes null terminator)
    var length = FPDFBookmark_GetTitle(bookmark, null, 0);
    if (length == 0)
    {
        return "";
    }

    // Phase 2: Allocate exact size and fill
    var buffer = new byte[length];
    FPDFBookmark_GetTitle(bookmark, buffer, length);

    return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
}
```

### Pattern for Binary Buffers

```csharp
public static byte[] GetMetadata(IntPtr document)
{
    // Phase 1: Get buffer size
    var length = FPDF_GetMetadataText(document, "Title", null, 0);
    if (length == 0)
    {
        return Array.Empty<byte>();
    }

    // Phase 2: Allocate and fill
    var buffer = new byte[length];
    FPDF_GetMetadataText(document, "Title", buffer, length);

    return buffer;
}
```

### Benefits

1. **No overflow**: Exact size allocated
2. **No waste**: No overallocation
3. **Handles any size**: Works for 10 bytes or 10 MB
4. **Clear errors**: Empty result if size is 0

---

## Memory Safety Checklist

Use this checklist when implementing buffer marshaling:

### Before Allocation

- [ ] Get buffer size from PDFium API (never calculate or guess)
- [ ] Validate size is positive and reasonable
- [ ] Use checked arithmetic for size calculations (`checked(stride * height)`)
- [ ] Handle edge case: size == 0 (empty buffer)

### During Allocation

- [ ] Allocate exact size from API (including null terminator for strings)
- [ ] Handle `OutOfMemoryException` for very large allocations
- [ ] Initialize buffer if needed (PDFium fills it, so usually not needed)

### Before Marshal.Copy

- [ ] Validate source pointer is not `IntPtr.Zero`
- [ ] Validate destination array is not null
- [ ] Validate `startIndex >= 0 && startIndex < array.Length`
- [ ] Validate `length >= 0`
- [ ] Validate `startIndex + length <= array.Length`

### After Marshal.Copy

- [ ] Process data (decode UTF-16LE, convert pixel format, etc.)
- [ ] Trim null terminators for strings (`.TrimEnd('\0')`)
- [ ] Validate data integrity (check for corruption, NaN values, etc.)
- [ ] Clean up unmanaged resources (`FPDFBitmap_Destroy`, etc.)

### Complete Example

```csharp
public static byte[] RenderPageBitmap(SafePdfPageHandle page, int width, int height)
{
    // Validate inputs
    ArgumentNullException.ThrowIfNull(page);
    if (width <= 0 || height <= 0)
    {
        throw new ArgumentException("Width and height must be positive");
    }

    // Create bitmap
    var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);
    if (bitmap == IntPtr.Zero)
    {
        throw new InvalidOperationException("Failed to create bitmap");
    }

    try
    {
        // Render page
        PdfiumInterop.RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 0);

        // Get buffer parameters
        var stride = FPDFBitmap_GetStride(bitmap);
        var actualHeight = FPDFBitmap_GetHeight(bitmap);
        var buffer = FPDFBitmap_GetBuffer(bitmap);

        // Validate buffer pointer
        if (buffer == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get bitmap buffer");
        }

        // Calculate buffer size with overflow check
        int bufferSize;
        try
        {
            bufferSize = checked(stride * actualHeight);
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException(
                $"Bitmap too large: stride={stride}, height={actualHeight}", ex);
        }

        // Allocate and copy
        var managedBuffer = new byte[bufferSize];
        Marshal.Copy(buffer, managedBuffer, 0, bufferSize);

        return managedBuffer;
    }
    finally
    {
        // Clean up
        FPDFBitmap_Destroy(bitmap);
    }
}
```

---

## Testing for Buffer Safety

### Unit Tests

Test edge cases that commonly cause buffer overflows:

```csharp
[Fact]
public void BufferCopy_ZeroLengthBuffer_DoesNotCrash()
{
    var buffer = new byte[0];
    // Should not crash with empty buffer
}

[Fact]
public void BufferCopy_1x1Bitmap_Succeeds()
{
    var buffer = RenderBitmap(width: 1, height: 1);
    Assert.Equal(4, buffer.Length);  // 1 pixel × 4 bytes
}

[Fact]
public void BufferCopy_LargeBitmap_DetectsOverflow()
{
    // Attempt to render absurdly large image
    var ex = Assert.Throws<InvalidOperationException>(() =>
        RenderBitmap(width: 100000, height: 100000));

    Assert.Contains("overflow", ex.Message, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void BufferCopy_NonPowerOf2Dimensions_HandlesStridePadding()
{
    // 1001 pixels requires stride padding
    var buffer = RenderBitmap(width: 1001, height: 1000);

    var stride = 1001 * 4;  // 4004 minimum
    var actualStride = buffer.Length / 1000;  // Actual bytes per row

    Assert.True(actualStride >= stride);  // Padding may be added
}

[Theory]
[InlineData(1920, 1080)]     // HD
[InlineData(3840, 2160)]     // 4K
[InlineData(7680, 4320)]     // 8K
[InlineData(8192, 8192)]     // Max tested size
public void BufferCopy_RealWorldDimensions_Succeeds(int width, int height)
{
    var buffer = RenderBitmap(width, height);

    var expectedMinSize = width * height * 4;
    Assert.True(buffer.Length >= expectedMinSize);
}
```

### Automated Validation

```bash
# Run buffer safety validator
FluentPDF.App.exe --validate-buffer-safety

# Output:
# ✓ Stride calculation validation
# ✓ Marshal.Copy with edge-case buffers
# ✓ Integer overflow detection
# ✓ Two-phase allocation validation
# Result: All buffer safety tests passed
```

---

## Summary

| Aspect | Guideline |
|--------|-----------|
| Integer overflow | Use `checked(stride * height)` for size calculations |
| Stride | Always use `FPDFBitmap_GetStride()`, never calculate manually |
| Buffer size | Get from PDFium API, never guess or calculate |
| Marshal.Copy | Validate source pointer, destination, startIndex, and length |
| Allocation | Use two-phase pattern (get size, then allocate) |
| Edge cases | Test with 0-length, 1×1, large images, non-power-of-2 dimensions |
| Cleanup | Always destroy bitmaps in `finally` block |
| Validation | Check all parameters before `Marshal.Copy` |

**Golden Rule:** Validate everything, use checked arithmetic, and never trust manual calculations.
