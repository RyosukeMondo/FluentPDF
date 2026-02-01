// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Reflection;
using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Avalonia.Services;
using FluentPDF.Rendering.Interop;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Health check endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app, IOperationWatchdog? watchdog = null)
    {
        app.MapGet("/api/health", (IDocumentSessionManager? sessionManager) =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";

            // Check if PDFium is initialized
            var pdfiumLoaded = PdfiumInterop.IsInitialized;

            if (!pdfiumLoaded)
            {
                return Results.Json(
                    new HealthResponse("unhealthy", version, false, 0, DateTime.UtcNow),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var activeSessions = sessionManager?.SessionCount ?? 0;

            return Results.Ok(new HealthResponse("healthy", version, true, activeSessions, DateTime.UtcNow));
        })
        .WithName("HealthCheck")
        .WithTags("Health")
        .Produces<HealthResponse>()
        .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        // Watchdog endpoints (if watchdog is available)
        if (watchdog != null)
        {
            // Get watchdog status (all operations)
            app.MapGet("/api/health/watchdog", () =>
            {
                var activeOps = watchdog.GetActiveOperations();
                var hungOps = watchdog.GetHungOperations();

                return Results.Ok(new
                {
                    TotalActive = activeOps.Length,
                    TotalHung = hungOps.Length,
                    IsHealthy = watchdog.IsHealthy(),
                    Operations = activeOps.Select(op => new
                    {
                        op.OperationId,
                        op.OperationName,
                        op.StartTime,
                        ElapsedMs = op.ElapsedTime.TotalMilliseconds,
                        TimeoutMs = op.TimeoutMs,
                        IsHung = op.IsHung,
                        HungFor = op.IsHung ? op.ElapsedTime.TotalMilliseconds - op.TimeoutMs : 0,
                        op.ErrorMessage
                    }).ToList()
                });
            })
            .WithName("WatchdogStatus")
            .WithTags("Health")
            .Produces<object>();

            // Get only hung operations
            app.MapGet("/api/health/hung", () =>
            {
                var hungOps = watchdog.GetHungOperations();

                if (hungOps.Length == 0)
                {
                    return Results.Ok(new
                    {
                        Message = "No hung operations detected",
                        HungOperations = Array.Empty<object>()
                    });
                }

                return Results.Json(new
                {
                    Message = $"WARNING: {hungOps.Length} hung operation(s) detected",
                    HungOperations = hungOps.Select(op => new
                    {
                        op.OperationId,
                        op.OperationName,
                        op.StartTime,
                        ElapsedMs = op.ElapsedTime.TotalMilliseconds,
                        TimeoutMs = op.TimeoutMs,
                        HungForMs = op.ElapsedTime.TotalMilliseconds - op.TimeoutMs,
                        Severity = op.ElapsedTime.TotalMilliseconds > op.TimeoutMs * 2 ? "CRITICAL" : "WARNING"
                    }).ToList()
                }, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetHungOperations")
            .WithTags("Health")
            .Produces<object>()
            .Produces<object>(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
