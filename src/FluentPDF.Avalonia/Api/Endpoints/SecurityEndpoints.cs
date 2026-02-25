// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for PDF security operations.
/// </summary>
public static class SecurityEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/security")
            .WithTags("Security");

        group.MapPost("/check-encrypted", async (
            CheckEncryptedRequest request,
            ISecurityService securityService) =>
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "filePath is required"));

            if (!File.Exists(request.FilePath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"File not found: {request.FilePath}"));

            var result = await securityService.IsEncryptedAsync(request.FilePath);

            return result.IsSuccess
                ? Results.Json(new { success = true, filePath = request.FilePath, isEncrypted = result.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Check failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("CheckEncrypted")
        .WithSummary("Check if a PDF file is encrypted");

        group.MapPost("/encrypt", async (
            EncryptDocumentRequest request,
            ISecurityService securityService) =>
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "inputPath is required"));

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "outputPath is required"));

            if (string.IsNullOrWhiteSpace(request.OwnerPassword))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "ownerPassword is required"));

            if (!File.Exists(request.InputPath))
                return Results.BadRequest(new ErrorResponse("FILE_NOT_FOUND", $"File not found: {request.InputPath}"));

            var permissions = Enum.TryParse<PdfPermissions>(request.Permissions, true, out var perms)
                ? perms : PdfPermissions.None;
            var strength = Enum.TryParse<EncryptionStrength>(request.Strength, true, out var str)
                ? str : EncryptionStrength.Aes256;

            var settings = new EncryptionSettings
            {
                UserPassword = request.UserPassword,
                OwnerPassword = request.OwnerPassword,
                Permissions = permissions,
                Strength = strength
            };

            var result = await securityService.EncryptDocumentAsync(request.InputPath, request.OutputPath, settings);

            return result.IsSuccess
                ? Results.Json(new { success = true, outputPath = request.OutputPath, strength = strength.ToString() })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Encryption failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("EncryptDocument")
        .WithSummary("Encrypt a PDF document with password protection");
    }

    internal record CheckEncryptedRequest(string FilePath);
    internal record EncryptDocumentRequest(string InputPath, string OutputPath, string? UserPassword, string OwnerPassword, string? Permissions, string? Strength);
}
