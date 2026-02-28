using System.Reactive;
using Avalonia.Threading;
using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Services.Contracts;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Dashboard;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly ITransactionService _transactionService;
    private readonly Action? _navigateToNeedsReview;

    private int _needsReviewCount;
    public int NeedsReviewCount
    {
        get => _needsReviewCount;
        set => this.RaiseAndSetIfChanged(ref _needsReviewCount, value);
    }

    private bool _showNeedsReview;
    public bool ShowNeedsReview
    {
        get => _showNeedsReview;
        set => this.RaiseAndSetIfChanged(ref _showNeedsReview, value);
    }

    public string WorkingDirectory { get; }

    public ReactiveCommand<Unit, Unit> OpenNeedsReview { get; }

    public DashboardViewModel(
        ITransactionService transactionService,
        Action? navigateToNeedsReview = null)
    {
        _transactionService = transactionService;
        _navigateToNeedsReview = navigateToNeedsReview;
        WorkingDirectory = AppPaths.GetAppDataDirectory();

        OpenNeedsReview = ReactiveCommand.Create(() =>
        {
            _navigateToNeedsReview?.Invoke();
        });

        _ = LoadNeedsReviewCountAsync();
    }

    private async Task LoadNeedsReviewCountAsync()
    {
        try
        {
            var transactions = await _transactionService.GetAllTransactionsAsync();
            var count = transactions.Count(t => t.Status == TransactionStatus.NeedsReview);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                NeedsReviewCount = count;
                ShowNeedsReview = count > 0;
            });
        }
        catch
        {
            // Silently handle — pill stays hidden
        }
    }
}
