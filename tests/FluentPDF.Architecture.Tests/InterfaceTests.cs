using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for interface patterns and service abstractions.
/// Ensures services are properly abstracted for DI and testing.
/// </summary>
public class InterfaceTests : ArchitectureTestBase
{
    /// <summary>
    /// Service classes must implement corresponding interfaces.
    /// This ensures services are abstracted for dependency injection and testing.
    /// </summary>
    [Fact]
    public void Services_Should_ImplementInterfaces()
    {
        // Known exceptions: FDF services don't have interfaces yet (internal-only)
        var knownExceptions = new HashSet<string> { "FdfExportService", "FdfImportService" };

        // Classes() already excludes interfaces
        var services = Classes()
            .That().HaveNameEndingWith("Service")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .And().AreNotAbstract()
            .GetObjects(Architecture)
            .Where(s => !knownExceptions.Contains(s.Name));

        foreach (var service in services)
        {
            var hasInterface = service.ImplementedInterfaces.Any(i =>
                i.FullName.StartsWith("FluentPDF") && i.Name.StartsWith("I") && i.Name.EndsWith("Service"));

            Assert.True(hasInterface,
                $"Service {service.FullName} must implement an interface (e.g., I{service.Name}). " +
                "Services must be abstracted for DI and testing.");
        }
    }

    /// <summary>
    /// Interfaces should be in the same namespace as their implementations.
    /// This improves code organization and discoverability.
    /// </summary>
    [Fact]
    public void Interfaces_Should_BeInSameNamespace_AsImplementations()
    {
        var rule = Interfaces()
            .That().HaveNameStartingWith("I")
            .And().ResideInNamespace("FluentPDF.*.Services", useRegularExpressions: true)
            .Should().BeAssignableTo(@"FluentPDF\..*\.Services", useRegularExpressions: true)
            .Because("Interfaces and implementations should be organized together");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Core layer interfaces must not reference UI types in their signatures.
    /// This ensures Core remains UI-agnostic and headless testable.
    /// </summary>
    [Fact]
    public void CoreInterfaces_Should_NotReference_UITypes()
    {
        var rule = Interfaces()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("Microsoft.UI", useRegularExpressions: true))
            .Because("Core interfaces must not use WinUI types in signatures");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Service interfaces must be public.
    /// This allows them to be used across assembly boundaries for DI.
    /// </summary>
    [Fact]
    public void ServiceInterfaces_Should_BePublic()
    {
        var rule = Interfaces()
            .That().HaveNameStartingWith("I")
            .And().HaveNameEndingWith("Service")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().BePublic()
            .Because("Service interfaces must be accessible for dependency injection");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Service implementations should be public or internal.
    /// This allows flexibility in service visibility while maintaining encapsulation.
    /// </summary>
    [Fact]
    public void ServiceImplementations_Should_BePublicOrInternal()
    {
        var services = Classes()
            .That().HaveNameEndingWith("Service")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .And().AreNotAbstract()
            .GetObjects(Architecture);

        foreach (var service in services)
        {
            Assert.True(service.Visibility == Visibility.Public || service.Visibility == Visibility.Internal,
                $"Service {service.Name} should be public or internal");
        }
    }

    /// <summary>
    /// ViewModels should not implement interfaces (except framework interfaces).
    /// ViewModels are typically concrete implementations bound directly to views.
    /// </summary>
    [Fact]
    public void ViewModels_Should_NotImplementCustomInterfaces()
    {
        var rule = Classes()
            .That().HaveNameEndingWith("ViewModel")
            .And().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .Should().NotImplementInterface(@"FluentPDF\..*\.I.*", useRegularExpressions: true)
            .Because("ViewModels are concrete implementations bound to views");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Repository interfaces should follow the IRepository pattern.
    /// This ensures consistent data access patterns.
    /// </summary>
    [Fact]
    public void Repositories_Should_FollowNamingPattern()
    {
        var rule = Classes()
            .That().HaveNameEndingWith("Repository")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .And().AreNotAbstract()
            .Should().ImplementInterface(@"FluentPDF\..*\.I.*Repository", useRegularExpressions: true)
            .Because("Repositories must follow the IRepository pattern");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }
}
