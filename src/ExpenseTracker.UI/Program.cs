using Avalonia;
using Avalonia.ReactiveUI;
using ExpenseTracker.Domain.ImportProfile;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Repositories;
using ExpenseTracker.Infrastructure.Persistence.Seed;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.Services.Services.FingerPrint;
using ExpenseTracker.Services.Services.Import;
using ExpenseTracker.Services.Services.Import.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.UI;

/// <summary>
/// Application entry point and composition root.
/// Responsible for configuring dependency injection, executing startup
/// initialization tasks, and launching the Avalonia desktop application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Configures the Avalonia application builder.
    /// This method is required by Avalonia tooling and the visual designer.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

    /// <summary>
    /// Application entry point.
    /// Performs all non-UI startup work before starting the Avalonia UI lifetime.
    /// Avalonia APIs must not be invoked before the desktop lifetime begins.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static async Task Main(string[] args)
    {
        // Create and configure the dependency injection container.
        var services = new ServiceCollection();
        using var provider = ConfigureServices(services);

        // Initialize logging as early as possible so all startup activity is captured.
        var appLogger = provider.GetRequiredService<IAppLogger>();
        appLogger.Initialize();
        appLogger.Info("===================== Application started =====================");
        appLogger.Info(AppPaths.GetConfigurationSummary());

        // Ensure the local SQLite database schema exists.
        appLogger.Trace("db.schema.apply", () =>
        {
            var initializer = provider.GetRequiredService<DbInitializer>();
            initializer.Initialize();
        });

        // Seed the database with default/system data.
        appLogger.Trace("db.seed", () =>
        {
            var seeder = provider.GetRequiredService<SystemSeeder>();
            seeder.Seed();
        });
        
        // Initialize the ImportProfileRegistry with all known import profiles.
        var fingerprintService = provider.GetRequiredService<IFingerprintService>();
        var importProfileRepository = provider.GetRequiredService<IImportProfileRepository>();
        var profiles = await importProfileRepository.GetAllAsync();
        provider.GetRequiredService<IImportProfileRegistry>()
            .InitializeRegistry(profiles);

        // Start the Avalonia desktop application.
        // This call blocks until the application exits.
        appLogger.Trace("app.run", () =>
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        });
    }

    /// <summary>
    /// Registers application services and builds the dependency injection container.
    /// This method defines service lifetimes and enables container validation.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>A configured <see cref="ServiceProvider"/> instance.</returns>
    private static ServiceProvider ConfigureServices(IServiceCollection services)
    {
        // Core infrastructure services shared for the lifetime of the application.
        services.AddSingleton<IAppLogger, AppLogger>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IFingerprintService, Sha256FingerprintService>();
        services.AddSingleton<IImportProfileResolver, ImportProfileResolver>();
        services.AddSingleton<IImportProfileRegistry, ImportProfileRegistry>();

        // Repositories
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IImportProfileRepository, ImportProfileRepository>();
        

        // Short-lived startup helpers used during application initialization.
        services.AddTransient<DbInitializer>();
        services.AddTransient<SystemSeeder>();
        
        // Enable validation to fail fast on DI misconfiguration.
        var options = new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        };

        return services.BuildServiceProvider(options);
    }
}