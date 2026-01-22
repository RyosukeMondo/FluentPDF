# Test PDF Files for Marshaling Validation

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
