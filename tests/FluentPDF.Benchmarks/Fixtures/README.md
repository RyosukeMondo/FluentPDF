# Benchmark PDF Fixtures

This directory contains sample PDF files used for performance benchmarking in FluentPDF. Each PDF represents a different workload type to test various aspects of PDF rendering performance.

## Files

### text-heavy.pdf
**Size:** ~7 KB
**Pages:** 5
**Type:** Text-only document
**Characteristics:**
- Plain text content with minimal formatting
- Multiple paragraphs with justified text alignment
- Section headings using standard fonts (Helvetica)
- No images or complex graphics
- Represents documentation-style PDFs

**Use Case:** Benchmarking text rendering performance, font caching, and text layout algorithms.

### image-heavy.pdf
**Size:** ~34 KB
**Pages:** 4
**Type:** Photo gallery / Image-rich document
**Characteristics:**
- 4 images per page (16 total images)
- JPEG-encoded gradient images at 85% quality
- Images sized at 250x200 pixels
- Mix of color gradients (red-blue, green-yellow, purple-pink, orange-cyan)
- Minimal text (page titles and captions only)

**Use Case:** Benchmarking image decoding, scaling, and rendering performance. Tests memory allocation patterns for image-heavy documents.

### vector-graphics.pdf
**Size:** ~3 KB
**Pages:** 3
**Type:** Technical diagrams
**Characteristics:**
- Vector shapes (rectangles, circles, lines, arrows)
- Flowchart with connected boxes and arrows (Page 1)
- Layered architecture diagram with colored rectangles (Page 2)
- Component diagram with circles and dashed connections (Page 3)
- No embedded images, all vector graphics
- Minimal text labels

**Use Case:** Benchmarking vector graphics rendering, path drawing, and shape fill performance.

### complex-layout.pdf
**Size:** ~7 KB
**Pages:** 4
**Type:** Magazine-style document with mixed content
**Characteristics:**
- Cover page with centered titles
- Table of contents with styled table
- Multiple articles with headings, subheadings, and body text
- Data tables with colored headers and grid lines
- Mix of fonts, sizes, and text alignment
- Justified paragraphs with custom styles
- Page breaks and spacing

**Use Case:** Benchmarking complex layout rendering, table rendering, and multi-element page composition.

## Generation

PDFs are generated programmatically using the `generate_pdfs.py` script with the following libraries:
- **ReportLab** - PDF generation
- **Pillow** - Image manipulation

To regenerate the fixtures:

```bash
cd tests/FluentPDF.Benchmarks/Fixtures
python3 generate_pdfs.py
```

## Requirements

All PDFs meet the following requirements:
- ✓ 3-5 pages per document
- ✓ < 5MB file size (largest is ~34 KB)
- ✓ Redistributable (synthetically generated, no copyright issues)
- ✓ Represent realistic user documents
- ✓ Load successfully in FluentPDF

## Benchmark Usage

These fixtures are used in the following benchmark suites:
- **RenderingBenchmarks** - Tests rendering at different zoom levels (50%, 100%, 150%, 200%)
- **MemoryBenchmarks** - Tests memory allocations during document load and page rendering
- **NavigationBenchmarks** - Tests page navigation and zoom operations

## Notes

- PDFs are synthetically generated to ensure consistent benchmark results
- File sizes are intentionally kept small to facilitate fast CI/CD builds
- Each PDF type exercises different code paths in the rendering engine
- Gradients in image-heavy.pdf provide realistic image data without requiring external assets
