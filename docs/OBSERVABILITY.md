# FluentPDF Observability Guide

This document describes FluentPDF's comprehensive observability infrastructure, including development-time monitoring with .NET Aspire Dashboard, in-app diagnostics, and structured log viewing.

## Table of Contents

- [Overview](#overview)
- [.NET Aspire Dashboard Setup](#net-aspire-dashboard-setup)
- [In-App Diagnostics Panel](#in-app-diagnostics-panel)
- [Structured Log Viewer](#structured-log-viewer)
- [Metrics Export](#metrics-export)
- [Correlation ID Tracing](#correlation-id-tracing)
- [Architecture](#architecture)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)

## Overview

FluentPDF provides three levels of observability:

1. **Development-time monitoring**: .NET Aspire Dashboard integration with OpenTelemetry (OTLP) for real-time metrics, logs, and distributed traces
2. **In-app diagnostics**: Real-time performance overlay showing FPS, memory usage, and render times
3. **Structured log viewer**: In-app log browser with advanced filtering and export capabilities

All observability features are built on industry-standard technologies:
- **OpenTelemetry**: Metrics collection and distributed tracing
- **Serilog**: Structured JSON logging
- **OTLP (OpenTelemetry Protocol)**: Unified telemetry export to Aspire Dashboard

## .NET Aspire Dashboard Setup

The .NET Aspire Dashboard provides a unified view of metrics, logs, and traces during development.

### Prerequisites

- Docker installed and running
- .NET 8 SDK or later

### Quick Start

1. **Start the Aspire Dashboard**:
   ```bash
   docker run -d \
     --name aspire-dashboard \
     -p 4317:4317 \
     -p 18888:18888 \
     -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
     mcr.microsoft.com/dotnet/aspire-dashboard:8.0
   ```

2. **Launch FluentPDF**:
   ```bash
   dotnet run --project src/FluentPDF.App -p:Platform=x64
   ```

3. **Access the Dashboard**:
   Open your browser to http://localhost:18888

### Dashboard Features

#### Metrics View
- **FPS (Frames Per Second)**: Real-time rendering performance
- **Memory Usage**: Managed and native memory consumption
- **Render Times**: Histogram of page rendering durations
- **Page Operations**: Counters for page loads, renders, and exports

#### Logs View
- **Structured logs**: All Serilog logs with full context
- **Correlation ID**: Track operations across components
- **Component filtering**: Filter by namespace (Rendering, Core, App)
- **Severity levels**: Trace, Debug, Info, Warning, Error, Critical

#### Traces View
- **Distributed tracing**: Complete rendering pipeline visibility
- **Parent-child spans**:
  - `RenderPage` (parent)
    - `LoadPage` (child)
    - `RenderBitmap` (child)
    - `ConvertToImage` (child)
- **Span attributes**: page.number, zoom.level, correlation.id, render.time.ms

### Using docker-compose

For easier management, use the provided docker-compose file:

```bash
# Start Aspire Dashboard
docker-compose -f tools/docker-compose-aspire.yml up -d

# View logs
docker-compose -f tools/docker-compose-aspire.yml logs -f

# Stop and remove
docker-compose -f tools/docker-compose-aspire.yml down
```

**docker-compose-aspire.yml**:
```yaml
version: '3.8'
services:
  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:8.0
    ports:
      - "4317:4317"   # OTLP gRPC endpoint
      - "18888:18888" # Dashboard UI
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
    restart: unless-stopped
```

### Graceful Fallback

If the Aspire Dashboard is not running, FluentPDF continues to operate normally:
- OpenTelemetry OTLP exporter silently drops metrics/traces
- Logs are still written to local JSON files
- In-app diagnostics and log viewer remain fully functional

No errors are shown to the user if Aspire is unavailable.

## In-App Diagnostics Panel

The diagnostics panel provides a real-time overlay showing performance metrics.

### Opening the Panel

**Keyboard shortcut**: `Ctrl+Shift+D`

Or programmatically:
```csharp
diagnosticsPanelViewModel.ToggleVisibility();
```

### Displayed Metrics

| Metric | Description | Color Coding |
|--------|-------------|--------------|
| **FPS** | Current frames per second | Green: ≥30, Yellow: 15-30, Red: <15 |
| **Memory** | Total memory (Managed + Native) | Green: <500MB, Yellow: 500-1000MB, Red: >1000MB |
| **Last Render** | Most recent page render time (ms) | N/A |
| **Page** | Current page number | N/A |

### Performance Levels

Metrics are automatically categorized into performance levels:

```csharp
public enum PerformanceLevel
{
    Good,      // FPS ≥ 30 AND Memory < 500MB
    Warning,   // FPS 15-30 OR Memory 500-1000MB
    Critical   // FPS < 15 OR Memory > 1000MB
}
```

### Metrics Update Frequency

- **Refresh interval**: 500ms
- **Overhead**: <2% CPU impact
- **Metrics history**: Last 1000 samples retained (circular buffer)

### Exporting Metrics

Click **"Export Metrics"** in the diagnostics panel to save metrics to a file.

**Supported formats**:
- **JSON**: Structured metrics with full metadata
- **CSV**: Tabular format for spreadsheet analysis

### Persistence

The diagnostics panel visibility state persists across app sessions:
- Enabled in session 1 → Automatically enabled in session 2
- State stored in `ApplicationData.LocalSettings`

## Structured Log Viewer

The log viewer provides in-app access to Serilog JSON logs with advanced filtering.

### Opening the Log Viewer

**Keyboard shortcut**: `Ctrl+Shift+L`

Or programmatically:
```csharp
logViewerViewModel.LoadLogsAsync();
```

### Filter Options

#### 1. Severity Level Filter
Filter logs by minimum severity:
- **Trace**: Shows all logs
- **Debug**: Shows Debug, Info, Warning, Error, Critical
- **Information**: Shows Info, Warning, Error, Critical
- **Warning**: Shows Warning, Error, Critical
- **Error**: Shows Error, Critical
- **Critical**: Shows only Critical logs

#### 2. Correlation ID Filter
Exact match filtering for tracing operations:
```
Correlation ID: 3f7b8c9d-e21a-4f5d-a6c8-1b2e3d4a5f6g
```

**Use case**: Track a single rendering operation across all components.

#### 3. Component Filter
Prefix match for filtering by namespace:
```
Component: FluentPDF.Rendering
```

Matches:
- `FluentPDF.Rendering.Services.PdfRenderingService`
- `FluentPDF.Rendering.Services.MetricsCollectionService`

#### 4. Time Range Filter
Filter logs between start and end times:
```
Start Time: 2026-01-11 09:00:00
End Time:   2026-01-11 17:00:00
```

#### 5. Search Text Filter
Case-insensitive search in log messages:
```
Search: "OutOfMemoryException"
```

**Debounced**: 500ms delay after last keystroke to avoid excessive filtering.

### Log Entry Details

Click on any log entry to view full details:

```json
{
  "Timestamp": "2026-01-11T14:32:15.1234567Z",
  "Level": "Warning",
  "Message": "Render time exceeded threshold",
  "CorrelationId": "3f7b8c9d-e21a-4f5d-a6c8-1b2e3d4a5f6g",
  "Component": "FluentPDF.Rendering.Services.PdfRenderingService",
  "Context": {
    "PageNumber": 42,
    "ZoomLevel": 1.5,
    "RenderTimeMs": 1250
  },
  "Exception": null,
  "StackTrace": null
}
```

### Exporting Logs

Click **"Export"** to save filtered logs to JSON:
- Preserves Serilog JSON format
- Includes all filtered log entries
- Compatible with log analysis tools

### Performance Optimization

- **LRU Cache**: Last 10,000 parsed log entries cached in memory
- **Streaming**: Large log files read in chunks (not fully loaded into memory)
- **Virtualization**: ListView only renders visible items
- **Background Filtering**: Filters applied on background thread

## Metrics Export

### JSON Format

Metrics are exported as a JSON array with full metadata:

```json
[
  {
    "CurrentFPS": 60.0,
    "ManagedMemoryMB": 128,
    "NativeMemoryMB": 256,
    "TotalMemoryMB": 384,
    "LastRenderTimeMs": 16.7,
    "CurrentPageNumber": 42,
    "Timestamp": "2026-01-11T14:32:15.1234567Z",
    "Level": "Good"
  },
  {
    "CurrentFPS": 58.5,
    "ManagedMemoryMB": 130,
    "NativeMemoryMB": 258,
    "TotalMemoryMB": 388,
    "LastRenderTimeMs": 17.1,
    "CurrentPageNumber": 43,
    "Timestamp": "2026-01-11T14:32:15.6234567Z",
    "Level": "Good"
  }
]
```

### CSV Format

Metrics are exported as comma-separated values:

```csv
Timestamp,CurrentFPS,ManagedMemoryMB,NativeMemoryMB,TotalMemoryMB,LastRenderTimeMs,CurrentPageNumber,Level
2026-01-11T14:32:15.1234567Z,60.0,128,256,384,16.7,42,Good
2026-01-11T14:32:15.6234567Z,58.5,130,258,388,17.1,43,Good
```

**Use cases**:
- Import into Excel for charting
- Analyze with pandas/R
- Load into Power BI for dashboards

## Correlation ID Tracing

Correlation IDs enable end-to-end tracing of operations across components.

### How It Works

1. **Generation**: Each rendering operation generates a unique correlation ID (GUID)
2. **Propagation**: Correlation ID flows through all components:
   - PdfRenderingService → MetricsCollectionService
   - PdfRenderingService → Serilog logs
   - OpenTelemetry spans
3. **Filtering**: Use correlation ID to view all logs/traces for a single operation

### Example Workflow

1. **Render a page** → Correlation ID generated: `3f7b8c9d-e21a-4f5d-a6c8-1b2e3d4a5f6g`

2. **View logs in Aspire Dashboard**:
   - Filter by correlation ID
   - See all logs from rendering, caching, memory management

3. **View traces in Aspire Dashboard**:
   - See complete span tree:
     ```
     RenderPage (correlation.id: 3f7b8c9d...)
     ├── LoadPage
     ├── RenderBitmap
     └── ConvertToImage
     ```

4. **View in-app logs**:
   - Open log viewer (Ctrl+Shift+L)
   - Filter by correlation ID: `3f7b8c9d-e21a-4f5d-a6c8-1b2e3d4a5f6g`
   - See same logs with full context

### Copy Correlation ID

In the log viewer, click on any log entry and use the **"Copy Correlation ID"** button to copy to clipboard for easy sharing.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    FluentPDF.App (WinUI 3)                  │
│                                                             │
│  ┌─────────────────────┐     ┌──────────────────────────┐ │
│  │ DiagnosticsPanel    │     │ LogViewerControl         │ │
│  │ Control             │     │                          │ │
│  │                     │     │ - Severity filter        │ │
│  │ - FPS display       │     │ - Correlation ID filter  │ │
│  │ - Memory display    │     │ - Component filter       │ │
│  │ - Render time       │     │ - Time range filter      │ │
│  │ - Export button     │     │ - Search text            │ │
│  └──────────┬──────────┘     └───────────┬──────────────┘ │
│             │                            │                │
│             ▼                            ▼                │
│  ┌─────────────────────┐     ┌──────────────────────────┐ │
│  │ DiagnosticsPanel    │     │ LogViewerViewModel       │ │
│  │ ViewModel           │     │                          │ │
│  └──────────┬──────────┘     └───────────┬──────────────┘ │
│             │                            │                │
└─────────────┼────────────────────────────┼────────────────┘
              │                            │
              ▼                            ▼
┌─────────────────────────────────────────────────────────────┐
│              FluentPDF.Core (Interfaces)                    │
│                                                             │
│  IMetricsCollectionService      ILogExportService          │
│  - RecordFPS()                  - GetRecentLogsAsync()     │
│  - RecordRenderTime()           - FilterLogsAsync()        │
│  - RecordMemoryUsage()          - ExportLogsAsync()        │
│  - GetCurrentMetrics()                                     │
│  - ExportMetricsAsync()                                    │
└─────────────┬───────────────────────────┬─────────────────┘
              │                           │
              ▼                           ▼
┌─────────────────────────────────────────────────────────────┐
│           FluentPDF.Rendering (Implementation)              │
│                                                             │
│  MetricsCollectionService       LogExportService           │
│  - OpenTelemetry Meter          - Serilog JSON reader      │
│  - Gauges (FPS, Memory)         - Log filtering            │
│  - Histogram (Render time)      - LRU cache                │
│  - Circular buffer (1000)       - Export to JSON           │
└─────────────┬───────────────────────────┬─────────────────┘
              │                           │
              ▼                           ▼
┌─────────────────────────────────────────────────────────────┐
│              Observability Infrastructure                   │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐ │
│  │ OpenTelemetry│  │   Serilog    │  │ .NET Aspire      │ │
│  │              │  │              │  │ Dashboard        │ │
│  │ - Metrics    │  │ - JSON sink  │  │                  │ │
│  │ - Traces     │  │ - OTLP sink  │  │ - Metrics view   │ │
│  │ - OTLP       │  │ - File sink  │  │ - Logs view      │ │
│  │   exporter   │  │              │  │ - Traces view    │ │
│  └──────────────┘  └──────────────┘  └──────────────────┘ │
│         │                  │                   ▲           │
│         └──────────────────┴───────────────────┘           │
│                         OTLP (gRPC)                        │
│                      localhost:4317                        │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Metrics Collection**:
   - `PdfRenderingService` calls `IMetricsCollectionService.RecordRenderTime()`
   - Metrics stored in circular buffer (last 1000 samples)
   - OpenTelemetry instruments updated (gauges, histograms)
   - OTLP exporter sends to Aspire Dashboard (if running)

2. **Log Writing**:
   - Components log using `ILogger<T>`
   - Serilog writes to JSON file (`ApplicationData.LocalFolder/logs/log-YYYYMMDD.json`)
   - Serilog OTLP sink sends to Aspire Dashboard (if running)

3. **Distributed Tracing**:
   - `PdfRenderingService` creates `ActivitySource` spans
   - OpenTelemetry `TracerProvider` captures spans
   - OTLP exporter sends traces to Aspire Dashboard (if running)

4. **In-App Viewing**:
   - `DiagnosticsPanelViewModel` polls `IMetricsCollectionService.GetCurrentMetrics()` every 500ms
   - `LogViewerViewModel` reads JSON logs via `ILogExportService.GetRecentLogsAsync()`
   - Both update observable properties → UI binding updates

## Performance Considerations

### Metrics Collection Overhead

- **CPU overhead**: <2% (measured with 60 FPS rendering)
- **Memory overhead**: ~10MB (circular buffer + OpenTelemetry state)
- **Sampling strategy**: Collect metrics only when diagnostics enabled (configurable)

### Log File Growth

- **Rolling files**: New file per day (`log-YYYYMMDD.json`)
- **Max file size**: 100MB (Serilog RollingFile configuration)
- **Retention**: 30 days (configurable)

**Disk usage estimate**:
- Light usage: ~50MB/month
- Heavy usage: ~500MB/month

### OpenTelemetry Export Batching

Metrics and traces are batched before export to Aspire:
- **Batch size**: 512 metrics/traces
- **Export interval**: 5 seconds
- **Timeout**: 10 seconds

This reduces network overhead and OTLP endpoint load.

## Troubleshooting

### Aspire Dashboard Not Showing Data

**Symptoms**: Dashboard UI loads but shows no metrics/logs/traces.

**Diagnosis**:
1. Check FluentPDF is running
2. Verify OTLP endpoint configuration:
   ```csharp
   // App.xaml.cs
   options.Endpoint = new Uri("http://localhost:4317");
   ```
3. Check Docker container logs:
   ```bash
   docker logs aspire-dashboard
   ```

**Solution**: Restart both FluentPDF and Aspire Dashboard.

### Diagnostics Panel Not Visible

**Symptoms**: Pressing `Ctrl+Shift+D` does nothing.

**Diagnosis**:
1. Check keyboard shortcut registered in `PdfViewerPage.xaml`:
   ```xml
   <KeyboardAccelerator Key="D" Modifiers="Control,Shift"
                        Invoked="ToggleDiagnostics_Invoked"/>
   ```
2. Check ViewModel binding:
   ```xml
   <controls:DiagnosticsPanelControl
       DataContext="{x:Bind ViewModel.DiagnosticsPanelViewModel}"
       IsVisible="{x:Bind ViewModel.DiagnosticsPanelViewModel.IsVisible, Mode=TwoWay}"/>
   ```

**Solution**: Verify DI registration in `App.xaml.cs`:
```csharp
services.AddTransient<DiagnosticsPanelViewModel>();
```

### Log Viewer Shows No Logs

**Symptoms**: Log viewer opens but displays "No logs available".

**Diagnosis**:
1. Check log files exist:
   ```bash
   ls %LOCALAPPDATA%/FluentPDF/logs/
   ```
2. Check file permissions (read access required)
3. Check Serilog configuration in `App.xaml.cs`:
   ```csharp
   .WriteTo.File(
       path: Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs", "log-.json"),
       rollingInterval: RollingInterval.Day,
       formatter: new JsonFormatter())
   ```

**Solution**:
- Ensure Serilog is writing to correct directory
- Check antivirus not blocking file access
- Manually create `logs` directory if missing

### High CPU Usage from Metrics Collection

**Symptoms**: FluentPDF uses >10% CPU when diagnostics enabled.

**Diagnosis**:
1. Check metrics refresh interval:
   ```csharp
   // DiagnosticsPanelViewModel
   _timer.Interval = TimeSpan.FromMilliseconds(500); // Should be 500ms
   ```
2. Profile with dotnet-trace:
   ```bash
   dotnet-trace collect --process-id <PID> --providers Microsoft-Diagnostics-DiagnosticSource
   ```

**Solution**:
- Increase refresh interval to 1000ms
- Disable diagnostics panel when not needed
- Check for leaked timers (not disposed)

### Metrics Export Fails

**Symptoms**: "Export Metrics" button does nothing or shows error.

**Diagnosis**:
1. Check file picker dialog appears
2. Check export path is writable
3. Check `IMetricsCollectionService.ExportMetricsAsync()` returns success:
   ```csharp
   var result = await _metricsService.ExportMetricsAsync(path, ExportFormat.Json);
   if (result.IsFailed)
   {
       _logger.LogError("Export failed: {Error}", result.Errors.First().Message);
   }
   ```

**Solution**:
- Verify disk space available
- Check file path permissions
- Try exporting to temp directory first

### Log Filtering Performance

**Symptoms**: Log viewer freezes when applying filters.

**Diagnosis**:
1. Check number of log entries:
   ```csharp
   var logs = await _logExportService.GetRecentLogsAsync(10000); // Too many?
   ```
2. Profile filtering logic:
   ```csharp
   // LogFilterCriteria.Matches() should be O(1)
   ```

**Solution**:
- Reduce recent logs count (1000 instead of 10000)
- Ensure filtering runs on background thread
- Enable LRU cache for parsed logs

## Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Documentation](https://serilog.net/)
- [.NET Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OTLP Specification](https://opentelemetry.io/docs/specs/otlp/)

## Future Enhancements

Planned improvements to observability:

1. **Real-time Aspire Dashboard Embedding**: Embed Aspire UI in app via WebView2
2. **CPU Profiling Integration**: dotnet-trace integration for performance analysis
3. **Memory Snapshots**: Heap snapshot capture for memory leak detection
4. **Custom Metrics**: User-defined metrics via settings
5. **Configurable Alerting**: Threshold-based notifications
6. **Historical Trends**: Store metrics over time, show 7-day trend graphs
7. **Remote Logging**: Optional secure logging to remote endpoint (enterprise)
8. **AI Quality Agent Integration**: Webhook for automated log/metrics analysis
