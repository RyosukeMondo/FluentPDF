using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.App.Services;
using FluentPDF.App.Views;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.Storage.Pickers;

namespace FluentPDF.App.ViewModels;

/// <summary>
/// ViewModel for DOCX to PDF conversion page.
/// Provides commands for file selection, conversion orchestration, and progress tracking.
/// Implements MVVM pattern with CommunityToolkit source generators.
/// </summary>
public partial class ConversionViewModel : ObservableObject
{
    private readonly IDocxConverterService _converterService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ConversionViewModel> _logger;
    private CancellationTokenSource? _conversionCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionViewModel"/> class.
    /// </summary>
    /// <param name="converterService">Service for DOCX to PDF conversion operations.</param>
    /// <param name="navigationService">Service for page navigation.</param>
    /// <param name="logger">Logger for tracking operations.</param>
    public ConversionViewModel(
        IDocxConverterService converterService,
        INavigationService navigationService,
        ILogger<ConversionViewModel> logger)
    {
        _converterService = converterService ?? throw new ArgumentNullException(nameof(converterService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("ConversionViewModel initialized");
    }

    /// <summary>
    /// Gets or sets the full path to the selected DOCX input file.
    /// </summary>
    [ObservableProperty]
    private string? _docxFilePath;

    /// <summary>
    /// Gets or sets the full path where the PDF output will be saved.
    /// </summary>
    [ObservableProperty]
    private string? _outputFilePath;

    /// <summary>
    /// Gets or sets the current status message displayed to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Select a DOCX file to begin conversion";

    /// <summary>
    /// Gets or sets a value indicating whether a conversion operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isConverting;

    /// <summary>
    /// Gets or sets the conversion progress percentage (0-100).
    /// </summary>
    [ObservableProperty]
    private double _conversionProgress;

    /// <summary>
    /// Gets or sets a value indicating whether quality validation is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _enableQualityValidation;

    /// <summary>
    /// Gets or sets the conversion result after successful conversion.
    /// </summary>
    [ObservableProperty]
    private ConversionResult? _result;

    /// <summary>
    /// Gets or sets a value indicating whether conversion results are visible.
    /// </summary>
    [ObservableProperty]
    private bool _hasResults;

    /// <summary>
    /// Opens a file picker dialog to select the source DOCX file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectDocxFile))]
    private async Task SelectDocxFileAsync()
    {
        _logger.LogInformation("SelectDocxFile command invoked");

        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".docx");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                DocxFilePath = file.Path;
                StatusMessage = "DOCX file selected. Choose output location.";
                _logger.LogInformation("DOCX file selected: {FilePath}", DocxFilePath);

                // Suggest output path based on input file
                SuggestOutputPath();
            }
            else
            {
                _logger.LogInformation("DOCX file selection cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting DOCX file");
            StatusMessage = "Error selecting file";
            await ShowErrorDialogAsync("File Selection Error", $"Failed to select file: {ex.Message}");
        }
    }

    private bool CanSelectDocxFile() => !IsConverting;

    /// <summary>
    /// Opens a file save picker dialog to choose the output PDF location.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectOutputPath))]
    private async Task SelectOutputPathAsync()
    {
        _logger.LogInformation("SelectOutputPath command invoked");

        try
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Suggest file name based on input file
            if (!string.IsNullOrEmpty(DocxFilePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(DocxFilePath);
                picker.SuggestedFileName = $"{fileName}.pdf";
            }
            else
            {
                picker.SuggestedFileName = "converted.pdf";
            }

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                OutputFilePath = file.Path;
                StatusMessage = "Output location selected. Ready to convert.";
                _logger.LogInformation("Output path selected: {FilePath}", OutputFilePath);
            }
            else
            {
                _logger.LogInformation("Output path selection cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting output path");
            StatusMessage = "Error selecting output location";
            await ShowErrorDialogAsync("Output Selection Error", $"Failed to select output location: {ex.Message}");
        }
    }

    private bool CanSelectOutputPath() => !IsConverting && !string.IsNullOrEmpty(DocxFilePath);

