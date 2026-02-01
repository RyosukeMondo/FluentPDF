# Accessibility Testing Guide

Comprehensive WCAG 2.1 Level AA accessibility testing for FluentPDF liquid glass UI.

## Overview

This test suite validates that FluentPDF meets Web Content Accessibility Guidelines (WCAG) 2.1 Level AA standards for desktop applications. Tests cover keyboard navigation, screen reader support, high contrast mode, and color contrast ratios.

## Test Files

- **AccessibilityTests.cs** (607 lines) - Main test suite with 14 test methods
- **ColorContrastAnalyzer.cs** (299 lines) - WCAG contrast ratio calculator with screen capture

## Test Categories

### 1. Keyboard Navigation Tests

Tests that all functionality is accessible via keyboard without requiring a mouse.

#### KeyboardNavigation_TabKey_NavigatesInLogicalOrder
- **WCAG**: 2.1.1 (Keyboard)
- **Purpose**: Validates Tab key moves focus through all interactive elements in logical order
- **Method**: Enumerates focusable elements, simulates Tab key presses, verifies all elements visited
- **Pass Criteria**: All focusable elements reachable, no elements skipped

#### KeyboardNavigation_ShiftTab_NavigatesBackwards
- **WCAG**: 2.1.1 (Keyboard)
- **Purpose**: Validates Shift+Tab reverses focus direction
- **Method**: Tab forward twice, Shift+Tab backward once, verify focus moved backwards
- **Pass Criteria**: Backward navigation works correctly

#### KeyboardShortcuts_CtrlO_OpensFileDialog
- **WCAG**: 2.1.1 (Keyboard)
- **Purpose**: Validates Ctrl+O keyboard shortcut opens file picker
- **Method**: Simulates Ctrl+O, checks for modal dialog appearance
- **Pass Criteria**: File dialog opens within 500ms

#### KeyboardShortcuts_CtrlF_OpensSearchPanel
- **WCAG**: 2.1.1 (Keyboard)
- **Purpose**: Validates Ctrl+F keyboard shortcut opens search panel
- **Method**: Simulates Ctrl+F, checks search panel visibility
- **Pass Criteria**: Search panel becomes visible and focused

#### KeyboardNavigation_ButtonActivation_WorksWithEnterAndSpace
- **WCAG**: 2.1.1 (Keyboard)
- **Purpose**: Validates buttons can be activated with Enter and Space keys
- **Method**: Focus button, press Enter, verify invocation; repeat with Space
- **Pass Criteria**: Both Enter and Space activate focused button

### 2. Screen Reader Support Tests

Tests that all interactive elements are properly labeled and announced to assistive technologies.

#### ScreenReader_AllInteractiveElements_HaveAccessibleNames
- **WCAG**: 4.1.2 (Name, Role, Value)
- **Purpose**: Ensures all interactive elements have AutomationProperties.Name
- **Method**: Queries all buttons, textboxes, checkboxes, etc. for accessible names
- **Pass Criteria**: No elements missing Name or HelpText properties

#### ScreenReader_Buttons_HaveDescriptiveNames
- **WCAG**: 4.1.2 (Name, Role, Value)
- **Purpose**: Validates button labels are descriptive, not generic
- **Method**: Checks button names aren't just "Button"
- **Pass Criteria**: All buttons have meaningful, descriptive names

#### ScreenReader_FormFields_HaveLabels
- **WCAG**: 3.3.2 (Labels or Instructions)
- **Purpose**: Ensures all form fields have associated labels
- **Method**: Checks textboxes for Name or HelpText properties
- **Pass Criteria**: All input fields properly labeled

#### ScreenReader_StateChanges_AreAnnounced
- **WCAG**: 4.1.3 (Status Messages)
- **Purpose**: Validates state changes are exposed to assistive tech
- **Method**: Toggles checkbox/radio button, verifies ToggleState property updates
- **Pass Criteria**: Toggle pattern properly implemented and exposed

### 3. High Contrast Mode Tests

Tests that application remains usable in Windows High Contrast mode.

#### HighContrast_ApplicationStillUsable
- **WCAG**: 1.4.3 (Contrast - Minimum)
- **Purpose**: Documents high contrast mode compatibility
- **Method**: Checks for theme system logging, documents manual test requirements
- **Pass Criteria**: Application has theme system (manual testing required)

#### HighContrast_AcrylicEffects_FallbackToSolidColors
- **Purpose**: Validates acrylic glass effects degrade gracefully
- **Method**: Checks theme system logs for contrast/theme change detection
- **Pass Criteria**: Application remains functional (manual verification needed)

**Manual Testing Required:**
1. Enable Windows High Contrast mode (Alt+Shift+PrtScn)
2. Verify all text is readable with sufficient contrast
3. Verify acrylic effects replaced with solid colors
4. Verify all interactive elements have visible borders
5. Verify focus indicators are clearly visible

### 4. Color Contrast Ratio Tests

Tests that text and UI components meet WCAG contrast requirements.

#### ContrastRatio_NormalText_MeetsWCAG_AA
- **WCAG**: 1.4.3 (Contrast - Minimum)
- **Purpose**: Validates text has 4.5:1 contrast (normal) or 3:1 (large)
- **Method**: Captures element screenshots, extracts colors, calculates ratios
- **Pass Criteria**: All text meets minimum contrast ratios
- **Limits**: Tests first 10 text elements for performance

#### ContrastRatio_UIComponents_MeetsWCAG_AA
- **WCAG**: 1.4.11 (Non-text Contrast)
- **Purpose**: Validates UI components have 3:1 contrast
- **Method**: Analyzes buttons, textboxes, checkboxes for contrast
- **Pass Criteria**: All components meet 3:1 ratio
- **Limits**: Tests first 10 components for performance

