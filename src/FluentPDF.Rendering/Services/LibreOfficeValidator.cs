using System.Diagnostics;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Validates PDF quality by comparing FluentPDF output against LibreOffice baseline using SSIM.
/// Gracefully handles LibreOffice not being installed by skipping validation.
/// </summary>
public sealed class LibreOfficeValidator : IQualityValidationService
{
    private readonly IPdfDocumentService _pdfDocumentService;
    private readonly IPdfRenderingService _pdfRenderingService;
    private readonly ILogger<LibreOfficeValidator> _logger;
    private const double DefaultDpi = 150.0; // Higher DPI for better comparison
    private const double DefaultZoomLevel = 1.0;

    public LibreOfficeValidator(
        IPdfDocumentService pdfDocumentService,
        IPdfRenderingService pdfRenderingService,
        ILogger<LibreOfficeValidator> logger)
    {
        _pdfDocumentService = pdfDocumentService ?? throw new ArgumentNullException(nameof(pdfDocumentService));
        _pdfRenderingService = pdfRenderingService ?? throw new ArgumentNullException(nameof(pdfRenderingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> IsLibreOfficeAvailableAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "soffice",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var isAvailable = process.ExitCode == 0 && output.Contains("LibreOffice", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation(
                "LibreOffice availability check completed. Available={Available}, Version={Version}",
                isAvailable, output.Trim());

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LibreOffice not found or not accessible");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Result<QualityReport?>> ValidateQualityAsync(
        string docxPath,
        string fluentPdfPath,
        double threshold = 0.95,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting quality validation. CorrelationId={CorrelationId}, DocxPath={DocxPath}, FluentPdfPath={FluentPdfPath}, Threshold={Threshold}",
            correlationId, docxPath, fluentPdfPath, threshold);

        // Check if LibreOffice is available
        if (!await IsLibreOfficeAvailableAsync())
        {
            _logger.LogWarning(
                "LibreOffice not available, skipping quality validation. CorrelationId={CorrelationId}",
                correlationId);
            return Result.Ok<QualityReport?>(null);
        }

        // Validate input files exist
        if (!File.Exists(docxPath))
        {
            var error = new PdfError(
                "VALIDATION_DOCX_NOT_FOUND",
                $"DOCX file not found: {docxPath}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("DocxPath", docxPath)
                .WithContext("CorrelationId", correlationId);

            return Result.Fail(error);
        }

        if (!File.Exists(fluentPdfPath))
        {
            var error = new PdfError(
                "VALIDATION_PDF_NOT_FOUND",
                $"FluentPDF output file not found: {fluentPdfPath}",
                ErrorCategory.Validation,
                ErrorSeverity.Error)
                .WithContext("FluentPdfPath", fluentPdfPath)
                .WithContext("CorrelationId", correlationId);

            return Result.Fail(error);
        }

        try
        {
            // Convert DOCX to PDF using LibreOffice
            var libreOfficePdfPath = await ConvertWithLibreOfficeAsync(docxPath, correlationId, cancellationToken);
            if (string.IsNullOrEmpty(libreOfficePdfPath))
            {
                var error = new PdfError(
                    "LIBREOFFICE_CONVERSION_FAILED",
                    "LibreOffice conversion failed to produce output PDF",
                    ErrorCategory.Conversion,
                    ErrorSeverity.Error)
                    .WithContext("DocxPath", docxPath)
                    .WithContext("CorrelationId", correlationId);

                return Result.Fail(error);
            }

            // Load both PDFs
            var fluentPdfResult = await _pdfDocumentService.LoadDocumentAsync(fluentPdfPath);
            if (fluentPdfResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to load FluentPDF document. CorrelationId={CorrelationId}, Error={Error}",
                    correlationId, fluentPdfResult.Errors.FirstOrDefault()?.Message);
                return Result.Fail(fluentPdfResult.Errors);
            }

            var librePdfResult = await _pdfDocumentService.LoadDocumentAsync(libreOfficePdfPath);
            if (librePdfResult.IsFailed)
            {
                _logger.LogError(
                    "Failed to load LibreOffice PDF. CorrelationId={CorrelationId}, Error={Error}",
                    correlationId, librePdfResult.Errors.FirstOrDefault()?.Message);
                fluentPdfResult.Value.Dispose();
                return Result.Fail(librePdfResult.Errors);
            }

            using var fluentPdf = fluentPdfResult.Value;
            using var librePdf = librePdfResult.Value;

            // Compare page counts
            var pageCount = Math.Min(fluentPdf.PageCount, librePdf.PageCount);
            if (fluentPdf.PageCount != librePdf.PageCount)
            {
                _logger.LogWarning(
                    "Page count mismatch. CorrelationId={CorrelationId}, FluentPages={FluentPages}, LibrePages={LibrePages}",
                    correlationId, fluentPdf.PageCount, librePdf.PageCount);
            }

            // Calculate SSIM for each page
            var ssimScores = new List<(int PageNumber, double Score)>();
            var comparisonImagePages = new List<int>();
            var comparisonImagesDir = Path.Combine(
                Path.GetDirectoryName(fluentPdfPath) ?? ".",
                $"quality_comparison_{correlationId:N}");

            for (int i = 1; i <= pageCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Quality validation cancelled. CorrelationId={CorrelationId}",
                        correlationId);
                    break;
                }

                var ssimScore = await CalculatePageSsimAsync(
                    fluentPdf, librePdf, i, correlationId, cancellationToken);

                if (ssimScore.HasValue)
                {
                    ssimScores.Add((i, ssimScore.Value));

                    // Save comparison images if below threshold
                    if (ssimScore.Value < threshold)
                    {
                        await SaveComparisonImagesAsync(
                            fluentPdf, librePdf, i, comparisonImagesDir, correlationId, cancellationToken);
                        comparisonImagePages.Add(i);
                    }
                }
            }

            if (ssimScores.Count == 0)
            {
                var error = new PdfError(
                    "SSIM_CALCULATION_FAILED",
                    "Failed to calculate SSIM for any page",
                    ErrorCategory.Validation,
                    ErrorSeverity.Error)
                    .WithContext("CorrelationId", correlationId);

                return Result.Fail(error);
            }

            // Calculate statistics
            var averageSsim = ssimScores.Average(s => s.Score);
            var minScore = ssimScores.MinBy(s => s.Score);

            stopwatch.Stop();

            var report = new QualityReport
            {
                AverageSsimScore = averageSsim,
                MinimumSsimScore = minScore.Score,
                MinimumScorePageNumber = minScore.PageNumber,
                LibreOfficePdfPath = libreOfficePdfPath,
                FluentPdfPath = fluentPdfPath,
                ComparisonImagePages = comparisonImagePages.AsReadOnly(),
                ComparisonImagesDirectory = comparisonImagePages.Count > 0 ? comparisonImagesDir : null,
                TotalPagesCompared = ssimScores.Count,
                ValidatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Quality validation completed. CorrelationId={CorrelationId}, AverageSsim={AverageSsim}, MinSsim={MinSsim}, TimeMs={TimeMs}",
                correlationId, averageSsim, minScore.Score, stopwatch.ElapsedMilliseconds);

            return Result.Ok<QualityReport?>(report);
        }
        catch (Exception ex)
        {
            var error = new PdfError(
                "VALIDATION_FAILED",
                $"Quality validation failed: {ex.Message}",
                ErrorCategory.System,
                ErrorSeverity.Error)
                .WithContext("CorrelationId", correlationId)
                .WithContext("ExceptionType", ex.GetType().Name);

            _logger.LogError(ex,
                "Quality validation failed. CorrelationId={CorrelationId}",
                correlationId);

            return Result.Fail(error);
        }
    }

