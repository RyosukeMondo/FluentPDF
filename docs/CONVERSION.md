# Office Document Conversion

FluentPDF provides high-quality conversion of Microsoft Word (.docx) documents to PDF format using a lightweight, semantic conversion pipeline.

## Overview

The conversion feature combines:
- **Mammoth.NET** for semantic DOCX parsing with clean HTML output
- **WebView2** for Chromium-based PDF generation with print-quality rendering
- **LibreOffice validation** for optional quality comparison using SSIM metrics

This approach provides professional-quality PDF conversion with a much smaller footprint (~5MB) compared to full LibreOffice integration (~300MB).

## Architecture

### Conversion Pipeline

```
DOCX File → Mammoth Parser → HTML + Images → WebView2 → PDF Output
                                                ↓
                                        Quality Validator
                                                ↓
                                        LibreOffice Comparison (Optional)
```

### Core Services

#### DocxParserService
- **Purpose**: Parse DOCX files and extract semantic content as HTML
- **Implementation**: Wraps Mammoth.NET document converter
- **Key Features**:
  - Preserves document structure (headings, paragraphs, lists, tables)
  - Maintains formatting (bold, italic, colors, fonts)
  - Embeds images as base64 data URIs
  - Handles parsing errors gracefully
- **Location**: `src/FluentPDF.Rendering/Services/DocxParserService.cs`

#### HtmlToPdfService
- **Purpose**: Convert HTML to PDF using Chromium rendering engine
- **Implementation**: Uses WebView2 CoreWebView2.PrintToPdfAsync
- **Key Features**:
  - Initializes WebView2 environment as singleton
  - Queues concurrent conversions to prevent resource contention
  - Applies optimized print settings (backgrounds, margins, scale)
  - Detects and handles missing WebView2 runtime
- **Location**: `src/FluentPDF.Rendering/Services/HtmlToPdfService.cs`

#### DocxConverterService
- **Purpose**: Orchestrate complete DOCX to PDF conversion workflow
- **Implementation**: Coordinates parser, renderer, and validator
- **Key Features**:
  - Input validation (file exists, valid DOCX format)
  - Timeout handling (default: 60 seconds)
  - Temporary file cleanup
  - Conversion metrics logging (time, size, pages)
- **Location**: `src/FluentPDF.Rendering/Services/DocxConverterService.cs`

#### LibreOfficeValidator
- **Purpose**: Validate conversion quality against LibreOffice baseline
- **Implementation**: Compares outputs using SSIM (Structural Similarity Index)
- **Key Features**:
  - Converts DOCX to PDF using LibreOffice CLI (if installed)
  - Renders both PDFs to images for comparison
  - Calculates SSIM score (0.0-1.0, higher is better)
  - Saves comparison images when score < threshold (0.85)
  - Gracefully handles LibreOffice not installed
- **Location**: `src/FluentPDF.Rendering/Services/LibreOfficeValidator.cs`

## Usage

### From UI

1. Open FluentPDF application
2. Navigate to "Convert DOCX to PDF" page
3. Click "Browse" to select a .docx file
4. Choose output location for PDF
5. (Optional) Enable "Validate Quality" for LibreOffice comparison
6. Click "Convert" or press Ctrl+Shift+C
7. Wait for conversion to complete (progress bar shows status)
8. Click "Open PDF" to view the converted document

### From Code

```csharp
// Inject the conversion service
public class MyService
{
    private readonly IDocxConverterService _converter;

    public MyService(IDocxConverterService converter)
    {
        _converter = converter;
    }

    public async Task<Result<ConversionResult>> ConvertDocxAsync(string docxPath, string pdfPath)
    {
        var options = new ConversionOptions
        {
            InputPath = docxPath,
            OutputPath = pdfPath,
            ValidateQuality = true,  // Optional: compare with LibreOffice
            TimeoutSeconds = 60      // Optional: custom timeout
        };

        var result = await _converter.ConvertAsync(options);

        if (result.IsSuccess)
        {
            var conversionResult = result.Value;
            Console.WriteLine($"Conversion completed in {conversionResult.DurationMs}ms");
            Console.WriteLine($"Output size: {conversionResult.OutputSizeBytes} bytes");
            Console.WriteLine($"Page count: {conversionResult.PageCount}");

            if (conversionResult.QualityScore.HasValue)
            {
                Console.WriteLine($"Quality score (SSIM): {conversionResult.QualityScore:F3}");
            }
        }
        else
        {
            Console.WriteLine($"Conversion failed: {result.Errors[0].Message}");
        }

        return result;
    }
}
```

