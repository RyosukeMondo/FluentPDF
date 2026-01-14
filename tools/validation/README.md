# PDF Validation Tools

This directory contains installation scripts and tools for PDF validation.

## Overview

FluentPDF uses three industry-standard validation tools:

1. **VeraPDF** - PDF/A compliance validation
   - Validates PDF/A-1, PDF/A-2, PDF/A-3 standards
   - Provides detailed compliance reports
   - Website: https://verapdf.org/

2. **JHOVE** - PDF format characterization
   - Validates PDF format structure
   - Extracts metadata (version, page count, encryption)
   - Detects format errors and warnings
   - Website: https://jhove.openpreserve.org/

3. **QPDF** - PDF structural validation
   - Validates PDF cross-reference tables
   - Detects structural corruption
   - Fast and lightweight
   - Website: https://qpdf.sourceforge.io/

## Installation

### Automatic Installation (Recommended)

Run the installation script:

```powershell
# Install all tools
pwsh ./install-tools.ps1

# Install specific tools only
pwsh ./install-tools.ps1 -SkipJHOVE
pwsh ./install-tools.ps1 -SkipVeraPDF -SkipQPDF

# Force reinstallation
pwsh ./install-tools.ps1 -Force
```

### System Requirements

- **PowerShell 7.0+** (cross-platform)
- **Java Runtime Environment** (for JHOVE)
  - Download: https://adoptium.net/
  - Minimum version: Java 8
- **Internet connection** (for downloading tools)

### Manual Installation

#### VeraPDF

1. Download from https://downloads.verapdf.org/
2. Extract to `tools/validation/verapdf/`
3. Verify: `verapdf --version`

#### JHOVE

1. Download from https://github.com/openpreserve/jhove/releases
2. Place `jhove.jar` in `tools/validation/jhove/`
3. Verify: `java -jar jhove.jar -v`

#### QPDF

**Windows:**
1. Download from https://github.com/qpdf/qpdf/releases
2. Extract to `tools/validation/qpdf/`
3. Verify: `qpdf --version`

**Linux/macOS:**
```bash
# Ubuntu/Debian
sudo apt-get install qpdf

# macOS
brew install qpdf

# Fedora
sudo dnf install qpdf
```

## Tool Versions

Current versions installed by the script:

| Tool      | Version | Release Date |
|-----------|---------|--------------|
| VeraPDF   | 1.26.1  | 2024-01      |
| JHOVE     | 1.30.1  | 2024-10      |
| QPDF      | 11.9.1  | 2024-06      |

## Directory Structure

After installation:

```
tools/validation/
├── install-tools.ps1       # Installation script
├── README.md               # This file
├── verapdf/                # VeraPDF installation
│   ├── verapdf.bat         # Windows executable
│   ├── verapdf             # Unix executable
│   └── ...
├── jhove/                  # JHOVE installation
│   └── jhove.jar           # JHOVE JAR file
└── qpdf/                   # QPDF installation (Windows only)
    └── bin/
        └── qpdf.exe        # QPDF executable
```

## Usage

### Command Line

#### VeraPDF

```powershell
# Validate PDF/A compliance
./verapdf/verapdf --format json myfile.pdf

# Validate with specific profile
./verapdf/verapdf --flavour 1b myfile.pdf
```

#### JHOVE

```powershell
# Validate PDF format
java -jar ./jhove/jhove.jar -m PDF-hul -h json myfile.pdf
```

#### QPDF

```powershell
# Validate PDF structure
qpdf --check myfile.pdf
```

### From FluentPDF Code

```csharp
using FluentPDF.Validation;

// Create validation service
var validationService = new PdfValidationService(
    qpdfWrapper: new QpdfWrapper(),
    jhoveWrapper: new JhoveWrapper(),
    veraPdfWrapper: new VeraPdfWrapper()
);

// Validate PDF with different profiles
var result = await validationService.ValidateAsync(
    filePath: "document.pdf",
    profile: ValidationProfile.Full
);

if (result.IsSuccess)
{
    var report = result.Value;
    Console.WriteLine($"Overall Status: {report.OverallStatus}");
    Console.WriteLine($"Compliant: {report.VeraPdfResult?.Compliant}");
}
```

## Validation Profiles

| Profile   | Tools Used           | Use Case                          | Speed  |
|-----------|---------------------|-----------------------------------|--------|
| Quick     | QPDF only           | Fast structural validation        | Fast   |
| Standard  | QPDF + JHOVE        | Format + structural validation    | Medium |
| Full      | QPDF + JHOVE + VeraPDF | Complete validation with PDF/A | Slow   |

## Troubleshooting

### Java not found (JHOVE)

```
ERROR: Java not found. JHOVE requires Java Runtime Environment.
```

**Solution:** Install Java from https://adoptium.net/ and ensure `java` is in your PATH.

### Permission denied (Linux/macOS)

```
ERROR: Permission denied: ./verapdf/verapdf
```

**Solution:** Make the executable file executable:
```bash
chmod +x ./verapdf/verapdf
```

### Tool not found in CI

**Solution:** Ensure the installation script runs before tests:
```yaml
- name: Install validation tools
  run: pwsh ./tools/validation/install-tools.ps1
```

### Download failures

If downloads fail due to network issues or URL changes:

1. Check tool versions in `install-tools.ps1`
2. Verify download URLs are still valid
3. Try manual installation
4. Check firewall/proxy settings

## CI Integration

The tools are automatically installed in GitHub Actions workflows:

```yaml
- name: Setup Java
  uses: actions/setup-java@v4
  with:
    distribution: 'temurin'
    java-version: '17'

- name: Install PDF validation tools
  run: pwsh ./tools/validation/install-tools.ps1

- name: Verify tools
  run: |
    ./tools/validation/verapdf/verapdf --version
    java -jar ./tools/validation/jhove/jhove.jar -v
    qpdf --version
```

## Updates

To update tools to newer versions:

1. Edit version numbers in `install-tools.ps1`
2. Verify download URLs are correct
3. Run `pwsh ./install-tools.ps1 -Force`
4. Test with validation suite

## Support

- VeraPDF documentation: https://docs.verapdf.org/
- JHOVE documentation: https://jhove.openpreserve.org/documentation/
- QPDF documentation: https://qpdf.readthedocs.io/

## License

The validation tools have their own licenses:
- VeraPDF: GPL v3 / MPL 2.0
- JHOVE: LGPL v2.1
- QPDF: Apache 2.0

This installation script is part of FluentPDF and follows the project's license.
