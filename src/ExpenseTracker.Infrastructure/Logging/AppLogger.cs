using System;
using System.IO;
using ExpenseTracker.Infrastructure.Configuration;

namespace ExpenseTracker.Infrastructure.Logging;

/// <summary>
/// Logger for the Application.
/// Writes logs to both console output and a Log file.
/// </summary>
public static class AppLogger
{
    private static readonly object _lock = new();
    private static string? _logFilePath;
    private const int MaxLogFiles = 5;
    private const long MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Initialize the app logger by creating the directory.
    /// </summary>
    public static void Initialize()
    {
        var baseDir = AppPaths.GetLogsDirectory();

        Directory.CreateDirectory(baseDir);

        _logFilePath = Path.Combine(
            baseDir,
            $"ExpenseTracker-{DateTime.Now:yyyy-MM-dd}.log");
        
        CleanupOldLogFiles(baseDir);
    }

    /// <summary>
    /// Logs an Info message.
    /// </summary>
    /// <param name="message">The text to log.</param>
    public static void Info(string message) =>
        Write("INFO", message);

    /// <summary>
    /// Logs a Warning message.
    /// </summary>
    /// <param name="message">The text to log.</param>
    public static void Warn(string message) =>
        Write("WARN", message);

    /// <summary>
    /// Logs an Error message.
    /// </summary>
    /// <param name="message">The text to log.</param>
    /// <param name="ex">[Optional] An exception to log.</param>
    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", message);

        if (ex != null)
            Write("ERROR", ex.ToString());
    }
    
    /// <summary>
    /// Run a timed action with begin/ok/fail logging, rethrowing on error.
    /// </summary>
    /// <param name="name">The name of the step to log.</param>
    /// <param name="action">The action to perform.</param>
    public static void Trace(string name, Action action)
    {
        Trace<object?>(
            name,
            () => { action(); return null; }
        );
    }
    
    /// <summary>
    /// Runs a timed function with begin/ok/fail logging, rethrowing on error.
    /// </summary>
    /// <param name="name">The name of the step to log.</param>
    /// <param name="func">The function to perform.</param>
    public static T Trace<T>(
        string name,
        Func<T> func)
    {
        Info($"STEP     {name}  status=begin");

        var start = DateTime.Now;
        try
        {
            var result = func();
            Info($"STEP     {name}  status=ok  ms={(DateTime.Now - start).TotalMilliseconds}");
            return result;
        }
        catch (Exception ex)
        {
            Error($"STEP     {name}  status=fail  ms={(DateTime.Now - start).TotalMilliseconds}", ex);
            throw;
        }
    }

    /// <summary>
    /// Writes the log message to the console window and to a log file.
    /// </summary>
    /// <param name="level">The log level (e.g. INFO, WARN, ERROR).</param>
    /// <param name="message">The text to log.</param>
    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

        lock (_lock)
        {
            Console.WriteLine(line);

            if (_logFilePath == null)
                return;

            File.AppendAllText(_logFilePath, line + Environment.NewLine);

            EnforceFileSizeLimit();
        }
    }
    
    /// <summary>
    /// Enforces that the number of log files never exceeds the maximum allotted size.
    /// </summary>
    /// <param name="logDirectory">The log directory to clean up.</param>
    private static void CleanupOldLogFiles(string logDirectory)
    {
        var files = new DirectoryInfo(logDirectory)
            .GetFiles("ExpenseTracker-*.log")
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (files.Count <= MaxLogFiles)
            return;

        foreach (var file in files.Skip(MaxLogFiles))
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                // Don't throw — logging should never crash the app
                Error($"Failed to delete old log file {file.Name}", ex);
            }
        }
    }


    /// <summary>
    /// Enforces that the log file never exceeds the maximum allotted size.
    /// </summary>
    private static void EnforceFileSizeLimit()
    {
        var info = new FileInfo(_logFilePath!);

        if (info.Length <= MaxLogFileSizeBytes)
            return;

        // Read all lines (safe at this size)
        var lines = File.ReadAllLines(_logFilePath!).ToList();

        // Remove oldest lines until under limit
        while (info.Length > MaxLogFileSizeBytes && lines.Count > 0)
        {
            lines.RemoveAt(0);
            File.WriteAllLines(_logFilePath!, lines);
            info.Refresh();
        }
    }
}