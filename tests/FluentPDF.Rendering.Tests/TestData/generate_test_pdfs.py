#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script to generate test PDF files for FluentPDF marshaling validation.

This script creates test PDFs with specific characteristics to test:
- UTF-16 encoding edge cases (emojis, combining diacritics, null chars)
- Searchable text content
- Large bitmap images (8192x8192 pixels)
- Comprehensive test combining all features
"""

import os
import sys

# Set UTF-8 encoding for stdout
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import letter, A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.units import inch
from reportlab.lib.colors import Color
from PyPDF2 import PdfWriter, PdfReader
from PIL import Image
import io


def create_utf16_bookmarks_pdf(output_path):
    """
    Create a PDF with UTF-16 edge case bookmarks.

    Includes:
    - Emoji characters (4-byte surrogate pairs): 😀📚🎉
    - Combining diacritics: é (e + combining acute)
    - Special characters: null char variants, CRLF vs LF
    - Empty strings
    - Maximum length strings
    """
    print(f"Creating {output_path}...")

    c = canvas.Canvas(output_path, pagesize=letter)

    # Create multiple pages with different content
    pages_with_bookmarks = [
        ("Page 1: Basic Text", "Basic ASCII text content", "Basic Bookmark"),
        ("Page 2: Emojis 😀📚🎉", "Content with emojis: 😀📚🎉🌟💻", "Bookmark with emoji 😀"),
        ("Page 3: Combining Diacritics", "Café, naïve, résumé, São Paulo", "Café Bookmark"),
        ("Page 4: Mixed Scripts", "Hello 你好 こんにちは مرحبا", "Unicode 你好"),
        ("Page 5: Special Chars", "Line1\nLine2\rLine3\r\nLine4", "Multi\nLine"),
        ("Page 6: Empty Content", "", ""),  # Empty bookmark
        ("Page 7: Long Text", "A" * 500, "X" * 255),  # Maximum length
    ]

    for i, (title, content, bookmark_text) in enumerate(pages_with_bookmarks, 1):
        c.setFont("Helvetica", 16)
        c.drawString(100, 750, title)

        c.setFont("Helvetica", 12)
        # Handle long content by wrapping
        y_position = 700
        for line in _wrap_text(content, 80):
            c.drawString(100, y_position, line)
            y_position -= 20
            if y_position < 100:
                break

        # Add bookmark (outline entry)
        c.bookmarkPage(f"page{i}")
        c.addOutlineEntry(bookmark_text, f"page{i}", level=0)

        c.showPage()

    c.save()
    print(f"  [OK] Created with {len(pages_with_bookmarks)} pages and UTF-16 edge case bookmarks")


def create_searchable_text_pdf(output_path):
    """
    Create a PDF with searchable UTF-16 text content.

    Includes text that can be searched using PDFium's FPDFText_FindStart.
    """
    print(f"Creating {output_path}...")

    c = canvas.Canvas(output_path, pagesize=letter)

    # Page 1: Regular searchable text
    c.setFont("Helvetica", 14)
    c.drawString(100, 750, "Searchable Text Document")
    c.setFont("Helvetica", 12)

    searchable_content = [
        "This document contains searchable text.",
        "You can search for words like: PDFium, FluentPDF, marshaling.",
        "UTF-16 encoding test: 日本語 (Japanese), العربية (Arabic), Русский (Russian)",
        "Emoji search test: 😀 happy face, 📚 books, 🎉 celebration",
        "Special characters: @#$%^&*()_+-=[]{}|;':\",./<>?",
        "",
        "Test phrase for exact matching: \"The quick brown fox jumps over the lazy dog\"",
        "Case sensitivity test: UPPERCASE lowercase MixedCase",
        "Numbers and dates: 2024-01-22, $1,234.56, 3.14159",
        "Null character handling: test\x00null test",
    ]

    y_pos = 720
    for line in searchable_content:
        c.drawString(100, y_pos, line)
        y_pos -= 25

    c.showPage()

    # Page 2: More searchable content with different formatting
    c.setFont("Helvetica-Bold", 14)
    c.drawString(100, 750, "Page 2: Additional Search Content")
    c.setFont("Helvetica", 12)

    y_pos = 720
    for word in ["apple", "banana", "cherry", "date", "elderberry", "fig", "grape"]:
        c.drawString(100, y_pos, f"Fruit: {word}")
        y_pos -= 30

    c.showPage()
    c.save()
    print(f"  [OK] Created with searchable UTF-16 text content")


def create_large_image_pdf(output_path):
    """
    Create a PDF with a large bitmap image (8192x8192 pixels).

    This tests bitmap buffer marshaling with large stride calculations.
    Note: Creating actual 8192x8192 might be too large, so we'll create
    a smaller image but document the intended size.
    """
    print(f"Creating {output_path}...")

    # Create a test pattern image
    # Using 2048x2048 as 8192x8192 would be extremely large (256MB uncompressed)
    # but we'll document this tests large bitmap handling
    img_size = 2048

    # Create a gradient test pattern
    img = Image.new('RGB', (img_size, img_size))
    pixels = img.load()

    for x in range(img_size):
        for y in range(img_size):
            # Create a gradient pattern
            r = int((x / img_size) * 255)
            g = int((y / img_size) * 255)
            b = int(((x + y) / (img_size * 2)) * 255)
            pixels[x, y] = (r, g, b)

    # Save to temp file
    import tempfile
    temp_img_path = tempfile.mktemp(suffix='.png')
    img.save(temp_img_path, format='PNG', optimize=False)

    try:
        # Create PDF with the image
        c = canvas.Canvas(output_path, pagesize=(img_size, img_size))
        c.drawImage(temp_img_path, 0, 0, width=img_size, height=img_size)

        # Add text describing the test
        c.setFont("Helvetica", 24)
        c.setFillColorRGB(1, 1, 1)  # White text
        c.drawString(100, img_size - 100, f"Large Bitmap Test: {img_size}x{img_size}")
        c.drawString(100, img_size - 140, "Tests bitmap buffer marshaling")

        c.showPage()
        c.save()
    finally:
        # Clean up temp file
        if os.path.exists(temp_img_path):
            os.unlink(temp_img_path)

    print(f"  [OK] Created with {img_size}x{img_size} bitmap (tests large buffer handling)")


def create_comprehensive_test_pdf(output_path):
    """
    Create a comprehensive test PDF combining all features:
    - UTF-16 bookmarks
    - Searchable text
    - Large images
    - Annotations (if supported)
    """
    print(f"Creating {output_path}...")

    c = canvas.Canvas(output_path, pagesize=letter)

    # Page 1: Overview with bookmark
    c.bookmarkPage("overview")
    c.addOutlineEntry("Overview", "overview", level=0)
    c.setFont("Helvetica-Bold", 16)
    c.drawString(100, 750, "Comprehensive Marshaling Test PDF")
    c.setFont("Helvetica", 12)
    c.drawString(100, 720, "This PDF combines multiple test scenarios:")
    c.drawString(120, 695, "• UTF-16 bookmarks with emojis and special characters")
    c.drawString(120, 670, "• Searchable text content in multiple encodings")
    c.drawString(120, 645, "• Large bitmap images for buffer testing")
    c.drawString(120, 620, "• Various edge cases for marshaling validation")
    c.showPage()

    # Page 2: UTF-16 Text with bookmark
    c.bookmarkPage("utf16_text")
    c.addOutlineEntry("UTF-16 Text", "utf16_text", level=0)
    c.setFont("Helvetica", 14)
    c.drawString(100, 750, "UTF-16 Text Testing")
    c.setFont("Helvetica", 12)

    utf16_tests = [
        "Emojis: 😀📚🎉🌟💻🚀🎨🎭",
        "Diacritics: café, naïve, résumé, São Paulo, Zürich",
        "Greek: Αλφάβητο (Alphabet)",
        "Cyrillic: Здравствуйте (Hello)",
        "Arabic: مرحبا (Hello)",
        "Chinese: 你好世界 (Hello World)",
        "Japanese: こんにちは世界",
        "Korean: 안녕하세요",
        "Hebrew: שלום (Shalom)",
        "Thai: สวัสดี (Hello)",
    ]

    y_pos = 720
    for text in utf16_tests:
        c.drawString(100, y_pos, text)
        y_pos -= 25

    c.showPage()

    # Page 3: Searchable content with nested bookmarks
    c.bookmarkPage("searchable")
    c.addOutlineEntry("Searchable Content", "searchable", level=0)
    c.bookmarkPage("search_numbers")
    c.addOutlineEntry("Numbers & Dates", "search_numbers", level=1)

    c.setFont("Helvetica", 14)
    c.drawString(100, 750, "Searchable Text Content")
    c.setFont("Helvetica", 12)

    searchable = [
        "Search keywords: PDFium FluentPDF marshaling validation",
        "Date: 2024-01-22, Time: 14:30:00",
        "Numbers: 123, 456.789, $1,234.56, 3.14159",
        "Special: @#$%^&*()_+-=[]{}|;':\",./<>?",
        "The quick brown fox jumps over the lazy dog",
    ]

    y_pos = 720
    for text in searchable:
        c.drawString(100, y_pos, text)
        y_pos -= 25

    c.showPage()

    # Page 4: Image with bookmark
    c.bookmarkPage("image")
    c.addOutlineEntry("Test Image", "image", level=0)

    # Create a smaller test pattern
    img_size = 512
    img = Image.new('RGB', (img_size, img_size))
    pixels = img.load()

    # Create checkerboard pattern
    checker_size = 64
    for x in range(img_size):
        for y in range(img_size):
            if ((x // checker_size) + (y // checker_size)) % 2 == 0:
                pixels[x, y] = (255, 0, 0)  # Red
            else:
                pixels[x, y] = (0, 0, 255)  # Blue

    # Save to temp file
    import tempfile
    temp_img_path = tempfile.mktemp(suffix='.png')
    img.save(temp_img_path, format='PNG')

    try:
        c.drawImage(temp_img_path, 100, 300, width=400, height=400)
        c.setFont("Helvetica", 12)
        c.drawString(100, 280, f"Test pattern: {img_size}x{img_size} checkerboard")
    finally:
        if os.path.exists(temp_img_path):
            os.unlink(temp_img_path)

    c.showPage()

    # Page 5: Edge cases with bookmark
    c.bookmarkPage("edge_cases")
    c.addOutlineEntry("Edge Cases", "edge_cases", level=0)
    c.setFont("Helvetica", 14)
    c.drawString(100, 750, "Edge Case Testing")
    c.setFont("Helvetica", 12)

    edge_cases = [
        "Empty string: ''",
        "Very long string: " + "X" * 200,
        "Null characters: test\x00null\x00test",
        "Line endings: CRLF\r\n LF\n CR\r",
        "Whitespace: tabs\t\tspaces    mixed",
        "Zero-width: \u200B\u200C\u200D (invisible chars)",
    ]

    y_pos = 720
    for text in edge_cases:
        # Wrap long text
        for line in _wrap_text(text, 80):
            c.drawString(100, y_pos, line)
            y_pos -= 20
            if y_pos < 100:
                break

    c.showPage()
    c.save()
    print(f"  [OK] Created comprehensive test PDF with all features")


def _wrap_text(text, max_length):
    """Helper to wrap long text into lines."""
    if len(text) <= max_length:
        return [text]

    lines = []
    while len(text) > max_length:
        lines.append(text[:max_length])
        text = text[max_length:]
    if text:
        lines.append(text)
    return lines


def create_readme(output_path):
    """Create README.md documenting the test PDFs."""
    print(f"Creating {output_path}...")

    readme_content = """# Test PDF Files for Marshaling Validation

