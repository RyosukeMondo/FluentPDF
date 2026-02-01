# FluentPDF - Claude Flow Integration

## Overview

FluentPDF is a PDF viewer for Windows built with WinUI 3 and PDFium. This document describes the complete claude-flow integration with DDD domains, specialized agents, and development workflows.

## Directory Structure

```
.claude-flow/
├── config.yaml              # Runtime configuration
├── PROJECT.md               # This file
├── CAPABILITIES.md          # Claude Flow V3 reference
├── domains.yaml             # DDD domain definitions
├── agents/                  # 10 domain-specific agents
│   ├── pdf-rendering-specialist.yaml
│   ├── interop-validator.yaml
│   ├── ui-coordinator.yaml
│   ├── verification-engineer.yaml
│   ├── observability-analyst.yaml
│   ├── document-operations-specialist.yaml
│   ├── form-specialist.yaml
│   ├── annotation-specialist.yaml
│   ├── content-specialist.yaml
│   └── conversion-specialist.yaml
├── workflows/               # 11 development workflows
│   ├── pdf-feature.yaml
│   ├── rendering-fix.yaml
│   ├── verification-run.yaml
│   ├── performance-audit.yaml
│   ├── ui-change.yaml
│   ├── document-operations.yaml
│   ├── form-feature.yaml
│   ├── annotation-feature.yaml
│   ├── interop-audit.yaml
│   ├── observability-setup.yaml
│   └── claude-flow-verify.yaml   # NEW: Integration validation
├── hooks/                   # 4 automation hooks
│   ├── pre-task.yaml
│   ├── post-edit.yaml
│   ├── session-start.yaml
│   └── route.yaml
├── data/                    # Memory storage (HNSW)
├── sessions/                # Session persistence
├── metrics/                 # Learning and metrics
└── logs/                    # Operation logs
```

## DDD Domains

FluentPDF is organized into bounded contexts and domains:

| Domain | Description | Aggregate Root |
|--------|-------------|----------------|
| **Document** | PDF lifecycle, merge, split, optimize | PdfDocument |
| **Rendering** | Page rendering, thumbnails, DPI | DisplayInfo |
| **Annotation** | Highlights, markup, ink | Annotation |
| **Form** | AcroForm fields, validation | PdfFormField |
| **Bookmark** | PDF outlines, navigation | BookmarkNode |
| **Content** | Text extraction, search, images | SearchMatch |
| **Observability** | Logging, metrics, diagnostics | PerformanceMetrics |
| **Interop** | P/Invoke, safe handles | SafePdfDocumentHandle |
| **Presentation** | ViewModels, UI, settings | MainViewModel |
| **Conversion** | Office to PDF | ConversionResult |
| **Validation** | PDF/A compliance | QualityReport |

See `domains.yaml` for complete domain definitions including entities, value objects, and service mappings.

## Agents

### Core Agents

| Agent | Domain | Use For |
|-------|--------|---------|
| `pdf-rendering-specialist` | Rendering | PDFium, thumbnails, DPI |
| `interop-validator` | Interop | P/Invoke, marshalling, crashes |
| `ui-coordinator` | Presentation | WinUI 3, MVVM, ViewModels |
| `verification-engineer` | Testing | E2E, API, diagnostics |
| `observability-analyst` | Observability | Logging, metrics, errors |

### Domain Specialists

| Agent | Domain | Use For |
|-------|--------|---------|
| `document-operations-specialist` | Document | Merge, split, optimize (QPDF) |
| `form-specialist` | Form | Field detection, validation, filling |
| `annotation-specialist` | Annotation | Highlights, markup, drawing |
| `content-specialist` | Content | Text extraction, search, images |
| `conversion-specialist` | Conversion | Word/HTML to PDF |

## Workflows

| Workflow | Trigger | Phases |
|----------|---------|--------|
| `pdf-feature` | "add feature" | specification → implementation → ui → verification → review |
| `rendering-fix` | "crash", "bug" | diagnose → fix → validate → document |
| `verification-run` | "verify", "e2e" | setup → document-tests → rendering-tests → regression → report |
| `performance-audit` | "slow", "optimize" | baseline → profile → optimize → validate → document |
| `ui-change` | "ui", "design" | prototype → tokens → implement → validate |
| `document-operations` | "merge", "split" | analysis → implementation → validation |
| `form-feature` | "form", "field" | interop → service → ui → testing |
| `annotation-feature` | "annotation" | interop → service → ui → testing |
| `interop-audit` | "marshalling review" | inventory → validation → fix → report |
| `observability-setup` | "diagnostics" | services → models → ui → instrumentation |

## Quick Commands

```bash
# Build
dotnet build src/FluentPDF.App -p:Platform=x64

# Test
dotnet test tests/FluentPDF.Core.Tests
dotnet test tests/FluentPDF.Architecture.Tests
dotnet test tests/FluentPDF.Verification.Pdfium.Tests

# Diagnostics
FluentPDF.App.exe --diagnostics
FluentPDF.App.exe --test-render "path/to/test.pdf"
FluentPDF.App.exe --validate-marshalling
FluentPDF.App.exe --api-server --port 5000
```

