// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Reflection;
using FluentPDF.App.Api.Endpoints;
using FluentPDF.App.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FluentPDF.App.Api;

/// <summary>
/// Interface for the verification API server.
/// </summary>
public interface IVerificationApiServer
{
    /// <summary>
    /// Starts the API server.
    /// </summary>
    /// <param name="port">Port to listen on.</param>
    /// <param name="bindAddress">Address to bind to (default: localhost).</param>
    /// <param name="ct">Cancellation token.</param>
    Task StartAsync(int port, string bindAddress = "localhost", CancellationToken ct = default);

    /// <summary>
    /// Stops the API server.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether the server is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the base URL of the running server.
    /// </summary>
    string? BaseUrl { get; }
}

/// <summary>
/// Embedded REST API server for autonomous verification.
/// Uses ASP.NET Core minimal APIs with Kestrel.
/// </summary>
public sealed class VerificationApiServer : IVerificationApiServer, IAsyncDisposable
{
    private readonly IServiceProvider _appServices;
    private readonly ILogger<VerificationApiServer> _logger;
    private WebApplication? _webApp;
    private bool _isRunning;
    private string? _baseUrl;

    public VerificationApiServer(
        IServiceProvider appServices,
        ILogger<VerificationApiServer> logger)
    {
        _appServices = appServices;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public string? BaseUrl => _baseUrl;

    /// <inheritdoc />
    public async Task StartAsync(int port, string bindAddress = "localhost", CancellationToken ct = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("API server already running at {BaseUrl}", _baseUrl);
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();

        // Configure Kestrel
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });

        // Configure minimal logging

        // Register API-specific services
        builder.Services.AddSingleton<IDocumentSessionManager, DocumentSessionManager>();
        builder.Services.AddSingleton<IHashingService, HashingService>();

        // Re-use existing services from the main app's DI container
        builder.Services.AddSingleton(_appServices.GetRequiredService<IPdfDocumentService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IPdfRenderingService>());

        // Configure JSON serialization
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        _webApp = builder.Build();

        // Add correlation ID middleware
        _webApp.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? Guid.NewGuid().ToString("N");
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });

        // Map endpoints
        HealthEndpoints.Map(_webApp);
        DocumentEndpoints.Map(_webApp);
        RenderEndpoints.Map(_webApp);
        VerifyEndpoints.Map(_webApp);

        _baseUrl = $"http://{bindAddress}:{port}";

        await _webApp.StartAsync(ct);
        _isRunning = true;

        _logger.LogInformation("Verification API server started at {BaseUrl}", _baseUrl);
        Console.WriteLine($"Verification API server running at {_baseUrl}");
        Console.WriteLine($"  Health: GET {_baseUrl}/api/health");
        Console.WriteLine($"  Load:   POST {_baseUrl}/api/document/load");
        Console.WriteLine($"  Render: POST {_baseUrl}/api/render");
        Console.WriteLine($"  Verify: POST {_baseUrl}/api/verify/render");
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (!_isRunning || _webApp is null)
        {
            return;
        }

        _logger.LogInformation("Stopping verification API server");

        // Close all document sessions
        var sessionManager = _webApp.Services.GetService<IDocumentSessionManager>();
        sessionManager?.CloseAllSessions();

        await _webApp.StopAsync();
        _isRunning = false;
        _baseUrl = null;

        _logger.LogInformation("Verification API server stopped");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_webApp is not null)
        {
            await _webApp.DisposeAsync();
        }
    }
}
