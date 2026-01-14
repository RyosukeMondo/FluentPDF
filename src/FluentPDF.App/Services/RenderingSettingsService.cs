using System;
using System.Reactive.Subjects;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Windows.Storage;

namespace FluentPDF.App.Services;

/// <summary>
/// Manages rendering quality settings with persistent storage and observable changes.
/// </summary>
/// <remarks>
/// Stores rendering quality in ApplicationData.LocalSettings.
/// Provides an observable stream for quality changes.
/// Defaults to Auto quality when no setting exists.
/// </remarks>
public sealed class RenderingSettingsService : IRenderingSettingsService, IDisposable
{
    private const string StorageKey = "RenderingQuality";
    private const RenderingQuality DefaultQuality = RenderingQuality.Auto;

    private readonly ILogger<RenderingSettingsService> _logger;
    private readonly ApplicationDataContainer _settings;
    private readonly BehaviorSubject<RenderingQuality> _qualitySubject;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingSettingsService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking settings operations.</param>
    public RenderingSettingsService(ILogger<RenderingSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = ApplicationData.Current.LocalSettings;

        // Load current quality and initialize subject
        var currentQuality = LoadQualityFromStorage();
        _qualitySubject = new BehaviorSubject<RenderingQuality>(currentQuality);

        _logger.LogInformation("RenderingSettingsService initialized with quality: {Quality}", currentQuality);
    }

    /// <inheritdoc/>
    public Result<RenderingQuality> GetRenderingQuality()
    {
        try
        {
            _logger.LogDebug("Getting rendering quality: {Quality}", _qualitySubject.Value);
            return Result.Ok(_qualitySubject.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rendering quality");
            return Result.Fail<RenderingQuality>($"Failed to get rendering quality: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result SetRenderingQuality(RenderingQuality quality)
    {
        try
        {
            if (!Enum.IsDefined(typeof(RenderingQuality), quality))
            {
                var error = $"Invalid rendering quality value: {quality}";
                _logger.LogWarning(error);
                return Result.Fail(error);
            }

            _logger.LogInformation("Setting rendering quality to: {Quality}", quality);

            // Save to storage
            _settings.Values[StorageKey] = (int)quality;

            // Notify observers if value changed
            if (_qualitySubject.Value != quality)
            {
                _qualitySubject.OnNext(quality);
                _logger.LogDebug("Notified observers of quality change: {OldQuality} -> {NewQuality}",
                    _qualitySubject.Value, quality);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set rendering quality to {Quality}", quality);
            return Result.Fail($"Failed to set rendering quality: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public IObservable<RenderingQuality> ObserveRenderingQuality()
    {
        _logger.LogDebug("Creating quality observer");
        return _qualitySubject;
    }

    /// <summary>
    /// Loads the rendering quality from storage.
    /// Returns the default quality if no setting exists or loading fails.
    /// </summary>
    private RenderingQuality LoadQualityFromStorage()
    {
        try
        {
            if (_settings.Values.TryGetValue(StorageKey, out var storedValue) &&
                storedValue is int qualityInt)
            {
                if (Enum.IsDefined(typeof(RenderingQuality), qualityInt))
                {
                    var quality = (RenderingQuality)qualityInt;
                    _logger.LogDebug("Loaded rendering quality from storage: {Quality}", quality);
                    return quality;
                }
                else
                {
                    _logger.LogWarning("Invalid quality value in storage: {Value}, using default", qualityInt);
                }
            }
            else
            {
                _logger.LogDebug("No rendering quality found in storage, using default: {Quality}", DefaultQuality);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rendering quality from storage, using default");
        }

        return DefaultQuality;
    }

    /// <summary>
    /// Disposes resources used by the service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _qualitySubject?.Dispose();
        _disposed = true;

        _logger.LogDebug("RenderingSettingsService disposed");
    }
}
