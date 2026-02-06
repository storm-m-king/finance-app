using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Rules;

public sealed class RulesViewModel : ViewModelBase
{
    public ObservableCollection<RuleRowViewModel> Rules { get; } = new();

    public ReactiveCommand<Unit, Unit> AddRule { get; }
    public ReactiveCommand<Unit, Unit> RerunAllRules { get; }

    private bool _isAddRuleModalOpen;
    public bool IsAddRuleModalOpen
    {
        get => _isAddRuleModalOpen;
        set => this.RaiseAndSetIfChanged(ref _isAddRuleModalOpen, value);
    }

    public AddRuleDraftViewModel AddRuleDraft { get; } = new();

    public ReactiveCommand<Unit, Unit> CloseAddRule { get; }
    public ReactiveCommand<Unit, Unit> SaveAddRule { get; }

    // -----------------------
    // Delete modal state + commands (minimal, focused on delete flow)
    // -----------------------
    private bool _isDeleteModalOpen;
    public bool IsDeleteModalOpen
    {
        get => _isDeleteModalOpen;
        private set => this.RaiseAndSetIfChanged(ref _isDeleteModalOpen, value);
    }

    private RuleRowViewModel? _deleteTarget;
    public RuleRowViewModel? DeleteTarget
    {
        get => _deleteTarget;
        private set
        {
            this.RaiseAndSetIfChanged(ref _deleteTarget, value);
            // Keep prompt text reactive
            this.RaisePropertyChanged(nameof(DeletePromptLine1));
            this.RaisePropertyChanged(nameof(DeletePromptLine2));
        }
    }

    public string DeletePromptLine1 =>
        DeleteTarget is null
            ? "Are you sure you want to delete this rule?"
            : $"Are you sure you want to delete “{DeleteTarget.Title}”?.";

    public string DeletePromptLine2 =>
        "This action cannot be undone.";

    public ReactiveCommand<RuleRowViewModel, Unit> OpenDelete { get; private set; }
    public ReactiveCommand<Unit, Unit> CloseModal { get; private set; }
    public ReactiveCommand<Unit, Unit> ConfirmDelete { get; private set; }

