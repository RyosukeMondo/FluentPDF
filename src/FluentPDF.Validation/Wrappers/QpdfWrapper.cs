using System.Diagnostics;
using System.Text;
using FluentPDF.Validation.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Wrapper for QPDF structural validation CLI tool.
/// </summary>
public sealed class QpdfWrapper : IQpdfWrapper
{
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private const string QpdfExecutable = "qpdf";

    /// <summary>
    /// Initializes a new instance of the <see cref="QpdfWrapper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    public QpdfWrapper(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<QpdfResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Fail<QpdfResult>("File path cannot be empty");
        }

        if (!File.Exists(filePath))
        {
            return Result.Fail<QpdfResult>($"File not found: {filePath}");
        }

        var correlationIdValue = correlationId ?? Guid.NewGuid().ToString("N")[..8];

        _logger.Information(
            "Starting QPDF validation for {FilePath} with correlation ID {CorrelationId}",
            filePath,
            correlationIdValue);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = QpdfExecutable,
                Arguments = $"--check \"{filePath}\"",
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
                    _logger.Warning(ex, "Failed to kill timed out QPDF process");
                }

                _logger.Warning(
                    "QPDF validation timed out after {Timeout}s for {FilePath} (CorrelationId: {CorrelationId})",
                    _timeout.TotalSeconds,
                    filePath,
                    correlationIdValue);

                return Result.Fail<QpdfResult>($"QPDF validation timed out after {_timeout.TotalSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();

            _logger.Debug(
                "QPDF completed with exit code {ExitCode} for {FilePath} (CorrelationId: {CorrelationId})",
                exitCode,
                filePath,
                correlationIdValue);

            var result = ParseQpdfOutput(exitCode, stdout, stderr);

            _logger.Information(
                "QPDF validation completed with status {Status} for {FilePath} (CorrelationId: {CorrelationId})",
                result.Status,
                filePath,
                correlationIdValue);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "QPDF validation failed with exception for {FilePath} (CorrelationId: {CorrelationId})",
                filePath,
                correlationIdValue);

            return Result.Fail<QpdfResult>($"QPDF validation failed: {ex.Message}");
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

    private static QpdfResult ParseQpdfOutput(int exitCode, string stdout, string stderr)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Exit code 0 means success, 2 means warnings, 3 means errors
        var status = exitCode switch
        {
            0 => ValidationStatus.Pass,
            2 => ValidationStatus.Warn,
            _ => ValidationStatus.Fail
        };

        // Parse stderr for errors and warnings
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // QPDF typically prefixes warnings with "WARNING:"
                if (trimmed.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add(trimmed);
                }
                else
                {
                    errors.Add(trimmed);
                }
            }
        }

        // Parse stdout for additional information
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // Look for error indicators in stdout
                if (trimmed.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
                {
                    if (!errors.Contains(trimmed))
                    {
                        errors.Add(trimmed);
                    }
                }
            }
        }

        return new QpdfResult
        {
            Status = status,
            Errors = errors.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }
}
