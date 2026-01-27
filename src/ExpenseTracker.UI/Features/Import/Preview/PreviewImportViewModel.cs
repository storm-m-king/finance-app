using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import.Preview;

public sealed class PreviewImportViewModel : ViewModelBase
{
    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; } = new();

    public string SummaryText =>
        $"Ready to import {PreviewRows.Count} transactions. Rules will be applied automatically.";

    public ReactiveCommand<Unit, Unit> Back { get; }
    public ReactiveCommand<Unit, Unit> ImportTransactions { get; }

    private readonly Action _onBack;
    private readonly Action<int> _onImport;

    public PreviewImportViewModel(string selectedPath, string mappingProfile, Action onBack, Action<int> onImport)
    {
        _onBack = onBack;
        _onImport = onImport;

        // TEMP mock data (replace with parsed CSV preview later)
        PreviewRows.Add(new PreviewRowViewModel(new DateTime(2026, 1, 26), "Trader Joe's", -71.23m, "Groceries"));
        PreviewRows.Add(new PreviewRowViewModel(new DateTime(2026, 1, 25), "Chipotle", -14.89m, "Dining Out"));
        PreviewRows.Add(new PreviewRowViewModel(new DateTime(2026, 1, 24), "Paycheck", 3500.00m, "Income"));
        PreviewRows.Add(new PreviewRowViewModel(new DateTime(2026, 1, 23), "Peloton", -39.99m, "Fitness"));

        Back = ReactiveCommand.Create(_onBack);
        ImportTransactions = ReactiveCommand.Create(() =>
        {
            _onImport(PreviewRows.Count);
        });
    }
}

public sealed class PreviewRowViewModel : ViewModelBase
{
    public string DateText { get; }
    public string Description { get; }
    public string AmountText { get; }
    public string Category { get; }

    public PreviewRowViewModel(DateTime date, string description, decimal amount, string category)
    {
        DateText = date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        Description = description;
        AmountText = amount < 0 ? $"-${Math.Abs(amount):0.00}" : $"${amount:0.00}";
        Category = category;
    }
}