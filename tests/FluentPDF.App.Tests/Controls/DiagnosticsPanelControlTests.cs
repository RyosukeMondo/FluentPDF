using FluentAssertions;
using FluentPDF.App.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace FluentPDF.App.Tests.Controls;

/// <summary>
/// Tests for DiagnosticsPanelControl custom WinUI control.
/// Tests basic property and dependency property behavior.
/// </summary>
public class DiagnosticsPanelControlTests
{
    [Fact]
    public void Constructor_ShouldInitializeControl()
    {
        // Arrange & Act
        var control = new DiagnosticsPanelControl();

        // Assert
        control.Should().NotBeNull();
        control.CurrentFPS.Should().Be(0.0, "initial FPS is zero");
        control.ManagedMemoryMB.Should().Be(0L, "initial managed memory is zero");
        control.NativeMemoryMB.Should().Be(0L, "initial native memory is zero");
        control.TotalMemoryMB.Should().Be(0L, "initial total memory is zero");
        control.LastRenderTimeMs.Should().Be(0.0, "initial render time is zero");
        control.CurrentPageNumber.Should().Be(0, "initial page number is zero");
        control.IsVisible.Should().BeFalse("initially not visible");
        control.FPSColor.Should().NotBeNull("FPS color should be initialized");
        control.FPSColor.Color.Should().Be(Colors.Green, "default FPS color is green");
    }

    [Fact]
    public void CurrentFPS_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.CurrentFPS = 60.5;

        // Assert
        control.CurrentFPS.Should().Be(60.5);
    }

    [Fact]
    public void ManagedMemoryMB_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.ManagedMemoryMB = 256L;

        // Assert
        control.ManagedMemoryMB.Should().Be(256L);
    }

    [Fact]
    public void NativeMemoryMB_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.NativeMemoryMB = 128L;

        // Assert
        control.NativeMemoryMB.Should().Be(128L);
    }

    [Fact]
    public void TotalMemoryMB_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.TotalMemoryMB = 384L;

        // Assert
        control.TotalMemoryMB.Should().Be(384L);
    }

    [Fact]
    public void LastRenderTimeMs_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.LastRenderTimeMs = 16.7;

        // Assert
        control.LastRenderTimeMs.Should().Be(16.7);
    }

    [Fact]
    public void CurrentPageNumber_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.CurrentPageNumber = 5;

        // Assert
        control.CurrentPageNumber.Should().Be(5);
    }

    [Fact]
    public void IsVisible_ShouldSetAndGetValue()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.IsVisible = true;

        // Assert
        control.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void FPSColor_ShouldAcceptCustomBrush()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();
        var redBrush = new SolidColorBrush(Colors.Red);

        // Act
        control.FPSColor = redBrush;

        // Assert
        control.FPSColor.Should().Be(redBrush);
        control.FPSColor.Color.Should().Be(Colors.Red);
    }

    [Fact]
    public void FPSColor_ShouldSupportYellowForWarning()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();
        var yellowBrush = new SolidColorBrush(Colors.Yellow);

        // Act
        control.FPSColor = yellowBrush;

        // Assert
        control.FPSColor.Color.Should().Be(Colors.Yellow);
    }

    [Fact]
    public void ExportMetricsCommand_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var control = new DiagnosticsPanelControl();

        // Assert
        control.ExportMetricsCommand.Should().BeNull("no command assigned initially");
    }

    [Fact]
    public void OpenLogViewerCommand_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var control = new DiagnosticsPanelControl();

        // Assert
        control.OpenLogViewerCommand.Should().BeNull("no command assigned initially");
    }

    [Fact]
    public void MultipleProperties_ShouldBeIndependent()
    {
        // Arrange
        var control = new DiagnosticsPanelControl();

        // Act
        control.CurrentFPS = 30.0;
        control.ManagedMemoryMB = 200L;
        control.NativeMemoryMB = 100L;
        control.TotalMemoryMB = 300L;
        control.LastRenderTimeMs = 33.3;
        control.CurrentPageNumber = 10;
        control.IsVisible = true;

        // Assert - all properties should maintain their values
        control.CurrentFPS.Should().Be(30.0);
        control.ManagedMemoryMB.Should().Be(200L);
        control.NativeMemoryMB.Should().Be(100L);
        control.TotalMemoryMB.Should().Be(300L);
        control.LastRenderTimeMs.Should().Be(33.3);
        control.CurrentPageNumber.Should().Be(10);
        control.IsVisible.Should().BeTrue();
    }
}
