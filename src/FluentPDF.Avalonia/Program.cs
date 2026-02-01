using Avalonia;
using System;
using System.Threading.Tasks;

namespace FluentPDF.Avalonia;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            DiagnosticLogger.LogSection("PROGRAM START");
            DiagnosticLogger.Log($"Arguments: {string.Join(" ", args)}");
            DiagnosticLogger.Log($"Log file location: {DiagnosticLogger.GetLogFilePath()}");

            // Parse command-line arguments
            var commandLine = new CommandLineOptions(args);
            CommandLineOptions.Current = commandLine;

            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            DiagnosticLogger.Log("Global exception handlers registered");
            DiagnosticLogger.Log("Building Avalonia app...");

            var appBuilder = BuildAvaloniaApp();
            DiagnosticLogger.Log("AppBuilder created successfully");

            DiagnosticLogger.Log("Starting classic desktop lifetime...");
            appBuilder.StartWithClassicDesktopLifetime(args);

            DiagnosticLogger.Log("Classic desktop lifetime ended normally");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("FATAL: Unhandled exception in Main", ex);
            Console.WriteLine($"\n\nFATAL ERROR - Check log file on desktop: {DiagnosticLogger.GetLogFilePath()}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
        finally
        {
            DiagnosticLogger.LogSection("PROGRAM END");
            DiagnosticLogger.Log($"Final log location: {DiagnosticLogger.GetLogFilePath()}");
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        DiagnosticLogger.LogError($"UNHANDLED EXCEPTION (IsTerminating: {e.IsTerminating})", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        DiagnosticLogger.LogError("UNOBSERVED TASK EXCEPTION", e.Exception);
        e.SetObserved(); // Prevent app crash
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        try
        {
            DiagnosticLogger.Log("Configuring AppBuilder...");

            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

            DiagnosticLogger.Log("AppBuilder configured successfully");
            return builder;
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("Failed to build AppBuilder", ex);
            throw;
        }
    }
}
