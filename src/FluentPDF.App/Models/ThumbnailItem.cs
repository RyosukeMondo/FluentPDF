using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentPDF.App.Models;

/// <summary>
/// Represents a thumbnail item for a PDF page in the thumbnails sidebar.
/// </summary>
public partial class ThumbnailItem : ObservableObject
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets or sets the thumbnail image for the page.
    /// </summary>
    [ObservableProperty]
    private BitmapImage? _thumbnail;

    /// <summary>
    /// Gets or sets a value indicating whether the thumbnail is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether this thumbnail is currently selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailItem"/> class.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    public ThumbnailItem(int pageNumber)
    {
        PageNumber = pageNumber;
        _isLoading = true;
        _isSelected = false;
    }
}
