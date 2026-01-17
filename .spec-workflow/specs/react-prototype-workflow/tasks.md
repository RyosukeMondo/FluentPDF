# Tasks Document

## Phase 1: Infrastructure Setup

- [x] 1. Initialize React development environment with Vite
  - Files:
    - prototype/package.json
    - prototype/tsconfig.json
    - prototype/vite.config.ts
    - prototype/index.html
    - prototype/src/main.tsx
    - prototype/src/App.tsx
    - prototype/.gitignore
    - prototype/README.md
  - Create Vite + React + TypeScript project structure in new `prototype/` directory
  - Configure Vite dev server to run on port 5173 with hot module replacement
  - Set up TypeScript with strict mode and React JSX support
  - Add npm scripts: `dev`, `build`, `typecheck`
  - Document setup instructions in prototype/README.md
  - Purpose: Establish foundational React development environment with instant hot reload
  - _Leverage: None (new project initialization)_
  - _Requirements: 1.1, 1.2, 1.3_
  - _Prompt: **Role:** Full-stack Developer with expertise in React, TypeScript, and modern frontend tooling | **Task:** Initialize a Vite-powered React + TypeScript project in the `prototype/` directory following requirements 1.1-1.3. Configure Vite for optimal development experience with hot module replacement on port 5173. Set up strict TypeScript configuration. Create comprehensive README with setup instructions, development server commands, and project purpose. | **Restrictions:** Must use Vite (not Create React App), TypeScript strict mode required, do not include any backend dependencies, ensure dev server starts within 3 seconds on modern hardware, do not create any XAML-related code in this task | **_Leverage:** None (greenfield initialization) | **Success:** `npm install` completes successfully, `npm run dev` starts dev server on port 5173 within 3 seconds, TypeScript compilation succeeds, hot reload works when editing components, README provides clear setup instructions for new developers | **Implementation Instructions:** Before starting, run `spec-workflow-guide` to load workflow instructions. Mark this task as in-progress in tasks.md by changing `[ ]` to `[-]`. After completion, use `log-implementation` tool to record all files created with detailed artifacts (components, functions, integrations), then mark as complete `[x]` in tasks.md._

- [ ] 2. Set up design token infrastructure
  - Files:
    - design-tokens/tokens.json
    - design-tokens/tokens.schema.json
    - design-tokens/README.md
    - prototype/tools/generate-tokens.ts
    - prototype/tools/generate-tokens.js (compiled)
  - Create design token JSON schema defining colors, spacing, typography, borders structure
  - Create initial tokens.json with FluentPDF design system values extracted from WinUI 3 theme
  - Implement TypeScript token generator script that validates JSON and generates outputs
  - Document token schema and usage in design-tokens/README.md
  - Purpose: Establish single source of truth for styling shared between React and XAML
  - _Leverage: Existing WinUI 3 theme resources (AccentFillColorDefaultBrush, etc.) from src/FluentPDF.App/_
  - _Requirements: 3.1, 3.2, 3.3_
  - _Prompt: **Role:** Frontend Architect with expertise in design systems, TypeScript, and JSON Schema validation | **Task:** Create design token infrastructure following requirements 3.1-3.3. Define comprehensive JSON schema for tokens (colors, spacing, typography, borders, shadows). Extract current color and spacing values from WinUI 3 XAML resources in src/FluentPDF.App and populate tokens.json. Implement TypeScript generator script with Ajv validation that reads tokens.json and prepares for CSS/XAML generation (generation logic in next task). Document token schema, adding new tokens, and regeneration workflow in README. | **Restrictions:** Must validate token values (hex colors, spacing units), do not hardcode token values in generator script, ensure schema is extensible for future token types (animations, gradients), do not generate output files yet (next task), schema must prevent invalid values | **_Leverage:** WinUI 3 theme resources from src/FluentPDF.App/App.xaml or resource dictionaries | **Success:** tokens.json validates against tokens.schema.json, schema enforces correct formats (hex colors, valid units), README explains token structure and workflow, extracted tokens match current WinUI 3 visual design, script compiles and validates JSON successfully | **Implementation Instructions:** Before starting, run `spec-workflow-guide` to load workflow instructions. Mark this task as in-progress in tasks.md. After completion, use `log-implementation` tool with detailed artifacts (schema definitions, token categories, validation rules), then mark complete in tasks.md._