## Quality Validation

### SSIM Metrics

The quality validator uses **Structural Similarity Index (SSIM)** to compare conversion output with LibreOffice baseline:

- **SSIM Score**: 0.0 (completely different) to 1.0 (identical)
- **Default Threshold**: 0.85 (good quality)
- **Typical Scores**:
  - 0.95-1.0: Excellent (near-identical)
  - 0.85-0.95: Good (acceptable differences in fonts/rendering)
  - 0.70-0.85: Fair (noticeable differences, may need review)
  - < 0.70: Poor (significant quality issues)

### When to Use Quality Validation

✅ **Enable validation when:**
- Converting important documents for distribution
- Testing conversion accuracy after code changes
- Debugging rendering issues with specific DOCX files
- Establishing quality baselines for automated tests

❌ **Skip validation when:**
- LibreOffice is not installed
- Converting large batches (slower due to LibreOffice overhead)
- Conversion speed is critical
- Working with simple documents (text-only)

### Comparison Images

When quality score falls below threshold, the validator saves comparison images:
- `comparison-fluentpdf-{guid}.png` - FluentPDF output (first page)
- `comparison-libreoffice-{guid}.png` - LibreOffice output (first page)
- `comparison-diff-{guid}.png` - Visual difference map (future enhancement)

Images are saved to: `{temp-directory}/FluentPDF/Quality/`

## Error Handling

### Common Error Codes

| Error Code | Description | Resolution |
|------------|-------------|------------|
| `DOCX_PARSE_FAILED` | Mammoth failed to parse DOCX | Check if file is valid DOCX format |
| `DOCX_CORRUPTED` | DOCX file is corrupted | Try opening in Word and re-saving |
| `DOCX_PASSWORD_PROTECTED` | DOCX is password protected | Remove password protection |
| `PDF_GENERATION_FAILED` | WebView2 failed to generate PDF | Check WebView2 runtime installed |
| `WEBVIEW2_NOT_FOUND` | WebView2 runtime not installed | Install from microsoft.com/webview2 |
| `CONVERSION_TIMEOUT` | Conversion exceeded timeout | Increase timeout or split large docs |
| `FILE_NOT_FOUND` | Input DOCX file not found | Verify file path |
| `QUALITY_VALIDATION_FAILED` | Quality below threshold | Review comparison images |

### Error Recovery

The conversion service ensures:
- **Automatic Cleanup**: Temporary files deleted even on error
- **Resource Disposal**: WebView2 resources properly released
- **Structured Logging**: All errors logged with correlation IDs
- **User-Friendly Messages**: Technical errors translated to actionable messages

## Performance Considerations

### Typical Conversion Times

| Document Type | Pages | Time (approx.) |
|---------------|-------|----------------|
| Simple text | 10 | 2-3 seconds |
| With images | 10 | 3-5 seconds |
| Complex formatting | 10 | 5-8 seconds |
| Large document | 100 | 30-60 seconds |

*Times measured on typical hardware (Intel i7, 16GB RAM)*

### Memory Usage

- **Baseline**: < 50MB when idle
- **During Conversion**: 200-500MB depending on document
- **Peak**: May spike to 1GB for documents with many large images

### Optimization Tips

1. **Batch Conversions**: Process sequentially to avoid memory pressure
2. **Image Compression**: Pre-compress images in DOCX before conversion
3. **Timeout Configuration**: Adjust timeout for very large documents
4. **Quality Validation**: Disable for batch conversions (adds 30-50% overhead)

## Dependencies

### Required NuGet Packages

- **Mammoth.NET** (≥1.6.0) - DOCX parsing
- **Microsoft.Web.WebView2** (≥1.0.2088.41) - PDF generation
- **FluentResults** (≥3.15.0) - Error handling
- **Serilog** (≥3.1.0) - Logging

### Required Runtimes

- **.NET 8.0** - Application runtime
- **WebView2 Runtime** - Chromium-based rendering
  - Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703
  - Usually pre-installed on Windows 11
  - Must be installed separately on Windows 10

### Optional Dependencies

