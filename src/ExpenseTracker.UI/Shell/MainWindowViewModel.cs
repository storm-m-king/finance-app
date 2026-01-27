using System;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;
using ExpenseTracker.UI.Features.Dashboard;
using ExpenseTracker.UI.Features.Import;
using ExpenseTracker.UI.Features.Import.Preview;

namespace ExpenseTracker.UI.Shell;

public sealed class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _current = new DashboardViewModel();
    public ViewModelBase Current
    {
        get => _current;
        private set => this.RaiseAndSetIfChanged(ref _current, value);
    }

    // ---- Selected state (exactly one should be true) ----
    private bool _isDashboardSelected;
    public bool IsDashboardSelected
    {
        get => _isDashboardSelected;
        set => this.RaiseAndSetIfChanged(ref _isDashboardSelected, value);
    }

    private bool _isTransactionsSelected;
    public bool IsTransactionsSelected
    {
        get => _isTransactionsSelected;
        set => this.RaiseAndSetIfChanged(ref _isTransactionsSelected, value);
    }

    private bool _isImportSelected;
    public bool IsImportSelected
    {
        get => _isImportSelected;
        set => this.RaiseAndSetIfChanged(ref _isImportSelected, value);
    }

    private bool _isCategoriesSelected;
    public bool IsCategoriesSelected
    {
        get => _isCategoriesSelected;
        set => this.RaiseAndSetIfChanged(ref _isCategoriesSelected, value);
    }

    private bool _isRulesSelected;
    public bool IsRulesSelected
    {
        get => _isRulesSelected;
        set => this.RaiseAndSetIfChanged(ref _isRulesSelected, value);
    }

    private bool _isAccountsSelected;
    public bool IsAccountsSelected
    {
        get => _isAccountsSelected;
        set => this.RaiseAndSetIfChanged(ref _isAccountsSelected, value);
    }

    // ---- Commands ----
    public ReactiveCommand<Unit, Unit> GoDashboard { get; }
    public ReactiveCommand<Unit, Unit> GoTransactions { get; }
    public ReactiveCommand<Unit, Unit> GoImport { get; }
    public ReactiveCommand<Unit, Unit> GoCategories { get; }
    public ReactiveCommand<Unit, Unit> GoRules { get; }
    public ReactiveCommand<Unit, Unit> GoAccounts { get; }

    public MainWindowViewModel()
    {
        GoDashboard = ReactiveCommand.Create(() =>
        {
            SelectNav(dashboard: true);
            Current = new DashboardViewModel();
        });

        GoTransactions = ReactiveCommand.Create(() =>
        {
            SelectNav(transactions: true);
            Current = new PlaceholderViewModel("Transactions");
        });

        GoImport = ReactiveCommand.Create(() =>
        {
            SelectNav(import: true);
            ShowImportUpload(); // always start at step 1 when clicking Import
        });

        GoCategories = ReactiveCommand.Create(() =>
        {
            SelectNav(categories: true);
            Current = new PlaceholderViewModel("Categories");
        });

        GoRules = ReactiveCommand.Create(() =>
        {
            SelectNav(rules: true);
            Current = new PlaceholderViewModel("Rules");
        });

        GoAccounts = ReactiveCommand.Create(() =>
        {
            SelectNav(accounts: true);
            Current = new PlaceholderViewModel("Accounts");
        });

        // ✅ Default selection
        SelectNav(dashboard: true);
        Current = new DashboardViewModel();
    }

    private void ShowImportUpload()
    {
        // Keep Import highlighted in the sidebar
        SelectNav(import: true);

        // Pass callback so ImportViewModel can advance to preview.
        Current = new ImportViewModel(
            onContinueToPreview: (selectedFilePath, selectedProfileName) =>
            {
                ShowImportPreview(selectedFilePath, selectedProfileName);
            });
    }

    private void ShowImportPreview(string selectedFilePathOrUri, string mappingProfile)
    {
        // Keep Import highlighted in the sidebar
        SelectNav(import: true);

        Current = new PreviewImportViewModel(
            selectedFilePath: selectedFilePathOrUri,
            mappingProfile: mappingProfile,
            onBack: () => ShowImportUpload(),
            onImport: importedCount =>
            {
                // Later: step 3 screen
                Current = new Features.Import.Complete.ImportCompleteViewModel(
                    successfulImportRowCount: importedCount,
                    importMore: GoImport,
                    viewTransactions: GoTransactions
                );
            });
    }

    private void SelectNav(
        bool dashboard = false,
        bool transactions = false,
        bool import = false,
        bool categories = false,
        bool rules = false,
        bool accounts = false)
    {
        IsDashboardSelected = false;
        IsTransactionsSelected = false;
        IsImportSelected = false;
        IsCategoriesSelected = false;
        IsRulesSelected = false;
        IsAccountsSelected = false;

        IsDashboardSelected = dashboard;
        IsTransactionsSelected = transactions;
        IsImportSelected = import;
        IsCategoriesSelected = categories;
        IsRulesSelected = rules;
        IsAccountsSelected = accounts;
    }

    public sealed class PlaceholderViewModel : ViewModelBase
    {
        public string Title { get; }
        public PlaceholderViewModel(string title) => Title = title;
    }
}
