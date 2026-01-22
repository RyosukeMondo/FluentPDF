# UTF-16LE Encoding Rules for PDFium

This document explains UTF-16LE (Little Endian) encoding used by PDFium for all string data, including bookmarks, text search, form fields, and annotations.

## Table of Contents

- [What is UTF-16LE?](#what-is-utf-16le)
- [Byte Order (Endianness)](#byte-order-endianness)
- [Null Terminators](#null-terminators)
- [Surrogate Pairs (4-Byte Characters)](#surrogate-pairs-4-byte-characters)
- [Combining Diacritics](#combining-diacritics)
- [Common Mistakes](#common-mistakes)
- [Edge Cases](#edge-cases)

---

## What is UTF-16LE?

**UTF-16** is a character encoding that represents Unicode code points using 16-bit (2-byte) or 32-bit (4-byte) code units.

**LE (Little Endian)** means the least significant byte comes first in memory.

### Key Characteristics

- **Most characters**: 2 bytes (U+0000 to U+FFFF, Basic Multilingual Plane)
- **Some characters**: 4 bytes (U+10000 to U+10FFFF, using surrogate pairs)
- **Byte order**: Little Endian (LSB first)
- **Null terminator**: 2 bytes (`0x00 0x00`)

### Example: "Hello" in UTF-16LE

```
Character | Unicode | UTF-16LE Bytes (hex)
----------|---------|---------------------
H         | U+0048  | 48 00
e         | U+0065  | 65 00
l         | U+006C  | 6C 00
l         | U+006C  | 6C 00
o         | U+006F  | 6F 00
(null)    | U+0000  | 00 00
```

**Total**: 12 bytes (5 chars × 2 bytes + 2-byte null terminator)

---

## Byte Order (Endianness)

### Little Endian (LE)

Bytes are stored with **least significant byte first**.

```csharp
char c = 'A';  // U+0041

// Big Endian (NOT used by PDFium):
// Memory: [00 41]

// Little Endian (used by PDFium):
// Memory: [41 00]
```

### Why It Matters

Using the wrong endianness or encoding corrupts all non-ASCII characters.

```csharp
// WRONG: UTF-8 decoding of UTF-16LE data
byte[] bytes = { 0x48, 0x00, 0x65, 0x00 };  // "He" in UTF-16LE
string wrong = Encoding.UTF8.GetString(bytes);
// Result: "H\0e\0" (embedded nulls)

// CORRECT: UTF-16LE decoding
string correct = Encoding.Unicode.GetString(bytes);
// Result: "He" ✓
```

### .NET Encoding

In .NET:
- `Encoding.Unicode` = UTF-16LE ✅
- `Encoding.BigEndianUnicode` = UTF-16BE ❌
- `Encoding.UTF8` = UTF-8 ❌

**Always use `Encoding.Unicode` for PDFium strings.**

---

## Null Terminators

PDFium strings are **null-terminated**, meaning they end with a 2-byte null character (`0x00 0x00`).

### Buffer Size Includes Null Terminator

When PDFium returns buffer size, it **includes** the null terminator.

```csharp
// PDFium returns length including null terminator
var length = FPDFBookmark_GetTitle(bookmark, null, 0);
// For "Hello": length = 12 bytes (5 chars × 2 + 2-byte null)

// Allocate exact buffer size
var buffer = new byte[length];  // 12 bytes
FPDFBookmark_GetTitle(bookmark, buffer, length);

// Buffer contents: [48 00 65 00 6C 00 6C 00 6F 00 00 00]
//                   H     e     l     l     o     null
```

### Always Trim Null Terminators After Decoding

```csharp
// Decode UTF-16LE
string rawString = Encoding.Unicode.GetString(buffer);
// Result: "Hello\0" (includes null character)

// Trim null terminators
string cleanString = rawString.TrimEnd('\0');
// Result: "Hello" ✓
```

### Embedded Null Characters

Some PDFs may have embedded null characters in strings. `TrimEnd` only removes trailing nulls.

```csharp
byte[] bytes = {
    0x48, 0x00,  // H
    0x00, 0x00,  // null
    0x69, 0x00   // i
};

string str = Encoding.Unicode.GetString(bytes);
// Result: "H\0i"

str = str.TrimEnd('\0');
// Result: "H\0i" (embedded null remains, only trailing nulls removed)
```

---

## Surrogate Pairs (4-Byte Characters)

Characters outside the Basic Multilingual Plane (U+10000 to U+10FFFF) require **surrogate pairs**: two 16-bit code units (4 bytes total).

### Examples: Emojis

```
Emoji | Unicode  | UTF-16LE Surrogate Pair
------|----------|-------------------------
🚀    | U+1F680  | D8 3D DE 80 (4 bytes)
🌟    | U+1F31F  | D8 3C DF 1F (4 bytes)
💻    | U+1F4BB  | D8 3D DC BB (4 bytes)
```

### How Surrogate Pairs Work

1. **High surrogate**: `0xD800` to `0xDBFF` (first 2 bytes)
2. **Low surrogate**: `0xDC00` to `0xDFFF` (second 2 bytes)

```csharp
// U+1F680 (🚀) encoded as surrogate pair:
// High surrogate: 0xD83D
// Low surrogate:  0xDE80

byte[] bytes = { 0x3D, 0xD8, 0x80, 0xDE };  // Little Endian
string emoji = Encoding.Unicode.GetString(bytes);
// Result: "🚀" ✓
```

### .NET Handles Surrogate Pairs Automatically

```csharp
string emoji = "🚀";
Console.WriteLine(emoji.Length);  // 2 (two 16-bit code units)

// Iterate code points (not code units)
foreach (var rune in emoji.EnumerateRunes())
{
    Console.WriteLine($"U+{rune.Value:X}");  // U+1F680
}
```

### Buffer Size Calculation

Emojis count as **2 UTF-16 code units** but **1 Unicode code point**.

```csharp
// "🚀" in UTF-16LE
string emoji = "🚀";

// .Length returns code units, not code points
Console.WriteLine(emoji.Length);  // 2 (UTF-16 code units)

// Encoding to bytes
byte[] bytes = Encoding.Unicode.GetBytes(emoji);
Console.WriteLine(bytes.Length);  // 4 bytes (2 code units × 2 bytes)
```

---

## Combining Diacritics

Some characters with diacritics (accents) can be represented two ways:

1. **Precomposed**: Single code point (e.g., `é` = U+00E9)
2. **Decomposed**: Base character + combining diacritic (e.g., `e` U+0065 + combining acute U+0301)

### Examples

```
Display | Precomposed | Decomposed
--------|-------------|------------------
é       | U+00E9      | U+0065 + U+0301
ñ       | U+00F1      | U+006E + U+0303
ü       | U+00FC      | U+0075 + U+0308
```

### PDFium May Return Either Form

```csharp
// Precomposed "é" (1 code point)
byte[] precomposed = { 0xE9, 0x00 };
string str1 = Encoding.Unicode.GetString(precomposed);
// Result: "é" (1 char)

// Decomposed "é" (2 code points: e + combining acute)
byte[] decomposed = { 0x65, 0x00, 0x01, 0x03 };
string str2 = Encoding.Unicode.GetString(decomposed);
// Result: "é" (2 chars: 'e' + '\u0301')

// Visually identical but different lengths
Console.WriteLine(str1.Length);  // 1
Console.WriteLine(str2.Length);  // 2

// String comparison may fail
Console.WriteLine(str1 == str2);  // False!
```

### Normalization for Comparison

Use Unicode normalization for reliable comparisons.

```csharp
string s1 = "é";           // Precomposed
string s2 = "e\u0301";     // Decomposed

// Direct comparison fails
Console.WriteLine(s1 == s2);  // False

// Normalize to same form
string n1 = s1.Normalize(NormalizationForm.FormC);  // Precomposed
string n2 = s2.Normalize(NormalizationForm.FormC);  // Precomposed

Console.WriteLine(n1 == n2);  // True ✓
```

---

## Common Mistakes

### Mistake 1: Using UTF-8 Encoding

```csharp
// ❌ WRONG
var buffer = new byte[length];
FPDFBookmark_GetTitle(bookmark, buffer, length);
string title = Encoding.UTF8.GetString(buffer);  // Mojibake!

// ✅ CORRECT
string title = Encoding.Unicode.GetString(buffer);
```

### Mistake 2: Forgetting to Trim Null Terminators

```csharp
// ❌ WRONG
string title = Encoding.Unicode.GetString(buffer);
// Result: "Chapter 1\0" (includes null character)

// ✅ CORRECT
string title = Encoding.Unicode.GetString(buffer).TrimEnd('\0');
// Result: "Chapter 1"
```

### Mistake 3: Subtracting Null Terminator from Buffer Size

```csharp
// ❌ WRONG
var length = FPDFBookmark_GetTitle(bookmark, null, 0);
var buffer = new byte[length - 2];  // Too small!

// ✅ CORRECT
var buffer = new byte[length];  // Includes null terminator
```

### Mistake 4: Counting Characters Instead of Bytes

```csharp
// ❌ WRONG
string title = "Hello";
var buffer = new byte[title.Length];  // Only 5 bytes - too small!
// Needs: 5 chars × 2 bytes + 2-byte null = 12 bytes

// ✅ CORRECT
var buffer = new byte[length];  // Use size from PDFium API
```

---

## Edge Cases

### Empty Strings

```csharp
// PDFium returns 0 for empty strings
var length = FPDFBookmark_GetTitle(bookmark, null, 0);
if (length == 0)
{
    return "";  // Empty string
}
```

### Very Long Strings

```csharp
// Bookmark titles can be very long (thousands of characters)
var length = FPDFBookmark_GetTitle(bookmark, null, 0);
// For 10,000-character title: length = 20,002 bytes

var buffer = new byte[length];  // Always use exact size from API
```

### Null Characters in Strings

```csharp
// Some PDFs have embedded null characters
// Example: "Hello\0World"
byte[] bytes = {
    0x48, 0x00,  // H
    0x65, 0x00,  // e
    0x6C, 0x00,  // l
    0x6C, 0x00,  // l
    0x6F, 0x00,  // o
    0x00, 0x00,  // null (embedded)
    0x57, 0x00,  // W
    0x6F, 0x00,  // o
    0x72, 0x00,  // r
    0x6C, 0x00,  // l
    0x64, 0x00,  // d
    0x00, 0x00   // null (terminator)
};

string str = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
// Result: "Hello\0World" (embedded null remains)
```

### Malformed Surrogate Pairs

```csharp
// Invalid: High surrogate without low surrogate
byte[] invalid = { 0x3D, 0xD8 };  // High surrogate only
string str = Encoding.Unicode.GetString(invalid);
// Result: "\uD83D" (replacement character)

// PDFium validation prevents this in practice
```

---

## Implementation Checklist

When implementing UTF-16LE string marshaling:

- [ ] Use `Encoding.Unicode` (UTF-16LE), **not** `Encoding.UTF8`
- [ ] Call API twice: first to get length, then to get data (two-phase allocation)
- [ ] Allocate buffer with **exact size** returned (includes null terminator)
- [ ] Always `TrimEnd('\0')` after decoding
- [ ] Handle empty strings (`length == 0`)
- [ ] Test with emojis (🚀, 🌟, 💻)
- [ ] Test with combining diacritics (é, ñ, ü)
- [ ] Test with CJK characters (你好, こんにちは, 안녕하세요)
- [ ] Test with embedded nulls ("Hello\0World")
- [ ] Use Unicode normalization for string comparisons if needed

---

## Testing UTF-16LE Marshaling

### Test Cases

```csharp
[Theory]
[InlineData("Simple")]                          // ASCII
[InlineData("Café Münchën")]                   // Latin with diacritics
[InlineData("你好世界")]                         // CJK (Chinese)
[InlineData("こんにちは")]                       // CJK (Japanese)
[InlineData("🚀🌟💻")]                          // Emojis (surrogate pairs)
[InlineData("e\u0301")]                         // Combining diacritic (é)
[InlineData("Hello\0World")]                    // Embedded null
[InlineData("")]                                // Empty string
[InlineData("A" + new string('B', 10000))]     // Very long (10,001 chars)
public async Task BookmarkTitle_HandlesUtf16EdgeCases(string expected)
{
    // Create PDF with bookmark containing test string
    // Extract bookmark title using PDFium
    // Assert: actual == expected
}
```

### Automated Validation

```bash
# Run UTF-16 marshaling validator
FluentPDF.App.exe --validate-utf16-marshalling

# Output:
# ✓ Bookmark extraction with emojis
# ✓ Text search with combining diacritics
# ✓ Form field values with CJK characters
# ✓ Embedded null character handling
# Result: All UTF-16 marshaling tests passed
```

---

## References

- [UTF-16 Wikipedia](https://en.wikipedia.org/wiki/UTF-16)
- [Unicode Standard](https://www.unicode.org/versions/latest/)
- [.NET Encoding.Unicode Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.unicode)
- [Surrogate Pairs Explained](https://en.wikipedia.org/wiki/Universal_Character_Set_characters#Surrogates)

---

## Summary

| Aspect | Rule |
|--------|------|
| Encoding | UTF-16LE (Little Endian) |
| .NET Class | `Encoding.Unicode` |
| Byte Order | LSB first (e.g., 'A' = `0x41 0x00`) |
| Null Terminator | 2 bytes (`0x00 0x00`) |
| Buffer Size | Includes null terminator |
| Most Characters | 2 bytes (U+0000 to U+FFFF) |
| Emojis | 4 bytes (surrogate pairs) |
| Post-Decode | Always `TrimEnd('\0')` |
| Comparison | Use normalization for diacritics |

**Golden Rule:** Use two-phase allocation, `Encoding.Unicode`, and `TrimEnd('\0')`.
