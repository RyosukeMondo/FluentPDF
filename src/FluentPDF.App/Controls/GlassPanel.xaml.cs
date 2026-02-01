using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Controls;

/// <summary>
/// Reusable frosted glass panel with acrylic backdrop and elevation shadow.
/// Provides liquid glass aesthetic for panels, sidebars, and dialogs.
/// </summary>
public sealed partial class GlassPanel : UserControl
{
    /// <summary>
    /// Dependency property for Elevation (shadow depth in dp).
    /// </summary>
    public static readonly DependencyProperty ElevationProperty =
        DependencyProperty.Register(
            nameof(Elevation),
            typeof(double),
            typeof(GlassPanel),
            new PropertyMetadata(8.0, OnElevationChanged));

    /// <summary>
    /// Dependency property for glass panel corner radius.
    /// </summary>
    public static readonly DependencyProperty GlassCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(GlassCornerRadius),
            typeof(CornerRadius),
            typeof(GlassPanel),
            new PropertyMetadata(new CornerRadius(8)));

    /// <summary>
    /// Gets or sets the elevation depth for the shadow (in dp).
    /// Range: 0-32dp. Default: 8dp.
    /// </summary>
    public double Elevation
    {
        get => (double)GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    /// <summary>
    /// Gets or sets the corner radius of the glass panel.
    /// Default: 8px rounded corners.
    /// </summary>
    public CornerRadius GlassCornerRadius
    {
        get => (CornerRadius)GetValue(GlassCornerRadiusProperty);
        set => SetValue(GlassCornerRadiusProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlassPanel"/> class.
    /// </summary>
    public GlassPanel()
    {
        InitializeComponent();
        UpdateElevation(Elevation);
    }

    /// <summary>
    /// Handles elevation property changes to update shadow depth.
    /// </summary>
    private static void OnElevationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassPanel panel)
        {
            panel.UpdateElevation((double)e.NewValue);
        }
    }

    /// <summary>
    /// Updates the shadow depth based on elevation value.
    /// </summary>
    private void UpdateElevation(double elevation)
    {
        if (GlassContainer == null)
        {
            return;
        }

        // Clamp elevation to 0-32dp range
        var clampedElevation = Math.Clamp(elevation, 0, 32);

        // Apply translation to create depth illusion
        GlassContainer.Translation = new System.Numerics.Vector3(0, 0, (float)clampedElevation);
    }
}
