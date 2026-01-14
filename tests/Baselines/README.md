# Visual Regression Test Baselines

This directory contains baseline images for visual regression testing. These images represent the expected, correct rendering output for PDF pages under test.

## Directory Structure

Baselines are organized by category and test name:

```
Baselines/
├── CoreRendering/
│   ├── SimpleTextRendering/
│   │   └── page_0.png
│   ├── ComplexLayoutRendering/
│   │   └── page_0.png
│   ├── FontRendering/
│   │   └── page_0.png
│   └── MultiPageDocument_SecondPage/
│       └── page_1.png
└── Zoom/
    ├── ZoomLevel_50/
    │   └── page_0.png
    ├── ZoomLevel_100/
    │   └── page_0.png
    └── ZoomLevel_200/
        └── page_0.png
```

**Pattern**: `{Category}/{TestName}/page_{pageNumber}.png`

- **Category**: Test category (e.g., "CoreRendering", "Zoom", "Filters")
- **TestName**: Unique test identifier (e.g., "SimpleTextRendering")
- **pageNumber**: 0-based page index (page 1 in test = `page_0.png`)

## Version Control

✅ **DO commit baselines to git** - These are the source of truth for visual tests.

All files in this directory should be tracked in version control so that:
- Team members share the same baseline expectations
- Visual changes are reviewed in pull requests
- Baseline history is preserved for audit trails

## First Run Behavior

When a visual test runs for the first time (no baseline exists):

1. The test automatically creates a baseline from the rendered output
2. The test passes (no comparison performed)
3. **You MUST review the baseline visually before committing**

### Example First Run

```bash
# Run new test
dotnet test --filter "FullyQualifiedName~MyNewTest"

# Output:
# ✓ MyNewTest passed (baseline created)

# Review the generated baseline
ls tests/Baselines/MyCategory/MyNewTest/
# page_0.png

# If correct, commit it
git add tests/Baselines/MyCategory/MyNewTest/
git commit -m "Add visual baseline for MyNewTest"
```

## Updating Baselines

When visual tests fail due to intentional changes, you need to update the baselines.

### Step 1: Review Failure Output

Failed tests generate three images in `tests/TestResults/`:

```
TestResults/{Category}/{TestName}/{timestamp}/
├── actual_page1.png      # Newly rendered image
└── difference_page1.png   # Visual diff with red highlights
```

The baseline is located at:

```
Baselines/{Category}/{TestName}/page_0.png
```

### Step 2: Compare Images

Review side-by-side:
1. **Baseline** (current expected)
2. **Actual** (new rendering)
3. **Difference** (highlighted changes)

Verify the changes are intentional and correct.

### Step 3: Update Baseline

Replace the baseline with the actual image:

```bash
# Copy actual to baseline
cp tests/TestResults/CoreRendering/SimpleTextRendering/20260111_143022/actual_page1.png \
   tests/Baselines/CoreRendering/SimpleTextRendering/page_0.png
```

### Step 4: Verify and Commit

```bash
# Re-run test to confirm it passes
dotnet test --filter "FullyQualifiedName~SimpleTextRendering"

# Commit updated baseline
git add tests/Baselines/CoreRendering/SimpleTextRendering/page_0.png
git commit -m "Update baseline for SimpleTextRendering after font rendering fix"
```

## Bulk Updates

When multiple tests fail due to intentional changes:

```bash
# Run all visual tests
dotnet test --filter "Category=VisualRegression"

# For each failure:
# 1. Review TestResults/{Category}/{TestName}/{timestamp}/
# 2. Copy actual_pageN.png to corresponding baseline
# 3. Verify changes are intentional

# Example script for bulk updates (use with caution!)
for dir in tests/TestResults/CoreRendering/*/20260111_*; do
  test_name=$(basename $(dirname $dir))
  cp "$dir/actual_page1.png" "tests/Baselines/CoreRendering/$test_name/page_0.png"
done

# Verify all tests pass
dotnet test --filter "Category=VisualRegression"

# Commit all updates
git add tests/Baselines/
git commit -m "Update all CoreRendering baselines after rendering engine upgrade"
```

## Common Scenarios

### Scenario 1: New Visual Test

**Situation**: You're adding a new visual regression test.

**Steps**:
1. Write the test using `VisualRegressionTestBase.AssertVisualMatchAsync()`
2. Run the test - it creates the baseline automatically
3. Review `tests/Baselines/{Category}/{TestName}/page_*.png`
4. If correct, commit the baseline
5. If incorrect, delete baseline, fix issue, re-run

### Scenario 2: Intentional Rendering Change