    public RulesViewModel()
    {
        // Seed sample rules using the same rendering logic as AddRuleDraft.BuildIfText/BuildThenText.
        // This ensures uniform text rendering and avoids constructing combined text inside a single condition.

        // Grocery rule
        var groceryDraft = new AddRuleDraftViewModel();
        groceryDraft.RuleTitle = "Classify Grocery Stores";
        groceryDraft.Conditions.Clear();
        var g1 = new ConditionRowViewModel(groceryDraft.AvailableCategories)
        {
            SelectedField = "Description",
            SelectedOperator = null
        };
        g1.SelectedOperator = g1.Operators.FirstOrDefault(o => o.Key == "contains");
        g1.TextValue = "Trader Joe";
        groceryDraft.Conditions.Add(g1);

        var g2 = new ConditionRowViewModel(groceryDraft.AvailableCategories)
        {
            ShowCombinator = true,
            SelectedCombinator = "OR",
            SelectedField = "Description"
        };
        g2.SelectedOperator = g2.Operators.FirstOrDefault(o => o.Key == "contains");
        g2.TextValue = "Whole Foods";
        groceryDraft.Conditions.Add(g2);

        var g3 = new ConditionRowViewModel(groceryDraft.AvailableCategories)
        {
            ShowCombinator = true,
            SelectedCombinator = "OR",
            SelectedField = "Description"
        };
        g3.SelectedOperator = g3.Operators.FirstOrDefault(o => o.Key == "contains");
        g3.TextValue = "Safeway";
        groceryDraft.Conditions.Add(g3);

        var groceryIf = groceryDraft.BuildIfText();
        var groceryThen = "Set category to 'Groceries'";
        Rules.Add(new RuleRowViewModel("Classify Grocery Stores", groceryIf, groceryThen, isEnabled: true));

        // Fast food
        var fastDraft = new AddRuleDraftViewModel();
        fastDraft.RuleTitle = "Classify Fast Food";
        fastDraft.Conditions.Clear();

        var f1 = new ConditionRowViewModel(fastDraft.AvailableCategories)
        {
            SelectedField = "Description"
        };
        f1.SelectedOperator = f1.Operators.FirstOrDefault(o => o.Key == "contains");
        f1.TextValue = "Chipotle";
        fastDraft.Conditions.Add(f1);

        var f2 = new ConditionRowViewModel(fastDraft.AvailableCategories)
        {
            ShowCombinator = true,
            SelectedCombinator = "OR",
            SelectedField = "Description"
        };
        f2.SelectedOperator = f2.Operators.FirstOrDefault(o => o.Key == "contains");
        f2.TextValue = "McDonalds";
        fastDraft.Conditions.Add(f2);

        var f3 = new ConditionRowViewModel(fastDraft.AvailableCategories)
        {
            ShowCombinator = true,
            SelectedCombinator = "OR",
            SelectedField = "Description"
        };
        f3.SelectedOperator = f3.Operators.FirstOrDefault(o => o.Key == "contains");
        f3.TextValue = "Taco Bell";
        fastDraft.Conditions.Add(f3);

        var fastIf = fastDraft.BuildIfText();
        var fastThen = "Set category to 'Dining Out'";
        Rules.Add(new RuleRowViewModel("Classify Fast Food", fastIf, fastThen, isEnabled: true));

        // Mark Paycheck: Description contains 'Payroll' AND Amount > 1000
        var payDraft = new AddRuleDraftViewModel();
        payDraft.RuleTitle = "Mark Paycheck";
        payDraft.Conditions.Clear();

        var p1 = new ConditionRowViewModel(payDraft.AvailableCategories)
        {
            SelectedField = "Description"
        };
        p1.SelectedOperator = p1.Operators.FirstOrDefault(o => o.Key == "contains");
        p1.TextValue = "Payroll";
        payDraft.Conditions.Add(p1);

        var p2 = new ConditionRowViewModel(payDraft.AvailableCategories)
        {
            ShowCombinator = true,
            SelectedCombinator = "AND",
            SelectedField = "Amount"
        };
        p2.SelectedOperator = p2.Operators.FirstOrDefault(o => o.Key == "gt");
        p2.AmountDollarsText = "1000";
        payDraft.Conditions.Add(p2);

        var payIf = payDraft.BuildIfText();
        var payThen = "Set category to 'Income'";
        Rules.Add(new RuleRowViewModel("Mark Paycheck", payIf, payThen, isEnabled: true));

        // Auto-approve small transactions (Amount < 10)
        var smallDraft = new AddRuleDraftViewModel();
        smallDraft.RuleTitle = "Auto-approve Small Transactions";
        smallDraft.Conditions.Clear();

        var s1 = new ConditionRowViewModel(smallDraft.AvailableCategories)
        {
            SelectedField = "Amount"
        };
        s1.SelectedOperator = s1.Operators.FirstOrDefault(o => o.Key == "lt");
        s1.AmountDollarsText = "10";
        smallDraft.Conditions.Add(s1);

        var smallIf = smallDraft.BuildIfText();
        var smallThen = "Set category to 'Uncategorized'"; // changed from 'Mark as Reviewed'
        Rules.Add(new RuleRowViewModel("Auto-approve Small Transactions", smallIf, smallThen, isEnabled: false));

        AddRule = ReactiveCommand.Create(() =>
        {
            AddRuleDraft.Reset();
            IsAddRuleModalOpen = true;
        });

        CloseAddRule = ReactiveCommand.Create(() =>
        {
            IsAddRuleModalOpen = false;
        });

        // SaveAddRule now enabled only when AddRuleDraft.IsValid == true
        SaveAddRule = ReactiveCommand.Create(
            () =>
            {
                var title = string.IsNullOrWhiteSpace(AddRuleDraft.RuleTitle)
                    ? "New Rule"
                    : AddRuleDraft.RuleTitle.Trim();

                var ifText = AddRuleDraft.BuildIfText();
                var thenText = AddRuleDraft.BuildThenText();

                Rules.Add(new RuleRowViewModel(title, ifText, thenText, isEnabled: true));
                Reindex();

                IsAddRuleModalOpen = false;
            },
            AddRuleDraft.WhenAnyValue(d => d.IsValid)
        );

        RerunAllRules = ReactiveCommand.Create(() => { /* later: call service */ });

        // ---- Delete command implementations (minimal, consistent with Categories pattern) ----
        OpenDelete = ReactiveCommand.Create<RuleRowViewModel>(rule =>
        {
            if (rule is null) return;

            DeleteTarget = rule;
            IsDeleteModalOpen = true;
        });

        CloseModal = ReactiveCommand.Create(() =>
        {
            IsDeleteModalOpen = false;
            DeleteTarget = null;
        });

        ConfirmDelete = ReactiveCommand.Create(() =>
        {
            if (DeleteTarget is null) return;

            Rules.Remove(DeleteTarget);
            Reindex();

            DeleteTarget = null;
            IsDeleteModalOpen = false;
        });

        Reindex();
    }

