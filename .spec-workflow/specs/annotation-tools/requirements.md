# Requirements Document

## Introduction

The Annotation Tools implements PDF annotation capabilities using PDFium's annotation API. This spec enables users to add highlights, underlines, text comments, sticky notes, and drawing annotations to PDF documents, with lossless save back to PDF format.

The annotation system enables:
- **Text Markup**: Highlighting, underline, strikethrough annotations
- **Text Comments**: Add text comments and sticky notes
- **Drawing Tools**: Rectangle, circle, freehand drawing annotations
- **Annotation Save**: Save annotations to PDF with lossless quality
- **Annotation Editing**: Modify and delete existing annotations

## Alignment with Product Vision

This spec transforms FluentPDF from viewer to editor, adding professional annotation capabilities.

Supports product principles:
- **Quality Over Features**: Uses PDFium's proven annotation API, lossless save
- **Standards Compliance**: PDF annotation standards (ISO 32000)
- **Verifiable Architecture**: Testable annotation services
- **Privacy-First**: All processing local, no cloud uploads

## Requirements

### Requirement 1: PDFium Annotation API Integration

**User Story:** As a developer, I want PDFium annotation API integrated, so that I can create and modify PDF annotations.

#### Acceptance Criteria

1. WHEN adding highlight THEN it SHALL use FPDFPage_CreateAnnot with FPDF_ANNOT_HIGHLIGHT
2. WHEN adding text comment THEN it SHALL use FPDF_ANNOT_TEXT
3. WHEN adding drawing THEN it SHALL use FPDF_ANNOT_SQUARE, FPDF_ANNOT_CIRCLE, or FPDF_ANNOT_INK
4. WHEN setting annotation color THEN it SHALL use FPDFAnnot_SetColor
5. WHEN setting annotation rectangle THEN it SHALL use FPDFAnnot_SetRect
6. WHEN deleting annotation THEN it SHALL use FPDFPage_RemoveAnnot
7. WHEN loading annotations THEN it SHALL use FPDFPage_GetAnnotCount and FPDFPage_GetAnnot

### Requirement 2: Text Markup Annotations

**User Story:** As a user, I want to highlight text, so that I can mark important sections.

#### Acceptance Criteria

1. WHEN user selects "Highlight Tool" THEN cursor SHALL change to crosshair
2. WHEN user drags over text THEN a yellow highlight SHALL appear
3. WHEN highlight is created THEN it SHALL be selectable
4. WHEN highlight is selected THEN color picker SHALL allow changing color
5. WHEN highlight is saved THEN it SHALL persist to PDF file
6. WHEN user selects "Underline Tool" THEN dragging SHALL create underline annotation
7. WHEN user selects "Strikethrough Tool" THEN dragging SHALL create strikethrough

### Requirement 3: Text Comments and Sticky Notes

**User Story:** As a user, I want to add text comments, so that I can annotate documents.

#### Acceptance Criteria

1. WHEN user clicks "Add Comment" THEN a comment dialog SHALL appear
2. WHEN user enters text and clicks OK THEN a comment annotation SHALL be created
3. WHEN comment is created THEN a sticky note icon SHALL appear on the page
4. WHEN user clicks sticky note THEN comment text SHALL display in tooltip
5. WHEN user double-clicks sticky note THEN comment SHALL be editable
6. WHEN comment is saved THEN it SHALL persist to PDF

### Requirement 4: Drawing Annotations

**User Story:** As a user, I want to draw shapes, so that I can mark areas of interest.

#### Acceptance Criteria

1. WHEN user selects "Rectangle Tool" THEN dragging SHALL create rectangle annotation
2. WHEN rectangle is created THEN it SHALL have stroke color and optional fill
3. WHEN user selects "Circle Tool" THEN dragging SHALL create circle annotation
4. WHEN user selects "Freehand Tool" THEN dragging SHALL create ink annotation
5. WHEN drawing annotation is selected THEN color/width can be changed
6. WHEN drawing is saved THEN it SHALL persist to PDF

### Requirement 5: Annotation Save and Persistence

**User Story:** As a user, I want annotations saved to PDF, so that they're preserved when sharing.

#### Acceptance Criteria

1. WHEN user clicks "Save" THEN all annotations SHALL be written to PDF file
2. WHEN saving THEN original file SHALL be backed up (filename.pdf.bak)
3. WHEN save completes THEN file SHALL be losslessly updated (no recompression)
4. WHEN reopening file THEN all annotations SHALL load correctly
5. WHEN save fails THEN backup SHALL be restored and error shown
6. IF file is read-only THEN "Save As" dialog SHALL appear

### Requirement 6: Annotation Editing and Management

**User Story:** As a user, I want to edit and delete annotations, so that I can correct mistakes.

#### Acceptance Criteria

1. WHEN user clicks annotation THEN it SHALL be selected with handles
2. WHEN annotation is selected THEN Delete key SHALL remove it
3. WHEN annotation is dragged THEN it SHALL move to new position
4. WHEN annotation handles are dragged THEN it SHALL resize
5. WHEN properties panel is open THEN color, opacity, stroke width can be changed
6. WHEN changes are made THEN they SHALL apply immediately

### Requirement 7: Performance and Resource Management

**User Story:** As a user, I want annotation tools to be responsive, so that marking up documents is smooth.

#### Acceptance Criteria

1. WHEN creating annotation THEN it SHALL render in < 100ms
2. WHEN page has 50 annotations THEN rendering SHALL complete in < 500ms
3. WHEN saving annotations THEN it SHALL complete in < 2 seconds
4. WHEN disposing document THEN all annotation handles SHALL be freed
5. IF memory is low THEN annotation rendering SHALL degrade gracefully

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: Separate annotation service, annotation model, annotation UI
- **Modular Design**: IAnnotationService independently testable

### Performance
- **Instant Feedback**: Annotation creation renders immediately
- **Efficient Storage**: Lossless PDF updates

### Security
- **Input Validation**: Validate annotation data
- **File Backup**: Preserve original before save

### Usability
- **Intuitive Tools**: Standard annotation toolbar
- **Keyboard Shortcuts**: Delete, Esc to deselect
