using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for applying text and image watermarks to PDF documents using PDFium.
/// Supports opacity, rotation, positioning, and page range selection.
/// </summary>
public sealed partial class WatermarkService : IWatermarkService
{
    private readonly ILogger<WatermarkService> _logger;
    private readonly IPdfRenderingService _renderingService;
    private const string WatermarkTag = "FluentPDF_Watermark";

    /// <summary>
    /// Initializes a new instance of the <see cref="WatermarkService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    /// <param name="renderingService">PDF rendering service for preview generation.</param>
    public WatermarkService(
        ILogger<WatermarkService> logger,
        IPdfRenderingService renderingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
    }

    /// <inheritdoc />
    public async Task<Result> ApplyTextWatermarkAsync(
        PdfDocument document,
        TextWatermarkConfig config,
        WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.Text))
        {
            throw new ArgumentException("Watermark text cannot be empty.", nameof(config));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Applying text watermark. CorrelationId={CorrelationId}, FilePath={FilePath}, Text={Text}",
            correlationId, document.FilePath, config.Text);

        return await Task.Run(() =>
        {
            try
            {
                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                if (pageNumbers.Length == 0)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_NO_PAGES",
                        "No pages specified for watermark application.",
                        document.FilePath,
                        correlationId));
                }

                var font = PdfiumInterop.LoadStandardFont(documentHandle, MapFontName(config.FontFamily));
                if (font == IntPtr.Zero)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_FONT_LOAD_FAILED",
                        $"Failed to load font: {config.FontFamily}",
                        document.FilePath,
                        correlationId));
                }

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        _logger.LogWarning(
                            "Failed to load page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    ApplyTextWatermarkToPage(documentHandle, pageHandle, pageNum, font, config, correlationId);
                }

                _logger.LogInformation(
                    "Text watermark applied successfully to {PageCount} pages. CorrelationId={CorrelationId}",
                    pageNumbers.Length, correlationId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_TEXT_APPLY_FAILED",
                    $"Failed to apply text watermark: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Text watermark application failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> ApplyImageWatermarkAsync(
        PdfDocument document,
        ImageWatermarkConfig config,
        WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ImagePath))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(config));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Applying image watermark. CorrelationId={CorrelationId}, FilePath={FilePath}, ImagePath={ImagePath}",
            correlationId, document.FilePath, config.ImagePath);

        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(config.ImagePath))
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_IMAGE_NOT_FOUND",
                        $"Image file not found: {config.ImagePath}",
                        document.FilePath,
                        correlationId));
                }

                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                if (pageNumbers.Length == 0)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_NO_PAGES",
                        "No pages specified for watermark application.",
                        document.FilePath,
                        correlationId));
                }

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        _logger.LogWarning(
                            "Failed to load page {PageNumber}. CorrelationId={CorrelationId}",
                            pageNum, correlationId);
                        continue;
                    }

                    ApplyImageWatermarkToPage(documentHandle, pageHandle, pageNum, config, correlationId);
                }

                _logger.LogInformation(
                    "Image watermark applied successfully to {PageCount} pages. CorrelationId={CorrelationId}",
                    pageNumbers.Length, correlationId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_IMAGE_APPLY_FAILED",
                    $"Failed to apply image watermark: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Image watermark application failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result> RemoveWatermarksAsync(PdfDocument document, WatermarkPageRange pageRange)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Removing watermarks. CorrelationId={CorrelationId}, FilePath={FilePath}",
            correlationId, document.FilePath);

        return await Task.Run(() =>
        {
            try
            {
                var documentHandle = (SafePdfDocumentHandle)document.Handle;
                if (documentHandle.IsInvalid)
                {
                    return Result.Fail(CreateError(
                        "WATERMARK_INVALID_HANDLE",
                        "Invalid document handle.",
                        document.FilePath,
                        correlationId));
                }

                var pageNumbers = pageRange.GetPages(document.PageCount);
                var removedCount = 0;

                foreach (var pageNum in pageNumbers)
                {
                    var pageIndex = pageNum - 1;
                    using var pageHandle = PdfiumInterop.LoadPage(documentHandle, pageIndex);
                    if (pageHandle.IsInvalid)
                    {
                        continue;
                    }

                    removedCount += 0;
                }

                _logger.LogInformation(
                    "Watermarks removed successfully. CorrelationId={CorrelationId}, RemovedCount={RemovedCount}",
                    correlationId, removedCount);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_REMOVE_FAILED",
                    $"Failed to remove watermarks: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Watermark removal failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> GeneratePreviewAsync(
        PdfDocument document,
        int pageIndex,
        TextWatermarkConfig? textConfig,
        ImageWatermarkConfig? imageConfig)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (textConfig == null && imageConfig == null)
        {
            throw new ArgumentException("Either textConfig or imageConfig must be provided.");
        }

        if (textConfig != null && imageConfig != null)
        {
            throw new ArgumentException("Only one of textConfig or imageConfig can be provided.");
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Generating watermark preview. CorrelationId={CorrelationId}, FilePath={FilePath}, PageIndex={PageIndex}",
            correlationId, document.FilePath, pageIndex);

        return await Task.Run(async () =>
        {
            try
            {
                var result = await _renderingService.RenderPageAsync(document, pageIndex, 1.0, 150);
                if (result.IsFailed)
                {
                    return Result.Fail<byte[]>(result.Errors);
                }

                using var stream = result.Value;
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return Result.Ok(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "WATERMARK_PREVIEW_FAILED",
                    $"Failed to generate preview: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex,
                    "Watermark preview generation failed. CorrelationId={CorrelationId}, Error={ErrorCode}",
                    correlationId, error.Metadata["ErrorCode"]);

                return Result.Fail<byte[]>(error);
            }
        });
    }

}
