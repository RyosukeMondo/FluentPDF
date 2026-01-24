// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Reflection;
using FluentPDF.App.Api.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// Health check endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/health", () =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";

            // Check if PDFium is initialized
            var pdfiumLoaded = PdfiumInterop.IsInitialized;

            if (!pdfiumLoaded)
            {
                return Results.Json(
                    new HealthResponse("unhealthy", version, false),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Ok(new HealthResponse("healthy", version, true));
        })
        .WithName("HealthCheck")
        .WithTags("Health")
        .Produces<HealthResponse>()
        .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
    }
}
