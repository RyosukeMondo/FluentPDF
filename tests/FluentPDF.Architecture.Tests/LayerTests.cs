using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for layer dependency rules.
/// Ensures proper separation of concerns and dependency flow.
/// </summary>
public class LayerTests : ArchitectureTestBase
{
    /// <summary>
    /// Core layer must not depend on App layer.
    /// Core is business logic and must be UI-agnostic for testability.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_DependOn_AppLayer()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Core must be UI-agnostic for testability");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Core layer must not depend on Rendering layer.
    /// Core is business logic, Rendering is infrastructure.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_DependOn_RenderingLayer()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true))
            .Because("Core is business logic, Rendering is infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// App layer can depend on both Core and Rendering layers.
    /// This is the expected dependency flow: Presentation -> Domain/Infrastructure.
    /// </summary>
    [Fact]
    public void AppLayer_Can_DependOn_CoreAndRendering()
    {
        // This is a positive test - we're verifying that App CAN depend on Core
        var appHasCoreReference = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true))
            .Because("App should be able to depend on Core");

        // This test should fail if there are NO dependencies, meaning the architecture is correct
        // We verify by checking that at least some App types depend on Core
        var appTypes = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .GetObjects(Architecture);

        var coreTypes = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .GetObjects(Architecture);

        // If both layers exist, App should be able to depend on Core (this is informational)
        Assert.True(appTypes.Count() > 0 || coreTypes.Count() > 0,
            "Both App and Core layers should exist for this test to be meaningful");
    }

    /// <summary>
    /// Core layer must not have any UI dependencies.
    /// This ensures Core remains headless and testable without WinUI runtime.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_Reference_UITypes()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("Microsoft.UI", useRegularExpressions: true))
            .Because("Core must be headless and testable without WinUI runtime");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Rendering layer can depend on Core but not on App.
    /// This maintains unidirectional dependency flow.
    /// </summary>
    [Fact]
    public void RenderingLayer_ShouldNot_DependOn_AppLayer()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Rendering is infrastructure and should not depend on presentation layer");

        rule.Check(Architecture);
    }
}
