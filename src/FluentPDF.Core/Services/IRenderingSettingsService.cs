using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for managing rendering quality settings.
/// Provides methods to persist and retrieve rendering quality preferences,
/// and observe changes to settings in real-time.
/// </summary>
public interface IRenderingSettingsService
{
    /// <summary>
    /// Gets the current rendering quality setting.
    /// Returns Auto if no setting has been saved.
    /// </summary>
    /// <returns>A result containing the current RenderingQuality, or an error if retrieval fails.</returns>
    Result<RenderingQuality> GetRenderingQuality();

    /// <summary>
    /// Sets the rendering quality setting and persists it to storage.
    /// Notifies all observers of the quality change.
    /// </summary>
    /// <param name="quality">The rendering quality to set.</param>
    /// <returns>A result indicating success or failure of the save operation.</returns>
    Result SetRenderingQuality(RenderingQuality quality);

    /// <summary>
    /// Observes changes to the rendering quality setting.
    /// Emits the current quality immediately upon subscription, then emits updates when the quality changes.
    /// </summary>
    /// <returns>An observable stream of RenderingQuality updates.</returns>
    IObservable<RenderingQuality> ObserveRenderingQuality();
}
