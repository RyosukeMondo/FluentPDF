using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// Main view model for the FluentPDF application.
/// Demonstrates MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking view model operations.</param>
    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("MainViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the application title.
    /// </summary>
    [ObservableProperty]
    private string _title = "FluentPDF";

    /// <summary>
    /// Gets or sets the current status message displayed to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Gets or sets a value indicating whether the application is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Command to load a document asynchronously.
    /// Simulates document loading with a delay.
    /// </summary>
    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        _logger.LogInformation("LoadDocumentAsync command invoked");

        IsLoading = true;
        StatusMessage = "Loading...";

        try
        {
            // Simulate document loading
            await Task.Delay(1000);

            StatusMessage = "Ready";
            _logger.LogInformation("Document loaded successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error loading document";
            _logger.LogError(ex, "Failed to load document");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to save the current document.
    /// Disabled when the application is loading.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        _logger.LogInformation("Save command invoked");
        StatusMessage = "Saved!";

        // Reset status after a short delay
        Task.Delay(2000).ContinueWith(_ => StatusMessage = "Ready");
    }

    /// <summary>
    /// Determines whether the Save command can execute.
    /// </summary>
    /// <returns>true if save is allowed; otherwise, false.</returns>
    private bool CanSave() => !IsLoading;

    /// <summary>
    /// Called when a property value changes.
    /// Overridden to provide logging for property changes.
    /// </summary>
    /// <param name="e">The property changed event arguments.</param>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsLoading))
        {
            // Notify that CanSave may have changed
            SaveCommand.NotifyCanExecuteChanged();
            _logger.LogDebug("IsLoading changed to {IsLoading}, SaveCommand CanExecute updated", IsLoading);
        }
    }
}
