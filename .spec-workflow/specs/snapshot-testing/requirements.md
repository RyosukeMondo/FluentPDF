# Requirements Document

## Introduction

Add Verify.Xaml snapshot testing to FluentPDF, enabling golden master testing for UI components. This fills the gap identified in steering documents (tech.md specifies Verify.Xaml for snapshot testing).

## Alignment with Product Vision

Supports product.md goal of "Verifiable Quality Architecture" and "Quality Over Features" by detecting unintended UI changes through automated snapshot comparison.

## Requirements

### Requirement 1: Verify.Xaml Infrastructure

**User Story:** As a developer, I want Verify.Xaml configured in the test project, so that I can write snapshot tests for UI components.

#### Acceptance Criteria

1. WHEN building test project THEN the system SHALL include Verify and Verify.Xaml packages
2. WHEN running snapshot tests THEN the system SHALL compare against approved snapshots
3. IF snapshot differs THEN the system SHALL generate diff report for review

### Requirement 2: Snapshot Test Base Class

**User Story:** As a developer, I want a base class for snapshot tests, so that I can easily create new snapshot tests.

#### Acceptance Criteria

1. WHEN creating a snapshot test THEN developer SHALL extend SnapshotTestBase
2. IF no approved snapshot exists THEN the system SHALL create initial snapshot for approval
3. WHEN snapshot is approved THEN the system SHALL store it in Snapshots directory

### Requirement 3: Core Component Snapshots

**User Story:** As a developer, I want snapshot tests for core UI components, so that visual regressions are detected.

#### Acceptance Criteria

1. WHEN running tests THEN the system SHALL verify PdfViewerControl appearance
2. WHEN running tests THEN the system SHALL verify toolbar appearance
3. IF component appearance changes THEN test SHALL fail until snapshot is updated

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: Each snapshot test file covers one component
- **Modular Design**: Base class handles Verify configuration, tests focus on scenarios

### Performance
- Snapshot comparison SHALL complete within 5 seconds per test
- Snapshots stored as compact format (not full bitmaps when possible)

### Reliability
- Snapshots SHALL be deterministic across runs
- Tests SHALL not depend on system theme or DPI
