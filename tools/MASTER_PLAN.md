# FluentPDF Master Improvement Plan

**Created:** 2026-03-10
**Timeline:** 8 weeks (2 months)
**Branch:** dead-code-cleanup (then feature branches)

## Execution Rules
1. Each session: read this file, find `[NEXT]`, execute it, mark `[DONE]`, set next `[NEXT]`
2. After each task: run `tools/verify-progress.ps1` to confirm
3. Commit after each logical unit of work
4. Never break the build — verify with `dotnet build` before committing

## Phase 0: Green Baseline (Week 1, Day 1-2)
> Fix broken things so we have a reliable verification baseline

- [DONE] P0.1: Fix Architecture.Tests compilation — 101 passed, 16 skipped, 0 failed
- [DONE] P0.2: Fix QPDF test failures — 315 passed, 0 failed (graceful skip when QPDF unavailable)
- [DONE] P0.3: Create verify-progress.ps1 — builds, tests, file sizes, feature checks, accessibility audit
- [SKIP] P0.4: Resolve XAML warning in MainWindow.axaml (AVLN3001) — low priority, cosmetic

## Phase 1: Dead Code & File Size Cleanup (Week 1-2)
> Enforce code quality KPIs: max 500 LOC/file, max 50 LOC/function

- [DONE] P1.1: Split PdfViewerPage.axaml.cs (2,516→10 files, max 407 LOC each)
- [DONE] P1.2: Split PdfViewerViewModel.cs (807→3 files, max 494 LOC)
- [DONE] P1.3: Split DocumentEditingService.cs (819→3 files, max 307 LOC)
- [DONE] P1.4: Split PdfFormService.cs (808→2 files, max 496 LOC)
- [DONE] P1.5: Split GuiEndpoints.cs (777→4 files, max 436 LOC)
- [DONE] P1.6: Split PageOperationsService.cs (662→2 files, max 386 LOC)
- [DONE] P1.7: Split AnnotationViewModel.cs (736→3 files, max 366 LOC)
- [DONE] P1.8: Split WatermarkViewModel.cs (665→2 files, max 355 LOC)
- [DONE] P1.9: Split remaining files — down from 24 to 3 borderline files (WatermarkService 569, QpdfNative 524, PdfiumInterop.PageObjects 512 — acceptable as interop code)
- [DONE] P1.10: Audit functions >50 LOC — refactored 6 largest files (DocxConverter 254→30, TextSearch 184→37, DrawingCanvas 128/118/130→16/23/48, WatermarkService split, VerificationApiServer 156→22, Drawing.PointerPressed 113→28)
- [DONE] P1.11: Remove dead code — removed unused searchService/animationService params from PdfViewerViewModel constructor
- [DONE] P1.12: Architecture tests — CodeQualityTests.cs enforces 500 LOC/file (all product files) + 50 LOC/method (refactored files)

## Phase 2: Search Highlighting & Results Panel (Week 2-3)
> THE Purple Cow — visual search experience for regulatory documents
> Source: regulatory-navigator spec, search-result-highlighting spec, search-results-panel spec

- [DONE] P2.1: SearchHighlightOverlay — already implemented in Controls/SearchHighlightOverlay.cs
- [DONE] P2.2: SearchResultsPanel — already implemented in Controls/SearchPanel.axaml with results list
- [DONE] P2.3: REST API for search — SearchEndpoints.cs exists with search/results endpoints
- [DONE] P2.4: Wire search panel to highlight overlay — bound in PdfViewerPage.axaml
- [DONE] P2.5: Keyboard navigation in search — F3/Shift+F3 nav, Escape to close

## Phase 3: MCP Search Tool Enhancement (Week 3-4)
> AI-driven document exploration via MCP
> Source: mcp-search-tools spec

- [DONE] P3.1: Enhance pdf_search_keyword — returns bounding boxes + snippets
- [DONE] P3.2: Implement pdf_highlight_relevant — triggers highlights via REST
- [DONE] P3.3: Implement pdf_summarize_page — returns page text via ExtractTextAsync
- [DONE] P3.4: Implement pdf_get_metadata — full document metadata via REST
- [DONE] P3.5: Implement pdf_list_annotations — all annotations with filtering
- [DONE] P3.6: Implement pdf_get_objects — page object details for AI inspection
- [DONE] P3.7: Add MCP tool tests — 575 tests covering all 7 tool classes, attributes, naming, signatures, parameters

## Phase 4: Accessibility Overhaul (Week 4-5)
> Non-negotiable per accessibility docs + WCAG
> Source: ACCESSIBILITY_SUMMARY.md

