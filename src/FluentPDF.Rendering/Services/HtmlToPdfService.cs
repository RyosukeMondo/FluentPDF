using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for HTML to PDF conversion using WebView2 (Chromium) rendering engine.
/// Implements asynchronous operations with comprehensive error handling, structured logging,
/// and queuing mechanism for concurrent conversions.
/// </summary>
public sealed class HtmlToPdfService : IHtmlToPdfService, IDisposable
{
    private readonly ILogger<HtmlToPdfService> _logger;
    private readonly SemaphoreSlim _conversionQueue;
    private CoreWebView2Environment? _environment;
    private bool _isInitialized;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const int MaxConcurrentConversions = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlToPdfService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public HtmlToPdfService(ILogger<HtmlToPdfService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conversionQueue = new SemaphoreSlim(MaxConcurrentConversions, MaxConcurrentConversions);
    }

    /// <inheritdoc />
    public async Task<Result<string>> ConvertHtmlToPdfAsync(
        string htmlContent,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();

        // Validate inputs first
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            var error = new PdfError(
                "HTML_EMPTY",
                "HTML content cannot be null or empty",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "HTML content is empty. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var error = new PdfError(
                "OUTPUT_PATH_EMPTY",
                "Output path cannot be null or empty",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Output path is empty. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }

        _logger.LogInformation(
            "Starting HTML to PDF conversion. CorrelationId={CorrelationId}, OutputPath={OutputPath}, HtmlLength={HtmlLength}",
            correlationId, outputPath, htmlContent.Length);

        // Ensure output directory exists
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogDebug(
                    "Created output directory. CorrelationId={CorrelationId}, Directory={Directory}",
                    correlationId, outputDirectory);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "OUTPUT_DIRECTORY_CREATE_FAILED",
                    $"Failed to create output directory: {ex.Message}",
                    ErrorCategory.IO,
                    ErrorSeverity.Error)
                    .WithContext("Directory", outputDirectory)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogError(ex,
                    "Failed to create output directory. CorrelationId={CorrelationId}, Directory={Directory}",
                    correlationId, outputDirectory);

