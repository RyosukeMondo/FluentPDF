using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Base class for ArchUnitNET architecture tests.
/// Initializes the Architecture object for analyzing assemblies.
/// </summary>
public abstract class ArchitectureTestBase
{
    /// <summary>
    /// The Architecture object containing all types from the FluentPDF solution.
    /// Used by ArchUnitNET tests to analyze dependencies and naming conventions.
    /// </summary>
    protected static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(FluentPDF.Core.Utilities.PageRangeParser).Assembly,
            // Note: App assembly temporarily disabled - requires pdfium.dll native dependency
            // typeof(FluentPDF.App.App).Assembly,
            typeof(FluentPDF.Rendering.Services.PdfRenderingService).Assembly
            // Note: Avalonia assembly temporarily disabled due to build errors
            // typeof(FluentPDF.Avalonia.App).Assembly
        ).Build();
}
