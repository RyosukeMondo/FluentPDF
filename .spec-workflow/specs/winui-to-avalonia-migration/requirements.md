# WinUI 3 to Avalonia Migration - Requirements

## Overview
Migrate FluentPDF from dual UI framework (WinUI 3 + Avalonia) to Avalonia-only architecture, consolidating shared logic in Core and eliminating 11,000+ lines of duplicate code.

## Business Goals
- **Single UI Framework**: Avalonia-only for true cross-platform support (Windows, macOS, Linux)
- **Code Consolidation**: Move shared logic to Core, eliminate duplication
- **Feature Parity**: All WinUI 3 features ported to Avalonia
- **Clean Architecture**: Proper separation of concerns (Core, Rendering, UI)

## Functional Requirements

### FR1: Feature Parity
- FR1.1: All text-selection-annotations features in Avalonia (H/U/S shortcuts, coordinate mapping, bounds extraction)
- FR1.2: All view modes working (single page, continuous scroll, two-page)
- FR1.3: All annotation tools (highlight, underline, strikethrough, shapes, stamps)
- FR1.4: All document operations (merge, split, rotate, delete pages)
- FR1.5: All form filling features
- FR1.6: Watermark and stamp functionality
- FR1.7: Theme system and liquid glass UI effects
- FR1.8: Search and text extraction
- FR1.9: Bookmarks and thumbnails panels
- FR1.10: Settings and preferences

### FR2: Architecture Consolidation
- FR2.1: ViewModels moved to FluentPDF.Core (shared between any future UI)
- FR2.2: Services remain in Core/Rendering layers
- FR2.3: UI-specific code only in FluentPDF.Avalonia
- FR2.4: No duplicate business logic between UI layers
- FR2.5: Dependency injection properly configured

### FR3: WinUI 3 Removal
- FR3.1: FluentPDF.App project deleted
- FR3.2: All WinUI 3 references removed from solution
- FR3.3: Build scripts updated for Avalonia only
- FR3.4: Documentation updated
- FR3.5: Tests migrated/updated for Avalonia

### FR4: Cross-Platform Support
- FR4.1: Avalonia app runs on Windows
- FR4.2: Avalonia app runs on Linux (tested)
- FR4.3: Avalonia app runs on macOS (tested if possible)
- FR4.4: Platform-specific features properly abstracted

## Non-Functional Requirements

### NFR1: Performance
- NFR1.1: Rendering performance ≥ WinUI 3 version (<500ms page render)
- NFR1.2: Text extraction <100ms
- NFR1.3: Annotation creation <50ms
- NFR1.4: Startup time <3 seconds

### NFR2: Code Quality
- NFR2.1: 80%+ test coverage maintained
- NFR2.2: No code duplication between layers
- NFR2.3: SOLID principles followed
- NFR2.4: All async operations properly handled

### NFR3: Maintainability
- NFR3.1: Single codebase to maintain (Avalonia only)
- NFR3.2: Clear separation of concerns
- NFR3.3: Comprehensive documentation
- NFR3.4: CI/CD updated for Avalonia

## Success Criteria

1. **Feature Complete**: All WinUI 3 features working in Avalonia
2. **Tests Pass**: All unit, integration, and E2E tests passing
3. **Build Clean**: Solution builds without errors on Windows, Linux
4. **WinUI Removed**: FluentPDF.App project completely deleted
5. **Performance Met**: All NFR performance targets achieved
6. **Documentation**: README, UAT guides updated for Avalonia

## Out of Scope
- New features beyond WinUI 3 parity
- Mobile platforms (iOS/Android)
- WASM/Browser deployment

## Constraints
- Must maintain backward compatibility for PDF files and saved state
- Must not break existing PDFium integration
- Must preserve all test coverage
