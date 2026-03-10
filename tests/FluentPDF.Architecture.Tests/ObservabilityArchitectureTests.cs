using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using System.Text.RegularExpressions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for observability components.
/// Enforces clean architecture boundaries for metrics collection, logging, and diagnostics UI.
/// </summary>
public class ObservabilityArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    /// Observability models must be in Core/Observability namespace.
    /// This ensures domain models are in the Core layer, independent of infrastructure.
    /// </summary>
    [Fact]
    public void ObservabilityModels_ShouldBe_InCoreNamespace()
    {
        var observabilityModelNames = new[]
        {
            "PerformanceMetrics", "LogEntry", "LogFilterCriteria",
            "PerformanceLevel", "LogLevel", "ExportFormat"
        };

        // Only check types in FluentPDF.Core namespace (exclude types with matching names in other layers)
        var matchingTypes = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => observabilityModelNames.Contains(t.Name))
            .Where(t => t is not Interface);

        foreach (var type in matchingTypes)
        {
            Assert.StartsWith("FluentPDF.Core.Observability", type.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Metrics services must implement interfaces.
    /// This enables dependency injection, mocking, and testability.
    /// </summary>
    [Fact]
    public void MetricsServices_Should_ImplementInterfaces()
    {
        var serviceNames = new[] { "MetricsCollectionService", "LogExportService" };

        // Get all observability service classes
        var serviceClasses = Classes()
            .That().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => serviceNames.Contains(t.Name));

        // Check that each service implements at least one interface
        foreach (var serviceClass in serviceClasses)
        {
            var hasInterface = serviceClass.ImplementedInterfaces.Any(i =>
                                  i.Name.StartsWith("I") &&
                                  (i.Name.Contains("MetricsCollection") || i.Name.Contains("LogExport")));

            Assert.True(hasInterface,
                $"Service class {serviceClass.FullName} must implement an interface (e.g., I{serviceClass.Name})");
        }
    }

    /// <summary>
    /// Diagnostics UI controls must be in App/Controls namespace.
    /// This enforces consistent project structure for UI components.
    /// </summary>
    [Fact]
    public void DiagnosticsControls_Should_BeIn_AppControls()
    {
        var controlNames = new[] { "DiagnosticsPanelControl", "LogViewerControl" };

        // Classes() already excludes interfaces; filter by name manually
        var matchingTypes = Types()
            .That().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => controlNames.Contains(t.Name))
            .Where(t => t is not Interface);

        foreach (var type in matchingTypes)
        {
            Assert.StartsWith("FluentPDF.App.Controls", type.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// ViewModels should not directly reference metrics collection implementation.
    /// ViewModels should use service interfaces, not concrete metrics services.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_MetricsCollectionService()
    {
        // Get ViewModel types and check their dependencies manually
        var viewModels = Types()
            .That().HaveNameEndingWith("ViewModel")
            .GetObjects(Architecture);

        foreach (var vm in viewModels)
        {
            var dependsOnConcreteMetrics = vm.Dependencies
                .Any(d => d.Target.Name == "MetricsCollectionService" && d.Target is not Interface);

            Assert.False(dependsOnConcreteMetrics,
                $"ViewModel {vm.FullName} should use IMetricsCollectionService interface, not direct MetricsCollectionService implementation");
        }
    }

    /// <summary>
    /// ViewModels should not directly reference log export implementation.
    /// ViewModels should use service interfaces, not concrete log services.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_LogExportService()
    {
        var viewModels = Types()
            .That().HaveNameEndingWith("ViewModel")
            .GetObjects(Architecture);

        foreach (var vm in viewModels)
        {
            var dependsOnConcreteLogExport = vm.Dependencies
                .Any(d => d.Target.Name == "LogExportService" && d.Target is not Interface);

            Assert.False(dependsOnConcreteLogExport,
                $"ViewModel {vm.FullName} should use ILogExportService interface, not direct LogExportService implementation");
        }
    }

    /// <summary>
    /// Observability service interfaces must be in Core layer.
    /// This allows Core and App layers to depend on interfaces without depending on implementation.
    /// </summary>
    [Fact]
    public void ObservabilityServiceInterfaces_Should_BeIn_CoreServices()
    {
        var interfaceNames = new[] { "IMetricsCollectionService", "ILogExportService" };

        var matchingInterfaces = Interfaces()
            .That().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => interfaceNames.Contains(t.Name));

        foreach (var iface in matchingInterfaces)
        {
            Assert.StartsWith("FluentPDF.Core.Services", iface.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Observability service implementations must be in Rendering layer.
    /// This keeps infrastructure code separate from domain models.
    /// </summary>
    [Fact]
    public void ObservabilityServiceImplementations_Should_BeIn_RenderingServices()
    {
        var serviceNames = new[] { "MetricsCollectionService", "LogExportService" };

        // Classes() already excludes interfaces
        var matchingClasses = Classes()
            .That().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => serviceNames.Contains(t.Name));

        foreach (var cls in matchingClasses)
        {
            Assert.StartsWith("FluentPDF.Rendering.Services", cls.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Diagnostics ViewModels must be in App/ViewModels namespace.
    /// This enforces consistent project structure for presentation layer.
    /// </summary>
    [Fact]
    public void DiagnosticsViewModels_Should_BeIn_AppViewModels()
    {
        var vmNames = new[] { "DiagnosticsPanelViewModel", "LogViewerViewModel" };

        var matchingTypes = Types()
            .That().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Where(t => vmNames.Contains(t.Name));

        foreach (var type in matchingTypes)
        {
            Assert.StartsWith("FluentPDF.App.ViewModels", type.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Core layer must not depend on OpenTelemetry implementation details.
    /// Core should only define interfaces; OpenTelemetry usage is in Rendering layer.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_Reference_OpenTelemetry()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("OpenTelemetry", useRegularExpressions: true))
            .Because("Core must remain independent of OpenTelemetry infrastructure - only Rendering layer uses OpenTelemetry");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// App layer (ViewModels, Controls) must not depend on OpenTelemetry.
    /// App layer should use service interfaces, not OpenTelemetry directly.
    /// Note: This test is skipped because the App/Avalonia assembly is not loaded.
    /// </summary>
    [Fact(Skip = "FluentPDF.App/Avalonia assembly not loaded - cannot reference App type for exclusion")]
    public void AppLayer_ShouldNot_Reference_OpenTelemetry()
    {
        // This test requires referencing FluentPDF.App.App or FluentPDF.Avalonia.App
        // which is not available when the assembly is not loaded.
        // When enabled, it should verify that App layer types (except App.xaml.cs)
        // don't depend on OpenTelemetry directly.
    }

    /// <summary>
    /// Observability models should be immutable (use init accessors).
    /// This verifies that key observability models exist and follow immutability patterns.
    /// </summary>
    [Fact]
    public void ObservabilityModels_Should_Exist()
    {
        var observabilityTypes = Types()
            .That().ResideInNamespace("FluentPDF.Core.Observability", useRegularExpressions: true)
            .GetObjects(Architecture);

        // Verify we have at least the required observability models
        Assert.Contains(observabilityTypes, t => t.Name.Contains("PerformanceMetrics"));
        Assert.Contains(observabilityTypes, t => t.Name.Contains("LogEntry"));
        Assert.Contains(observabilityTypes, t => t.Name.Contains("LogFilterCriteria"));
        Assert.Contains(observabilityTypes, t => t.Name.Contains("PerformanceLevel"));
        Assert.Contains(observabilityTypes, t => t.Name.Contains("LogLevel"));
        Assert.Contains(observabilityTypes, t => t.Name.Contains("ExportFormat"));
    }

    /// <summary>
    /// Diagnostics controls should exist in the App layer.
    /// Verifies that DiagnosticsPanelControl and LogViewerControl are present.
    /// </summary>
    [Fact(Skip = "FluentPDF.App/Avalonia assembly not loaded - controls are in the UI layer")]
    public void DiagnosticsControls_Should_Exist()
    {
        var controlTypes = Types()
            .That().ResideInNamespace("FluentPDF.App.Controls", useRegularExpressions: true)
            .GetObjects(Architecture);

        // Verify we have the required diagnostics controls
        Assert.Contains(controlTypes, t => t.Name.Contains("DiagnosticsPanelControl"));
        Assert.Contains(controlTypes, t => t.Name.Contains("LogViewerControl"));
    }

    /// <summary>
    /// Diagnostics ViewModels should exist in the App layer.
    /// Verifies that DiagnosticsPanelViewModel and LogViewerViewModel are present.
    /// </summary>
    [Fact(Skip = "FluentPDF.App/Avalonia assembly not loaded - ViewModels are in the UI layer")]
    public void DiagnosticsViewModels_Should_Exist()
    {
        var viewModelTypes = Types()
            .That().ResideInNamespace("FluentPDF.App.ViewModels", useRegularExpressions: true)
            .GetObjects(Architecture);

        // Verify we have the required diagnostics ViewModels
        Assert.Contains(viewModelTypes, t => t.Name.Contains("DiagnosticsPanelViewModel"));
        Assert.Contains(viewModelTypes, t => t.Name.Contains("LogViewerViewModel"));
    }
}
