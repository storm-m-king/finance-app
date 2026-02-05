using System;
using System.Globalization;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;
using ExpenseTracker.UI.Features.Dashboard;
using ExpenseTracker.UI.Features.Import.ImportView;
using ExpenseTracker.UI.Features.Import.PreviewView;

namespace ExpenseTracker.UI.Shell;

public sealed class MainWindowViewModel : ViewModelBase
{
    // ✅ Factory for step 1 (ImportUpload) VM (DI gives it IImportService, etc.)
    private readonly Func<Action<string, string>, ImportViewModel> _importVmFactory;

    // ✅ Factory for step 2 (Preview) VM (DI gives it ImportService, repos, etc.)
    private readonly Func<string, string, Action, Action<int>, PreviewImportViewModel> _previewVmFactory;

    private ViewModelBase _current = new DashboardViewModel();

    public string CurrentMonthText =>
        DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

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

    // ✅ DI constructor
    public MainWindowViewModel(
        Func<Action<string, string>, ImportViewModel> importVmFactory,
        Func<string, string, Action, Action<int>, PreviewImportViewModel> previewVmFactory)
    {
        _importVmFactory = importVmFactory;
        _previewVmFactory = previewVmFactory;

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
            Current = new ExpenseTracker.UI.Features.Categories.CategoriesViewModel();
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

        // ✅ Create Import VM via DI-backed factory
        Current = _importVmFactory((selectedFilePath, selectedProfileKey) =>
        {
            ShowImportPreview(selectedFilePath, selectedProfileKey);
        });
    }

    private void ShowImportPreview(string selectedFilePathOrUri, string mappingProfileKey)
    {
        // Keep Import highlighted in the sidebar
        SelectNav(import: true);

        // Callbacks for the preview step
        Action onBack = () => ShowImportUpload();

        Action<int> onImport = importedCount =>
        {
            // Step 3 screen
            Current = new Features.Import.CompleteView.ImportCompleteViewModel(
                successfulImportRowCount: importedCount,
                importMore: GoImport,
                viewTransactions: GoTransactions
            );
        };

        // ✅ Create preview VM via DI-backed factory
        Current = _previewVmFactory(selectedFilePathOrUri, mappingProfileKey, onBack, onImport);
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
