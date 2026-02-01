# FluentPDF CLI Verification Commands

This directory contains comprehensive CLI verification commands for autonomous testing of FluentPDF business logic without requiring human UAT.

## Architecture

```
FluentPDF.App.exe --test-command [options]
  ↓
  DiagnosticCommandRouter
  ↓
  Specific Command (e.g., TestMergeCommand)
  ↓
  JSON Report + Exit Code
```

## Directory Structure

```
Diagnostics/
├── DiagnosticCommandRouter.cs          # Entry point and command router
├── Commands/
│   ├── TestMergeCommand.cs             # PDF merge verification
│   ├── TestSplitCommand.cs             # PDF split verification
│   ├── TestOptimizeCommand.cs          # PDF optimization verification
│   ├── TestWatermarkCommand.cs         # Watermark application verification
│   ├── TestAnnotationsCommand.cs       # Annotation persistence verification
│   ├── TestFormsCommand.cs             # Form filling and validation verification
│   └── TestConversionCommand.cs        # DOCX to PDF conversion verification
└── Models/
    ├── CommandResult.cs                # Base result class
    ├── MergeCommandResult.cs           # Merge command result
    ├── SplitCommandResult.cs           # Split command result
    ├── OptimizeCommandResult.cs        # Optimize command result
    ├── WatermarkCommandResult.cs       # Watermark command result
    ├── AnnotationsCommandResult.cs     # Annotations command result
    ├── FormsCommandResult.cs           # Forms command result
    └── ConversionCommandResult.cs      # Conversion command result
```

## Commands

### 1. Test Merge (`--test-merge`)

Verifies PDF merge functionality.

```bash
FluentPDF.App.exe --test-merge "file1.pdf;file2.pdf;file3.pdf" --output "merged.pdf"
```

**Options:**
- `--output <path>`: Output merged PDF path
- `--verify-structure`: Validate merged PDF structure with QPDF
- `--verify-pages`: Confirm page count matches sum of inputs

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Merge operation failed
- 3: Verification failed (page count mismatch)
- 4: Structure validation failed

### 2. Test Split (`--test-split`)

Verifies PDF split functionality.

```bash
FluentPDF.App.exe --test-split "document.pdf" --ranges "1-5;6-10;11-15" --output "output_dir"
```

**Options:**
- `--ranges <ranges>`: Semicolon-separated page ranges
- `--output <dir>`: Output directory for split files
- `--verify-structure`: Validate each split PDF with QPDF

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Split operation failed
- 3: Invalid page ranges
- 4: Structure validation failed

### 3. Test Optimize (`--test-optimize`)

Verifies PDF optimization functionality.

```bash
FluentPDF.App.exe --test-optimize "large.pdf" --output "optimized.pdf" --min-reduction 20
```

**Options:**
- `--output <path>`: Output optimized PDF path
- `--min-reduction <percent>`: Minimum file size reduction percentage (default: 10)
- `--verify-visual`: Render both PDFs and compare with SSIM

**Exit Codes:**
- 0: Success (met reduction threshold)
- 1: Input file not found
- 2: Optimization failed
- 3: Insufficient file size reduction
- 4: Visual regression detected

### 4. Test Watermark (`--test-watermark`)

Verifies watermark application.

```bash
FluentPDF.App.exe --test-watermark "doc.pdf" --text "CONFIDENTIAL" --output "watermarked.pdf"
```

**Options:**
- `--text <text>`: Watermark text
- `--image <path>`: Watermark image path (alternative to --text)
- `--position <pos>`: center|tl|tr|bl|br (default: center)
- `--opacity <0-100>`: Opacity percentage (default: 50)
- `--output <path>`: Output watermarked PDF path
- `--verify-visual`: Render and verify watermark present

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Watermark operation failed
- 3: Verification failed (watermark not detected)

### 5. Test Annotations (`--test-annotations-cmd`)

Verifies annotation persistence.

```bash
FluentPDF.App.exe --test-annotations-cmd "doc.pdf" --annotations "annotations.json" --output "annotated.pdf"
```

**Annotation JSON Format:**
```json
{
  "annotations": [
    {
      "type": "highlight",
      "page": 0,
      "rect": [100, 100, 200, 120],
      "color": "#FFFF00",
      "opacity": 0.5
    },
    {
      "type": "text",
      "page": 1,
      "rect": [50, 50, 150, 100],
      "content": "Review this section"
    }
  ]
}
```

**Options:**
- `--annotations <json>`: JSON file with annotation definitions
- `--output <path>`: Output PDF with annotations
- `--verify-persistence`: Reopen PDF and verify annotations present

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Annotation application failed
- 3: Persistence verification failed

### 6. Test Forms (`--test-forms-cmd`)

Verifies form filling and validation.

```bash
FluentPDF.App.exe --test-forms-cmd "form.pdf" --data "formdata.json" --output "filled.pdf"
```

**Form Data JSON Format:**
```json
{
  "fields": {
    "name": "John Doe",
    "email": "john@example.com",
    "subscribe": true,
    "gender": "male"
  }
}
```

**Options:**
- `--data <json>`: JSON file with form field values
- `--output <path>`: Output filled PDF
- `--verify-validation`: Test validation rules
- `--verify-persistence`: Reopen and verify field values

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Form fill failed
- 3: Validation failed
- 4: Persistence verification failed

### 7. Test Conversion (`--test-conversion`)

Verifies DOCX to PDF conversion.

```bash
FluentPDF.App.exe --test-conversion "document.docx" --output "converted.pdf"
```

**Options:**
- `--output <path>`: Output PDF path
- `--verify-structure`: Validate PDF structure with QPDF

**Exit Codes:**
- 0: Success
- 1: Input file not found
- 2: Conversion failed
- 3: Verification failed

## JSON Report Schema

All commands generate JSON reports with the following base structure:

```json
{
  "command": "test-merge",
  "timestamp": "2026-01-25T10:30:00Z",
  "durationMs": 1234,
  "status": "pass|fail|error",
  "errors": []
}
```

Each command extends this base with command-specific fields. See the specification for detailed schemas.

## CI/CD Integration

Exit codes are designed for CI/CD integration:

```bash
# Example Jenkins/GitHub Actions usage
./FluentPDF.App.exe --test-merge "file1.pdf;file2.pdf" --output "merged.pdf"
if [ $? -eq 0 ]; then
  echo "Merge test passed"
else
  echo "Merge test failed with exit code $?"
  exit 1
fi
```

## QPDF Integration

Commands with `--verify-structure` option use QPDF for PDF structure validation. The system automatically searches for QPDF in:
- `qpdf` (PATH)
- `C:\Program Files\qpdf\bin\qpdf.exe`
- `C:\Program Files (x86)\qpdf\bin\qpdf.exe`

If QPDF is not found, structure validation is skipped gracefully.

## Implementation Notes

- All commands execute headless (no UI dependencies)
- Commands use existing FluentPDF services (IDocumentEditingService, IWatermarkService, etc.)
- JSON serialization uses System.Text.Json with camelCase naming
- Reports are timestamped and saved to the output directory
- Comprehensive error handling with structured error messages
- Full logging integration with Serilog
