// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Reflection;
using FluentPDF.Avalonia.Api.Endpoints;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FluentPDF.Avalonia.Api;

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

        var builder = WebApplication.CreateBuilder();

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
        builder.Services.AddSingleton(_appServices.GetRequiredService<IPageOperationsService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IAnnotationService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<ITextExtractionService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<ITextReplacementService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IImageExportService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IDocumentEditingService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IPdfFormService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IWatermarkService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IStampService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IFdfService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<IImageInsertionService>());
        builder.Services.AddSingleton(_appServices.GetRequiredService<ISecurityService>());

        // Register ShapeService with document resolver wired to session manager
        builder.Services.AddSingleton<IShapeService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FluentPDF.Rendering.Services.ShapeService>>();
            var sessions = sp.GetRequiredService<IDocumentSessionManager>();
            return new FluentPDF.Rendering.Services.ShapeService(logger, sessions.GetDocument);
        });

        // Configure JSON serialization
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        // Configure Swagger/OpenAPI (development mode only)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "FluentPDF Verification API",
                Description = "REST API for autonomous testing and verification of FluentPDF document operations and rendering",
                Contact = new OpenApiContact
                {
                    Name = "FluentPDF Project",
                    Url = new Uri("https://github.com/yourusername/FluentPDF")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments if available
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        _webApp = builder.Build();

        // Enable Swagger UI (serves at root URL for convenience)
        _webApp.UseSwagger();
        _webApp.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FluentPDF Verification API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
            options.DocumentTitle = "FluentPDF Verification API";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
        });

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
        GuiEndpoints.Map(_webApp);
        PdfOperationsEndpoints.Map(_webApp);
        ShapeEndpoints.Map(_webApp);
        DiagnosticEndpoints.Map(_webApp);
        InteractionEndpoints.Map(_webApp);

        _baseUrl = $"http://{bindAddress}:{port}";

        await _webApp.StartAsync(ct);
        _isRunning = true;

        _logger.LogInformation("Verification API server started at {BaseUrl}", _baseUrl);
        _logger.LogInformation("  Swagger UI:  {BaseUrl}/", _baseUrl);
        _logger.LogInformation("  OpenAPI:     {BaseUrl}/swagger/v1/swagger.json", _baseUrl);
        _logger.LogInformation("  Health:      GET {BaseUrl}/api/health", _baseUrl);
        _logger.LogInformation("  Load:        POST {BaseUrl}/api/document/load", _baseUrl);
        _logger.LogInformation("  Render:      POST {BaseUrl}/api/render", _baseUrl);
        _logger.LogInformation("  Verify:      POST {BaseUrl}/api/verify/render", _baseUrl);

#if DEBUG
        // Debug console output for development convenience
        Console.WriteLine($"Verification API server running at {_baseUrl}");
        Console.WriteLine($"  Swagger UI:  {_baseUrl}/");
        Console.WriteLine($"  OpenAPI:     {_baseUrl}/swagger/v1/swagger.json");
        Console.WriteLine($"  Health:      GET {_baseUrl}/api/health");
        Console.WriteLine($"  Load:        POST {_baseUrl}/api/document/load");
        Console.WriteLine($"  Render:      POST {_baseUrl}/api/render");
        Console.WriteLine($"  Verify:      POST {_baseUrl}/api/verify/render");
#endif
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
