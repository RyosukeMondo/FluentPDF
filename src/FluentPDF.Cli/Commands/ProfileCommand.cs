// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.CommandLine;

namespace FluentPDF.Cli.Commands;

/// <summary>
/// Command to profile marshalling performance.
/// </summary>
internal static class ProfileCommand
{
    public static Command Create()
    {
        var command = new Command("profile", "Profile marshalling performance");

        var baselineOption = new Option<string?>("--baseline", "Baseline file to compare against");
        var outputOption = new Option<string?>("--output", "Output file for profiling results");

        command.AddOption(baselineOption);
        command.AddOption(outputOption);

        command.SetHandler((baseline, output) =>
        {
            Console.WriteLine("Marshalling performance profiling - Not yet implemented");
            if (baseline != null)
            {
                Console.WriteLine($"Baseline: {baseline}");
            }
            if (output != null)
            {
                Console.WriteLine($"Output: {output}");
            }
            return Task.FromResult(0);
        }, baselineOption, outputOption);

        return command;
    }
}
