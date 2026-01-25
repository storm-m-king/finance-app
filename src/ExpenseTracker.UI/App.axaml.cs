using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Seed;

namespace ExpenseTracker.UI;

public sealed partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        // Start the application window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}