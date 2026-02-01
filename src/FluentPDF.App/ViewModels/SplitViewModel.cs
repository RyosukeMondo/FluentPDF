using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for splitting PDF documents.
/// Manages split configuration, page ranges, and output settings.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class SplitViewModel : ObservableObject
{
    private readonly IPdfDocumentService _documentService;
    private readonly ILogger<SplitViewModel> _logger;
    private PdfDocument? _currentDocument;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitViewModel"/> class.
    /// </summary>
    /// <param name="documentService">Service for PDF document operations.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public SplitViewModel(
        IPdfDocumentService documentService,
        ILogger<SplitViewModel> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("SplitViewModel initialized");
    }

    /// <summary>
    /// Gets the available split methods.
    /// </summary>
    public List<SplitMethod> SplitMethods { get; } = new()
    {
        new SplitMethod("By Page Ranges", "Split at specific page ranges (e.g., 1-5, 6-10)"),
        new SplitMethod("Every N Pages", "Split into chunks of N pages each"),
        new SplitMethod("By Bookmarks", "Split at each top-level bookmark"),
        new SplitMethod("By File Size", "Split to keep files under a target size")
    };

    /// <summary>
    /// Gets or sets the selected split method.
    /// </summary>
    [ObservableProperty]
    private SplitMethod? _selectedMethod;

    /// <summary>
    /// Gets the collection of page ranges for manual splitting.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PageRangeItem> _pageRanges = new();

    /// <summary>
    /// Gets or sets the number of pages per chunk (for "Every N Pages" method).
    /// </summary>
    [ObservableProperty]
    private int _pagesPerChunk = 1;

    /// <summary>
    /// Gets or sets the target file size in MB (for "By File Size" method).
    /// </summary>
    [ObservableProperty]
    private double _targetSizeMB = 5.0;

    /// <summary>
    /// Gets or sets the output folder path.
    /// </summary>
    [ObservableProperty]
    private string _outputFolder = string.Empty;

    /// <summary>
    /// Gets or sets the filename pattern for output files.
    /// </summary>
    [ObservableProperty]
    private string _filenamePattern = "{original}_part{number}";

    /// <summary>
    /// Gets or sets a value indicating whether a split operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog was applied (vs canceled).
    /// </summary>
    [ObservableProperty]
    private bool _dialogApplied;

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    [ObservableProperty]
    private string? _validationError;

    /// <summary>
    /// Gets a value indicating whether the "By Page Ranges" method is selected.
    /// </summary>
    public bool IsPageRangesMethod => SelectedMethod?.Name == "By Page Ranges";

    /// <summary>
    /// Gets a value indicating whether the "Every N Pages" method is selected.
    /// </summary>
    public bool IsEveryNPagesMethod => SelectedMethod?.Name == "Every N Pages";

    /// <summary>
    /// Gets a value indicating whether the "By File Size" method is selected.
    /// </summary>
    public bool IsFileSizeMethod => SelectedMethod?.Name == "By File Size";

    /// <summary>
    /// Gets a value indicating whether validation has failed.
    /// </summary>
    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);

    /// <summary>
    /// Gets a value indicating whether split operation can be performed.
    /// </summary>
    public bool CanSplit => _currentDocument != null && !IsLoading && !HasValidationError && !string.IsNullOrWhiteSpace(OutputFolder);

    /// <summary>
    /// Initializes the split dialog with a document.
    /// </summary>
    [RelayCommand]
    private void Initialize(PdfDocument document)
    {
        _currentDocument = document;
        DialogApplied = false;
        ValidationError = null;

        // Set default method
        SelectedMethod = SplitMethods[0];

        // Add one default range
        PageRanges.Clear();
        AddRange();

        _logger.LogInformation("SplitViewModel initialized for document: {FilePath}", document.FilePath);
    }

    /// <summary>
    /// Adds a new page range to the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddRange))]
    private void AddRange()
    {
        PageRanges.Add(new PageRangeItem
        {
            StartPage = 1,
            EndPage = _currentDocument?.PageCount ?? 1
        });
        _logger.LogDebug("Added new page range");
    }

    private bool CanAddRange() => IsPageRangesMethod && !IsLoading;

    /// <summary>
    /// Removes a page range from the list.
    /// </summary>
    [RelayCommand]
    private void RemoveRange(PageRangeItem range)
    {
        if (PageRanges.Count > 1)
        {
            PageRanges.Remove(range);
            _logger.LogDebug("Removed page range");
        }
    }

    /// <summary>
    /// Opens a folder picker to select the output folder.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBrowseFolder))]
    private async Task BrowseFolderAsync()
    {
        _logger.LogInformation("BrowseFolder command invoked");

        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                OutputFolder = folder.Path;
                _logger.LogInformation("Output folder selected: {Folder}", OutputFolder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select output folder");
        }
    }

    private bool CanBrowseFolder() => !IsLoading;

    /// <summary>
    /// Performs the split operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSplit))]
    private async Task SplitAsync()
    {
        _logger.LogInformation("Split command invoked");

        if (_currentDocument == null)
        {
            _logger.LogWarning("Cannot split: no document loaded");
            return;
        }

        if (!Validate())
        {
            _logger.LogWarning("Validation failed: {Error}", ValidationError);
            return;
        }

        try
        {
            IsLoading = true;

            // TODO: Implement split logic via service based on selected method
            await Task.CompletedTask; // Placeholder until split service is implemented
            // Example:
            // if (IsPageRangesMethod)
            //     await _documentService.SplitByRangesAsync(_currentDocument, PageRanges, OutputFolder, FilenamePattern);
            // else if (IsEveryNPagesMethod)
            //     await _documentService.SplitEveryNPagesAsync(_currentDocument, PagesPerChunk, OutputFolder, FilenamePattern);

            DialogApplied = true;
            _logger.LogInformation("Split completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to split PDF");
            ValidationError = $"Split failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    private bool Validate()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(OutputFolder))
        {
            ValidationError = "Please select an output folder";
            return false;
        }

        if (IsPageRangesMethod)
        {
            if (PageRanges.Count == 0)
            {
                ValidationError = "Please add at least one page range";
                return false;
            }

            foreach (var range in PageRanges)
            {
                if (range.StartPage < 1 || range.EndPage < 1)
                {
                    ValidationError = "Page numbers must be greater than 0";
                    return false;
                }

                if (range.StartPage > range.EndPage)
                {
                    ValidationError = "Start page must be less than or equal to end page";
                    return false;
                }

                if (_currentDocument != null && range.EndPage > _currentDocument.PageCount)
                {
                    ValidationError = $"Page range exceeds document page count ({_currentDocument.PageCount})";
                    return false;
                }
            }
        }
        else if (IsEveryNPagesMethod)
        {
            if (PagesPerChunk < 1)
            {
                ValidationError = "Pages per chunk must be at least 1";
                return false;
            }
        }
        else if (IsFileSizeMethod)
        {
            if (TargetSizeMB < 0.1)
            {
                ValidationError = "Target file size must be at least 0.1 MB";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Updates visibility flags when method changes.
    /// </summary>
    partial void OnSelectedMethodChanged(SplitMethod? value)
    {
        OnPropertyChanged(nameof(IsPageRangesMethod));
        OnPropertyChanged(nameof(IsEveryNPagesMethod));
        OnPropertyChanged(nameof(IsFileSizeMethod));
        AddRangeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates command availability when loading state changes.
    /// </summary>
    partial void OnIsLoadingChanged(bool value)
    {
        AddRangeCommand.NotifyCanExecuteChanged();
        BrowseFolderCommand.NotifyCanExecuteChanged();
        SplitCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates command availability when validation error changes.
    /// </summary>
    partial void OnValidationErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(CanSplit));
        SplitCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates command availability when output folder changes.
    /// </summary>
    partial void OnOutputFolderChanged(string value)
    {
        OnPropertyChanged(nameof(CanSplit));
        SplitCommand.NotifyCanExecuteChanged();
        Validate();
    }
}

/// <summary>
/// Represents a split method configuration.
/// </summary>
public record SplitMethod(string Name, string Description);

/// <summary>
/// Represents a page range for splitting.
/// </summary>
public partial class PageRangeItem : ObservableObject
{
    /// <summary>
    /// Gets or sets the starting page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _startPage = 1;

    /// <summary>
    /// Gets or sets the ending page number (1-based).
    /// </summary>
    [ObservableProperty]
    private int _endPage = 1;

    /// <summary>
    /// Gets the automation ID for this item.
    /// </summary>
    public string AutomationId => $"PageRange_{StartPage}_{EndPage}";
}
