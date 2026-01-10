using System;
using System.Collections.Generic;

namespace FluentPDF.Core.Services;

/// <summary>
/// Provides telemetry tracking capabilities for events and exceptions.
/// </summary>
/// <remarks>
/// This abstraction enables application insights, diagnostics, and usage analytics.
/// Implementations should track events, exceptions, and custom metrics for observability.
/// </remarks>
public interface ITelemetryService
{
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
    void TrackEvent(string eventName, Dictionary<string, object>? properties = null);

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
    void TrackException(Exception exception, Dictionary<string, object>? properties = null);
}
