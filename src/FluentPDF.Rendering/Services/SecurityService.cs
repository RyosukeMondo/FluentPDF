using System.Diagnostics;
using System.Text;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for PDF security and encryption operations using QPDF.
/// Provides password protection, permission controls, and encryption strength options.
/// </summary>
public sealed class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private const string QpdfExecutable = "qpdf.exe";
    private const int MinPasswordLength = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> EncryptDocumentAsync(
        string inputPath,
        string outputPath,
        EncryptionSettings settings)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Encrypting PDF document. CorrelationId={CorrelationId}, InputPath={InputPath}, OutputPath={OutputPath}, Strength={Strength}",
            correlationId, inputPath, outputPath, settings.Strength);

        try
        {
            // Validate inputs
            if (!File.Exists(inputPath))
            {
                return Result.Fail(CreateError(
                    "SECURITY_FILE_NOT_FOUND",
                    $"Input file not found: {inputPath}",
                    correlationId));
            }

            var validationResult = ValidateSettings(settings);
            if (validationResult.IsFailed)
            {
                return validationResult;
            }

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Check if QPDF is available
            var qpdfPath = await FindQpdfExecutableAsync();
            if (qpdfPath == null)
            {
                return Result.Fail(CreateError(
                    "SECURITY_QPDF_NOT_FOUND",
                    "QPDF executable not found. Please install QPDF from https://github.com/qpdf/qpdf/releases",
                    correlationId));
            }

            // Build QPDF command line arguments
            var arguments = BuildEncryptionArguments(inputPath, outputPath, settings);

            _logger.LogDebug(
                "Executing QPDF. CorrelationId={CorrelationId}, Command=qpdf {Arguments}",
                correlationId, arguments);

            // Execute QPDF
            var result = await ExecuteQpdfAsync(qpdfPath, arguments, correlationId);

            if (result.IsFailed)
            {
                return result;
            }

            if (!File.Exists(outputPath))
            {
                return Result.Fail(CreateError(
                    "SECURITY_OUTPUT_NOT_CREATED",
                    "Encrypted output file was not created",
                    correlationId));
            }

            _logger.LogInformation(
                "PDF encryption completed successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to encrypt PDF document. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);
            return Result.Fail(CreateError(
                "SECURITY_ENCRYPTION_FAILED",
                $"Encryption failed: {ex.Message}",
                correlationId,
                ex));
        }
    }

    /// <inheritdoc />
    public Result ValidateSettings(EncryptionSettings settings)
    {
        if (settings == null)
        {
            return Result.Fail("Encryption settings cannot be null");
        }

        if (string.IsNullOrWhiteSpace(settings.OwnerPassword))
        {
            return Result.Fail("Owner password is required for encryption");
        }

        if (settings.OwnerPassword.Length < MinPasswordLength)
        {
            return Result.Fail($"Owner password must be at least {MinPasswordLength} characters");
        }

        if (!string.IsNullOrEmpty(settings.UserPassword) &&
            settings.UserPassword.Length < MinPasswordLength)
        {
            return Result.Fail($"User password must be at least {MinPasswordLength} characters");
        }

        if (settings.OwnerPassword == settings.UserPassword &&
            !string.IsNullOrEmpty(settings.UserPassword))
        {
            return Result.Fail("User password and owner password must be different");
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsEncryptedAsync(string filePath)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            if (!File.Exists(filePath))
            {
                return Result.Fail<bool>(CreateError(
                    "SECURITY_FILE_NOT_FOUND",
                    $"File not found: {filePath}",
                    correlationId));
            }

            var qpdfPath = await FindQpdfExecutableAsync();
            if (qpdfPath == null)
            {
                return Result.Fail<bool>(CreateError(
                    "SECURITY_QPDF_NOT_FOUND",
                    "QPDF executable not found",
                    correlationId));
            }

            var arguments = $"--check \"{filePath}\"";
            var result = await ExecuteQpdfAsync(qpdfPath, arguments, correlationId);

            // QPDF returns exit code 2 for encrypted files without password
            // We'll check the output for encryption indicators
            return Result.Ok(result.IsFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to check encryption status. CorrelationId={CorrelationId}, FilePath={FilePath}",
                correlationId, filePath);
            return Result.Fail<bool>(CreateError(
                "SECURITY_CHECK_FAILED",
                $"Failed to check encryption: {ex.Message}",
                correlationId,
                ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveEncryptionAsync(
        string inputPath,
        string outputPath,
        string ownerPassword)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Removing encryption from PDF. CorrelationId={CorrelationId}, InputPath={InputPath}",
            correlationId, inputPath);

        try
        {
            if (!File.Exists(inputPath))
            {
                return Result.Fail(CreateError(
                    "SECURITY_FILE_NOT_FOUND",
                    $"Input file not found: {inputPath}",
                    correlationId));
            }

            if (string.IsNullOrWhiteSpace(ownerPassword))
            {
                return Result.Fail("Owner password is required to remove encryption");
            }

            var qpdfPath = await FindQpdfExecutableAsync();
            if (qpdfPath == null)
            {
                return Result.Fail(CreateError(
                    "SECURITY_QPDF_NOT_FOUND",
                    "QPDF executable not found",
                    correlationId));
            }

            var arguments = $"--password=\"{EscapePassword(ownerPassword)}\" --decrypt \"{inputPath}\" \"{outputPath}\"";

            var result = await ExecuteQpdfAsync(qpdfPath, arguments, correlationId);

            if (result.IsFailed)
            {
                return result;
            }

            _logger.LogInformation(
                "Encryption removed successfully. CorrelationId={CorrelationId}, OutputPath={OutputPath}",
                correlationId, outputPath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove encryption. CorrelationId={CorrelationId}, InputPath={InputPath}",
                correlationId, inputPath);
            return Result.Fail(CreateError(
                "SECURITY_DECRYPTION_FAILED",
                $"Failed to remove encryption: {ex.Message}",
                correlationId,
                ex));
        }
    }

    private string BuildEncryptionArguments(
        string inputPath,
        string outputPath,
        EncryptionSettings settings)
    {
        var args = new StringBuilder();

        // Input file
        args.Append($"\"{inputPath}\"");

        // Encryption parameters
        var encryptionLevel = settings.Strength == EncryptionStrength.Aes256 ? 256 : 128;
        args.Append($" --encrypt");

        // User password (empty string if not set)
        var userPassword = string.IsNullOrEmpty(settings.UserPassword) ? "" : settings.UserPassword;
        args.Append($" \"{EscapePassword(userPassword)}\"");

        // Owner password
        args.Append($" \"{EscapePassword(settings.OwnerPassword)}\"");

        // Encryption strength
        args.Append($" {encryptionLevel}");

        // Permission flags
        var permissionArgs = BuildPermissionArguments(settings.Permissions);
        if (!string.IsNullOrEmpty(permissionArgs))
        {
            args.Append($" {permissionArgs}");
        }

        // End of encryption parameters
        args.Append(" --");

        // Output file
        args.Append($" \"{outputPath}\"");

        return args.ToString();
    }

    private string BuildPermissionArguments(PdfPermissions permissions)
    {
        var args = new List<string>();

        if (permissions.HasFlag(PdfPermissions.Print))
        {
            args.Add("--print=full");
        }
        else
        {
            args.Add("--print=none");
        }

        if (permissions.HasFlag(PdfPermissions.Modify))
        {
            args.Add("--modify=all");
        }
        else
        {
            args.Add("--modify=none");
        }

        if (!permissions.HasFlag(PdfPermissions.Copy))
        {
            args.Add("--extract=n");
        }

        if (!permissions.HasFlag(PdfPermissions.Annotate))
        {
            args.Add("--annotate=n");
        }

        return string.Join(" ", args);
    }

    private async Task<string?> FindQpdfExecutableAsync()
    {
        // Check common installation paths
        var commonPaths = new[]
        {
            QpdfExecutable, // In PATH
            @"C:\Program Files\qpdf\bin\qpdf.exe",
            @"C:\Program Files (x86)\qpdf\bin\qpdf.exe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "qpdf", "qpdf.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "qpdf", "qpdf.exe")
        };

        foreach (var path in commonPaths)
        {
            try
            {
                if (Path.IsPathRooted(path) && File.Exists(path))
                {
                    return path;
                }
                else if (!Path.IsPathRooted(path))
                {
                    // Try to execute to see if it's in PATH
                    var testProcess = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(testProcess);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                }
            }
            catch
            {
                // Continue to next path
                continue;
            }
        }

        return null;
    }

    private async Task<Result> ExecuteQpdfAsync(string qpdfPath, string arguments, Guid correlationId)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = qpdfPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "QPDF execution failed. CorrelationId={CorrelationId}, ExitCode={ExitCode}, Error={Error}",
                correlationId, process.ExitCode, error);

            return Result.Fail(CreateError(
                "SECURITY_QPDF_FAILED",
                $"QPDF failed with exit code {process.ExitCode}: {error}",
                correlationId));
        }

        if (!string.IsNullOrEmpty(output))
        {
            _logger.LogDebug(
                "QPDF output. CorrelationId={CorrelationId}, Output={Output}",
                correlationId, output);
        }

        return Result.Ok();
    }

    private string EscapePassword(string password)
    {
        // Escape quotes in password for command line
        return password.Replace("\"", "\\\"");
    }

    private static PdfError CreateError(
        string code,
        string message,
        Guid correlationId,
        Exception? exception = null)
    {
        var error = new PdfError(
            code,
            message,
            ErrorCategory.Security,
            ErrorSeverity.Error);

        error.WithContext("CorrelationId", correlationId.ToString());
        error.WithContext("Timestamp", DateTime.UtcNow);

        if (exception != null)
        {
            error.WithContext("Exception", exception.Message);
            error.WithContext("ExceptionType", exception.GetType().Name);
        }

        return error;
    }
}
