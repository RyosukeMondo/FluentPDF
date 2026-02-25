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
            try
            {
                appBuilder.StartWithClassicDesktopLifetime(args);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Dispatcher shut down"))
            {
                // Expected when using --test-render or other headless CLI modes
                DiagnosticLogger.Log("Dispatcher shut down (expected for CLI mode)");
            }

            DiagnosticLogger.Log("Classic desktop lifetime ended normally");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.LogError("FATAL: Unhandled exception in Main", ex);

#if DEBUG
            // LOUD ERROR MESSAGE for development builds
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n");
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                       FATAL APPLICATION ERROR                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Error Type: {ex.GetType().Name}");
            Console.WriteLine($"Message:    {ex.Message}");
            Console.WriteLine();
            Console.WriteLine($"Log File:   {DiagnosticLogger.GetLogFilePath()}");
            Console.ResetColor();
            Console.WriteLine();

            // Only try to read key if console input is available
            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("Press any key to exit...");
                try { Console.ReadKey(); } catch { }
            }
            else
            {
                Console.WriteLine("Application failed to start. See log file above for details.");
            }
#endif

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
