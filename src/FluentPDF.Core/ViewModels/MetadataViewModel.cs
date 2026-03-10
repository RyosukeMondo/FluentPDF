using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// ViewModel for displaying PDF document metadata in the sidebar panel.
/// </summary>
public partial class MetadataViewModel : ViewModelBase
{
    private readonly ILogger<MetadataViewModel> _logger;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _keywords = string.Empty;

    [ObservableProperty]
    private string _creator = string.Empty;

    [ObservableProperty]
    private string _producer = string.Empty;

    [ObservableProperty]
    private string _creationDate = string.Empty;

    [ObservableProperty]
    private string _modificationDate = string.Empty;

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private string _fileSize = string.Empty;

    [ObservableProperty]
    private string _pdfVersion = string.Empty;

    [ObservableProperty]
    private bool _isEncrypted;

    [ObservableProperty]
    private string _permissions = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _hasMetadata;

    /// <summary>
    /// Callback to read metadata from the native PDF handle.
    /// Signature: (PdfDocument) => MetadataResult
    /// Set by the UI layer since Core cannot reference Rendering/PDFium.
    /// </summary>
    public Func<PdfDocument, DocumentMetadataResult?>? ReadMetadataCallback { get; set; }

    public MetadataViewModel(ILogger<MetadataViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("MetadataViewModel initialized");
    }

    /// <summary>
    /// Updates all metadata properties from a PDF document.
    /// </summary>
    public void UpdateFromDocument(PdfDocument? document)
    {
        if (document == null)
        {
            ClearMetadata();
            return;
        }

        _logger.LogInformation("Updating metadata from document: {FilePath}", document.FilePath);

        FileName = Path.GetFileName(document.FilePath);
        FilePath = document.FilePath;
        PageCount = document.PageCount;
        FileSize = FormatFileSize(document.FileSizeBytes);

        if (ReadMetadataCallback != null)
        {
            try
            {
                var result = ReadMetadataCallback(document);
                if (result != null)
                {
                    Title = result.Title ?? string.Empty;
                    Author = result.Author ?? string.Empty;
                    Subject = result.Subject ?? string.Empty;
                    Keywords = result.Keywords ?? string.Empty;
                    Creator = result.Creator ?? string.Empty;
                    Producer = result.Producer ?? string.Empty;
                    CreationDate = FormatPdfDate(result.CreationDate);
                    ModificationDate = FormatPdfDate(result.ModificationDate);
                    PdfVersion = result.PdfVersion ?? string.Empty;
                    IsEncrypted = result.IsEncrypted;
                    Permissions = result.Permissions ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read metadata from document");
            }
        }

        HasMetadata = true;
        _logger.LogInformation("Metadata updated: Title={Title}, Author={Author}, Pages={Pages}",
            Title, Author, PageCount);
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    private void ClearMetadata()
    {
        Title = string.Empty;
        Author = string.Empty;
        Subject = string.Empty;
        Keywords = string.Empty;
        Creator = string.Empty;
        Producer = string.Empty;
        CreationDate = string.Empty;
        ModificationDate = string.Empty;
        PageCount = 0;
        FileSize = string.Empty;
        PdfVersion = string.Empty;
        IsEncrypted = false;
        Permissions = string.Empty;
        FileName = string.Empty;
        FilePath = string.Empty;
        HasMetadata = false;
    }

    /// <summary>
    /// Formats a byte count into a human-readable file size string.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";

        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{size:F0} {units[unitIndex]}"
            : $"{size:F1} {units[unitIndex]}";
    }

    /// <summary>
    /// Formats a PDF date string (D:YYYYMMDDHHmmSS) into a readable format.
    /// </summary>
    public static string FormatPdfDate(string? pdfDate)
    {
        if (string.IsNullOrWhiteSpace(pdfDate))
            return string.Empty;

        // PDF dates are in format: D:YYYYMMDDHHmmSSOHH'mm'
        var dateStr = pdfDate.StartsWith("D:") ? pdfDate[2..] : pdfDate;

        if (dateStr.Length >= 8 &&
            int.TryParse(dateStr[..4], out int year) &&
            int.TryParse(dateStr[4..6], out int month) &&
            int.TryParse(dateStr[6..8], out int day))
        {
            int hour = 0, minute = 0, second = 0;
            if (dateStr.Length >= 10) int.TryParse(dateStr[8..10], out hour);
            if (dateStr.Length >= 12) int.TryParse(dateStr[10..12], out minute);
            if (dateStr.Length >= 14) int.TryParse(dateStr[12..14], out second);

            try
            {
                var dt = new DateTime(year, month, day, hour, minute, second);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return pdfDate;
            }
        }

        return pdfDate;
    }
}

/// <summary>
/// Result object for metadata read from native PDF handle.
/// </summary>
public sealed class DocumentMetadataResult
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Keywords { get; init; }
    public string? Creator { get; init; }
    public string? Producer { get; init; }
    public string? CreationDate { get; init; }
    public string? ModificationDate { get; init; }
    public string? PdfVersion { get; init; }
    public bool IsEncrypted { get; init; }
    public string? Permissions { get; init; }
}
