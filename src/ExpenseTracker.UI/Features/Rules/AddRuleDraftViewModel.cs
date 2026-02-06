using ExpenseTracker.UI.ViewModels;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using static ExpenseTracker.UI.Features.Rules.RulesViewModel;

namespace ExpenseTracker.UI.Features.Rules
{
    // -------------------------
    // Draft VM used by Add/Edit modals
    // -------------------------
    public sealed class AddRuleDraftViewModel : ViewModelBase
    {
        public ObservableCollection<ConditionRowViewModel> Conditions { get; } = new();

        private string _ruleTitle = "";
        public string RuleTitle
        {
            get => _ruleTitle;
            set => this.RaiseAndSetIfChanged(ref _ruleTitle, value);
        }

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
                RecomputeIsValid();
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
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCategory, value);
                RecomputeIsValid();
            }
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

            Conditions.CollectionChanged += ConditionsOnCollectionChanged;
            RecomputeIsValid();
        }

        public void Reset()
        {
            RuleTitle = "";
            SelectedThenAction = "Set Category";
            SelectedCategory = AvailableCategories.FirstOrDefault();

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

        private void ConditionPropertyChanged(object? sender, PropertyChangedEventArgs e) => RecomputeIsValid();

        private void AddConditionRow()
        {
            var row = new ConditionRowViewModel(AvailableCategories);
            row.RemoveMe = ReactiveCommand.Create(() =>
            {
                if (Conditions.Contains(row))
                    Conditions.Remove(row);
                if (Conditions.Count == 0)
                    Conditions.Add(new ConditionRowViewModel(AvailableCategories));
            });

            Conditions.Add(row);
        }

        private void RecomputeRowFlags()
        {
            for (int i = 0; i < Conditions.Count; i++)
                Conditions[i].ShowCombinator = i != 0;
        }

        private void RecomputeIsValid()
        {
            var allValid = Conditions.All(c => c.IsValid);

            if (SelectedThenAction == "Set Category" && string.IsNullOrWhiteSpace(SelectedCategory))
                allValid = false;

            IsValid = allValid;
        }

        public string BuildIfText()
        {
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
            return SelectedThenAction switch
            {
                "Set Category" => $"Set category to '{(SelectedCategory ?? "Uncategorized")}'",
                _ => $"Set category to '{(SelectedCategory ?? "Uncategorized")}'"
            };
        }
    }
}
