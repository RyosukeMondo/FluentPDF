using BenchmarkDotNet.Running;
using FluentPDF.Benchmarks.Config;
using FluentPDF.Benchmarks.Suites;

namespace FluentPDF.Benchmarks;

/// <summary>
/// Entry point for FluentPDF benchmark suite.
/// Run with: dotnet run -c Release --project tests/FluentPDF.Benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Check if running in Debug mode
        #if DEBUG
        Console.Error.WriteLine("ERROR: Benchmarks must be run in Release mode.");
        Console.Error.WriteLine("Use: dotnet run -c Release --project tests/FluentPDF.Benchmarks");
        Environment.Exit(1);
        #endif

        var config = new BenchmarkConfig();

        // If specific benchmark suite is requested via args, run it
        // Otherwise show help
        if (args.Length == 0)
        {
            Console.WriteLine("FluentPDF Benchmark Suite");
            Console.WriteLine("========================");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -c Release --project tests/FluentPDF.Benchmarks [suite]");
            Console.WriteLine();
            Console.WriteLine("Available suites:");
            Console.WriteLine("  --all              Run all benchmark suites");
            Console.WriteLine("  --rendering        Run rendering benchmarks");
            Console.WriteLine("  --memory           Run memory benchmarks");
            Console.WriteLine("  --startup          Run startup benchmarks");
            Console.WriteLine("  --navigation       Run navigation benchmarks");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --rendering");
            Console.WriteLine("  dotnet run -c Release --project tests/FluentPDF.Benchmarks -- --all");
            Console.WriteLine();
            return;
        }

        // Parse command-line arguments
        var suite = args[0].ToLowerInvariant();

        switch (suite)
        {
            case "--all":
                Console.WriteLine("Running all benchmark suites...");
                Console.WriteLine();
                Console.WriteLine("=== Rendering Benchmarks ===");
                BenchmarkRunner.Run<RenderingBenchmarks>(config);
                Console.WriteLine();
                Console.WriteLine("=== Memory Benchmarks ===");
                BenchmarkRunner.Run<MemoryBenchmarks>(config);
                Console.WriteLine();
                Console.WriteLine("=== Startup Benchmarks ===");
                BenchmarkRunner.Run<StartupBenchmarks>(config);
                Console.WriteLine();
                Console.WriteLine("=== Navigation Benchmarks ===");
                BenchmarkRunner.Run<NavigationBenchmarks>(config);
                Console.WriteLine();
                Console.WriteLine("All benchmark suites completed.");
                break;

            case "--rendering":
                Console.WriteLine("Running rendering benchmarks...");
                BenchmarkRunner.Run<RenderingBenchmarks>(config);
                break;

            case "--memory":
                Console.WriteLine("Running memory benchmarks...");
                BenchmarkRunner.Run<MemoryBenchmarks>(config);
                break;

            case "--startup":
                Console.WriteLine("Running startup benchmarks...");
                BenchmarkRunner.Run<StartupBenchmarks>(config);
                break;

            case "--navigation":
                Console.WriteLine("Running navigation benchmarks...");
                BenchmarkRunner.Run<NavigationBenchmarks>(config);
                break;

            default:
                Console.Error.WriteLine($"Unknown benchmark suite: {suite}");
                Console.Error.WriteLine("Run without arguments to see available suites.");
                Environment.Exit(1);
                break;
        }
    }
}
