using FlaUI.Core.AutomationElements;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FluentPDF.E2E.Tests.Fixtures;

/// <summary>
/// Analyzes color contrast ratios for WCAG 2.1 compliance.
/// Provides utilities for calculating contrast ratios, extracting colors from UI elements,
/// and validating accessibility requirements.
/// </summary>
public class ColorContrastAnalyzer
{
    /// <summary>
    /// WCAG AA minimum contrast ratio for normal text (4.5:1).
    /// </summary>
    public const double WcagAA_NormalText = 4.5;

    /// <summary>
    /// WCAG AA minimum contrast ratio for large text (3:1).
    /// Large text is 18pt+ normal or 14pt+ bold.
    /// </summary>
    public const double WcagAA_LargeText = 3.0;

    /// <summary>
    /// WCAG AA minimum contrast ratio for UI components (3:1).
    /// </summary>
    public const double WcagAA_UIComponents = 3.0;

    /// <summary>
    /// WCAG AAA minimum contrast ratio for normal text (7:1).
    /// </summary>
    public const double WcagAAA_NormalText = 7.0;

    /// <summary>
    /// WCAG AAA minimum contrast ratio for large text (4.5:1).
    /// </summary>
    public const double WcagAAA_LargeText = 4.5;

    /// <summary>
    /// Calculates the WCAG 2.1 contrast ratio between two colors.
    /// Formula: (L1 + 0.05) / (L2 + 0.05) where L1 is lighter, L2 is darker.
    /// </summary>
    /// <param name="foreground">Foreground color (e.g., text color).</param>
    /// <param name="background">Background color.</param>
    /// <returns>Contrast ratio (e.g., 4.5 for 4.5:1).</returns>
    public double CalculateContrastRatio(Color foreground, Color background)
    {
        var l1 = GetRelativeLuminance(foreground);
        var l2 = GetRelativeLuminance(background);

        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Validates if contrast ratio meets WCAG AA requirements.
    /// </summary>
    /// <param name="contrastRatio">Calculated contrast ratio.</param>
    /// <param name="isLargeText">Whether text is considered large (18pt+ or 14pt+ bold).</param>
    /// <param name="isUIComponent">Whether this is a UI component (not text).</param>
    /// <returns>True if contrast meets WCAG AA requirements.</returns>
    public bool MeetsWcagAA(double contrastRatio, bool isLargeText = false, bool isUIComponent = false)
    {
        if (isUIComponent)
            return contrastRatio >= WcagAA_UIComponents;

        return isLargeText
            ? contrastRatio >= WcagAA_LargeText
            : contrastRatio >= WcagAA_NormalText;
    }

    /// <summary>
    /// Validates if contrast ratio meets WCAG AAA requirements.
    /// </summary>
    public bool MeetsWcagAAA(double contrastRatio, bool isLargeText = false)
    {
        return isLargeText
            ? contrastRatio >= WcagAAA_LargeText
            : contrastRatio >= WcagAAA_NormalText;
    }

    /// <summary>
    /// Extracts the dominant color from a UI element by capturing a screenshot.
    /// Uses the center pixel as representative color.
    /// </summary>
    /// <param name="element">UI element to analyze.</param>
    /// <returns>Dominant color or null if extraction failed.</returns>
    public Color? ExtractElementColor(AutomationElement element)
    {
        try
        {
            // Get element bounds
            var bounds = element.BoundingRectangle;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            // Capture screenshot of element
            using var bitmap = CaptureScreenRegion(
                (int)bounds.X,
                (int)bounds.Y,
                (int)bounds.Width,
                (int)bounds.Height);

            if (bitmap == null)
                return null;

            // Get center pixel color as representative
            var centerX = bitmap.Width / 2;
            var centerY = bitmap.Height / 2;

            return bitmap.GetPixel(centerX, centerY);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Analyzes contrast between element foreground and background.
    /// </summary>
    /// <param name="element">Element to analyze.</param>
    /// <returns>Contrast analysis result with ratio and colors.</returns>
    public ContrastAnalysisResult? AnalyzeElementContrast(AutomationElement element)
    {
        try
        {
            var bounds = element.BoundingRectangle;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            using var bitmap = CaptureScreenRegion(
                (int)bounds.X,
                (int)bounds.Y,
                (int)bounds.Width,
                (int)bounds.Height);

            if (bitmap == null)
                return null;

            // Sample multiple points to estimate foreground/background
            var colors = SampleColors(bitmap, sampleCount: 9);

            // Assume darkest is foreground (text) and lightest is background
            var foreground = colors.OrderBy(c => GetRelativeLuminance(c)).First();
            var background = colors.OrderBy(c => GetRelativeLuminance(c)).Last();

            var ratio = CalculateContrastRatio(foreground, background);

            return new ContrastAnalysisResult
            {
                ForegroundColor = foreground,
                BackgroundColor = background,
                ContrastRatio = ratio,
                MeetsWcagAA = MeetsWcagAA(ratio),
                MeetsWcagAAA = MeetsWcagAAA(ratio)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Captures a region of the screen as a bitmap.
    /// </summary>
    private Bitmap? CaptureScreenRegion(int x, int y, int width, int height)
    {
        try
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Samples colors from bitmap at multiple points.
    /// </summary>
    private List<Color> SampleColors(Bitmap bitmap, int sampleCount = 9)
    {
        var colors = new List<Color>();
        var step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / (int)Math.Sqrt(sampleCount));

        for (int x = step; x < bitmap.Width; x += step)
        {
            for (int y = step; y < bitmap.Height; y += step)
            {
                if (colors.Count >= sampleCount)
                    break;

                colors.Add(bitmap.GetPixel(x, y));
            }

            if (colors.Count >= sampleCount)
                break;
        }

        return colors;
    }

    /// <summary>
    /// Calculates relative luminance for WCAG contrast formula.
    /// Y = 0.2126 * R + 0.7152 * G + 0.0722 * B
    /// </summary>
    private double GetRelativeLuminance(Color color)
    {
        var r = GetSRGBComponent(color.R / 255.0);
        var g = GetSRGBComponent(color.G / 255.0);
        var b = GetSRGBComponent(color.B / 255.0);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Converts sRGB component to linear RGB for luminance calculation.
    /// </summary>
    private double GetSRGBComponent(double component)
    {
        return component <= 0.03928
            ? component / 12.92
            : Math.Pow((component + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Formats contrast ratio as readable string (e.g., "4.5:1").
    /// </summary>
    public string FormatRatio(double ratio) => $"{ratio:F2}:1";

    /// <summary>
    /// Gets WCAG level achieved by contrast ratio.
    /// </summary>
    public string GetWcagLevel(double ratio, bool isLargeText = false)
    {
        if (MeetsWcagAAA(ratio, isLargeText))
            return "AAA";
        if (MeetsWcagAA(ratio, isLargeText))
            return "AA";
        return "Fail";
    }
}

/// <summary>
/// Result of contrast analysis for a UI element.
/// </summary>
public record ContrastAnalysisResult
{
    /// <summary>
    /// Detected foreground color (e.g., text color).
    /// </summary>
    public Color ForegroundColor { get; init; }

    /// <summary>
    /// Detected background color.
    /// </summary>
    public Color BackgroundColor { get; init; }

    /// <summary>
    /// Calculated contrast ratio.
    /// </summary>
    public double ContrastRatio { get; init; }

    /// <summary>
    /// Whether contrast meets WCAG AA requirements.
    /// </summary>
    public bool MeetsWcagAA { get; init; }

    /// <summary>
    /// Whether contrast meets WCAG AAA requirements.
    /// </summary>
    public bool MeetsWcagAAA { get; init; }

    /// <summary>
    /// Formatted summary of analysis.
    /// </summary>
    public override string ToString()
    {
        var analyzer = new ColorContrastAnalyzer();
        return $"Ratio: {analyzer.FormatRatio(ContrastRatio)}, " +
               $"FG: {ForegroundColor}, BG: {BackgroundColor}, " +
               $"WCAG: {analyzer.GetWcagLevel(ContrastRatio)}";
    }
}
