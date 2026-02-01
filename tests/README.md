# FluentPDF Avalonia Testing

Complete E2E test suite for the FluentPDF Avalonia application.

## Quick Start

### Fix Build Issue (Required First)
```powershell
pwsh tools/fix-thumbnails-xaml.ps1
```

### Run Automated Tests
```powershell
pwsh tools/test-avalonia-app.ps1
```

### Manual Testing
Follow the guide in `AVALONIA_MANUAL_TEST_GUIDE.md`

---

## Test Reports

| Document | Purpose |
|----------|---------|
| [TESTING_SUMMARY.md](./TESTING_SUMMARY.md) | **START HERE** - Quick overview and next steps |
| [AVALONIA_TEST_REPORT.md](./AVALONIA_TEST_REPORT.md) | Automated test results |
| [AVALONIA_E2E_COMPREHENSIVE_REPORT.md](./AVALONIA_E2E_COMPREHENSIVE_REPORT.md) | Detailed analysis and findings |
| [AVALONIA_MANUAL_TEST_GUIDE.md](./AVALONIA_MANUAL_TEST_GUIDE.md) | 30+ manual test procedures |

---

## Current Status

### Test Results

- ✅ **12 tests PASSED**
- ❌ **2 tests FAILED** (build issue)
- ⚠️ **1 warning**
- ⏭️ **2 tests skipped** (interactive)

### Critical Issues

1. **XAML Compilation Error** in ThumbnailsSidebar.axaml
   - **Fix available:** `tools/fix-thumbnails-xaml.ps1`
   - **ETA:** 5 minutes

2. **Missing Avalonia.DesktopRuntime.dll**
   - Needs investigation
   - May not be required

---

## Test Infrastructure

### Automated Test Suite
- **Location:** `tools/test-avalonia-app.ps1`
- **Duration:** ~60 seconds
- **Coverage:** Build, dependencies, platform, fixtures

### Manual Test Guide
- **Location:** `AVALONIA_MANUAL_TEST_GUIDE.md`
- **Tests:** 30+ procedures
- **Duration:** ~60 minutes
- **Coverage:** UI, functionality, performance, errors

### Test Fixtures
- **Location:** `Fixtures/`
- **Count:** 13 PDF files
- **Types:** Text, bookmarks, forms, corrupted, complex layouts

---

## Test Phases

1. **Build Verification** - Compile project
2. **Binary Validation** - Check executables and DLLs
3. **Dependency Check** - Verify all libraries present
4. **Platform Detection** - Confirm Windows/macOS/Linux
5. **Application Launch** - Test startup and window
6. **UI Functionality** - Manual verification
7. **PDF Operations** - Load, render, navigate
8. **Performance** - Memory and speed benchmarks
9. **Error Handling** - Invalid files and edge cases
10. **Advanced Features** - Thumbnails, bookmarks, search

---

## Success Criteria

### Build Success
- ✅ 0 compilation errors
- ✅ All dependencies present
- ✅ Executable created

### Application Success
- ✅ Launches within 5 seconds
- ✅ Window appears correctly
- ✅ No crashes

### Functionality Success
- ✅ Can open PDFs
- ✅ PDF renders correctly
- ✅ Navigation works
- ✅ Graceful error handling

### Performance Success
- ✅ Memory < 100 MB per PDF
- ✅ Load time < 3 seconds (small PDFs)
- ✅ UI responsive (< 100ms)

---

## Quick Commands

### Build and Test
```powershell
# Fix XAML error
pwsh tools/fix-thumbnails-xaml.ps1

# Run automated tests
pwsh tools/test-avalonia-app.ps1

# Run with interactive verification
pwsh tools/test-avalonia-app.ps1 -TestTimeout 60

# Launch application directly
.\src\FluentPDF.Avalonia\bin\Debug\net8.0\FluentPDF.Avalonia.exe
```

### Rebuild Application
```powershell
dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Debug
```

### Check for Diagnostic Logs
```powershell
Get-ChildItem $HOME\Desktop -Filter "FluentPDF_Avalonia_Diagnostic_*.txt"
```

---

## Test Fixtures

| File | Size | Purpose |
|------|------|---------|
| sample-with-text.pdf | 66KB | Text extraction |
| bookmarked.pdf | 5KB | Bookmark functionality |
| complex-layout.pdf | 66KB | Complex rendering |
| corrupted.pdf | 135B | Error handling |
| multi-page.pdf | 5KB | Navigation |
| sample-form.pdf | 15KB | Form testing |
| *8 more files* | - | Various scenarios |

---

## Known Issues

### Critical
- ❌ **ThumbnailsSidebar.axaml line 74** - XAML compilation error
  - Border control with multiple children
  - Fix: Wrap in Grid container
  - Script: `tools/fix-thumbnails-xaml.ps1`

### Medium
- ⚠️ **Avalonia.DesktopRuntime.dll** - Missing dependency
  - Impact unclear
  - Needs runtime testing

### Low
- ℹ️ No diagnostic logs yet (normal for fresh build)

---

## Next Steps

1. [ ] Fix XAML error (5 min)
2. [ ] Run automated tests (2 min)
3. [ ] Launch application (2 min)
4. [ ] Complete manual testing (60 min)
5. [ ] Generate final report (5 min)

**Total Time:** ~75 minutes

---

## Support

**Test Framework Version:** 1.0
**Created:** 2026-01-28
**Platform:** Windows x64
**.NET:** 8.0
**Avalonia:** 11.3.9

---

## Cross-Platform Notes

### Windows (Primary Target)
- ✅ Full testing framework available
- ✅ Native PDFium library (pdfium.dll)
- ✅ Complete dependency verification

### macOS (Secondary Target)
- ⚠️ Requires macOS build environment
- ⚠️ Native library: libpdfium.dylib
- ⚠️ Test fixtures portable

### Linux (Secondary Target)
- ⚠️ Requires Linux build environment
- ⚠️ Native library: libpdfium.so
- ⚠️ Test fixtures portable

**Note:** Automated test suite can run on all platforms, but interactive tests require GUI environment.

---

*For complete documentation, see TESTING_SUMMARY.md*