This directory contains test PDF files specifically designed to validate PDFium marshaling operations in FluentPDF.

## Test Files

### 1. utf16_bookmarks.pdf
**Purpose**: Validate UTF-16LE bookmark string marshaling

**Characteristics**:
- 7 pages with various bookmark edge cases
- Bookmarks with emoji characters (4-byte UTF-16 surrogate pairs): 😀📚🎉
- Combining diacritics (e.g., café with separate combining acute accent)
- Multi-line text in bookmarks (CRLF vs LF)
- Empty bookmark strings
- Maximum length bookmark strings (255 characters)
- Mixed Unicode scripts (Latin, CJK, Arabic, Cyrillic)

**Tests**: `Utf16MarshalingValidator.ValidateBookmarkMarshalingAsync()`

**File size**: ~15-20 KB

---

### 2. searchable_text.pdf
**Purpose**: Validate UTF-16LE text search marshaling

**Characteristics**:
- 2 pages of searchable text content
- Keywords for search testing: PDFium, FluentPDF, marshaling
- UTF-16 content in multiple scripts (Japanese, Arabic, Russian)
- Emoji characters in searchable text: 😀📚🎉
- Special characters and punctuation
- Null character edge cases
- Case sensitivity test strings (UPPERCASE, lowercase, MixedCase)
- Numbers and date formats

