using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ExpenseTracker.UI.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve window + VM via DI
            var window = Program.Services.GetRequiredService<MainWindow>();
            window.DataContext = Program.Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}