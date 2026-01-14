using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentPDF.Validation.Models;
using FluentResults;
using Serilog;

namespace FluentPDF.Validation.Wrappers;

/// <summary>
/// Wrapper for JHOVE format validation and characterization CLI tool.
/// </summary>
public sealed class JhoveWrapper : IJhoveWrapper
{
    private readonly ILogger _logger;
    private readonly string _jhoveJarPath;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private const string JavaExecutable = "java";

    /// <summary>
    /// Initializes a new instance of the <see cref="JhoveWrapper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging.</param>
    /// <param name="jhoveJarPath">Path to the JHOVE JAR file. If null, attempts to use 'jhove.jar' from PATH.</param>
    public JhoveWrapper(ILogger logger, string? jhoveJarPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jhoveJarPath = jhoveJarPath ?? "jhove.jar";
    }

    /// <inheritdoc/>
    public async Task<Result<JhoveResult>> ValidateAsync(
        string filePath,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Fail<JhoveResult>("File path cannot be empty");
        }

        if (!File.Exists(filePath))
        {
            return Result.Fail<JhoveResult>($"File not found: {filePath}");
        }

        var correlationIdValue = correlationId ?? Guid.NewGuid().ToString("N")[..8];

        _logger.Information(
            "Starting JHOVE validation for {FilePath} with correlation ID {CorrelationId}",
            filePath,
            correlationIdValue);

        // Verify Java is available
        var javaCheck = await VerifyJavaInstallationAsync(cancellationToken);
        if (javaCheck.IsFailed)
        {
            return Result.Fail<JhoveResult>(javaCheck.Errors);
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = JavaExecutable,
                Arguments = $"-jar \"{_jhoveJarPath}\" -m PDF-hul -h json \"{filePath}\"",
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
                    _logger.Warning(ex, "Failed to kill timed out JHOVE process");
                }

                _logger.Warning(
                    "JHOVE validation timed out after {Timeout}s for {FilePath} (CorrelationId: {CorrelationId})",
                    _timeout.TotalSeconds,
                    filePath,
                    correlationIdValue);

                return Result.Fail<JhoveResult>($"JHOVE validation timed out after {_timeout.TotalSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();

            _logger.Debug(
                "JHOVE completed with exit code {ExitCode} for {FilePath} (CorrelationId: {CorrelationId})",
                exitCode,
                filePath,
                correlationIdValue);

            if (exitCode != 0)
            {
                _logger.Error(
                    "JHOVE failed with exit code {ExitCode}: {StdErr} (CorrelationId: {CorrelationId})",
                    exitCode,
                    stderr,
                    correlationIdValue);
                return Result.Fail<JhoveResult>($"JHOVE execution failed with exit code {exitCode}: {stderr}");
            }

            var parseResult = ParseJhoveOutput(stdout);
            if (parseResult.IsFailed)
            {
                _logger.Error(
                    "Failed to parse JHOVE output for {FilePath} (CorrelationId: {CorrelationId}): {Errors}",
                    filePath,
                    correlationIdValue,
                    string.Join("; ", parseResult.Errors.Select(e => e.Message)));
                return parseResult;
            }

            var result = parseResult.Value;

            _logger.Information(
                "JHOVE validation completed with status {Status}, format {Format}, validity {Validity} for {FilePath} (CorrelationId: {CorrelationId})",
                result.Status,
                result.Format,
                result.Validity,
                filePath,
                correlationIdValue);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "JHOVE validation failed with exception for {FilePath} (CorrelationId: {CorrelationId})",
                filePath,
                correlationIdValue);

