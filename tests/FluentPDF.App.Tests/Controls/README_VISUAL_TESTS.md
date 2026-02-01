# Visual Regression Tests for Liquid Glass UI Components

## Overview

This directory contains comprehensive visual regression tests for FluentPDF's liquid glass UI components using **Verify.Xaml** snapshot testing with **Win2D** for headless CI-compatible rendering.

## Components Tested

### GlassPanel
- **File**: `GlassPanelVisualTests.cs`
- **Coverage**: 14 test scenarios
- **Variants Tested**:
  - Light/Dark themes
  - Elevation levels: 0dp (flat), 8dp (default), 32dp (maximum)
  - Corner radius: 0px (sharp), 8px (default), 16px (rounded)
  - Content variations: empty, text content, nested panels

### LiquidButton
- **File**: `LiquidButtonVisualTests.cs`
- **Coverage**: 16 test scenarios
- **States Tested**:
  - Normal (rest state)
  - PointerOver (hover)
  - Pressed (active)
  - Disabled
- **Style Variants**:
  - Default button
  - Primary (accent) button
  - Compact size button
  - Button with icon
- **Themes**: Light and Dark for all variants

## Test Architecture

### Headless Rendering with Win2D

All tests use **Win2D CanvasRenderTarget** for headless rendering, ensuring CI compatibility without requiring a display or UI thread:

```csharp
var renderTarget = new CanvasRenderTarget(
    canvasDevice,
    width: 400,
    height: 300,
    dpi: 96.0f);
```

### Snapshot Verification Process

1. **Render**: XAML control is rendered to `RenderTargetBitmap`
2. **Convert**: Bitmap pixels are transferred to Win2D `CanvasBitmap`
3. **Save**: PNG image is saved to `Snapshots/[ComponentName]/` directory
4. **Verify**: Verify.Xaml compares against approved baseline with 95% SSIM threshold

### Directory Structure

```
FluentPDF.App.Tests/
├── Controls/
│   ├── GlassPanelVisualTests.cs       # GlassPanel snapshot tests
│   ├── LiquidButtonVisualTests.cs     # LiquidButton snapshot tests
│   └── README_VISUAL_TESTS.md         # This file
├── Snapshots/
│   ├── GlassPanel/
│   │   ├── default_light.received.png
│   │   ├── default_light.verified.png
│   │   ├── default_dark.received.png
│   │   └── ... (all variants)
│   └── LiquidButton/
│       ├── normal_light.received.png
│       ├── normal_light.verified.png
│       └── ... (all states)
└── VerifyConfig.cs                     # Global Verify settings
```

## Running Tests

### Local Development

```bash
# Run all visual tests
dotnet test tests/FluentPDF.App.Tests --filter "FullyQualifiedName~VisualTests"

# Run only GlassPanel tests
dotnet test tests/FluentPDF.App.Tests --filter "FullyQualifiedName~GlassPanelVisualTests"

# Run only LiquidButton tests
dotnet test tests/FluentPDF.App.Tests --filter "FullyQualifiedName~LiquidButtonVisualTests"
```

### First-Time Setup

When you run tests for the first time, Verify will create `*.received.png` files. Review these snapshots:

1. **Inspect**: Open each `*.received.png` in an image viewer
2. **Approve**: If correct, rename `*.received.png` → `*.verified.png`
3. **Commit**: Add `*.verified.png` files to version control

**Automated Approval** (use with caution):

```bash
# Move all received snapshots to verified (only if you've reviewed them!)
cd tests/FluentPDF.App.Tests/Snapshots
for file in **/*.received.png; do mv "$file" "${file/.received./.verified.}"; done
```

### CI Execution

In CI environments (GitHub Actions, Azure Pipelines), tests run headlessly:

```yaml
- name: Run Visual Regression Tests
  run: dotnet test tests/FluentPDF.App.Tests --filter "FullyQualifiedName~VisualTests" --logger "trx;LogFileName=visual-tests.trx"

- name: Upload Visual Diffs (on failure)
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: visual-test-diffs
    path: tests/FluentPDF.App.Tests/Snapshots/**/*.received.png
```

## Snapshot Management

### When Snapshots Fail

Tests fail when visual differences exceed 95% SSIM similarity threshold. Investigate:

1. **Review Diff**: Verify.Xaml generates diff images highlighting pixel changes
2. **Determine Cause**:
   - **Intentional change**: New feature or design update → approve new snapshot
   - **Regression**: Unintended visual change → fix code, re-run tests
3. **Update Baseline**: Rename `*.received.png` → `*.verified.png` if change is approved

### Best Practices

✅ **DO**:
- Commit `*.verified.png` files to Git
- Review all visual changes before approving
- Re-run tests after UI code changes
- Use descriptive scenario names

❌ **DON'T**:
- Commit `*.received.png` files (these are transient)
- Auto-approve without reviewing visual diffs
- Ignore failing visual tests
- Modify `*.verified.png` files manually

## Configuration

### Verify Settings (`VerifyConfig.cs`)

- **SSIM Threshold**: 95% similarity required (5% tolerance for anti-aliasing differences)
- **Snapshot Location**: `tests/FluentPDF.App.Tests/Snapshots/`
- **Auto-Verify**: Enabled locally, disabled in CI
- **Scrubbers**: Remove timestamps and machine-specific paths

### Win2D Configuration

- **DPI**: 96.0 (standard Windows DPI)
- **Pixel Format**: B8G8R8A8UIntNormalized (32-bit BGRA)
- **File Format**: PNG with lossless compression

## Test Coverage Metrics

| Component     | Scenarios | Themes | States | Total Tests |
|---------------|-----------|--------|--------|-------------|
| GlassPanel    | 7         | 2      | -      | 14          |
| LiquidButton  | 8         | 2      | 4      | 16          |
| **TOTAL**     | -         | -      | -      | **30**      |

## Troubleshooting

### Win2D Initialization Fails

**Error**: `Failed to initialize Win2D canvas device`

**Solution**: Ensure Win2D NuGet package is installed:
```bash
dotnet add tests/FluentPDF.App.Tests package Microsoft.Graphics.Win2D --version 1.3.0
```

### Snapshots Not Generated

**Error**: No `*.received.png` files created

**Solution**: Check test output for rendering errors. Ensure controls are properly measured and arranged:
```csharp
control.Measure(new Size(width, height));
control.Arrange(new Rect(0, 0, width, height));
control.UpdateLayout();
```

### Tests Pass Locally But Fail in CI

**Cause**: Different DPI scaling or font rendering

**Solution**:
1. Ensure CI uses same DPI (96.0) as local
2. Use `CanvasRenderTarget` (not `RenderTargetBitmap` directly) for consistent rendering
3. Increase tolerance threshold if minor anti-aliasing differences occur

### Memory Issues in CI

**Error**: Out of memory during test execution

**Solution**: Run visual tests in smaller batches:
```bash
dotnet test --filter "FullyQualifiedName~GlassPanelVisualTests"
dotnet test --filter "FullyQualifiedName~LiquidButtonVisualTests"
```

## Related Documentation

- [Verify.Xaml Documentation](https://github.com/VerifyTests/Verify.Xaml)
- [Win2D API Reference](https://microsoft.github.io/Win2D/)
- [Task 4.1 Specification](.spec-workflow/specs/liquid-glass-ui/tasks.md#phase-4-polish--testing-week-7-8)

## Maintenance

**Last Updated**: 2026-01-30
**Maintainer**: Testing Team
**Related Spec**: `.spec-workflow/specs/liquid-glass-ui/tasks.md` (Task 4.1)
