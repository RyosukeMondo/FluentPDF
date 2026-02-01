using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Avalonia.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.ViewModels;

/// <summary>
/// ViewModel for merging multiple PDF documents.
/// Manages file list, reordering, and merge operation configuration.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class MergeViewModel : ObservableObject
{
    private readonly ILogger<MergeViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking operations.</param>
    public MergeViewModel(ILogger<MergeViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("MergeViewModel initialized");
    }

    /// <summary>
    /// Gets the collection of files to merge.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MergeFileItem> _files = new();

    /// <summary>
    /// Gets or sets the selected file in the list.
    /// </summary>
    [ObservableProperty]
    private MergeFileItem? _selectedFile;

    /// <summary>
    /// Gets or sets the output filename.
    /// </summary>
    [ObservableProperty]
    private string _outputFileName = "merged.pdf";

    /// <summary>
    /// Gets or sets a value indicating whether a merge operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog was applied (vs canceled).
    /// </summary>
    [ObservableProperty]
    private bool _dialogApplied;

    /// <summary>
    /// Gets the total number of pages across all files.
    /// </summary>
    public int TotalPages => Files.Sum(f => f.PageCount);

    /// <summary>
    /// Gets the total size of all files in MB.
    /// </summary>
    public double TotalSizeMB => Files.Sum(f => f.FileSizeBytes) / 1024.0 / 1024.0;

    /// <summary>
    /// Gets text representation of total pages.
    /// </summary>
    public string TotalPagesText => $"{TotalPages:N0} pages";

    /// <summary>
    /// Gets text representation of total size.
    /// </summary>
    public string TotalSizeText => $"{TotalSizeMB:F2} MB";

    /// <summary>
    /// Gets a value indicating whether files can be merged.
    /// </summary>
    public bool CanMerge => Files.Count >= 2 && !IsLoading;

    /// <summary>
    /// Opens a file picker to add PDF files to the merge list.
    /// TODO: Implement IFileDialogService for Avalonia
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddFiles))]
    private async Task AddFilesAsync()
    {
        _logger.LogInformation("AddFiles command invoked - file dialog not yet implemented");
        await Task.CompletedTask;

        // TODO: Replace with IFileDialogService
        // var filePaths = await _fileDialogService.OpenFilesAsync(new FileDialogOptions
        // {
        //     AllowMultiple = true,
        //     Filters = new[] { new FileDialogFilter { Name = "PDF Files", Extensions = { "pdf" } } }
        // });
        // foreach (var path in filePaths)
        // {
        //     await AddFileAsync(path);
        // }
        // UpdateTotals();
    }

    private bool CanAddFiles() => !IsLoading;

    /// <summary>
    /// Opens a folder picker to add all PDFs from a folder.
    /// TODO: Implement IFileDialogService for Avalonia
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddFolder))]
    private async Task AddFolderAsync()
    {
        _logger.LogInformation("AddFolder command invoked - folder dialog not yet implemented");
        await Task.CompletedTask;

        // TODO: Replace with IFileDialogService
        // var folderPath = await _fileDialogService.OpenFolderAsync();
        // if (folderPath != null)
        // {
        //     var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
        //     foreach (var file in pdfFiles)
        //     {
        //         await AddFileAsync(file);
        //     }
        //     UpdateTotals();
        // }
    }

    private bool CanAddFolder() => !IsLoading;

    /// <summary>
    /// Removes the selected file from the merge list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedFile == null)
        {
            return;
        }

        _logger.LogInformation("Removing file: {FileName}", SelectedFile.FileName);
        Files.Remove(SelectedFile);
        SelectedFile = null;
        UpdateTotals();
    }

    private bool CanRemove() => SelectedFile != null && !IsLoading;

    /// <summary>
    /// Moves the selected file up in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedFile == null)
        {
            return;
        }

        var index = Files.IndexOf(SelectedFile);
        if (index > 0)
        {
            Files.Move(index, index - 1);
            _logger.LogDebug("Moved file up: {FileName}", SelectedFile.FileName);
        }
    }

    private bool CanMoveUp() => SelectedFile != null && Files.IndexOf(SelectedFile) > 0 && !IsLoading;

    /// <summary>
    /// Moves the selected file down in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedFile == null)
        {
            return;
        }

        var index = Files.IndexOf(SelectedFile);
        if (index < Files.Count - 1)
        {
            Files.Move(index, index + 1);
            _logger.LogDebug("Moved file down: {FileName}", SelectedFile.FileName);
        }
    }

    private bool CanMoveDown() => SelectedFile != null && Files.IndexOf(SelectedFile) < Files.Count - 1 && !IsLoading;

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMerge))]
    private async Task MergeAsync()
    {
        _logger.LogInformation("Merge command invoked");

        if (Files.Count < 2)
        {
            _logger.LogWarning("Cannot merge: need at least 2 files");
            return;
        }

        try
        {
            IsLoading = true;

            var fileDialogService = App.GetService<IFileDialogService>();

            var filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "PDF Documents",
                    Extensions = new List<string> { "pdf" }
                }
            };

            var outputPath = await fileDialogService.SaveFileAsync(
                "Save Merged PDF",
                OutputFileName,
                filters);

            if (!string.IsNullOrEmpty(outputPath))
            {
                // TODO: Implement merge logic via service
                // var result = await _documentEditingService.MergeDocumentsAsync(
                //     Files.Select(f => f.FilePath).ToArray(),
                //     outputPath,
                //     null,
                //     CancellationToken.None);

                DialogApplied = true;
                _logger.LogInformation("Merge completed successfully: {OutputPath}", outputPath);
            }
            else
            {
                _logger.LogInformation("Merge cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge PDFs");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Adds a file to the merge list and loads its metadata.
    /// </summary>
    private async Task AddFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            // TODO: Load actual page count from PDF
            // For now, use a placeholder
            var pageCount = 1;

            var item = new MergeFileItem
            {
                FileName = fileInfo.Name,
                FilePath = filePath,
                PageCount = pageCount,
                FileSizeBytes = fileInfo.Length
            };

            Files.Add(item);
            _logger.LogInformation("Added file: {FileName} ({PageCount} pages, {Size} bytes)",
                item.FileName, item.PageCount, item.FileSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add file: {FilePath}", filePath);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates computed totals and notifies UI.
    /// </summary>
    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(TotalSizeMB));
        OnPropertyChanged(nameof(TotalPagesText));
        OnPropertyChanged(nameof(TotalSizeText));
        OnPropertyChanged(nameof(CanMerge));
        MergeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates command availability when selected file changes.
    /// </summary>
    partial void OnSelectedFileChanged(MergeFileItem? value)
    {
        RemoveCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates command availability when loading state changes.
    /// </summary>
    partial void OnIsLoadingChanged(bool value)
    {
        AddFilesCommand.NotifyCanExecuteChanged();
        AddFolderCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        MergeCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Represents a file in the merge list with its metadata.
/// </summary>
public partial class MergeFileItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// Gets or sets the number of pages in the PDF.
    /// </summary>
    [ObservableProperty]
    private int _pageCount;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [ObservableProperty]
    private long _fileSizeBytes;

    /// <summary>
    /// Gets the file size formatted as text.
    /// </summary>
    public string FileSizeText
    {
        get
        {
            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
        }
    }

    /// <summary>
    /// Gets the automation ID for this item.
    /// </summary>
    public string AutomationId => $"MergeFileItem_{FileName}";
}
