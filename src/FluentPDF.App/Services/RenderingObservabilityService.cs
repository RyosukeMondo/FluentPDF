using System.Diagnostics;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Services;

/// <summary>
/// Provides centralized observability for the PDF rendering pipeline.
/// Captures structured logs with timing, memory metrics, and diagnostic context.
/// </summary>
public sealed class RenderingObservabilityService
{
    private readonly ILogger<RenderingObservabilityService> _logger;
    private readonly MemoryMonitor _memoryMonitor;

    public RenderingObservabilityService(
        ILogger<RenderingObservabilityService> logger,
        MemoryMonitor memoryMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryMonitor = memoryMonitor ?? throw new ArgumentNullException(nameof(memoryMonitor));
    }

    /// <summary>
    /// Begins a rendering operation scope that automatically logs start/end with timing and memory metrics.
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "RenderPage", "RenderThumbnail").</param>
    /// <param name="context">Rendering context containing document and page information.</param>
    /// <returns>An IDisposable that will log completion when disposed.</returns>
    /// <example>
    /// <code>
    /// using (var operation = observabilityService.BeginRenderOperation("RenderPage", context))
    /// {
    ///     // ... rendering code ...
    ///     operation.SetSuccess(imageSize);
    /// }
    /// </code>
    /// </example>
    public IDisposable BeginRenderOperation(string operationName, RenderContext context)
    {
        return new RenderOperationScope(this, operationName, context);
    }

    /// <summary>
    /// Logs successful completion of a rendering operation with performance metrics.
    /// </summary>
    /// <param name="operation">Name of the operation that completed.</param>
    /// <param name="duration">Time taken to complete the operation.</param>
    /// <param name="outputSize">Size of the rendered output in bytes.</param>
    /// <param name="context">Rendering context for structured logging.</param>
    public void LogRenderSuccess(string operation, TimeSpan duration, long outputSize, RenderContext context)
    {
        _logger.LogInformation(
            "Render operation '{Operation}' succeeded in {DurationMs}ms. " +
            "Document: {DocumentPath}, Page: {PageNumber}/{TotalPages}, OutputSize: {OutputSizeKB}KB, " +
            "RequestSource: {RequestSource}, OperationId: {OperationId}",
            operation,
            duration.TotalMilliseconds,
            context.DocumentPath,
            context.PageNumber,
            context.TotalPages,
            outputSize / 1024.0,
            context.RequestSource,
            context.OperationId);
    }

    /// <summary>
    /// Logs failure of a rendering operation with exception details and diagnostics.
    /// </summary>
    /// <param name="operation">Name of the operation that failed.</param>
    /// <param name="ex">Exception that caused the failure.</param>
    /// <param name="context">Rendering context for structured logging.</param>
    /// <param name="diagnostics">Additional diagnostic information (e.g., memory state, PDFium state).</param>
    public void LogRenderFailure(string operation, Exception ex, RenderContext context, object? diagnostics = null)
    {
        var logMessage = "Render operation '{Operation}' failed. " +
                        "Document: {DocumentPath}, Page: {PageNumber}/{TotalPages}, " +
                        "RequestSource: {RequestSource}, OperationId: {OperationId}";

        if (diagnostics != null)
        {
            _logger.LogError(ex,
                logMessage + ", Diagnostics: {@Diagnostics}",
                operation,
                context.DocumentPath,
                context.PageNumber,
                context.TotalPages,
                context.RequestSource,
                context.OperationId,
                diagnostics);
        }
        else
        {
            _logger.LogError(ex,
                logMessage,
                operation,
                context.DocumentPath,
                context.PageNumber,
                context.TotalPages,
                context.RequestSource,
                context.OperationId);
        }
    }

    /// <summary>
    /// Logs failure of UI binding verification, indicating rendered content didn't appear in UI.
    /// </summary>
    /// <param name="propertyName">Name of the property that failed to update UI.</param>
    /// <param name="viewModelType">Type of the ViewModel that owns the property.</param>
    /// <param name="context">Rendering context for correlation.</param>
    /// <param name="diagnostics">Diagnostic information about the binding failure.</param>
    public void LogUIBindingFailure(string propertyName, string viewModelType, RenderContext context, string diagnostics)
    {
        _logger.LogWarning(
            "UI binding verification failed for property '{PropertyName}' in {ViewModelType}. " +
            "Rendered content may not be visible to user. " +
            "Document: {DocumentPath}, Page: {PageNumber}, OperationId: {OperationId}, " +
            "Diagnostics: {Diagnostics}",
            propertyName,
            viewModelType,
            context.DocumentPath,
            context.PageNumber,
            context.OperationId,
            diagnostics);
    }

    /// <summary>
    /// Logs abnormal memory growth detected during rendering.
    /// </summary>
    /// <param name="memoryDelta">Memory delta information.</param>
    /// <param name="context">Rendering context for correlation.</param>
    public void LogAbnormalMemoryGrowth(MemoryDelta memoryDelta, RenderContext context)
    {
        _logger.LogWarning(
            "Abnormal memory growth detected during rendering. " +
            "WorkingSet: {WorkingSetDeltaMB}MB, PrivateMemory: {PrivateMemoryDeltaMB}MB, " +
            "ManagedHeap: {ManagedMemoryDeltaMB}MB, Handles: {HandleCountDelta}. " +
            "Document: {DocumentPath}, Page: {PageNumber}, OperationId: {OperationId}",
            memoryDelta.WorkingSetDelta / (1024.0 * 1024.0),
            memoryDelta.PrivateMemoryDelta / (1024.0 * 1024.0),
            memoryDelta.ManagedMemoryDelta / (1024.0 * 1024.0),
            memoryDelta.HandleCountDelta,
            context.DocumentPath,
            context.PageNumber,
            context.OperationId);
    }

    /// <summary>
    /// Logs that a fallback rendering strategy was used after primary strategy failed.
    /// </summary>
    /// <param name="primaryStrategy">Name of the strategy that failed.</param>
    /// <param name="fallbackStrategy">Name of the strategy that succeeded.</param>
    /// <param name="context">Rendering context for correlation.</param>
    public void LogFallbackStrategyUsed(string primaryStrategy, string fallbackStrategy, RenderContext context)
    {
        _logger.LogWarning(
            "Primary rendering strategy '{PrimaryStrategy}' failed, fell back to '{FallbackStrategy}'. " +
            "Document: {DocumentPath}, Page: {PageNumber}, OperationId: {OperationId}",
            primaryStrategy,
            fallbackStrategy,
            context.DocumentPath,
            context.PageNumber,
            context.OperationId);
    }

    /// <summary>
    /// Nested class that manages a rendering operation scope with automatic timing and memory tracking.
    /// Disposed when the operation completes to log final metrics.
    /// </summary>
    private sealed class RenderOperationScope : IDisposable
    {
        private readonly RenderingObservabilityService _service;
        private readonly string _operationName;
        private readonly RenderContext _context;
        private readonly Stopwatch _stopwatch;
        private readonly MemorySnapshot _memoryBefore;
        private bool _disposed;
        private bool _success;
        private long _outputSize;
        private Exception? _exception;

        public RenderOperationScope(RenderingObservabilityService service, string operationName, RenderContext context)
        {
            _service = service;
            _operationName = operationName;
            _context = context;
            _stopwatch = Stopwatch.StartNew();
            _memoryBefore = service._memoryMonitor.CaptureSnapshot($"Before_{operationName}");

            // Log operation start
            service._logger.LogDebug(
                "Starting render operation '{Operation}'. Document: {DocumentPath}, Page: {PageNumber}/{TotalPages}, OperationId: {OperationId}",
                operationName,
                context.DocumentPath,
                context.PageNumber,
                context.TotalPages,
                context.OperationId);
        }

        /// <summary>
        /// Marks the operation as successful with output size.
        /// </summary>
        public void SetSuccess(long outputSize)
        {
            _success = true;
            _outputSize = outputSize;
        }

        /// <summary>
        /// Marks the operation as failed with exception.
        /// </summary>
        public void SetFailure(Exception ex)
        {
            _success = false;
            _exception = ex;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            var memoryAfter = _service._memoryMonitor.CaptureSnapshot($"After_{_operationName}");
            var memoryDelta = _service._memoryMonitor.CalculateDelta(_memoryBefore, memoryAfter);

            if (_success)
            {
                _service.LogRenderSuccess(_operationName, _stopwatch.Elapsed, _outputSize, _context);

                // Check for abnormal memory growth even on success
                if (memoryDelta.IsAbnormal)
                {
                    _service.LogAbnormalMemoryGrowth(memoryDelta, _context);
                }
            }
            else if (_exception != null)
            {
                _service.LogRenderFailure(_operationName, _exception, _context, new
                {
                    DurationMs = _stopwatch.ElapsedMilliseconds,
                    MemoryDelta = new
                    {
                        WorkingSetDeltaMB = memoryDelta.WorkingSetDelta / (1024.0 * 1024.0),
                        HandleCountDelta = memoryDelta.HandleCountDelta
                    }
                });
            }
        }
    }
}
