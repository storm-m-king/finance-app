using Avalonia;
using Avalonia.ReactiveUI;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Seed;

namespace ExpenseTracker.UI;

internal static class Program
{
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet.
    [STAThread]
    public static void Main(string[] args)
    {
        // Setup the logger
        AppLogger.Initialize();
        AppLogger.Info("===================== Application started =====================");
        AppLogger.Info($"{AppPaths.GetConfigurationSummary()}");
        
        // Create the SQLite DB locally
        var factory = new SqliteConnectionFactory();
        AppLogger.Trace("db.schema.apply",
            () =>
            {
                var initializer = new DbInitializer(factory);
                initializer.Initialize();
            });

        // Seed the DB with default values
        AppLogger.Trace("db.seed",
            () =>
            {
                var seeder = new SystemSeeder(factory);
                seeder.Seed();
            });
        
        // Build the Avalonia App
        AppLogger.Trace("app.run",
            () =>
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            });
    }
}