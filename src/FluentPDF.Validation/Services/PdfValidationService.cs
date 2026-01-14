using System.Diagnostics;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Wrappers;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Validation.Services;

/// <summary>
/// Service for orchestrating PDF validation across multiple tools.
/// </summary>
public sealed class PdfValidationService : IPdfValidationService
{
    private readonly IQpdfWrapper _qpdfWrapper;
    private readonly IJhoveWrapper _jhoveWrapper;
    private readonly IVeraPdfWrapper _veraPdfWrapper;
    private readonly ILogger<PdfValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfValidationService"/> class.
    /// </summary>
    /// <param name="qpdfWrapper">QPDF wrapper for structural validation.</param>
    /// <param name="jhoveWrapper">JHOVE wrapper for format validation.</param>
    /// <param name="veraPdfWrapper">VeraPDF wrapper for PDF/A compliance validation.</param>
    /// <param name="logger">Logger instance.</param>
    public PdfValidationService(
        IQpdfWrapper qpdfWrapper,
        IJhoveWrapper jhoveWrapper,
        IVeraPdfWrapper veraPdfWrapper,
        ILogger<PdfValidationService> logger)
    {
        _qpdfWrapper = qpdfWrapper ?? throw new ArgumentNullException(nameof(qpdfWrapper));
        _jhoveWrapper = jhoveWrapper ?? throw new ArgumentNullException(nameof(jhoveWrapper));
        _veraPdfWrapper = veraPdfWrapper ?? throw new ArgumentNullException(nameof(veraPdfWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<ValidationReport>> ValidateAsync(
        string filePath,
        ValidationProfile profile,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        correlationId ??= Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Starting PDF validation. File: {FilePath}, Profile: {Profile}, CorrelationId: {CorrelationId}",
            filePath, profile, correlationId);

        // Validate file exists
        if (!File.Exists(filePath))
        {
            _logger.LogError(
                "File not found: {FilePath}, CorrelationId: {CorrelationId}",
                filePath, correlationId);
            return Result.Fail<ValidationReport>($"File not found: {filePath}");
        }

        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute validation tools based on profile
            var (qpdfResult, jhoveResult, veraPdfResult) = await ExecuteValidationToolsAsync(
                filePath, profile, correlationId, cancellationToken);

            stopwatch.Stop();

            // Determine overall status
            var overallStatus = DetermineOverallStatus(qpdfResult, jhoveResult, veraPdfResult);

            var report = new ValidationReport
            {
                OverallStatus = overallStatus,
                FilePath = filePath,
                ValidationDate = startTime,
                Profile = profile,
                QpdfResult = qpdfResult,
                JhoveResult = jhoveResult,
                VeraPdfResult = veraPdfResult,
                Duration = stopwatch.Elapsed
            };

            _logger.LogInformation(
                "Validation completed. Status: {Status}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                overallStatus, stopwatch.ElapsedMilliseconds, correlationId);

            return Result.Ok(report);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Validation cancelled. File: {FilePath}, CorrelationId: {CorrelationId}",
                filePath, correlationId);
            return Result.Fail<ValidationReport>("Validation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Validation failed with exception. File: {FilePath}, CorrelationId: {CorrelationId}",
                filePath, correlationId);
            return Result.Fail<ValidationReport>($"Validation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> VerifyToolsInstalledAsync(ValidationProfile profile)
    {
        _logger.LogInformation("Verifying tools installed for profile: {Profile}", profile);

        var errors = new List<string>();

        // Always need QPDF for all profiles
        var qpdfResult = await VerifyToolAsync(_qpdfWrapper, "QPDF");
        if (qpdfResult.IsFailed)
        {
            errors.Add(qpdfResult.Errors[0].Message);
        }

        // Standard and Full profiles need JHOVE
        if (profile is ValidationProfile.Standard or ValidationProfile.Full)
        {
            var jhoveResult = await VerifyToolAsync(_jhoveWrapper, "JHOVE");
            if (jhoveResult.IsFailed)
            {
                errors.Add(jhoveResult.Errors[0].Message);
            }
        }

        // Full profile needs VeraPDF
        if (profile is ValidationProfile.Full)
        {
            var veraPdfResult = await VerifyToolAsync(_veraPdfWrapper, "VeraPDF");
            if (veraPdfResult.IsFailed)
            {
                errors.Add(veraPdfResult.Errors[0].Message);
            }
        }

        if (errors.Count > 0)
        {
            var errorMessage = $"Required tools not installed: {string.Join(", ", errors)}";
            _logger.LogError(errorMessage);
            return Result.Fail(errorMessage);
        }

        _logger.LogInformation("All required tools are installed for profile: {Profile}", profile);
        return Result.Ok();
    }

    private async Task<(QpdfResult?, JhoveResult?, VeraPdfResult?)> ExecuteValidationToolsAsync(
        string filePath,
        ValidationProfile profile,
        string correlationId,
        CancellationToken cancellationToken)
    {
        QpdfResult? qpdfResult = null;
        JhoveResult? jhoveResult = null;
        VeraPdfResult? veraPdfResult = null;

        var tasks = new List<Task>();

        // Quick profile: QPDF only
        var qpdfTask = Task.Run(async () =>
        {
            var result = await _qpdfWrapper.ValidateAsync(filePath, correlationId, cancellationToken);
            qpdfResult = result.IsSuccess ? result.Value : null;
            if (result.IsFailed)
            {
                _logger.LogWarning(
                    "QPDF validation failed: {Error}, CorrelationId: {CorrelationId}",
                    result.Errors[0].Message, correlationId);
            }
        }, cancellationToken);
        tasks.Add(qpdfTask);

        // Standard and Full profiles: add JHOVE
        if (profile is ValidationProfile.Standard or ValidationProfile.Full)
        {
            var jhoveTask = Task.Run(async () =>
            {
                var result = await _jhoveWrapper.ValidateAsync(filePath, correlationId, cancellationToken);
                jhoveResult = result.IsSuccess ? result.Value : null;
                if (result.IsFailed)
                {
                    _logger.LogWarning(
                        "JHOVE validation failed: {Error}, CorrelationId: {CorrelationId}",
                        result.Errors[0].Message, correlationId);
                }
            }, cancellationToken);
            tasks.Add(jhoveTask);
        }

        // Full profile: add VeraPDF
        if (profile is ValidationProfile.Full)
        {
            var veraPdfTask = Task.Run(async () =>
            {
                var result = await _veraPdfWrapper.ValidateAsync(filePath, correlationId, cancellationToken);
                veraPdfResult = result.IsSuccess ? result.Value : null;
                if (result.IsFailed)
                {
                    _logger.LogWarning(
                        "VeraPDF validation failed: {Error}, CorrelationId: {CorrelationId}",
                        result.Errors[0].Message, correlationId);
                }
            }, cancellationToken);
            tasks.Add(veraPdfTask);
        }

        // Execute all tasks in parallel
        await Task.WhenAll(tasks);

        return (qpdfResult, jhoveResult, veraPdfResult);
    }

    private ValidationStatus DetermineOverallStatus(
        QpdfResult? qpdfResult,
        JhoveResult? jhoveResult,
        VeraPdfResult? veraPdfResult)
    {
        var statuses = new List<ValidationStatus>();

        if (qpdfResult != null)
            statuses.Add(qpdfResult.Status);

        if (jhoveResult != null)
            statuses.Add(jhoveResult.Status);

        if (veraPdfResult != null)
            statuses.Add(veraPdfResult.Status);

        // If any tool failed, overall status is Fail
        if (statuses.Contains(ValidationStatus.Fail))
            return ValidationStatus.Fail;

        // If any tool warned, overall status is Warn
        if (statuses.Contains(ValidationStatus.Warn))
            return ValidationStatus.Warn;

        // All tools passed
        return ValidationStatus.Pass;
    }

    private async Task<Result> VerifyToolAsync<T>(T wrapper, string toolName)
    {
        try
        {
            // Create a temporary empty file to test the tool
            var tempFile = Path.GetTempFileName();
            try
            {
                // Execute the appropriate wrapper based on type
                switch (wrapper)
                {
                    case IQpdfWrapper qpdf:
                        await qpdf.ValidateAsync(tempFile);
                        break;
                    case IJhoveWrapper jhove:
                        await jhove.ValidateAsync(tempFile);
                        break;
                    case IVeraPdfWrapper veraPdf:
                        await veraPdf.ValidateAsync(tempFile);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown wrapper type: {typeof(T)}");
                }

                // We expect the tool to execute (even if validation fails on empty file)
                // If the tool is not installed, we'll get an exception
                return Result.Ok();
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"{toolName} not available: {ex.Message}");
        }
    }
}