- [ ] 3. Implement token generation script outputs (CSS and XAML)
  - Files:
    - prototype/tools/generate-tokens.ts (modify)
    - prototype/src/styles/tokens.css (generated)
    - design-tokens/Tokens.xaml (generated)
  - Add CSS generation: transform tokens.json → CSS custom properties `:root { --color-accent: #0078D4; }`
  - Add XAML generation: transform tokens.json → WinUI 3 ResourceDictionary with SolidColorBrush and Thickness resources
  - Implement npm script `generate-tokens` that runs generator and outputs both files
  - Add file header comments indicating files are auto-generated
  - Test generation with sample tokens, verify CSS and XAML compile
  - Purpose: Automate CSS and XAML generation from design tokens
  - _Leverage: prototype/tools/generate-tokens.ts from task 2, design-tokens/tokens.json_
  - _Requirements: 3.2, 3.4, 3.5_
  - _Prompt: **Role:** DevOps Engineer with expertise in code generation, template engines, and build automation | **Task:** Extend token generator script from task 2 to produce CSS and XAML outputs following requirements 3.2-3.5. Implement CSS custom properties generation (`:root` with `--color-*`, `--spacing-*`, etc.). Implement XAML ResourceDictionary generation with `SolidColorBrush`, `Thickness`, `FontSize` resources following WinUI 3 conventions. Add npm script `generate-tokens` to package.json. Include auto-generated file headers warning against manual edits. Test that generated CSS works in React and XAML compiles in WinUI 3. | **Restrictions:** Generated files must be valid (CSS syntax, XAML xmlns), do not commit generated files to git (add to .gitignore), must handle all token types defined in schema, output files must be deterministic (same input = same output), preserve WinUI 3 naming conventions (PascalCase for XAML resource keys) | **_Leverage:** generate-tokens.ts validation logic from task 2, tokens.json schema | **Success:** `npm run generate-tokens` produces valid tokens.css and Tokens.xaml, CSS custom properties work in browser dev tools, XAML compiles successfully when added to WinUI 3 project, generated files have clear auto-generated warnings, token values match source JSON exactly | **Implementation Instructions:** Before starting, run `spec-workflow-guide` to load workflow instructions. Mark in-progress in tasks.md. After completion, use `log-implementation` tool with artifacts (code generation functions, template logic, npm scripts), then mark complete._

- [ ] 4. Create TypeScript type definitions and dummy data models
  - Files:
    - prototype/src/types/models.ts
    - prototype/src/data/dummyPdfDocument.ts
    - prototype/src/data/dummyThumbnails.ts
    - prototype/src/data/dummyBookmarks.ts
    - prototype/src/data/dummyFormFields.ts
  - Define TypeScript interfaces for PDF documents, pages, thumbnails, bookmarks, form fields
  - Implement dummy PDF document generator with configurable page count and content types
  - Implement dummy thumbnail generator with loading and selected states
  - Implement dummy hierarchical bookmark structure generator
  - Implement dummy form field generators (text, checkbox, radio, dropdown)
  - Purpose: Provide realistic mock data for React components without PDF rendering logic
  - _Leverage: None (new code, but informed by FluentPDF.App ViewModels structure)_
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_
  - _Prompt: **Role:** TypeScript Developer specializing in data modeling and test fixtures | **Task:** Create comprehensive TypeScript type definitions and dummy data generators following requirements 4.1-4.6. Define interfaces matching FluentPDF domain models (PdfDocument, Page, Thumbnail, Bookmark, FormField) but simplified for UI prototyping. Implement realistic dummy data generators with configurable variety (page counts, content types, loading states, selected states). Ensure dummy data provides visual variety for testing different UI states without implementing actual PDF rendering. | **Restrictions:** Do not implement real PDF parsing or rendering, dummy data must be deterministic with optional randomization seed, types must be simple (no complex business logic), ensure hierarchical bookmark structure supports multiple nesting levels, thumbnail placeholders should indicate content type visually | **_Leverage:** FluentPDF.App ViewModels (src/FluentPDF.App/ViewModels/) for understanding domain model structure | **Success:** TypeScript types compile without errors, dummy generators produce varied realistic data, generated data covers all UI states (loading, selected, error), bookmark hierarchy supports 3+ nesting levels, form fields cover all major types (text, checkbox, radio, select), data is suitable for visual testing of all components | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with detailed artifacts (type definitions, data generation functions), then mark complete._

## Phase 2: XAML → React Reverse Engineering

