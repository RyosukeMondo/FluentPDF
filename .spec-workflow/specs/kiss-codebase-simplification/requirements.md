# Requirements Document: KISS Codebase Simplification

## Introduction

This specification addresses critical complexity issues discovered during debugging (DiagnosticsPanel close button issue) that revealed systemic over-engineering throughout the FluentPDF codebase. The KISS (Keep It Simple, Stupid) principle will be systematically applied to reduce unnecessary complexity, improve maintainability, and prevent similar debugging nightmares.

**Root Problem**: The codebase contains multiple instances of:
- Over-engineered UI components (GlassPanel with 5+ layers when a Border would suffice)
- Complex abstractions where simple direct implementations work better
- Unnecessary indirection (commands when click handlers work)
- Redundant code paths and duplicate functionality

**Value**: Simplified code is easier to debug, faster to modify, more reliable, and reduces cognitive load for developers.

## Alignment with Product Vision

From product.md:
- **Privacy-First Architecture**: Simpler code = fewer places for bugs/vulnerabilities
- **Performance**: Less complexity = faster execution and lower memory usage
- **Verifiable Quality Architecture**: Simpler code is easier to validate and test
- **Brand Trust**: Reliable, bug-free software builds user trust

Success Metrics alignment:
- **Performance**: Simpler code → faster app launch, better scrolling FPS
- **Reliability**: Fewer moving parts → fewer failure modes
- **Maintainability**: Direct code paths → easier debugging

## Requirements

### REQ-1: UI Component Simplification

**User Story:** As a developer, I want UI components to use the simplest possible implementation, so that I can debug issues quickly and understand code at a glance.

#### Acceptance Criteria

1. WHEN a component only needs basic styling THEN it SHALL use Border/Panel instead of custom controls
2. WHEN visual effects are not critical to functionality THEN they SHALL be removed or made optional
3. IF a component has multiple layers (>2) AND serves a single visual purpose THEN it SHALL be consolidated
4. WHEN debugging UI issues THEN developers SHALL be able to identify hit-testing boundaries within 30 seconds
5. WHEN hover/click events are needed THEN code-behind event handlers SHALL be preferred over command bindings

**Examples**:
- DiagnosticsPanel: Replace multi-layer GlassPanel with simple Border ✓ (already done)
- Any component using ExperimentalAcrylicBorder: Evaluate if blur effect is essential or cosmetic

### REQ-2: Event Handling Simplification

**User Story:** As a developer, I want event handling to use the most direct mechanism, so that I can trace event flow without navigating multiple abstractions.

#### Acceptance Criteria

1. WHEN a button needs to perform an action THEN direct event handlers SHALL be preferred over ICommand/RelayCommand
2. IF event logic is <10 lines THEN it SHALL be inline in code-behind, not abstracted
3. WHEN debugging event issues THEN event handler code SHALL be immediately visible in code-behind
4. IF command pattern is used THEN there MUST be a clear justification (undo/redo, testability for complex logic)

**Examples**:
- Close button: Click handler instead of ToggleVisibilityCommand ✓ (already done)
- All simple button actions: Migrate from commands to click handlers

### REQ-3: Remove Redundant Abstractions

**User Story:** As a developer, I want to access functionality directly without navigating unnecessary abstraction layers, so that I can implement features faster.

#### Acceptance Criteria

1. WHEN a service has only one implementation THEN the interface SHALL be removed unless required for DI testing
2. IF a wrapper class adds no logic AND only forwards calls THEN it SHALL be eliminated
3. WHEN factory patterns are used AND only produce one type THEN they SHALL be replaced with direct instantiation
4. IF inheritance hierarchy depth > 2 AND provides no polymorphism benefit THEN it SHALL be flattened

**Examples**:
- Service interfaces with single implementation: Evaluate necessity
- Factory classes that create one type: Replace with `new` or DI

### REQ-4: Eliminate Duplicate Code

**User Story:** As a developer, I want each piece of functionality to exist in exactly one place, so that I don't have to hunt for the "real" implementation.

#### Acceptance Criteria

1. WHEN identical logic exists in multiple places THEN it SHALL be consolidated to one location
2. IF similar-but-not-identical code exists THEN shared parts SHALL be extracted to utilities
3. WHEN fixing a bug THEN there SHALL be only one place to fix it
4. IF configuration exists in multiple formats THEN it SHALL be unified to one source of truth

**Examples**:
- Multiple theme/styling approaches: Standardize on one
- Duplicate validation logic: Extract to shared validators

### REQ-5: Simplify Dependencies

**User Story:** As a developer, I want minimal dependencies between modules, so that I can modify one area without breaking others.

#### Acceptance Criteria

1. WHEN a component needs data THEN it SHALL request only what it needs, not entire objects
2. IF circular dependencies exist THEN they SHALL be broken through refactoring
3. WHEN adding new functionality THEN it SHALL not introduce dependencies on unrelated modules
4. IF a module has > 5 dependencies THEN it SHALL be reviewed for over-coupling

### REQ-6: Remove Dead Code

**User Story:** As a developer, I want the codebase to contain only actively used code, so that I don't waste time understanding obsolete implementations.

#### Acceptance Criteria

1. WHEN code is commented out for > 2 weeks THEN it SHALL be deleted (git preserves history)
2. IF a class/method/property is unused THEN it SHALL be removed
3. WHEN feature flags permanently disable code THEN that code SHALL be deleted
4. IF debugging/diagnostic code exists in production paths THEN it SHALL be moved to DEBUG-only regions

## Non-Functional Requirements

### Code Architecture and Modularity
- **File Size Limit**: No file > 500 lines (excluding comments/blanks)
- **Function Complexity**: Max cyclomatic complexity = 10 per method
- **Inheritance Depth**: Max 2 levels unless clear polymorphism benefit
- **Coupling**: Prefer composition over inheritance

### Performance
- **Build Time**: Simplification SHALL NOT increase build time
- **Runtime Performance**: Removing abstractions SHALL improve or maintain performance (never degrade)
- **Memory**: Fewer objects/layers SHALL reduce memory footprint

### Maintainability
- **Debug Time**: Common issues SHALL be debuggable in < 5 minutes (vs. 2+ hours for DiagnosticsPanel)
- **Code Readability**: New developers SHALL understand code flow within 15 minutes per component
- **Change Impact**: Modifications SHALL be localized (< 3 files for typical changes)

### Testing
- **Test Coverage**: Simplification SHALL NOT reduce test coverage below current levels
- **Test Simplicity**: Tests SHALL become simpler as code simplifies (fewer mocks, less setup)

## Out of Scope

- Rewriting entire architecture from scratch (incremental improvements only)
- Changing external APIs or user-visible behavior (internal refactoring only)
- Performance optimizations unrelated to simplicity (separate effort)
- Adding new features (pure simplification effort)

## Success Criteria

1. **Quantitative**:
   - Average file size reduced by 20%
   - Number of abstraction layers reduced by 30%
   - Cyclomatic complexity reduced by 25%
   - Build warnings reduced to 0

2. **Qualitative**:
   - Debugging sessions complete 3x faster
   - New developer onboarding time reduced
   - Code review comments focus on logic, not "why is this so complex?"

3. **Validation**:
   - All existing tests pass
   - No regressions in functionality
   - Performance metrics maintained or improved
