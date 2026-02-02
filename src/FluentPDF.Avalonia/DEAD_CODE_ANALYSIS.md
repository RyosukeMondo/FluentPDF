# Dead Code Analysis - FluentPDF.Avalonia

**Analysis Date**: 2026-02-03
**Analyzed By**: Dead Code Detection Agent
**Scope**: `src/FluentPDF.Avalonia` directory
**Total Files Analyzed**: 92 C# files (excluding generated obj/ files)

## Executive Summary

This document identifies unused code in the FluentPDF.Avalonia project per KISS REQ-6 guidelines. The analysis found **5 ViewModels** and **5 value converters** that are defined but never used in the codebase.

### Severity Classification
- 🔴 **HIGH**: Complete classes with no references (can be safely deleted)
- 🟡 **MEDIUM**: Registered in DI but no view usage (investigate further)
- 🟢 **LOW**: Used in limited contexts (keep for now)

---

## 1. Unused ViewModels (HIGH Priority)

### 1.1 StampGalleryViewModel.cs &#x1F534;

**Location**: `ViewModels/StampGalleryViewModel.cs` (241 lines)
**Status**: NOT registered in DI, NO view references
**References Found**: Only in its own file

**Evidence**:
- ✗ Not registered in `App.axaml.cs` DI container
- ✗ No corresponding `.axaml` view file
- ✗ No references in other ViewModels
- ✓ Service dependency (`IStampService`) IS implemented in `FluentPDF.Rendering`

**Reason for Existence**: Likely prepared for a stamp gallery dialog UI feature that was never completed.

**Recommendation**: **DELETE** unless stamp gallery UI is planned soon. If keeping, add to DI and create view.

**Dependencies**:
```csharp
- Uses: IFileDialogService, StampType, Stamp
- Contains: StampItemViewModel (also unused)
```

---

### 1.2 PresentationViewModel.cs &#x1F534;

**Location**: `ViewModels/PresentationViewModel.cs` (218 lines)
**Status**: NOT registered in DI, NO view references
**References Found**: Only in its own file

**Evidence**:
- ✗ Not registered in `App.axaml.cs` DI container
- ✗ No corresponding `PresentationWindow.axaml` file
- ✗ No references from `MainViewModel` or other entry points
- ✓ References `PdfViewerViewModel` (which IS used)

**Reason for Existence**: Full-screen presentation mode feature (like PowerPoint) that was designed but never implemented.

**Recommendation**: **DELETE** unless presentation mode is actively planned. Requires significant UI work to become functional.

**Features Implemented**:
- Auto-hiding controls with 3-second timer
- Page navigation in presentation mode
- Exit event handling
- Mouse move detection

---

### 1.3 EncryptDialogViewModel.cs &#x1F534;

**Location**: `ViewModels/EncryptDialogViewModel.cs`
**Status**: NOT registered in DI, NO view references
**References Found**: Only in its own file

**Evidence**:
- ✗ Not registered in `App.axaml.cs` DI container
- ✗ No corresponding `EncryptDialog.axaml` view
- ✗ No menu/command hooks in MainWindow or toolbars
- ✓ Service dependency (`ISecurityService`) IS implemented

**Reason for Existence**: PDF encryption dialog prepared for security features.

**Recommendation**: **DELETE** or complete implementation. If security is a priority, add view + DI registration.

---

### 1.4 MergeViewModel.cs &#x1F534;

**Location**: `ViewModels/MergeViewModel.cs`
**Status**: NOT registered in DI, NO view references
**References Found**: Only in its own file

**Evidence**:
- ✗ Not registered in `App.axaml.cs` DI container
- ✗ No merge dialog view
- ✗ No menu commands to trigger merge
- ✓ Would use `IDocumentEditingService` (which IS registered)

**Reason for Existence**: PDF merge functionality skeleton.

**Recommendation**: **DELETE** unless document merging is an active requirement.

---

### 1.5 SplitViewModel.cs &#x1F534;

**Location**: `ViewModels/SplitViewModel.cs`
**Status**: NOT registered in DI, NO view references
**References Found**: Only in its own file

**Evidence**:
- ✗ Not registered in `App.axaml.cs` DI container
- ✗ No split dialog view
- ✗ No menu commands to trigger split
- ✓ Would use `IDocumentEditingService` (which IS registered)

**Reason for Existence**: PDF split functionality skeleton.

**Recommendation**: **DELETE** unless document splitting is an active requirement.

