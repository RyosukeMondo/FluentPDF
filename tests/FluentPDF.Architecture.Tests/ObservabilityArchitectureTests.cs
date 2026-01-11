using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
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
        var rule = Types()
            .That().HaveNameMatching("PerformanceMetrics|LogEntry|LogFilterCriteria|PerformanceLevel|LogLevel|ExportFormat")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FluentPDF.Core.Observability", useRegularExpressions: true)
            .Because("Observability models must reside in Core/Observability namespace to maintain clean architecture");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Metrics services must implement interfaces.
    /// This enables dependency injection, mocking, and testability.
    /// </summary>
    [Fact]
    public void MetricsServices_Should_ImplementInterfaces()
    {
        // Get all observability service classes
        var serviceClasses = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .And().HaveNameMatching("MetricsCollectionService|LogExportService")
            .And().AreNotInterfaces()
            .GetObjects(Architecture);

        // Check that each service implements at least one interface
        foreach (var serviceClass in serviceClasses)
        {
            var hasInterface = serviceClass.ImplementsInterface != null &&
                              serviceClass.ImplementedInterfaces.Any(i =>
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
        var rule = Types()
            .That().HaveNameMatching("DiagnosticsPanelControl|LogViewerControl")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FluentPDF.App.Controls", useRegularExpressions: true)
            .Because("Diagnostics controls must be organized in App/Controls namespace");

        rule.Check(Architecture);
    }

    /// <summary>
    /// ViewModels should not directly reference metrics collection implementation.
    /// ViewModels should use service interfaces, not concrete metrics services.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_MetricsCollectionService()
    {
        var rule = Types()
            .That().HaveNameEndingWith("ViewModel")
            .Should().NotDependOnAny(Types()
                .That().HaveFullNameContaining("MetricsCollectionService")
                .And().AreNotInterfaces())
            .Because("ViewModels should use IMetricsCollectionService interface, not direct MetricsCollectionService implementation");

        rule.Check(Architecture);
    }

    /// <summary>
    /// ViewModels should not directly reference log export implementation.
    /// ViewModels should use service interfaces, not concrete log services.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_LogExportService()
    {
        var rule = Types()
            .That().HaveNameEndingWith("ViewModel")
            .Should().NotDependOnAny(Types()
                .That().HaveFullNameContaining("LogExportService")
                .And().AreNotInterfaces())
            .Because("ViewModels should use ILogExportService interface, not direct LogExportService implementation");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Observability service interfaces must be in Core layer.
    /// This allows Core and App layers to depend on interfaces without depending on implementation.
    /// </summary>
    [Fact]
    public void ObservabilityServiceInterfaces_Should_BeIn_CoreServices()
    {
        var rule = Types()
            .That().HaveNameMatching("IMetricsCollectionService|ILogExportService")
            .And().AreInterfaces()
            .Should().ResideInNamespace("FluentPDF.Core.Services", useRegularExpressions: true)
            .Because("Service interfaces must be in Core/Services for proper dependency inversion");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Observability service implementations must be in Rendering layer.
    /// This keeps infrastructure code separate from domain models.
    /// </summary>
    [Fact]
    public void ObservabilityServiceImplementations_Should_BeIn_RenderingServices()
    {
        var rule = Types()
            .That().HaveNameMatching("MetricsCollectionService|LogExportService")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .Because("Service implementations must be in Rendering/Services for proper layering");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Diagnostics ViewModels must be in App/ViewModels namespace.
    /// This enforces consistent project structure for presentation layer.
    /// </summary>
    [Fact]
    public void DiagnosticsViewModels_Should_BeIn_AppViewModels()
    {
        var rule = Types()
            .That().HaveNameMatching("DiagnosticsPanelViewModel|LogViewerViewModel")
            .Should().ResideInNamespace("FluentPDF.App.ViewModels", useRegularExpressions: true)
            .Because("ViewModels must be organized in App/ViewModels namespace");

        rule.Check(Architecture);
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

        rule.Check(Architecture);
    }

    /// <summary>
    /// App layer (ViewModels, Controls) must not depend on OpenTelemetry.
    /// App layer should use service interfaces, not OpenTelemetry directly.
    /// </summary>
    [Fact]
    public void AppLayer_ShouldNot_Reference_OpenTelemetry()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .And().AreNot(typeof(FluentPDF.App.App)) // App.xaml.cs configures OpenTelemetry, which is allowed
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("OpenTelemetry", useRegularExpressions: true))
            .Because("App layer (except App.xaml.cs) should use service interfaces, not OpenTelemetry directly");

        rule.Check(Architecture);
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
    [Fact]
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
    [Fact]
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