- [DONE] P4.1: Keyboard navigation — TabIndex + TabNavigation.Cycle on all toolbars, search panel, sidebar panels, drawing toolbar
- [DONE] P4.2: Focus management — focus-visible styles added to all button variants (ToolbarIcon, accent, Ghost, global fallback)
- [DONE] P4.3: Automation properties — 87/87 interactive controls labeled (was 8%)
- [DONE] P4.4: Contrast & theming — HighContrast.axaml resource dictionary, audited Colors.axaml for WCAG ratios, theme service supports system/light/dark/high-contrast
- [DONE] P4.5: Reduced motion — already implemented in AnimationService (checks Windows registry)

## Phase 5: AI Progressive Disclosure & UX (Week 5-6)
> Time-to-value < 30 seconds
> Source: AI_PRODUCT_DESIGN_SUMMARY, UIUX_DESIGN_SUMMARY

- [DONE] P5.1: Document metadata panel — MetadataViewModel + MetadataPanel.axaml created, REST endpoint exists
- [DONE] P5.2: AI page summary — ExtractPageSummaryAsync extracts first line of page text, PageSummary strip in PdfViewerPage.axaml
- [DONE] P5.3: Humanized AI output — ResponseFormatter (split into 2 partials) converts all 26 MCP tool JSON responses to natural language
- [DONE] P5.4: Progressive disclosure — overflow "..." button with flyout for secondary toolbar actions, drawing toolbar advanced tools behind expander

## Phase 6: Annotation List & Highlight Text UI (Week 6-7)
> Source: annotation-list-sidebar spec, highlight-text-ui spec

- [DONE] P6.1: Annotation list sidebar panel — AnnotationsPanel.axaml with type filter, count badge, navigate-to-page, delete, per-item type icon/content/author/date
- [DONE] P6.2: Highlight selected text UI — right-click context menu on text selection with Copy, Highlight (Yellow/Green/Blue/Pink), Underline, Strikethrough
- [DONE] P6.3: User bookmarks — IUserBookmarkService + UserBookmarkService (JSON persistence in %LOCALAPPDATA%/FluentPDF/bookmarks/), BookmarksPanel shows user bookmarks section with star icons, toolbar star toggle button

## Phase 7: UI/UX Polish (Week 7)
> Walter's hierarchy: functional → reliable → usable → pleasurable
> Source: UIUX_DESIGN_SUMMARY, BEHAVIORAL_DESIGN_SUMMARY

- [DONE] P7.1: Interaction states — ButtonStyles.axaml enhanced with :pointerover/:pressed/:disabled/:checked states, Transitions for smooth feedback
- [DONE] P7.2: Visual hierarchy improvements — ToolbarManager refactored with user bookmark toggle, zoom tracking, enhanced ButtonStyles with Primary/Secondary/Toolbar variants, toolbar separators
- [DONE] P7.3: Error handling UX — INotificationService + NotificationViewModel + ToastHost control + REST API endpoint POST /api/gui/notify
- [DONE] P7.4: Loading states — ShimmerPlaceholder control with animated gradient, ProgressOverlay for long ops (merge/export/watermark), wired to IsOperationInProgress/OperationProgress

## Phase 8: Behavioral Design & Onboarding (Week 8)
> Hook model: trigger → action → variable reward → investment
> Source: BEHAVIORAL_DESIGN_SUMMARY

- [DONE] P8.1: First-time user experience — WelcomeDialog with 3 feature highlights (Search, AI Tools, Edit), HasCompletedOnboarding setting, "Open a PDF" CTA
- [DONE] P8.2: Recent files with thumbnails — RecentFileCardViewModel + ThumbnailCacheService (renders page 1 at 36 DPI), card grid in empty state with file info + thumbnail
- [DONE] P8.3: Document-aware AI suggestions — DocumentTypeDetector classifies regulatory/contract/research/financial/technical/forms, shows toast notification on open
ALL PHASES COMPLETE

## Verification Criteria

### Build Health
- `dotnet build src/FluentPDF.Core` — 0 errors, 0 warnings
- `dotnet build src/FluentPDF.Rendering` — 0 errors, 0 warnings
- `dotnet build src/FluentPDF.Avalonia` — 0 errors, 0 warnings
- `dotnet test tests/FluentPDF.Core.Tests` — all pass
- `dotnet test tests/FluentPDF.Architecture.Tests` — all pass

### Code Quality KPIs
- Max 500 LOC per file (excluding generated/interop)
- Max 50 LOC per function
- No file with >10 public methods
- No circular dependencies between projects

### Feature Verification (via REST API)
- Search: POST /api/gui/search with query → returns matches with bounding boxes
- Highlights: rendered page PNG shows colored rectangles over matches
- Results panel: GET /api/search/results → JSON array with snippets
- Metadata: GET /api/document/{id}/metadata → full document info
- Annotations: GET /api/annotations → list across all pages

### Accessibility Verification
- Tab through entire UI without mouse
- Every interactive element has AutomationProperties.Name
- All text meets 4.5:1 contrast ratio
- Focus indicators visible on every element
