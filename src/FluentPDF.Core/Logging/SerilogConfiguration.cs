using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;

namespace FluentPDF.Core.Logging;

/// <summary>
/// Configures Serilog with structured logging, file output, and OpenTelemetry integration.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Creates and configures a Serilog ILogger instance with JSON file sink and OpenTelemetry sink.
    /// </summary>
    /// <returns>Configured ILogger instance ready for use.</returns>
    public static ILogger CreateLogger()
    {
        var logPath = GetLogPath();
        var version = GetVersion();

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "FluentPDF")
            .Enrich.WithProperty("Version", version)
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Async(a => a.File(
                new CompactJsonFormatter(),
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7))
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = "http://localhost:4317";
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "FluentPDF.Desktop",
                    ["service.version"] = version
                };
            })
            .CreateLogger();
    }

    /// <summary>
    /// Gets the log file path, using ApplicationData.LocalFolder when available (MSIX sandbox).
    /// Falls back to temp directory for headless testing scenarios.
    /// </summary>
    /// <returns>Full path to the log file with rolling date suffix.</returns>
    private static string GetLogPath()
    {
        // Try to get Windows ApplicationData path via reflection to avoid direct dependency
        try
        {
            var type = Type.GetType("Windows.Storage.ApplicationData, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");
            if (type != null)
            {
                var currentProperty = type.GetProperty("Current");
                var current = currentProperty?.GetValue(null);
                if (current != null)
                {
                    var localFolderProperty = current.GetType().GetProperty("LocalFolder");
                    var localFolder = localFolderProperty?.GetValue(current);
                    if (localFolder != null)
                    {
                        var pathProperty = localFolder.GetType().GetProperty("Path");
                        var path = pathProperty?.GetValue(localFolder) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            var logsDir = Path.Combine(path, "logs");
                            Directory.CreateDirectory(logsDir);
                            return Path.Combine(logsDir, "log-.json");
                        }
                    }
                }
            }
        }
        catch
        {
            // Fall through to temp path
        }

        // Fallback for tests or non-MSIX contexts
        var tempDir = Path.Combine(Path.GetTempPath(), "FluentPDF", "logs");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, "log-.json");
    }

    /// <summary>
    /// Gets the application version from the executing assembly.
    /// </summary>
    /// <returns>Assembly version string.</returns>
    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return version ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
