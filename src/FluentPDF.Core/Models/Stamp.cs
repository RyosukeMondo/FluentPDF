using System.Drawing;

namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a PDF stamp annotation.
/// Stamps include predefined types (Approved, Draft, etc.) and custom image stamps.
/// </summary>
public class Stamp
{
    /// <summary>
    /// Gets or sets the unique identifier for this stamp.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the stamp type.
    /// </summary>
    public StampType Type { get; set; }

    /// <summary>
    /// Gets or sets the stamp text (for built-in stamps).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to a custom image stamp file.
    /// Only used when Type is Custom.
    /// </summary>
    public string? CustomImagePath { get; set; }

    /// <summary>
    /// Gets or sets the background color of the stamp.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the text color of the stamp.
    /// </summary>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the border color of the stamp.
    /// </summary>
    public Color BorderColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the rotation angle in degrees (0-360).
    /// </summary>
    public float RotationAngle { get; set; } = -45f;

    /// <summary>
    /// Gets or sets the opacity of the stamp (0.0 to 1.0).
    /// </summary>
    public double Opacity { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the width of the stamp in PDF points.
    /// </summary>
    public float Width { get; set; } = 100f;

    /// <summary>
    /// Gets or sets the height of the stamp in PDF points.
    /// </summary>
    public float Height { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the font name for text stamps.
    /// </summary>
    public string FontName { get; set; } = "Helvetica";

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public float FontSize { get; set; } = 24f;

    /// <summary>
    /// Gets or sets the border width in points.
    /// </summary>
    public float BorderWidth { get; set; } = 2f;

    /// <summary>
    /// Gets or sets dynamic text replacements (e.g., {{DATE}}, {{TIME}}).
    /// </summary>
    public Dictionary<string, string> DynamicReplacements { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date of the stamp.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the author/creator of the stamp.
    /// </summary>
    public string Author { get; set; } = string.Empty;
}

/// <summary>
/// Enumerates the built-in stamp types available in FluentPDF.
/// </summary>
public enum StampType
{
    /// <summary>
    /// Approved stamp (green).
    /// </summary>
    Approved,

    /// <summary>
    /// Rejected stamp (red).
    /// </summary>
    Rejected,

    /// <summary>
    /// Draft stamp (blue).
    /// </summary>
    Draft,

    /// <summary>
    /// Final stamp (purple).
    /// </summary>
    Final,

    /// <summary>
    /// Confidential stamp (red).
    /// </summary>
    Confidential,

    /// <summary>
    /// For Review stamp (orange).
    /// </summary>
    ForReview,

    /// <summary>
    /// Copy stamp (gray).
    /// </summary>
    Copy,

    /// <summary>
    /// Custom stamp with user-provided image.
    /// </summary>
    Custom
}

/// <summary>
/// Predefined stamp configurations for standard stamp types.
/// </summary>
public static class StampPresets
{
    private static readonly Dictionary<StampType, StampConfig> Presets = new()
    {
        {
            StampType.Approved,
            new StampConfig
            {
                Text = "APPROVED",
                BackgroundColor = Color.FromArgb(0, 128, 0), // Green
                TextColor = Color.White,
                BorderColor = Color.FromArgb(0, 100, 0)
            }
        },
        {
            StampType.Rejected,
            new StampConfig
            {
                Text = "REJECTED",
                BackgroundColor = Color.Red,
                TextColor = Color.White,
                BorderColor = Color.DarkRed
            }
        },
        {
            StampType.Draft,
            new StampConfig
            {
                Text = "DRAFT",
                BackgroundColor = Color.FromArgb(0, 0, 255), // Blue
                TextColor = Color.White,
                BorderColor = Color.DarkBlue
            }
        },
        {
            StampType.Final,
            new StampConfig
            {
                Text = "FINAL",
                BackgroundColor = Color.FromArgb(128, 0, 128), // Purple
                TextColor = Color.White,
                BorderColor = Color.Indigo
            }
        },
        {
            StampType.Confidential,
            new StampConfig
            {
                Text = "CONFIDENTIAL",
                BackgroundColor = Color.Red,
                TextColor = Color.White,
                BorderColor = Color.DarkRed
            }
        },
        {
            StampType.ForReview,
            new StampConfig
            {
                Text = "FOR REVIEW",
                BackgroundColor = Color.FromArgb(255, 165, 0), // Orange
                TextColor = Color.White,
                BorderColor = Color.OrangeRed
            }
        },
        {
            StampType.Copy,
            new StampConfig
            {
                Text = "COPY",
                BackgroundColor = Color.FromArgb(128, 128, 128), // Gray
                TextColor = Color.White,
                BorderColor = Color.DarkGray
            }
        }
    };

    /// <summary>
    /// Gets the preset configuration for a stamp type.
    /// </summary>
    /// <param name="type">The stamp type.</param>
    /// <returns>The preset configuration.</returns>
    public static StampConfig GetPreset(StampType type)
    {
        return Presets.TryGetValue(type, out var preset)
            ? preset
            : new StampConfig { Text = type.ToString() };
    }
}

/// <summary>
/// Configuration for a stamp preset.
/// </summary>
public class StampConfig
{
    /// <summary>
    /// Gets or sets the stamp text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor { get; set; } = Color.Black;
}
