using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using ExpenseTracker.Domain.Account;
using ExpenseTracker.Domain.Category;
using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Services.Contracts;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Transactions;

/// <summary>
/// Checkbox item used in multi-select filter flyouts.
/// </summary>
public sealed class FilterCheckItem : ViewModelBase
{
    public string Label { get; }

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public FilterCheckItem(string label, bool isChecked = false)
    {
        Label = label;
        _isChecked = isChecked;
    }
}

public sealed class TransactionsViewModel : ViewModelBase
{
    private const string AddNewCategorySentinel = "+ Add New Category";

    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAccountRepository _accountRepository;

    private readonly ObservableCollection<TransactionRowViewModel> _allRows = new();

    public ObservableCollection<TransactionRowViewModel> FilteredRows { get; } = new();

    // Search
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    // Multi-select filters
    public ObservableCollection<FilterCheckItem> CategoryFilters { get; } = new();
    public ObservableCollection<FilterCheckItem> StatusFilters { get; } = new();
    public ObservableCollection<FilterCheckItem> AccountFilters { get; } = new();

    // Category options for per-row editing (display format: "Name (Type)")
    public ObservableCollection<string> AllCategoryNames { get; } = new();

    // New category dialog
    private bool _isNewCategoryDialogOpen;
    public bool IsNewCategoryDialogOpen
    {
        get => _isNewCategoryDialogOpen;
        set => this.RaiseAndSetIfChanged(ref _isNewCategoryDialogOpen, value);
    }

    private string _newCategoryName = string.Empty;
    public string NewCategoryName
    {
        get => _newCategoryName;
        set => this.RaiseAndSetIfChanged(ref _newCategoryName, value);
    }

    private string _selectedNewCategoryType = "Expense";
    public string SelectedNewCategoryType
    {
        get => _selectedNewCategoryType;
        set => this.RaiseAndSetIfChanged(ref _selectedNewCategoryType, value);
    }

    public ObservableCollection<string> CategoryTypes { get; } = new() { "Expense", "Income", "Transfer" };

    public ReactiveCommand<Unit, Unit> ConfirmNewCategory { get; }
    public ReactiveCommand<Unit, Unit> CancelNewCategory { get; }

    private TransactionRowViewModel? _pendingNewCategoryRow;

    // Date range presets
    public ObservableCollection<DateRangePreset> DatePresets { get; } = new();

    private DateRangePreset? _selectedDatePreset;
    public DateRangePreset? SelectedDatePreset
    {
        get => _selectedDatePreset;
        set => this.RaiseAndSetIfChanged(ref _selectedDatePreset, value);
    }

    // Custom date range dialog
    private bool _isCustomDateDialogOpen;
    public bool IsCustomDateDialogOpen
    {
        get => _isCustomDateDialogOpen;
        set => this.RaiseAndSetIfChanged(ref _isCustomDateDialogOpen, value);
    }

    private DateTimeOffset? _customDateFrom;
    public DateTimeOffset? CustomDateFrom
    {
        get => _customDateFrom;
        set => this.RaiseAndSetIfChanged(ref _customDateFrom, value);
    }

    private DateTimeOffset? _customDateTo;
    public DateTimeOffset? CustomDateTo
    {
        get => _customDateTo;
        set => this.RaiseAndSetIfChanged(ref _customDateTo, value);
    }

    public ReactiveCommand<Unit, Unit> ApplyCustomDateRange { get; }
    public ReactiveCommand<Unit, Unit> CancelCustomDateRange { get; }
    public ReactiveCommand<DateRangePreset, Unit> SelectPresetCommand { get; }

    // Active date filter label shown on screen
    private string _dateRangeLabel = string.Empty;
    public string DateRangeLabel
    {
        get => _dateRangeLabel;
        set => this.RaiseAndSetIfChanged(ref _dateRangeLabel, value);
    }

