using FluentPDF.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// All logging to stderr (stdout is JSON-RPC)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(opts =>
{
    opts.LogToStandardErrorThreshold = LogLevel.Trace;
});

// HTTP client targeting Avalonia REST API
builder.Services.AddHttpClient("FluentPdf", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<FluentPdfClient>();

// MCP server with stdio transport
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "fluentpdf",
            Version = "2.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