                return Result.Fail(error);
            }
        }

        // Initialize WebView2 environment if needed
        var initResult = await EnsureInitializedAsync(correlationId, cancellationToken);
        if (initResult.IsFailed)
        {
            return Result.Fail(initResult.Errors);
        }

        // Queue conversion to prevent resource exhaustion
        await _conversionQueue.WaitAsync(cancellationToken);

        try
        {
            return await PerformConversionAsync(htmlContent, outputPath, correlationId, cancellationToken);
        }
        finally
        {
            _conversionQueue.Release();
        }
    }

    private async Task<Result> EnsureInitializedAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return Result.Ok();
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return Result.Ok();
            }

            _logger.LogInformation(
                "Initializing WebView2 environment. CorrelationId={CorrelationId}",
                correlationId);

            try
            {
                var options = new CoreWebView2EnvironmentOptions();
                _environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: Path.Combine(Path.GetTempPath(), "FluentPDF_WebView2"),
                    options: options);

                _isInitialized = true;

                _logger.LogInformation(
                    "WebView2 environment initialized. CorrelationId={CorrelationId}, BrowserVersion={BrowserVersion}",
                    correlationId, _environment.BrowserVersionString);

                return Result.Ok();
            }
            catch (WebView2RuntimeNotFoundException ex)
            {
                var error = new PdfError(
                    "WEBVIEW2_RUNTIME_NOT_FOUND",
                    "WebView2 runtime is not installed. Please install the WebView2 runtime from https://developer.microsoft.com/microsoft-edge/webview2/",
                    ErrorCategory.System,
                    ErrorSeverity.Critical)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogCritical(ex,
                    "WebView2 runtime not found. CorrelationId={CorrelationId}",
                    correlationId);

                return Result.Fail(error);
            }
            catch (Exception ex)
            {
                var error = new PdfError(
                    "WEBVIEW2_INIT_FAILED",
                    $"Failed to initialize WebView2 environment: {ex.Message}",
                    ErrorCategory.System,
                    ErrorSeverity.Critical)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name);

                _logger.LogCritical(ex,
                    "Failed to initialize WebView2 environment. CorrelationId={CorrelationId}",
                    correlationId);

                return Result.Fail(error);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<Result<string>> PerformConversionAsync(
        string htmlContent,
        string outputPath,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        CoreWebView2? webView = null;
        var startTime = DateTime.UtcNow;

        try
        {
            // Create WebView2 controller (headless)
            _logger.LogDebug(
                "Creating WebView2 controller. CorrelationId={CorrelationId}",
                correlationId);

            var controller = await _environment!.CreateCoreWebView2ControllerAsync(IntPtr.Zero);
            webView = controller.CoreWebView2;

            // Configure print settings for optimal PDF output
            var printSettings = webView.Environment.CreatePrintSettings();
            printSettings.ShouldPrintBackgrounds = true;
            printSettings.ShouldPrintSelectionOnly = false;
            printSettings.ShouldPrintHeaderAndFooter = false;
            printSettings.MarginTop = 0.4;
            printSettings.MarginBottom = 0.4;
            printSettings.MarginLeft = 0.4;
            printSettings.MarginRight = 0.4;
            printSettings.ScaleFactor = 1.0;
            printSettings.PageWidth = 8.27; // A4 width in inches
            printSettings.PageHeight = 11.69; // A4 height in inches

            _logger.LogDebug(
                "Loading HTML content. CorrelationId={CorrelationId}, ContentLength={ContentLength}",
                correlationId, htmlContent.Length);

            // Load HTML content
            var navigationCompleted = new TaskCompletionSource<bool>();
            void NavigationCompletedHandler(object? sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                if (e.IsSuccess)
                {
                    navigationCompleted.TrySetResult(true);
                }
                else
                {
                    navigationCompleted.TrySetException(
                        new InvalidOperationException($"Navigation failed with status: {e.WebErrorStatus}"));
                }
            }

            webView.NavigationCompleted += NavigationCompletedHandler;

            try
            {
                webView.NavigateToString(htmlContent);

                // Wait for navigation with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                await navigationCompleted.Task.WaitAsync(timeoutCts.Token);
            }
            finally
            {
                webView.NavigationCompleted -= NavigationCompletedHandler;
            }

            _logger.LogDebug(
                "Navigation completed. CorrelationId={CorrelationId}",
                correlationId);

            // Add small delay to ensure content is fully rendered
            await Task.Delay(500, cancellationToken);

            // Print to PDF
            _logger.LogDebug(
                "Generating PDF. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            var printCompleted = await webView.PrintToPdfAsync(outputPath, printSettings);

            if (!printCompleted)
            {
                var error = new PdfError(
                    "PDF_GENERATION_FAILED",
                    "WebView2 PrintToPdfAsync returned false",
                    ErrorCategory.Conversion,
                    ErrorSeverity.Error)
                    .WithContext("OutputPath", outputPath)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError(
                    "PDF generation failed. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                return Result.Fail(error);
            }

            // Verify output file exists
            if (!File.Exists(outputPath))
            {
                var error = new PdfError(
                    "PDF_OUTPUT_NOT_FOUND",
                    $"PDF was generated but output file not found: {outputPath}",
                    ErrorCategory.IO,
                    ErrorSeverity.Error)
                    .WithContext("OutputPath", outputPath)
                    .WithContext("CorrelationId", correlationId);

                _logger.LogError(
                    "PDF output file not found. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);

                return Result.Fail(error);
            }

            var duration = DateTime.UtcNow - startTime;
            var fileSize = new FileInfo(outputPath).Length;

            _logger.LogInformation(
                "PDF generated successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}, FileSizeBytes={FileSizeBytes}, DurationMs={DurationMs}",
                correlationId, outputPath, fileSize, duration.TotalMilliseconds);

            return Result.Ok(outputPath);
        }
        catch (OperationCanceledException ex)
        {
            var error = new PdfError(
                "CONVERSION_CANCELLED",
                "HTML to PDF conversion was cancelled",
                ErrorCategory.Conversion,
                ErrorSeverity.Warning)
                .WithContext("OutputPath", outputPath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogWarning(ex,
                "Conversion cancelled. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            return Result.Fail(error);
        }
        catch (TimeoutException ex)
        {
            var error = new PdfError(
                "CONVERSION_TIMEOUT",
                "HTML to PDF conversion timed out",
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("OutputPath", outputPath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(ex,
                "Conversion timed out. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            return Result.Fail(error);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "HTML_TO_PDF_FAILED",
                $"Failed to convert HTML to PDF: {ex.Message}",
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("OutputPath", outputPath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to convert HTML to PDF. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            return Result.Fail(error);
        }
    }

    /// <summary>
    /// Disposes the service and releases WebView2 resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _conversionQueue.Dispose();
        _initLock.Dispose();
        _disposed = true;

        _logger.LogDebug("HtmlToPdfService disposed");
    }
}