    // Internal resolved range used for filtering
    private DateOnly? _dateFrom;
    private DateOnly? _dateTo;

    // Summary labels for filter buttons
    private bool _showSourceFile;
    public bool ShowSourceFile
    {
        get => _showSourceFile;
        set => this.RaiseAndSetIfChanged(ref _showSourceFile, value);
    }

    private string _categoryFilterLabel = "All";
    public string CategoryFilterLabel
    {
        get => _categoryFilterLabel;
        set => this.RaiseAndSetIfChanged(ref _categoryFilterLabel, value);
    }

    private string _statusFilterLabel = "All";
    public string StatusFilterLabel
    {
        get => _statusFilterLabel;
        set => this.RaiseAndSetIfChanged(ref _statusFilterLabel, value);
    }

    private string _accountFilterLabel = "All";
    public string AccountFilterLabel
    {
        get => _accountFilterLabel;
        set => this.RaiseAndSetIfChanged(ref _accountFilterLabel, value);
    }

    // Summary
    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        set => this.RaiseAndSetIfChanged(ref _summaryText, value);
    }

    // Tracks whether any filter is active
    private bool _hasActiveFilters;
    public bool HasActiveFilters
    {
        get => _hasActiveFilters;
        set => this.RaiseAndSetIfChanged(ref _hasActiveFilters, value);
    }

    public ReactiveCommand<Unit, Unit> ClearFilters { get; }

    // Trigger property — incremented to signal a re-filter from checkbox changes
    private int _filterTrigger;
    private int FilterTrigger
    {
        get => _filterTrigger;
        set => this.RaiseAndSetIfChanged(ref _filterTrigger, value);
    }

    // Lookups populated during load
    private Dictionary<Guid, string> _categoryDisplayLookup = new();
    private Dictionary<Guid, Account> _accountObjectLookup = new();
    private Dictionary<Guid, string> _accountLookup = new();
    private Dictionary<string, Guid> _categoryDisplayToId = new(StringComparer.OrdinalIgnoreCase);

    public TransactionsViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAccountRepository accountRepository)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _accountRepository = accountRepository;

        var canConfirm = this.WhenAnyValue(x => x.NewCategoryName)
            .Select(n => !string.IsNullOrWhiteSpace(n));
        ConfirmNewCategory = ReactiveCommand.CreateFromTask(CreateNewCategoryAsync, canConfirm);
        CancelNewCategory = ReactiveCommand.Create(() =>
        {
            IsNewCategoryDialogOpen = false;
            _pendingNewCategoryRow = null;
        });

        // Build date presets
        var now = DateTime.Today;
        var currentMonth = new DateRangePreset("Current Month", new DateOnly(now.Year, now.Month, 1), DateOnly.FromDateTime(now), false);
        DatePresets.Add(currentMonth);
        DatePresets.Add(new DateRangePreset("Last 30 Days", DateOnly.FromDateTime(now.AddDays(-30)), DateOnly.FromDateTime(now), false));
        DatePresets.Add(new DateRangePreset("Last 60 Days", DateOnly.FromDateTime(now.AddDays(-60)), DateOnly.FromDateTime(now), false));
        DatePresets.Add(new DateRangePreset("Last 90 Days", DateOnly.FromDateTime(now.AddDays(-90)), DateOnly.FromDateTime(now), false));
        DatePresets.Add(new DateRangePreset("Year To Date", new DateOnly(now.Year, 1, 1), DateOnly.FromDateTime(now), false));
        DatePresets.Add(new DateRangePreset($"{now.Year - 1}", new DateOnly(now.Year - 1, 1, 1), new DateOnly(now.Year - 1, 12, 31), false));
        DatePresets.Add(new DateRangePreset($"{now.Year - 2}", new DateOnly(now.Year - 2, 1, 1), new DateOnly(now.Year - 2, 12, 31), false));
        DatePresets.Add(new DateRangePreset("Custom Range", null, null, true));

        ClearFilters = ReactiveCommand.Create(ClearAllFilters);
        ApplyCustomDateRange = ReactiveCommand.Create(ApplyCustomRange);
        CancelCustomDateRange = ReactiveCommand.Create(() => { IsCustomDateDialogOpen = false; });
        SelectPresetCommand = ReactiveCommand.Create<DateRangePreset>(preset => SelectedDatePreset = preset);

        // React to preset selection
        this.WhenAnyValue(x => x.SelectedDatePreset)
            .Subscribe(OnDatePresetSelected);

        // Reactive filter pipeline
        this.WhenAnyValue(
                x => x.SearchText,
                x => x.FilterTrigger)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyFilters());

        // Load data — no default date filter applied
        _ = LoadTransactionsAsync();
    }

    private async Task LoadTransactionsAsync()
    {
        try
        {
            // Load categories and accounts for display lookups
            var categories = await _categoryService.GetAllCategoriesAsync();
            _categoryDisplayLookup = categories.ToDictionary(c => c.Id, c => $"{c.Name} ({c.Type})");
            _categoryDisplayToId = categories.ToDictionary(c => $"{c.Name} ({c.Type})", c => c.Id, StringComparer.OrdinalIgnoreCase);

            var accounts = await _accountRepository.GetAllAsync();
            _accountLookup = accounts.ToDictionary(a => a.Id, a => a.Name);
            _accountObjectLookup = accounts.ToDictionary(a => a.Id);

            // Load all transactions
            var transactions = await _transactionService.GetAllTransactionsAsync();

            // Switch to UI thread for collection updates
            await Dispatcher.UIThread.InvokeAsync(() => PopulateUI(categories, transactions));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                SummaryText = $"Error loading transactions: {ex.Message}");
        }
    }

    private void PopulateUI(
        IReadOnlyList<Category> categories,
        IReadOnlyList<Transaction> transactions)
    {
        // Build category display names for per-row editing ComboBox: "Name (Type)"
        AllCategoryNames.Clear();
        foreach (var cat in categories.OrderBy(c => c.Name))
            AllCategoryNames.Add($"{cat.Name} ({cat.Type})");
        AllCategoryNames.Add(AddNewCategorySentinel);

        var categoryNames = new HashSet<string>();
        var accountNames = new HashSet<string>();

        _allRows.Clear();
        foreach (var t in transactions)
        {
            var categoryDisplay = _categoryDisplayLookup.TryGetValue(t.CategoryId, out var cd) ? cd : "Uncategorized (Expense)";
            var accountName = _accountLookup.TryGetValue(t.AccountId, out var an) ? an : "Unknown";
            var statusText = FormatStatus(t.Status);

            // Normalize amount so positive = income, negative = expense
            var displayAmount = NormalizeAmount(t.AmountCents, t.AccountId);

            categoryNames.Add(categoryDisplay);
            accountNames.Add(accountName);

            var row = new TransactionRowViewModel(
                t.Id,
                t.PostedDate, t.RawDescription, displayAmount,
                categoryDisplay, accountName, statusText,
                t.IsTransfer, t.SourceFileName ?? string.Empty,
                AllCategoryNames);

            // Wire category change to persist
            row.WhenAnyValue(x => x.SelectedCategory)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async newCategory =>
                {
                    if (newCategory == AddNewCategorySentinel)
                    {
                        _pendingNewCategoryRow = row;
                        NewCategoryName = string.Empty;
                        SelectedNewCategoryType = "Expense";
                        IsNewCategoryDialogOpen = true;
                        return;
                    }
                    if (_categoryDisplayToId.TryGetValue(newCategory, out var catId))
                        await _transactionService.UpdateCategoryAsync(row.TransactionId, catId);
                    FilterTrigger++;
                });

            // Wire status change to persist
            row.WhenAnyValue(x => x.SelectedStatus)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async newStatus =>
                {
                    var status = ParseStatus(newStatus);
                    await _transactionService.UpdateStatusAsync(row.TransactionId, status);
                    FilterTrigger++;
                });

            _allRows.Add(row);
        }

        // Build multi-select filter items
        CategoryFilters.Clear();
        foreach (var c in categoryNames.OrderBy(c => c))
        {
            var item = new FilterCheckItem(c);
            SubscribeToFilterItem(item);
            CategoryFilters.Add(item);
        }

        StatusFilters.Clear();
        foreach (var s in new[] { "Needs Review", "Reviewed", "Ignored" })
        {
            var item = new FilterCheckItem(s);
            SubscribeToFilterItem(item);
            StatusFilters.Add(item);
        }

        AccountFilters.Clear();
        foreach (var a in accountNames.OrderBy(a => a))
        {
            var item = new FilterCheckItem(a);
            SubscribeToFilterItem(item);
            AccountFilters.Add(item);
        }

        // Directly populate FilteredRows — no filter applied by default
        FilteredRows.Clear();
        foreach (var row in _allRows)
            FilteredRows.Add(row);

        // Update summary
        var totalAmount = _allRows.Where(r => !r.SelectedCategory.Contains("(Transfer)", StringComparison.OrdinalIgnoreCase)).Sum(r => r.AmountCents);
        var dollars = totalAmount / 100m;
        var sign = dollars >= 0 ? "+" : "";
        SummaryText = $"Showing {_allRows.Count} of {_allRows.Count} transactions  •  Net: {sign}{dollars.ToString("C", CultureInfo.GetCultureInfo("en-US"))}";
    }

    private static string FormatStatus(TransactionStatus status) => status switch
    {
        TransactionStatus.NeedsReview => "Needs Review",
        TransactionStatus.Reviewed => "Reviewed",
        TransactionStatus.Ignored => "Ignored",
        _ => "Unknown"
    };

    private static TransactionStatus ParseStatus(string status) => status switch
    {
        "Needs Review" => TransactionStatus.NeedsReview,
        "Reviewed" => TransactionStatus.Reviewed,
        "Ignored" => TransactionStatus.Ignored,
        _ => TransactionStatus.NeedsReview
    };

    /// <summary>
    /// Normalizes raw amount so that positive = income, negative = expense,
    /// regardless of the source institution's sign convention.
    /// Checking accounts already follow this convention.
    /// Credit accounts need the sign flipped when CreditNegative_DebitPositive.
    /// </summary>
    private long NormalizeAmount(long rawAmountCents, Guid accountId)
    {
        if (!_accountObjectLookup.TryGetValue(accountId, out var account))
            return rawAmountCents;

        if (account.Type == AccountType.Credit &&
            account.CreditSignConvention == CreditSignConvention.CreditNegative_DebitPositive)
        {
            // Convention: positive = expense, negative = credit/payment → flip
            return -rawAmountCents;
        }

        return rawAmountCents;
    }

    private async Task CreateNewCategoryAsync()
    {
        var name = NewCategoryName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        await _categoryService.CreateUserCategoryAsync(name, SelectedNewCategoryType);

        var displayEntry = $"{name} ({SelectedNewCategoryType})";

        // Fetch the newly created category to get its Guid (match both name and type)
        var allCategories = await _categoryService.GetAllCategoriesAsync();
        var newCat = allCategories.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Type.ToString(), SelectedNewCategoryType, StringComparison.OrdinalIgnoreCase));
        if (newCat != null)
            _categoryDisplayToId[displayEntry] = newCat.Id;

        // Insert before the sentinel
        var sentinelIdx = AllCategoryNames.IndexOf(AddNewCategorySentinel);
        if (sentinelIdx >= 0)
            AllCategoryNames.Insert(sentinelIdx, displayEntry);
        else
            AllCategoryNames.Add(displayEntry);

        // Select the new category on the row that triggered the dialog and persist
        if (_pendingNewCategoryRow != null)
        {
            _pendingNewCategoryRow.SelectedCategory = displayEntry;
            if (newCat != null)
                await _transactionService.UpdateCategoryAsync(_pendingNewCategoryRow.TransactionId, newCat.Id);
        }

        IsNewCategoryDialogOpen = false;
        _pendingNewCategoryRow = null;
        NewCategoryName = string.Empty;
    }

    private void OnDatePresetSelected(DateRangePreset? preset)
    {
        if (preset == null)
        {
            _dateFrom = null;
            _dateTo = null;
            DateRangeLabel = string.Empty;
            FilterTrigger++;
            return;
        }

        if (preset.IsCustom)
        {
            CustomDateFrom = null;
            CustomDateTo = null;
            IsCustomDateDialogOpen = true;
            return;
        }

        _dateFrom = preset.From;
        _dateTo = preset.To;
        DateRangeLabel = FormatDateRange(preset.From, preset.To, preset.Label);
        FilterTrigger++;
    }

    private void ApplyCustomRange()
    {
        _dateFrom = CustomDateFrom.HasValue ? DateOnly.FromDateTime(CustomDateFrom.Value.DateTime) : null;
        _dateTo = CustomDateTo.HasValue ? DateOnly.FromDateTime(CustomDateTo.Value.DateTime) : null;
        DateRangeLabel = FormatDateRange(_dateFrom, _dateTo, "Custom");
        IsCustomDateDialogOpen = false;
        FilterTrigger++;
    }

    private static string FormatDateRange(DateOnly? from, DateOnly? to, string label)
    {
        if (from == null && to == null) return string.Empty;
        var fromStr = from?.ToString("MM/dd/yyyy") ?? "...";
        var toStr = to?.ToString("MM/dd/yyyy") ?? "...";
        return $"{label}: {fromStr} — {toStr}";
    }

    private void SubscribeToFilterItem(FilterCheckItem item)
    {
        item.WhenAnyValue(x => x.IsChecked)
            .Skip(1)
            .Subscribe(_ => FilterTrigger++);
    }

    private void ClearAllFilters()
    {
        SearchText = string.Empty;
        SelectedDatePreset = null;
        _dateFrom = null;
        _dateTo = null;
        DateRangeLabel = string.Empty;

        foreach (var f in CategoryFilters) f.IsChecked = false;
        foreach (var f in StatusFilters) f.IsChecked = false;
        foreach (var f in AccountFilters) f.IsChecked = false;
    }

    private void ApplyFilters()
    {
        var filtered = _allRows.AsEnumerable();

        // Search across all visible columns
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.SelectedCategory.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Account.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.AmountText.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.DateText.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.SelectedStatus.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Multi-select filters: if any checked, include only those values
        var checkedCategories = CategoryFilters.Where(f => f.IsChecked).Select(f => f.Label).ToHashSet();
        if (checkedCategories.Count > 0)
            filtered = filtered.Where(r => checkedCategories.Contains(r.SelectedCategory));

        var checkedStatuses = StatusFilters.Where(f => f.IsChecked).Select(f => f.Label).ToHashSet();
        if (checkedStatuses.Count > 0)
            filtered = filtered.Where(r => checkedStatuses.Contains(r.SelectedStatus));

        var checkedAccounts = AccountFilters.Where(f => f.IsChecked).Select(f => f.Label).ToHashSet();
        if (checkedAccounts.Count > 0)
            filtered = filtered.Where(r => checkedAccounts.Contains(r.Account));

        if (_dateFrom.HasValue)
            filtered = filtered.Where(r => r.PostedDate >= _dateFrom.Value);

        if (_dateTo.HasValue)
            filtered = filtered.Where(r => r.PostedDate <= _dateTo.Value);

        // Default sort: date descending
        var results = filtered.OrderByDescending(r => r.PostedDate).ToList();

        FilteredRows.Clear();
        foreach (var row in results)
            FilteredRows.Add(row);

        // Update filter labels
        CategoryFilterLabel = checkedCategories.Count == 0 ? "All"
            : checkedCategories.Count == 1 ? checkedCategories.First()
            : $"{checkedCategories.Count} selected";

        StatusFilterLabel = checkedStatuses.Count == 0 ? "All"
            : checkedStatuses.Count == 1 ? checkedStatuses.First()
            : $"{checkedStatuses.Count} selected";

        AccountFilterLabel = checkedAccounts.Count == 0 ? "All"
            : checkedAccounts.Count == 1 ? checkedAccounts.First()
            : $"{checkedAccounts.Count} selected";

        // Summary — exclude transfers (display format includes "(Transfer)")
        var totalAmount = results.Where(r => !r.SelectedCategory.Contains("(Transfer)", StringComparison.OrdinalIgnoreCase)).Sum(r => r.AmountCents);
        var dollars = totalAmount / 100m;
        var sign = dollars >= 0 ? "+" : "";
        SummaryText = $"Showing {results.Count} of {_allRows.Count} transactions  •  Net: {sign}{dollars.ToString("C", CultureInfo.GetCultureInfo("en-US"))}";

        // Active filters indicator
        HasActiveFilters = !string.IsNullOrWhiteSpace(SearchText)
            || checkedCategories.Count > 0
            || checkedStatuses.Count > 0
            || checkedAccounts.Count > 0
            || !string.IsNullOrEmpty(DateRangeLabel);
    }
}

