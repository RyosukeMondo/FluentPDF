# Tasks Document: UI/UX Overhaul

## Phase 1: Theme System Fixes (Priority: Critical)

- [x] 1.1 Fix dialog theme resource issues
  - File: src/FluentPDF.App/Views/Dialogs/*.xaml
  - Replace Color resources with Brush resources (SystemErrorTextColor → SystemFillColorCriticalBrush)
  - Fixed: ErrorDialog, SaveConfirmationDialog, DeletePagesDialog
  - _Requirements: REQ-1_

- [x] 1.2 Remove hardcoded brushes from App.xaml
  - File: src/FluentPDF.App/App.xaml
  - Remove WhiteBrush/BlackBrush, use ThemeResource alternatives
  - Update PrimaryAction style to use TextOnAccentFillColorPrimaryBrush
  - _Requirements: REQ-1_

- [ ] 1.3 [NEXT] Create comprehensive theme resource dictionary
  **Action Steps**:
  1. Check if src/FluentPDF.App/Styles/ directory exists. If not: Create it
  2. Create file src/FluentPDF.App/Styles/ThemeResources.xaml
  3. Add ResourceDictionary root element
  4. Add Light theme colors (ThemeDictionary with Key="Light"):
     - PdfViewerBackgroundBrush (Brush, not Color)
     - AnnotationHighlightBrush
     - AnnotationUnderlineBrush
     - AnnotationStrikethroughBrush
     - ThumbnailBackgroundBrush
     - ThumbnailBorderBrush
     - BookmarkExpandedBrush
     - SearchHighlightBrush
     - (Add 12 total semantic brushes)
  5. Add Dark theme variant with same brush names, different values
  6. Reference in App.xaml: Add <ResourceDictionary Source="Styles/ThemeResources.xaml"/> to MergedDictionaries
  7. Build: dotnet build src/FluentPDF.App -p:Platform=x64
  8. Verify: Open App.xaml in IDE, verify ThemeResources.xaml shows in IntelliSense, no XAML errors
  - File: src/FluentPDF.App/Styles/ThemeResources.xaml (new)
  - _Requirements: REQ-1_

- [ ] 1.4 Add real-time system theme change detection
  - File: src/FluentPDF.App/App.xaml.cs
  - Subscribe to UISettings.ColorValuesChanged event
  - Update theme when system theme changes (when set to System)
  - _Requirements: REQ-1_

## Phase 2: Keyboard Navigation (Priority: High)

- [ ] 2.1 Implement standard keyboard shortcuts
  - File: src/FluentPDF.App/Views/MainWindow.xaml
  - Add KeyboardAccelerators for Ctrl+O, Ctrl+S, Ctrl+P, Ctrl+F, Ctrl+G
  - _Requirements: REQ-2_

- [ ] 2.2 Add page navigation shortcuts
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml
  - Page Up/Down for page scroll
  - Home/End for first/last page
  - +/- for zoom
  - _Requirements: REQ-2_

- [ ] 2.3 Add Escape key handling
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml.cs
  - Close search panel, dialogs, and overlays with Escape
  - _Requirements: REQ-2_

## Phase 3: Visual Consistency (Priority: High)

- [ ] 3.1 Add destructive button style
  - File: src/FluentPDF.App/Styles/ButtonStyles.xaml (new)
  - Create DestructiveButtonStyle with red/warning colors
  - Apply to Reset, Delete, and other destructive actions
  - _Requirements: REQ-6_

- [ ] 3.2 Standardize spacing and layout
  - File: src/FluentPDF.App/Views/*.xaml
  - Ensure consistent 8px grid spacing
  - Standardize margins and padding
  - _Requirements: REQ-6_

- [ ] 3.3 Add status bar
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml
  - Add bottom status bar with: page number, zoom %, modified indicator
  - Bind to ViewModel properties
  - _Requirements: REQ-7_

## Phase 4: Accessibility (Priority: High)

- [ ] 4.1 Audit and fix AutomationProperties
  - Files: All XAML files
  - Ensure all interactive elements have AutomationProperties.Name
  - Add AutomationProperties.HelpText for complex controls
  - _Requirements: REQ-3_

- [ ] 4.2 Verify focus indicators
  - Files: All XAML files
  - Ensure FocusVisualPrimaryBrush is visible on all focusable elements
  - Test keyboard navigation flow
  - _Requirements: REQ-3_

- [ ] 4.3 Test with Narrator
  - Manual testing with Windows Narrator
  - Verify all controls are announced correctly
  - Fix any missing or incorrect announcements
  - _Requirements: REQ-3_

## Phase 5: Loading States (Priority: Medium)

- [ ] 5.1 Add page loading skeleton
  - File: src/FluentPDF.App/Controls/PageLoadingSkeleton.xaml (new)
  - Create skeleton UI for page loading state
  - Show immediately while page renders
  - _Requirements: REQ-4_

- [ ] 5.2 Improve document loading feedback
  - File: src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs
  - Add LoadingProgress property (0-100)
  - Show progress bar for large documents
  - _Requirements: REQ-4_

## Phase 6: Responsive Layout (Priority: Medium)

- [ ] 6.1 Add adaptive triggers for sidebar
  - File: src/FluentPDF.App/Views/PdfViewerPage.xaml
  - Auto-collapse sidebar when window < 800px
  - Add VisualStateManager for responsive states
  - _Requirements: REQ-5_

- [ ] 6.2 Test on various screen sizes
  - Manual testing at 1280x720, 1920x1080, 2560x1440, 3840x2160
  - Verify layout adapts correctly
  - Fix any overflow or spacing issues
  - _Requirements: REQ-5_

## Verification Tasks

- [ ] V.1 Create theme verification test
  - Start app with --api-server
  - Switch themes via API or settings
  - Capture screenshots of each theme
  - Compare for theme consistency

- [ ] V.2 Create keyboard navigation test
  - Script that simulates keyboard inputs
  - Verify each shortcut triggers correct action
  - Report any missing shortcuts

- [ ] V.3 Accessibility audit
  - Run Accessibility Insights for Windows
  - Document any violations
  - Fix critical issues
