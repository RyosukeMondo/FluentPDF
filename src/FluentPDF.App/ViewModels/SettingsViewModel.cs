using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// View model for the application settings page.
/// Manages rendering quality settings, app preferences, and user settings.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly IRenderingSettingsService _renderingSettingsService;
    private readonly ISettingsService _settingsService;
    private IDisposable? _qualitySubscription;
    private bool _disposed;
    private bool _isLoadingSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking view model operations.</param>
    /// <param name="renderingSettingsService">Service for managing rendering quality settings.</param>
    /// <param name="settingsService">Service for managing application settings.</param>
    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IRenderingSettingsService renderingSettingsService,
        ISettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _renderingSettingsService = renderingSettingsService ?? throw new ArgumentNullException(nameof(renderingSettingsService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

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

        // Load app settings
        LoadSettings();

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
    /// Gets or sets the default zoom level for newly opened documents.
    /// </summary>
    [ObservableProperty]
    private ZoomLevel _defaultZoom;

    /// <summary>
    /// Gets or sets the scroll mode for document viewing.
    /// </summary>
    [ObservableProperty]
    private ScrollMode _scrollMode;

    /// <summary>
    /// Gets or sets the application theme preference.
    /// </summary>
    [ObservableProperty]
    private AppTheme _theme;

    /// <summary>
    /// Gets or sets whether anonymous telemetry is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _telemetryEnabled;

    /// <summary>
    /// Gets or sets whether anonymous crash reporting is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _crashReportingEnabled;

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
    /// Called when the default zoom level changes.
    /// Saves the updated setting to persistent storage.
    /// </summary>
    partial void OnDefaultZoomChanged(ZoomLevel value)
    {
        if (_isLoadingSettings)
            return;

        _logger.LogInformation("Default zoom changed to: {ZoomLevel}", value);
        _settingsService.Settings.DefaultZoom = value;
        _ = _settingsService.SaveAsync();
    }

    /// <summary>
    /// Called when the scroll mode changes.
    /// Saves the updated setting to persistent storage.
    /// </summary>
    partial void OnScrollModeChanged(ScrollMode value)
    {
        if (_isLoadingSettings)
            return;

        _logger.LogInformation("Scroll mode changed to: {ScrollMode}", value);
        _settingsService.Settings.ScrollMode = value;
        _ = _settingsService.SaveAsync();
    }

    /// <summary>
    /// Called when the theme changes.
    /// Saves the updated setting to persistent storage.
    /// </summary>
    partial void OnThemeChanged(AppTheme value)
    {
        if (_isLoadingSettings)
            return;

        _logger.LogInformation("Theme changed to: {Theme}", value);
        _settingsService.Settings.Theme = value;
        _ = _settingsService.SaveAsync();
    }

    /// <summary>
    /// Called when telemetry enabled state changes.
    /// Saves the updated setting to persistent storage.
    /// </summary>
    partial void OnTelemetryEnabledChanged(bool value)
    {
        if (_isLoadingSettings)
            return;

        _logger.LogInformation("Telemetry enabled changed to: {Enabled}", value);
        _settingsService.Settings.TelemetryEnabled = value;
        _ = _settingsService.SaveAsync();
    }

    /// <summary>
    /// Called when crash reporting enabled state changes.
    /// Saves the updated setting to persistent storage.
    /// </summary>
    partial void OnCrashReportingEnabledChanged(bool value)
    {
        if (_isLoadingSettings)
            return;

        _logger.LogInformation("Crash reporting enabled changed to: {Enabled}", value);
        _settingsService.Settings.CrashReportingEnabled = value;
        _ = _settingsService.SaveAsync();
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        _logger.LogInformation("Reset to defaults command invoked");

        try
        {
            await _settingsService.ResetToDefaultsAsync();
            LoadSettings();
            _logger.LogInformation("Settings reset to defaults successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults");
        }
    }

    /// <summary>
    /// Loads current settings from the settings service.
    /// </summary>
    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = _settingsService.Settings;
            DefaultZoom = settings.DefaultZoom;
            ScrollMode = settings.ScrollMode;
            Theme = settings.Theme;
            TelemetryEnabled = settings.TelemetryEnabled;
            CrashReportingEnabled = settings.CrashReportingEnabled;

            _logger.LogInformation("Settings loaded: Zoom={Zoom}, ScrollMode={ScrollMode}, Theme={Theme}, Telemetry={Telemetry}, CrashReporting={CrashReporting}",
                DefaultZoom, ScrollMode, Theme, TelemetryEnabled, CrashReportingEnabled);
        }
        finally
        {
            _isLoadingSettings = false;
        }
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
