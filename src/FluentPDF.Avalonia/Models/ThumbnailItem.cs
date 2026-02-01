using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPDF.Avalonia.Models;

/// <summary>
/// Represents a thumbnail item for the thumbnails sidebar.
/// </summary>
public partial class ThumbnailItem : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailItem"/> class.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    public ThumbnailItem(int pageNumber)
    {
        _pageNumber = pageNumber;
        _label = $"Page {pageNumber}";
    }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _pageNumber;

    /// <summary>
    /// Gets or sets the thumbnail image.
    /// </summary>
    [ObservableProperty]
    private Bitmap? _thumbnail;

    /// <summary>
    /// Gets or sets whether this thumbnail is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets whether this thumbnail is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the label text for the thumbnail.
    /// </summary>
    [ObservableProperty]
    private string _label = string.Empty;

    /// <summary>
    /// Gets the automation ID for UI testing.
    /// </summary>
    public string AutomationId => $"Thumbnail_Page{PageNumber}";
}
