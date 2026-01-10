# PDF Validation Test Fixtures

This directory contains PDF test fixtures for validating the FluentPDF validation system. Each fixture is designed to test specific validation scenarios with QPDF, JHOVE, and VeraPDF tools.

## Fixtures Overview

### Valid PDFs

#### 1. `valid-pdf17.pdf` (543 bytes)
- **Description**: Minimal valid PDF 1.7 document
- **PDF Version**: 1.7
- **Pages**: 1
- **Content**: Simple text "Valid PDF 1.7"
- **Expected Results**:
  - QPDF: ✓ Pass (structurally valid)
  - JHOVE: ✓ Pass (Well-formed and valid PDF-hul)
  - VeraPDF: N/A (not PDF/A)
- **Use Cases**:
  - Test basic PDF structural validation
  - Test format characterization with JHOVE
  - Baseline for non-archival PDF validation

#### 2. `valid-pdfa-1b.pdf` (~1.3 KB)
- **Description**: Minimal PDF/A-1b compliant document
- **PDF Version**: 1.4 (PDF/A-1 requirement)
- **PDF/A Level**: PDF/A-1b (Basic conformance)
- **Pages**: 1
- **Content**: Simple text "Valid PDF/A-1b"
- **Features**:
  - XMP metadata declaring PDF/A-1b conformance
  - Output intent (sRGB IEC61966-2.1)
  - Standard Type1 font (Helvetica)
- **Expected Results**:
  - QPDF: ✓ Pass
  - JHOVE: ✓ Pass
  - VeraPDF: ✓ Pass (PDF/A-1b compliant)
- **Use Cases**:
  - Test PDF/A-1b compliance validation
  - Test archival format validation
  - Verify output intent handling

#### 3. `valid-pdfa-2u.pdf` (~1.3 KB)
- **Description**: Minimal PDF/A-2u compliant document
- **PDF Version**: 1.7 (PDF/A-2 requirement)
- **PDF/A Level**: PDF/A-2u (Unicode conformance)
- **Pages**: 1
- **Content**: Simple text "Valid PDF/A-2u"
- **Features**:
  - XMP metadata declaring PDF/A-2u conformance
  - Output intent (sRGB IEC61966-2.1)
  - Mark information dictionary (marked content)
  - Standard Type1 font (Helvetica)
- **Expected Results**:
  - QPDF: ✓ Pass
  - JHOVE: ✓ Pass
  - VeraPDF: ✓ Pass (PDF/A-2u compliant)
- **Use Cases**:
  - Test PDF/A-2u compliance validation
  - Test Unicode-level conformance checking
  - Verify marked content handling

### Invalid PDFs

#### 4. `invalid-structure.pdf` (462 bytes)
- **Description**: PDF with corrupted cross-reference table
- **PDF Version**: 1.7 (claimed)
- **Pages**: 1 (claimed, but unreachable)
- **Corruption Type**: Invalid cross-reference (xref) table entries
- **Details**:
  - Cross-reference offsets point to non-existent locations (999999)
  - Startxref points to invalid location
  - Object references cannot be resolved
- **Expected Results**:
  - QPDF: ✗ Fail (cross-reference errors)
  - JHOVE: ✗ Fail (invalid structure)
  - VeraPDF: ✗ Fail or error (cannot parse)
- **Use Cases**:
  - Test detection of structural corruption
  - Test error handling for invalid cross-references
  - Verify validation fails for corrupted PDFs

#### 5. `invalid-pdfa.pdf` (~1.1 KB)
- **Description**: PDF claiming PDF/A-1b conformance but violating rules
- **PDF Version**: 1.4
- **PDF/A Claim**: PDF/A-1b (in XMP metadata)
- **Pages**: 1
- **Content**: Simple text "Invalid PDF/A"
- **Violations**:
  - Missing output intent (required for PDF/A-1b)
  - XMP metadata claims conformance but document doesn't comply
  - May have additional font embedding issues
- **Expected Results**:
  - QPDF: ✓ Pass (structurally valid)
  - JHOVE: ✓ Pass (valid PDF structure)
  - VeraPDF: ✗ Fail (PDF/A-1b non-compliant - missing output intent)
- **Use Cases**:
  - Test PDF/A compliance validation catches violations
  - Test detection of false PDF/A claims
  - Verify VeraPDF identifies missing output intent

## File Size Requirements

All fixtures are kept small (< 100 KB) to:
- Minimize repository size
- Enable fast test execution
- Allow easy distribution and version control

