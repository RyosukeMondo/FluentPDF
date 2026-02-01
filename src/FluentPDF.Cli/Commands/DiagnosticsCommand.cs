// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.CommandLine;
using System.Runtime.InteropServices;
using FluentPDF.Rendering.Interop;
using Serilog;

namespace FluentPDF.Cli.Commands;

/// <summary>
/// Command to display system diagnostics information.
/// </summary>
internal static class DiagnosticsCommand
{
    public static Command Create()
    {
        var command = new Command("diagnostics", "Display system diagnostics (OS, .NET, PDFium version, memory)");

        command.SetHandler(context => Execute());

        return command;
    }

    private static int Execute()
    {
        try
        {
            Console.WriteLine("FluentPDF System Diagnostics");
            Console.WriteLine("============================");
            Console.WriteLine();

            // Operating System
            Console.WriteLine($"Operating System: {Environment.OSVersion}");
            Console.WriteLine($"OS Platform: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine();

            // .NET Runtime
            Console.WriteLine($".NET Runtime: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($".NET Version: {Environment.Version}");
            Console.WriteLine($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
            Console.WriteLine();

            // Machine Information
            Console.WriteLine($"Machine Name: {Environment.MachineName}");
            Console.WriteLine($"User Name: {Environment.UserName}");
            Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine();

            // Memory
            var workingSet = Environment.WorkingSet;
            Console.WriteLine($"Working Set Memory: {workingSet / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"GC Total Memory: {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine();

            // PDFium
            Console.WriteLine("PDFium Library:");
            Console.WriteLine($"  Status: Initialized");
            Console.WriteLine($"  DLL Location: {Path.Combine(AppContext.BaseDirectory, "pdfium.dll")}");
            Console.WriteLine($"  DLL Exists: {File.Exists(Path.Combine(AppContext.BaseDirectory, "pdfium.dll"))}");
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "pdfium.dll")))
            {
                var fileInfo = new FileInfo(Path.Combine(AppContext.BaseDirectory, "pdfium.dll"));
                Console.WriteLine($"  DLL Size: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine($"  DLL Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();

            // Application Info
            Console.WriteLine("Application:");
            Console.WriteLine($"  Base Directory: {AppContext.BaseDirectory}");
            Console.WriteLine($"  Current Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"  Command Line: {Environment.CommandLine}");
            Console.WriteLine();

            Log.Information("Diagnostics completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to execute diagnostics command");
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }
}
