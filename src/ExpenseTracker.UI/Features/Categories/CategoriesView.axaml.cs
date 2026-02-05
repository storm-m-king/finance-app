using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ExpenseTracker.UI.Features.Categories;

public partial class CategoriesView : UserControl
{
    public CategoriesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}