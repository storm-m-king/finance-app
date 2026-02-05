using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Categories;

public sealed class CategoriesViewModel : ViewModelBase
{
    // ===== Mocked sections =====
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

    public CategoriesViewModel()
    {
        // ---- Mock data (replace later with service calls) ----
        IncomeCategories.Add(new CategoryCardViewModel("Income", 24, isLocked: true, type: "Income"));

        ExpenseCategories.Add(new CategoryCardViewModel("Groceries", 45, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Dining Out", 32, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Transportation", 18, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Housing", 12, isLocked: true, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Healthcare", 8, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Shopping", 56, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Utilities", 15, isLocked: false, type: "Expense"));
        ExpenseCategories.Add(new CategoryCardViewModel("Entertainment", 28, isLocked: false, type: "Expense"));

        TransferCategories.Add(new CategoryCardViewModel("Transfer", 10, isLocked: true, type: "Transfer"));

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

        OpenDelete = ReactiveCommand.Create<CategoryCardViewModel>(cat =>
        {
            // prevent deleting locked/system categories (optional)
            if (cat.IsLocked) return;

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

        SaveUpsert = ReactiveCommand.Create(() =>
        {
            var trimmed = (CategoryName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            if (!CategoryTypes.Contains(SelectedType))
                SelectedType = "Expense";

            if (IsEditing && EditingCategory is not null)
            {
                // Update in-place (mock behavior)
                EditingCategory.Name = trimmed;
                EditingCategory.Type = SelectedType;

                // If type changed, move across sections
                MoveIfTypeChanged(EditingCategory);
            }
            else
            {
                // Create (mock behavior)
                var newCard = new CategoryCardViewModel(
                    name: trimmed,
                    transactionCount: 0,
                    isLocked: false,
                    type: SelectedType);

                AddToSection(newCard);
            }

            IsUpsertModalOpen = false;
            RaiseModalComputed();
        });

        ConfirmDelete = ReactiveCommand.Create(() =>
        {
            if (DeleteTarget is null)
                return;

            RemoveFromSection(DeleteTarget);

            // In the real backend, this is where you'd:
            // 1) Move its transactions to Uncategorized
            // 2) Delete the category
            // For now: just remove it.

            DeleteTarget = null;
            IsDeleteModalOpen = false;
            RaiseModalComputed();
        });

        // Keep IsAnyModalOpen reactive for bindings that depend on it
        this.WhenAnyValue(x => x.IsUpsertModalOpen, x => x.IsDeleteModalOpen)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsAnyModalOpen)));
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

    public CategoryCardViewModel(string name, int transactionCount, bool isLocked, string type)
    {
        _name = name;
        _transactionCount = transactionCount;
        _type = type;
        IsLocked = isLocked;
    }
}
