using FluentPDF.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FluentPDF.App.Views;

/// <summary>
/// Page for DOCX to PDF conversion with file selection, conversion controls, and results display.
/// Follows Fluent Design principles with data binding to ConversionViewModel.
/// </summary>
public sealed partial class ConversionPage : Page
{
    /// <summary>
    /// Gets the view model for this page.
    /// </summary>
    public ConversionViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionPage"/> class.
    /// </summary>
    public ConversionPage()
    {
        this.InitializeComponent();

        // Resolve ViewModel from DI container
        var app = (App)Application.Current;
        ViewModel = app.GetService<ConversionViewModel>();

        // Set DataContext for runtime binding
        this.DataContext = ViewModel;
    }

    /// <summary>
    /// Formats file size in bytes to kilobytes with one decimal place.
    /// </summary>
    /// <param name="sizeBytes">File size in bytes.</param>
    /// <returns>Formatted size string.</returns>
    private string FormatFileSize(long sizeBytes)
    {
        return (sizeBytes / 1024.0).ToString("F1");
    }

    /// <summary>
    /// Formats conversion time to two decimal places.
    /// </summary>
    /// <param name="time">Conversion time.</param>
    /// <returns>Formatted time string.</returns>
    private string FormatTime(TimeSpan time)
    {
        return time.TotalSeconds.ToString("F2");
    }

    /// <summary>
    /// Formats quality score to three decimal places.
    /// </summary>
    /// <param name="qualityScore">SSIM quality score (nullable).</param>
    /// <returns>Formatted score string or empty if null.</returns>
    private string FormatQualityScore(double? qualityScore)
    {
        return qualityScore.HasValue ? qualityScore.Value.ToString("F3") : string.Empty;
    }

    /// <summary>
    /// Checks if page count is available.
    /// </summary>
    /// <param name="pageCount">Page count (nullable).</param>
    /// <returns>Visibility.Visible if page count has value, Collapsed otherwise.</returns>
    private Visibility HasPageCount(int? pageCount)
    {
        return pageCount.HasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Checks if quality score is available.
    /// </summary>
    /// <param name="qualityScore">Quality score (nullable).</param>
    /// <returns>Visibility.Visible if quality score has value, Collapsed otherwise.</returns>
    private Visibility HasQualityScore(double? qualityScore)
    {
        return qualityScore.HasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Gets a quality rating based on SSIM score.
    /// </summary>
    /// <param name="qualityScore">SSIM quality score (0-1).</param>
    /// <returns>Quality rating text.</returns>
    private string GetQualityRating(double? qualityScore)
    {
        if (!qualityScore.HasValue)
            return string.Empty;

        return qualityScore.Value switch
        {
            >= 0.95 => "(Excellent)",
            >= 0.90 => "(Very Good)",
            >= 0.80 => "(Good)",
            >= 0.70 => "(Fair)",
            _ => "(Poor)"
        };
    }

    /// <summary>
    /// Gets a color brush based on quality score.
    /// </summary>
    /// <param name="qualityScore">SSIM quality score (0-1).</param>
    /// <returns>Color brush for quality rating.</returns>
    private SolidColorBrush GetQualityColor(double? qualityScore)
    {
        if (!qualityScore.HasValue)
            return new SolidColorBrush(Colors.Gray);

        return qualityScore.Value switch
        {
            >= 0.90 => new SolidColorBrush(Colors.Green),
            >= 0.80 => new SolidColorBrush(Colors.YellowGreen),
            >= 0.70 => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Red)
        };
    }
}
