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
    protected static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(FluentPDF.Core.Placeholder).Assembly,
            typeof(FluentPDF.App.App).Assembly,
            typeof(FluentPDF.Rendering.Class1).Assembly
        ).Build();
}
