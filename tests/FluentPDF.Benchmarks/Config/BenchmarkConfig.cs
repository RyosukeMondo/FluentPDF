using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace FluentPDF.Benchmarks.Config;

/// <summary>
/// Shared benchmark configuration with memory diagnostics and multiple exporters.
/// Enforces Release-only execution with proper JIT optimizations.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        // Add memory diagnostics (managed allocations)
        AddDiagnoser(MemoryDiagnoser.Default);

        // Add native memory profiler (Windows only) - requires BenchmarkDotNet.Diagnostics.Windows package
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // NativeMemoryProfiler is in the BenchmarkDotNet.Diagnostics.Windows package
                var nativeMemoryProfilerType = Type.GetType("BenchmarkDotNet.Diagnostics.Windows.NativeMemoryProfiler, BenchmarkDotNet.Diagnostics.Windows");
                if (nativeMemoryProfilerType != null)
                {
                    var nativeMemoryProfiler = Activator.CreateInstance(nativeMemoryProfilerType);
                    if (nativeMemoryProfiler is IDiagnoser diagnoser)
                    {
                        AddDiagnoser(diagnoser);
                    }
                }
            }
            catch
            {
                // If NativeMemoryProfiler is not available, continue without it
            }
        }

        // Add exporters for multiple formats
        AddExporter(JsonExporter.Full);
        AddExporter(HtmlExporter.Default);
        AddExporter(MarkdownExporter.GitHub);

        // Add standard columns (includes Mean, Median, StdDev, Min, Max)
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);

        // Configure job with Release mode and platform settings
        AddJob(Job.Default
            .WithPlatform(BenchmarkDotNet.Environments.Platform.X64)
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .AsDefault());

        // Enforce Release mode - fail if running in Debug
        AddValidator(JitOptimizationsValidator.FailOnError);

        // Add baseline validator
        AddValidator(BaselineValidator.FailOnError);
    }
}
