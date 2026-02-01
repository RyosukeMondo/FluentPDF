# Text Selection and Annotation Tools - Requirements

## Overview
Implement functional text selection with coordinate-based extraction and wire up annotation tools (highlight, underline, strikethrough) to create annotations from selected text.

## Problem Statement
Currently:
- Continuous scroll mode works but may not be obvious to users
- Annotation tool buttons click but don't create annotations
- Text selection extracts all page text instead of selected region
- No visual feedback when annotation tools are active

## Functional Requirements

### FR1: Text Selection with Coordinate Extraction
- **FR1.1**: Extract text within selection rectangle bounds (not entire page)
- **FR1.2**: Convert mouse coordinates to PDF coordinates
- **FR1.3**: Use PDFium text page APIs to get text in bounds
- **FR1.4**: Support text across multiple lines
- **FR1.5**: Visual selection rectangle with proper coordinates

### FR2: Annotation Tool Integration
- **FR2.1**: When highlight tool is active, create yellow highlight on selected text
- **FR2.2**: When underline tool is active, create underline on selected text
- **FR2.3**: When strikethrough tool is active, create strikethrough on selected text
- **FR2.4**: Visual cursor feedback when tool is active (crosshair or custom cursor)
- **FR2.5**: Tool remains active until clicked again or another tool selected
- **FR2.6**: Annotation appears immediately on canvas after creation

### FR3: Continuous Scroll Mode Enhancement
- **FR3.1**: Ensure continuous scroll loads when mode is toggled
- **FR3.2**: Update page indicator as user scrolls
- **FR3.3**: Support text selection in continuous scroll mode
- **FR3.4**: Support annotations in continuous scroll mode
- **FR3.5**: Performance optimization - render only visible pages

### FR4: User Experience
- **FR4.1**: Clear visual indication of active annotation tool
- **FR4.2**: Tooltip showing "Select text to highlight" when tool active
- **FR4.3**: Undo/redo support for annotation creation
- **FR4.4**: Keyboard shortcuts: H for highlight, U for underline, S for strikethrough
- **FR4.5**: Status bar message showing tool state

## Non-Functional Requirements

### NFR1: Performance
- Text extraction within 100ms for typical selection
- Annotation creation within 50ms
- Continuous scroll renders visible pages within 500ms

### NFR2: Accuracy
- Text extraction must include all characters within bounds
- Coordinate mapping accuracy within 1px
- Annotation positioning matches text bounds precisely

### NFR3: Usability
- Intuitive tool selection with toggle behavior
- Clear visual feedback at all stages
- Graceful handling of no-text selections

## Success Criteria
1. ✅ User can select text region and see only selected text extracted
2. ✅ Clicking highlight tool + selecting text creates yellow highlight annotation
3. ✅ Annotation appears on canvas immediately after creation
4. ✅ Tool stays active until user deselects it
5. ✅ Continuous scroll mode displays all pages with smooth scrolling
6. ✅ Text selection works in both single page and continuous scroll modes

## Out of Scope
- OCR for scanned PDFs (text must exist in PDF)
- Complex annotation types (polygons, stamps)
- Annotation editing (move, resize)
- Multi-page selection
- Right-to-left text support
