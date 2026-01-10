using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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
            .And().HaveNameContaining("SafeHandle")
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
    }

    /// <summary>
    /// Text service implementations should reside in Rendering.Services namespace.
    /// Text-related service implementations belong in the Rendering layer.
    /// </summary>
    [Fact]
    public void TextServices_Should_ResideIn_RenderingServices()
    {
        var rule = Classes()
            .That().HaveNameMatching("^TextExtractionService$|^TextSearchService$")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FluentPDF.Rendering.Services")
            .Because("Text service implementations belong in the Rendering layer");

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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

        rule.Check(Architecture);
    }

    /// <summary>
    /// Text services should be sealed.
    /// Services should be sealed unless designed for inheritance.
    /// </summary>
    [Fact]
    public void TextServices_Should_BeSealed()
    {
        var rule = Classes()
            .That().HaveNameMatching("^TextExtractionService$|^TextSearchService$")
            .And().AreNotInterfaces()
            .And().ResideInNamespace("FluentPDF.Rendering.Services")
            .Should().BeSealed()
            .Because("Services should be sealed unless designed for inheritance");

        rule.Check(Architecture);
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

        rule.Check(Architecture);
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
                .And().HaveNameMatching(".*Text.*"))
            .Because("App layer should only use text services, not text interop classes directly");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Text-related types should not depend on form types.
    /// Text extraction/search and forms are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Forms()
    {
        var rule = Types()
            .That().HaveNameMatching(".*Text.*|.*Search.*")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveNameMatching(".*Form.*"))
            .Because("Text functionality is independent of form handling");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Text-related types should not depend on bookmark types.
    /// Text extraction/search and bookmarks are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Bookmarks()
    {
        var rule = Types()
            .That().HaveNameMatching(".*Text.*|.*Search.*")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveNameMatching(".*Bookmark.*"))
            .Because("Text functionality is independent of bookmark extraction");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Text-related types should not depend on conversion types.
    /// Text extraction/search and document conversion are separate concerns.
    /// </summary>
    [Fact]
    public void TextTypes_ShouldNot_DependOn_Conversion()
    {
        var rule = Types()
            .That().HaveNameMatching(".*Text.*|.*Search.*")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveNameMatching(".*Conversion.*")
                .Or().HaveNameMatching(".*Docx.*"))
            .Because("Text functionality is independent of document conversion");

        rule.Check(Architecture);
    }

    /// <summary>
    /// SafePdfTextPageHandle should inherit from SafeHandle.
    /// This ensures proper native resource management.
    /// </summary>
    [Fact]
    public void SafePdfTextPageHandle_Should_InheritFrom_SafeHandle()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Interop.SafePdfTextPageHandle")
            .Should().BeAssignableTo("System.Runtime.InteropServices.SafeHandle")
            .Because("Text page handles must use SafeHandle for proper resource management");

        rule.Check(Architecture);
    }
}