    private async Task<string?> ConvertWithLibreOfficeAsync(
        string docxPath,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var outputDir = Path.GetDirectoryName(docxPath) ?? ".";
        var outputFileName = Path.GetFileNameWithoutExtension(docxPath) + "_libreoffice.pdf";
        var outputPath = Path.Combine(outputDir, outputFileName);

        // Delete existing file if present
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        _logger.LogInformation(
            "Starting LibreOffice conversion. CorrelationId={CorrelationId}, Input={Input}, Output={Output}",
            correlationId, docxPath, outputPath);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "soffice",
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDir}\" \"{docxPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "LibreOffice conversion failed. CorrelationId={CorrelationId}, ExitCode={ExitCode}, Error={Error}",
                correlationId, process.ExitCode, error);
            return null;
        }

        // LibreOffice creates a file with the same name but .pdf extension
        var expectedOutput = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(docxPath) + ".pdf");

        // Rename to avoid conflicts
        if (File.Exists(expectedOutput) && expectedOutput != outputPath)
        {
            File.Move(expectedOutput, outputPath, overwrite: true);
        }

        if (!File.Exists(outputPath))
        {
            _logger.LogError(
                "LibreOffice output file not found. CorrelationId={CorrelationId}, ExpectedPath={Path}",
                correlationId, outputPath);
            return null;
        }

        _logger.LogInformation(
            "LibreOffice conversion completed. CorrelationId={CorrelationId}, Output={Output}",
            correlationId, outputPath);

        return outputPath;
    }

    private async Task<double?> CalculatePageSsimAsync(
        PdfDocument fluentPdf,
        PdfDocument librePdf,
        int pageNumber,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Render both pages
            var fluentImageResult = await _pdfRenderingService.RenderPageAsync(
                fluentPdf, pageNumber, DefaultZoomLevel, DefaultDpi);

            if (fluentImageResult.IsFailed)
            {
                _logger.LogWarning(
                    "Failed to render FluentPDF page. CorrelationId={CorrelationId}, Page={Page}",
                    correlationId, pageNumber);
                return null;
            }

            var libreImageResult = await _pdfRenderingService.RenderPageAsync(
                librePdf, pageNumber, DefaultZoomLevel, DefaultDpi);

            if (libreImageResult.IsFailed)
            {
                _logger.LogWarning(
                    "Failed to render LibreOffice page. CorrelationId={CorrelationId}, Page={Page}",
                    correlationId, pageNumber);
                fluentImageResult.Value.Dispose();
                return null;
            }

            using var fluentStream = fluentImageResult.Value;
            using var libreStream = libreImageResult.Value;

            // Load images
            using var fluentImage = await Image.LoadAsync<Rgb24>(fluentStream, cancellationToken);
            using var libreImage = await Image.LoadAsync<Rgb24>(libreStream, cancellationToken);

            // Calculate SSIM using a simplified implementation
            var ssim = CalculateSsim(fluentImage, libreImage);

            _logger.LogDebug(
                "SSIM calculated for page. CorrelationId={CorrelationId}, Page={Page}, SSIM={SSIM}",
                correlationId, pageNumber, ssim);

            return ssim;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to calculate SSIM for page. CorrelationId={CorrelationId}, Page={Page}",
                correlationId, pageNumber);
            return null;
        }
    }

    private async Task SaveComparisonImagesAsync(
        PdfDocument fluentPdf,
        PdfDocument librePdf,
        int pageNumber,
        string outputDir,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(outputDir);

            // Render both pages again (could cache previous renders, but keeping it simple)
            var fluentImageResult = await _pdfRenderingService.RenderPageAsync(
                fluentPdf, pageNumber, DefaultZoomLevel, DefaultDpi);

            var libreImageResult = await _pdfRenderingService.RenderPageAsync(
                librePdf, pageNumber, DefaultZoomLevel, DefaultDpi);

            if (fluentImageResult.IsSuccess && libreImageResult.IsSuccess)
            {
                using var fluentStream = fluentImageResult.Value;
                using var libreStream = libreImageResult.Value;

                var fluentPath = Path.Combine(outputDir, $"page_{pageNumber}_fluent.png");
                var librePath = Path.Combine(outputDir, $"page_{pageNumber}_libreoffice.png");

                await using (var fluentFile = File.Create(fluentPath))
                {
                    fluentStream.Seek(0, SeekOrigin.Begin);
                    await fluentStream.CopyToAsync(fluentFile, cancellationToken);
                }

                await using (var libreFile = File.Create(librePath))
                {
                    libreStream.Seek(0, SeekOrigin.Begin);
                    await libreStream.CopyToAsync(libreFile, cancellationToken);
                }

                _logger.LogInformation(
                    "Saved comparison images. CorrelationId={CorrelationId}, Page={Page}, Dir={Dir}",
                    correlationId, pageNumber, outputDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to save comparison images. CorrelationId={CorrelationId}, Page={Page}",
                correlationId, pageNumber);
        }
    }

    /// <summary>
    /// Calculates SSIM (Structural Similarity Index) between two images.
    /// This is a simplified implementation that calculates luminance similarity.
    /// For production use, consider using a library like OpenCvSharp for full SSIM calculation.
    /// </summary>
    private double CalculateSsim(Image<Rgb24> img1, Image<Rgb24> img2)
    {
        // Ensure images are same size (resize if needed)
        if (img1.Width != img2.Width || img1.Height != img2.Height)
        {
            // Resize img2 to match img1
            var resizeWidth = img1.Width;
            var resizeHeight = img1.Height;

            // Create a new image with the target size
            var resizedImg2 = img2.Clone(context =>
                context.Resize(resizeWidth, resizeHeight));

            // Calculate on the resized image
            var result = CalculateSsimSameSize(img1, resizedImg2);
            resizedImg2.Dispose();
            return result;
        }

        return CalculateSsimSameSize(img1, img2);
    }

    private double CalculateSsimSameSize(Image<Rgb24> img1, Image<Rgb24> img2)
    {
        // Calculate mean squared error (MSE) and use it as a proxy for SSIM
        // This is a simplified approach; true SSIM uses sliding windows and multiple metrics
        long sumSquaredDiff = 0;
        long totalPixels = img1.Width * img1.Height;

        // Process the image pixel by pixel
        img1.ProcessPixelRows(img2, (accessor1, accessor2) =>
        {
            for (int y = 0; y < accessor1.Height; y++)
            {
                var row1 = accessor1.GetRowSpan(y);
                var row2 = accessor2.GetRowSpan(y);

                for (int x = 0; x < accessor1.Width; x++)
                {
                    var p1 = row1[x];
                    var p2 = row2[x];

                    // Calculate luminance (simplified)
                    var lum1 = 0.299 * p1.R + 0.587 * p1.G + 0.114 * p1.B;
                    var lum2 = 0.299 * p2.R + 0.587 * p2.G + 0.114 * p2.B;

                    var diff = lum1 - lum2;
                    sumSquaredDiff += (long)(diff * diff);
                }
            }
        });

        // Calculate MSE
        var mse = (double)sumSquaredDiff / totalPixels;

        // Convert MSE to a similarity score (0 = identical, larger = more different)
        // Map to SSIM-like scale (0-1, where 1 is identical)
        // Using formula: similarity = 1 / (1 + MSE/C) where C is a constant
        const double C = 10000.0; // Normalization constant
        var similarity = 1.0 / (1.0 + mse / C);

        return similarity;
    }
}
