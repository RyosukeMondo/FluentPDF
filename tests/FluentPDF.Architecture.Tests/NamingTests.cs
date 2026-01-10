using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for naming conventions.
/// Ensures consistent naming patterns across the codebase.
/// </summary>
public class NamingTests : ArchitectureTestBase
{
    /// <summary>
    /// ViewModels must end with "ViewModel" suffix.
    /// This ensures consistent naming and makes ViewModels easily identifiable.
    /// </summary>
    [Fact]
    public void ViewModels_Should_EndWith_ViewModel()
    {
        var rule = Classes()
            .That().ResideInNamespace("FluentPDF.App.ViewModels", useRegularExpressions: true)
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("ViewModel")
            .Because("Consistent naming for ViewModels improves code readability");

        rule.Check(Architecture);
    }

    /// <summary>
    /// ViewModels must inherit from ObservableObject.
    /// This ensures all ViewModels use CommunityToolkit.Mvvm for property change notifications.
    /// </summary>
    [Fact]
    public void ViewModels_Should_InheritFrom_ObservableObject()
    {
        var rule = Classes()
            .That().HaveNameEndingWith("ViewModel")
            .And().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .And().AreNotAbstract()
            .Should().Inherit("CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
            .Because("ViewModels must use CommunityToolkit.Mvvm for MVVM pattern");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Service implementations must end with "Service" suffix.
    /// This ensures consistent naming for service classes.
    /// </summary>
    [Fact]
    public void Services_Should_EndWith_Service()
    {
        var rule = Classes()
            .That().ResideInNamespace("FluentPDF.*.Services", useRegularExpressions: true)
            .And().AreNotInterfaces()
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("Service")
            .Because("Services should have consistent naming conventions");

        rule.Check(Architecture);
    }

    /// <summary>
    /// All interfaces must start with "I" prefix.
    /// This follows standard .NET naming conventions.
    /// </summary>
    [Fact]
    public void Interfaces_Should_StartWith_I()
    {
        var rule = Interfaces()
            .That().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().HaveNameStartingWith("I")
            .Because("Interfaces should follow .NET naming conventions with 'I' prefix");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Error types must end with "Error" suffix.
    /// This makes error types easily identifiable in the codebase.
    /// </summary>
    [Fact]
    public void ErrorTypes_Should_EndWith_Error()
    {
        var rule = Classes()
            .That().ResideInNamespace("FluentPDF.Core.ErrorHandling", useRegularExpressions: true)
            .And().AreNotAbstract()
            .And().AreNotInterfaces()
            .And().DoNotHaveNameMatching(".*Category$")
            .And().DoNotHaveNameMatching(".*Severity$")
            .Should().HaveNameEndingWith("Error")
            .Because("Error types should have consistent naming for easy identification");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Test classes must end with "Tests" suffix.
    /// This ensures consistent naming for test classes.
    /// </summary>
    [Fact]
    public void TestClasses_Should_EndWith_Tests()
    {
        var rule = Classes()
            .That().ResideInNamespace("FluentPDF.*Tests", useRegularExpressions: true)
            .And().AreNotAbstract()
            .And().DoNotHaveNameMatching(".*Base$")
            .Should().HaveNameEndingWith("Tests")
            .Because("Test classes should follow consistent naming conventions");

        rule.Check(Architecture);
    }
}
