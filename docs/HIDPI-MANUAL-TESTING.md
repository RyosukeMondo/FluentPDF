# HiDPI Manual Testing Guide

This document provides a comprehensive manual testing guide for validating FluentPDF's HiDPI display scaling support across different hardware configurations.

## Overview

FluentPDF supports automatic HiDPI scaling with dynamic quality adjustment. This testing guide ensures the feature works correctly across different display configurations and scaling levels.

## Test Requirements

### Hardware Setup

You will need access to the following display configurations:

1. **Standard 1080p Monitor** (1920x1080)
   - Windows scaling: 100%
   - Effective DPI: 96

2. **4K Monitor** (3840x2160)
   - Windows scaling: 150%
   - Effective DPI: 144

3. **Surface Pro or similar high-DPI device**
   - Windows scaling: 200%
   - Effective DPI: 192

4. **Multi-monitor Setup**
   - At least 2 monitors with different scaling factors
   - E.g., 1080p @ 100% + 4K @ 150%

### Software Requirements

- Windows 10/11 with latest updates
- FluentPDF built in Debug mode for diagnostics
- Sample PDF files of varying complexity:
  - Simple text document (1-5 pages)
  - Image-heavy document (10+ pages with photos)
  - Complex layout document (charts, tables, mixed content)

## Test Matrix

Test each quality level on each display configuration:

| Display Config | Quality Level | Expected DPI | Expected Behavior |
|----------------|---------------|--------------|-------------------|
| 1080p @ 100%   | Auto          | 96           | Sharp text, fast rendering |
| 1080p @ 100%   | Low           | 72           | Slightly blurry, very fast |
| 1080p @ 100%   | Medium        | 96           | Standard quality |
| 1080p @ 100%   | High          | 144          | Enhanced quality |
| 1080p @ 100%   | Ultra         | 192          | Maximum quality |
| 4K @ 150%      | Auto          | 144          | Sharp text, good performance |
| 4K @ 150%      | Low           | 72           | Noticeably lower quality |
| 4K @ 150%      | Medium        | 96           | Acceptable quality |
| 4K @ 150%      | High          | 144          | Native quality |
| 4K @ 150%      | Ultra         | 192          | Enhanced quality |
| Surface @ 200% | Auto          | 192          | Very sharp text |
| Surface @ 200% | Low           | 72           | Poor quality (should warn) |
| Surface @ 200% | Medium        | 96           | Lower than native |
| Surface @ 200% | High          | 144          | Good quality |
| Surface @ 200% | Ultra         | 192          | Native quality |

## Test Procedures

### Test 1: Initial DPI Detection

**Objective:** Verify that FluentPDF correctly detects display DPI on startup.

**Steps:**
1. Launch FluentPDF on each display configuration
2. Open a PDF document
3. Check the detected DPI (should be visible in diagnostics/logs)

**Expected Results:**
- 1080p @ 100%: Detected DPI = 96
- 4K @ 150%: Detected DPI = 144
- Surface @ 200%: Detected DPI = 192

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 2: Quality Settings

**Objective:** Verify that users can change rendering quality from Settings.

**Steps:**
1. Navigate to Settings page
2. Locate "Rendering Quality" section
3. Try changing quality level from Auto to each option:
   - Low
   - Medium
   - High
   - Ultra
4. Click "Apply" after each change
5. Return to PDF viewer

**Expected Results:**
- ComboBox shows all 5 quality options with descriptions
- Ultra quality shows performance warning on low-end devices
- Changing quality triggers immediate re-render
- PDF appearance changes according to quality level

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 3: Window Movement Between Displays

**Objective:** Verify smooth DPI transitions when moving windows between monitors.

**Setup:** Multi-monitor configuration with different scaling levels

**Steps:**
1. Open a PDF on Monitor 1 (e.g., 1080p @ 100%)
2. Note the rendering quality and sharpness
3. Drag the FluentPDF window to Monitor 2 (e.g., 4K @ 150%)
4. Observe the transition
5. Wait 1-2 seconds for re-render
6. Drag back to Monitor 1

**Expected Results:**
- Window movement is smooth without crashes
- PDF automatically re-renders within 1-2 seconds after movement stops
- Quality adjusts to match new display DPI
- Text remains sharp and readable on both displays
- No visual artifacts or corruption

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 4: Dynamic DPI Change (Scaling Change)

**Objective:** Verify that FluentPDF responds to Windows scaling changes.

**Steps:**
1. Open a PDF in FluentPDF
2. Go to Windows Settings > Display
3. Change the scaling factor (e.g., from 100% to 150%)
4. Return to FluentPDF (do not close/reopen)
5. Observe the rendering

**Expected Results:**
- FluentPDF detects the DPI change within 1-2 seconds
- PDF automatically re-renders at new DPI
- "Adjusting quality..." overlay appears briefly during re-render
- Rendering quality matches new scaling factor
- No crashes or freezes

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 5: Performance Testing

**Objective:** Verify acceptable performance at different DPI levels.

**Setup:** Use a 20-page PDF with mixed content

**Steps:**
1. Set quality to Auto
2. For each display configuration:
   - Open the test PDF
   - Time the initial render of first page
   - Navigate through all pages
   - Note any lag or stuttering
3. Repeat with Ultra quality

**Expected Results:**
- Auto quality on any display: < 1 second per page
- Ultra quality on 1080p: < 2 seconds per page
- Ultra quality on 4K/Surface: < 3 seconds per page
- Smooth scrolling and navigation
- No UI freezing during renders

**Performance Measurements:**

