# Requirements Document

## Introduction

The PDF Validation Integration spec integrates industry-standard PDF validation tools (VeraPDF, JHOVE, QPDF) into FluentPDF's testing and quality assurance infrastructure. This establishes verifiable PDF quality through automated validation of ISO standards compliance, format integrity, and structural correctness.

The validation suite provides:
- **PDF/A Compliance**: VeraPDF validation for PDF/A-1, PDF/A-2, PDF/A-3 archival standards
- **Format Characterization**: JHOVE identification and validation of PDF format specifications
- **Structural Validation**: QPDF integrity checks for internal PDF structure
- **JSON Reporting**: Machine-readable validation reports for CI integration and trend analysis

This directly supports the product principle **"Verifiable Architecture"** - every PDF operation's output can be validated against ISO standards.

## Alignment with Product Vision

Aligns with product objectives:
- **Quality Over Features**: Validates that PDF operations produce standards-compliant output
- **Standards Compliance**: Ensures adherence to PDF/ISO 32000 specifications through automated validation
- **Verifiable Quality Architecture**: All PDF outputs are measurable against objective standards

Supports success metrics:
- **Rendering Quality**: Pass ISO 32000 conformance test suites with ≥ 95% accuracy
- **PDF Validity**: All generated PDFs (merge, split, optimize) pass QPDF validation
- **Compliance**: PDF/A generation meets archival standards verified by VeraPDF

## Requirements

### Requirement 1: VeraPDF CLI Integration

**User Story:** As a developer, I want VeraPDF CLI integrated for PDF/A validation, so that I can verify archival compliance of generated PDFs.

#### Acceptance Criteria

1. WHEN installing VeraPDF THEN it SHALL be downloaded to `tools/validation/verapdf/` via PowerShell script
2. WHEN installing VeraPDF THEN it SHALL include verapdf-installer.zip (latest stable release)
3. WHEN VeraPDF is installed THEN the CLI SHALL be executable via `tools/validation/verapdf/verapdf.bat`
4. WHEN validating a PDF THEN it SHALL invoke VeraPDF with `--format json` for machine-readable output
5. WHEN validation completes THEN it SHALL return JSON report with compliance status, errors, warnings
6. IF PDF is PDF/A compliant THEN report SHALL have `compliant: true` and flavour (1a, 1b, 2a, 2b, 2u, 3a, 3b, 3u)
7. IF PDF is non-compliant THEN report SHALL list all validation errors with rule references
8. WHEN validation fails THEN it SHALL include detailed error context (page number, location, violated rule)

### Requirement 2: JHOVE CLI Integration

**User Story:** As a developer, I want JHOVE CLI integrated for PDF format validation, so that I can characterize PDF files and detect format issues.

#### Acceptance Criteria

1. WHEN installing JHOVE THEN it SHALL be downloaded to `tools/validation/jhove/` via PowerShell script
2. WHEN installing JHOVE THEN it SHALL include jhove-x.x.x.jar (latest stable)
3. WHEN JHOVE is installed THEN the CLI SHALL be executable via `java -jar tools/validation/jhove/jhove.jar`
4. WHEN validating a PDF THEN it SHALL invoke JHOVE with PDF module and JSON output format
5. WHEN validation completes THEN it SHALL return JSON report with:
   - File format identification (PDF version)
   - Validity status (Well-Formed, Valid, Not Valid)
   - Format profile (PDF/A, PDF/X, standard PDF)
   - Document metadata (title, author, creation date)
   - Page count, security settings
6. IF PDF has format issues THEN report SHALL list specific format violations
7. WHEN validation completes THEN it SHALL log execution time and file size

### Requirement 3: QPDF CLI Integration

**User Story:** As a developer, I want QPDF CLI integrated for structural validation, so that I can verify PDF internal structure integrity.

#### Acceptance Criteria

1. WHEN QPDF is installed THEN it SHALL be available via vcpkg as part of native library build
2. WHEN validating a PDF THEN it SHALL invoke QPDF with `--check` flag
3. WHEN validation completes THEN it SHALL return exit code: 0 = valid, non-zero = errors
4. WHEN validation detects errors THEN it SHALL capture stderr output with error descriptions
5. WHEN validating structure THEN it SHALL check:
   - Cross-reference table integrity
   - Object stream validity
   - Encryption/decryption correctness
   - Internal consistency
6. IF PDF has structural errors THEN report SHALL include error type and affected object IDs
7. WHEN validation completes THEN it SHALL log whether PDF is linearized (web-optimized)

### Requirement 4: Validation Service Architecture

**User Story:** As a developer, I want a unified validation service, so that I can validate PDFs using multiple tools through a single interface.

#### Acceptance Criteria

1. WHEN creating validation service THEN it SHALL implement IPdfValidationService interface
2. WHEN service is initialized THEN it SHALL verify all validation tools are installed and executable
3. IF any validation tool is missing THEN service initialization SHALL fail with clear error message
4. WHEN validating a PDF THEN service SHALL support validation profiles:
   - `Quick`: QPDF structural check only
   - `Standard`: QPDF + JHOVE format validation
   - `Full`: QPDF + JHOVE + VeraPDF compliance check
