using ExpenseTracker.UI.ViewModels;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using static ExpenseTracker.UI.Features.Rules.RulesViewModel;

namespace ExpenseTracker.UI.Features.Rules
{
    // -------------------------
    // Condition row used in draft UI
    // -------------------------
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
            }
        }

        public ObservableCollection<OperatorOption> Operators { get; } = new();
        private OperatorOption? _selectedOperator;
        public OperatorOption? SelectedOperator
        {
            get => _selectedOperator;
            set => this.RaiseAndSetIfChanged(ref _selectedOperator, value);
        }

        private string _textValue = "";
        public string TextValue
        {
            get => _textValue;
            set => this.RaiseAndSetIfChanged(ref _textValue, value);
        }

        public ObservableCollection<string> AvailableCategories { get; }

        private string _selectedCategoryValue = "Uncategorized";
        public string SelectedCategoryValue
        {
            get => _selectedCategoryValue;
            set => this.RaiseAndSetIfChanged(ref _selectedCategoryValue, value);
        }

        private DateTime _dateValue;
        public DateTime DateValue
        {
            get => _dateValue;
            set => this.RaiseAndSetIfChanged(ref _dateValue, value);
        }

        private string _amountDollarsText = "";
        public string AmountDollarsText
        {
            get => _amountDollarsText;
            set => this.RaiseAndSetIfChanged(ref _amountDollarsText, value);
        }

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
}
