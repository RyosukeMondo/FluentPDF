using BenchmarkDotNet.Running;
using FluentPDF.Benchmarks.Config;

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
                Console.WriteLine("Note: Individual suites will be added as they are implemented.");
                Console.WriteLine("Currently no benchmark suites are available.");
                break;

            case "--rendering":
                Console.WriteLine("Rendering benchmarks will be added in a future task.");
                break;

            case "--memory":
                Console.WriteLine("Memory benchmarks will be added in a future task.");
                break;

            case "--startup":
                Console.WriteLine("Startup benchmarks will be added in a future task.");
                break;

            case "--navigation":
                Console.WriteLine("Navigation benchmarks will be added in a future task.");
                break;

            default:
                Console.Error.WriteLine($"Unknown benchmark suite: {suite}");
                Console.Error.WriteLine("Run without arguments to see available suites.");
                Environment.Exit(1);
                break;
        }
    }
}