## Domain-Agent Routing

Tasks are automatically routed based on keywords:

| Keywords | Domain | Agent |
|----------|--------|-------|
| render, thumbnail, dpi | Rendering | pdf-rendering-specialist |
| marshal, interop, crash | Interop | interop-validator |
| ui, xaml, viewmodel | Presentation | ui-coordinator |
| merge, split, optimize | Document | document-operations-specialist |
| form, field, checkbox | Form | form-specialist |
| annotation, highlight | Annotation | annotation-specialist |
| search, text, image | Content | content-specialist |
| convert, docx, html | Conversion | conversion-specialist |
| log, metric, error | Observability | observability-analyst |
| test, e2e, verify | Testing | verification-engineer |

## Spec Workflow Integration

FluentPDF uses `.spec-workflow/` for feature specifications:

| Spec | Domain | Status |
|------|--------|--------|
| document-structure-operations | Document | Implemented |
| form-filling | Form | Implemented |
| annotation-tools | Annotation | Implemented |
| bookmarks-panel | Bookmark | Implemented |
| observability-dashboard | Observability | Implemented |
| hidpi-display-scaling | Rendering | Implemented |
| text-extraction-search | Content | Implemented |
| image-insertion | Content | In Progress |
| watermarks | Watermark | In Progress |
| office-document-conversion | Conversion | In Progress |

## Using the System

### Automatic Routing
Simply describe your task - the hooks will suggest the right agent and workflow:

```
"Fix the crash when rendering page 5"
→ Agent: interop-validator
→ Workflow: rendering-fix
→ Context: Interop/*.cs, Verification/*.cs
```

### Explicit Agent Request
```
"Use the form-specialist to add dropdown support"
"Have the interop-validator audit marshalling"
```

### Explicit Workflow
```
"Start the form-feature workflow for checkbox validation"
"Run the interop-audit workflow"
```

## MCP Integration

The claude-flow MCP server provides programmatic access:

```bash
# Memory operations
mcp__claude-flow__memory_store
mcp__claude-flow__memory_search

# Agent operations
mcp__claude-flow__agent_spawn
mcp__claude-flow__agent_status

# Workflow operations
mcp__claude-flow__workflow_execute
mcp__claude-flow__workflow_status
```

## Bounded Contexts

| Context | Domains | Description |
|---------|---------|-------------|
| **pdf_core** | document, rendering, interop | Core PDF operations |
| **pdf_editing** | annotation, form, content, watermark | Content modification |
| **pdf_navigation** | bookmark | Document navigation |
| **pdf_quality** | validation, observability | Quality assurance |
| **office_integration** | conversion | Office document support |
| **user_experience** | presentation | Application UI |

## Learning System

The hooks system learns from development patterns:

- **pre-task**: Learns which agents succeed for which tasks
- **post-edit**: Learns which tests catch which issues
- **route**: Improves routing accuracy over time

Metrics stored in `.claude-flow/metrics/learning.json`.

## Knowledge Base

The knowledge base provides context and guidance:

| File | Purpose |
|------|---------|
| `known-issues.yaml` | PDFium/QPDF/WinUI workarounds and regression tests |
| `error-codes.yaml` | Centralized error code registry |
| `quality-gates.yaml` | Domain-specific quality criteria |
| `performance-baselines.yaml` | Performance benchmarks and targets |
| `spec-traceability.yaml` | Spec-to-implementation mapping |
| `cli-commands.yaml` | CLI command reference |
| `architecture.yaml` | Service dependencies and patterns |
| `test-wiring.yaml` | Test project routing and domain mapping |
| `index.yaml` | Knowledge base index and lookup rules |

## Verification

Run the verification workflow to validate the integration:

```bash
# MCP verification (via claude-flow tools)
mcp__claude-flow__system_health

# CLI verification
ls .claude-flow/agents/*.yaml | wc -l   # Should be 10+
ls .claude-flow/workflows/*.yaml | wc -l # Should be 11+
ls .claude-flow/hooks/*.yaml | wc -l    # Should be 4+

# Test suite verification
dotnet test tests/FluentPDF.Architecture.Tests
dotnet test tests/FluentPDF.Core.Tests
```

## Test Wiring

Tests are automatically routed to appropriate agents based on domain:

| Domain | Test Projects | Agent |
|--------|--------------|-------|
| Core | FluentPDF.Core.Tests, FluentPDF.Architecture.Tests | coder |
| Rendering | FluentPDF.Rendering.Tests (rendering) | pdf-rendering-specialist |
| Interop | FluentPDF.Verification.Pdfium.Tests | interop-validator |
| Form | FluentPDF.Rendering.Tests (forms) | form-specialist |
| Annotation | FluentPDF.Rendering.Tests (annotations) | annotation-specialist |
| E2E | FluentPDF.E2E.Tests | verification-engineer |
| UI | FluentPDF.App.Tests | ui-coordinator |

See `knowledge/test-wiring.yaml` for complete routing configuration.
