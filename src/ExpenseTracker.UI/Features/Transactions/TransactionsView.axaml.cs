using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace ExpenseTracker.UI.Features.Transactions;

public partial class TransactionsView : ReactiveUserControl<TransactionsViewModel>
{
    public TransactionsView()
    {
        InitializeComponent();
        this.WhenActivated(_ => { });
    }

    public void CloseNotesFlyout(object? sender, RoutedEventArgs e)
    {
        // Walk the logical tree (Parent) to find the Popup hosting the flyout
        var current = sender as StyledElement;
        while (current != null)
        {
            if (current is Popup popup)
            {
                popup.IsOpen = false;
                return;
            }
            current = current.Parent;
        }
    }
}
