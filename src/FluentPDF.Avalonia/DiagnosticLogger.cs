using System;
using System.IO;
using System.Text;

namespace FluentPDF.Avalonia;

/// <summary>
/// Diagnostic logger that writes to both console and file.
/// Used for debugging startup issues.
/// </summary>
public static class DiagnosticLogger
{
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        $"FluentPDF-Diagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.log"
    );

    private static readonly object LockObj = new();

    static DiagnosticLogger()
    {
        try
        {
            File.WriteAllText(LogFile, $"=== FluentPDF Diagnostic Log ===\n");
            File.AppendAllText(LogFile, $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n");
            File.AppendAllText(LogFile, $"Log file: {LogFile}\n");
            File.AppendAllText(LogFile, $"Working directory: {Environment.CurrentDirectory}\n");
            File.AppendAllText(LogFile, $"OS: {Environment.OSVersion}\n");
            File.AppendAllText(LogFile, $".NET: {Environment.Version}\n\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize diagnostic log: {ex.Message}");
        }
    }

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";

        lock (LockObj)
        {
            try
            {
                // Console output
                Console.WriteLine(logLine);

                // File output
                File.AppendAllText(LogFile, logLine + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var sb = new StringBuilder();
        sb.AppendLine($"[{timestamp}] ERROR: {message}");

        if (ex != null)
        {
            sb.AppendLine($"  Exception: {ex.GetType().Name}");
            sb.AppendLine($"  Message: {ex.Message}");
            sb.AppendLine($"  Stack: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"  Inner Exception: {ex.InnerException.GetType().Name}");
                sb.AppendLine($"  Inner Message: {ex.InnerException.Message}");
                sb.AppendLine($"  Inner Stack: {ex.InnerException.StackTrace}");
            }
        }

        var logText = sb.ToString();

        lock (LockObj)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(logText);
                Console.ResetColor();

                File.AppendAllText(LogFile, logText);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }

    public static void LogSection(string section)
    {
        var line = new string('=', 60);
        Log($"\n{line}");
        Log($"  {section}");
        Log($"{line}\n");
    }

    public static string GetLogFilePath() => LogFile;
}
