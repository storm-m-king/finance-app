using System;

namespace ExpenseTracker.Infrastructure.Configuration;

public static class AppPaths
{
    public static string GetAppDataDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(home, "ExpenseTracker");
    }

    public static string GetAppContextDirectory()
    {
        return AppContext.BaseDirectory;
    }

    public static string GetLogsDirectory()
        => Path.Combine(GetAppDataDirectory(), "Logs");
    

    public static string GetDatabasePath()
        => Path.Combine(GetAppDataDirectory(), "expense-tracker.db");

    public static string GetSchemaSqlPath()
        => Path.Combine(GetAppContextDirectory(), "Persistence", "Schema", "schema.sql");
    
    public static string GetConfigurationSummary()
    {
        return $"** App Configuration Paths **:\n" +
               $"\tApp Data Directory: {GetAppDataDirectory()}\n" +
               $"\tLogs Directory: {GetLogsDirectory()}\n " +
               $"\tDatabase Path: {GetDatabasePath()}";
    }
}