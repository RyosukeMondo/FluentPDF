using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using System.Text.RegularExpressions;
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

        rule.WithoutRequiringPositiveResults().Check(Architecture);
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
            .Should().BeAssignableTo("CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
            .Because("ViewModels must use CommunityToolkit.Mvvm for MVVM pattern");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Service implementations must end with "Service" suffix.
    /// This ensures consistent naming for service classes.
    /// </summary>
    [Fact]
    public void Services_Should_EndWith_Service()
    {
        // Check classes whose name contains "Service" (to filter out DTOs, Validators, etc.)
        // and verify they end with "Service" suffix.
        var rule = Classes()
            .That().ResideInNamespace("FluentPDF.*.Services", useRegularExpressions: true)
            .And().AreNotAbstract()
            .And().HaveNameContaining("Service")
            .Should().HaveNameEndingWith("Service")
            .Because("Services should have consistent naming conventions");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
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

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Error types must end with "Error" suffix.
    /// This makes error types easily identifiable in the codebase.
    /// </summary>
    [Fact]
    public void ErrorTypes_Should_EndWith_Error()
    {
        // Use GetObjects + manual filter instead of DoNotHaveNameMatching
        var errorTypes = Classes()
            .That().ResideInNamespace("FluentPDF.Core.ErrorHandling", useRegularExpressions: true)
            .And().AreNotAbstract()
            .GetObjects(Architecture)
            .Where(t => !Regex.IsMatch(t.Name, ".*Category$"))
            .Where(t => !Regex.IsMatch(t.Name, ".*Severity$"));

        foreach (var errorType in errorTypes)
        {
            Assert.EndsWith("Error", errorType.Name);
        }
    }

    /// <summary>
    /// Test classes must end with "Tests" suffix.
    /// This ensures consistent naming for test classes.
    /// </summary>
    [Fact]
    public void TestClasses_Should_EndWith_Tests()
    {
        // Use GetObjects + manual filter instead of DoNotHaveNameMatching
        var testClasses = Classes()
            .That().ResideInNamespace("FluentPDF.*Tests", useRegularExpressions: true)
            .And().AreNotAbstract()
            .GetObjects(Architecture)
            .Where(t => !Regex.IsMatch(t.Name, ".*Base$"));

        foreach (var testClass in testClasses)
        {
            Assert.EndsWith("Tests", testClass.Name);
        }
    }
}
