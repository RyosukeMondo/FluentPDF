using FluentPDF.App.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FluentPDF.App.Tests.Controls;

/// <summary>
/// Visual regression tests for GlassPanel using Verify.Xaml snapshot testing.
/// Tests varying blur/opacity configurations and Light/Dark theme variants.
/// Uses Win2D CanvasRenderTarget for headless CI-compatible rendering.
/// </summary>
// [UsesVerify] // TODO: Fix UsesVerify attribute after Verify.Xunit version upgrade
public class GlassPanelVisualTests : IAsyncLifetime
{
    private const int SnapshotWidth = 400;
    private const int SnapshotHeight = 300;
    private const float SnapshotDpi = 96.0f;

    private CanvasDevice? _canvasDevice;

    /// <summary>
    /// Initializes Win2D canvas device for headless rendering.
    /// </summary>
    public Task InitializeAsync()
    {
        try
        {
            _canvasDevice = CanvasDevice.GetSharedDevice();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize Win2D canvas device. Ensure Win2D is properly installed.",
                ex);
        }
    }

    /// <summary>
    /// Cleans up Win2D resources.
    /// </summary>
    public Task DisposeAsync()
    {
        _canvasDevice?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public Task GlassPanel_DefaultConfiguration_LightTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "default_light");
    }

    [Fact]
    public Task GlassPanel_DefaultConfiguration_DarkTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "default_dark");
    }

    [Fact]
    public Task GlassPanel_ZeroElevation_LightTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 0.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "zero_elevation_light");
    }

    [Fact]
    public Task GlassPanel_ZeroElevation_DarkTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 0.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "zero_elevation_dark");
    }

    [Fact]
    public Task GlassPanel_MaxElevation_LightTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 32.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "max_elevation_light");
    }

    [Fact]
    public Task GlassPanel_MaxElevation_DarkTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 32.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "max_elevation_dark");
    }

    [Fact]
    public Task GlassPanel_SharpCorners_LightTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(0),
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "sharp_corners_light");
    }

    [Fact]
    public Task GlassPanel_SharpCorners_DarkTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(0),
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "sharp_corners_dark");
    }

    [Fact]
    public Task GlassPanel_RoundedCorners_LightTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(16),
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "rounded_corners_light");
    }

    [Fact]
    public Task GlassPanel_RoundedCorners_DarkTheme()
    {
        var panel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(16),
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "rounded_corners_dark");
    }

    [Fact]
    public Task GlassPanel_WithTextContent_LightTheme()
    {
        var panel = CreateGlassPanelWithContent(
            elevation: 8.0,
            theme: ElementTheme.Light);

        return VerifyPanel(panel, "with_content_light");
    }

    [Fact]
    public Task GlassPanel_WithTextContent_DarkTheme()
    {
        var panel = CreateGlassPanelWithContent(
            elevation: 8.0,
            theme: ElementTheme.Dark);

        return VerifyPanel(panel, "with_content_dark");
    }

    [Fact]
    public Task GlassPanel_NestedElevations_LightTheme()
    {
        var outerPanel = CreateGlassPanel(
            elevation: 16.0,
            cornerRadius: new CornerRadius(12),
            theme: ElementTheme.Light);

        var innerPanel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Light);

        innerPanel.Width = 200;
        innerPanel.Height = 150;
        innerPanel.HorizontalAlignment = HorizontalAlignment.Center;
        innerPanel.VerticalAlignment = VerticalAlignment.Center;

        outerPanel.Content = innerPanel;

        return VerifyPanel(outerPanel, "nested_elevations_light");
    }

    [Fact]
    public Task GlassPanel_NestedElevations_DarkTheme()
    {
        var outerPanel = CreateGlassPanel(
            elevation: 16.0,
            cornerRadius: new CornerRadius(12),
            theme: ElementTheme.Dark);

        var innerPanel = CreateGlassPanel(
            elevation: 8.0,
            cornerRadius: new CornerRadius(8),
            theme: ElementTheme.Dark);

        innerPanel.Width = 200;
        innerPanel.Height = 150;
        innerPanel.HorizontalAlignment = HorizontalAlignment.Center;
        innerPanel.VerticalAlignment = VerticalAlignment.Center;

        outerPanel.Content = innerPanel;

        return VerifyPanel(outerPanel, "nested_elevations_dark");
    }

    /// <summary>
    /// Creates a GlassPanel with specified configuration.
    /// </summary>
    private static GlassPanel CreateGlassPanel(
        double elevation,
        CornerRadius cornerRadius,
        ElementTheme theme)
    {
        var panel = new GlassPanel
        {
            Width = SnapshotWidth,
            Height = SnapshotHeight,
            Elevation = elevation,
            GlassCornerRadius = cornerRadius,
            RequestedTheme = theme
        };

        return panel;
    }

    /// <summary>
    /// Creates a GlassPanel with sample text content.
    /// </summary>
    private static GlassPanel CreateGlassPanelWithContent(
        double elevation,
        ElementTheme theme)
    {
        var textBlock = new TextBlock
        {
            Text = "Glass Panel Sample Content\n\nThis demonstrates how content appears within a glass panel with acrylic backdrop and elevation shadow.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            Margin = new Thickness(24)
        };

        var panel = new GlassPanel
        {
            Width = SnapshotWidth,
            Height = SnapshotHeight,
            Elevation = elevation,
            GlassCornerRadius = new CornerRadius(8),
            RequestedTheme = theme,
            Content = textBlock
        };

        return panel;
    }

    /// <summary>
    /// Renders panel to Win2D canvas and verifies snapshot.
    /// Uses headless rendering for CI compatibility.
    /// </summary>
    private async Task VerifyPanel(GlassPanel panel, string scenario)
    {
        if (_canvasDevice == null)
        {
            throw new InvalidOperationException("Canvas device not initialized.");
        }

        // Force layout pass
        panel.Measure(new Windows.Foundation.Size(SnapshotWidth, SnapshotHeight));
        panel.Arrange(new Windows.Foundation.Rect(0, 0, SnapshotWidth, SnapshotHeight));
        panel.UpdateLayout();

        // Wait for rendering to complete
        await Task.Delay(100);

        // Create render target
        using var renderTarget = new CanvasRenderTarget(
            _canvasDevice,
            SnapshotWidth,
            SnapshotHeight,
            SnapshotDpi);

        // Render control to canvas
        using (var drawingSession = renderTarget.CreateDrawingSession())
        {
            drawingSession.Clear(Microsoft.Graphics.Canvas.UI.Colors.Transparent);

            // Capture XAML visual tree to bitmap
            var xamlRenderer = new RenderTargetBitmap();
            await xamlRenderer.RenderAsync(panel);

            var pixelBuffer = await xamlRenderer.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();

            // Draw pixels to canvas
            using var bitmap = CanvasBitmap.CreateFromBytes(
                _canvasDevice,
                pixels,
                xamlRenderer.PixelWidth,
                xamlRenderer.PixelHeight,
                Microsoft.Graphics.Canvas.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);

            drawingSession.DrawImage(bitmap);
        }

        // Save to PNG for verification
        var snapshotBytes = renderTarget.GetPixelBytes();
        var snapshotPath = Path.Combine(
            "Snapshots",
            "GlassPanel",
            $"{scenario}.png");

        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        // Convert to PNG using Win2D
        using var stream = File.Create(snapshotPath);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png);

        // Verify snapshot
        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("Snapshots/GlassPanel");
        verifySettings.UseFileName($"GlassPanel_{scenario}");

        await Verifier.VerifyFile(snapshotPath, verifySettings);
    }
}
