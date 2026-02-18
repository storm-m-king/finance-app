using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Domain.Category;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Services.Contracts;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Import.PreviewView;

public sealed class PreviewImportViewModel : ViewModelBase
{
    private const string AddNewCategorySentinel = "+ Add New Category";

    private readonly IImportService _importService;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IRuleService _ruleService;

    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; } = new();
    public ObservableCollection<string> AvailableCategories { get; } = new();

    public string SummaryText =>
        $"Ready to import {PreviewRows.Count} transactions. Rules will be applied automatically.";

    public ReactiveCommand<Unit, Unit> Back { get; }
    public ReactiveCommand<Unit, Unit> ImportTransactions { get; }
    public ReactiveCommand<Unit, Unit> ConfirmNewCategory { get; }
    public ReactiveCommand<Unit, Unit> CancelNewCategory { get; }

    private readonly Action _onBack;
    private readonly Action<int> _onImport;

    private readonly string _selectedFilePath;
    private readonly string _mappingProfile;

    // Cached category lookup: "Name (Type)" -> Category
    private readonly Dictionary<string, Category> _categoryLookup = new(StringComparer.OrdinalIgnoreCase);

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

    // Tracks which row triggered the "Add New" dialog so we can select it after creation
    private PreviewRowViewModel? _pendingNewCategoryRow;

    public PreviewImportViewModel(
        IImportService importService,
        ITransactionService transactionService,
        ICategoryService categoryService,
        IRuleService ruleService,
        string selectedFilePath,
        string mappingProfile,
        Action onBack,
        Action<int> onImport)
    {
        _importService = importService;
        _transactionService = transactionService;
        _categoryService = categoryService;
        _ruleService = ruleService;
        _selectedFilePath = selectedFilePath;
        _mappingProfile = mappingProfile;
        _onBack = onBack;
        _onImport = onImport;

        PreviewRows.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(SummaryText));

        Back = ReactiveCommand.Create(_onBack);
        ImportTransactions = ReactiveCommand.CreateFromTask(ImportTransactionsAsync);

        var canConfirm = this.WhenAnyValue(x => x.NewCategoryName)
            .Select(n => !string.IsNullOrWhiteSpace(n));

        ConfirmNewCategory = ReactiveCommand.CreateFromTask(CreateNewCategoryAsync, canConfirm);
        CancelNewCategory = ReactiveCommand.Create(() =>
        {
            IsNewCategoryDialogOpen = false;
            _pendingNewCategoryRow = null;
        });

        _ = LoadPreviewAsync();
    }

    private async Task ImportTransactionsAsync()
    {
        // Create rules for any rows where the user manually changed the category
        await CreateRulesForManualEditsAsync();

        // Re-run preview to get final categorized rows (with new rules applied)
        var finalRows = await _importService.PreviewAsync(_mappingProfile, _selectedFilePath);

        // Apply any user overrides from the preview grid
        var overriddenRows = ApplyUserOverrides(finalRows);

        // Backup the imported CSV to the Imports directory
        var backupFileName = BackupImportFile(_selectedFilePath);

        // Persist transactions — store only the backup filename, not the full path
        var importedCount = await _transactionService.ImportTransactionsAsync(
            overriddenRows, backupFileName);

        _onImport(importedCount);
    }

    /// <summary>
    /// Copies the source CSV into the Imports directory.
    /// If a file with the same name already exists and has identical contents, reuses it.
    /// If contents differ, appends (1), (2), etc. until a unique name is found.
    /// Returns the backup filename only (not the full path).
    /// </summary>
    private static string BackupImportFile(string sourcePath)
    {
        var importsDir = AppPaths.GetImportsDirectory();
        Directory.CreateDirectory(importsDir);

        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var ext = Path.GetExtension(sourcePath);
        var candidateName = $"{baseName}{ext}";
        var candidatePath = Path.Combine(importsDir, candidateName);

        var sourceHash = ComputeFileHash(sourcePath);

        // If file exists with same name, check if contents match
        if (File.Exists(candidatePath))
        {
            if (ComputeFileHash(candidatePath) == sourceHash)
                return candidateName;

            // Contents differ — find next available (N) suffix
            var n = 1;
            do
            {
                candidateName = $"{baseName} ({n}){ext}";
                candidatePath = Path.Combine(importsDir, candidateName);

                if (File.Exists(candidatePath) && ComputeFileHash(candidatePath) == sourceHash)
                    return candidateName;

                n++;
            } while (File.Exists(candidatePath));
        }

        File.Copy(sourcePath, candidatePath, overwrite: false);
        return candidateName;
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }

    private IReadOnlyList<ExpenseTracker.Services.DTOs.TransactionPreviewRow> ApplyUserOverrides(
        IReadOnlyList<ExpenseTracker.Services.DTOs.TransactionPreviewRow> rows)
    {
        // Build a lookup of user overrides: description → categoryId
        var overrides = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var previewRow in PreviewRows)
        {
            if (!previewRow.WasManuallyEdited) continue;
            var categoryId = ResolveCategoryId(previewRow.SelectedCategory);
            if (categoryId.HasValue)
                overrides[previewRow.Description] = categoryId.Value;
        }

        if (overrides.Count == 0) return rows;

        var result = new List<ExpenseTracker.Services.DTOs.TransactionPreviewRow>(rows.Count);
        foreach (var row in rows)
        {
            if (overrides.TryGetValue(row.RawDescription, out var overrideCategoryId))
                result.Add(row with { CategoryId = overrideCategoryId });
            else
                result.Add(row);
        }
        return result;
    }

    private async Task CreateRulesForManualEditsAsync()
    {
        // Collect manual edits first, deduplicating by (description, category)
        var edits = new List<(string Description, string CategoryDisplay, Guid CategoryId)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in PreviewRows)
        {
            if (!row.WasManuallyEdited) continue;

            var categoryDisplay = row.SelectedCategory;
            if (string.IsNullOrEmpty(categoryDisplay) || categoryDisplay == AddNewCategorySentinel)
                continue;

            var categoryId = ResolveCategoryId(categoryDisplay);
            if (categoryId == null) continue;

            var dedupeKey = $"{row.Description}|{categoryId}";
            if (!seen.Add(dedupeKey)) continue;

            edits.Add((row.Description, categoryDisplay, categoryId.Value));
        }

        if (edits.Count == 0) return;

        // Shift all existing rules up to make room at the top
        var existingRules = await _ruleService.GetAllRulesAsync();
        if (existingRules.Count > 0)
        {
            var reordered = existingRules
                .OrderBy(r => r.Priority)
                .Select(r => r.Id)
                .ToList();

            // Insert placeholder positions at the front so existing rules start at edits.Count
            var shiftedIds = new List<Guid>(edits.Count + reordered.Count);
            shiftedIds.AddRange(reordered);

            // Reorder with offset: each existing rule gets priority = edits.Count + index
            for (var i = 0; i < reordered.Count; i++)
            {
                var rule = existingRules.First(r => r.Id == reordered[i]);
                if (rule.Priority != edits.Count + i)
                {
                    await _ruleService.UpdateRuleAsync(
                        rule.Id, rule.Name, rule.Condition, rule.CategoryId,
                        edits.Count + i, rule.Enabled);
                }
            }
        }

        // Create new rules at priority 0, 1, 2, ...
        for (var i = 0; i < edits.Count; i++)
        {
            var (description, categoryDisplay, categoryId) = edits[i];
            var condition = new ContainsCondition(description);
            var ruleName = $"Auto: {description} -> {ExtractCategoryName(categoryDisplay)}";
            await _ruleService.CreateRuleAsync(ruleName, condition, categoryId, i);
        }
    }

    private Guid? ResolveCategoryId(string displayEntry)
    {
        if (_categoryLookup.TryGetValue(displayEntry, out var cat))
            return cat.Id;
        return null;
    }

    private static string ExtractCategoryName(string displayEntry)
    {
        var parenIdx = displayEntry.LastIndexOf(" (", StringComparison.Ordinal);
        return parenIdx > 0 ? displayEntry[..parenIdx] : displayEntry;
    }

    private async Task LoadPreviewAsync()
    {
        await LoadCategoriesAsync();

        var preview = await _importService.PreviewAsync(_mappingProfile, _selectedFilePath);

        foreach (var row in preview)
        {
            var categoryDisplay = row.CategoryName ?? "Uncategorized";
            // Find matching display entry in AvailableCategories
            var match = AvailableCategories
                .FirstOrDefault(c => c.StartsWith(categoryDisplay + " (", StringComparison.OrdinalIgnoreCase))
                ?? AvailableCategories.FirstOrDefault(c => c.Equals(categoryDisplay, StringComparison.OrdinalIgnoreCase))
                ?? AvailableCategories.FirstOrDefault()
                ?? "Uncategorized";

            var vm = new PreviewRowViewModel(row.PostedDate, row.RawDescription, row.AmountCents, match, AvailableCategories);
            vm.WhenAnyValue(x => x.SelectedCategory)
                .Skip(1)
                .Subscribe(selected => OnCategorySelected(vm, selected));
            PreviewRows.Add(vm);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        AvailableCategories.Clear();
        _categoryLookup.Clear();
        foreach (var cat in categories.OrderBy(c => c.Name))
        {
            var display = $"{cat.Name} ({cat.Type})";
            AvailableCategories.Add(display);
            _categoryLookup[display] = cat;
        }
        AvailableCategories.Add(AddNewCategorySentinel);
    }

    private void OnCategorySelected(PreviewRowViewModel row, string? selected)
    {
        if (selected == AddNewCategorySentinel)
        {
            _pendingNewCategoryRow = row;
            NewCategoryName = string.Empty;
            SelectedNewCategoryType = "Expense";
            IsNewCategoryDialogOpen = true;
        }
    }

    private async Task CreateNewCategoryAsync()
    {
        var name = NewCategoryName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        await _categoryService.CreateUserCategoryAsync(name, SelectedNewCategoryType);

        var displayEntry = $"{name} ({SelectedNewCategoryType})";

        // Fetch the newly created category to get its Guid for the lookup cache
        var allCategories = await _categoryService.GetAllCategoriesAsync();
        var newCat = allCategories.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (newCat != null)
            _categoryLookup[displayEntry] = newCat;

        // Insert before the sentinel without clearing the collection
        var sentinelIdx = AvailableCategories.IndexOf(AddNewCategorySentinel);
        if (sentinelIdx >= 0)
            AvailableCategories.Insert(sentinelIdx, displayEntry);
        else
            AvailableCategories.Add(displayEntry);

        // Select the new category on the row that triggered the dialog
        if (_pendingNewCategoryRow != null)
            _pendingNewCategoryRow.SelectedCategory = displayEntry;

        IsNewCategoryDialogOpen = false;
        _pendingNewCategoryRow = null;
        NewCategoryName = string.Empty;
    }
}

public sealed class PreviewRowViewModel : ViewModelBase
{
    public string DateText { get; }
    public string Description { get; }
    public string AmountText { get; }

    public ObservableCollection<string> AvailableCategories { get; }

    /// <summary>The category initially assigned by the rule engine.</summary>
    public string OriginalCategory { get; }

    /// <summary>True when the user has manually changed the category from the auto-assigned one.</summary>
    public bool WasManuallyEdited => !string.Equals(SelectedCategory, OriginalCategory, StringComparison.Ordinal);

    private string _selectedCategory;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    public PreviewRowViewModel(DateOnly date, string description, long amount, string category, ObservableCollection<string> availableCategories)
    {
        decimal dollars = amount / 100m;
        string dollarsFormatted = dollars.ToString("C", CultureInfo.GetCultureInfo("en-US"));

        DateText = date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        Description = description;
        AmountText = dollarsFormatted;
        AvailableCategories = availableCategories;
        OriginalCategory = category;
        _selectedCategory = category;
    }
}