---

## 2. Unused Value Converters (MEDIUM Priority)

### 2.1 InverseCountToVisibilityConverter.cs &#x1F7E1;

**Location**: `Converters/InverseCountToVisibilityConverter.cs` (39 lines)
**References**: Only in its own file, NOT used in any `.axaml` files

**Recommendation**: **DELETE** - No XAML bindings found.

---

### 2.2 MatchCounterConverter.cs &#x1F7E1;

**Location**: `Converters/MatchCounterConverter.cs` (32 lines)
**References**: Only in its own file, NOT used in any `.axaml` files
**Purpose**: Formats match counts (e.g., "5 matches")

**Recommendation**: **DELETE** - No XAML bindings found. Search panel likely uses direct bindings.

---

### 2.3 EnumToIntConverter.cs &#x1F7E1;

**Location**: `Converters/EnumToIntConverter.cs` (31 lines)
**References**: Only in its own file, NOT used in any `.axaml` files

**Recommendation**: **DELETE** - No XAML bindings found.

---

### 2.4 FpsToLevelConverter.cs &#x1F7E1;

**Location**: `Converters/FpsToLevelConverter.cs` (51 lines)
**References**: Only in its own file, NOT used in any `.axaml` files
**Purpose**: Converts FPS to performance level (Excellent/Good/Fair/Poor)

**Recommendation**: **DELETE** - Performance monitoring UI not implemented.

---

### 2.5 FpsToStatusConverter.cs &#x1F7E1;

**Location**: `Converters/FpsToStatusConverter.cs` (51 lines)
**References**: Only in its own file, NOT used in any `.axaml` files
**Purpose**: Converts FPS to status text

**Recommendation**: **DELETE** - Performance monitoring UI not implemented.

---

## 3. ViewModels Registered in DI but Underutilized (LOW Priority)

### 3.1 ImageInsertionViewModel.cs 🟢

**Status**: Registered in DI (line 205 of App.axaml.cs)
**References**: App.axaml.cs only
**Verdict**: **KEEP** - Registered for future use, service fully implemented

---

### 3.2 WatermarkViewModel.cs 🟢

**Status**: Registered in DI (line 206 of App.axaml.cs)
**References**: App.axaml.cs only
**Verdict**: **KEEP** - Registered for future use, service fully implemented

---

## 4. Code Quality Issues Found

### 4.1 Empty Catch Blocks ✅
**Result**: NONE FOUND - All catch blocks have logging or error handling.

### 4.2 Commented-Out Code ✅
**Result**: NONE FOUND - No large blocks of commented methods or classes.

### 4.3 #if FALSE Blocks ✅
**Result**: NONE FOUND - No permanently disabled code sections.

### 4.4 [Obsolete] Attributes ✅
**Result**: NONE FOUND - No deprecated code markers.

---

## 5. Verified Active Code (Keep)

These classes were checked and confirmed as actively used:

| Class | Used By | Status |
|-------|---------|--------|
| `ViewLocator` | App.axaml (DataTemplate) | ✅ Active |
| `ConversionViewModel` | Registered in DI | ✅ Active |
| `FormFieldViewModel` | Registered in DI | ✅ Active |
| `SearchPanelViewModel` | SearchPanel.axaml, PdfViewerPage.axaml | ✅ Active |
| `MainToolbarViewModel` | MainWindowViewModel | ✅ Active |
| `AnnotationToolbarViewModel` | MainWindowViewModel | ✅ Active |
| `ThumbnailItem` | ThumbnailsViewModel | ✅ Active |
| `DisposableBitmapImage` | Multiple ViewModels | ✅ Active |

---

## 6. Recommended Actions

### Immediate (Before Next Release)

1. **DELETE 5 Unused ViewModels** (~1,200 LOC savings):
   ```
   ViewModels/StampGalleryViewModel.cs
   ViewModels/PresentationViewModel.cs
   ViewModels/EncryptDialogViewModel.cs
   ViewModels/MergeViewModel.cs
   ViewModels/SplitViewModel.cs
   ```

2. **DELETE 5 Unused Converters** (~200 LOC savings):
   ```
   Converters/InverseCountToVisibilityConverter.cs
   Converters/MatchCounterConverter.cs
   Converters/EnumToIntConverter.cs
   Converters/FpsToLevelConverter.cs
   Converters/FpsToStatusConverter.cs
   ```

3. **Verify Build After Deletion**:
   ```bash
   dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj
   dotnet test tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj
   ```