- [ ] 5. Reverse engineer ThumbnailsSidebar component
  - Files:
    - prototype/src/components/ThumbnailsSidebar.tsx
    - prototype/src/components/ThumbnailsSidebar.module.css
  - Analyze src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml structure
  - Create React component with same visual structure: 150px thumbnails, 8px spacing, vertical scrollable layout
  - Use dummy thumbnail data with selected and loading states
  - Apply design tokens for colors, spacing, borders
  - Implement simplified visual-only interaction (stub event handlers with console.log)
  - Purpose: Create React prototype of most complex UI component for layout iteration
  - _Leverage: src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml, dummy data from task 4, design tokens from task 3_
  - _Requirements: 2.1, 2.2, 2.3, 2.4_
  - _Prompt: **Role:** React Developer with expertise in component architecture and CSS layout | **Task:** Reverse engineer ThumbnailsSidebar.xaml into React component following requirements 2.1-2.4. Analyze XAML structure (ItemsRepeater, StackLayout, 150px×220px thumbnails, 8px margins, selected border, loading spinner). Create React equivalent using flexbox layout, CSS modules for styling, design tokens for colors/spacing. Use dummy thumbnail data from task 4. Implement visual structure only—stub drag/drop and context menu interactions with console.log. Ensure visual parity with XAML version (same dimensions, spacing, selection indicator). | **Restrictions:** Do not implement real drag-and-drop or context menus, do not load real PDF thumbnails (use gradient placeholders), must use CSS modules (not inline styles), must reference design token CSS variables (not hardcoded values), component must be stateless (receive props only) | **_Leverage:** ThumbnailsSidebar.xaml for structure, dummyThumbnails.ts for data, tokens.css for styling | **Success:** Component renders scrollable thumbnail list matching XAML visual design, 150px thumbnail width preserved, 8px spacing between items matches XAML, selected state shows accent border, loading state shows spinner, gradient placeholders provide visual variety, TypeScript types are correct, hot reload works when editing | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (React component, CSS module, integration with dummy data), then mark complete._

- [ ] 6. Reverse engineer PdfViewerControl component
  - Files:
    - prototype/src/components/PdfViewerControl.tsx
    - prototype/src/components/PdfViewerControl.module.css
  - Analyze src/FluentPDF.App/Controls/PdfViewerControl.xaml and code-behind structure
  - Create React component with main viewer area, zoom controls, page navigation
  - Use dummy PDF document with single visible page (canvas placeholder)
  - Apply design tokens for layout, spacing, control styling
  - Implement zoom slider and navigation buttons (visual only, stub zoom logic)
  - Purpose: Create main viewer component prototype for testing layout and control positioning
  - _Leverage: src/FluentPDF.App/Controls/PdfViewerControl.xaml, dummy PDF document from task 4, design tokens_
  - _Requirements: 2.1, 2.2, 2.3_
  - _Prompt: **Role:** React Developer with expertise in complex layout components and UI controls | **Task:** Reverse engineer PdfViewerControl.xaml into React component following requirements 2.1-2.3. Analyze XAML viewer structure (main canvas area, toolbar, zoom controls, page navigation). Create React component with similar layout using CSS Grid or Flexbox. Display single dummy PDF page as gradient rectangle (no real rendering). Implement zoom slider, page navigation buttons, fit-to-width/fit-to-page controls as visual elements with stub handlers. Apply design tokens for spacing and colors. | **Restrictions:** Do not implement actual PDF rendering or zoom transforms, controls should be visual mockups only, must use design token CSS variables, component layout should adapt to viewport size, do not duplicate toolbar logic (separate component in future task) | **_Leverage:** PdfViewerControl.xaml for layout structure, dummyPdfDocument.ts for metadata, tokens.css for styling | **Success:** Component displays main viewer area with dummy page, zoom controls positioned correctly matching XAML, page navigation buttons styled with design tokens, layout is responsive, hot reload works, TypeScript compilation succeeds | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (component, layout CSS, stub interaction handlers), then mark complete._