    /// <summary>
    /// Performs the DOCX to PDF conversion operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        _logger.LogInformation(
            "Convert command invoked. Input={InputPath}, Output={OutputPath}, QualityValidation={QualityValidation}",
            DocxFilePath, OutputFilePath, EnableQualityValidation);

        if (string.IsNullOrEmpty(DocxFilePath) || string.IsNullOrEmpty(OutputFilePath))
        {
            await ShowErrorDialogAsync("Conversion Error", "Please select both input and output files.");
            return;
        }

        IsConverting = true;
        ConversionProgress = 0;
        HasResults = false;
        Result = null;
        StatusMessage = "Starting conversion...";
        _conversionCts = new CancellationTokenSource();

        try
        {
            // Simulate progress updates (actual progress would need to be reported by service)
            var progressTimer = new System.Timers.Timer(500);
            progressTimer.Elapsed += (s, e) =>
            {
                if (ConversionProgress < 90 && IsConverting)
                {
                    ConversionProgress += 10;
                }
            };
            progressTimer.Start();

            var options = new ConversionOptions
            {
                EnableQualityValidation = EnableQualityValidation,
                Timeout = TimeSpan.FromSeconds(120),
                PreserveTempFiles = false
            };

            var result = await _converterService.ConvertDocxToPdfAsync(
                DocxFilePath,
                OutputFilePath,
                options,
                _conversionCts.Token);

            progressTimer.Stop();
            progressTimer.Dispose();
            ConversionProgress = 100;

            if (result.IsSuccess)
            {
                Result = result.Value;
                HasResults = true;
                StatusMessage = "Conversion completed successfully!";

                _logger.LogInformation(
                    "Conversion successful. Time={Time}s, OutputSize={Size}KB, QualityScore={Score}",
                    Result.ConversionTime.TotalSeconds,
                    Result.OutputSizeBytes / 1024.0,
                    Result.QualityScore);

                // Show success notification
                var message = BuildSuccessMessage(Result);
                await ShowSuccessDialogAsync("Conversion Complete", message);
            }
            else
            {
                _logger.LogError("Conversion failed: {Errors}", result.Errors);
                StatusMessage = "Conversion failed";

                var errorMessage = string.Join("\n", result.Errors.Select(e => e.Message));
                await ShowErrorDialogAsync("Conversion Failed", errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Conversion cancelled by user");
            StatusMessage = "Conversion cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during conversion");
            StatusMessage = "Unexpected error during conversion";
            await ShowErrorDialogAsync("Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsConverting = false;
            ConversionProgress = 0;
            _conversionCts?.Dispose();
            _conversionCts = null;
        }
    }

    private bool CanConvert() =>
        !IsConverting &&
        !string.IsNullOrEmpty(DocxFilePath) &&
        !string.IsNullOrEmpty(OutputFilePath);

    /// <summary>
    /// Opens the converted PDF file in the PDF viewer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenPdf))]
    private void OpenPdf()
    {
        _logger.LogInformation("OpenPdf command invoked");

        try
        {
            if (Result != null && File.Exists(Result.OutputPath))
            {
                _navigationService.NavigateTo(typeof(PdfViewerPage), Result.OutputPath);
                _logger.LogInformation("Navigated to PdfViewerPage with path: {Path}", Result.OutputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening PDF");
            StatusMessage = "Error opening PDF";
        }
    }

    private bool CanOpenPdf() => !IsConverting && Result != null && File.Exists(Result.OutputPath);

    /// <summary>
    /// Cancels the current conversion operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelConversion))]
    private void CancelConversion()
    {
        _logger.LogInformation("CancelConversion command invoked");
        _conversionCts?.Cancel();
        StatusMessage = "Cancelling conversion...";
    }

    private bool CanCancelConversion() => IsConverting && _conversionCts != null;

    /// <summary>
    /// Suggests an output path based on the input DOCX file path.
    /// </summary>
    private void SuggestOutputPath()
    {
        if (!string.IsNullOrEmpty(DocxFilePath))
        {
            var directory = Path.GetDirectoryName(DocxFilePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(DocxFilePath);
            OutputFilePath = Path.Combine(directory ?? string.Empty, $"{fileNameWithoutExt}.pdf");
        }
    }

    /// <summary>
    /// Builds a success message with conversion metrics.
    /// </summary>
    private static string BuildSuccessMessage(ConversionResult result)
    {
        var message = $"PDF successfully created!\n\n" +
                      $"Output: {Path.GetFileName(result.OutputPath)}\n" +
                      $"Size: {result.OutputSizeBytes / 1024.0:F1} KB\n" +
                      $"Time: {result.ConversionTime.TotalSeconds:F2}s";

        if (result.PageCount.HasValue)
        {
            message += $"\nPages: {result.PageCount.Value}";
        }

        if (result.QualityScore.HasValue)
        {
            message += $"\nQuality Score (SSIM): {result.QualityScore.Value:F3}";
        }

        return message;
    }

    /// <summary>
    /// Shows an error dialog to the user.
    /// </summary>
    private static async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a success dialog to the user.
    /// </summary>
    private static async Task ShowSuccessDialogAsync(string title, string message)
    {
        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Update command CanExecute states when relevant properties change
        if (e.PropertyName == nameof(IsConverting) ||
            e.PropertyName == nameof(DocxFilePath) ||
            e.PropertyName == nameof(OutputFilePath))
        {
            SelectDocxFileCommand.NotifyCanExecuteChanged();
            SelectOutputPathCommand.NotifyCanExecuteChanged();
            ConvertCommand.NotifyCanExecuteChanged();
            CancelConversionCommand.NotifyCanExecuteChanged();

            _logger.LogDebug(
                "Command states updated. Property={PropertyName}, IsConverting={IsConverting}",
                e.PropertyName, IsConverting);
        }

        if (e.PropertyName == nameof(Result))
        {
            OpenPdfCommand.NotifyCanExecuteChanged();
        }
    }
}
