using System;

namespace ExpenseTracker.Infrastructure.Configuration;

/// <summary>
/// Defines all paths for the application.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Gets the directory where all app data will be stored.
    /// </summary>
    /// <returns>The App Data Directory.</returns>
    public static string GetAppDataDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(home, "ExpenseTracker");
    }

    /// <summary>
    /// Gets the directory where app files will be retrieved from build output.
    /// </summary>
    /// <returns>The App Context Directory.</returns>
    public static string GetAppContextDirectory()
    {
        return AppContext.BaseDirectory;
    }

    /// <summary>
    /// Gets the directory where logs will be stored.
    /// </summary>
    /// <returns>The Log directory.</returns>
    public static string GetLogsDirectory()
        => Path.Combine(GetAppDataDirectory(), "Logs");
    
    /// <summary>
    /// Gets the directory where the database will be stored.
    /// </summary>
    /// <returns>The database path.</returns>
    public static string GetDatabasePath()
        => Path.Combine(GetAppDataDirectory(), "expense-tracker.db");

    /// <summary>
    /// Gets the directory where imported CSV files are backed up.
    /// </summary>
    /// <returns>The imports backup directory.</returns>
    public static string GetImportsDirectory()
        => Path.Combine(GetAppDataDirectory(), "Imports");

    /// <summary>
    /// Gets the path to the schema.sql file used to create the db.
    /// </summary>
    /// <returns>The schema.sql path.</returns>
    public static string GetSchemaSqlPath()
        => Path.Combine(GetAppContextDirectory(), "Persistence", "Schema", "schema.sql");
    
    /// <summary>
    /// Gets a summary of all paths returned by this class.
    /// </summary>
    /// <returns>A summary string with all paths.</returns>
    public static string GetConfigurationSummary()
    {
        return $"** App Configuration Paths **:\n" +
               $"\tApp Data Directory: {GetAppDataDirectory()}\n" +
               $"\tLogs Directory: {GetLogsDirectory()}\n " +
               $"\tDatabase Path: {GetDatabasePath()}\n " +
               $"\tApp Context Directory: {GetAppContextDirectory()}\n " +
               $"\tSchema Sql Path: {GetSchemaSqlPath()}";
    }
}