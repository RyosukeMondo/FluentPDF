using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the application settings page.
/// Manages rendering quality settings and provides UI-friendly descriptions.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly IRenderingSettingsService _renderingSettingsService;
    private IDisposable? _qualitySubscription;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking view model operations.</param>
    /// <param name="renderingSettingsService">Service for managing rendering quality settings.</param>
    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IRenderingSettingsService renderingSettingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _renderingSettingsService = renderingSettingsService ?? throw new ArgumentNullException(nameof(renderingSettingsService));

        // Initialize quality options for ComboBox
        QualityOptions = new List<QualityOption>
        {
            new QualityOption(
                RenderingQuality.Auto,
                "Auto (Recommended)",
                "Automatically adjusts quality based on your display DPI and zoom level. Best balance of quality and performance."),
            new QualityOption(
                RenderingQuality.Low,
                "Low (75 DPI)",
                "Minimal memory usage, fastest rendering. Suitable for quick previews or low-end devices."),
            new QualityOption(
                RenderingQuality.Medium,
                "Medium (96 DPI)",
                "Standard quality for 100% scaling. Balanced memory usage and rendering speed."),
            new QualityOption(
                RenderingQuality.High,
                "High (144 DPI)",
                "Higher quality for high-DPI displays or 150% scaling. Increased memory usage."),
            new QualityOption(
                RenderingQuality.Ultra,
                "Ultra (192+ DPI)",
                "Maximum quality for 4K displays, 200% scaling, or professional use. High memory usage - may cause out-of-memory errors on large documents.")
        };

        // Load current quality
        var currentQualityResult = _renderingSettingsService.GetRenderingQuality();
        if (currentQualityResult.IsSuccess)
        {
            var currentOption = QualityOptions.FirstOrDefault(q => q.Quality == currentQualityResult.Value);
            SelectedQualityOption = currentOption ?? QualityOptions[0];
        }
        else
        {
            _logger.LogWarning("Failed to load rendering quality: {Error}", currentQualityResult.ToString());
            SelectedQualityOption = QualityOptions[0]; // Default to Auto
        }

        // Subscribe to quality changes from other sources
        _qualitySubscription = _renderingSettingsService.ObserveRenderingQuality()
            .Subscribe(quality =>
            {
                var option = QualityOptions.FirstOrDefault(q => q.Quality == quality);
                if (option != null && SelectedQualityOption?.Quality != quality)
                {
                    SelectedQualityOption = option;
                }
            });

        _logger.LogInformation("SettingsViewModel initialized with quality: {Quality}", SelectedQualityOption?.Quality);
    }

    /// <summary>
    /// Gets the list of available rendering quality options for the ComboBox.
    /// </summary>
    public IReadOnlyList<QualityOption> QualityOptions { get; }

    /// <summary>
    /// Gets or sets the selected rendering quality option.
    /// </summary>
    [ObservableProperty]
    private QualityOption? _selectedQualityOption;

    /// <summary>
    /// Gets a value indicating whether the selected quality is Ultra, which requires a performance warning.
    /// </summary>
    public bool IsUltraQualitySelected => SelectedQualityOption?.Quality == RenderingQuality.Ultra;

    /// <summary>
    /// Applies the selected rendering quality setting.
    /// </summary>
    [RelayCommand]
    private void ApplyQuality()
    {
        if (SelectedQualityOption == null)
        {
            _logger.LogWarning("Cannot apply quality: no quality selected");
            return;
        }

        _logger.LogInformation("Applying rendering quality: {Quality}", SelectedQualityOption.Quality);

        var result = _renderingSettingsService.SetRenderingQuality(SelectedQualityOption.Quality);
        if (result.IsFailed)
        {
            _logger.LogError("Failed to apply rendering quality: {Error}", result.ToString());
        }
        else
        {
            _logger.LogInformation("Successfully applied rendering quality: {Quality}", SelectedQualityOption.Quality);
        }
    }

    /// <summary>
    /// Called when the selected quality option changes.
    /// Updates the IsUltraQualitySelected property.
    /// </summary>
    partial void OnSelectedQualityOptionChanged(QualityOption? value)
    {
        OnPropertyChanged(nameof(IsUltraQualitySelected));
    }

    /// <summary>
    /// Disposes resources used by the SettingsViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing SettingsViewModel");

        _qualitySubscription?.Dispose();
        _qualitySubscription = null;

        _disposed = true;
    }
}

/// <summary>
/// Represents a rendering quality option for display in the UI.
/// </summary>
public record QualityOption(RenderingQuality Quality, string DisplayName, string Description);
