using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// ViewModel for PDF zoom operations.
/// Manages zoom level and zoom commands.
/// </summary>
public partial class ZoomViewModel : ViewModelBase
{
    private readonly ILogger<ZoomViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomViewModel"/> class.
    /// </summary>
    public ZoomViewModel(ILogger<ZoomViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ZoomViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the current zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    /// </summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether an operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether a document is loaded.
    /// </summary>
    [ObservableProperty]
    private bool _hasDocument;

    /// <summary>
    /// Callback invoked when zoom level changes.
    /// </summary>
    public Func<double, Task>? OnZoomChanged { get; set; }

    /// <summary>
    /// Increases the zoom level.
    /// </summary>
    private const double ZoomStep = 0.1;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;

    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private async Task ZoomInAsync()
    {
        _logger.LogInformation("ZoomIn command invoked. CurrentZoom={CurrentZoom}", ZoomLevel);
        ZoomLevel = Math.Min(MaxZoom, Math.Round((ZoomLevel + ZoomStep) * 10) / 10);
        await NotifyZoomChangedAsync();
    }

    private bool CanZoomIn() => ZoomLevel < MaxZoom && HasDocument;

    /// <summary>
    /// Decreases the zoom level.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private async Task ZoomOutAsync()
    {
        _logger.LogInformation("ZoomOut command invoked. CurrentZoom={CurrentZoom}", ZoomLevel);
        ZoomLevel = Math.Max(MinZoom, Math.Round((ZoomLevel - ZoomStep) * 10) / 10);
        await NotifyZoomChangedAsync();
    }

    private bool CanZoomOut() => ZoomLevel > MinZoom && HasDocument;

    /// <summary>
    /// Resets the zoom level to 100%.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetZoom))]
    private async Task ResetZoomAsync()
    {
        _logger.LogInformation("ResetZoom command invoked");
        ZoomLevel = 1.0;
        await NotifyZoomChangedAsync();
    }

    private bool CanResetZoom() => Math.Abs(ZoomLevel - 1.0) > 0.01 && !IsLoading && HasDocument;

    /// <summary>
    /// Sets the zoom level to a specific value.
    /// </summary>
    [RelayCommand]
    private async Task SetZoomAsync(double zoomLevel)
    {
        _logger.LogInformation("SetZoom command invoked. ZoomLevel={ZoomLevel}", zoomLevel);

        if (zoomLevel >= MinZoom && zoomLevel <= MaxZoom && HasDocument)
        {
            ZoomLevel = zoomLevel;
            await NotifyZoomChangedAsync();
        }
    }

    /// <summary>
    /// Adjusts zoom to fit the page width in the viewport.
    /// </summary>
    [RelayCommand]
    private async Task FitWidthAsync()
    {
        _logger.LogInformation("FitWidth command invoked");
        ZoomLevel = 1.0;
        await NotifyZoomChangedAsync();
    }

    /// <summary>
    /// Adjusts zoom to fit the entire page in the viewport.
    /// </summary>
    [RelayCommand]
    private async Task FitPageAsync()
    {
        _logger.LogInformation("FitPage command invoked");
        ZoomLevel = 0.75;
        await NotifyZoomChangedAsync();
    }

    private async Task NotifyZoomChangedAsync()
    {
        if (OnZoomChanged != null)
        {
            await OnZoomChanged(ZoomLevel);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading) ||
            e.PropertyName == nameof(ZoomLevel) ||
            e.PropertyName == nameof(HasDocument))
        {
            ZoomInCommand.NotifyCanExecuteChanged();
            ZoomOutCommand.NotifyCanExecuteChanged();
            ResetZoomCommand.NotifyCanExecuteChanged();
        }
    }
}
