# FluentPDF React Prototype

[![React Prototype Build](https://github.com/FluentPDF/FluentPDF/actions/workflows/prototype-build.yml/badge.svg)](https://github.com/FluentPDF/FluentPDF/actions/workflows/prototype-build.yml)

React + TypeScript prototype environment for rapid FluentPDF UI iteration with instant hot reload.

## Purpose

This prototype enables developers to:
- **Rapid UI iteration**: Design and iterate on UI components in React with instant hot module replacement (<500ms)
- **Visual experimentation**: Test layouts, spacing, and design tokens without WinUI 3 build overhead
- **Screenshot-driven development**: Create React prototypes → screenshot → translate to XAML
- **Design token synchronization**: Maintain visual consistency between React and WinUI 3 through shared design tokens

## Prerequisites

- **Node.js**: 20.x or higher (check: `node --version`)
- **npm**: 10.x or higher (check: `npm --version`)

## Setup Instructions

1. **Install dependencies**:
   ```bash
   cd prototype
   npm install
   ```

2. **Start development server**:
   ```bash
   npm run dev
   ```
   Server starts on http://localhost:5173 with hot module replacement enabled.

3. **Verify hot reload**:
   - Edit `src/App.tsx`
   - Save the file
   - Browser should update instantly (<500ms)

## Available Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start development server on port 5173 with hot reload |
| `npm run build` | Build for production (outputs to `dist/`) |
| `npm run typecheck` | Run TypeScript type checking without building |
| `npm run preview` | Preview production build locally |
| `npm run generate-tokens` | Regenerate CSS and XAML design token files from `tokens.json` |

## Project Structure

```
prototype/
├── src/
│   ├── main.tsx           # Application entry point
│   ├── App.tsx            # Root component
│   ├── App.css            # Root component styles
│   ├── index.css          # Global styles
│   ├── components/        # React components (TBD)
│   ├── types/             # TypeScript type definitions (TBD)
│   ├── data/              # Dummy data generators (TBD)
│   └── styles/            # Design token CSS (TBD)
├── tools/                 # Build tools and generators (TBD)
├── index.html             # HTML entry point
├── vite.config.ts         # Vite configuration
├── tsconfig.json          # TypeScript configuration (strict mode)
└── package.json           # Dependencies and scripts
```

## Development Workflow

**Quick Reference:**
1. **Design in React** (5-15 min): Create or modify components in `src/` with instant hot reload
2. **Screenshot** (1-2 min): Capture finalized UI for visual reference
3. **Translate to XAML** (10-20 min): Use component mapping guide to convert structure
4. **Wire ViewModels** (10-15 min): Connect XAML to ViewModels and data bindings
5. **Add WinUI 3 features** (5-20 min): Implement platform-specific features (drag/drop, context menus, etc.)

**Detailed workflow:** See [docs/react-prototype-workflow.md](../docs/react-prototype-workflow.md) for comprehensive workflow guide with troubleshooting, CI integration, and optimization tips.

## TypeScript Configuration

- **Strict mode enabled**: Catch errors early with strict type checking
- **ESNext modules**: Modern JavaScript features and bundler-friendly module resolution
- **React JSX**: JSX transformation using React 18 automatic runtime
- **No unused code**: Enforces cleanup of unused variables and parameters

## Hot Module Replacement (HMR)

Vite's HMR provides instant feedback:
- **<500ms reload time**: Changes appear almost instantly
- **State preservation**: Component state preserved across most edits
- **Error overlay**: TypeScript and runtime errors shown in browser

## Design Tokens Integration

Design tokens (colors, spacing, typography) are shared between React and XAML:
- **Source of truth**: `design-tokens/tokens.json`
- **CSS generation**: `npm run generate-tokens` → `src/styles/tokens.css`
- **XAML generation**: `npm run generate-tokens` → `design-tokens/Tokens.xaml`

See `design-tokens/README.md` for token workflow documentation.

## Troubleshooting

### Port 5173 already in use
```bash
# Kill the process using port 5173 (Windows)
netstat -ano | findstr :5173
taskkill /PID <PID> /F

# Kill the process using port 5173 (Linux/macOS)
lsof -ti:5173 | xargs kill -9
```

### TypeScript errors
```bash
# Run type checking
npm run typecheck

# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Slow hot reload
- Check browser console for errors
- Disable browser extensions
- Ensure you're editing files inside `src/` directory
- Restart dev server: `Ctrl+C` then `npm run dev`

### Node version mismatch
```bash
# Check current version
node --version

# Install Node 20.x from https://nodejs.org/
```

## When to Use This Prototype

**✅ Use for:**
- Complex UI components with multiple panels or intricate layouts
- Visual design iteration (colors, spacing, typography)
- Testing component composition and visual hierarchy
- Layout experimentation and responsive design

**❌ Skip for:**
- Simple dialogs or minor XAML tweaks
- Backend-heavy features without UI changes
- Single-value adjustments (margins, labels)

See [workflow documentation](../docs/react-prototype-workflow.md) for detailed guidance.

## Resources

- **FluentPDF Documentation:**
  - [React Prototype Workflow](../docs/react-prototype-workflow.md) - Complete development workflow guide
  - [Component Mapping Guide](../docs/component-mapping.md) - React ↔ XAML translation reference
  - [Design Tokens](../design-tokens/README.md) - Design token system documentation
- **External Documentation:**
  - [Vite Documentation](https://vitejs.dev/)
  - [React Documentation](https://react.dev/)
  - [TypeScript Handbook](https://www.typescriptlang.org/docs/)