| Display Config | Quality | First Page (ms) | Avg Page (ms) | Pass/Fail |
|----------------|---------|-----------------|---------------|-----------|
| 1080p @ 100%   | Auto    |                 |               |           |
| 1080p @ 100%   | Ultra   |                 |               |           |
| 4K @ 150%      | Auto    |                 |               |           |
| 4K @ 150%      | Ultra   |                 |               |           |
| Surface @ 200% | Auto    |                 |               |           |
| Surface @ 200% | Ultra   |                 |               |           |

**Notes:**
```


```

---

### Test 6: Memory Usage

**Objective:** Verify that high DPI rendering doesn't cause excessive memory usage.

**Setup:** Use Task Manager to monitor FluentPDF memory usage

**Steps:**
1. Note baseline memory usage with no PDF open
2. Open a complex 50-page PDF
3. Set quality to Ultra
4. Navigate through several pages
5. Monitor peak memory usage
6. Open multiple PDFs simultaneously

**Expected Results:**
- Baseline memory: < 100 MB
- Single PDF at Ultra quality: < 500 MB on 1080p, < 1 GB on 4K
- No memory leaks (memory stabilizes, doesn't continuously grow)
- Out-of-memory handling triggers fallback to lower DPI if needed

**Memory Measurements:**

| Scenario | Baseline | Single PDF | Multiple PDFs | Pass/Fail |
|----------|----------|------------|---------------|-----------|
| 1080p Auto |        |            |               |           |
| 1080p Ultra |       |            |               |           |
| 4K Auto    |        |            |               |           |
| 4K Ultra   |        |            |               |           |

**Notes:**
```


```

---

### Test 7: Quality Visual Comparison

**Objective:** Verify visible quality differences between settings.

**Setup:** Open a PDF with small text and detailed images

**Steps:**
1. For each display configuration:
   - View the same page at Low quality
   - Take a screenshot or note appearance
   - Switch to Medium, then High, then Ultra
   - Compare visual quality

**Expected Results:**
- Clear visual progression from Low to Ultra
- Low quality: Visible pixelation on text
- Medium quality: Acceptable text, some image softness
- High quality: Sharp text, good image detail
- Ultra quality: Maximum sharpness, no visible pixelation
- Auto quality: Matches display DPI appropriately

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 8: Edge Cases

**Objective:** Test unusual scenarios and error conditions.

**Steps:**
1. **Very Large PDF:** Open 500+ page PDF at Ultra quality
2. **Rapid Quality Changes:** Change quality 10 times rapidly
3. **Window Resize:** Resize window while rendering
4. **System Sleep:** Put system to sleep while rendering, wake up
5. **Display Disconnect:** Disconnect external monitor while window is on it
6. **Extreme Scaling:** Try custom Windows scaling (e.g., 350% if supported)

**Expected Results:**
- Large PDFs render without crashes (may be slow)
- Rapid quality changes don't cause crashes or UI freezes
- Window resize doesn't corrupt rendering
- System sleep/wake maintains correct DPI
- Display disconnect moves window gracefully
- Extreme scaling is clamped to valid DPI range (50-576 DPI)

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 9: Settings Persistence

**Objective:** Verify that quality settings persist across app restarts.

**Steps:**
1. Open FluentPDF
2. Change quality to High
3. Close FluentPDF completely
4. Reopen FluentPDF
5. Check Settings page
6. Open a PDF

**Expected Results:**
- Quality setting is still "High" after restart
- PDF renders at High quality automatically
- Settings persist even after system reboot

**Pass/Fail:** ___________

**Notes:**
```


```

---

### Test 10: Accessibility and Usability

**Objective:** Ensure HiDPI features are user-friendly.

**Steps:**
1. Check that quality descriptions are clear
2. Verify that "Auto" is the default and recommended
3. Ensure performance warnings appear for Ultra on low-end devices
4. Check that quality adjustment feedback is visible but not intrusive
5. Verify keyboard navigation works in Settings

**Expected Results:**
- Clear, non-technical descriptions for each quality level
- "Auto (Recommended)" is default
- Warning text is visible but not alarming
- "Adjusting quality..." overlay is subtle and brief
- All settings accessible via keyboard

**Pass/Fail:** ___________

**Notes:**
```


```

---

## Issue Reporting

If you encounter any issues during testing, please document:

### Issue Template

```markdown
**Issue ID:** HIDPI-###
**Display Configuration:** [e.g., 4K @ 150%]
**Quality Setting:** [e.g., Ultra]
**Test:** [e.g., Test 3 - Window Movement]

**Description:**
[Detailed description of the issue]

**Steps to Reproduce:**
1.
2.
3.

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happened]

**Screenshots/Videos:**
[Attach if applicable]

**System Info:**
- Windows Version:
- FluentPDF Version:
- Display Info:
- Hardware:

**Severity:** [Critical / High / Medium / Low]
```

## Sign-Off

Once all tests are complete:

**Tester Name:** ___________________________

**Date:** ___________________________

**Overall Result:** [ ] PASS  [ ] FAIL  [ ] PASS WITH ISSUES

**Summary:**
```




```

**Recommendation:**
- [ ] Approve for release
- [ ] Requires fixes before release
- [ ] Requires further testing

---

## Automated Test Coverage

Note: The following aspects are already covered by automated tests:

- ✅ DPI detection algorithm (unit tests)
- ✅ DPI clamping (50-576 DPI range)
- ✅ Observable DPI change events
- ✅ Settings persistence
- ✅ Rendering at specific DPI values
- ✅ Out-of-memory handling
- ✅ Performance benchmarks

This manual testing guide focuses on real-world hardware scenarios that cannot be automated.