public sealed class TransactionRowViewModel : ViewModelBase
{
    public Guid TransactionId { get; }
    public DateOnly PostedDate { get; }
    public string DateText { get; }
    public string Description { get; }
    public long AmountCents { get; }
    public string AmountText { get; }
    public bool IsNegative { get; }
    public string Account { get; }
    public string SourceFile { get; }
    public bool IsTransfer { get; }

    public ObservableCollection<string> StatusOptions { get; } = new() { "Needs Review", "Reviewed", "Ignored" };
    public ObservableCollection<string> CategoryOptions { get; }

    private string _selectedCategory;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    private string _selectedStatus;
    public string SelectedStatus
    {
        get => _selectedStatus;
        set => this.RaiseAndSetIfChanged(ref _selectedStatus, value);
    }

    public string StatusColor => SelectedStatus switch
    {
        "Needs Review" => "#C9A640",
        "Reviewed" => "#3FA97A",
        "Ignored" => "#6B7A99",
        _ => "#A7B4D1"
    };

    public string AmountColor => IsTransfer || SelectedCategory.Contains("(Transfer)", StringComparison.OrdinalIgnoreCase)
        ? "#A7B4D1" : IsNegative ? "#E05555" : "#3FA97A";

    public TransactionRowViewModel(Guid transactionId, DateOnly date, string description, long amountCents, string category, string account, string status, bool isTransfer, string sourceFile, ObservableCollection<string> categoryOptions)
    {
        TransactionId = transactionId;
        PostedDate = date;
        DateText = date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        Description = description;
        AmountCents = amountCents;

        var dollars = amountCents / 100m;
        IsNegative = dollars < 0;
        AmountText = dollars.ToString("C", CultureInfo.GetCultureInfo("en-US"));

        _selectedCategory = category;
        Account = account;
        _selectedStatus = status;
        IsTransfer = isTransfer;
        SourceFile = sourceFile;
        CategoryOptions = categoryOptions;

        // Raise StatusColor when SelectedStatus changes
        this.WhenAnyValue(x => x.SelectedStatus)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(StatusColor)));

        // Raise AmountColor when category changes (Transfer detection)
        this.WhenAnyValue(x => x.SelectedCategory)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(AmountColor)));
    }
}

public sealed record DateRangePreset(string Label, DateOnly? From, DateOnly? To, bool IsCustom);
