using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ExpenseTracker.UI.Features.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}