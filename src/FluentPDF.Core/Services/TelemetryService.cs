using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.Services;

/// <summary>
/// Telemetry service using structured logging.
/// </summary>
/// <remarks>
/// This implementation logs telemetry events to the configured ILogger.
/// Future implementations can send data to Application Insights, OpenTelemetry, or other platforms.
/// For now, events are logged to Debug output and structured logs for analysis.
/// </remarks>
public sealed class TelemetryService
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

    /// <summary>
    /// Tracks a custom event with optional properties.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="properties">Optional dictionary of custom properties associated with the event.</param>
    /// <remarks>
    /// Use this method to track user actions, feature usage, or application state changes.
    /// Properties are structured data that can be queried and aggregated in telemetry systems.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when eventName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when eventName is empty or whitespace.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON serialization is safe for telemetry dictionaries")]
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

#if DEBUG
        Debug.WriteLine($"[Telemetry] Event: {eventName}, Properties: {propertiesJson}");
#endif
    }

    /// <summary>
    /// Tracks an exception with optional properties.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional dictionary of custom properties associated with the exception.</param>
    /// <remarks>
    /// Use this method to track exceptions for error monitoring and diagnostics.
    /// Properties can include context such as user ID, operation ID, or component name.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JSON serialization is safe for telemetry dictionaries")]
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

#if DEBUG
        Debug.WriteLine($"[Telemetry] Exception: {exception.GetType().Name}, Message: {exception.Message}, Properties: {propertiesJson}");
#endif
    }
}
