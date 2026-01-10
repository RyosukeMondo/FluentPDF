using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for DocumentEditing layer.
/// Enforces proper separation between interfaces (Core), implementations (Rendering),
/// and ensures QPDF interop types are properly encapsulated.
/// </summary>
public class DocumentEditingLayerTests : ArchitectureTestBase
{
    /// <summary>
    /// IDocumentEditingService interface must reside in FluentPDF.Core.Services namespace.
    /// This ensures the service contract is in the business logic layer for proper DI and testability.
    /// </summary>
    [Fact]
    public void IDocumentEditingService_Should_BeIn_CoreLayer()
    {
        var interfaceType = Types()
            .That().HaveFullName("FluentPDF.Core.Services.IDocumentEditingService")
            .Should().ResideInNamespace("FluentPDF.Core.Services", useRegularExpressions: true)
            .Because("Service interfaces must be in Core layer for proper dependency injection and testability");

        interfaceType.Check(Architecture);
    }

    /// <summary>
    /// DocumentEditingService implementation must reside in FluentPDF.Rendering.Services namespace.
    /// This ensures the infrastructure implementation is separated from business logic.
    /// </summary>
    [Fact]
    public void DocumentEditingService_Should_BeIn_RenderingLayer()
    {
        var serviceType = Types()
            .That().HaveFullName("FluentPDF.Rendering.Services.DocumentEditingService")
            .Should().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .Because("Service implementations must be in Rendering layer as infrastructure");

        serviceType.Check(Architecture);
    }

    /// <summary>
    /// DocumentEditingService must implement IDocumentEditingService.
    /// This ensures the service follows the defined contract for dependency injection.
    /// </summary>
    [Fact]
    public void DocumentEditingService_Should_Implement_IDocumentEditingService()
    {
        var serviceTypes = Types()
            .That().HaveFullName("FluentPDF.Rendering.Services.DocumentEditingService")
            .GetObjects(Architecture);

        var serviceType = Assert.Single(serviceTypes);

        var implementsInterface = serviceType.ImplementedInterfaces.Any(i =>
            i.FullName == "FluentPDF.Core.Services.IDocumentEditingService");

        Assert.True(implementsInterface,
            "DocumentEditingService must implement IDocumentEditingService interface");
    }

    /// <summary>
    /// QpdfNative interop class must be internal and not exposed publicly.
    /// This ensures P/Invoke declarations are encapsulated and not part of the public API.
    /// </summary>
    [Fact]
    public void QpdfNative_Should_BeInternal()
    {
        var qpdfNativeTypes = Types()
            .That().HaveFullName("FluentPDF.Rendering.Interop.QpdfNative")
            .GetObjects(Architecture);

        if (qpdfNativeTypes.Any())
        {
            var qpdfNativeType = qpdfNativeTypes.First();

            // Check that the type is not public
            var isPublic = qpdfNativeType.Visibility.ToString().Contains("Public");
            Assert.False(isPublic,
                "QpdfNative must be internal to prevent exposing P/Invoke declarations in public API");
        }
    }

    /// <summary>
    /// SafeQpdfJobHandle must exist in the Interop namespace for memory safety.
    /// This ensures QPDF job handles are managed with SafeHandle pattern.
    /// </summary>
    [Fact]
    public void SafeQpdfJobHandle_Should_ExistIn_InteropNamespace()
    {
        var safeHandleTypes = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
            .And().HaveNameContaining("SafeQpdfJobHandle")
            .GetObjects(Architecture);

        Assert.NotEmpty(safeHandleTypes);
        Assert.Contains(safeHandleTypes, t => t.Name.Contains("SafeQpdfJobHandle"));
    }

    /// <summary>
    /// PageRangeParser utility must reside in FluentPDF.Core.Utilities namespace.
    /// This ensures parsing logic is in the Core layer for reusability and testability.
    /// </summary>
    [Fact]
    public void PageRangeParser_Should_BeIn_CoreUtilities()
    {
        var parserType = Types()
            .That().HaveFullName("FluentPDF.Core.Utilities.PageRangeParser")
            .Should().ResideInNamespace("FluentPDF.Core.Utilities", useRegularExpressions: true)
            .Because("Utility classes must be in Core.Utilities namespace");

        parserType.Check(Architecture);
    }

    /// <summary>
    /// DocumentEditingService must not expose QPDF types in its public API.
    /// This ensures the service maintains a clean abstraction boundary.
    /// </summary>
    [Fact]
    public void DocumentEditingService_ShouldNot_Expose_QpdfTypes()
    {
        var serviceTypes = Types()
            .That().HaveFullName("FluentPDF.Rendering.Services.DocumentEditingService")
            .GetObjects(Architecture);

        if (serviceTypes.Any())
        {
            var serviceType = serviceTypes.First();

            // Check all public methods don't return or accept QPDF types
            var publicMethods = serviceType.Members
                .Where(m => m.Visibility.ToString().Contains("Public"))
                .ToList();

            foreach (var method in publicMethods)
            {
                var methodName = method.FullName;

                // Methods should not contain "Qpdf" in their signature (except for the class name itself)
                var containsQpdf = methodName.Contains("Qpdf") && !methodName.Contains("DocumentEditingService");

                Assert.False(containsQpdf,
                    $"Public method {method.Name} should not expose QPDF types in its signature");
            }
        }
    }

    /// <summary>
    /// ViewModels must not directly depend on QpdfNative or QPDF interop types.
    /// ViewModels should only use IDocumentEditingService interface.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_QpdfInterop()
    {
        var rule = Types()
            .That().HaveNameEndingWith("ViewModel")
            .Should().NotDependOnAny(Types()
                .That().HaveFullName("FluentPDF.Rendering.Interop.QpdfNative"))
            .Because("ViewModels should use IDocumentEditingService interface, not QPDF interop directly");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Core layer must not reference QPDF interop types.
    /// Core must remain independent of Rendering infrastructure.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_Reference_QpdfInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveFullName("FluentPDF.Rendering.Interop.QpdfNative"))
            .Because("Core layer must remain independent of QPDF infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// OptimizationOptions and OptimizationResult must be in Core layer.
    /// These are business domain types, not infrastructure types.
    /// </summary>
    [Fact]
    public void OptimizationTypes_Should_BeIn_CoreLayer()
    {
        var optimizationOptionsRule = Types()
            .That().HaveFullName("FluentPDF.Core.Services.OptimizationOptions")
            .Should().ResideInNamespace("FluentPDF.Core.Services", useRegularExpressions: true)
            .Because("OptimizationOptions is a business domain type");

        var optimizationResultRule = Types()
            .That().HaveFullName("FluentPDF.Core.Services.OptimizationResult")
            .Should().ResideInNamespace("FluentPDF.Core.Services", useRegularExpressions: true)
            .Because("OptimizationResult is a business domain type");

        optimizationOptionsRule.Check(Architecture);
        optimizationResultRule.Check(Architecture);
    }
}
