using System.Drawing;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Service for creating and applying stamp annotations to PDF documents.
/// Supports predefined stamps (Approved, Draft, etc.) and custom image stamps.
/// Stamps can include dynamic text replacement ({{DATE}}, {{TIME}}).
/// </summary>
public sealed class StampService : IStampService
{
    private readonly ILogger<StampService> _logger;
    private const float DefaultDpi = 96f;

    /// <summary>
    /// Initializes a new instance of the <see cref="StampService"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured logging.</param>
    public StampService(ILogger<StampService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<string>> GenerateStampImageAsync(Stamp stamp, string outputPath)
    {
        if (stamp == null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Generating stamp image. CorrelationId={CorrelationId}, StampType={StampType}, OutputPath={OutputPath}",
            correlationId, stamp.Type, outputPath);

        return await Task.Run(() =>
        {
            try
            {
                // Handle custom image stamps
                if (stamp.Type == StampType.Custom && !string.IsNullOrEmpty(stamp.CustomImagePath))
                {
                    if (!File.Exists(stamp.CustomImagePath))
                    {
                        return Result.Fail<string>(CreateError(
                            "STAMP_IMAGE_NOT_FOUND",
                            $"Custom stamp image not found: {stamp.CustomImagePath}",
                            outputPath,
                            correlationId));
                    }

                    try
                    {
                        File.Copy(stamp.CustomImagePath, outputPath, overwrite: true);
                        _logger.LogInformation(
                            "Custom stamp image copied. CorrelationId={CorrelationId}, Path={Path}",
                            correlationId, outputPath);
                        return Result.Ok(outputPath);
                    }
                    catch (Exception ex)
                    {
                        return Result.Fail<string>(CreateError(
                            "STAMP_IMAGE_COPY_FAILED",
                            $"Failed to copy custom stamp image: {ex.Message}",
                            outputPath,
                            correlationId,
                            ex));
                    }
                }

                // Generate text-based stamp image
                var image = GenerateTextStampImage(stamp, correlationId);

                try
                {
                    // Create directory if it doesn't exist
                    var directory = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    image.SaveAsPng(outputPath);

                    _logger.LogInformation(
                        "Stamp image generated successfully. CorrelationId={CorrelationId}, Path={Path}, Size={Width}x{Height}",
                        correlationId, outputPath, image.Width, image.Height);

                    return Result.Ok(outputPath);
                }
                catch (Exception ex)
                {
                    return Result.Fail<string>(CreateError(
                        "STAMP_IMAGE_SAVE_FAILED",
                        $"Failed to save stamp image: {ex.Message}",
                        outputPath,
                        correlationId,
                        ex));
                }
                finally
                {
                    image.Dispose();
                }
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "STAMP_GENERATION_FAILED",
                    $"Failed to generate stamp image: {ex.Message}",
                    outputPath,
                    correlationId,
                    ex);

                _logger.LogError(ex, "Stamp generation failed. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail<string>(error);
            }
        });
    }

    /// <inheritdoc />
    public async Task<Result<Annotation>> ApplyStampAsync(
        PdfDocument document,
        Stamp stamp,
        int pageNumber,
        System.Drawing.PointF position)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (stamp == null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        var correlationId = Guid.NewGuid();
        _logger.LogInformation(
            "Applying stamp to document. CorrelationId={CorrelationId}, FilePath={FilePath}, StampType={StampType}, Page={Page}",
            correlationId, document.FilePath, stamp.Type, pageNumber);

        return await Task.Run(async () =>
        {
            try
            {
                // Generate temporary stamp image
                var tempStampPath = Path.Combine(
                    Path.GetTempPath(),
                    $"stamp_{stamp.Id}_{DateTime.UtcNow.Ticks}.png");

                var generateResult = await GenerateStampImageAsync(stamp, tempStampPath);
                if (!generateResult.IsSuccess)
                {
                    return Result.Fail<Annotation>(generateResult.Errors);
                }

                try
                {
                    // Create annotation for the stamp
                    var annotation = new Annotation
                    {
                        Type = AnnotationType.Stamp,
                        PageNumber = pageNumber,
                        Bounds = new PdfRectangle
                        {
                            Left = position.X,
                            Top = position.Y,
                            Right = position.X + stamp.Width,
                            Bottom = position.Y + stamp.Height
                        },
                        Contents = stamp.Text,
                        Opacity = stamp.Opacity,
                        Author = stamp.Author,
                        CreatedDate = stamp.CreatedDate,
                        ModifiedDate = DateTime.UtcNow,
                        FillColor = stamp.BackgroundColor,
                        StrokeColor = stamp.BorderColor
                    };

                    _logger.LogInformation(
                        "Stamp annotation created. CorrelationId={CorrelationId}, Id={Id}, Page={Page}",
                        correlationId, annotation.Id, pageNumber);

                    return Result.Ok(annotation);
                }
                finally
                {
                    // Clean up temporary file
                    try
                    {
                        if (File.Exists(tempStampPath))
                        {
                            File.Delete(tempStampPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to clean up temporary stamp image. Path={Path}",
                            tempStampPath);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = CreateError(
                    "STAMP_APPLY_FAILED",
                    $"Failed to apply stamp: {ex.Message}",
                    document.FilePath,
                    correlationId,
                    ex);

                _logger.LogError(ex, "Stamp application failed. CorrelationId={CorrelationId}", correlationId);
                return Result.Fail<Annotation>(error);
            }
        });
    }

    /// <inheritdoc />
    public Stamp CreateStamp(StampType type, string? customImagePath = null)
    {
        var preset = StampPresets.GetPreset(type);

        var stamp = new Stamp
        {
            Type = type,
            Text = preset.Text,
            BackgroundColor = preset.BackgroundColor,
            TextColor = preset.TextColor,
            BorderColor = preset.BorderColor,
            CustomImagePath = customImagePath
        };

        _logger.LogInformation(
            "Stamp created. Type={Type}, Text={Text}",
            type, stamp.Text);

        return stamp;
    }

    /// <inheritdoc />
    public void ApplyDynamicReplacements(Stamp stamp)
    {
        if (stamp == null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        var now = DateTime.Now;

        stamp.DynamicReplacements["{{DATE}}"] = now.ToString("yyyy-MM-dd");
        stamp.DynamicReplacements["{{TIME}}"] = now.ToString("HH:mm:ss");
        stamp.DynamicReplacements["{{DATETIME}}"] = now.ToString("yyyy-MM-dd HH:mm:ss");
        stamp.DynamicReplacements["{{USER}}"] = Environment.UserName;
        stamp.DynamicReplacements["{{YEAR}}"] = now.Year.ToString();
        stamp.DynamicReplacements["{{MONTH}}"] = now.ToString("MMMM");
        stamp.DynamicReplacements["{{DAY}}"] = now.Day.ToString();

        // Apply replacements to text
        var text = stamp.Text;
        foreach (var (placeholder, value) in stamp.DynamicReplacements)
        {
            text = text.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }

        stamp.Text = text;

        _logger.LogInformation(
            "Dynamic replacements applied to stamp. Id={Id}, ReplacementCount={Count}",
            stamp.Id, stamp.DynamicReplacements.Count);
    }

    private Image<Rgba32> GenerateTextStampImage(Stamp stamp, Guid correlationId)
    {
        // Convert PDF points to pixels (assuming 96 DPI)
        var widthPx = (int)(stamp.Width * DefaultDpi / 72f);
        var heightPx = (int)(stamp.Height * DefaultDpi / 72f);

        // Ensure minimum size
        widthPx = Math.Max(widthPx, 80);
        heightPx = Math.Max(heightPx, 40);

        _logger.LogDebug(
            "Creating text stamp image. CorrelationId={CorrelationId}, Size={Width}x{Height}px",
            correlationId, widthPx, heightPx);

        // Create image with background color
        var backgroundColor = new Rgba32(
            stamp.BackgroundColor.R,
            stamp.BackgroundColor.G,
            stamp.BackgroundColor.B,
            (byte)(stamp.BackgroundColor.A * stamp.Opacity));

        var image = new Image<Rgba32>(widthPx, heightPx, backgroundColor);

        // Note: ImageSharp doesn't have built-in text rendering and drawing in the core library
        // For now, we'll focus on the background color
        // In production, you would use a font library like SixLabors.Fonts with SixLabors.ImageSharp.Drawing

        return image;
    }

    private static FluentResults.Error CreateError(
        string code,
        string message,
        string context,
        Guid correlationId,
        Exception? exception = null)
    {
        return new FluentResults.Error($"{code}: {message}");
    }
}
