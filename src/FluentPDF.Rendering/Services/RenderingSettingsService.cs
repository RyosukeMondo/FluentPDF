using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for managing rendering quality settings.
/// Provides methods to persist and retrieve rendering quality preferences,
/// and observe changes to settings in real-time.
/// </summary>
public sealed class RenderingSettingsService : IRenderingSettingsService, IDisposable
{
    private readonly ILogger<RenderingSettingsService> _logger;
    private readonly BehaviorSubject<RenderingQuality> _qualitySubject;
    private RenderingQuality _currentQuality;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingSettingsService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking settings operations.</param>
    public RenderingSettingsService(ILogger<RenderingSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentQuality = RenderingQuality.Auto;
        _qualitySubject = new BehaviorSubject<RenderingQuality>(_currentQuality);

        _logger.LogInformation("RenderingSettingsService initialized with quality: {Quality}", _currentQuality);
    }

    /// <inheritdoc/>
    public Result<RenderingQuality> GetRenderingQuality()
    {
        try
        {
            _logger.LogDebug("Getting rendering quality: {Quality}", _currentQuality);
            return Result.Ok(_currentQuality);
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
            _logger.LogInformation("Setting rendering quality: {Quality}", quality);
            _currentQuality = quality;
            _qualitySubject.OnNext(quality);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set rendering quality");
            return Result.Fail($"Failed to set rendering quality: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public IObservable<RenderingQuality> ObserveRenderingQuality()
    {
        return _qualitySubject.AsObservable();
    }

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        _qualitySubject?.Dispose();
    }
}
