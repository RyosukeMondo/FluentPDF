using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PointF = System.Drawing.PointF;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Handles Tools menu operations (watermark, stamp, export, security).
/// Extracted from MenuManager to keep file sizes manageable.
/// </summary>
internal sealed class ToolsMenuHandler
{
    private readonly Window _owner;
    private readonly Func<PdfViewerViewModel?> _getActiveViewer;
    private readonly ILogger? _logger;

    public ToolsMenuHandler(
        Window owner,
        Func<PdfViewerViewModel?> getActiveViewer,
        ILogger? logger)
    {
        _owner = owner;
        _getActiveViewer = getActiveViewer;
        _logger = logger;
    }

    public async Task OnWatermarkClickAsync()
    {
        try
        {
            var viewer = _getActiveViewer();
            if (viewer?.CurrentDocument == null) return;

            var dialogResult = await DialogHelper.ShowWatermarkDialogAsync(_owner);
            if (dialogResult == null) return;

            var position = dialogResult.PositionIndex switch
            {
                1 => WatermarkPosition.TopLeft,
                2 => WatermarkPosition.TopRight,
                3 => WatermarkPosition.BottomLeft,
                4 => WatermarkPosition.BottomRight,
                _ => WatermarkPosition.Center
            };

            var config = new TextWatermarkConfig
            {
                Text = dialogResult.Text,
                Opacity = dialogResult.Opacity,
                FontSize = dialogResult.FontSize,
                Position = position
            };

            var watermarkService = App.Services.GetRequiredService<IWatermarkService>();
            var result = await watermarkService.ApplyTextWatermarkAsync(
                viewer.CurrentDocument, config, WatermarkPageRange.All);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("Watermark applied: {Text}", config.Text);
                await viewer.RefreshCurrentPageAsync();
            }
            else
            {
                await DialogHelper.ShowErrorDialogAsync(
                    _owner, "Watermark Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply watermark");
            await DialogHelper.ShowErrorDialogAsync(_owner, "Error", ex.Message);
        }
    }

    public async Task OnStampClickAsync()
    {
        try
        {
            var viewer = _getActiveViewer();
            if (viewer?.CurrentDocument == null) return;

            var dialogResult = await DialogHelper.ShowStampDialogAsync(_owner);
            if (dialogResult == null) return;

            var stampType = (StampType)dialogResult.StampTypeIndex;
            var stampService = App.Services.GetRequiredService<IStampService>();
            var stamp = stampService.CreateStamp(stampType);
            stampService.ApplyDynamicReplacements(stamp);

            var pageIndex = viewer.CurrentPageIndex;
            var position = new PointF(200f, 400f);

            var result = await stampService.ApplyStampAsync(
                viewer.CurrentDocument, stamp, pageIndex, position);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("Stamp applied: {Type}", stampType);
                await viewer.RefreshCurrentPageAsync();
            }
            else
            {
                await DialogHelper.ShowErrorDialogAsync(
                    _owner, "Stamp Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply stamp");
            await DialogHelper.ShowErrorDialogAsync(_owner, "Error", ex.Message);
        }
    }

    public async Task OnExportImagesClickAsync()
    {
        try
        {
            var viewer = _getActiveViewer();
            if (viewer?.CurrentDocument == null) return;

            var folders = await _owner.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select Output Folder for Images",
                    AllowMultiple = false
                });

            if (folders.Count == 0) return;

            var outputDir = folders[0].Path.LocalPath;
            var exportService = App.Services.GetRequiredService<IImageExportService>();
            var options = new ImageExportOptions
            {
                Format = ImageFormat.Png,
                Dpi = 150,
                PageRange = "all",
                OverwriteExisting = true
            };

            viewer.StatusMessage = "Exporting pages as images...";
            viewer.IsOperationInProgress = true;

            var progress = new Progress<double>(p => viewer.OperationProgress = p);
            var result = await exportService.ExportAsync(
                viewer.CurrentDocument, outputDir, options, progress);

            viewer.IsOperationInProgress = false;

            if (result.IsSuccess)
            {
                var exportResult = result.Value;
                _logger?.LogInformation(
                    "Exported {Count} pages to {Dir}", exportResult.PageCount, outputDir);
                viewer.StatusMessage =
                    $"Exported {exportResult.PageCount} pages as PNG images";
            }
            else
            {
                await DialogHelper.ShowErrorDialogAsync(
                    _owner, "Export Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export images");
            await DialogHelper.ShowErrorDialogAsync(_owner, "Error", ex.Message);
        }
    }

    public async Task OnExportFdfClickAsync()
    {
        try
        {
            var viewer = _getActiveViewer();
            if (viewer?.CurrentDocument == null) return;

            var file = await _owner.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export Form Data (XFDF)",
                    DefaultExtension = "xfdf",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("XFDF Files")
                            { Patterns = new[] { "*.xfdf" } },
                        new FilePickerFileType("All Files")
                            { Patterns = new[] { "*.*" } }
                    }
                });

            if (file == null) return;

            var outputPath = file.Path.LocalPath;
            var fdfService = App.Services.GetRequiredService<IFdfService>();
            var result = await fdfService.ExportFormDataToXfdfAsync(
                viewer.CurrentDocument, outputPath);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("FDF exported to {Path}", outputPath);
                viewer.StatusMessage =
                    $"Form data exported to {Path.GetFileName(outputPath)}";
            }
            else
            {
                await DialogHelper.ShowErrorDialogAsync(
                    _owner, "FDF Export Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export FDF");
            await DialogHelper.ShowErrorDialogAsync(_owner, "Error", ex.Message);
        }
    }

    public async Task OnSecurityClickAsync()
    {
        try
        {
            var viewer = _getActiveViewer();
            if (viewer?.CurrentDocument == null) return;

            var dialogResult = await DialogHelper.ShowSecurityDialogAsync(_owner);
            if (dialogResult == null) return;

            var file = await _owner.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save Encrypted PDF",
                    DefaultExtension = "pdf",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Files")
                            { Patterns = new[] { "*.pdf" } }
                    }
                });

            if (file == null) return;

            var outputPath = file.Path.LocalPath;
            var permissions = PdfPermissions.None;
            if (dialogResult.AllowPrint) permissions |= PdfPermissions.Print;
            if (dialogResult.AllowCopy) permissions |= PdfPermissions.Copy;

            var settings = new EncryptionSettings
            {
                OwnerPassword = dialogResult.OwnerPassword,
                UserPassword = dialogResult.UserPassword,
                Strength = dialogResult.UseAes256
                    ? EncryptionStrength.Aes256
                    : EncryptionStrength.Aes128,
                Permissions = permissions
            };

            var securityService = App.Services.GetRequiredService<ISecurityService>();
            var inputPath = viewer.CurrentDocument.FilePath;
            var result = await securityService.EncryptDocumentAsync(
                inputPath, outputPath, settings);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("Document encrypted to {Path}", outputPath);
                viewer.StatusMessage =
                    $"Encrypted document saved to {Path.GetFileName(outputPath)}";
            }
            else
            {
                await DialogHelper.ShowErrorDialogAsync(
                    _owner, "Encryption Error", result.Errors[0].Message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to encrypt document");
            await DialogHelper.ShowErrorDialogAsync(_owner, "Error", ex.Message);
        }
    }
}