- [ ] 7. Reverse engineer BookmarksPanel component
  - Files:
    - prototype/src/components/BookmarksPanel.tsx
    - prototype/src/components/BookmarksPanel.module.css
  - Analyze src/FluentPDF.App/Controls/BookmarksPanel.xaml structure
  - Create React component with hierarchical tree view for bookmarks/TOC
  - Use dummy bookmark data with 3-level nesting
  - Apply design tokens for indentation, colors, hover states
  - Implement expand/collapse UI (visual only, stub navigation)
  - Purpose: Create bookmark navigation prototype for testing hierarchical layout
  - _Leverage: src/FluentPDF.App/Controls/BookmarksPanel.xaml, dummy bookmarks from task 4, design tokens_
  - _Requirements: 2.1, 2.2, 2.3, 4.6_
  - _Prompt: **Role:** React Developer with expertise in tree view components and recursive rendering | **Task:** Reverse engineer BookmarksPanel.xaml into React component following requirements 2.1-2.3 and 4.6. Analyze XAML TreeView structure (hierarchical outline, expandable nodes). Create React recursive component rendering bookmark hierarchy with indentation for nesting levels. Use dummy bookmark data from task 4 (3+ levels deep). Implement expand/collapse icons and indentation. Style with design tokens. Stub click handlers (console.log page number). | **Restrictions:** Do not implement actual page navigation, tree state (expanded/collapsed) can use local React state, must handle arbitrary nesting depth, must use design token spacing for indentation, do not hardcode tree levels | **_Leverage:** BookmarksPanel.xaml for structure, dummyBookmarks.ts for hierarchical data, tokens.css for styling | **Success:** Component renders bookmark tree with correct nesting, indentation increases by token spacing value per level, expand/collapse icons toggle visibility, hover states styled with token colors, supports 3+ nesting levels, TypeScript recursion types correct, hot reload preserves tree state | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (recursive component, tree rendering logic), then mark complete._

