# FluentPDF - Claude Code Context

## Project Structure
- `src/FluentPDF.Core` - Business logic (.NET 8, cross-platform)
- `src/FluentPDF.Rendering` - PDF rendering (cross-platform)
- `src/FluentPDF.App` - WinUI 3 UI (Windows-only)
- `tests/` - xUnit tests

## Build Commands
```bash
# Cross-platform (Linux/macOS/Windows)
dotnet build src/FluentPDF.Core
dotnet build src/FluentPDF.Rendering
dotnet test tests/FluentPDF.Core.Tests

# Windows-only (WinUI 3) - requires x64 platform
dotnet build src/FluentPDF.App -p:Platform=x64
dotnet test tests/FluentPDF.Architecture.Tests
dotnet test tests/FluentPDF.App.Tests
```

## Windows Build Environment

### SSH to Windows PC
```bash
ssh ryosu@192.168.11.48
# Project synced to: C:\dev\FluentPDF
```

### Sync files to Windows
```bash
scp -r src tests *.sln Directory.Build.props ryosu@192.168.11.48:"C:/dev/FluentPDF/"
```

### Alternative: Vagrant Windows VM
```bash
cd ~/vagrant/windows-vm
vagrant up && vagrant ssh  # Start and connect
vagrant rsync              # Sync files to C:\vagrant
```

## CLI Diagnostic Commands

FluentPDF supports command-line diagnostic operations for testing and troubleshooting:

```bash
# Test rendering of a PDF file (returns exit code for automation)
FluentPDF.App.exe --test-render "path/to/file.pdf"
# Exit codes: 0=success, 1=load failed, 2=render failed, 3=UI failed

# Display system diagnostics (OS, .NET, PDFium version, memory)
FluentPDF.App.exe --diagnostics

# Render all pages of a PDF to PNG files
FluentPDF.App.exe --render-test "path/to/file.pdf" --output "output/directory"

# Enable verbose logging for any command
FluentPDF.App.exe --diagnostics --verbose

# Capture crash dumps on failures (Windows Error Reporting)
FluentPDF.App.exe --test-render "file.pdf" --capture-crash-dump
```

These commands execute without showing UI and are useful for:
- Automated testing in CI/CD pipelines
- Troubleshooting rendering issues
- Performance profiling
- Validating PDFium integration

## React Prototype Workflow

FluentPDF includes a React prototype environment for rapid UI iteration without WinUI 3 build overhead. Use this for complex layouts, new features, and visual experimentation.

### When to Use the Prototype

**Use for:**
- New complex UI components (multi-panel layouts, complex dialogs)
- Layout experimentation and responsive design testing
- Visual design iteration (colors, spacing, typography)
- Component composition and hierarchy validation

**Skip for:**
- Simple dialogs or minor XAML tweaks
- Single-value changes (margins, labels)
- Backend-heavy features without UI changes
- XAML-specific features (drag-and-drop, complex bindings)

### Key Commands

```bash
# Start development server (instant hot reload <500ms)
cd prototype
npm run dev                # http://localhost:5173

# Regenerate design tokens (React CSS + XAML)
npm run generate-tokens

# Verify build (TypeScript + production build)
npm run typecheck
npm run build
```

### Development Workflow

1. **Design in React** - Edit components in `prototype/src/components/`, save, see changes instantly
2. **Screenshot** - Capture finalized UI for reference
3. **Translate to XAML** - Use `docs/component-mapping.md` for React ↔ XAML patterns
4. **Wire ViewModels** - Add data bindings and business logic
5. **Add WinUI 3 features** - Implement drag-and-drop, context menus, keyboard shortcuts

### Design Tokens

Design tokens maintain visual consistency between React and XAML:
- Source of truth: `design-tokens/tokens.json`
- Generated outputs: `prototype/src/styles/tokens.css` (React), `design-tokens/Tokens.xaml` (WinUI 3)
- Workflow: Edit JSON → Run `npm run generate-tokens` → Verify in both UIs

### Documentation

- `prototype/README.md` - Setup and quick reference
- `docs/component-mapping.md` - React ↔ XAML component translation guide
- `docs/react-prototype-workflow.md` - Complete workflow documentation

## Verification API Server

FluentPDF includes an embedded REST API server for autonomous E2E verification without UAT.

### CLI Commands

```bash
# Start API server on default port (5000)
FluentPDF.App.exe --api-server

# Start on custom port in headless mode (no UI)
FluentPDF.App.exe --api-server --port 8080 --headless

# Start with verbose logging
FluentPDF.App.exe --api-server --verbose
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/health | Health check (returns status, version, PDFium state) |
| POST | /api/document/load | Load PDF document (returns session ID) |
| GET | /api/document/{id} | Get document info |
| DELETE | /api/document/{id} | Close document |
| POST | /api/render | Render page to PNG |
| GET | /api/render/{id}/{page} | Render page to PNG (convenience) |
| POST | /api/verify/render | Verify page render against baseline hash |
| POST | /api/verify/batch | Batch verify multiple pages |

### Example Usage

```bash
# Health check
curl http://localhost:5000/api/health

# Load document
curl -X POST http://localhost:5000/api/document/load \
  -H "Content-Type: application/json" \
  -d '{"path":"C:/test.pdf"}'

# Render page 0 to PNG
curl http://localhost:5000/api/render/SESSION_ID/0 -o page0.png

# Verify page rendering
curl -X POST http://localhost:5000/api/verify/render \
  -H "Content-Type: application/json" \
  -d '{"documentId":"SESSION_ID","pageIndex":0}'
```

### Automated Verification Script

```bash
# Run autonomous verification
pwsh tools/verify-rendering.ps1

# With custom PDF and port
pwsh tools/verify-rendering.ps1 -PdfPath "path/to/test.pdf" -Port 8080
```

## Spec Workflow
Specs in `.spec-workflow/specs/`. Use `spec-status` tool to check progress.