**Situation**: You fixed a rendering bug or improved visual output.

**Steps**:
1. Make your code changes
2. Run visual tests - they will fail
3. Review failure output in `tests/TestResults/`
4. Compare baseline vs actual - verify changes are correct
5. Update baselines by copying actual images
6. Re-run tests to verify they pass
7. Commit updated baselines with descriptive message

### Scenario 3: Unintentional Regression

**Situation**: Visual tests fail unexpectedly.

**Steps**:
1. Review failure output to understand what changed
2. Examine the difference image to see highlighted changes
3. **Do NOT update the baseline** - the test caught a regression
4. Fix your code to match the baseline
5. Re-run tests until they pass
6. The baseline remains unchanged

### Scenario 4: Platform-Specific Rendering Differences

**Situation**: Tests pass locally but fail in CI (or vice versa).

**Steps**:
1. Download CI test artifacts to compare images
2. Identify if differences are due to environment (fonts, drivers, OS)
3. Consider:
   - Lowering SSIM threshold slightly (0.93 instead of 0.95)
   - Creating separate baseline sets for different platforms
   - Accepting minor platform differences as expected

## Best Practices

### ✅ DO

- Review all baselines visually before committing
- Use descriptive test names that indicate what's being tested
- Keep baseline images small (test single pages, not entire documents)
- Commit baseline updates with clear commit messages explaining why
- Run visual tests before pushing code changes
- Document any non-obvious baseline expectations

### ❌ DON'T

- Commit auto-generated baselines without manual review
- Update baselines to make tests pass without understanding why they failed
- Use the same baseline for multiple unrelated tests
- Commit test results from `TestResults/` directory (they're git-ignored)
- Delete baselines to bypass failing tests
- Approve baseline changes in PRs without reviewing the actual images

## Troubleshooting

### Baseline Not Found

**Error**: "No baseline exists, creating..."

**Cause**: First run or baseline was deleted.

**Solution**: The test creates the baseline automatically. Review and commit it.

### Baseline Path Mismatch

**Error**: Test can't find baseline despite it existing.

**Cause**: Category/TestName contains special characters or path case mismatch.

**Solution**:
- Use `BaselineManager.GetBaselinePath()` to see expected path
- Ensure test name matches directory name exactly
- Avoid special characters in category/test names

### Image Format Issues

**Error**: "Failed to load baseline image"

**Cause**: Baseline image is corrupted or wrong format.

**Solution**:
- Verify baseline is a valid PNG file
- Re-generate baseline by deleting and re-running test
- Check file permissions

## File Naming Convention

All baselines use PNG format with specific naming:

```
page_{pageNumber}.png
```

Where `pageNumber` is **0-based**:
- Page 1 in test → `page_0.png`
- Page 2 in test → `page_1.png`
- Page 3 in test → `page_2.png`

This matches the internal 0-based page indexing used by the rendering service.

## Related Documentation

For comprehensive information on visual regression testing:

- [docs/VISUAL-TESTING.md](../../docs/VISUAL-TESTING.md) - Complete visual testing guide
- [docs/TESTING.md](../../docs/TESTING.md) - General testing practices
- `.github/workflows/visual-regression.yml` - CI workflow configuration

## Quick Reference

### Check if baseline exists
```bash
ls tests/Baselines/{Category}/{TestName}/page_*.png
```

### Update single baseline
```bash
cp tests/TestResults/{Category}/{TestName}/{timestamp}/actual_page1.png \
   tests/Baselines/{Category}/{TestName}/page_0.png
```

### Delete baseline to regenerate
```bash
rm tests/Baselines/{Category}/{TestName}/page_0.png
dotnet test --filter "FullyQualifiedName~{TestName}"
# Review and commit new baseline
```

### View baseline in terminal (if imgcat/kitty available)
```bash
imgcat tests/Baselines/{Category}/{TestName}/page_0.png
```

### Compare baseline vs actual
```bash
# Using ImageMagick
compare tests/Baselines/{Category}/{TestName}/page_0.png \
        tests/TestResults/{Category}/{TestName}/{timestamp}/actual_page1.png \
        diff.png

# Or open both in image viewer
eog tests/Baselines/{Category}/{TestName}/page_0.png \
    tests/TestResults/{Category}/{TestName}/{timestamp}/actual_page1.png
```

## Support

If you have questions about baseline management:

1. Read [docs/VISUAL-TESTING.md](../../docs/VISUAL-TESTING.md)
2. Check test failure output for detailed error messages
3. Review existing baselines for examples
4. Consult the team before making bulk baseline updates
