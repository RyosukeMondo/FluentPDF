using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using System.Runtime.InteropServices;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Architecture tests for form filling components.
/// Enforces clean architecture boundaries for form field detection,
/// validation, and user interaction.
/// </summary>
public class FormArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    /// Form P/Invoke declarations must be isolated in Rendering.Interop namespace.
    /// This ensures native form interop is contained and testable via mocking at service layer.
    /// </summary>
    [Fact]
    public void FormPInvoke_ShouldOnly_ExistIn_RenderingInterop()
    {
        var rule = Methods()
            .That().HaveAttribute(typeof(DllImportAttribute).FullName!)
            .And().HaveNameMatching(".*Form.*|.*FPDF.*Form.*")
            .Should().BeDeclaredIn(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("Form P/Invoke declarations must be isolated in Rendering.Interop namespace for proper encapsulation");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormFieldControl must reside in App.Controls namespace.
    /// This ensures UI controls are properly organized in the presentation layer.
    /// </summary>
    [Fact]
    public void FormFieldControl_Should_BeIn_AppControls()
    {
        var rule = Classes()
            .That().HaveName("FormFieldControl")
            .Should().ResideInNamespace("FluentPDF.App.Controls")
            .Because("Form controls belong in the App.Controls namespace");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormFieldViewModel must not directly reference PDFium interop layer.
    /// ViewModels should use service interfaces, not direct PDFium access.
    /// </summary>
    [Fact]
    public void FormFieldViewModel_ShouldNot_Reference_FormInterop()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.App.ViewModels.FormFieldViewModel")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("ViewModels should use service interfaces (IPdfFormService, IFormValidationService), not direct PDFium access");

        rule.Check(Architecture);
    }

    /// <summary>
    /// PdfFormService must implement IPdfFormService interface.
    /// This ensures the service is properly abstracted for dependency injection and testing.
    /// </summary>
    [Fact]
    public void PdfFormService_Should_ImplementInterface()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Services.PdfFormService")
            .Should().ImplementInterface("FluentPDF.Core.Services.IPdfFormService")
            .Because("PdfFormService must be abstracted for dependency injection and testing");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormValidationService must implement IFormValidationService interface.
    /// This ensures the service is properly abstracted for dependency injection and testing.
    /// </summary>
    [Fact]
    public void FormValidationService_Should_ImplementInterface()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Rendering.Services.FormValidationService")
            .Should().ImplementInterface("FluentPDF.Core.Services.IFormValidationService")
            .Because("FormValidationService must be abstracted for dependency injection and testing");

        rule.Check(Architecture);
    }

    /// <summary>
    /// IPdfFormService interface should reside in Core.Services namespace.
    /// Service interfaces belong in the Core layer for proper layering.
    /// </summary>
    [Fact]
    public void IPdfFormService_Should_ResideIn_CoreServices()
    {
        var rule = Interfaces()
            .That().HaveName("IPdfFormService")
            .Should().ResideInNamespace("FluentPDF.Core.Services")
            .Because("Service interfaces should be defined in the Core layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// IFormValidationService interface should reside in Core.Services namespace.
    /// Service interfaces belong in the Core layer for proper layering.
    /// </summary>
    [Fact]
    public void IFormValidationService_Should_ResideIn_CoreServices()
    {
        var rule = Interfaces()
            .That().HaveName("IFormValidationService")
            .Should().ResideInNamespace("FluentPDF.Core.Services")
            .Because("Service interfaces should be defined in the Core layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Form service implementations should reside in Rendering.Services namespace.
    /// Form-related service implementations belong in the Rendering layer.
    /// </summary>
    [Fact]
    public void FormServices_Should_ResideIn_RenderingServices()
    {
        var rule = Classes()
            .That().HaveNameMatching(".*FormService$|.*FormValidationService$")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FluentPDF.Rendering.Services")
            .Because("Form service implementations belong in the Rendering layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormFieldViewModel should reside in App.ViewModels namespace.
    /// ViewModels belong in the App layer presentation logic.
    /// </summary>
    [Fact]
    public void FormFieldViewModel_Should_ResideIn_AppViewModels()
    {
        var rule = Classes()
            .That().HaveName("FormFieldViewModel")
            .Should().ResideInNamespace("FluentPDF.App.ViewModels")
            .Because("ViewModels belong in the App layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormFieldViewModel should inherit from ObservableObject.
    /// This ensures MVVM data binding works correctly.
    /// </summary>
    [Fact]
    public void FormFieldViewModel_Should_Inherit_ObservableObject()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.App.ViewModels.FormFieldViewModel")
            .Should().BeAssignableTo("CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
            .Because("ViewModels must inherit ObservableObject for property change notifications");

        rule.Check(Architecture);
    }

    /// <summary>
    /// PdfFormField model should have no dependencies on infrastructure layers.
    /// Domain models must remain pure and infrastructure-agnostic.
    /// </summary>
    [Fact]
    public void PdfFormField_Should_HaveNoDependencies()
    {
        var rule = Classes()
            .That().HaveFullName("FluentPDF.Core.Models.PdfFormField")
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering", useRegularExpressions: true))
            .AndShould().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true))
            .Because("Domain models should have no infrastructure dependencies");

        rule.Check(Architecture);
    }

    /// <summary>
    /// PdfFormField model should reside in Core.Models namespace.
    /// Domain models belong in the Core layer.
    /// </summary>
    [Fact]
    public void PdfFormField_Should_ResideIn_CoreModels()
    {
        var rule = Classes()
            .That().HaveName("PdfFormField")
            .Should().ResideInNamespace("FluentPDF.Core.Models")
            .Because("Domain models belong in the Core layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// FormValidationResult model should reside in Core.Models namespace.
    /// Domain models belong in the Core layer.
    /// </summary>
    [Fact]
    public void FormValidationResult_Should_ResideIn_CoreModels()
    {
        var rule = Classes()
            .That().HaveName("FormValidationResult")
            .Should().ResideInNamespace("FluentPDF.Core.Models")
            .Because("Domain models belong in the Core layer");

        rule.Check(Architecture);
    }

    /// <summary>
    /// SafePdfFormHandle must exist for native form resource management.
    /// Verifies that SafeHandle pattern is used for form environment handle.
    /// </summary>
    [Fact]
    public void SafePdfFormHandle_Should_Exist()
    {
        var safeHandleTypes = Types()
            .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
            .And().HaveNameContaining("SafeHandle")
            .GetObjects(Architecture);

        Assert.Contains(safeHandleTypes, t => t.Name.Contains("SafePdfForm"));
    }

    /// <summary>
    /// Form services should be sealed.
    /// Services should be sealed unless designed for inheritance.
    /// </summary>
    [Fact]
    public void FormServices_Should_BeSealed()
    {
        var rule = Classes()
            .That().HaveNameMatching(".*FormService$|.*FormValidationService$")
            .And().AreNotInterfaces()
            .And().ResideInNamespace("FluentPDF.Rendering.Services")
            .Should().BeSealed()
            .Because("Services should be sealed unless designed for inheritance");

        rule.Check(Architecture);
    }

    /// <summary>
    /// App layer should not directly reference form interop classes.
    /// App layer should only use form services.
    /// </summary>
    [Fact]
    public void AppLayer_ShouldNot_Reference_FormInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.App", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true)
                .And().HaveNameMatching(".*Form.*"))
            .Because("App layer should only use form services, not form interop classes directly");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Core layer must not reference form interop.
    /// Core must remain independent of Rendering infrastructure.
    /// </summary>
    [Fact]
    public void CoreLayer_ShouldNot_Reference_FormInterop()
    {
        var rule = Types()
            .That().ResideInNamespace("FluentPDF.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().ResideInNamespace("FluentPDF.Rendering.Interop", useRegularExpressions: true))
            .Because("Core must remain independent of Rendering infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Form-related types should not depend on conversion types.
    /// Forms are a separate concern from document conversion.
    /// </summary>
    [Fact]
    public void FormTypes_ShouldNot_DependOn_Conversion()
    {
        var rule = Types()
            .That().HaveNameMatching(".*Form.*")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveNameMatching(".*Conversion.*")
                .Or().HaveNameMatching(".*Docx.*"))
            .Because("Form functionality is independent of document conversion");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Form-related types should not depend on bookmark types.
    /// Forms and bookmarks are separate concerns.
    /// </summary>
    [Fact]
    public void FormTypes_ShouldNot_DependOn_Bookmarks()
    {
        var rule = Types()
            .That().HaveNameMatching(".*Form.*")
            .And().ResideInNamespace("FluentPDF", useRegularExpressions: true)
            .Should().NotDependOnAny(Types()
                .That().HaveNameMatching(".*Bookmark.*"))
            .Because("Form functionality is independent of bookmark extraction");

        rule.Check(Architecture);
    }
}
