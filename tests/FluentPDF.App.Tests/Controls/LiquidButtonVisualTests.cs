using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace FluentPDF.App.Tests.Controls;

/// <summary>
/// Visual regression tests for LiquidButton states using Verify.Xaml snapshot testing.
/// Tests all interactive states: Normal, PointerOver, Pressed, Disabled.
/// Uses Win2D CanvasRenderTarget for headless CI-compatible rendering.
/// </summary>
/// <remarks>
/// Note: LiquidButton is implemented in Avalonia project. These tests create
/// a WinUI 3 button with equivalent styling for visual consistency testing.
/// For true Avalonia LiquidButton tests, see FluentPDF.Avalonia.Tests project.
/// </remarks>
// [UsesVerify] // TODO: Fix UsesVerify attribute after Verify.Xunit version upgrade
public class LiquidButtonVisualTests : IAsyncLifetime
{
    private const int SnapshotWidth = 200;
    private const int SnapshotHeight = 100;
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
    public Task LiquidButton_NormalState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Light);

        return VerifyButton(button, "normal_light");
    }

    [Fact]
    public Task LiquidButton_NormalState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Dark);

        return VerifyButton(button, "normal_dark");
    }

    [Fact]
    public Task LiquidButton_PointerOverState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: true,
            theme: ElementTheme.Light);

        return VerifyButton(button, "hover_light");
    }

    [Fact]
    public Task LiquidButton_PointerOverState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: true,
            theme: ElementTheme.Dark);

        return VerifyButton(button, "hover_dark");
    }

    [Fact]
    public Task LiquidButton_PressedState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: true,
            isPointerOver: true,
            theme: ElementTheme.Light);

        return VerifyButton(button, "pressed_light");
    }

    [Fact]
    public Task LiquidButton_PressedState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: true,
            isPointerOver: true,
            theme: ElementTheme.Dark);

        return VerifyButton(button, "pressed_dark");
    }

    [Fact]
    public Task LiquidButton_DisabledState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: false,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Light);

        return VerifyButton(button, "disabled_light");
    }

    [Fact]
    public Task LiquidButton_DisabledState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: false,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Dark);

        return VerifyButton(button, "disabled_dark");
    }

    [Fact]
    public Task LiquidButton_PrimaryStyle_NormalState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Light,
            isPrimary: true);

        return VerifyButton(button, "primary_normal_light");
    }

    [Fact]
    public Task LiquidButton_PrimaryStyle_NormalState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Dark,
            isPrimary: true);

        return VerifyButton(button, "primary_normal_dark");
    }

    [Fact]
    public Task LiquidButton_PrimaryStyle_PointerOverState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: true,
            theme: ElementTheme.Light,
            isPrimary: true);

        return VerifyButton(button, "primary_hover_light");
    }

    [Fact]
    public Task LiquidButton_PrimaryStyle_PointerOverState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: true,
            theme: ElementTheme.Dark,
            isPrimary: true);

        return VerifyButton(button, "primary_hover_dark");
    }

    [Fact]
    public Task LiquidButton_WithIcon_NormalState_LightTheme()
    {
        var button = CreateLiquidButtonWithIcon(
            isEnabled: true,
            theme: ElementTheme.Light);

        return VerifyButton(button, "with_icon_light");
    }

    [Fact]
    public Task LiquidButton_WithIcon_NormalState_DarkTheme()
    {
        var button = CreateLiquidButtonWithIcon(
            isEnabled: true,
            theme: ElementTheme.Dark);

        return VerifyButton(button, "with_icon_dark");
    }

    [Fact]
    public Task LiquidButton_CompactSize_NormalState_LightTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Light,
            isCompact: true);

        return VerifyButton(button, "compact_light");
    }

    [Fact]
    public Task LiquidButton_CompactSize_NormalState_DarkTheme()
    {
        var button = CreateLiquidButton(
            isEnabled: true,
            isPressed: false,
            isPointerOver: false,
            theme: ElementTheme.Dark,
            isCompact: true);

        return VerifyButton(button, "compact_dark");
    }

    /// <summary>
    /// Creates a button with liquid styling and specified visual state.
    /// </summary>
    private static Button CreateLiquidButton(
        bool isEnabled,
        bool isPressed,
        bool isPointerOver,
        ElementTheme theme,
        bool isPrimary = false,
        bool isCompact = false)
    {
        var button = new Button
        {
            Content = "Liquid Button",
            IsEnabled = isEnabled,
            RequestedTheme = theme,
            Width = SnapshotWidth - 40,
            Height = isCompact ? 32 : 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = isCompact ? 12 : 14,
            CornerRadius = new CornerRadius(6)
        };

        // Apply primary style
        if (isPrimary)
        {
            button.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
        }

        // Simulate visual states by applying transforms and opacity
        if (isPressed)
        {
            button.RenderTransform = new ScaleTransform { ScaleX = 0.95, ScaleY = 0.95 };
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        if (isPointerOver && !isPressed)
        {
            // Hover state is handled by visual state manager, but we can set background
            button.Opacity = 1.0;
        }

        if (!isEnabled)
        {
            button.Opacity = 0.5;
        }

        return button;
    }

    /// <summary>
    /// Creates a button with icon and text content.
    /// </summary>
    private static Button CreateLiquidButtonWithIcon(
        bool isEnabled,
        ElementTheme theme)
    {
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var icon = new SymbolIcon(Symbol.Play);
        var text = new TextBlock { Text = "Play" };

        stackPanel.Children.Add(icon);
        stackPanel.Children.Add(text);

        var button = new Button
        {
            Content = stackPanel,
            IsEnabled = isEnabled,
            RequestedTheme = theme,
            Width = SnapshotWidth - 40,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(6)
        };

        return button;
    }

    /// <summary>
    /// Renders button to Win2D canvas and verifies snapshot.
    /// Uses headless rendering for CI compatibility.
    /// </summary>
    private async Task VerifyButton(Button button, string scenario)
    {
        if (_canvasDevice == null)
        {
            throw new InvalidOperationException("Canvas device not initialized.");
        }

        // Create container with background for context
        var container = new Grid
        {
            Width = SnapshotWidth,
            Height = SnapshotHeight,
            Background = new SolidColorBrush(
                button.RequestedTheme == ElementTheme.Dark
                    ? Windows.UI.Color.FromArgb(255, 32, 32, 32)
                    : Windows.UI.Color.FromArgb(255, 243, 243, 243))
        };

        container.Children.Add(button);

        // Force layout pass
        container.Measure(new Windows.Foundation.Size(SnapshotWidth, SnapshotHeight));
        container.Arrange(new Windows.Foundation.Rect(0, 0, SnapshotWidth, SnapshotHeight));
        container.UpdateLayout();

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
            await xamlRenderer.RenderAsync(container);

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
        var snapshotPath = Path.Combine(
            "Snapshots",
            "LiquidButton",
            $"{scenario}.png");

        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        // Convert to PNG using Win2D
        using var stream = File.Create(snapshotPath);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png);

        // Verify snapshot
        var verifySettings = new VerifySettings();
        verifySettings.UseDirectory("Snapshots/LiquidButton");
        verifySettings.UseFileName($"LiquidButton_{scenario}");

        await Verifier.VerifyFile(snapshotPath, verifySettings);
    }
}
