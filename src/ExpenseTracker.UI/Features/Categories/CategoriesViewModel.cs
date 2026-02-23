using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;
using ExpenseTracker.Services.Contracts;
using System.Reactive.Concurrency;

namespace ExpenseTracker.UI.Features.Categories;

public sealed class CategoriesViewModel : ViewModelBase
{
    // ===== Sections (backed by DB) =====
    public ObservableCollection<CategoryCardViewModel> IncomeCategories { get; } = new();
    public ObservableCollection<CategoryCardViewModel> ExpenseCategories { get; } = new();
    public ObservableCollection<CategoryCardViewModel> TransferCategories { get; } = new();

    // ===== Modal state =====
    private bool _isUpsertModalOpen;
    public bool IsUpsertModalOpen
    {
        get => _isUpsertModalOpen;
        private set => this.RaiseAndSetIfChanged(ref _isUpsertModalOpen, value);
    }

    private bool _isDeleteModalOpen;
    public bool IsDeleteModalOpen
    {
        get => _isDeleteModalOpen;
        private set => this.RaiseAndSetIfChanged(ref _isDeleteModalOpen, value);
    }

    public bool IsAnyModalOpen => IsUpsertModalOpen || IsDeleteModalOpen;

    // Keep these computed props reactive:
    public string UpsertTitle => IsEditing ? "Edit Category" : "Create Category";
    public string UpsertSubtitle => IsEditing ? "Update category details" : "Add a new category for transactions";
    public string UpsertPrimaryButtonText => IsEditing ? "Update" : "Create";

    // ===== Upsert fields =====
    public ObservableCollection<string> CategoryTypes { get; } = new() { "Income", "Expense", "Transfer" };

    private string _categoryName = "";
    public string CategoryName
    {
        get => _categoryName;
        set => this.RaiseAndSetIfChanged(ref _categoryName, value);
    }

    private string _selectedType = "Expense";
    public string SelectedType
    {
        get => _selectedType;
        set => this.RaiseAndSetIfChanged(ref _selectedType, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        private set => this.RaiseAndSetIfChanged(ref _isEditing, value);
    }

    private CategoryCardViewModel? _editingCategory;
    private CategoryCardViewModel? EditingCategory
    {
        get => _editingCategory;
        set => this.RaiseAndSetIfChanged(ref _editingCategory, value);
    }

    // ===== Delete selection =====
    private CategoryCardViewModel? _deleteTarget;
    public CategoryCardViewModel? DeleteTarget
    {
        get => _deleteTarget;
        private set
        {
            this.RaiseAndSetIfChanged(ref _deleteTarget, value);
            this.RaisePropertyChanged(nameof(DeletePromptLine1));
            this.RaisePropertyChanged(nameof(DeletePromptLine2));
        }
    }

    public string DeletePromptLine1 =>
        DeleteTarget is null
            ? "Are you sure you want to delete this category?"
            : $"Are you sure you want to delete “{DeleteTarget.Name}”?";

    public string DeletePromptLine2 =>
        DeleteTarget is null
            ? "Transactions will be moved to Uncategorized."
            : $"{DeleteTarget.TransactionCount} transactions will be moved to Uncategorized.";

    // ===== Commands =====
    public ReactiveCommand<Unit, Unit> OpenCreate { get; }
    public ReactiveCommand<CategoryCardViewModel, Unit> OpenEdit { get; }
    public ReactiveCommand<CategoryCardViewModel, Unit> OpenDelete { get; }
    public ReactiveCommand<Unit, Unit> CloseModal { get; }
    public ReactiveCommand<Unit, Unit> SaveUpsert { get; }
    public ReactiveCommand<Unit, Unit> ConfirmDelete { get; }

    // ===== Services =====
    private readonly IAppLogger _appLogger;
    private readonly ICategoryService _categoryService;
    private readonly ITransactionService _transactionService;

    public CategoriesViewModel(IAppLogger appLogger, ICategoryService categoryService, ITransactionService transactionService)
    {
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));

        // ---- Commands ----
        OpenCreate = ReactiveCommand.Create(() =>
        {
            IsEditing = false;
            EditingCategory = null;

            CategoryName = "";
            SelectedType = "Expense";

            IsUpsertModalOpen = true;
            IsDeleteModalOpen = false;

            RaiseModalComputed();
        });

        OpenEdit = ReactiveCommand.Create<CategoryCardViewModel>(cat =>
        {
            IsEditing = true;
            EditingCategory = cat;

            CategoryName = cat.Name;
            SelectedType = cat.Type;

            IsUpsertModalOpen = true;
            IsDeleteModalOpen = false;

            RaiseModalComputed();
        });

