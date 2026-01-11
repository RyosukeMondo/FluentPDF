using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage;

using CoreScrollMode = FluentPDF.Core.Models.ScrollMode;

namespace FluentPDF.App.Services;

/// <summary>
/// Manages application settings with JSON persistence in LocalFolder.
/// </summary>
/// <remarks>
/// Stores settings in ApplicationData.LocalFolder/settings.json.
/// Uses debouncing to batch rapid saves and reduce I/O operations.
/// Validates settings on load, falling back to defaults for corrupt data.
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";
    private const int DebounceDurationMs = 500;

    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private CancellationTokenSource? _debounceCts;

    private AppSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking settings operations.</param>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = AppSettings.CreateDefault();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    public AppSettings Settings => _settings;

    /// <inheritdoc/>
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            _logger.LogInformation("Loading settings from storage");

            var localFolder = ApplicationData.Current.LocalFolder;
            var settingsFile = await localFolder.TryGetItemAsync(SettingsFileName) as StorageFile;

            if (settingsFile == null)
            {
                _logger.LogInformation("Settings file not found, using defaults");
                _settings = AppSettings.CreateDefault();
                await SaveAsync(); // Create initial settings file
                return;
            }

            var json = await FileIO.ReadTextAsync(settingsFile);

            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (loadedSettings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults");
                _settings = AppSettings.CreateDefault();
                await SaveAsync();
                return;
            }

            // Validate and apply settings
            _settings = ValidateSettings(loadedSettings);
            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _settings = AppSettings.CreateDefault();

            // Attempt to save default settings
            try
            {
                await SaveAsync();
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save default settings after load failure");
            }
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync()
    {
        // Cancel any pending debounced save
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var cancellationToken = _debounceCts.Token;

        try
        {
            // Debounce rapid saves
            await Task.Delay(DebounceDurationMs, cancellationToken);

            await _saveSemaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogDebug("Saving settings to storage");

                var localFolder = ApplicationData.Current.LocalFolder;
                var settingsFile = await localFolder.CreateFileAsync(
                    SettingsFileName,
                    CreationCollisionOption.ReplaceExisting);

                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                await FileIO.WriteTextAsync(settingsFile, json);

                _logger.LogInformation("Settings saved successfully");
                SettingsChanged?.Invoke(this, _settings);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Settings save was debounced/cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    /// <inheritdoc/>
    public async Task ResetToDefaultsAsync()
    {
        _logger.LogInformation("Resetting settings to defaults");

        _settings = AppSettings.CreateDefault();
        await SaveImmediatelyAsync();

        _logger.LogInformation("Settings reset to defaults");
    }

    /// <summary>
    /// Saves settings immediately without debouncing.
    /// Used for critical operations like reset.
    /// </summary>
    private async Task SaveImmediatelyAsync()
    {
        // Cancel any pending debounced save
        _debounceCts?.Cancel();

        await _saveSemaphore.WaitAsync();
        try
        {
            _logger.LogDebug("Saving settings immediately");

            var localFolder = ApplicationData.Current.LocalFolder;
            var settingsFile = await localFolder.CreateFileAsync(
                SettingsFileName,
                CreationCollisionOption.ReplaceExisting);

            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await FileIO.WriteTextAsync(settingsFile, json);

            _logger.LogInformation("Settings saved immediately");
            SettingsChanged?.Invoke(this, _settings);
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    /// <summary>
    /// Validates settings values and corrects any invalid data.
    /// </summary>
    private AppSettings ValidateSettings(AppSettings settings)
    {
        var hasInvalidValues = false;

        // Validate DefaultZoom enum
        if (!Enum.IsDefined(typeof(ZoomLevel), settings.DefaultZoom))
        {
            _logger.LogWarning("Invalid DefaultZoom value: {Value}, resetting to default", settings.DefaultZoom);
            settings.DefaultZoom = ZoomLevel.OneHundredPercent;
            hasInvalidValues = true;
        }

        // Validate ScrollMode enum
        if (!Enum.IsDefined(typeof(CoreScrollMode), settings.ScrollMode))
        {
            _logger.LogWarning("Invalid ScrollMode value: {Value}, resetting to default", settings.ScrollMode);
            settings.ScrollMode = CoreScrollMode.Vertical;
            hasInvalidValues = true;
        }

        // Validate Theme enum
        if (!Enum.IsDefined(typeof(AppTheme), settings.Theme))
        {
            _logger.LogWarning("Invalid Theme value: {Value}, resetting to default", settings.Theme);
            settings.Theme = AppTheme.UseSystem;
            hasInvalidValues = true;
        }

        if (hasInvalidValues)
        {
            _logger.LogInformation("Settings validation found invalid values, corrected settings will be saved");
        }

        return settings;
    }
}
