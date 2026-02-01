// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.CommandLine;

namespace FluentPDF.Cli.Commands;

/// <summary>
/// Command to start the verification API server.
/// </summary>
internal static class ApiServerCommand
{
    public static Command Create()
    {
        var command = new Command("api-server", "Start verification REST API server");

        var portOption = new Option<int>("--port", () => 5000, "Port to listen on");
        var bindAddressOption = new Option<string>("--bind-address", () => "localhost", "Address to bind to");

        command.AddOption(portOption);
        command.AddOption(bindAddressOption);

        command.SetHandler(async (context) =>
        {
            var port = context.ParseResult.GetValueForOption(portOption);
            var bindAddress = context.ParseResult.GetValueForOption(bindAddressOption);

            Console.WriteLine($"API Server - Not yet implemented");
            Console.WriteLine($"Port: {port}");
            Console.WriteLine($"Bind Address: {bindAddress}");
            Console.WriteLine();
            Console.WriteLine("Planned endpoints:");
            Console.WriteLine("  GET  /api/health - Health check");
            Console.WriteLine("  POST /api/document/load - Load PDF document");
            Console.WriteLine("  GET  /api/document/{id} - Get document info");
            Console.WriteLine("  POST /api/render - Render page to PNG");
            Console.WriteLine("  POST /api/verify/render - Verify page render");

            // TODO: Implement API server
            await Task.Delay(1000);
            context.ExitCode = 0;
        });

        return command;
    }
}