### Future (If Features Are Needed)

If any of the deleted features become requirements:

1. **Presentation Mode**: Recreate `PresentationViewModel` + `PresentationWindow.axaml` + register in DI
2. **Stamp Gallery**: Recreate `StampGalleryViewModel` + dialog view + register in DI
3. **Encryption**: Recreate `EncryptDialogViewModel` + dialog view + register in DI
4. **Merge/Split**: Recreate ViewModels + dialog views + register in DI

All supporting services (IStampService, ISecurityService, etc.) are already implemented and can be reused.

---

## 7. Compliance Check

### KISS REQ-6 Requirements

| Requirement | Status | Notes |
|-------------|--------|-------|
| Unused classes/methods SHALL be deleted | ⚠️ **PENDING** | 10 unused files identified for deletion |
| Empty catch blocks SHALL log or be justified | ✅ **PASS** | All catch blocks have logging |
| [Obsolete] code >2 weeks SHALL be removed | ✅ **PASS** | No [Obsolete] attributes found |
| Document all findings | ✅ **PASS** | This document |

---

## 8. Metrics

### Code Cleanup Impact

- **Files to Delete**: 10
- **Lines of Code Removed**: ~1,400
- **Maintainability Improvement**: HIGH (reduces cognitive load, faster builds)
- **Risk Level**: LOW (no references found, build will verify)
- **Effort Required**: 30 minutes (deletion + build verification)

### Before/After

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| C# Files | 92 | 82 | -10.9% |
| ViewModels | 26 | 21 | -19.2% |
| Converters | 14 | 9 | -35.7% |
| Estimated LOC | ~25,000 | ~23,600 | -5.6% |

---

## 9. Execution Plan

### Step 1: Create Backup Branch
```bash
git checkout -b dead-code-cleanup
```

### Step 2: Delete Files
```bash
# ViewModels
rm src/FluentPDF.Avalonia/ViewModels/StampGalleryViewModel.cs
rm src/FluentPDF.Avalonia/ViewModels/PresentationViewModel.cs
rm src/FluentPDF.Avalonia/ViewModels/EncryptDialogViewModel.cs
rm src/FluentPDF.Avalonia/ViewModels/MergeViewModel.cs
rm src/FluentPDF.Avalonia/ViewModels/SplitViewModel.cs

# Converters
rm src/FluentPDF.Avalonia/Converters/InverseCountToVisibilityConverter.cs
rm src/FluentPDF.Avalonia/Converters/MatchCounterConverter.cs
rm src/FluentPDF.Avalonia/Converters/EnumToIntConverter.cs
rm src/FluentPDF.Avalonia/Converters/FpsToLevelConverter.cs
rm src/FluentPDF.Avalonia/Converters/FpsToStatusConverter.cs
```

### Step 3: Verify Build
```bash
dotnet clean
dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj
dotnet test tests/FluentPDF.App.Tests/FluentPDF.App.Tests.csproj
```

### Step 4: Commit Changes
```bash
git add -A
git commit -m "refactor: remove unused ViewModels and converters per KISS REQ-6

Deleted 5 unused ViewModels (StampGalleryViewModel, PresentationViewModel,
EncryptDialogViewModel, MergeViewModel, SplitViewModel) and 5 unused converters.

- Reduces codebase by ~1,400 LOC
- Improves maintainability
- All deleted code had no references in XAML or C#
- Build verified after deletion

Ref: DEAD_CODE_ANALYSIS.md"
```

---

## 10. Notes for Manual Review

### Conservative Approach Taken

This analysis marks code as "unused" ONLY when:
1. No references found via grep in all `.cs` and `.axaml` files
2. Not registered in DI container
3. No corresponding view files exist
4. No menu/command hooks found

### Potential False Negatives

The following were NOT flagged but should be manually reviewed:

1. **ViewModels registered in DI but never instantiated** (ImageInsertionViewModel, WatermarkViewModel) - Kept because services are implemented
2. **Helper classes with minimal usage** (ToolbarManager, MenuManager) - Kept because they ARE used in MainWindow
3. **Models used only by unused ViewModels** - Kept because they're in FluentPDF.Core (shared project)

---

## Conclusion

This dead code analysis identified **10 unused files** (~1,400 LOC) that can be safely deleted to improve codebase maintainability per KISS principles. All deletions are low-risk with compiler verification available.

**Next Steps**: Execute deletion plan, verify build, and commit changes.
