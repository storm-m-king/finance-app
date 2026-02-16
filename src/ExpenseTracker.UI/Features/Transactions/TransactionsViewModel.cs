using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
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

    // Category options for per-row editing
    public ObservableCollection<string> AllCategoryNames { get; } = new();

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

    public TransactionsViewModel()
    {
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

        SeedSampleData();

        ClearFilters = ReactiveCommand.Create(ClearAllFilters);
        ApplyCustomDateRange = ReactiveCommand.Create(ApplyCustomRange);
        CancelCustomDateRange = ReactiveCommand.Create(() => { IsCustomDateDialogOpen = false; });
        SelectPresetCommand = ReactiveCommand.Create<DateRangePreset>(preset => SelectedDatePreset = preset);

        // React to preset selection
        this.WhenAnyValue(x => x.SelectedDatePreset)
            .Subscribe(OnDatePresetSelected);

        // Default to Current Month
        SelectedDatePreset = currentMonth;

        // Reactive filter pipeline
        this.WhenAnyValue(
                x => x.SearchText,
                x => x.FilterTrigger)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyFilters());

        ApplyFilters();
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

        // Summary
        var totalAmount = results.Where(r => !string.Equals(r.SelectedCategory, "Transfer", StringComparison.OrdinalIgnoreCase)).Sum(r => r.AmountCents);
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

    private void SeedSampleData()
    {
        var samples = new[]
        {
            ("2026-02-15", "HARRIS TEETER #325", -7123L, "Groceries", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-14", "CHICK-FIL-A #02187", -1489L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-14", "STARBUCKS STORE 12345", -675L, "Dining Out", "Amex Platinum", "Needs Review", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-13", "AMAZON.COM*2K8HY7VX3", -6456L, "Shopping", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-13", "MOBILE PAYMENT - THANK YOU", 250000L, "Transfer", "Checking ••4521", "Reviewed", true, @"C:\Users\stoking\Downloads\checking-feb.csv"),
            ("2026-02-12", "MICROSOFT*XBOX GAMEPASS", -1699L, "Subscriptions", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-12", "APPLE.COM/BILL", -999L, "Subscriptions", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-11", "LOWE'S #1234", -15432L, "Home Improvement", "Amex Platinum", "Needs Review", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-11", "ADTSECURITY 800-878-7806", -5999L, "Home Services", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-10", "NC QUICK PASS", -3500L, "Transportation", "Checking ••4521", "Reviewed", false, @"C:\Users\stoking\Downloads\checking-feb.csv"),
            ("2026-02-10", "WENDY'S #8876", -1245L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-09", "TARGET 00012345", -8934L, "Shopping", "Amex Platinum", "Needs Review", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-09", "UBER EATS", -3245L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-08", "YMCA MEMBERSHIP", -4500L, "Fitness", "Checking ••4521", "Reviewed", false, @"C:\Users\stoking\Downloads\checking-feb.csv"),
            ("2026-02-08", "DOMINO'S PIZZA 8765", -2199L, "Dining Out", "Amex Platinum", "Ignored", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-07", "SPOTIFY USA", -1099L, "Subscriptions", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-07", "FOODLION #0452", -4523L, "Groceries", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-06", "HAWX SERVICES LLC", -12500L, "Home Services", "Checking ••4521", "Reviewed", false, @"C:\Users\stoking\Downloads\checking-feb.csv"),
            ("2026-02-06", "PAYROLL DEPOSIT", 550000L, "Transfer", "Checking ••4521", "Reviewed", true, @"C:\Users\stoking\Downloads\checking-feb.csv"),
            ("2026-02-05", "TACO BELL #7634", -1087L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-05", "CRUNCHYROLL", -799L, "Subscriptions", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-04", "OPENAI *CHATGPT PLUS", -2000L, "Subscriptions", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-03", "TESLA SUPERCHARGER", -2845L, "Transportation", "Amex Platinum", "Needs Review", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-02", "VIET-THAI RESTAURANT", -4567L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
            ("2026-02-01", "TST* THE VAVE", -3421L, "Dining Out", "Amex Platinum", "Reviewed", false, @"C:\Users\stoking\Downloads\activity.csv"),
        };

        var categories = new HashSet<string>();
        var accounts = new HashSet<string>();
        var statuses = new HashSet<string>();

        // First pass: collect unique values
        foreach (var (_, _, _, cat, acct, status, _, _) in samples)
        {
            categories.Add(cat);
            accounts.Add(acct);
            statuses.Add(status);
        }

        // Build category names for per-row editing ComboBox
        foreach (var c in categories.OrderBy(c => c))
            AllCategoryNames.Add(c);

        // Second pass: create row VMs with shared category collection
        foreach (var (date, desc, amount, cat, acct, status, transfer, source) in samples)
        {
            var row = new TransactionRowViewModel(
                DateOnly.Parse(date), desc, amount, cat, acct, status, transfer, source, AllCategoryNames);
            row.WhenAnyValue(x => x.SelectedCategory)
                .Skip(1)
                .Subscribe(_ => FilterTrigger++);
            _allRows.Add(row);
        }

        // Build multi-select filter items
        foreach (var c in categories.OrderBy(c => c))
        {
            var item = new FilterCheckItem(c);
            SubscribeToFilterItem(item);
            CategoryFilters.Add(item);
        }
        foreach (var s in new[] { "Needs Review", "Reviewed", "Ignored" })
        {
            var item = new FilterCheckItem(s);
            SubscribeToFilterItem(item);
            StatusFilters.Add(item);
        }
        foreach (var a in accounts.OrderBy(a => a))
        {
            var item = new FilterCheckItem(a);
            SubscribeToFilterItem(item);
            AccountFilters.Add(item);
        }
    }
}

public sealed class TransactionRowViewModel : ViewModelBase
{
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

    public string AmountColor => IsNegative ? "#EAF0FF" : "#3FA97A";

    public TransactionRowViewModel(DateOnly date, string description, long amountCents, string category, string account, string status, bool isTransfer, string sourceFile, ObservableCollection<string> categoryOptions)
    {
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
    }
}

public sealed record DateRangePreset(string Label, DateOnly? From, DateOnly? To, bool IsCustom);