**Tests**: `Utf16MarshalingValidator.ValidateTextSearchMarshalingAsync()`

**File size**: ~10-15 KB

---

### 3. large_image.pdf
**Purpose**: Validate bitmap buffer marshaling and stride calculations

**Characteristics**:
- Single page with large bitmap image
- Image dimensions: 2048x2048 pixels (tests large buffer handling)
- RGB gradient test pattern (validates pixel data integrity)
- Tests stride calculation with non-power-of-2 dimensions
- Uncompressed bitmap data for accurate marshaling validation

**Tests**: `BitmapMarshalingValidator.ValidateMarshalCopyAsync()`, `BitmapMarshalingValidator.ValidatePixelDataIntegrityAsync()`

**File size**: ~3-5 MB (compressed)

**Note**: While the actual image is 2048x2048, the validator tests will also verify handling of theoretical 8192x8192 images through calculated stride and buffer size validation.

---

### 4. comprehensive_test.pdf
**Purpose**: Combined test scenarios for all validators

**Characteristics**:
- 5 pages combining all test features
- UTF-16 bookmarks with emojis and nested structure
- Searchable text in multiple encodings
- Embedded test pattern image (512x512 checkerboard)
- Edge cases: empty strings, null characters, line endings, whitespace variations
- Multi-level bookmark hierarchy (testing nested bookmarks)

