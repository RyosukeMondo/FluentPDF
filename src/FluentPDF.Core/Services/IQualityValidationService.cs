using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service contract for PDF quality validation against LibreOffice baseline.
/// Compares FluentPDF-generated PDFs with LibreOffice output using SSIM (Structural Similarity Index).
/// Gracefully handles LibreOffice not being installed by skipping validation.
/// </summary>
public interface IQualityValidationService
{
    /// <summary>
    /// Validates PDF quality by comparing against LibreOffice baseline conversion.
    /// Converts the source DOCX to PDF using LibreOffice, renders both PDFs to images,
    /// and calculates SSIM score. Saves comparison images if quality is below threshold.
    /// </summary>
    /// <param name="docxPath">Full path to the source DOCX file.</param>
    /// <param name="fluentPdfPath">Full path to the FluentPDF-generated PDF file.</param>
    /// <param name="threshold">SSIM threshold below which comparison images are saved (default 0.95).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// A Result containing QualityReport with SSIM score and comparison paths if successful,
    /// or a PdfError if validation failed.
    /// Returns null QualityReport if LibreOffice is not installed (graceful degradation).
    /// Error codes: LIBREOFFICE_CONVERSION_FAILED, PDF_RENDERING_FAILED, SSIM_CALCULATION_FAILED.
    /// </returns>
    Task<Result<QualityReport?>> ValidateQualityAsync(
        string docxPath,
        string fluentPdfPath,
        double threshold = 0.95,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if LibreOffice is installed and available for quality validation.
    /// </summary>
    /// <returns>True if LibreOffice is detected and functional, false otherwise.</returns>
    Task<bool> IsLibreOfficeAvailableAsync();
}
