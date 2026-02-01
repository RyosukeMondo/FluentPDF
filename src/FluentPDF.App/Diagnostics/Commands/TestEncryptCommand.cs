// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentPDF.App.Diagnostics.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Diagnostics.Commands;

/// <summary>
/// Implements --test-encrypt command for verifying PDF encryption functionality.
/// </summary>
public sealed class TestEncryptCommand
{
    private readonly ISecurityService _securityService;
    private readonly ILogger<TestEncryptCommand> _logger;

    public TestEncryptCommand(
        ISecurityService securityService,
        ILogger<TestEncryptCommand> logger)
    {
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EncryptCommandResult> ExecuteAsync(
        string inputFile,
        string outputFile,
        string? userPassword,
        string ownerPassword,
        int strength = 256,
        bool allowPrint = true,
        bool allowCopy = true,
        bool allowModify = true,
        bool allowAnnotate = true)
    {
        var result = new EncryptCommandResult
        {
            Command = "test-encrypt",
            Timestamp = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting encryption test: {InputFile}", inputFile);

            if (!File.Exists(inputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Input file not found: {inputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Input.File = inputFile;
            result.Input.SizeBytes = new FileInfo(inputFile).Length;

            // Build encryption settings
            var permissions = PdfPermissions.None;
            if (allowPrint) permissions |= PdfPermissions.Print;
            if (allowCopy) permissions |= PdfPermissions.Copy;
            if (allowModify) permissions |= PdfPermissions.Modify;
            if (allowAnnotate) permissions |= PdfPermissions.Annotate;

            var settings = new EncryptionSettings
            {
                UserPassword = userPassword,
                OwnerPassword = ownerPassword,
                Permissions = permissions,
                Strength = strength == 128 ? EncryptionStrength.Aes128 : EncryptionStrength.Aes256
            };

            result.Encryption.StrengthBits = strength;
            result.Encryption.HasUserPassword = !string.IsNullOrEmpty(userPassword);
            result.Encryption.HasOwnerPassword = !string.IsNullOrEmpty(ownerPassword);
            result.Encryption.AllowPrint = allowPrint;
            result.Encryption.AllowCopy = allowCopy;
            result.Encryption.AllowModify = allowModify;
            result.Encryption.AllowAnnotate = allowAnnotate;

            // Validate settings
            var validationResult = _securityService.ValidateSettings(settings);
            if (validationResult.IsFailed)
            {
                result.Status = "error";
                result.Errors.Add($"Invalid encryption settings: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Encrypt document
            var encryptResult = await _securityService.EncryptDocumentAsync(
                inputFile,
                outputFile,
                settings);

            if (encryptResult.IsFailed)
            {
                result.Status = "error";
                var errorMessages = encryptResult.Errors.Select(e => e.Message).ToList();
                result.Errors.AddRange(errorMessages);

                // Check for QPDF not found error
                if (errorMessages.Any(e => e.Contains("QPDF")))
                {
                    result.QpdfAvailable = false;
                }

                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.QpdfAvailable = true;

            if (!File.Exists(outputFile))
            {
                result.Status = "error";
                result.Errors.Add($"Output file was not created: {outputFile}");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.Output.File = outputFile;
            result.Output.SizeBytes = new FileInfo(outputFile).Length;

            // Verify encryption was applied
            var isEncryptedResult = await _securityService.IsEncryptedAsync(outputFile);
            if (isEncryptedResult.IsSuccess)
            {
                result.VerificationPassed = isEncryptedResult.Value;
                if (!isEncryptedResult.Value)
                {
                    result.Errors.Add("Output file is not encrypted");
                }
            }

            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            result.Status = result.Errors.Count == 0 ? "success" : "error";

            _logger.LogInformation("Encryption test completed: {Status} in {DurationMs}ms", result.Status, result.DurationMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption test failed");
            result.Status = "error";
            result.Errors.Add($"Unexpected error: {ex.Message}");
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }
}