    public int IndexOf(RuleRowViewModel rule) => Rules.IndexOf(rule);

    /// <summary>
    /// Moves a rule to an "insertion index" (0..Count). This is snap-to-slot.
    /// </summary>
    public void MoveRuleToIndex(RuleRowViewModel dragging, int insertionIndex)
    {
        var from = Rules.IndexOf(dragging);
        if (from < 0) return;

        // clamp insertion index to [0..Count]
        if (insertionIndex < 0) insertionIndex = 0;
        if (insertionIndex > Rules.Count) insertionIndex = Rules.Count;

        // If you remove an item above the insertion point, the insertion point shifts up by 1.
        var adjusted = insertionIndex;
        if (adjusted > from) adjusted--;

        if (adjusted == from) return;

        Rules.RemoveAt(from);
        Rules.Insert(adjusted, dragging);

        Reindex();
    }

    public void ClearAllDragHints()
    {
        foreach (var r in Rules)
            r.ClearDragHints();
    }

    private void Reindex()
    {
        for (int i = 0; i < Rules.Count; i++)
            Rules[i].Order = i + 1;
    }

    // Helper to clone a ConditionRowViewModel (so rule gets its own instances)
    private static ConditionRowViewModel CloneCondition(ConditionRowViewModel src, ObservableCollection<string> categories)
    {
        var dest = new ConditionRowViewModel(categories)
        {
            ShowCombinator = src.ShowCombinator,
            SelectedCombinator = src.SelectedCombinator,
            SelectedField = src.SelectedField,
            TextValue = src.TextValue,
            SelectedCategoryValue = src.SelectedCategoryValue,
            DateValue = src.DateValue,
            AmountDollarsText = src.AmountDollarsText
        };

        // Attempt to pick the same operator by key
        if (src.SelectedOperator is not null)
        {
            var match = dest.Operators.FirstOrDefault(o => o.Key == src.SelectedOperator.Key);
            if (match is not null)
                dest.SelectedOperator = match;
        }

        return dest;
    }

    // ============================
    // Add Rule Draft VM
    // ============================
    public sealed class AddRuleDraftViewModel : ViewModelBase
    {
        public ObservableCollection<ConditionRowViewModel> Conditions { get; } = new();

        private string _ruleTitle = "";
        public string RuleTitle
        {
            get => _ruleTitle;
            set => this.RaiseAndSetIfChanged(ref _ruleTitle, value);
        }

        // THEN
        public ObservableCollection<string> ThenActions { get; } = new()
        {
            "Set Category"
        };