- **LibreOffice** (≥7.0) - Quality validation baseline
  - Only required if quality validation is enabled
  - Download: https://www.libreoffice.org/download/
  - CLI must be accessible via `soffice` command

## Testing

### Unit Tests

All conversion services have comprehensive unit tests with mocked dependencies:
- `DocxParserServiceTests.cs` - Mammoth parsing tests
- `HtmlToPdfServiceTests.cs` - WebView2 rendering tests (mocked)
- `DocxConverterServiceTests.cs` - Orchestration tests
- `LibreOfficeValidatorTests.cs` - Quality validation tests

Run unit tests:
```bash
dotnet test tests/FluentPDF.Rendering.Tests --filter "Category!=Integration"
```

### Integration Tests

Integration tests use real Mammoth.NET and WebView2:
- `DocxConversionIntegrationTests.cs` - End-to-end conversion tests

Run integration tests (requires WebView2 runtime):
```bash
dotnet test tests/FluentPDF.Rendering.Tests --filter "Category=Integration"
```

### Architecture Tests

ArchUnitNET enforces clean architecture boundaries:
- Conversion services must implement interfaces
- Core must not depend on Mammoth or WebView2
- ViewModels must not reference conversion implementations
- Services must return Result<T> for operations

Run architecture tests:
```bash
dotnet test tests/FluentPDF.Architecture.Tests
```

## CI/CD Support

The conversion feature is fully supported in GitHub Actions CI pipeline:

1. **WebView2 Installation**: CI workflow installs WebView2 runtime
2. **Integration Tests**: Run with real WebView2 if available
3. **Graceful Fallback**: Skip conversion tests if WebView2 unavailable
4. **Artifact Upload**: Conversion outputs saved as test artifacts

See: `.github/workflows/test.yml`

## Troubleshooting

### WebView2 Runtime Not Found

**Symptom**: Error code `WEBVIEW2_NOT_FOUND` when converting

**Resolution**:
1. Download WebView2 Runtime: https://go.microsoft.com/fwlink/p/?LinkId=2124703
2. Run installer with `/silent /install` for unattended installation
3. Restart application after installation

### Poor Conversion Quality

**Symptom**: Converted PDF looks different from original DOCX

**Possible Causes**:
1. **Custom Fonts**: DOCX uses fonts not installed on system
   - Solution: Install missing fonts or embed fonts in DOCX
2. **Complex Formatting**: Advanced Word features not supported by Mammoth
   - Solution: Simplify formatting or use LibreOffice for complex docs
3. **Images**: High-resolution images may be downscaled
   - Solution: Pre-optimize images to target resolution

**Debugging**:
1. Enable quality validation to see SSIM score
2. Review comparison images in temp directory
3. Check logs for Mammoth warnings about unsupported features

### Conversion Timeout

**Symptom**: Error code `CONVERSION_TIMEOUT` for large documents

**Resolution**:
1. Increase timeout in ConversionOptions:
   ```csharp
   options.TimeoutSeconds = 120; // 2 minutes
   ```
2. Split very large documents into smaller chunks
3. Check system resources (CPU, memory) aren't constrained

### LibreOffice Not Found

**Symptom**: Warning "LibreOffice not installed, skipping validation"

**Resolution**:
1. Install LibreOffice: https://www.libreoffice.org/download/
2. Ensure `soffice` is in system PATH
3. Verify installation: `soffice --version`

**Note**: LibreOffice is optional and only required for quality validation.

## Future Enhancements

Potential improvements for future versions:

1. **Batch Conversion**: UI support for converting multiple files
2. **Custom CSS**: Allow custom CSS for HTML-to-PDF rendering
3. **Progress Callbacks**: Fine-grained progress reporting during conversion
4. **PDF/A Output**: Generate PDF/A compliant documents
5. **Template Support**: Convert with custom page headers/footers
6. **Cloud Integration**: Upload converted PDFs to OneDrive/SharePoint
7. **OCR Support**: Extract text from images in DOCX
8. **Diff Visualization**: Generate visual diff images for quality validation

## References

- **Mammoth.NET**: https://github.com/mwilliamson/dotnet-mammoth
- **WebView2**: https://developer.microsoft.com/microsoft-edge/webview2/
- **SSIM Algorithm**: https://en.wikipedia.org/wiki/Structural_similarity
- **PDF Specification**: https://www.adobe.com/devnet/pdf/pdf_reference.html
