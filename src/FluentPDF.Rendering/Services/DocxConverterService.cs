using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for complete DOCX to PDF conversion operations.
/// Orchestrates the conversion pipeline: validation → parsing → rendering → validation → cleanup.
/// Implements comprehensive error handling, timeout management, and resource cleanup.
/// </summary>
public sealed class DocxConverterService : IDocxConverterService
{
    private readonly ILogger<DocxConverterService> _logger;
    private readonly IDocxParserService _docxParser;
    private readonly IHtmlToPdfService _htmlToPdf;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocxConverterService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    /// <param name="docxParser">Service for parsing DOCX files to HTML.</param>
    /// <param name="htmlToPdf">Service for converting HTML to PDF.</param>
    public DocxConverterService(
        ILogger<DocxConverterService> logger,
        IDocxParserService docxParser,
        IHtmlToPdfService htmlToPdf)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _docxParser = docxParser ?? throw new ArgumentNullException(nameof(docxParser));
        _htmlToPdf = htmlToPdf ?? throw new ArgumentNullException(nameof(htmlToPdf));
    }

    /// <inheritdoc />
    public async Task<Result<ConversionResult>> ConvertDocxToPdfAsync(
        string inputPath,
        string outputPath,
        ConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        options ??= new ConversionOptions();

        _logger.LogInformation(
            "Starting DOCX to PDF conversion. CorrelationId={CorrelationId}, InputPath={InputPath}, OutputPath={OutputPath}, Timeout={Timeout}",
            correlationId, inputPath, outputPath, options.Timeout);

        // Validate input file exists
        if (!File.Exists(inputPath))
        {
            var error = new PdfError(
                "DOCX_FILE_NOT_FOUND",
                $"Source DOCX file not found: {inputPath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Source DOCX file not found. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);

            return Result.Fail(error);
        }

        // Validate input file is DOCX
        var extension = Path.GetExtension(inputPath).ToLowerInvariant();
        if (extension != ".docx")
        {
            var error = new PdfError(
                "DOCX_INVALID_FORMAT",
                $"Input file is not a DOCX document. Extension: {extension}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("Extension", extension)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Invalid input file format. CorrelationId={CorrelationId}, InputPath={InputPath}, Extension={Extension}",
                correlationId, inputPath, extension);

            return Result.Fail(error);
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var error = new PdfError(
                "OUTPUT_PATH_INVALID",
                "Output path cannot be null or empty",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(
                "Output path is invalid. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }

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

        // Get source file size
        long sourceSizeBytes;
        try
        {
            sourceSizeBytes = new FileInfo(inputPath).Length;
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "DOCX_READ_FAILED",
                $"Failed to read source file information: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Failed to read source file information. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);

            return Result.Fail(error);
        }

        try
        {
            // Create timeout cancellation token source
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.Timeout);

            // Step 1: Parse DOCX to HTML
            _logger.LogDebug(
                "Step 1: Parsing DOCX to HTML. CorrelationId={CorrelationId}",
                correlationId);

            var parseResult = await _docxParser.ParseDocxToHtmlAsync(inputPath);
            if (parseResult.IsFailed)
            {
                _logger.LogError(
                    "DOCX parsing failed. CorrelationId={CorrelationId}, Errors={Errors}",
                    correlationId, parseResult.Errors);

                return Result.Fail(parseResult.Errors);
            }

            var htmlContent = parseResult.Value;
            _logger.LogDebug(
                "DOCX parsed successfully. CorrelationId={CorrelationId}, HtmlLength={HtmlLength}",
                correlationId, htmlContent.Length);

            // Step 2: Convert HTML to PDF
            _logger.LogDebug(
                "Step 2: Converting HTML to PDF. CorrelationId={CorrelationId}",
                correlationId);

            var renderResult = await _htmlToPdf.ConvertHtmlToPdfAsync(
                htmlContent,
                outputPath,
                timeoutCts.Token);

            if (renderResult.IsFailed)
            {
                _logger.LogError(
                    "HTML to PDF conversion failed. CorrelationId={CorrelationId}, Errors={Errors}",
                    correlationId, renderResult.Errors);

                return Result.Fail(renderResult.Errors);
            }

            // Get output file size
            long outputSizeBytes;
            try
            {
                outputSizeBytes = new FileInfo(outputPath).Length;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to read output file size. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                    correlationId, outputPath);
                outputSizeBytes = 0;
            }

            stopwatch.Stop();

            // Build conversion result
            var result = new ConversionResult
            {
                OutputPath = outputPath,
                SourcePath = inputPath,
                ConversionTime = stopwatch.Elapsed,
                OutputSizeBytes = outputSizeBytes,
                SourceSizeBytes = sourceSizeBytes,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "DOCX to PDF conversion completed successfully. CorrelationId={CorrelationId}, " +
                "InputPath={InputPath}, OutputPath={OutputPath}, ConversionTime={ConversionTime}, " +
                "SourceSize={SourceSize}, OutputSize={OutputSize}",
                correlationId, inputPath, outputPath, result.ConversionTime,
                sourceSizeBytes, outputSizeBytes);

            return Result.Ok(result);
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();

            var errorCode = cancellationToken.IsCancellationRequested
                ? "CONVERSION_CANCELLED"
                : "CONVERSION_TIMEOUT";

            var errorMessage = cancellationToken.IsCancellationRequested
                ? "Conversion was cancelled by user"
                : $"Conversion timed out after {options.Timeout.TotalSeconds} seconds";

            var error = new PdfError(
                errorCode,
                errorMessage,
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("OutputPath", outputPath)
                .WithContext("Timeout", options.Timeout)
                .WithContext("ElapsedTime", stopwatch.Elapsed)
                .WithContext("CorrelationId", correlationId);

            _logger.LogError(ex,
                "Conversion cancelled or timed out. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, ElapsedTime={ElapsedTime}",
                correlationId, errorCode, stopwatch.Elapsed);

            return Result.Fail(error);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var error = new PdfError(
                "CONVERSION_FAILED",
                $"Unexpected error during conversion: {ex.Message}",
                ErrorCategory.Conversion,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("OutputPath", outputPath)
                .WithContext("ElapsedTime", stopwatch.Elapsed)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Unexpected error during conversion. CorrelationId={CorrelationId}, ElapsedTime={ElapsedTime}",
                correlationId, stopwatch.Elapsed);

            return Result.Fail(error);
        }
    }
}
