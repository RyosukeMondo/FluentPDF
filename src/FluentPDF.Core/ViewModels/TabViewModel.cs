using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// View model for a single tab in the PDF viewer.
/// UI-framework agnostic implementation.
/// </summary>
public partial class TabViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<TabViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the file path of the document in this tab.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file name (without path) of the document in this tab.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the PDF viewer view model for this tab.
    /// </summary>
    public PdfViewerViewModel ViewerViewModel { get; }

    /// <summary>
    /// Gets or sets the display title for the tab.
    /// </summary>
    [ObservableProperty]
    private string _title;

    /// <summary>
    /// Gets or sets a value indicating whether this tab is active.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Gets or sets a value indicating whether the tab has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => ViewerViewModel.HasUnsavedChanges;

    /// <summary>
    /// Gets the icon for the close button (shown when hovering or modified).
    /// </summary>
    public string CloseIcon => HasUnsavedChanges ? "\uE73E" : "\uE8BB"; // Dot or X

    /// <summary>
    /// Initializes a new instance of the <see cref="TabViewModel"/> class.
    /// </summary>
    public TabViewModel(
        string filePath,
        PdfViewerViewModel viewerViewModel,
        ILogger<TabViewModel> logger)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ViewerViewModel = viewerViewModel ?? throw new ArgumentNullException(nameof(viewerViewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _title = Path.GetFileName(filePath);

        // Subscribe to changes in the viewer's unsaved changes
        ViewerViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfViewerViewModel.HasUnsavedChanges))
            {
                OnPropertyChanged(nameof(HasUnsavedChanges));
                OnPropertyChanged(nameof(CloseIcon));
            }
        };

        _logger.LogInformation("TabViewModel created for: {FilePath}", filePath);
    }

    /// <summary>
    /// Activates this tab.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        _logger.LogDebug("Tab activated: {FilePath}", FilePath);
    }

    /// <summary>
    /// Deactivates this tab.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        _logger.LogDebug("Tab deactivated: {FilePath}", FilePath);
    }

    /// <summary>
    /// Closes this tab.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _logger.LogInformation("Close command invoked for tab: {FilePath}", FilePath);
        // The actual closing is handled by MainViewModel
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

        _logger.LogInformation("Disposing TabViewModel: {FilePath}", FilePath);

        ViewerViewModel.Dispose();

        _disposed = true;
    }
}
