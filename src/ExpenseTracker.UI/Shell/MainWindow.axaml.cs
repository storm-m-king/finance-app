using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ExpenseTracker.UI.Shell;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}