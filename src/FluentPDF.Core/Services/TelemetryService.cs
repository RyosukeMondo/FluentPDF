using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.Services;

/// <summary>
/// Default implementation of ITelemetryService using structured logging.
/// </summary>
/// <remarks>
/// This implementation logs telemetry events to the configured ILogger.
/// Future implementations can send data to Application Insights, OpenTelemetry, or other platforms.
/// For now, events are logged to Debug output and structured logs for analysis.
/// </remarks>
public sealed class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryService"/> class.
    /// </summary>
    /// <param name="logger">Logger for telemetry events.</param>
    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (eventName is null)
        {
            throw new ArgumentNullException(nameof(eventName));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be empty or whitespace.", nameof(eventName));
        }

        var propertiesJson = properties is not null
            ? JsonSerializer.Serialize(properties)
            : "{}";

        _logger.LogInformation(
            "Telemetry Event: {EventName}, Properties: {Properties}",
            eventName,
            propertiesJson);

        Debug.WriteLine($"[Telemetry] Event: {eventName}, Properties: {propertiesJson}");
    }

    /// <inheritdoc/>
    public void TrackException(Exception exception, Dictionary<string, object>? properties = null)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var propertiesJson = properties is not null
            ? JsonSerializer.Serialize(properties)
            : "{}";

        _logger.LogError(
            exception,
            "Telemetry Exception: {ExceptionType}, Properties: {Properties}",
            exception.GetType().Name,
            propertiesJson);

        Debug.WriteLine($"[Telemetry] Exception: {exception.GetType().Name}, Message: {exception.Message}, Properties: {propertiesJson}");
    }
}
