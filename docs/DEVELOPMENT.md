# Development Guide

This document provides guidance for developers working on FluentPDF.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Building the Project](#building-the-project)
- [Running Tests](#running-tests)
- [Observability with .NET Aspire Dashboard](#observability-with-net-aspire-dashboard)
- [Code Quality](#code-quality)

## Prerequisites

- .NET 8 SDK
- Visual Studio 2022 (for WinUI 3 development on Windows)
- Docker (optional, for Aspire Dashboard)

## Building the Project

### Cross-platform Components (Linux/macOS/Windows)

```bash
# Build Core library
dotnet build src/FluentPDF.Core

# Build Rendering library
dotnet build src/FluentPDF.Rendering

# Run Core tests
dotnet test tests/FluentPDF.Core.Tests
```

### Windows-only Components (WinUI 3)

**Note:** WinUI 3 requires Windows and x64 platform.

```bash
# Build WinUI 3 application
dotnet build src/FluentPDF.App -p:Platform=x64

# Run App tests
dotnet test tests/FluentPDF.App.Tests

# Run Architecture tests
dotnet test tests/FluentPDF.Architecture.Tests
```

### Building on Remote Windows PC via SSH

```bash
# SSH to Windows PC
ssh ryosu@192.168.11.48

# Project location on Windows
cd C:\dev\FluentPDF

# Build and test
dotnet build src/FluentPDF.App -p:Platform=x64
dotnet test
```

### Syncing Files to Windows PC

```bash
# From Linux/macOS, sync files to Windows PC
scp -r src tests *.sln Directory.Build.props ryosu@192.168.11.48:"C:/dev/FluentPDF/"
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/FluentPDF.Core.Tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Observability with .NET Aspire Dashboard

FluentPDF provides comprehensive observability through three integrated layers:

1. **Development-time monitoring**: .NET Aspire Dashboard for OpenTelemetry data
2. **In-app diagnostics**: Real-time performance overlay (`Ctrl+Shift+D`)
3. **In-app log viewer**: Structured log browser with filtering (`Ctrl+Shift+L`)

See [OBSERVABILITY.md](OBSERVABILITY.md) for complete usage guide.

### What is .NET Aspire Dashboard?

.NET Aspire Dashboard is a standalone web-based UI for viewing OpenTelemetry data during development. It provides:

- **Metrics**: Real-time performance metrics (FPS, memory, render times)
- **Traces**: Distributed tracing for rendering pipeline
- **Logs**: Structured log viewer with filtering

FluentPDF also includes in-app observability features that work without the dashboard:
- **Diagnostics Panel** (`Ctrl+Shift+D`): Acrylic overlay showing FPS, memory, render times
- **Log Viewer** (`Ctrl+Shift+L`): In-app browser for Serilog JSON logs with advanced filtering

### Starting the Aspire Dashboard

The Aspire Dashboard runs in a Docker container and receives telemetry data via OTLP (OpenTelemetry Protocol).

```bash
# Navigate to tools directory
cd tools

# Start the Aspire Dashboard
docker-compose -f docker-compose-aspire.yml up -d

# View logs
docker-compose -f docker-compose-aspire.yml logs -f

# Stop the dashboard
docker-compose -f docker-compose-aspire.yml down
```

### Accessing the Dashboard

Once the container is running, access the dashboard at:

**http://localhost:18888**

### How It Works

FluentPDF automatically sends telemetry data to the Aspire Dashboard when it's running:

1. **Metrics** - `IMetricsCollectionService` exports metrics via OTLP to `localhost:4317`
2. **Traces** - Distributed tracing activities are exported via OTLP to `localhost:4317`
3. **Logs** - Serilog sends structured logs via OpenTelemetry sink to `localhost:4317`

**Graceful Degradation**: If the Aspire Dashboard is not running, FluentPDF continues to operate normally. Telemetry export failures are logged as warnings but do not impact application functionality.

### Viewing Telemetry Data

#### Metrics Tab
- View real-time FPS (frames per second)
- Monitor managed and native memory usage
- Track render times per page

#### Traces Tab
- View distributed traces for PDF rendering operations
- See detailed spans for LoadPage, RenderBitmap, ConvertToImage
- Track correlation IDs across operations

#### Logs Tab
- View structured logs with JSON formatting
- Filter by severity, component, correlation ID
- Search log messages

### Configuration

OpenTelemetry is configured in `src/FluentPDF.App/App.xaml.cs`:

- **OTLP Endpoint**: `http://localhost:4317` (gRPC)
- **Service Name**: `FluentPDF.Desktop`
- **Meter Name**: `FluentPDF.Rendering`
- **Activity Source**: `FluentPDF.Rendering`

Serilog OpenTelemetry sink is configured in `src/FluentPDF.Core/Logging/SerilogConfiguration.cs`.

### Troubleshooting

#### Dashboard Not Receiving Data

1. Check if the container is running:
   ```bash
   docker ps | grep aspire-dashboard
   ```

2. Check application logs for OpenTelemetry warnings:
   ```bash
   # Logs are in ApplicationData.LocalFolder/logs or %TEMP%/FluentPDF/logs
   ```

3. Verify OTLP endpoint is accessible:
   ```bash
   nc -zv localhost 4317
   ```

#### Port Conflicts

If ports 4317 or 18888 are already in use, modify `tools/docker-compose-aspire.yml`:

```yaml
ports:
  - "14317:4317"  # Change 4317 to 14317
  - "18889:18888" # Change 18888 to 18889
```

Then update `App.xaml.cs` and `SerilogConfiguration.cs` to match the new port.

## Code Quality

### Pre-commit Hooks

FluentPDF enforces code quality standards through pre-commit hooks (future implementation):

- Linting
- Formatting
- Unit tests

### Code Metrics (KPI)

Excluding comments and blank lines:
- Max 500 lines per file
- Max 50 lines per function
- 80% test coverage minimum (90% for critical paths)

### Architecture Principles

- **SOLID**: Single responsibility, Open/closed, Liskov substitution, Interface segregation, Dependency inversion
- **DI**: Dependency injection mandatory for all services
- **SSOT**: Single source of truth
- **KISS**: Keep it simple, stupid
- **SLAP**: Single level of abstraction principle

### Error Handling

- **Fail fast**: Validate at entry, reject invalid immediately
- **Structured logging**: JSON format with timestamp, level, service, event, context
- **Custom exceptions**: Exception hierarchy with error codes
- **Never log secrets/PII**: Sanitize sensitive data

### Development Workflow

1. **CLI first, GUI later**: Implement business logic in Core/Rendering, UI in App
2. **Debug mode mandatory**: All services must support debug logging
3. **No backward compatibility required**: Break APIs freely unless explicitly requested
