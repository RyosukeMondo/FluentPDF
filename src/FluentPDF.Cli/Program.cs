// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.CommandLine;
using FluentPDF.Cli.Commands;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FluentPDF.Cli;

/// <summary>
/// FluentPDF CLI tool - console application for PDF diagnostics and verification.
/// Runs in headless environments without GUI dependencies.
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentPDF_CLI.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        try
        {
            // Initialize PDFium library
            Log.Information("Initializing PDFium library...");
            var initialized = PdfiumInterop.Initialize();
            if (!initialized)
            {
                Log.Fatal("Failed to initialize PDFium library");
                Console.Error.WriteLine("ERROR: Failed to initialize PDFium. Ensure pdfium.dll is available.");
                return 1;
            }
            Log.Information("PDFium library initialized successfully");

            // Build root command
            var rootCommand = new RootCommand("FluentPDF CLI - PDF diagnostics and verification tool");

            // Add subcommands
            rootCommand.AddCommand(DiagnosticsCommand.Create());
            rootCommand.AddCommand(VerifyCommand.Create());
            rootCommand.AddCommand(ValidateCommand.Create());
            rootCommand.AddCommand(ProfileCommand.Create());
            rootCommand.AddCommand(TestCommand.Create());
            rootCommand.AddCommand(ApiServerCommand.Create());

            // Invoke command
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in FluentPDF CLI");
            Console.Error.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            // Cleanup PDFium
            try
            {
                PdfiumInterop.Shutdown();
                Log.Information("PDFium library shut down");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error shutting down PDFium library");
            }

            Log.CloseAndFlush();
        }
    }
}