#### ContrastRatio_FocusIndicators_AreVisible
- **WCAG**: 2.4.7 (Focus Visible)
- **Purpose**: Ensures focus indicators are sufficiently visible
- **Method**: Focuses elements, checks for HasKeyboardFocus property
- **Pass Criteria**: All focusable elements show visual focus state

## ColorContrastAnalyzer Utility

WCAG 2.1 compliant contrast ratio calculator with automated color extraction.

### Key Features

- **WCAG Constants**: Pre-defined thresholds (4.5:1 text, 3:1 UI, AAA levels)
- **CalculateContrastRatio**: Implements WCAG formula (L1+0.05)/(L2+0.05)
- **ExtractElementColor**: Screen capture-based color sampling
- **AnalyzeElementContrast**: Full element analysis with foreground/background detection
- **MeetsWcagAA/AAA**: Validation helpers for compliance checking

### Luminance Calculation

Implements WCAG 2.1 relative luminance formula:
```
Y = 0.2126 * R + 0.7152 * G + 0.0722 * B
```

With sRGB to linear RGB conversion:
```
if (c <= 0.03928) then c / 12.92
else ((c + 0.055) / 1.055) ^ 2.4
```

### Screen Capture Method

1. Get element bounding rectangle from FlaUI
2. Capture screenshot using `Graphics.CopyFromScreen`
3. Sample 9 points in grid pattern across element
4. Identify darkest (foreground) and lightest (background) colors
5. Calculate contrast ratio between extreme luminance values

### Limitations

- Color extraction is approximation (samples center/grid points)
- Cannot detect text color directly from automation properties
- Requires element to be on-screen and visible
- Performance limited by screen capture (tests limit to 10 elements)

## Running Tests

### Prerequisites

- Windows 10/11 (WinUI 3 requirement)
- .NET 9.0 Windows SDK
- FluentPDF.App built in Debug or Release mode

### Build

```bash
dotnet build tests/FluentPDF.E2E.Tests -p:Platform=x64
```

### Run All Accessibility Tests

```bash
dotnet test tests/FluentPDF.E2E.Tests -p:Platform=x64 --filter "FullyQualifiedName~AccessibilityTests"
```

### Run Specific Category

```bash
# Keyboard navigation only
dotnet test tests/FluentPDF.E2E.Tests -p:Platform=x64 --filter "Name~KeyboardNavigation"

# Screen reader only
dotnet test tests/FluentPDF.E2E.Tests -p:Platform=x64 --filter "Name~ScreenReader"

# Contrast ratios only
dotnet test tests/FluentPDF.E2E.Tests -p:Platform=x64 --filter "Name~ContrastRatio"
```

## Test Output

### Passing Test
```
[PASS] KeyboardNavigation_TabKey_NavigatesInLogicalOrder
  Duration: 1.2s
  All 15 focusable elements reachable via Tab key
```

### Failing Test (Example)
```
[FAIL] ContrastRatio_NormalText_MeetsWCAG_AA
  Duration: 2.3s

  Expected: contrastFailures to be empty
  Actual: 2 failures

  Details:
  - StatusText: 3.2:1 (minimum: 4.5:1, FG: #777777, BG: #FFFFFF)
  - CaptionLabel: 2.8:1 (minimum: 4.5:1, FG: #999999, BG: #FFFFFF)
```

## WCAG 2.1 Coverage

### Level A (Baseline)
- ✅ 2.1.1 Keyboard - All functionality keyboard accessible
- ✅ 4.1.2 Name, Role, Value - All elements properly labeled

### Level AA (Target)
- ✅ 1.4.3 Contrast (Minimum) - 4.5:1 text, 3:1 UI
- ✅ 2.4.7 Focus Visible - Focus indicators present
- ✅ 3.3.2 Labels or Instructions - Form fields labeled
- ⚠️ 1.4.11 Non-text Contrast - Automated with limitations
- ⚠️ 4.1.3 Status Messages - Toggle states exposed

### Level AAA (Enhanced)
- ⚠️ 1.4.6 Contrast (Enhanced) - 7:1 text, 4.5:1 large text
  - ColorContrastAnalyzer supports AAA validation
  - Not enforced by default tests

## Known Limitations

1. **Color Extraction**: Screen capture approximation, not pixel-perfect
2. **High Contrast**: Requires manual verification (can't programmatically enable)
3. **Screen Reader**: Tests automation properties, not actual narrator output
4. **Large Text Detection**: Estimates based on bounding box height
5. **Performance**: Limited to 10 elements per test for speed

## Future Enhancements

1. **Narrator API Integration**: Capture actual screen reader announcements
2. **Automated High Contrast**: Programmatically enable/disable HC mode
3. **Image Analysis**: More sophisticated color extraction with edge detection
4. **Touch Targets**: Validate 44x44px minimum touch target sizes
5. **Focus Order**: Validate logical reading order (not just presence)

## Resources

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI)
- [Windows Accessibility](https://docs.microsoft.com/en-us/windows/apps/design/accessibility/accessibility)
- [Contrast Ratio Calculator](https://webaim.org/resources/contrastchecker/)

## Compliance Checklist

Before release, verify:

- [ ] All keyboard navigation tests pass
- [ ] All screen reader tests pass
- [ ] Manual high contrast testing completed
- [ ] All contrast ratio tests pass (or exceptions documented)
- [ ] Focus indicators visible on all interactive elements
- [ ] Keyboard shortcuts documented in help
- [ ] Accessibility statement published
- [ ] Third-party accessibility audit (optional)

## Contact

For accessibility questions or to report issues:
- Open GitHub issue with `accessibility` label
- Email: accessibility@fluentpdf.dev (if configured)
- Refer to WCAG success criterion in reports
