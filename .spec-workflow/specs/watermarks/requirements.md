# Requirements Document

## Introduction

The Watermarks feature enables users to add text or image watermarks to PDF pages. Watermarks can be applied to all pages or selected pages, positioned anywhere on the page, and configured with opacity, rotation, and scaling. This is essential for document security, branding, and draft marking.

The feature provides:
- **Text Watermarks**: Add customizable text (e.g., "CONFIDENTIAL", "DRAFT")
- **Image Watermarks**: Add logo or stamp images
- **Batch Application**: Apply to all pages or selected range
- **Positioning**: Center, corners, or custom position
- **Styling**: Opacity, rotation, scale, color (text)

## Alignment with Product Vision

This spec adds professional document marking capabilities, enhancing FluentPDF's value for business users.

Supports product principles:
- **Quality Over Features**: High-quality watermark rendering
- **Local-First Processing**: All watermarking done locally
- **Transparency Above All**: Clear preview before applying

## Requirements

### Requirement 1: Text Watermark Creation

**User Story:** As a user, I want to add text watermarks, so that I can mark documents as confidential, draft, or with custom text.

#### Acceptance Criteria

1. WHEN user clicks "Add Watermark" THEN watermark dialog SHALL open
2. WHEN user selects "Text Watermark" THEN text input field SHALL appear
3. WHEN user enters text THEN preview SHALL update in real-time
4. WHEN text is entered THEN font family selector SHALL be available
5. WHEN text is entered THEN font size slider SHALL be available (12-144pt)
6. WHEN text is entered THEN color picker SHALL be available
7. WHEN text contains special characters THEN they SHALL render correctly

### Requirement 2: Image Watermark Creation

**User Story:** As a user, I want to add image watermarks, so that I can add logos or stamps to documents.

#### Acceptance Criteria

1. WHEN user selects "Image Watermark" THEN file picker SHALL open
2. WHEN image is selected THEN preview SHALL show on page
3. WHEN PNG with transparency is used THEN alpha channel SHALL be preserved
4. WHEN image is selected THEN scale slider SHALL be available (10%-200%)
5. WHEN image is oversized THEN it SHALL scale to fit page by default

### Requirement 3: Watermark Positioning

**User Story:** As a user, I want to position watermarks precisely, so that they appear where intended.

#### Acceptance Criteria

1. WHEN watermark dialog opens THEN position presets SHALL show: Center, Top-Left, Top-Right, Bottom-Left, Bottom-Right
2. WHEN preset is selected THEN preview SHALL update immediately
3. WHEN "Custom" position selected THEN X/Y coordinate inputs SHALL appear
4. WHEN coordinates entered THEN values SHALL be in percentage (0-100%)
5. WHEN position is set THEN it SHALL apply consistently to all target pages

### Requirement 4: Watermark Styling

**User Story:** As a user, I want to style watermarks, so that they suit my document's appearance.

#### Acceptance Criteria

1. WHEN watermark is configured THEN opacity slider SHALL be available (0-100%)
2. WHEN opacity is set THEN preview SHALL update showing transparency
3. WHEN watermark is configured THEN rotation input SHALL be available (-180° to 180°)
4. WHEN rotation is set THEN preview SHALL show rotated watermark
5. WHEN "Diagonal" preset is clicked THEN rotation SHALL set to 45°
6. WHEN "Behind content" is checked THEN watermark SHALL render behind page content
7. WHEN "Above content" is checked THEN watermark SHALL render above page content

### Requirement 5: Page Range Selection

**User Story:** As a user, I want to apply watermarks to specific pages, so that I can mark only relevant sections.

#### Acceptance Criteria

1. WHEN watermark dialog opens THEN page range selector SHALL appear
2. WHEN "All Pages" is selected THEN watermark SHALL apply to every page
3. WHEN "Current Page" is selected THEN watermark SHALL apply to active page only
4. WHEN "Page Range" is selected THEN range input SHALL appear (e.g., "1-5, 10, 15-20")
5. WHEN "Odd Pages" is selected THEN watermark SHALL apply to odd-numbered pages
6. WHEN "Even Pages" is selected THEN watermark SHALL apply to even-numbered pages
7. IF invalid range is entered THEN validation error SHALL show

### Requirement 6: Watermark Preview

**User Story:** As a user, I want to preview watermarks before applying, so that I can ensure correct appearance.

#### Acceptance Criteria

1. WHEN dialog is open THEN live preview SHALL show on current page
2. WHEN any setting changes THEN preview SHALL update within 100ms
3. WHEN page navigation occurs in preview THEN watermark SHALL show on new page
4. WHEN preview is satisfactory THEN "Apply" button SHALL finalize watermark
5. WHEN "Cancel" is clicked THEN no changes SHALL be made

### Requirement 7: Watermark Management

**User Story:** As a user, I want to manage existing watermarks, so that I can modify or remove them.

#### Acceptance Criteria

1. WHEN document has watermarks THEN "Remove Watermark" option SHALL be available
2. WHEN "Remove Watermark" is clicked THEN confirmation dialog SHALL appear
3. WHEN removal is confirmed THEN watermarks SHALL be removed from selected pages
4. WHEN watermarks are applied THEN HasUnsavedChanges SHALL become true

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: IWatermarkService handles watermark operations
- **Modular Design**: Separate dialog for watermark configuration
- **Clear Interfaces**: Commands for add, remove, configure

### Performance
- **Preview**: Update within 100ms of setting change
- **Apply**: < 500ms per page for watermark embedding
- **Batch**: Process 100 pages in < 10 seconds

### Security
- **Local Processing**: No watermark data sent externally
- **File Validation**: Verify image files before use

### Usability
- **Presets**: Quick options for common watermarks (CONFIDENTIAL, DRAFT, etc.)
- **Live Preview**: Real-time visual feedback
- **Batch Operations**: Efficient multi-page application
