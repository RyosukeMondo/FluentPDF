using FluentAssertions;
using FluentPDF.Core.Models;
using Xunit;

namespace FluentPDF.Core.Tests.Models;

/// <summary>
/// Unit tests for the DisplayInfo model and RenderingQuality enum.
/// </summary>
public sealed class DisplayInfoTests
{
    #region DisplayInfo Creation Tests

    [Fact]
    public void DisplayInfo_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var displayInfo = new DisplayInfo
        {
            RasterizationScale = 1.5,
            EffectiveDpi = 144.0
        };

        // Assert
        displayInfo.RasterizationScale.Should().Be(1.5);
        displayInfo.EffectiveDpi.Should().Be(144.0);
    }

    [Fact]
    public void DisplayInfo_Create_ReturnsValidInstance()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.Create(1.5, 144.0);

        // Assert
        displayInfo.RasterizationScale.Should().Be(1.5);
        displayInfo.EffectiveDpi.Should().Be(144.0);
    }

    [Fact]
    public void DisplayInfo_Create_ThrowsException_WhenRasterizationScaleIsZero()
    {
        // Arrange & Act
        var act = () => DisplayInfo.Create(0, 96.0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("rasterizationScale")
            .WithMessage("*Rasterization scale must be positive.*");
    }

    [Fact]
    public void DisplayInfo_Create_ThrowsException_WhenRasterizationScaleIsNegative()
    {
        // Arrange & Act
        var act = () => DisplayInfo.Create(-1.0, 96.0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("rasterizationScale")
            .WithMessage("*Rasterization scale must be positive.*");
    }

    [Fact]
    public void DisplayInfo_Create_ThrowsException_WhenEffectiveDpiTooLow()
    {
        // Arrange & Act
        var act = () => DisplayInfo.Create(1.0, 49.0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("effectiveDpi")
            .WithMessage("*Effective DPI must be between 50 and 576.*");
    }

    [Fact]
    public void DisplayInfo_Create_ThrowsException_WhenEffectiveDpiTooHigh()
    {
        // Arrange & Act
        var act = () => DisplayInfo.Create(1.0, 577.0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("effectiveDpi")
            .WithMessage("*Effective DPI must be between 50 and 576.*");
    }

    [Fact]
    public void DisplayInfo_Create_Accepts_MinimumValidDpi()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.Create(1.0, 50.0);

        // Assert
        displayInfo.EffectiveDpi.Should().Be(50.0);
    }

    [Fact]
    public void DisplayInfo_Create_Accepts_MaximumValidDpi()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.Create(1.0, 576.0);

        // Assert
        displayInfo.EffectiveDpi.Should().Be(576.0);
    }

    #endregion

    #region DisplayInfo Computed Properties Tests

    [Fact]
    public void DisplayInfo_IsHighDpi_ReturnsFalse_WhenScaleIsOne()
    {
        // Arrange
        var displayInfo = DisplayInfo.Create(1.0, 96.0);

        // Act & Assert
        displayInfo.IsHighDpi.Should().BeFalse();
    }

    [Fact]
    public void DisplayInfo_IsHighDpi_ReturnsFalse_WhenScaleIsLessThanOne()
    {
        // Arrange - Using direct initialization to bypass DPI validation
        var displayInfo = new DisplayInfo
        {
            RasterizationScale = 0.9,
            EffectiveDpi = 86.4
        };

        // Act & Assert
        displayInfo.IsHighDpi.Should().BeFalse();
    }

    [Fact]
    public void DisplayInfo_IsHighDpi_ReturnsTrue_WhenScaleIsGreaterThanOne()
    {
        // Arrange
        var displayInfo = DisplayInfo.Create(1.5, 144.0);

        // Act & Assert
        displayInfo.IsHighDpi.Should().BeTrue();
    }

    [Fact]
    public void DisplayInfo_ScalingPercentage_CalculatesCorrectly()
    {
        // Arrange & Act
        var display100 = DisplayInfo.Create(1.0, 96.0);
        var display150 = DisplayInfo.Create(1.5, 144.0);
        var display200 = DisplayInfo.Create(2.0, 192.0);

        // Assert
        display100.ScalingPercentage.Should().Be(100);
        display150.ScalingPercentage.Should().Be(150);
        display200.ScalingPercentage.Should().Be(200);
    }

    [Fact]
    public void DisplayInfo_ScalingPercentage_RoundsCorrectly()
    {
        // Arrange
        var displayInfo = new DisplayInfo
        {
            RasterizationScale = 1.249,
            EffectiveDpi = 119.9
        };

        // Act & Assert
        displayInfo.ScalingPercentage.Should().Be(125);
    }

    #endregion

    #region DisplayInfo Factory Methods Tests

    [Fact]
    public void DisplayInfo_Standard_ReturnsStandardDisplaySettings()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.Standard();

        // Assert
        displayInfo.RasterizationScale.Should().Be(1.0);
        displayInfo.EffectiveDpi.Should().Be(96.0);
        displayInfo.IsHighDpi.Should().BeFalse();
        displayInfo.ScalingPercentage.Should().Be(100);
    }

    [Fact]
    public void DisplayInfo_FromScale_CalculatesEffectiveDpi_For100Percent()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.FromScale(1.0);

        // Assert
        displayInfo.RasterizationScale.Should().Be(1.0);
        displayInfo.EffectiveDpi.Should().Be(96.0);
        displayInfo.IsHighDpi.Should().BeFalse();
    }

    [Fact]
    public void DisplayInfo_FromScale_CalculatesEffectiveDpi_For150Percent()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.FromScale(1.5);

        // Assert
        displayInfo.RasterizationScale.Should().Be(1.5);
        displayInfo.EffectiveDpi.Should().Be(144.0);
        displayInfo.IsHighDpi.Should().BeTrue();
    }

    [Fact]
    public void DisplayInfo_FromScale_CalculatesEffectiveDpi_For200Percent()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.FromScale(2.0);

        // Assert
        displayInfo.RasterizationScale.Should().Be(2.0);
        displayInfo.EffectiveDpi.Should().Be(192.0);
        displayInfo.IsHighDpi.Should().BeTrue();
    }

    [Fact]
    public void DisplayInfo_FromScale_CalculatesEffectiveDpi_For250Percent()
    {
        // Arrange & Act
        var displayInfo = DisplayInfo.FromScale(2.5);

        // Assert
        displayInfo.RasterizationScale.Should().Be(2.5);
        displayInfo.EffectiveDpi.Should().Be(240.0);
        displayInfo.IsHighDpi.Should().BeTrue();
    }

    #endregion

    #region RenderingQuality Multiplier Tests

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsCorrectValue_ForLow()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.Low);

        // Assert
        multiplier.Should().BeApproximately(0.78125, 0.00001); // 75/96
    }

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsCorrectValue_ForMedium()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.Medium);

        // Assert
        multiplier.Should().Be(1.0);
    }

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsCorrectValue_ForHigh()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.High);

        // Assert
        multiplier.Should().Be(1.5);
    }

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsCorrectValue_ForUltra()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.Ultra);

        // Assert
        multiplier.Should().Be(2.0);
    }

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsCorrectValue_ForAuto()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.Auto);

        // Assert
        multiplier.Should().Be(1.0);
    }

    [Fact]
    public void DisplayInfo_GetQualityMultiplier_ReturnsDefault_ForInvalidValue()
    {
        // Arrange & Act
        var multiplier = DisplayInfo.GetQualityMultiplier((RenderingQuality)999);

        // Assert
        multiplier.Should().Be(1.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void DisplayInfo_ComplexScenario_StandardMonitor()
    {
        // Arrange - Simulating a standard 1080p monitor at 100% scaling
        var displayInfo = DisplayInfo.Standard();

        // Act & Assert
        displayInfo.RasterizationScale.Should().Be(1.0);
        displayInfo.EffectiveDpi.Should().Be(96.0);
        displayInfo.IsHighDpi.Should().BeFalse();
        displayInfo.ScalingPercentage.Should().Be(100);
    }

    [Fact]
    public void DisplayInfo_ComplexScenario_4KMonitor_150Percent()
    {
        // Arrange - Simulating a 4K monitor at 150% scaling
        var displayInfo = DisplayInfo.FromScale(1.5);

        // Act & Assert
        displayInfo.RasterizationScale.Should().Be(1.5);
        displayInfo.EffectiveDpi.Should().Be(144.0);
        displayInfo.IsHighDpi.Should().BeTrue();
        displayInfo.ScalingPercentage.Should().Be(150);
    }

    [Fact]
    public void DisplayInfo_ComplexScenario_SurfacePro_200Percent()
    {
        // Arrange - Simulating a Surface Pro at 200% scaling
        var displayInfo = DisplayInfo.FromScale(2.0);

        // Act & Assert
        displayInfo.RasterizationScale.Should().Be(2.0);
        displayInfo.EffectiveDpi.Should().Be(192.0);
        displayInfo.IsHighDpi.Should().BeTrue();
        displayInfo.ScalingPercentage.Should().Be(200);
    }

    [Fact]
    public void DisplayInfo_ComplexScenario_QualityMultipliers()
    {
        // Arrange - Testing all quality levels with their multipliers
        var baseDisplayInfo = DisplayInfo.Standard();
        var baseDpi = baseDisplayInfo.EffectiveDpi;

        // Act
        var lowDpi = baseDpi * DisplayInfo.GetQualityMultiplier(RenderingQuality.Low);
        var mediumDpi = baseDpi * DisplayInfo.GetQualityMultiplier(RenderingQuality.Medium);
        var highDpi = baseDpi * DisplayInfo.GetQualityMultiplier(RenderingQuality.High);
        var ultraDpi = baseDpi * DisplayInfo.GetQualityMultiplier(RenderingQuality.Ultra);

        // Assert
        lowDpi.Should().BeApproximately(75.0, 0.1);   // 96 * 0.78125
        mediumDpi.Should().Be(96.0);                   // 96 * 1.0
        highDpi.Should().Be(144.0);                    // 96 * 1.5
        ultraDpi.Should().Be(192.0);                   // 96 * 2.0
    }

    [Fact]
    public void DisplayInfo_ComplexScenario_CombinedScalingAndQuality()
    {
        // Arrange - 4K monitor at 150% with Ultra quality
        var displayInfo = DisplayInfo.FromScale(1.5);
        var qualityMultiplier = DisplayInfo.GetQualityMultiplier(RenderingQuality.Ultra);

        // Act
        var effectiveDpi = displayInfo.EffectiveDpi * qualityMultiplier;

        // Assert
        displayInfo.EffectiveDpi.Should().Be(144.0);  // 96 * 1.5
        effectiveDpi.Should().Be(288.0);               // 144 * 2.0
    }

    [Fact]
    public void DisplayInfo_EdgeCase_VeryLowDpi()
    {
        // Arrange - Testing minimum DPI boundary
        var displayInfo = DisplayInfo.Create(0.5208, 50.0);

        // Act & Assert
        displayInfo.EffectiveDpi.Should().Be(50.0);
        displayInfo.RasterizationScale.Should().BeApproximately(0.5208, 0.0001);
        displayInfo.IsHighDpi.Should().BeFalse();
    }

    [Fact]
    public void DisplayInfo_EdgeCase_VeryHighDpi()
    {
        // Arrange - Testing maximum DPI boundary
        var displayInfo = DisplayInfo.Create(6.0, 576.0);

        // Act & Assert
        displayInfo.EffectiveDpi.Should().Be(576.0);
        displayInfo.RasterizationScale.Should().Be(6.0);
        displayInfo.IsHighDpi.Should().BeTrue();
        displayInfo.ScalingPercentage.Should().Be(600);
    }

    #endregion

    #region RenderingQuality Enum Tests

    [Fact]
    public void RenderingQuality_AllValuesAreDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues<RenderingQuality>();

        // Assert
        values.Should().Contain(RenderingQuality.Auto);
        values.Should().Contain(RenderingQuality.Low);
        values.Should().Contain(RenderingQuality.Medium);
        values.Should().Contain(RenderingQuality.High);
        values.Should().Contain(RenderingQuality.Ultra);
        values.Should().HaveCount(5);
    }

    [Fact]
    public void RenderingQuality_HasExpectedIntegerValues()
    {
        // Arrange & Act & Assert
        ((int)RenderingQuality.Auto).Should().Be(0);
        ((int)RenderingQuality.Low).Should().Be(1);
        ((int)RenderingQuality.Medium).Should().Be(2);
        ((int)RenderingQuality.High).Should().Be(3);
        ((int)RenderingQuality.Ultra).Should().Be(4);
    }

    #endregion
}