- [ ] 8. Create main layout components (MainWindow, MainPage, PdfViewerPage)
  - Files:
    - prototype/src/components/MainWindow.tsx
    - prototype/src/components/MainPage.tsx
    - prototype/src/components/PdfViewerPage.tsx
    - prototype/src/components/layouts.module.css
  - Analyze XAML page hierarchy and navigation structure
  - Create MainWindow layout with title bar area and content area
  - Create MainPage with navigation menu
  - Create PdfViewerPage composing PdfViewerControl, ThumbnailsSidebar, BookmarksPanel
  - Apply design tokens for layout spacing and backgrounds
  - Purpose: Establish complete app layout hierarchy for holistic visual testing
  - _Leverage: src/FluentPDF.App/Views/*.xaml, components from tasks 5-7, design tokens_
  - _Requirements: 2.4, 2.5_
  - _Prompt: **Role:** Frontend Architect specializing in React application structure and layout composition | **Task:** Reverse engineer XAML view hierarchy (MainWindow → MainPage → PdfViewerPage) into React layout components following requirements 2.4-2.5. Analyze navigation structure and page composition patterns. Create MainWindow with title bar and content area. Create MainPage with main navigation (home, viewer, settings, conversion). Create PdfViewerPage composing PdfViewerControl, ThumbnailsSidebar, and BookmarksPanel in classic three-panel layout (sidebar | viewer | panel). Apply design token spacing and backgrounds. Use CSS Grid or Flexbox for responsive layout. | **Restrictions:** Do not implement real navigation (use simple state toggling), layout must be responsive to viewport changes, must use design token CSS variables for all spacing and colors, do not duplicate component code (import from tasks 5-7), maintain XAML page hierarchy structure | **_Leverage:** MainWindow.xaml, MainPage.xaml, PdfViewerPage.xaml for structure; PdfViewerControl, ThumbnailsSidebar, BookmarksPanel components from tasks 5-7; tokens.css | **Success:** MainWindow renders with title bar and content area, MainPage shows navigation menu, PdfViewerPage displays three-panel layout (thumbnails | viewer | bookmarks), layout is responsive and matches XAML visual hierarchy, design tokens applied consistently, hot reload works across all layout components | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (layout components, composition patterns, responsive CSS), then mark complete._

- [ ] 9. Create dialog components (WatermarkDialog, DeletePagesDialog, ErrorDialog, SettingsPage)
  - Files:
    - prototype/src/components/dialogs/WatermarkDialog.tsx
    - prototype/src/components/dialogs/DeletePagesDialog.tsx
    - prototype/src/components/dialogs/ErrorDialog.tsx
    - prototype/src/components/dialogs/SettingsPage.tsx
    - prototype/src/components/dialogs/dialogs.module.css
  - Analyze XAML dialog structures and form layouts
  - Create React modal dialog components with similar visual structure
  - Use design tokens for dialog styling (backgrounds, borders, spacing)
  - Implement form controls (text inputs, checkboxes, buttons) as visual mockups
  - Stub dialog actions (OK, Cancel) with console.log
  - Purpose: Prototype dialog UIs for rapid iteration on form layouts and validation displays
  - _Leverage: src/FluentPDF.App/Views/*Dialog.xaml, SettingsPage.xaml, design tokens_
  - _Requirements: 2.1, 2.2, 2.3_
  - _Prompt: **Role:** React Developer with expertise in form design and modal dialogs | **Task:** Reverse engineer XAML dialogs (WatermarkDialog, DeletePagesDialog, ErrorDialog, SettingsPage) into React components following requirements 2.1-2.3. Analyze XAML ContentDialog and form field layouts. Create modal dialog components with similar visual structure using React portals or modal libraries. Implement form controls (text inputs for watermark text, checkboxes for delete options, error message display, settings toggles) as visual mockups. Apply design tokens for consistent styling. Stub dialog actions (submit, cancel) with console.log. | **Restrictions:** Do not implement real dialog logic or form validation, dialogs should be visual prototypes only, must use design token CSS variables, do not implement real settings persistence, form controls should be styled but non-functional (visual only) | **_Leverage:** WatermarkDialog.xaml, DeletePagesDialog.xaml, ErrorDialog.xaml, SettingsPage.xaml for structure; tokens.css for styling | **Success:** All dialog components render with correct visual structure, form layouts match XAML designs, design tokens applied consistently (spacing, colors, typography), dialogs can be opened/closed (simple state toggle), buttons styled correctly, TypeScript types for dialog props correct | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (dialog components, form layouts, modal patterns), then mark complete._

- [ ] 10. Update App.tsx to compose all components
  - Files:
    - prototype/src/App.tsx (modify)
    - prototype/src/App.module.css
  - Import all layout and component modules from tasks 5-9
  - Create simple state management for navigation between views
  - Compose MainWindow → MainPage → PdfViewerPage hierarchy
  - Add simple UI for opening dialogs (buttons in toolbar area)
  - Apply global styles and design token imports
  - Purpose: Integrate all components into complete prototype app
  - _Leverage: All components from tasks 5-9, design tokens_
  - _Requirements: 1.5, 2.4, 2.5_
  - _Prompt: **Role:** React Developer with expertise in application composition and state management | **Task:** Integrate all React components from tasks 5-9 into complete prototype application in App.tsx following requirements 1.5, 2.4, 2.5. Import MainWindow, MainPage, PdfViewerPage, and dialog components. Implement simple navigation state (home, viewer, settings, conversion pages). Compose component hierarchy matching XAML app structure. Add toolbar buttons to open dialog components for visual testing. Import and apply design token CSS globally. Ensure hot reload works for entire app. | **Restrictions:** Do not use complex state management libraries (Redux, Zustand), simple useState is sufficient for prototype, must maintain XAML page hierarchy structure, do not implement real routing (simple state toggling), ensure all components are visible and testable | **_Leverage:** MainWindow, MainPage, PdfViewerPage from task 8; dialogs from task 9; all other components from tasks 5-7; tokens.css | **Success:** App.tsx renders complete prototype app, all components visible and navigable, MainWindow → MainPage → PdfViewerPage hierarchy matches XAML, dialogs can be opened via toolbar buttons, design tokens applied globally, hot reload works across entire app, TypeScript compilation succeeds, dev server starts successfully | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (app composition, navigation state, component integration), then mark complete._

## Phase 3: Documentation and Workflow

- [ ] 11. Create component mapping documentation
  - Files:
    - docs/component-mapping.md
  - Document React ↔ XAML component mapping for all 18 components
  - Provide side-by-side code examples (React props ↔ XAML properties)
  - Explain XAML-specific feature simplification strategy (drag/drop, ItemsRepeater, etc.)
  - Create translation checklist: structure → tokens → bindings → features
  - Add template section for new component mappings
  - Purpose: Guide developers in efficiently translating between React and XAML
  - _Leverage: All React components from tasks 5-10, corresponding XAML files_
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - _Prompt: **Role:** Technical Writer with expertise in React and XAML documentation | **Task:** Create comprehensive component mapping documentation following requirements 5.1-5.5. Document all 18 components (Controls: PdfViewerControl, ThumbnailsSidebar, BookmarksPanel, etc.; Views: MainWindow, MainPage, etc.; Dialogs: WatermarkDialog, ErrorDialog, etc.). For each component: list file paths (React .tsx ↔ XAML .xaml), show side-by-side code examples of key patterns (props/properties, events, styling), explain XAML-specific features and React simplifications. Create step-by-step translation checklist. Add template for documenting new components. Include code snippets showing prop mapping (e.g., `currentPage` prop ↔ `CurrentPage` XAML property). | **Restrictions:** Documentation must be accurate to actual code from tasks 5-10, examples must compile in both React and XAML, do not document features not implemented in prototype, use consistent formatting for code snippets, include both directions (React → XAML and XAML → React) | **_Leverage:** All React components (tasks 5-10), corresponding XAML files from src/FluentPDF.App/ | **Success:** Documentation covers all 18 components, side-by-side examples are clear and accurate, XAML feature simplification strategy explained, translation checklist is actionable, template section provided for extensibility, code snippets compile without errors, document is readable by developers unfamiliar with either framework | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (documentation sections, code examples, mapping tables), then mark complete._

- [ ] 12. Create developer workflow documentation
  - Files:
    - prototype/README.md (update)
    - docs/react-prototype-workflow.md
  - Document complete development workflow: React → Screenshot → XAML → ViewModel
  - Add setup instructions for new developers
  - Document design token workflow: edit JSON → generate → verify in both UIs
  - Add troubleshooting section (port conflicts, Node version, TypeScript errors)
  - Document CI integration and build verification process
  - Purpose: Enable developers to adopt React prototype workflow efficiently
  - _Leverage: prototype/README.md from task 1, component mapping from task 11_
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  - _Prompt: **Role:** Developer Advocate with expertise in technical documentation and developer onboarding | **Task:** Create comprehensive workflow documentation following requirements 6.1-6.5. Document complete development cycle: (1) design in React with hot reload, (2) take screenshot for reference, (3) translate structure to XAML, (4) wire ViewModels and data bindings, (5) add WinUI 3-specific features. Update prototype/README.md with setup instructions, prerequisite checks, dev server commands. Create react-prototype-workflow.md with workflow diagrams, design token regeneration process, troubleshooting common issues (port conflicts, Node.js version mismatches, TypeScript errors), and CI verification process. Include time estimates for each workflow phase. | **Restrictions:** Instructions must be accurate and tested (verify commands work), troubleshooting should cover actual errors developers will encounter, workflow documentation should be beginner-friendly (assume no React experience), do not document features not implemented, include prerequisite checks (Node.js version, npm availability) | **_Leverage:** prototype/README.md from task 1, component-mapping.md from task 11, design token generation from tasks 2-3 | **Success:** New developer can set up prototype environment in 5 minutes following README, workflow documentation is clear and actionable, design token workflow documented with commands and examples, troubleshooting section covers common errors, CI integration explained, estimated timings realistic (React iteration: 5-10min, XAML translation: 10-15min), all commands tested and verified working | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (documentation sections, workflow diagrams, setup scripts), then mark complete._

- [ ] 13. Update CLAUDE.md with React prototype workflow
  - Files:
    - CLAUDE.md (modify)
  - Add React prototype section to project context
  - Document when to use prototype (new UI features, layout changes) vs when to skip (simple dialogs, minor tweaks)
  - Add npm commands for common tasks (dev server, token generation, build verification)
  - Link to component-mapping.md and workflow documentation
  - Purpose: Integrate React prototype workflow into Claude Code assistant context
  - _Leverage: CLAUDE.md existing structure, docs from tasks 11-12_
  - _Requirements: 6.1, 6.2_
  - _Prompt: **Role:** Developer Experience Engineer with expertise in AI-assisted development workflows | **Task:** Update CLAUDE.md to include React prototype workflow following requirements 6.1-6.2. Add new section "React Prototype Workflow" explaining purpose (rapid UI iteration without WinUI 3 build overhead), when to use prototype (complex layouts, new features, visual experimentation) vs when to skip (simple dialogs, minor XAML tweaks). Document key npm commands: `npm run dev` (start dev server), `npm run generate-tokens` (regenerate design tokens), `npm run build` (verify TypeScript compilation). Link to component-mapping.md and react-prototype-workflow.md. Keep section concise (3-5 paragraphs) for Claude Code context window efficiency. | **Restrictions:** Do not remove or modify existing CLAUDE.md content, additions should be concise and focused, commands must be accurate, links must use correct file paths, maintain existing document formatting style | **_Leverage:** Existing CLAUDE.md structure and conventions, component-mapping.md and workflow docs from tasks 11-12 | **Success:** CLAUDE.md updated with React prototype section, purpose and usage guidelines clear, npm commands documented and accurate, links to detailed documentation work, section is concise (<500 words), maintains CLAUDE.md formatting conventions | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (documentation additions), then mark complete._

## Phase 4: Integration and Validation

- [ ] 14. Set up CI/CD verification for React prototype
  - Files:
    - .github/workflows/prototype-build.yml
  - Create GitHub Actions workflow for React prototype build verification
  - Add steps: checkout, setup Node.js 20, npm ci, npm run build, npm run typecheck
  - Configure workflow to run on pull requests affecting prototype/ directory
  - Add status badge to prototype/README.md
  - Purpose: Prevent prototype rot through automated build verification
  - _Leverage: Existing GitHub Actions workflows in .github/workflows/_
  - _Requirements: 6.5_
  - _Prompt: **Role:** DevOps Engineer with expertise in GitHub Actions and CI/CD pipelines | **Task:** Create CI/CD workflow for React prototype build verification following requirement 6.5. Set up GitHub Actions workflow (prototype-build.yml) that runs on pull requests. Include steps: checkout code, setup Node.js 20.x, run `npm ci` for clean install, run `npm run build` to verify production build, run `npm run typecheck` to verify TypeScript compilation. Configure workflow to run only when prototype/ files change (path filter). Add clear job names and step descriptions. Add status badge to prototype/README.md. Ensure workflow fails PR if build or typecheck fails. | **Restrictions:** Workflow must run on Ubuntu (fastest), must use npm ci (not npm install) for reproducibility, must fail fast on errors, do not upload build artifacts (prototype is not deployed), ensure Node.js version matches local development (20.x), path filter should include prototype/** and design-tokens/** | **_Leverage:** Existing GitHub Actions workflows for patterns (.github/workflows/) | **Success:** Workflow file created and validates (YAML syntax), workflow runs successfully on sample PR, build failures cause workflow failure, TypeScript errors cause workflow failure, workflow only runs when prototype files change, status badge displays correctly in README, workflow completes in under 2 minutes | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (GitHub Actions workflow, CI configuration), then mark complete._

- [ ] 15. Create .gitignore entries for React prototype
  - Files:
    - .gitignore (modify at root)
    - prototype/.gitignore
  - Add prototype build outputs to .gitignore: dist/, node_modules/, *.log
  - Add generated design token files: tokens.css, Tokens.xaml (regenerated from JSON)
  - Add IDE files: .vscode/, .idea/ (if not already present)
  - Ensure tokens.json is committed (source of truth)
  - Purpose: Prevent committing generated files and dependencies
  - _Leverage: Existing .gitignore patterns_
  - _Requirements: Non-functional requirement (reliability)_
  - _Prompt: **Role:** DevOps Engineer with expertise in Git workflows and repository hygiene | **Task:** Configure .gitignore for React prototype to prevent committing generated files and dependencies. Add prototype-specific ignores to root .gitignore (prototype/dist/, prototype/node_modules/, prototype/*.log). Create prototype/.gitignore for local ignores. Ensure generated token files (tokens.css, Tokens.xaml) are ignored since they're generated from tokens.json. Keep tokens.json committed (source of truth). Add common IDE directories if not present (.vscode/, .idea/). Verify tokens.json is NOT ignored. | **Restrictions:** Do not ignore tokens.json (it's the source of truth), ensure node_modules/ is ignored, do not break existing .gitignore patterns, use appropriate .gitignore syntax, test with `git status` that generated files are ignored | **_Leverage:** Existing .gitignore for patterns and conventions | **Success:** `git status` shows tokens.json as trackable, generated files (dist/, node_modules/, tokens.css, Tokens.xaml) are ignored, no prototype build artifacts appear in git status, IDE directories ignored, .gitignore syntax is valid, existing patterns not broken | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (.gitignore rules, ignore patterns), then mark complete._

- [ ] 16. Validate design token synchronization between React and XAML
  - Files:
    - docs/token-validation-report.md
  - Run `npm run generate-tokens` and verify outputs
  - Manually verify token CSS variables work in React dev server
  - Manually verify XAML ResourceDictionary compiles in WinUI 3 project
  - Compare visual appearance of React and XAML UIs using same token values
  - Document validation results and any discrepancies
  - Purpose: Ensure design token system maintains visual consistency
  - _Leverage: tokens.json from task 2, React components from tasks 5-10, XAML components from src/FluentPDF.App/_
  - _Requirements: 3.2, 3.4, 3.5_
  - _Prompt: **Role:** QA Engineer with expertise in visual testing and cross-platform UI validation | **Task:** Validate design token synchronization following requirements 3.2-3.5. Generate tokens using `npm run generate-tokens`. Inspect generated tokens.css (verify CSS custom properties syntax, values match JSON). Test tokens.css in React app (check browser dev tools, verify variables applied correctly). Copy Tokens.xaml to FluentPDF.App project, verify XAML compiles without errors. Compare visual appearance: take screenshots of equivalent components in React vs WinUI 3, verify colors and spacing match. Document findings in validation report with screenshots and any discrepancies found. | **Restrictions:** Do not modify token generation script (report issues instead), validation must be manual (no automated visual regression at this stage), screenshots should compare equivalent components (e.g., ThumbnailsSidebar React vs XAML), document any color or spacing mismatches, note any missing token mappings | **_Leverage:** tokens.json and generator from tasks 2-3, React components for visual testing, FluentPDF.App XAML for comparison | **Success:** tokens.css and Tokens.xaml generated successfully, CSS variables work in browser dev tools, XAML compiles in WinUI 3 without errors, visual comparison shows matching colors and spacing (within 1-2px tolerance), validation report documents process and results, any discrepancies identified and documented | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (validation report, token verification process), then mark complete._

- [ ] 17. End-to-end manual testing of React prototype workflow
  - Files:
    - docs/workflow-validation-report.md
  - Simulate complete workflow: design new UI component in React → translate to XAML
  - Test hot reload performance (measure save-to-reload time)
  - Test design token modification workflow (edit JSON → generate → verify both UIs)
  - Test developer onboarding (follow README from scratch on clean machine or VM)
  - Validate all documentation accuracy (commands work, links valid)
  - Purpose: Validate entire workflow end-to-end before handoff
  - _Leverage: All documentation from tasks 11-13, React prototype from tasks 1-10_
  - _Requirements: All requirements_
  - _Prompt: **Role:** QA Engineer with expertise in workflow validation and user acceptance testing | **Task:** Conduct end-to-end validation of React prototype workflow covering all requirements. Test complete cycle: (1) Create new dummy UI component in React (e.g., PageNumberControl), iterate with hot reload (measure save-to-reload time, verify <500ms), (2) Screenshot React version, (3) Create equivalent XAML component following component-mapping.md guide, (4) Modify design token (change accent color), regenerate tokens, verify change in both React and XAML UIs, (5) Test developer onboarding by following prototype/README.md on clean environment (verify setup time <5 minutes). Validate all documentation commands execute successfully. Document process, timings, and issues in validation report. | **Restrictions:** Perform testing on clean environment (fresh clone or VM) for onboarding validation, measure actual timings (hot reload, token generation, setup time), do not skip documentation steps (validate they work as written), report any errors or unclear instructions, take screenshots of key workflow steps | **_Leverage:** Complete prototype from tasks 1-10, all documentation from tasks 11-13, design token system from tasks 2-3 | **Success:** Complete workflow executes successfully, hot reload time measured <500ms, design token changes reflect in both UIs, new component created in React and translated to XAML following docs, developer onboarding completed in <5 minutes, all documentation commands work, validation report documents timings and workflow, any issues identified and documented for follow-up | **Implementation Instructions:** Before starting, run `spec-workflow-guide`. Mark in-progress in tasks.md. After completion, use `log-implementation` with artifacts (validation report, workflow test results, timing measurements), then mark complete._

## Task Summary

**Phase 1: Infrastructure Setup** (Tasks 1-4)
- React/Vite environment, design tokens, dummy data

**Phase 2: XAML → React Reverse Engineering** (Tasks 5-10)
- Core components, layout components, dialogs, app integration

**Phase 3: Documentation and Workflow** (Tasks 11-13)
- Component mapping, workflow docs, CLAUDE.md update

**Phase 4: Integration and Validation** (Tasks 14-17)
- CI/CD, .gitignore, token validation, E2E testing

**Total Estimated Effort:** ~24-32 hours (3-4 working days for experienced developer)
- Infrastructure: 6-8 hours
- Reverse Engineering: 10-14 hours
- Documentation: 4-6 hours
- Integration/Validation: 4-4 hours
