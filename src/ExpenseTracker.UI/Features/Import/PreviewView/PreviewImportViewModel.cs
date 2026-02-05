using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using ExpenseTracker.Services.Contracts;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import.PreviewView;

public sealed class PreviewImportViewModel : ViewModelBase
{
    private readonly IImportService _importService;

    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; } = new();

    public string SummaryText =>
        $"Ready to import {PreviewRows.Count} transactions. Rules will be applied automatically.";

    public ReactiveCommand<Unit, Unit> Back { get; }
    public ReactiveCommand<Unit, Unit> ImportTransactions { get; }

    private readonly Action _onBack;
    private readonly Action<int> _onImport;
    
    private readonly string _selectedFilePath;
    private readonly string _mappingProfile;

    public PreviewImportViewModel(IImportService importService, string selectedFilePath, string mappingProfile, Action onBack, Action<int> onImport)
    {
        _importService = importService;
        _selectedFilePath = selectedFilePath;
        _mappingProfile = mappingProfile;
        _onBack = onBack;
        _onImport = onImport;
        
        // Keep SummaryText in sync as rows change
        PreviewRows.CollectionChanged += (_, __) => this.RaisePropertyChanged(nameof(SummaryText));

        Back = ReactiveCommand.Create(_onBack);
        ImportTransactions = ReactiveCommand.CreateFromTask(async () =>
        {
            var importedCount = await _importService.PreviewAsync(mappingProfile, selectedFilePath);
            _onImport(importedCount.Count);
        });
        
        // Load preview rows
        _ = LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        var preview = await _importService.PreviewAsync(_mappingProfile, _selectedFilePath);

        foreach (var row in preview)
            PreviewRows.Add(new PreviewRowViewModel(row.PostedDate, row.RawDescription, row.AmountCents, "Uncategorized"));
    }
}

public sealed class PreviewRowViewModel : ViewModelBase
{
    public string DateText { get; }
    public string Description { get; }
    public string AmountText { get; }
    public string Category { get; }

    public PreviewRowViewModel(DateOnly date, string description, long amount, string category)
    {
        decimal dollars = amount / 100m;
        string dollarsFormatted = dollars.ToString("C", CultureInfo.GetCultureInfo("en-US"));

        DateText = date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        Description = description;
        AmountText = dollarsFormatted;
        Category = category;
    }
}