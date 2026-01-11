using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for a single tab in the multi-document interface.
/// Wraps PdfViewerViewModel with tab-specific state (file path, name, active state).
/// </summary>
public partial class TabViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<TabViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabViewModel"/> class.
    /// </summary>
    /// <param name="filePath">The path to the PDF file.</param>
    /// <param name="viewerViewModel">The PDF viewer view model for this tab.</param>
    /// <param name="logger">Logger for tracking tab operations.</param>
    public TabViewModel(
        string filePath,
        PdfViewerViewModel viewerViewModel,
        ILogger<TabViewModel> logger)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        ViewerViewModel = viewerViewModel ?? throw new ArgumentNullException(nameof(viewerViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to ViewerViewModel property changes to update HasUnsavedChanges and DisplayName
        ViewerViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewerViewModel.HasUnsavedChanges))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
                OnPropertyChanged(nameof(DisplayName));
            }
        };

        _logger.LogInformation("TabViewModel created for file: {FilePath}", filePath);
    }

    /// <summary>
    /// Gets the full file path of the PDF document in this tab.
    /// </summary>
    [ObservableProperty]
    private string _filePath;

    /// <summary>
    /// Gets the file name (without path) of the PDF document.
    /// </summary>
    [ObservableProperty]
    private string _fileName;

    /// <summary>
    /// Gets the PDF viewer view model for this tab.
    /// </summary>
    [ObservableProperty]
    private PdfViewerViewModel _viewerViewModel;

    /// <summary>
    /// Gets or sets a value indicating whether this tab is currently active.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Gets a value indicating whether this tab has unsaved changes.
    /// Delegates to the ViewerViewModel's HasUnsavedChanges property.
    /// </summary>
    public bool HasUnsavedChanges => ViewerViewModel.HasUnsavedChanges;

    /// <summary>
    /// Gets the display name for the tab header.
    /// Returns "*" + FileName when there are unsaved changes, otherwise just FileName.
    /// </summary>
    public string DisplayName => HasUnsavedChanges ? $"*{FileName}" : FileName;

    /// <summary>
    /// Activates this tab, setting its state to active.
    /// </summary>
    public void Activate()
    {
        _logger.LogInformation("Activating tab for file: {FilePath}", FilePath);
        IsActive = true;
    }

    /// <summary>
    /// Deactivates this tab, setting its state to inactive.
    /// </summary>
    public void Deactivate()
    {
        _logger.LogInformation("Deactivating tab for file: {FilePath}", FilePath);
        IsActive = false;
    }

    /// <summary>
    /// Called when a property value changes.
    /// </summary>
    /// <param name="e">The property changed event arguments.</param>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(IsActive))
        {
            _logger.LogDebug(
                "Tab active state changed. FilePath={FilePath}, IsActive={IsActive}",
                FilePath, IsActive);
        }
    }

    /// <summary>
    /// Disposes resources used by the TabViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing TabViewModel for file: {FilePath}", FilePath);

        ViewerViewModel?.Dispose();

        _disposed = true;
    }
}