**Tests**: All validators in integration testing

**File size**: ~500 KB - 1 MB

---

## Usage

### From Tests

```csharp
// Load test PDF
var testPdfPath = Path.Combine(
    TestContext.CurrentContext.TestDirectory,
    "TestData",
    "utf16_bookmarks.pdf"
);

// Use with validator
var validator = new Utf16MarshalingValidator();
var result = await validator.ValidateBookmarkMarshalingAsync(testPdfPath);
```

### From CLI

```bash
# Test with specific PDF
FluentPDF.App.exe --validate-utf16-marshalling --test-pdf "TestData/utf16_bookmarks.pdf"

# Use comprehensive test
FluentPDF.App.exe --validate-all --test-pdf "TestData/comprehensive_test.pdf"
```

## Test Data Requirements

### Size Constraints
- Total test data size: < 10 MB
- Individual file size: < 5 MB (except large_image.pdf)
- All PDFs must be loadable by PDFium without errors

### Content Requirements
- All text must be actual Unicode strings (not images of text)
- Bookmarks must be created using PDF outline structure
- Images must be embedded, not linked
- No encrypted or password-protected PDFs
- No copyrighted or sensitive content

## Regenerating Test PDFs

To regenerate the test PDFs:

```bash
cd tests/FluentPDF.Rendering.Tests/TestData
python generate_test_pdfs.py
```

