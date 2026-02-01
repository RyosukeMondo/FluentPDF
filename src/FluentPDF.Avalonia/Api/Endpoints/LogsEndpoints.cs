// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Logs retrieval endpoints for debugging.
/// </summary>
public static class LogsEndpoints
{
    /// <summary>
    /// Maps logs endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app, ILogBufferService logBuffer)
    {
        // Get recent logs
        app.MapGet("/api/logs", (int? count) =>
        {
            var logs = logBuffer.GetRecentLogs(count ?? 100);
            return Results.Ok(new
            {
                Count = logs.Count,
                Logs = logs
            });
        })
        .WithName("GetLogs")
        .WithTags("Logs")
        .Produces<object>();

        // Get logs by level
        app.MapGet("/api/logs/level/{level}", (string level) =>
        {
            var logs = logBuffer.GetLogsByLevel(level);
            return Results.Ok(new
            {
                Level = level,
                Count = logs.Count,
                Logs = logs
            });
        })
        .WithName("GetLogsByLevel")
        .WithTags("Logs")
        .Produces<object>();

        // Get logs since timestamp
        app.MapGet("/api/logs/since/{timestamp}", (string timestamp) =>
        {
            if (!DateTimeOffset.TryParse(timestamp, out var since))
            {
                return Results.BadRequest(new { Error = "Invalid timestamp format" });
            }

            var logs = logBuffer.GetLogsSince(since);
            return Results.Ok(new
            {
                Since = since,
                Count = logs.Count,
                Logs = logs
            });
        })
        .WithName("GetLogsSince")
        .WithTags("Logs")
        .Produces<object>()
        .Produces(StatusCodes.Status400BadRequest);

        // Clear logs
        app.MapDelete("/api/logs", () =>
        {
            logBuffer.Clear();
            return Results.Ok(new { Message = "Logs cleared" });
        })
        .WithName("ClearLogs")
        .WithTags("Logs")
        .Produces<object>();

        // Get error logs only
        app.MapGet("/api/logs/errors", () =>
        {
            var errors = logBuffer.GetLogsByLevel("Error")
                .Concat(logBuffer.GetLogsByLevel("Critical"))
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            return Results.Ok(new
            {
                Count = errors.Count,
                Logs = errors
            });
        })
        .WithName("GetErrorLogs")
        .WithTags("Logs")
        .Produces<object>();
    }
}
