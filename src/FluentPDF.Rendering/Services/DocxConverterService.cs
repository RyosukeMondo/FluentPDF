using System.Diagnostics;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

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

        var inputValidation = ValidateInputFile(inputPath, correlationId);
        if (inputValidation.IsFailed)
            return inputValidation.ToResult<ConversionResult>();

        var outputValidation = ValidateOutputPath(outputPath, correlationId);
        if (outputValidation.IsFailed)
            return outputValidation.ToResult<ConversionResult>();

        var sizeResult = GetSourceFileSize(inputPath, correlationId);
        if (sizeResult.IsFailed)
            return sizeResult.ToResult<ConversionResult>();

        return await ExecuteConversionPipelineAsync(
            inputPath, outputPath, options,
            sizeResult.Value, stopwatch, correlationId, cancellationToken);
    }

    private Result ValidateInputFile(string inputPath, Guid correlationId)
    {
        if (!File.Exists(inputPath))
        {
            _logger.LogError(
                "Source DOCX file not found. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);

            return Result.Fail(new PdfError(
                "DOCX_FILE_NOT_FOUND",
                $"Source DOCX file not found: {inputPath}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("CorrelationId", correlationId));
        }

        var extension = Path.GetExtension(inputPath).ToLowerInvariant();
        if (extension != ".docx")
        {
            _logger.LogError(
                "Invalid input file format. CorrelationId={CorrelationId}, InputPath={InputPath}, Extension={Extension}",
                correlationId, inputPath, extension);

            return Result.Fail(new PdfError(
                "DOCX_INVALID_FORMAT",
                $"Input file is not a DOCX document. Extension: {extension}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("Extension", extension)
                .WithContext("CorrelationId", correlationId));
        }

        return Result.Ok();
    }

    private Result ValidateOutputPath(string outputPath, Guid correlationId)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            _logger.LogError(
                "Output path is invalid. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(new PdfError(
                "OUTPUT_PATH_INVALID",
                "Output path cannot be null or empty",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId));
        }

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
                _logger.LogError(ex,
                    "Failed to create output directory. CorrelationId={CorrelationId}, Directory={Directory}",
                    correlationId, outputDirectory);

                return Result.Fail(new PdfError(
                    "OUTPUT_DIRECTORY_CREATE_FAILED",
                    $"Failed to create output directory: {ex.Message}",
                    ErrorCategory.IO,
                    ErrorSeverity.Error)
                    .WithContext("Directory", outputDirectory)
                    .WithContext("CorrelationId", correlationId)
                    .WithContext("ExceptionType", ex.GetType().Name));
            }
        }

        return Result.Ok();
    }

    private Result<long> GetSourceFileSize(string inputPath, Guid correlationId)
    {
        try
        {
            return Result.Ok(new FileInfo(inputPath).Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read source file information. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);

            return Result.Fail(new PdfError(
                "DOCX_READ_FAILED",
                $"Failed to read source file information: {ex.Message}",
                ErrorCategory.IO,
                ErrorSeverity.Error)
                .WithContext("InputPath", inputPath)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name));
        }
    }

    private async Task<Result<ConversionResult>> ExecuteConversionPipelineAsync(
        string inputPath,
        string outputPath,
        ConversionOptions options,
        long sourceSizeBytes,
        Stopwatch stopwatch,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.Timeout);

            var htmlResult = await ParseDocxToHtmlAsync(inputPath, correlationId);
            if (htmlResult.IsFailed)
                return Result.Fail(htmlResult.Errors);

            var renderResult = await RenderHtmlToPdfAsync(
                htmlResult.Value, outputPath, correlationId, timeoutCts.Token);
            if (renderResult.IsFailed)
                return Result.Fail(renderResult.Errors);

            stopwatch.Stop();
            return BuildConversionResult(
                inputPath, outputPath, sourceSizeBytes, stopwatch.Elapsed, correlationId);
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            return HandleCancellation(
                ex, inputPath, outputPath, options, stopwatch.Elapsed,
                correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HandleUnexpectedError(
                ex, inputPath, outputPath, stopwatch.Elapsed, correlationId);
        }
    }

    private async Task<Result<string>> ParseDocxToHtmlAsync(
        string inputPath, Guid correlationId)
    {
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

        _logger.LogDebug(
            "DOCX parsed successfully. CorrelationId={CorrelationId}, HtmlLength={HtmlLength}",
            correlationId, parseResult.Value.Length);

        return parseResult;
    }

    private async Task<Result> RenderHtmlToPdfAsync(
        string htmlContent, string outputPath,
        Guid correlationId, CancellationToken ct)
    {
        _logger.LogDebug(
            "Step 2: Converting HTML to PDF. CorrelationId={CorrelationId}",
            correlationId);

        var renderResult = await _htmlToPdf.ConvertHtmlToPdfAsync(
            htmlContent, outputPath, ct);

        if (renderResult.IsFailed)
        {
            _logger.LogError(
                "HTML to PDF conversion failed. CorrelationId={CorrelationId}, Errors={Errors}",
                correlationId, renderResult.Errors);
            return Result.Fail(renderResult.Errors);
        }

        return Result.Ok();
    }

    private Result<ConversionResult> BuildConversionResult(
        string inputPath, string outputPath, long sourceSizeBytes,
        TimeSpan elapsed, Guid correlationId)
    {
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

        var result = new ConversionResult
        {
            OutputPath = outputPath,
            SourcePath = inputPath,
            ConversionTime = elapsed,
            OutputSizeBytes = outputSizeBytes,
            SourceSizeBytes = sourceSizeBytes,
            CompletedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "DOCX to PDF conversion completed successfully. CorrelationId={CorrelationId}, " +
            "InputPath={InputPath}, OutputPath={OutputPath}, ConversionTime={ConversionTime}, " +
            "SourceSize={SourceSize}, OutputSize={OutputSize}",
            correlationId, inputPath, outputPath, elapsed,
            sourceSizeBytes, outputSizeBytes);

        return Result.Ok(result);
    }

    private Result<ConversionResult> HandleCancellation(
        OperationCanceledException ex,
        string inputPath, string outputPath,
        ConversionOptions options, TimeSpan elapsed,
        Guid correlationId, CancellationToken cancellationToken)
    {
        var errorCode = cancellationToken.IsCancellationRequested
            ? "CONVERSION_CANCELLED"
            : "CONVERSION_TIMEOUT";

        var errorMessage = cancellationToken.IsCancellationRequested
            ? "Conversion was cancelled by user"
            : $"Conversion timed out after {options.Timeout.TotalSeconds} seconds";

        _logger.LogError(ex,
            "Conversion cancelled or timed out. CorrelationId={CorrelationId}, ErrorCode={ErrorCode}, ElapsedTime={ElapsedTime}",
            correlationId, errorCode, elapsed);

        return Result.Fail(new PdfError(
            errorCode,
            errorMessage,
            ErrorCategory.Conversion,
            ErrorSeverity.Error)
            .WithContext("InputPath", inputPath)
            .WithContext("OutputPath", outputPath)
            .WithContext("Timeout", options.Timeout)
            .WithContext("ElapsedTime", elapsed)
            .WithContext("CorrelationId", correlationId));
    }

    private Result<ConversionResult> HandleUnexpectedError(
        Exception ex,
        string inputPath, string outputPath,
        TimeSpan elapsed, Guid correlationId)
    {
        _logger.LogError(ex,
            "Unexpected error during conversion. CorrelationId={CorrelationId}, ElapsedTime={ElapsedTime}",
            correlationId, elapsed);

        return Result.Fail(new PdfError(
            "CONVERSION_FAILED",
            $"Unexpected error during conversion: {ex.Message}",
            ErrorCategory.Conversion,
            ErrorSeverity.Error)
            .WithContext("InputPath", inputPath)
            .WithContext("OutputPath", outputPath)
            .WithContext("ElapsedTime", elapsed)
            .WithContext("CorrelationId", correlationId)
            .WithContext("ExceptionType", ex.GetType().Name));
    }
}