Current sizes: 462 bytes - 1.3 KB per file

## Regenerating Fixtures

To regenerate all test fixtures, run:

```bash
cd tests/Fixtures/validation
python3 generate_fixtures.py
```

The generation script creates minimal PDFs using the PDF specification directly, without external dependencies.

## Testing with Validation Tools

### QPDF
```bash
# Valid PDFs
qpdf --check valid-pdf17.pdf        # Should pass
qpdf --check valid-pdfa-1b.pdf      # Should pass
qpdf --check valid-pdfa-2u.pdf      # Should pass

# Invalid PDFs
qpdf --check invalid-structure.pdf  # Should fail with xref errors
qpdf --check invalid-pdfa.pdf       # Should pass (structurally valid)
```

### JHOVE
```bash
# Valid PDFs
java -jar jhove.jar -m PDF-hul -h json valid-pdf17.pdf    # Should be valid
java -jar jhove.jar -m PDF-hul -h json valid-pdfa-1b.pdf  # Should be valid
java -jar jhove.jar -m PDF-hul -h json valid-pdfa-2u.pdf  # Should be valid

# Invalid PDFs
java -jar jhove.jar -m PDF-hul -h json invalid-structure.pdf  # Should be invalid
java -jar jhove.jar -m PDF-hul -h json invalid-pdfa.pdf       # Should be valid
```

### VeraPDF
```bash
# Valid PDFs
verapdf --format json valid-pdf17.pdf       # Not PDF/A
verapdf --format json valid-pdfa-1b.pdf     # Should be compliant
verapdf --format json valid-pdfa-2u.pdf     # Should be compliant

# Invalid PDFs
verapdf --format json invalid-structure.pdf # Should fail/error
verapdf --format json invalid-pdfa.pdf      # Should be non-compliant
```

## Usage in Tests

### Unit Tests
Use these fixtures in `FluentPDF.Validation.Tests` integration tests:

```csharp
[Fact]
public async Task ValidPdf17_ShouldPass_QuickProfile()
{
    var result = await _validationService.ValidateAsync(
        "tests/Fixtures/validation/valid-pdf17.pdf",
        ValidationProfile.Quick);

    Assert.True(result.IsSuccess);
    Assert.Equal(ValidationStatus.Pass, result.Value.OverallStatus);
}

[Fact]
public async Task InvalidStructure_ShouldFail_QuickProfile()
{
    var result = await _validationService.ValidateAsync(
        "tests/Fixtures/validation/invalid-structure.pdf",
        ValidationProfile.Quick);

    Assert.True(result.IsSuccess);
    Assert.Equal(ValidationStatus.Fail, result.Value.OverallStatus);
}

[Fact]
public async Task ValidPdfA1b_ShouldPass_FullProfile()
{
    var result = await _validationService.ValidateAsync(
        "tests/Fixtures/validation/valid-pdfa-1b.pdf",
        ValidationProfile.Full);

    Assert.True(result.IsSuccess);
    Assert.Equal(ValidationStatus.Pass, result.Value.OverallStatus);
    Assert.True(result.Value.VeraPdfResult?.Compliant);
}

[Fact]
public async Task InvalidPdfA_ShouldFail_VeraPdfValidation()
{
    var result = await _validationService.ValidateAsync(
        "tests/Fixtures/validation/invalid-pdfa.pdf",
        ValidationProfile.Full);

    Assert.True(result.IsSuccess);
    Assert.False(result.Value.VeraPdfResult?.Compliant);
    // Should have errors about missing output intent
}
```

### Integration Tests
Use in `FluentPDF.Core.Tests` to validate generated PDFs:

```csharp
[Fact]
public async Task MergedPdf_ShouldPassValidation()
{
    // Arrange
    var merger = new PdfMerger();
    merger.AddFile("input1.pdf");
    merger.AddFile("input2.pdf");

    // Act
    var outputPath = "merged-output.pdf";
    await merger.MergeAsync(outputPath);

    // Assert - validate output
    var validationResult = await _validationService.ValidateAsync(
        outputPath,
        ValidationProfile.Standard);

    Assert.Equal(ValidationStatus.Pass, validationResult.Value.OverallStatus);
}
```

## Notes

- These fixtures are for testing purposes only
- They are intentionally minimal to keep file sizes small
- Real-world PDFs may have additional complexity not covered here
- For comprehensive testing, consider adding more fixtures from real-world sources

## License

These test fixtures are part of the FluentPDF project and follow the same license.