5. WHEN validation runs THEN it SHALL execute validation tools in parallel for performance
6. WHEN validation completes THEN it SHALL return `Result<ValidationReport>` with combined results
7. WHEN any tool fails THEN it SHALL include tool name, error message, and exit code in report

### Requirement 5: JSON Validation Report Schema

**User Story:** As a developer, I want validation reports in JSON format, so that I can parse results programmatically and integrate with CI.

#### Acceptance Criteria

1. WHEN generating validation report THEN it SHALL follow JSON schema defined in `schemas/validation-report.schema.json`
2. WHEN report is generated THEN it SHALL include:
   ```json
   {
     "filePath": "path/to/file.pdf",
     "validationDate": "2026-01-11T10:30:00Z",
     "profile": "Full",
     "overallStatus": "Pass|Warn|Fail",
     "qpdf": { "status": "...", "errors": [] },
     "jhove": { "status": "...", "format": "...", "metadata": {} },
     "verapdf": { "compliant": true|false, "flavour": "...", "errors": [] }
   }
   ```
3. WHEN validation passes THEN `overallStatus` SHALL be "Pass"
4. WHEN validation has warnings THEN `overallStatus` SHALL be "Warn"
5. WHEN validation fails THEN `overallStatus` SHALL be "Fail" and errors SHALL be listed
6. WHEN report is serialized THEN it SHALL validate against the JSON schema
7. WHEN report is generated THEN it SHALL be human-readable (formatted JSON with indentation)

### Requirement 6: Integration Tests with Validation

**User Story:** As a developer, I want PDF generation operations validated in integration tests, so that I can verify output quality automatically.

#### Acceptance Criteria

1. WHEN integration tests generate PDFs THEN they SHALL validate output using IPdfValidationService
2. WHEN testing PDF merge THEN merged output SHALL pass QPDF structural validation
3. WHEN testing PDF split THEN split outputs SHALL pass JHOVE format validation
4. WHEN testing PDF optimization THEN optimized output SHALL maintain validity (QPDF check)
5. IF generated PDF fails validation THEN test SHALL fail with validation report details
6. WHEN validation fails THEN test output SHALL include:
   - Validation profile used
   - Failed tool name
   - Error messages and rule references
   - Path to generated PDF for manual inspection
7. WHEN tests run in CI THEN validation reports SHALL be uploaded as artifacts

### Requirement 7: Validation Test Fixtures

**User Story:** As a developer, I want test fixtures for validation testing, so that I can test validation logic with known-good and known-bad PDFs.

#### Acceptance Criteria

1. WHEN creating test fixtures THEN they SHALL include:
   - `valid-pdf17.pdf` - Valid PDF 1.7 document
   - `valid-pdfa-1b.pdf` - Valid PDF/A-1b document
   - `valid-pdfa-2u.pdf` - Valid PDF/A-2u document
   - `invalid-structure.pdf` - Corrupted cross-reference table
   - `invalid-pdfa.pdf` - PDF claiming PDF/A but non-compliant
2. WHEN tests validate fixtures THEN valid PDFs SHALL pass their respective validations
3. WHEN tests validate fixtures THEN invalid PDFs SHALL fail with expected errors
4. WHEN fixture is invalid THEN test SHALL verify error messages match expected violation

### Requirement 8: CI Integration for Validation

**User Story:** As a team, we want PDF validation integrated into CI, so that we can prevent invalid PDF generation from merging.

#### Acceptance Criteria

1. WHEN CI runs tests THEN validation tools SHALL be available in CI environment
2. WHEN build.yml runs THEN it SHALL install validation tools via PowerShell script
3. WHEN integration tests run THEN they SHALL execute validation checks
4. IF validation fails THEN CI build SHALL fail and block PR merge
5. WHEN validation completes THEN JSON reports SHALL be uploaded as artifacts
6. WHEN viewing CI artifacts THEN validation reports SHALL be downloadable
7. WHEN PR is created THEN CI SHALL comment with validation summary (pass/fail counts)

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Separate tool wrappers (VeraPdfWrapper, JhoveWrapper, QpdfWrapper) from validation orchestration (PdfValidationService)
- **Modular Design**: Each validation tool wrapper is independently testable
- **Dependency Management**: Validation service depends on tool wrappers via interfaces
- **Clear Interfaces**: IPdfValidationService provides unified validation API

### Performance
- **Parallel Execution**: Run QPDF, JHOVE, VeraPDF in parallel for Full profile validation
- **Timeout Handling**: Validation tools timeout after 30 seconds to prevent hanging
- **Large File Support**: Handle PDFs up to 100MB without performance degradation

### Security
- **Input Validation**: Validate file paths to prevent command injection
- **Safe Execution**: Use ProcessStartInfo with no shell execution for CLI tool invocation
- **Resource Limits**: Limit memory and CPU usage of validation processes

### Reliability
- **Error Recovery**: If one validation tool fails, others should still run
- **Logging**: All validation operations logged with correlation IDs
- **Retry Logic**: Retry validation if tool exits with transient error (e.g., file locked)

### Usability
- **Clear Reports**: Validation reports are easy to read and understand
- **Developer Friendly**: Developers can run validation locally with simple commands
- **CI Integration**: Validation results visible in PR checks and artifacts
