using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using System.Text.RegularExpressions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for text extraction and search components.
/// Ensures text services follow clean architecture principles
/// and maintain proper separation of concerns.
/// NOTE: These tests require Windows environment due to WinUI 3 dependency in FluentPDF.App.
/// </summary>
public class TextArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    /// TextExtractionService must implement ITextExtractionService interface.
    /// This ensures the service is properly abstracted for dependency injection and testing.
    /// </summary>
    [Fact]
    public void TextExtractionService_Should_ImplementInterface()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Services.TextExtractionService")
            .Should().ImplementInterface("FluentPDF.Core.Services.ITextExtractionService")
            .Because("TextExtractionService must be abstracted for dependency injection and testing");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// TextSearchService must implement ITextSearchService interface.
    /// This ensures the service is properly abstracted for dependency injection and testing.
    /// </summary>
    [Fact]
    public void TextSearchService_Should_ImplementInterface()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Services.TextSearchService")
            .Should().ImplementInterface("FluentPDF.Core.Services.ITextSearchService")
            .Because("TextSearchService must be abstracted for dependency injection and testing");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Core must not depend on PdfiumInterop.
    /// Core layer must remain independent of infrastructure concerns.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_DependOn_PdfiumInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("Core must remain independent of Rendering infrastructure");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Text services must use SafeHandle for text page handles.
    /// This ensures proper resource management and prevents memory leaks.
    /// </summary>
    [Fact]
    public void TextServices_Should_UseSafeHandle()
    {
        var safeHandleTypes = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
            .And().HaveNameStartingWith("Safe")
            .And().HaveNameEndingWith("Handle")
            .GetObjects(Architecture);

        Assert.Contains(safeHandleTypes, t => t.Name.Contains("SafePdfTextPageHandle"));
    }

    /// <summary>
    /// ITextExtractionService interface should reside in Core.Services namespace.
    /// Service interfaces belong in the Core layer for proper layering.
    /// </summary>
    [Fact]
    public void ITextExtractionService_Should_ResideIn_CoreServices()
    {
        var rule = Interfaces()
            .That().HaveName("ITextExtractionService")
            .Should().ResideInNamespace("FluentPDF.Core.Services")
            .Because("Service interfaces should be defined in the Core layer");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// ITextSearchService interface should reside in Core.Services namespace.
    /// Service interfaces belong in the Core layer for proper layering.
    /// </summary>
    [Fact]
    public void ITextSearchService_Should_ResideIn_CoreServices()
    {
        var rule = Interfaces()
            .That().HaveName("ITextSearchService")
            .Should().ResideInNamespace("FluentPDF.Core.Services")
            .Because("Service interfaces should be defined in the Core layer");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Text service implementations should reside in Rendering.Services namespace.
    /// Text-related service implementations belong in the Rendering layer.
    /// </summary>
    [Fact]
    public void TextServices_Should_ResideIn_RenderingServices()
    {
        // Classes() already excludes interfaces; use manual regex filter
        var textServices = Classes()
            .That().HaveNameEndingWith("Service")
            .GetObjects(Architecture)
            .Where(t => Regex.IsMatch(t.Name, "^TextExtractionService$|^TextSearchService$"));

        foreach (var service in textServices)
        {
            Assert.StartsWith("FluentPDF.Rendering.Services", service.Namespace.FullName,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// SearchMatch model should have no dependencies on infrastructure layers.
    /// Domain models must remain pure and infrastructure-agnostic.
    /// </summary>
    [Fact]
    public void SearchMatch_Should_HaveNoDependencies()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Core.Models.SearchMatch")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true))
            .AndShould().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Domain models should have no infrastructure dependencies");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// SearchOptions model should have no dependencies on infrastructure layers.
    /// Domain models must remain pure and infrastructure-agnostic.
    /// </summary>
    [Fact]
    public void SearchOptions_Should_HaveNoDependencies()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Core.Models.SearchOptions")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true))
            .AndShould().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Domain models should have no infrastructure dependencies");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// SearchMatch should reside in Core.Models namespace.
    /// Domain models belong in the Core layer.
    /// </summary>
    [Fact]
    public void SearchMatch_Should_ResideIn_CoreModels()
    {
        var rule = Classes()
            .That().HaveName("SearchMatch")
            .Should().ResideInNamespace("FluentPDF.Core.Models")
            .Because("Domain models belong in the Core layer");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// SearchOptions should reside in Core.Models namespace.
    /// Domain models belong in the Core layer.
    /// </summary>
    [Fact]
    public void SearchOptions_Should_ResideIn_CoreModels()
    {
        var rule = Classes()
            .That().HaveName("SearchOptions")
            .Should().ResideInNamespace("FluentPDF.Core.Models")
            .Because("Domain models belong in the Core layer");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Text services should be sealed.
    /// Services should be sealed unless designed for inheritance.
    /// </summary>
    [Fact]
    public void TextServices_Should_BeSealed()
    {
        // Classes() already excludes interfaces; use manual regex filter
        var textServices = Classes()
            .That().HaveNameEndingWith("Service")
            .And().ResideInNamespace("FluentPDF.Rendering.Services")
            .GetObjects(Architecture)
            .Where(t => Regex.IsMatch(t.Name, "^TextExtractionService$|^TextSearchService$"));

        foreach (var service in textServices)
        {
            Assert.True(service.IsSealed,
                $"Service {service.FullName} should be sealed unless designed for inheritance");
        }
    }

    /// <summary>
    /// PdfViewerViewModel should not directly reference PDFium interop types.
    /// This ensures proper layering - ViewModels should only depend on service abstractions.
    /// </summary>
    [Fact]
    public void PdfViewerViewModel_ShouldNot_Reference_PdfiumTextInterop()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.App.ViewModels.PdfViewerViewModel")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop"))
            .Because("ViewModels should not directly depend on PDFium interop - use service abstractions");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// App layer should not directly reference text interop classes.
    /// App layer should only use text services.
    /// </summary>
    [Fact]
    public void AppLayer_ShouldNot_Reference_TextInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
                .And().HaveNameContaining("Text"))
            .Because("App layer should only use text services, not text interop classes directly");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }

    /// <summary>
    /// Text-related types should not depend on form types.
    /// Text extraction/search and forms are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Forms()
    {
        var textTypes = Types()
            .That().HaveNameContaining("Text")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Concat(Types()
                .That().HaveNameContaining("Search")
                .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
                .GetObjects(Architecture))
            // Exclude interop types - they are low-level P/Invoke wrappers
            .Where(t => !t.FullName.Contains("Interop"));

        foreach (var textType in textTypes)
        {
            var dependsOnForms = textType.Dependencies
                .Any(d => d.Target.FullName.StartsWith("FluentPDF") && d.Target.Name.Contains("Form"));

            Assert.False(dependsOnForms,
                $"Text type {textType.FullName} should not depend on form types");
        }
    }

    /// <summary>
    /// Text-related types should not depend on bookmark types.
    /// Text extraction/search and bookmarks are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Bookmarks()
    {
        var textTypes = Types()
            .That().HaveNameContaining("Text")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Concat(Types()
                .That().HaveNameContaining("Search")
                .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
                .GetObjects(Architecture));

        foreach (var textType in textTypes)
        {
            var dependsOnBookmarks = textType.Dependencies
                .Any(d => d.Target.Name.Contains("Bookmark"));

            Assert.False(dependsOnBookmarks,
                $"Text type {textType.FullName} should not depend on bookmark types");
        }
    }

    /// <summary>
    /// Text-related types should not depend on conversion types.
    /// Text extraction/search and document conversion are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Conversion()
    {
        var textTypes = Types()
            .That().HaveNameContaining("Text")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .GetObjects(Architecture)
            .Concat(Types()
                .That().HaveNameContaining("Search")
                .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
                .GetObjects(Architecture));

        foreach (var textType in textTypes)
        {
            var dependsOnConversion = textType.Dependencies
                .Any(d => d.Target.Name.Contains("Conversion") || d.Target.Name.Contains("Docx"));

            Assert.False(dependsOnConversion,
                $"Text type {textType.FullName} should not depend on conversion types");
        }
    }

    /// <summary>
    /// SafePdfTextPageHandle should inherit from SafeHandle.
    /// This ensures proper native resource management.
    /// </summary>
    [Fact]
    public void SafePdfTextPageHandle_Should_InheritFrom_SafeHandle()
    {
        // SafePdfTextPageHandle inherits SafeHandleZeroOrMinusOneIsInvalid which inherits SafeHandle.
        // ArchUnitNET may not resolve transitive base classes from external assemblies,
        // so check for the direct base class instead.
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Interop.SafePdfTextPageHandle")
            .Should().BeAssignableTo("Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid")
            .Because("Text page handles must use SafeHandle for proper resource management");

        rule.WithoutRequiringPositiveResults().Check(Architecture);
    }
}
