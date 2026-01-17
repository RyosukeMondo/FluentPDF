# React Prototype Workflow

This document describes the complete development workflow for using the React prototype environment to rapidly iterate on FluentPDF UI designs before implementing them in WinUI 3 XAML.

## Table of Contents

1. [Overview](#overview)
2. [When to Use the Prototype](#when-to-use-the-prototype)
3. [Development Workflow](#development-workflow)
4. [Design Token Workflow](#design-token-workflow)
5. [Setup Instructions](#setup-instructions)
6. [Troubleshooting](#troubleshooting)
7. [CI Integration](#ci-integration)

---

## Overview

The React prototype provides a **rapid iteration environment** that eliminates WinUI 3 build overhead during UI design. The workflow follows this cycle:

```
┌─────────────────────────────────────────────────────┐
│  1. Design in React (instant hot reload <500ms)     │
│  2. Screenshot finalized UI                          │
│  3. Translate structure to XAML                      │
│  4. Wire ViewModels and data bindings                │
│  5. Add WinUI 3-specific features                    │
└─────────────────────────────────────────────────────┘
```

**Key Benefits:**
- **Instant feedback**: Hot module replacement shows changes in <500ms vs 30-60s WinUI 3 builds
- **Visual experimentation**: Try different layouts, colors, spacing without compilation overhead
- **Screenshot-driven development**: Visual reference for precise XAML translation
- **Design token synchronization**: Shared styling between React and XAML ensures consistency

---

## When to Use the Prototype

### ✅ Use the React Prototype For:

1. **New complex UI components**
   - Multi-panel layouts (viewer, sidebar, panels)
   - Complex forms and dialogs with many inputs
   - Custom controls with intricate visual structure

2. **Layout experimentation**
   - Testing different panel arrangements
   - Responsive layout behavior at different window sizes
   - Spacing and alignment adjustments

3. **Visual design iteration**
   - Color scheme experimentation
   - Typography and sizing adjustments
   - Border radius, shadows, visual effects

4. **Component composition**
   - Testing how components fit together
   - Verifying visual hierarchy
   - Validating information architecture

### ❌ Skip the Prototype For:

1. **Simple dialogs**
   - Single-input confirmations (e.g., "Are you sure?")
   - Error messages with only text
   - Standard system dialogs

2. **Minor XAML tweaks**
   - Changing a single margin value
   - Updating button text or labels
   - Small alignment fixes in existing components

3. **Backend-heavy features**
   - PDF rendering logic
   - File I/O operations
   - Business logic without UI changes

4. **XAML-specific features**
   - Drag-and-drop implementation
   - Complex data binding scenarios
   - Platform-specific integrations

**Rule of Thumb:** If the change is primarily visual and involves multiple components or complex layout, use the prototype. If it's a simple fix or backend logic, implement directly in XAML.

---

## Development Workflow

### Phase 1: Design in React (Fast Iteration)

**Time Estimate:** 5-15 minutes per component

1. **Start the development server**
   ```bash
   cd prototype
   npm run dev
   ```
   Server starts on http://localhost:5173 with hot module replacement enabled.

2. **Create or modify components**
   ```bash
   # Create new component
   touch src/components/MyNewComponent.tsx
   touch src/components/MyNewComponent.module.css
   ```

3. **Implement visual structure**
   - Use design token CSS variables for all styling
   - Reference existing components for patterns
   - Use dummy data from `src/data/` for realistic content
   - Focus on layout and visual appearance only

4. **Iterate with instant feedback**
   - Edit component file
   - Save (Ctrl+S / Cmd+S)
   - Browser updates in <500ms automatically
   - Repeat until design is finalized

**Example Component:**
```tsx
// src/components/MyNewComponent.tsx
import React from 'react';
import styles from './MyNewComponent.module.css';

export const MyNewComponent: React.FC = () => (
  <div className={styles.container}>
    <h2 className={styles.title}>New Feature</h2>
    <button className={styles.actionButton}>Click Me</button>
  </div>
);
```

```css
/* src/components/MyNewComponent.module.css */
.container {
  padding: var(--spacing-md);
  background: var(--color-background-card);
  border-radius: var(--border-radius-md);
}

.title {
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-sm);
}

.actionButton {
  background: var(--color-primary);
  color: var(--color-white);
  padding: var(--spacing-button-padding-vertical) var(--spacing-button-padding-horizontal);
  border-radius: var(--border-radius-md);
}
```

### Phase 2: Screenshot for Reference

**Time Estimate:** 1-2 minutes

1. **Finalize component in browser**
   - Verify all states (default, hover, selected, loading)
   - Test at different viewport sizes if responsive
   - Check interactions work (stub handlers log correctly)

2. **Take screenshots**
   - Use browser DevTools device emulation for consistent sizing
   - Capture different states if component has multiple modes
   - Save screenshots with descriptive names

   **Recommended screenshot tool:** Windows Snipping Tool (Win+Shift+S), macOS Screenshot (Cmd+Shift+4)

3. **Annotate if necessary**
   - Add measurements for critical spacing
   - Highlight token usage (colors, spacing)
   - Note any dynamic behavior

### Phase 3: Translate to XAML

**Time Estimate:** 10-20 minutes per component

1. **Reference component mapping guide**
   - Open `docs/component-mapping.md`
   - Find similar component examples
   - Review pattern translation guide

2. **Create XAML structure**
   ```xml
   <!-- src/FluentPDF.App/Controls/MyNewComponent.xaml -->
   <UserControl
       x:Class="FluentPDF.App.Controls.MyNewComponent"
       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

       <Grid Padding="16" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" CornerRadius="8">
           <StackPanel Spacing="8">
               <TextBlock Text="New Feature" Style="{StaticResource BodyTextBlockStyle}"/>
               <Button Content="Click Me" Click="ActionButton_Click"/>
           </StackPanel>
       </Grid>
   </UserControl>
   ```

3. **Apply design tokens**
   - Replace hardcoded values with token references
   - Use `{StaticResource}` or `{ThemeResource}` for colors
   - Use generated `Tokens.xaml` resource dictionary

4. **Match layout precisely**
   - Use screenshot as pixel-perfect reference
   - Verify spacing matches token values
   - Test at different window sizes

**Translation Checklist:**
- [ ] Component structure matches React hierarchy
- [ ] All spacing uses design tokens
- [ ] All colors use design tokens or theme resources
- [ ] Layout is responsive (Grid with * columns/rows)
- [ ] Visual appearance matches screenshot

### Phase 4: Wire ViewModels and Data Bindings

**Time Estimate:** 10-15 minutes per component

1. **Create or update ViewModel**
   ```csharp
   // src/FluentPDF.App/ViewModels/MyNewComponentViewModel.cs
   public class MyNewComponentViewModel : ObservableObject
   {
       private string _title = "New Feature";
       public string Title
       {
           get => _title;
           set => SetProperty(ref _title, value);
       }

       public ICommand ActionCommand { get; }

       public MyNewComponentViewModel()
       {
           ActionCommand = new RelayCommand(ExecuteAction);
       }

       private void ExecuteAction()
       {
           // Implement actual logic
       }
   }
   ```

2. **Set up data binding in XAML**
   ```xml
   <UserControl x:Name="Root">
       <Grid>
           <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}"/>
           <Button Command="{x:Bind ViewModel.ActionCommand}" Content="Click Me"/>
       </Grid>
   </UserControl>
   ```

3. **Wire code-behind**
   ```csharp
   // src/FluentPDF.App/Controls/MyNewComponent.xaml.cs
   public sealed partial class MyNewComponent : UserControl
   {
       public MyNewComponentViewModel ViewModel { get; }

       public MyNewComponent()
       {
           ViewModel = new MyNewComponentViewModel();
           this.InitializeComponent();
       }
   }
   ```

### Phase 5: Add WinUI 3-Specific Features

**Time Estimate:** 5-20 minutes depending on feature complexity

Add features that were stubbed or omitted in the React prototype:

1. **Drag-and-drop** (if applicable)
   ```xml
   <Button CanDrag="True" DragStarting="Button_DragStarting">
   ```

2. **Context menus**
   ```xml
   <Button.ContextFlyout>
       <MenuFlyout>
           <MenuFlyoutItem Text="Edit" Click="Edit_Click"/>
           <MenuFlyoutItem Text="Delete" Click="Delete_Click"/>
       </MenuFlyout>
   </Button.ContextFlyout>
   ```

3. **Keyboard accelerators**
   ```xml
   <Button>
       <Button.KeyboardAccelerators>
           <KeyboardAccelerator Key="S" Modifiers="Control" Invoked="Save_Invoked"/>
       </Button.KeyboardAccelerators>
   </Button>
   ```

4. **Accessibility**
   ```xml
   <Button
       AutomationProperties.Name="Save Document"
       AutomationProperties.AutomationId="SaveButton">
   ```

5. **Actual functionality**
   - Replace `console.log` stubs with real business logic
   - Connect to services (PDF rendering, file I/O)
   - Implement error handling

---

## Design Token Workflow

Design tokens maintain visual consistency between React and XAML. Follow this workflow when modifying styling:

### 1. Identify Token to Modify

**Common scenarios:**
- Changing brand color: `tokens.colors.primary`
- Adjusting spacing: `tokens.spacing.sm`, `tokens.spacing.md`
- Typography updates: `tokens.typography.fontSize.base`

### 2. Edit tokens.json

```bash
# Open token file
code design-tokens/tokens.json
```

**Example change:**
```json
{
  "colors": {
    "primary": "#0078D4"  // Changed from #512BD4
  },
  "spacing": {
    "md": 20  // Changed from 16
  }
}
```

### 3. Regenerate Token Files

```bash
cd prototype
npm run generate-tokens
```

**Output:**
```
✓ Validated tokens.json against schema
✓ Generated prototype/src/styles/tokens.css
✓ Generated design-tokens/Tokens.xaml
```

### 4. Verify Changes

**In React (instant):**
1. Dev server automatically reloads CSS
2. Check browser - changes appear immediately
3. Verify color/spacing updates are correct

**In XAML:**
1. Copy `design-tokens/Tokens.xaml` to `src/FluentPDF.App/` (if not already using symlink)
2. Rebuild WinUI 3 project: `dotnet build src/FluentPDF.App -p:Platform=x64`
3. Launch app and verify changes match React

### 5. Visual Comparison

Take screenshots of equivalent components in React and XAML:
- Colors should match exactly
- Spacing should match within 1-2px tolerance
- Typography sizes and weights should match

**Token Validation Checklist:**
- [ ] `tokens.json` validates against schema (automatically checked on generation)
- [ ] CSS variables work in browser DevTools
- [ ] XAML resources compile without errors
- [ ] React and XAML visual appearance matches
- [ ] No hardcoded values remain in component code

---

## Setup Instructions

### Prerequisites

**Required:**
- **Node.js 20.x or higher** - [Download](https://nodejs.org/)
- **npm 10.x or higher** (included with Node.js)
- **Modern browser** (Chrome, Edge, Firefox - for React dev)

**Verify installation:**
```bash
node --version   # Should show v20.x.x or higher
npm --version    # Should show 10.x.x or higher
```

### First-Time Setup

1. **Navigate to prototype directory**
   ```bash
   cd prototype
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```
   This installs React, TypeScript, Vite, and other dependencies.

3. **Generate design tokens**
   ```bash
   npm run generate-tokens
   ```
   This creates `src/styles/tokens.css` and `design-tokens/Tokens.xaml`.

4. **Start development server**
   ```bash
   npm run dev
   ```
   Server starts on http://localhost:5173.

5. **Verify hot reload works**
   - Open http://localhost:5173 in browser
   - Edit `src/App.tsx`
   - Save file
   - Browser should update in <500ms

**Total setup time:** <5 minutes

### Daily Development Workflow

```bash
# 1. Start dev server (one time per session)
cd prototype
npm run dev

# 2. Open browser
# Navigate to http://localhost:5173

# 3. Edit components in src/
# Save files and watch browser update automatically

# 4. Regenerate tokens if styling changes (as needed)
npm run generate-tokens
```

---

## Troubleshooting

### Port 5173 Already in Use

**Symptom:** `Error: Port 5173 is already in use`

**Solution (Windows):**
```bash
# Find process using port 5173
netstat -ano | findstr :5173

# Kill process by PID
taskkill /PID <PID> /F
```

**Solution (Linux/macOS):**
```bash
# Find and kill process using port 5173
lsof -ti:5173 | xargs kill -9
```

**Alternative:** Change port in `vite.config.ts`:
```ts
export default defineConfig({
  server: {
    port: 5174  // Use different port
  }
})
```

### TypeScript Errors

**Symptom:** Red squiggles in editor, build fails with type errors

**Solutions:**

1. **Run type checking**
   ```bash
   npm run typecheck
   ```
   This shows all TypeScript errors without building.

2. **Clear cache and reinstall**
   ```bash
   rm -rf node_modules package-lock.json
   npm install
   ```

3. **Check tsconfig.json**
   Ensure strict mode is enabled and paths are correct.

4. **Restart TypeScript server** (VS Code)
   - Open Command Palette (Ctrl+Shift+P)
   - Type "TypeScript: Restart TS Server"

### Slow Hot Reload (>2 seconds)

**Symptom:** Changes take longer than 500ms to appear in browser

**Diagnosis:**
1. Check browser console for errors
2. Verify you're editing files inside `src/` directory
3. Check for infinite loops in component code

**Solutions:**

1. **Disable browser extensions**
   - Ad blockers, security extensions can slow down HMR
   - Test in incognito/private browsing mode

2. **Restart dev server**
   ```bash
   # Press Ctrl+C in terminal, then:
   npm run dev
   ```

3. **Clear Vite cache**
   ```bash
   rm -rf node_modules/.vite
   npm run dev
   ```

4. **Check file watcher limits** (Linux)
   ```bash
   # Increase inotify watchers
   echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
   sudo sysctl -p
   ```

### Node Version Mismatch

**Symptom:** `Error: The engine "node" is incompatible with this module`

**Solution:**
1. Check current Node.js version
   ```bash
   node --version
   ```

2. Install Node.js 20.x from https://nodejs.org/

3. Verify npm version
   ```bash
   npm --version  # Should be 10.x or higher
   ```

4. Reinstall dependencies
   ```bash
   rm -rf node_modules package-lock.json
   npm install
   ```

### Design Token Generation Fails

**Symptom:** `npm run generate-tokens` fails with validation error

**Common causes:**

1. **Invalid JSON syntax**
   ```
   Error: Unexpected token } in JSON at position 123
   ```
   **Fix:** Check for trailing commas, missing quotes, unclosed braces

2. **Schema validation failure**
   ```
   Error: tokens.json does not match schema
   ```
   **Fix:** Verify color format is `#RRGGBB`, spacing values are integers

3. **Missing required fields**
   ```
   Error: Required property 'colors.primary' is missing
   ```
   **Fix:** Add missing token to `tokens.json`

**Debugging steps:**
```bash
# Validate JSON syntax
cat design-tokens/tokens.json | jq .

# Run generator with verbose output
cd prototype
npm run generate-tokens
```

### CSS Custom Properties Not Working

**Symptom:** Design tokens don't apply in React components

**Solutions:**

1. **Verify tokens.css is imported**
   Check `src/main.tsx` imports:
   ```tsx
   import './styles/tokens.css';
   ```

2. **Check browser DevTools**
   - Open DevTools (F12)
   - Go to Elements tab
   - Inspect `:root` element
   - Verify `--color-primary`, `--spacing-md`, etc. are defined

3. **Restart dev server**
   ```bash
   npm run dev
   ```

4. **Regenerate tokens**
   ```bash
   npm run generate-tokens
   npm run dev
   ```

### XAML Won't Compile with Generated Tokens

**Symptom:** WinUI 3 build fails with XAML errors after using `Tokens.xaml`

**Solutions:**

1. **Verify Tokens.xaml syntax**
   Open `design-tokens/Tokens.xaml` and check for:
   - Valid XAML namespace declarations
   - PascalCase resource keys
   - Correct color format (`#RRGGBB`)

2. **Check ResourceDictionary is merged**
   In `src/FluentPDF.App/App.xaml`:
   ```xml
   <Application.Resources>
       <ResourceDictionary>
           <ResourceDictionary.MergedDictionaries>
               <ResourceDictionary Source="Tokens.xaml"/>
           </ResourceDictionary.MergedDictionaries>
       </ResourceDictionary>
   </Application.Resources>
   ```

3. **Rebuild clean**
   ```bash
   dotnet clean src/FluentPDF.App
   dotnet build src/FluentPDF.App -p:Platform=x64
   ```

---

## CI Integration

The React prototype includes automated build verification in CI/CD pipelines.

### GitHub Actions Workflow

**File:** `.github/workflows/prototype-build.yml`

**What it does:**
- Runs on pull requests affecting `prototype/` or `design-tokens/` directories
- Installs Node.js 20.x
- Installs dependencies with `npm ci`
- Runs TypeScript type checking with `npm run typecheck`
- Builds production bundle with `npm run build`
- Fails PR if build or type errors occur

**Workflow configuration:**
```yaml
name: React Prototype Build

on:
  pull_request:
    paths:
      - 'prototype/**'
      - 'design-tokens/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '20'
      - run: cd prototype && npm ci
      - run: cd prototype && npm run typecheck
      - run: cd prototype && npm run build
```

### Local CI Verification

Before pushing, verify your changes pass CI checks:

```bash
cd prototype

# Install dependencies (clean install)
npm ci

# Run type checking
npm run typecheck

# Build for production
npm run build
```

All commands should succeed with no errors.

### Build Status Badge

Add this badge to `prototype/README.md`:

```markdown
[![Prototype Build](https://github.com/yourusername/FluentPDF/actions/workflows/prototype-build.yml/badge.svg)](https://github.com/yourusername/FluentPDF/actions/workflows/prototype-build.yml)
```

---

## Workflow Optimization Tips

### 1. Use Component Templates

Create a template script to scaffold new components quickly:

```bash
# tools/new-component.sh
COMPONENT_NAME=$1
mkdir -p src/components
cat > src/components/${COMPONENT_NAME}.tsx <<EOF
import React from 'react';
import styles from './${COMPONENT_NAME}.module.css';

export const ${COMPONENT_NAME}: React.FC = () => (
  <div className={styles.container}>
    {/* Component content */}
  </div>
);
EOF

cat > src/components/${COMPONENT_NAME}.module.css <<EOF
.container {
  padding: var(--spacing-md);
}
EOF

echo "Created ${COMPONENT_NAME} component"
```

**Usage:**
```bash
./tools/new-component.sh MyNewComponent
```

### 2. Keep Dev Server Running

Leave the dev server running in a dedicated terminal:
- No startup delay when switching to prototype work
- Hot reload always available
- Browser stays in sync

### 3. Use Browser DevTools Workspace

Enable DevTools workspace to edit CSS directly in browser:
1. Open Chrome DevTools (F12)
2. Go to Sources → Filesystem
3. Add `prototype/src` folder
4. Edit CSS in DevTools, changes persist to files

### 4. Split-Screen Development

Arrange windows for efficient workflow:
```
┌────────────────┬────────────────┐
│                │                │
│  Code Editor   │  Browser       │
│  (VS Code)     │  (React app)   │
│                │                │
├────────────────┼────────────────┤
│                │                │
│  XAML Editor   │  WinUI 3 App   │
│  (VS/VS Code)  │  (running)     │
│                │                │
└────────────────┴────────────────┘
```

### 5. Bookmark Common Documentation

Keep these docs open in browser tabs:
- `docs/component-mapping.md` - Component translation reference
- `design-tokens/README.md` - Token usage guide
- `prototype/README.md` - Quick command reference

---

## Summary

The React prototype workflow enables rapid UI iteration without WinUI 3 build overhead:

**Workflow Phases:**
1. **Design in React** (5-15 min) - Fast iteration with hot reload
2. **Screenshot** (1-2 min) - Visual reference capture
3. **Translate to XAML** (10-20 min) - Structure conversion using component mapping
4. **Wire ViewModels** (10-15 min) - Data binding and business logic
5. **Add WinUI 3 features** (5-20 min) - Platform-specific functionality

**Design Token Workflow:**
1. Edit `tokens.json`
2. Run `npm run generate-tokens`
3. Verify in React (instant)
4. Verify in XAML (rebuild required)

**Key Files:**
- `prototype/README.md` - Quick setup and commands
- `docs/component-mapping.md` - React ↔ XAML translation guide
- `design-tokens/README.md` - Design token system documentation

**Common Commands:**
```bash
npm run dev              # Start dev server
npm run generate-tokens  # Regenerate design tokens
npm run typecheck        # Check TypeScript types
npm run build            # Build for production
```

**Next Steps:**
- Read `docs/component-mapping.md` for component translation patterns
- Review existing components in `prototype/src/components/` for examples
- Experiment with design tokens in `design-tokens/tokens.json`
- Try creating a new component following the workflow