        OpenDelete = ReactiveCommand.CreateFromTask<CategoryCardViewModel>(async cat =>
        {
            // prevent deleting locked/system categories (optional)
            if (cat.IsLocked) return;

            // Refresh the transaction count for accurate delete confirmation
            try
            {
                cat.TransactionCount = await _transactionService
                    .GetTransactionCountByCategoryAsync(cat.Id, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch { /* keep existing count */ }

            DeleteTarget = cat;

            IsDeleteModalOpen = true;
            IsUpsertModalOpen = false;

            RaiseModalComputed();
        });

        CloseModal = ReactiveCommand.Create(() =>
        {
            IsUpsertModalOpen = false;
            IsDeleteModalOpen = false;

            DeleteTarget = null;
            EditingCategory = null;
            IsEditing = false;

            RaiseModalComputed();
        });

        SaveUpsert = ReactiveCommand.CreateFromTask(async () =>
        {
            var trimmed = (CategoryName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            if (!CategoryTypes.Contains(SelectedType))
                SelectedType = "Expense";

            try
            {
                if (IsEditing && EditingCategory is not null)
                {
                    // Update via service
                    await _categoryService.UpdateUserCategoryAsync(
                        EditingCategory.Id,
                        trimmed,
                        SelectedType,
                        CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    // Create via service
                    await _categoryService.CreateUserCategoryAsync(
                        trimmed,
                        SelectedType,
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch(Exception ex)
            {
                // TODO: surface errors to UI (snackbar/dialog) - swallow for now to avoid crash
                _appLogger.Error($"[CategoriesViewModel] Failed to upsert category '{trimmed}' of type '{SelectedType}'. Exception: {ex}");
            }

            // Refresh displayed categories from DB
            await LoadCategoriesAsync().ConfigureAwait(false);

            IsUpsertModalOpen = false;
            RaiseModalComputed();
        });

        ConfirmDelete = ReactiveCommand.CreateFromTask(async () =>
        {
            if (DeleteTarget is null)
                return;

            try
            {
                await _categoryService.DeleteUserCategoryAsync(DeleteTarget.Id, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: surfacing errors to UI. swallow for now.
                _appLogger.Error($"[CategoriesViewModel] Failed to delete category id {DeleteTarget.Id}. Exception: {ex}");
            }

            // Refresh displayed categories from DB
            await LoadCategoriesAsync().ConfigureAwait(false);

            DeleteTarget = null;
            IsDeleteModalOpen = false;
            RaiseModalComputed();
        });

        // Keep IsAnyModalOpen reactive for bindings that depend on it
        this.WhenAnyValue(x => x.IsUpsertModalOpen, x => x.IsDeleteModalOpen)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsAnyModalOpen)));

        // Initial load (fire-and-forget)
        _ = LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        IReadOnlyList<Domain.Category.Category> categories;
        try
        {
            categories = await _categoryService.GetAllCategoriesAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            // If the load fails, keep UI empty. TODO: surface error to UI.
            _appLogger.Error($"[CategoriesViewModel] Failed to load categories from service with exception: {ex}.");
            categories = Array.Empty<Domain.Category.Category>();
        }

        // Fetch transaction counts per category
        var counts = new Dictionary<Guid, int>();
        foreach (var cat in categories)
        {
            try
            {
                counts[cat.Id] = await _transactionService
                    .GetTransactionCountByCategoryAsync(cat.Id, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
                counts[cat.Id] = 0;
            }
        }

        // Map and update collections on the UI/main thread.
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            IncomeCategories.Clear();
            ExpenseCategories.Clear();
            TransferCategories.Clear();

            foreach (var cat in categories.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                var vm = new CategoryCardViewModel(
                    id: cat.Id,
                    name: cat.Name,
                    transactionCount: counts.GetValueOrDefault(cat.Id, 0),
                    isLocked: cat.IsSystemCategory,
                    type: cat.Type.ToString());

                AddToSection(vm);
            }
        });
    }

    private void RaiseModalComputed()
    {
        this.RaisePropertyChanged(nameof(IsAnyModalOpen));
        this.RaisePropertyChanged(nameof(UpsertTitle));
        this.RaisePropertyChanged(nameof(UpsertSubtitle));
        this.RaisePropertyChanged(nameof(UpsertPrimaryButtonText));
        this.RaisePropertyChanged(nameof(DeletePromptLine1));
        this.RaisePropertyChanged(nameof(DeletePromptLine2));
    }

    private void AddToSection(CategoryCardViewModel card)
    {
        switch (card.Type)
        {
            case "Income":
                IncomeCategories.Add(card);
                break;
            case "Transfer":
                TransferCategories.Add(card);
                break;
            default:
                ExpenseCategories.Add(card);
                break;
        }
    }

    private void RemoveFromSection(CategoryCardViewModel card)
    {
        IncomeCategories.Remove(card);
        ExpenseCategories.Remove(card);
        TransferCategories.Remove(card);
    }

    private void MoveIfTypeChanged(CategoryCardViewModel card)
    {
        // Remove from all, then add to correct section
        RemoveFromSection(card);
        AddToSection(card);
    }
}

public sealed class CategoryCardViewModel : ViewModelBase
{
    public Guid Id { get; }

    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private int _transactionCount;
    public int TransactionCount
    {
        get => _transactionCount;
        set
        {
            this.RaiseAndSetIfChanged(ref _transactionCount, value);
            this.RaisePropertyChanged(nameof(TransactionCountText));
        }
    }

    public string TransactionCountText => $"{TransactionCount} transactions";

    private string _type;
    public string Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public bool IsLocked { get; }
    public bool CanEdit => !IsLocked;

    public CategoryCardViewModel(Guid id, string name, int transactionCount, bool isLocked, string type)
    {
        Id = id;
        _name = name;
        _transactionCount = transactionCount;
        _type = type;
        IsLocked = isLocked;
    }
}