using FluentPDF.Core.Models;
using FluentResults;
using System.Drawing;

namespace FluentPDF.Core.Services;

/// <summary>
/// Service interface for creating and applying stamp annotations to PDF documents.
/// </summary>
public interface IStampService
{
    /// <summary>
    /// Generates a stamp image and saves it to the specified path.
    /// </summary>
    /// <param name="stamp">The stamp configuration.</param>
    /// <param name="outputPath">The path where the stamp image will be saved.</param>
    /// <returns>Result containing the path to the generated image, or error details.</returns>
    Task<Result<string>> GenerateStampImageAsync(Stamp stamp, string outputPath);

    /// <summary>
    /// Applies a stamp to a specific page in a PDF document.
    /// </summary>
    /// <param name="document">The PDF document.</param>
    /// <param name="stamp">The stamp configuration.</param>
    /// <param name="pageNumber">The zero-based page number.</param>
    /// <param name="position">The position where the stamp will be placed.</param>
    /// <returns>Result containing the created annotation, or error details.</returns>
    Task<Result<Annotation>> ApplyStampAsync(
        PdfDocument document,
        Stamp stamp,
        int pageNumber,
        PointF position);

    /// <summary>
    /// Creates a stamp from a predefined type with optional custom image.
    /// </summary>
    /// <param name="type">The stamp type.</param>
    /// <param name="customImagePath">Optional path to a custom image for custom stamps.</param>
    /// <returns>The configured stamp.</returns>
    Stamp CreateStamp(StampType type, string? customImagePath = null);

    /// <summary>
    /// Applies dynamic text replacements to a stamp (e.g., {{DATE}}, {{TIME}}, {{USER}}).
    /// </summary>
    /// <param name="stamp">The stamp to modify with dynamic replacements.</param>
    void ApplyDynamicReplacements(Stamp stamp);
}
