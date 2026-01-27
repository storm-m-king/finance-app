using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import.Complete;

public sealed class ImportCompleteViewModel : ViewModelBase
{
    public string CompleteMessage { get; }

    public ReactiveCommand<Unit, Unit> ImportMore { get; }
    public ReactiveCommand<Unit, Unit> ViewTransactions { get; }

    public ImportCompleteViewModel(
        int successfulImportRowCount,
        ReactiveCommand<Unit, Unit> importMore,
        ReactiveCommand<Unit, Unit> viewTransactions)
    {
        CompleteMessage = $"Successfully imported {successfulImportRowCount} transactions. " +
                          $"Rules have been applied and transactions are ready for review.";
        ImportMore = importMore;
        ViewTransactions = viewTransactions;
    }
}