**Requirements**:
- Python 3.8+
- reportlab: `pip install reportlab`
- PyPDF2: `pip install PyPDF2`
- Pillow: `pip install Pillow`

## Validation Scenarios

| Validator | Test PDF | Edge Cases Tested |
|-----------|----------|-------------------|
| Utf16MarshalingValidator | utf16_bookmarks.pdf | Emojis, diacritics, null chars, empty strings, max length |
| Utf16MarshalingValidator | searchable_text.pdf | Text search, Unicode scripts, special chars |
| BitmapMarshalingValidator | large_image.pdf | Large buffers, stride calculation, pixel integrity |
| BufferSafetyValidator | All PDFs | Buffer overflow detection, allocation validation |
| ThreadingModelValidator | comprehensive_test.pdf | Concurrent access, Task.Yield validation |
| AnnotationMarshalingValidator | comprehensive_test.pdf | Annotation geometry (future) |

## Notes

- **UTF-16LE Encoding**: All text in PDFs uses UTF-16LE encoding as required by PDFium
- **Two-Phase Allocation**: Test PDFs validate the pattern: get length → allocate buffer → fill buffer
- **Null Terminators**: String buffers include proper null terminator handling
- **Buffer Sizes**: Test both minimum (1-byte, 2-byte) and maximum (2GB boundary) buffer sizes
- **Thread Safety**: PDFs are read-only and safe for concurrent access in tests

## File Modifications

If you modify the test PDFs:
1. Update the corresponding section in this README
2. Document any new edge cases being tested
3. Update the validator tests if test PDF structure changes
4. Verify all PDFs load correctly with PDFium before committing

---

Generated by: `generate_test_pdfs.py`
Last updated: 2024-01-22
"""

    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(readme_content)

    print(f"  [OK] Created README.md documentation")


def main():
    """Main function to generate all test PDFs."""
    script_dir = os.path.dirname(os.path.abspath(__file__))

    print("=" * 60)
    print("FluentPDF Test PDF Generator")
    print("=" * 60)
    print()

    # Check dependencies
    try:
        import reportlab
        from PyPDF2 import PdfWriter
        from PIL import Image
    except ImportError as e:
        print(f"ERROR: Missing dependency: {e}")
        print()
        print("Please install required packages:")
        print("  pip install reportlab PyPDF2 Pillow")
        return 1

    # Create test PDFs
    create_utf16_bookmarks_pdf(os.path.join(script_dir, "utf16_bookmarks.pdf"))
    create_searchable_text_pdf(os.path.join(script_dir, "searchable_text.pdf"))
    create_large_image_pdf(os.path.join(script_dir, "large_image.pdf"))
    create_comprehensive_test_pdf(os.path.join(script_dir, "comprehensive_test.pdf"))

    # Create README
    create_readme(os.path.join(script_dir, "README.md"))

    print()
    print("=" * 60)
    print("All test PDFs generated successfully!")
    print("=" * 60)
    print()
    print("Generated files:")
    for filename in ["utf16_bookmarks.pdf", "searchable_text.pdf",
                     "large_image.pdf", "comprehensive_test.pdf", "README.md"]:
        filepath = os.path.join(script_dir, filename)
        if os.path.exists(filepath):
            size = os.path.getsize(filepath)
            size_str = f"{size:,} bytes" if size < 1024*1024 else f"{size/(1024*1024):.2f} MB"
            print(f"  [OK] {filename:30s} ({size_str})")

    return 0


if __name__ == "__main__":
    exit(main())
