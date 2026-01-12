# Requirements Document

## Introduction

The Image Insertion feature enables users to add images (PNG, JPEG, BMP) to PDF pages. Images can be positioned, scaled, and rotated on the page. This uses PDFium's content editing APIs to embed images directly into the PDF structure.

The feature provides:
- **Image Import**: Add images from file system to PDF pages
- **Position & Scale**: Place and resize images on the page
- **Rotation**: Rotate images in 90° increments
- **Multiple Formats**: Support PNG, JPEG, BMP, GIF image formats

## Alignment with Product Vision

This spec extends FluentPDF's editing capabilities to include content insertion, moving beyond annotations to actual PDF content modification.

Supports product principles:
- **Quality Over Features**: High-quality image embedding with proper compression
- **Local-First Processing**: All image processing happens locally
- **Standards Compliance**: Follows PDF image embedding standards

## Requirements

### Requirement 1: Image Import

**User Story:** As a user, I want to add images to PDF pages, so that I can enhance documents with visual content.

#### Acceptance Criteria

1. WHEN user clicks "Insert Image" button THEN file picker SHALL open with image filters
2. WHEN user selects image file THEN image SHALL appear on current page
3. WHEN image is inserted THEN it SHALL appear at center of visible area
4. WHEN image is inserted THEN it SHALL have selection handles
5. WHEN PNG with transparency is inserted THEN transparency SHALL be preserved
6. WHEN JPEG is inserted THEN quality SHALL be preserved (no recompression)
7. IF image file is corrupted THEN error SHALL display with reason

### Requirement 2: Image Positioning

**User Story:** As a user, I want to position images precisely, so that I can place them where needed.

#### Acceptance Criteria

1. WHEN image is selected THEN drag SHALL move image to new position
2. WHEN image is dragged THEN real-time preview SHALL show new position
3. WHEN image is positioned THEN coordinates SHALL be in PDF points
4. WHEN image is moved outside page bounds THEN it SHALL clip to page edges
5. WHEN arrow keys pressed with selection THEN image SHALL nudge by 1 point
6. WHEN Shift+arrow pressed THEN image SHALL nudge by 10 points

### Requirement 3: Image Scaling

**User Story:** As a user, I want to resize images, so that I can fit them appropriately on the page.

#### Acceptance Criteria

1. WHEN image is selected THEN resize handles SHALL appear at corners
2. WHEN corner handle is dragged THEN image SHALL scale proportionally
3. WHEN Shift is held during resize THEN aspect ratio lock SHALL be disabled
4. WHEN image is scaled THEN minimum size SHALL be 10x10 points
5. WHEN image is scaled larger than page THEN it SHALL clip to page bounds
6. WHEN scaling completes THEN image quality SHALL be preserved

### Requirement 4: Image Rotation

**User Story:** As a user, I want to rotate images, so that I can correct orientation.

#### Acceptance Criteria

1. WHEN image is selected THEN rotation handle SHALL appear above image
2. WHEN rotation handle is dragged THEN image SHALL rotate in real-time
3. WHEN right-click menu shows THEN "Rotate 90° Right/Left" options SHALL appear
4. WHEN rotation completes THEN image SHALL maintain center position
5. WHEN image is rotated THEN quality SHALL be preserved

### Requirement 5: Image Management

**User Story:** As a user, I want to manage inserted images, so that I can modify or remove them.

#### Acceptance Criteria

1. WHEN image is selected and Delete pressed THEN image SHALL be removed
2. WHEN image is right-clicked THEN context menu SHALL show Delete option
3. WHEN image is selected THEN properties panel SHALL show dimensions
4. WHEN multiple images overlap THEN most recent SHALL be on top
5. WHEN page is saved THEN all inserted images SHALL persist

### Requirement 6: Supported Formats

**User Story:** As a user, I want to insert various image formats, so that I can use my existing images.

#### Acceptance Criteria

1. WHEN PNG file is selected THEN it SHALL be imported with alpha channel
2. WHEN JPEG file is selected THEN it SHALL be imported with original quality
3. WHEN BMP file is selected THEN it SHALL be converted to embedded format
4. WHEN GIF file is selected THEN first frame SHALL be imported
5. IF unsupported format is selected THEN error SHALL show supported formats

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility**: IImageInsertionService handles image embedding
- **Modular Design**: Separate UI control for image manipulation overlay
- **Clear Interfaces**: Commands for insert, delete, transform

### Performance
- **Import**: < 2 seconds for images up to 10MB
- **Rendering**: Inserted images render at 60 FPS
- **Memory**: Images compressed in PDF, not stored as raw bitmaps

### Security
- **File Validation**: Verify image file integrity before import
- **Size Limits**: Warn for images larger than 50MB

### Usability
- **Visual Handles**: Clear selection and resize handles
- **Cursor Feedback**: Cursor changes during operations
- **Undo Support**: Image operations are reversible until saved
