using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// View model for a recent file card on the empty state screen.
/// </summary>
public partial class RecentFileCardViewModel : ObservableObject
{
    public string FilePath { get; }
    public string FileName { get; }
    public string DirectoryPath { get; }
    public string LastAccessedText { get; }

    [ObservableProperty]
    private string? _thumbnailPath;

    [ObservableProperty]
    private bool _isThumbnailLoading = true;

    public RecentFileCardViewModel(string filePath, DateTime lastAccessed)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        DirectoryPath = Path.GetDirectoryName(filePath) ?? "";
        LastAccessedText = FormatLastAccessed(lastAccessed);
    }

    private static string FormatLastAccessed(DateTime lastAccessed)
    {
        var diff = DateTime.Now - lastAccessed;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return lastAccessed.ToString("MMM d, yyyy");
    }
}
