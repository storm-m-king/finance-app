using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;
using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Services.Contracts;

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

    public ObservableCollection<MetricCardVm> Metrics { get; } = new()
    {
        new MetricCardVm("This Month", "$15,100", "-14.5%"),
        new MetricCardVm("Last Month", "$13,200", "+5.2%"),
        new MetricCardVm("Average", "$9,127", "+5.2%"),
        new MetricCardVm("Net Worth", "$335,608", "+3.4%")
    };

    public ObservableCollection<RecentTransactionVm> RecentTransactions { get; } = new()
    {
        new RecentTransactionVm("Trader Joe's", "$71.23"),
        new RecentTransactionVm("Chipotle", "$14.89"),
        new RecentTransactionVm("Peloton", "$39.99"),
        new RecentTransactionVm("Walgreens", "$12.34"),
        new RecentTransactionVm("Delta Airlines", "$500.56"),
        new RecentTransactionVm("Amazon", "$64.56"),
    };

    // Now includes Percent (0..1) + ColorBrush for proportional colored bars
    public ObservableCollection<TopCategoryVm> TopCategories { get; } = new();

    public ReactiveCommand<Unit, Unit> OpenNeedsReview { get; }

    public DashboardViewModel(
        ITransactionService transactionService,
        Action? navigateToNeedsReview = null)
    {
        _transactionService = transactionService;
        _navigateToNeedsReview = navigateToNeedsReview;

        OpenNeedsReview = ReactiveCommand.Create(() =>
        {
            _navigateToNeedsReview?.Invoke();
        });

        // Seed top categories with computed Percent + mock palette colors
        var raw = new[]
        {
            new { Name = "Mortgage",      Amount = 2700.00m },
            new { Name = "Pets",          Amount = 813.39m },
            new { Name = "Groceries",     Amount = 510.63m },
            new { Name = "Dining Out",    Amount = 254.03m },
            new { Name = "Food Delivery", Amount = 120.94m  },
        };

        var total = raw.Sum(x => x.Amount);

        for (var i = 0; i < raw.Length; i++)
        {
            var item = raw[i];

            TopCategories.Add(new TopCategoryVm(
                item.Name,
                item.Amount,
                total == 0 ? 0 : (double)(item.Amount / total),
                GetCategoryColor(i)
            ));
        }

        _ = LoadNeedsReviewCountAsync();
    }

    private async Task LoadNeedsReviewCountAsync()
    {
        try
        {
            var transactions = await _transactionService.GetAllTransactionsAsync();
            var count = transactions.Count(t => t.Status == TransactionStatus.NeedsReview);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
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

    private static string GetCategoryColor(int index)
        => index switch
        {
            0 => "CatBlue", // Blue
            1 => "CatYellow", // Yellow
            2 => "CatGreen", // Green
            3 => "CatOrange", // Orange
            _ => "CatPurple", // Purple
        };

    public sealed record MetricCardVm(string Title, string Value, string Delta);
    public sealed record RecentTransactionVm(string Merchant, string Amount);
    
    public sealed record TopCategoryVm(
        string Name,
        decimal Amount,
        double Percent,
        string ColorKey
    );

}
