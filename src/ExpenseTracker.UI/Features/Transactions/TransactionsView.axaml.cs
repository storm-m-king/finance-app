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
}