        private string _selectedThenAction = "Set Category";
        public string SelectedThenAction
        {
            get => _selectedThenAction;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedThenAction, value);
                this.RaisePropertyChanged(nameof(IsSetCategoryAction));
            }
        }

        public bool IsSetCategoryAction => SelectedThenAction == "Set Category";

        public ObservableCollection<string> AvailableCategories { get; } = new()
        {
            "Uncategorized",
            "Groceries",
            "Dining Out",
            "Income",
            "Utilities",
            "Shopping",
            "Healthcare",
            "Transport"
        };

        private string? _selectedCategory = "Groceries";
        public string? SelectedCategory
        {
            get => _selectedCategory;
            set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        }

        public ReactiveCommand<Unit, Unit> AddCondition { get; }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            private set => this.RaiseAndSetIfChanged(ref _isValid, value);
        }

        public AddRuleDraftViewModel()
        {
            AddCondition = ReactiveCommand.Create(AddConditionRow);
            Reset();

            // react to collection changes to maintain validity
            Conditions.CollectionChanged += ConditionsOnCollectionChanged;
            RecomputeIsValid();
        }

        public void Reset()
        {
            RuleTitle = "";
            SelectedThenAction = "Set Category";
            SelectedCategory = AvailableCategories.FirstOrDefault();

            // detach existing handlers
            foreach (var c in Conditions.OfType<INotifyPropertyChanged>())
                c.PropertyChanged -= ConditionPropertyChanged;

            Conditions.Clear();
            AddConditionRow();
            RecomputeRowFlags();
            RecomputeIsValid();
        }

        private void ConditionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (var o in e.OldItems.OfType<INotifyPropertyChanged>())
                    o.PropertyChanged -= ConditionPropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (var n in e.NewItems.OfType<INotifyPropertyChanged>())
                    n.PropertyChanged += ConditionPropertyChanged;
            }

            RecomputeRowFlags();
            RecomputeIsValid();
        }

        private void ConditionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Any condition's property change may affect validity.
            RecomputeIsValid();
        }

        private void AddConditionRow()
        {
            var row = new ConditionRowViewModel(AvailableCategories);

            row.RemoveMe = ReactiveCommand.Create(() =>
            {
                if (Conditions.Contains(row))
                    Conditions.Remove(row);

                if (Conditions.Count == 0)
                    Conditions.Add(new ConditionRowViewModel(AvailableCategories));

                RecomputeRowFlags();
                RecomputeIsValid();
            });

            Conditions.Add(row);
            RecomputeRowFlags();
            RecomputeIsValid();
        }

        private void RecomputeRowFlags()
        {
            for (int i = 0; i < Conditions.Count; i++)
                Conditions[i].ShowCombinator = i != 0;
        }

        private void RecomputeIsValid()
        {
            // Each condition must be valid (Description must have text, Amount must parse).
            var allValid = Conditions.All(c => c.IsValid);

            // If THEN is Set Category ensure a category is selected (defensive).
            if (SelectedThenAction == "Set Category" && string.IsNullOrWhiteSpace(SelectedCategory))
                allValid = false;

            IsValid = allValid;
        }

        public string BuildIfText()
        {
            // Natural language output that mirrors UI selection
            // Example:
            // Description Contains 'Chipotle' AND Amount Is greater than $10.00
            var parts = Conditions.Select((c, i) =>
            {
                var field = c.SelectedField;
                var op = c.SelectedOperator?.Label ?? "";
                var renderedValue = c.RenderValueForPreview();

                var core = string.IsNullOrWhiteSpace(renderedValue)
                    ? $"{field} {op}".Trim()
                    : $"{field} {op} {renderedValue}".Trim();

                if (i == 0) return core;
                return $"{c.SelectedCombinator} {core}";
            });

            return string.Join(" ", parts).Trim();
        }

        public string BuildThenText()
        {
            // Only "Set Category" is supported by the current storage model.
            return SelectedThenAction switch
            {
                "Set Category" => $"Set category to '{(SelectedCategory ?? "Uncategorized")}'",
                _ => $"Set category to '{(SelectedCategory ?? "Uncategorized")}'"
            };
        }
    }

    // ============================
    // Condition Row VM
    // ============================
    public sealed class ConditionRowViewModel : ViewModelBase
    {
        public ConditionRowViewModel(ObservableCollection<string> categories)
        {
            AvailableCategories = categories;
            SelectedCategoryValue = AvailableCategories.FirstOrDefault() ?? "Uncategorized";

            RefreshOperators();
            RaiseValueVisibilityChanged();
        }

        private bool _showCombinator;
        public bool ShowCombinator
        {
            get => _showCombinator;
            set => this.RaiseAndSetIfChanged(ref _showCombinator, value);
        }

        public ObservableCollection<string> Combinators { get; } = new() { "AND", "OR" };

        private string _selectedCombinator = "AND";
        public string SelectedCombinator
        {
            get => _selectedCombinator;
            set => this.RaiseAndSetIfChanged(ref _selectedCombinator, value);
        }

        public ObservableCollection<string> Fields { get; } = new()
        {
            "Date",
            "Description",
            "Amount",
            "Category"
        };

        private string _selectedField = "Description";
        public string SelectedField
        {
            get => _selectedField;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedField, value);
                RefreshOperators();
                ResetValueForField();
                RaiseValueVisibilityChanged();
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        public ObservableCollection<OperatorOption> Operators { get; } = new();

        private OperatorOption? _selectedOperator;
        public OperatorOption? SelectedOperator
        {
            get => _selectedOperator;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOperator, value);
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        // Text field (Description)
        private string _textValue = "";
        public string TextValue
        {
            get => _textValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _textValue, value);
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        // Category field
        public ObservableCollection<string> AvailableCategories { get; }

        private string _selectedCategoryValue = "Uncategorized";
        public string SelectedCategoryValue
        {
            get => _selectedCategoryValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCategoryValue, value);
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        // Date field
        private DateTime _dateValue;
        public DateTime DateValue
        {
            get => _dateValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _dateValue, value);
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        // Amount field (stored as long cents; input is dollars)
        private string _amountDollarsText = "";
        public string AmountDollarsText
        {
            get => _amountDollarsText;
            set
            {
                this.RaiseAndSetIfChanged(ref _amountDollarsText, value);
                this.RaisePropertyChanged(nameof(IsValid));
            }
        }

        // Visibility flags used by the View
        public bool IsTextValueVisible => SelectedField == "Description";
        public bool IsCategoryValueVisible => SelectedField == "Category";
        public bool IsDateValueVisible => SelectedField == "Date";
        public bool IsAmountValueVisible => SelectedField == "Amount";

        public ReactiveCommand<Unit, Unit> RemoveMe { get; set; } = ReactiveCommand.Create(() => { });

        private void RaiseValueVisibilityChanged()
        {
            this.RaisePropertyChanged(nameof(IsTextValueVisible));
            this.RaisePropertyChanged(nameof(IsCategoryValueVisible));
            this.RaisePropertyChanged(nameof(IsDateValueVisible));
            this.RaisePropertyChanged(nameof(IsAmountValueVisible));
        }

        private void RefreshOperators()
        {
            Operators.Clear();

            if (SelectedField == "Description")
            {
                Operators.Add(new OperatorOption("contains", "Contains"));
                Operators.Add(new OperatorOption("not_contains", "Does not contain"));
                Operators.Add(new OperatorOption("starts_with", "Starts with"));
                Operators.Add(new OperatorOption("ends_with", "Ends with"));
            }
            else if (SelectedField == "Category")
            {
                Operators.Add(new OperatorOption("is", "Is"));
                Operators.Add(new OperatorOption("is_not", "Is not"));
            }
            else if (SelectedField == "Amount")
            {
                Operators.Add(new OperatorOption("gt", "Is greater than"));
                Operators.Add(new OperatorOption("lt", "Is less than"));
                Operators.Add(new OperatorOption("eq", "Is equal to"));
            }
            else // Date
            {
                Operators.Add(new OperatorOption("after", "Is after"));
                Operators.Add(new OperatorOption("before", "Is before"));
                Operators.Add(new OperatorOption("on", "Is on"));
            }

            SelectedOperator = Operators.FirstOrDefault();
        }

        private void ResetValueForField()
        {
            TextValue = "";
            AmountDollarsText = "";
            DateValue = DateTime.Now;

            if (SelectedField == "Category" && string.IsNullOrWhiteSpace(SelectedCategoryValue))
                SelectedCategoryValue = AvailableCategories.FirstOrDefault() ?? "Uncategorized";
        }

        public string RenderValueForPreview()
        {
            if (SelectedField == "Description")
            {
                var v = (TextValue ?? "").Trim();
                return string.IsNullOrWhiteSpace(v) ? "" : $"'{v}'";
            }

            if (SelectedField == "Category")
            {
                var v = (SelectedCategoryValue ?? "").Trim();
                return string.IsNullOrWhiteSpace(v) ? "" : $"'{v}'";
            }

            if (SelectedField == "Date")
            {
                if (DateValue == default) return "";
                return DateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            // Amount
            var cents = ParseAmountTextToCents(AmountDollarsText);
            if (cents is null) return "";
            return FormatCentsAsDollars(cents.Value);
        }

        public bool IsValid
        {
            get
            {
                if (SelectedField == "Description")
                    return !string.IsNullOrWhiteSpace(TextValue);

                if (SelectedField == "Amount")
                    return ParseAmountTextToCents(AmountDollarsText) is not null;

                if (SelectedField == "Category")
                    return !string.IsNullOrWhiteSpace(SelectedCategoryValue);

                if (SelectedField == "Date")
                    return DateValue != default;

                return true;
            }
        }

        private static long? ParseAmountTextToCents(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var cleaned = text.Trim().Replace("$", "");

            if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var dollars))
                return null;

            var cents = (long)Math.Round(dollars * 100m, MidpointRounding.AwayFromZero);
            return cents;
        }

        private static string FormatCentsAsDollars(long cents)
        {
            var abs = Math.Abs(cents);
            var dollars = abs / 100;
            var rem = abs % 100;
            var sign = cents < 0 ? "-" : "";
            return $"{sign}${dollars}.{rem:00}";
        }
    }

    public sealed record OperatorOption(string Key, string Label);
}