            return Result.Fail<JhoveResult>($"JHOVE validation failed: {ex.Message}");
        }
    }

    private async Task<Result> VerifyJavaInstallationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = JavaExecutable,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var completed = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken);

            if (!completed || process.ExitCode != 0)
            {
                return Result.Fail("Java is not installed or not available in PATH. JHOVE requires Java to run.");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to verify Java installation: {ex.Message}");
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

    private static Result<JhoveResult> ParseJhoveOutput(string jsonOutput)
    {
        if (string.IsNullOrWhiteSpace(jsonOutput))
        {
            return Result.Fail<JhoveResult>("JHOVE output is empty");
        }

        try
        {
            using var document = JsonDocument.Parse(jsonOutput);
            var root = document.RootElement;

            // JHOVE output structure: { "jhove": { "repInfo": [...] } }
            if (!root.TryGetProperty("jhove", out var jhoveElement))
            {
                return Result.Fail<JhoveResult>("Invalid JHOVE JSON: missing 'jhove' property");
            }

            if (!jhoveElement.TryGetProperty("repInfo", out var repInfoArray))
            {
                return Result.Fail<JhoveResult>("Invalid JHOVE JSON: missing 'repInfo' property");
            }

            if (repInfoArray.GetArrayLength() == 0)
            {
                return Result.Fail<JhoveResult>("Invalid JHOVE JSON: 'repInfo' array is empty");
            }

            var repInfo = repInfoArray[0];

            // Extract format (PDF version)
            var format = "Unknown";
            if (repInfo.TryGetProperty("version", out var versionElement))
            {
                format = versionElement.GetString() ?? "Unknown";
            }

            // Extract validity status
            var validity = "Not-Valid";
            if (repInfo.TryGetProperty("status", out var statusElement))
            {
                validity = statusElement.GetString() ?? "Not-Valid";
            }

            // Determine validation status
            var status = validity switch
            {
                "Well-Formed and valid" => ValidationStatus.Pass,
                "Well-Formed" => ValidationStatus.Warn,
                _ => ValidationStatus.Fail
            };

            // Extract metadata
            string? title = null;
            string? author = null;
            DateTime? creationDate = null;
            DateTime? modificationDate = null;
            int? pageCount = null;
            bool isEncrypted = false;
            var messages = new List<string>();

            if (repInfo.TryGetProperty("properties", out var propertiesArray))
            {
                foreach (var property in propertiesArray.EnumerateArray())
                {
                    if (!property.TryGetProperty("name", out var nameElement))
                    {
                        continue;
                    }

                    var name = nameElement.GetString();
                    if (name == null)
                    {
                        continue;
                    }

                    if (!property.TryGetProperty("values", out var valuesElement))
                    {
                        continue;
                    }

                    if (name == "Info")
                    {
                        ExtractInfoMetadata(valuesElement, ref title, ref author, ref creationDate, ref modificationDate);
                    }
                    else if (name == "Pages" && valuesElement.ValueKind == JsonValueKind.Number)
                    {
                        pageCount = valuesElement.GetInt32();
                    }
                    else if (name == "Encryption")
                    {
                        isEncrypted = true;
                    }
                }
            }

            // Extract messages (errors, warnings, info)
            if (repInfo.TryGetProperty("messages", out var messagesArray))
            {
                foreach (var messageElement in messagesArray.EnumerateArray())
                {
                    if (messageElement.TryGetProperty("message", out var msgText))
                    {
                        var msg = msgText.GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            messages.Add(msg);
                        }
                    }
                    else if (messageElement.ValueKind == JsonValueKind.String)
                    {
                        var msg = messageElement.GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                        {
                            messages.Add(msg);
                        }
                    }
                }
            }

            return Result.Ok(new JhoveResult
            {
                Format = format,
                Validity = validity,
                Status = status,
                Title = title,
                Author = author,
                CreationDate = creationDate,
                ModificationDate = modificationDate,
                PageCount = pageCount,
                IsEncrypted = isEncrypted,
                Messages = messages.AsReadOnly()
            });
        }
        catch (JsonException ex)
        {
            return Result.Fail<JhoveResult>($"Failed to parse JHOVE JSON output: {ex.Message}");
        }
    }

    private static void ExtractInfoMetadata(
        JsonElement valuesElement,
        ref string? title,
        ref string? author,
        ref DateTime? creationDate,
        ref DateTime? modificationDate)
    {
        if (valuesElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (valuesElement.TryGetProperty("Title", out var titleElement))
        {
            title = titleElement.GetString();
        }

        if (valuesElement.TryGetProperty("Author", out var authorElement))
        {
            author = authorElement.GetString();
        }

        if (valuesElement.TryGetProperty("CreationDate", out var creationDateElement))
        {
            var dateStr = creationDateElement.GetString();
            if (!string.IsNullOrWhiteSpace(dateStr) && TryParsePdfDate(dateStr, out var date))
            {
                creationDate = date;
            }
        }

        if (valuesElement.TryGetProperty("ModDate", out var modDateElement))
        {
            var dateStr = modDateElement.GetString();
            if (!string.IsNullOrWhiteSpace(dateStr) && TryParsePdfDate(dateStr, out var date))
            {
                modificationDate = date;
            }
        }
    }

    private static bool TryParsePdfDate(string pdfDate, out DateTime result)
    {
        result = default;

        // PDF dates are in format: D:YYYYMMDDHHmmSSOHH'mm'
        // Example: D:20230115123045+01'00'
        if (!pdfDate.StartsWith("D:", StringComparison.Ordinal))
        {
            return DateTime.TryParse(pdfDate, out result);
        }

        // Remove D: prefix
        pdfDate = pdfDate[2..];

        // Try to parse at least YYYYMMDD
        if (pdfDate.Length < 8)
        {
            return false;
        }

        try
        {
            var year = int.Parse(pdfDate[..4]);
            var month = int.Parse(pdfDate.Substring(4, 2));
            var day = int.Parse(pdfDate.Substring(6, 2));

            var hour = pdfDate.Length >= 10 ? int.Parse(pdfDate.Substring(8, 2)) : 0;
            var minute = pdfDate.Length >= 12 ? int.Parse(pdfDate.Substring(10, 2)) : 0;
            var second = pdfDate.Length >= 14 ? int.Parse(pdfDate.Substring(12, 2)) : 0;

            result = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
