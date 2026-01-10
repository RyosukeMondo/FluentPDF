using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using System.Runtime.InteropServices;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for PDF rendering components.
/// Enforces clean architecture boundaries and P/Invoke isolation.
/// </summary>
public class PdfRenderingArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    /// P/Invoke declarations must be isolated in Rendering.Interop namespace.
    /// This ensures native interop is contained and testable via mocking at service layer.
    /// </summary>
    [Fact]
    public void PInvoke_ShouldOnly_ExistIn_RenderingInteropNamespace()
    {
        var rule = Methods()
            .That().HaveAttribute(typeof(DllImportAttribute).FullName!)
            .Should().BeDeclaredIn(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("P/Invoke declarations must be isolated in Rendering.Interop namespace for proper encapsulation");

        rule.Check(Architecture);
    }

    /// <summary>
    /// ViewModels must not directly depend on PDFium interop layer.
    /// ViewModels should use service interfaces, not direct PDFium access.
    /// </summary>
    [Fact]
    public void ViewModels_ShouldNot_Reference_PdfiumInterop()
    {
        var rule = Types()
            .That().HaveNameEndingWith("ViewModel")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("ViewModels should use service interfaces, not direct PDFium access");

        rule.Check(Architecture);
    }

    /// <summary>
    /// All services in Rendering namespace must implement an interface.
    /// This enables dependency injection, mocking, and testability.
    /// </summary>
    [Fact]
    public void RenderingServices_Should_ImplementInterfaces()
    {
        // Get all service classes (ending with "Service" but not interface)
        var serviceClasses = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .And().HaveNameEndingWith("Service")
            .And().AreNotInterfaces()
            .GetObjects(Architecture);

        // Check that each service implements at least one interface
        foreach (var serviceClass in serviceClasses)
        {
            var hasInterface = serviceClass.ImplementsInterface != null &&
                              serviceClass.ImplementedInterfaces.Any(i =>
                                  i.Name.StartsWith("I") && i.Name.EndsWith("Service"));

            Assert.True(hasInterface,
                $"Service class {serviceClass.FullName} must implement an interface (e.g., I{serviceClass.Name})");
        }
    }

    /// <summary>
    /// Core layer must not reference PDFium interop.
    /// Core must remain independent of Rendering infrastructure.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_Reference_PdfiumInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("Core must remain independent of Rendering infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// SafeHandle types must be used for native resource management.
    /// Verifies that SafePdfDocumentHandle and SafePdfPageHandle exist and are properly structured.
    /// </summary>
    [Fact]
    public void SafeHandleTypes_Should_ExistFor_NativeResources()
    {
        var safeHandleTypes = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
            .And().HaveNameContaining("SafeHandle")
            .GetObjects(Architecture);

        // Verify we have at least the required SafeHandle types
        Assert.Contains(safeHandleTypes, t => t.Name.Contains("SafePdfDocument"));
        Assert.Contains(safeHandleTypes, t => t.Name.Contains("SafePdfPage"));
    }

    /// <summary>
    /// Service implementations must reside in Services namespace.
    /// This enforces consistent project structure.
    /// </summary>
    [Fact]
    public void Services_Should_ResideIn_ServicesNamespace()
    {
        var rule = Types()
            .That().HaveNameEndingWith("Service")
            .And().AreNotInterfaces()
            .And().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true)
            .Should().ResideInNamespace("FluentPDF.Rendering.Services", useRegularExpressions: true)
            .Because("Service implementations must be organized in Services namespace");

        rule.Check(Architecture);
    }

    /// <summary>
    /// P/Invoke methods should be internal to the Interop classes.
    /// Only wrapper methods should be public to maintain encapsulation.
    /// </summary>
    [Fact]
    public void PInvokeMethods_Should_BeInternal()
    {
        var pInvokeMethods = Methods()
            .That().HaveAttribute(typeof(DllImportAttribute).FullName!)
            .GetObjects(Architecture);

        foreach (var method in pInvokeMethods)
        {
            // P/Invoke methods should be private or internal, not public
            // This ensures they are wrapped by public managed methods
            var isPublic = method.Visibility.ToString().Contains("Public");
            Assert.False(isPublic,
                $"P/Invoke method {method.FullName} should be internal or private, not public. Wrap it with a public managed method.");
        }
    }

    /// <summary>
    /// Interop classes must not be used directly by App layer.
    /// App layer should only use services.
    /// </summary>
    [Fact]
    public void AppLayer_ShouldNot_Reference_InteropClasses()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("App layer should only use services, not interop classes directly");

        rule.Check(Architecture);
    }
}
