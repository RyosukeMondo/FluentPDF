using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentPDF.Validation.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Wrapper for VeraPDF PDF/A compliance validation CLI tool.
/// </summary>
public sealed class VeraPdfWrapper : IVeraPdfWrapper
{
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private const string VeraPdfExecutable = "verapdf";

    /// <summary>
    /// Initializes a new instance of the <see cref="VeraPdfWrapper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    public VeraPdfWrapper(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<VeraPdfResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Fail<VeraPdfResult>("File path cannot be empty");
        }

        if (!File.Exists(filePath))
        {
            return Result.Fail<VeraPdfResult>($"File not found: {filePath}");
        }

        var correlationIdValue = correlationId ?? Guid.NewGuid().ToString("N")[..8];

        _logger.Information(
            "Starting VeraPDF validation for {FilePath} with correlation ID {CorrelationId}",
            filePath,
            correlationIdValue);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = VeraPdfExecutable,
                Arguments = $"--format json \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stdoutBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stderrBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await WaitForExitAsync(process, _timeout, cancellationToken);

            if (!completed)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to kill timed out VeraPDF process");
                }

                _logger.Warning(
                    "VeraPDF validation timed out after {Timeout}s for {FilePath} (CorrelationId: {CorrelationId})",
                    _timeout.TotalSeconds,
                    filePath,
                    correlationIdValue);

                return Result.Fail<VeraPdfResult>($"VeraPDF validation timed out after {_timeout.TotalSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();

            _logger.Debug(
                "VeraPDF completed with exit code {ExitCode} for {FilePath} (CorrelationId: {CorrelationId})",
                exitCode,
                filePath,
                correlationIdValue);

            // Check for execution errors in stderr
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.Warning(
                    "VeraPDF reported errors in stderr for {FilePath}: {Stderr}",
                    filePath,
                    stderr);
            }

            var result = ParseVeraPdfOutput(stdout, correlationIdValue);

            _logger.Information(
                "VeraPDF validation completed with status {Status} for {FilePath} (CorrelationId: {CorrelationId})",
                result.Status,
                filePath,
                correlationIdValue);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "VeraPDF validation failed with exception for {FilePath} (CorrelationId: {CorrelationId})",
                filePath,
                correlationIdValue);

            return Result.Fail<VeraPdfResult>($"VeraPDF validation failed: {ex.Message}");
        }
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private VeraPdfResult ParseVeraPdfOutput(string jsonOutput, string correlationId)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonOutput);
            var root = document.RootElement;

            // VeraPDF JSON structure has a "jobs" array with batch validation results
            if (!root.TryGetProperty("jobs", out var jobs) || jobs.GetArrayLength() == 0)
            {
                _logger.Warning(
                    "VeraPDF JSON output missing jobs array (CorrelationId: {CorrelationId})",
                    correlationId);

                return CreateFailedResult("Invalid VeraPDF JSON output: missing jobs array");
            }

            // Get the first job (we validate one file at a time)
            var job = jobs[0];

            // Extract validation report
            if (!job.TryGetProperty("validationReport", out var validationReport))
            {
                _logger.Warning(
                    "VeraPDF JSON output missing validationReport (CorrelationId: {CorrelationId})",
                    correlationId);

                return CreateFailedResult("Invalid VeraPDF JSON output: missing validationReport");
            }

            // Extract compliance status
            var isCompliant = validationReport.TryGetProperty("compliant", out var compliantProp)
                ? compliantProp.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
                : false;

            // Extract PDF/A flavour
            var flavour = PdfFlavour.None;
            if (validationReport.TryGetProperty("profileName", out var profileName))
            {
                flavour = ParsePdfFlavour(profileName.GetString());
            }

            // Extract validation details
            var totalChecks = 0;
            var failedChecks = 0;
            var errors = new List<VeraPdfError>();

            if (validationReport.TryGetProperty("details", out var details))
            {
                totalChecks = details.TryGetProperty("passedChecks", out var passedProp)
                    ? passedProp.GetInt32()
                    : 0;

                failedChecks = details.TryGetProperty("failedChecks", out var failedProp)
                    ? failedProp.GetInt32()
                    : 0;

                totalChecks += failedChecks;

                // Extract validation errors
                if (details.TryGetProperty("validationErrors", out var validationErrors) &&
                    validationErrors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var error in validationErrors.EnumerateArray())
                    {
                        var veraPdfError = ParseVeraPdfError(error);
                        if (veraPdfError != null)
                        {
                            errors.Add(veraPdfError);
                        }
                    }
                }
            }

            var status = isCompliant
                ? ValidationStatus.Pass
                : (failedChecks > 0 ? ValidationStatus.Fail : ValidationStatus.Warn);

            return new VeraPdfResult
            {
                IsCompliant = isCompliant,
                Flavour = flavour,
                Status = status,
                Errors = errors.AsReadOnly(),
                TotalChecks = totalChecks,
                FailedChecks = failedChecks
            };
        }
        catch (JsonException ex)
        {
            _logger.Error(
                ex,
                "Failed to parse VeraPDF JSON output (CorrelationId: {CorrelationId})",
                correlationId);

            return CreateFailedResult($"Failed to parse VeraPDF JSON output: {ex.Message}");
        }
    }

    private static PdfFlavour ParsePdfFlavour(string? profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return PdfFlavour.None;
        }

        // VeraPDF profile names are like "PDF/A-1B validation profile"
        var lowerProfile = profileName.ToLowerInvariant();

        return lowerProfile switch
        {
            _ when lowerProfile.Contains("pdf/a-1a") => PdfFlavour.PdfA1a,
            _ when lowerProfile.Contains("pdf/a-1b") => PdfFlavour.PdfA1b,
            _ when lowerProfile.Contains("pdf/a-2a") => PdfFlavour.PdfA2a,
            _ when lowerProfile.Contains("pdf/a-2b") => PdfFlavour.PdfA2b,
            _ when lowerProfile.Contains("pdf/a-2u") => PdfFlavour.PdfA2u,
            _ when lowerProfile.Contains("pdf/a-3a") => PdfFlavour.PdfA3a,
            _ when lowerProfile.Contains("pdf/a-3b") => PdfFlavour.PdfA3b,
            _ when lowerProfile.Contains("pdf/a-3u") => PdfFlavour.PdfA3u,
            _ => PdfFlavour.None
        };
    }

    private static VeraPdfError? ParseVeraPdfError(JsonElement errorElement)
    {
        try
        {
            var ruleReference = errorElement.TryGetProperty("specification", out var spec)
                ? spec.GetString() ?? "Unknown"
                : "Unknown";

            var description = errorElement.TryGetProperty("message", out var msg)
                ? msg.GetString() ?? "Unknown error"
                : "Unknown error";

            int? pageNumber = null;
            if (errorElement.TryGetProperty("context", out var context) &&
                context.TryGetProperty("page", out var page))
            {
                pageNumber = page.GetInt32();
            }

            string? objectReference = null;
            if (errorElement.TryGetProperty("test", out var test))
            {
                objectReference = test.GetString();
            }

            return new VeraPdfError
            {
                RuleReference = ruleReference,
                Description = description,
                PageNumber = pageNumber,
                ObjectReference = objectReference
            };
        }
        catch
        {
            return null;
        }
    }

    private static VeraPdfResult CreateFailedResult(string errorMessage)
    {
        return new VeraPdfResult
        {
            IsCompliant = false,
            Flavour = PdfFlavour.None,
            Status = ValidationStatus.Fail,
            Errors = new List<VeraPdfError>
            {
                new VeraPdfError
                {
                    RuleReference = "N/A",
                    Description = errorMessage,
                    PageNumber = null,
                    ObjectReference = null
                }
            }.AsReadOnly(),
            TotalChecks = 0,
            FailedChecks = 1
        };
    }
}
