using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Text.RegularExpressions;
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

    private bool _isEditRuleModalOpen;
    public bool IsEditRuleModalOpen
    {
        get => _isEditRuleModalOpen;
        private set => this.RaiseAndSetIfChanged(ref _isEditRuleModalOpen, value);
    }

    private bool _isDeleteModalOpen;
    public bool IsDeleteModalOpen
    {
        get => _isDeleteModalOpen;
        private set => this.RaiseAndSetIfChanged(ref _isDeleteModalOpen, value);
    }

    public AddRuleDraftViewModel AddRuleDraft { get; } = new();

    private RuleRowViewModel? _editTarget;
    private RuleRowViewModel? _deleteTarget;

    public string DeletePromptLine1 =>
        _deleteTarget is null
            ? "Are you sure you want to delete this rule?"
            : $"Are you sure you want to delete “{_deleteTarget.Title}”?	";

    public string DeletePromptLine2 => "This action cannot be undone.";

    public ReactiveCommand<Unit, Unit> CloseAddRule { get; }
    public ReactiveCommand<Unit, Unit> SaveAddRule { get; }

    public ReactiveCommand<RuleRowViewModel, Unit> OpenEdit { get; private set; }
    public ReactiveCommand<Unit, Unit> CloseEdit { get; private set; }
    public ReactiveCommand<Unit, Unit> SaveEdit { get; private set; }

    public ReactiveCommand<RuleRowViewModel, Unit> OpenDelete { get; private set; }
    public ReactiveCommand<Unit, Unit> CloseModal { get; private set; }
    public ReactiveCommand<Unit, Unit> ConfirmDelete { get; private set; }

    public RulesViewModel()
    {
        // Seed a few example rules (preview text produced by drafts for consistency)
        var draft = new AddRuleDraftViewModel();
        draft.RuleTitle = "Classify Grocery Stores";
        draft.Conditions.Clear();
        var g1 = new ConditionRowViewModel(draft.AvailableCategories) { SelectedField = "Description", TextValue = "Trader Joe" };
        g1.SelectedOperator = g1.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(g1);
        var g2 = new ConditionRowViewModel(draft.AvailableCategories) { ShowCombinator = true, SelectedCombinator = "OR", SelectedField = "Description", TextValue = "Whole Foods" };
        g2.SelectedOperator = g2.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(g2);
        var g3 = new ConditionRowViewModel(draft.AvailableCategories) { ShowCombinator = true, SelectedCombinator = "OR", SelectedField = "Description", TextValue = "Safeway" };
        g3.SelectedOperator = g3.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(g3);
        Rules.Add(new RuleRowViewModel("Classify Grocery Stores", draft.BuildIfText(), "Set category to 'Groceries'", isEnabled: true));

        // Fast food
        draft.Reset();
        draft.RuleTitle = "Classify Fast Food";
        draft.Conditions.Clear();
        var f1 = new ConditionRowViewModel(draft.AvailableCategories) { SelectedField = "Description", TextValue = "Chipotle" };
        f1.SelectedOperator = f1.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(f1);
        var f2 = new ConditionRowViewModel(draft.AvailableCategories) { ShowCombinator = true, SelectedCombinator = "OR", SelectedField = "Description", TextValue = "McDonalds" };
        f2.SelectedOperator = f2.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(f2);
        var f3 = new ConditionRowViewModel(draft.AvailableCategories) { ShowCombinator = true, SelectedCombinator = "OR", SelectedField = "Description", TextValue = "Taco Bell" };
        f3.SelectedOperator = f3.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(f3);
        Rules.Add(new RuleRowViewModel("Classify Fast Food", draft.BuildIfText(), "Set category to 'Dining Out'", isEnabled: true));

        // Paycheck
        draft.Reset();
        draft.RuleTitle = "Mark Paycheck";
        draft.Conditions.Clear();
        var p1 = new ConditionRowViewModel(draft.AvailableCategories) { SelectedField = "Description", TextValue = "Payroll" };
        p1.SelectedOperator = p1.Operators.FirstOrDefault(o => o.Key == "contains");
        draft.Conditions.Add(p1);
        var p2 = new ConditionRowViewModel(draft.AvailableCategories) { ShowCombinator = true, SelectedCombinator = "AND", SelectedField = "Amount", AmountDollarsText = "1000" };
        p2.SelectedOperator = p2.Operators.FirstOrDefault(o => o.Key == "gt");
        draft.Conditions.Add(p2);
        Rules.Add(new RuleRowViewModel("Mark Paycheck", draft.BuildIfText(), "Set category to 'Income'", isEnabled: true));

        AddRule = ReactiveCommand.Create(() =>
        {
            AddRuleDraft.Reset();
            IsAddRuleModalOpen = true;
        });

        CloseAddRule = ReactiveCommand.Create(() => { IsAddRuleModalOpen = false; });

        SaveAddRule = ReactiveCommand.Create(
            () =>
            {
                var title = string.IsNullOrWhiteSpace(AddRuleDraft.RuleTitle) ? "New Rule" : AddRuleDraft.RuleTitle.Trim();
                var ifText = AddRuleDraft.BuildIfText();
                var thenText = AddRuleDraft.BuildThenText();

                Rules.Add(new RuleRowViewModel(title, ifText, thenText, isEnabled: true));
                Reindex();
                IsAddRuleModalOpen = false;
            },
            AddRuleDraft.WhenAnyValue(x => x.IsValid)
        );

        RerunAllRules = ReactiveCommand.Create(() => { /* placeholder */ });

        OpenEdit = ReactiveCommand.Create<RuleRowViewModel>(OpenEditImpl);
        CloseEdit = ReactiveCommand.Create(() =>
        {
            IsEditRuleModalOpen = false;
            _editTarget = null;
        });
        SaveEdit = ReactiveCommand.Create(
            () =>
            {
                if (_editTarget is null) return;
                var title = string.IsNullOrWhiteSpace(AddRuleDraft.RuleTitle) ? "New Rule" : AddRuleDraft.RuleTitle.Trim();
                var ifText = AddRuleDraft.BuildIfText();
                var thenText = AddRuleDraft.BuildThenText();

                var idx = Rules.IndexOf(_editTarget);
                var enabled = _editTarget.IsEnabled;
                if (idx >= 0)
                    Rules[idx] = new RuleRowViewModel(title, ifText, thenText, enabled);

                _editTarget = null;
                IsEditRuleModalOpen = false;
                Reindex();
            },
            AddRuleDraft.WhenAnyValue(x => x.IsValid)
        );

        OpenDelete = ReactiveCommand.Create<RuleRowViewModel>(rule =>
        {
            if (rule is null) return;
            _deleteTarget = rule;
            IsDeleteModalOpen = true;
            this.RaisePropertyChanged(nameof(DeletePromptLine1));
        });

        CloseModal = ReactiveCommand.Create(() =>
        {
            IsDeleteModalOpen = false;
            _delete_target_clear();
        });

        ConfirmDelete = ReactiveCommand.Create(() =>
        {
            if (_deleteTarget is null) return;
            Rules.Remove(_deleteTarget);
            Reindex();
            // Close modal and clear target so UI doesn't remain focused on an empty selection
            IsDeleteModalOpen = false;
            _delete_target_clear();
        });

        Reindex();
    }

    private void _delete_target_clear()
    {
        _deleteTarget = null;
        this.RaisePropertyChanged(nameof(DeletePromptLine1));
    }

    private void OpenEditImpl(RuleRowViewModel rule)
    {
        if (rule is null) return;

        // Reset draft
        AddRuleDraft.Reset();
        AddRuleDraft.RuleTitle = rule.Title ?? "";

        // THEN: parse Set category to 'X' (exact format produced by BuildThenText)
        if (!string.IsNullOrWhiteSpace(rule.ThenText) && rule.ThenText.StartsWith("Set category to", StringComparison.OrdinalIgnoreCase))
        {
            // Use regex to reliably extract the quoted category (handles edge cases)
            var m = Regex.Match(rule.ThenText, @"'([^']+)'");
            if (m.Success)
            {
                var cat = m.Groups[1].Value;
                if (AddRuleDraft.AvailableCategories.Contains(cat))
                    AddRuleDraft.SelectedCategory = cat;
            }
            AddRuleDraft.SelectedThenAction = "Set Category";
        }

        // IF: parse preview created by BuildIfText (stable format)
        AddRuleDraft.Conditions.Clear();
        var ifText = (rule.IfText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ifText))
        {
            AddRuleDraft.AddCondition.Execute().Subscribe();
        }
        else
        {
            // Pattern: optional leading combinator then Field OperatorLabel [Value]
            // We rely on the exact labels produced by BuildIfText (Operator.Label).
            var pattern = @"\s*(AND|OR)?\s*(Date|Description|Amount|Category)\s+([A-Za-z\s]+?)(?:\s+('.*?'|\$[\d,]+\.\d{2}|\d{4}-\d{2}-\d{2}))?(?=(\s+(?:AND|OR)\s+|$))";
            var matches = Regex.Matches(ifText, pattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
            {
                // fallback: single description condition with the whole preview text
                var row = new ConditionRowViewModel(AddRuleDraft.AvailableCategories) { SelectedField = "Description", TextValue = ifText };
                row.SelectedOperator = row.Operators.FirstOrDefault(o => o.Key == "contains");
                // ensure RemoveMe behaves same as AddConditionRow
                row.RemoveMe = ReactiveCommand.Create(() =>
                {
                    if (AddRuleDraft.Conditions.Contains(row))
                        AddRuleDraft.Conditions.Remove(row);
                    if (AddRuleDraft.Conditions.Count == 0)
                        AddRuleDraft.AddCondition.Execute().Subscribe();
                });
                AddRuleDraft.Conditions.Add(row);
            }
            else
            {
                var first = true;
                foreach (Match m in matches)
                {
                    var comb = m.Groups[1].Value;
                    var field = m.Groups[2].Value;
                    var opLabel = m.Groups[3].Value.Trim();
                    var rawVal = m.Groups[4].Value;

                    var row = new ConditionRowViewModel(AddRuleDraft.AvailableCategories) { SelectedField = field };
                    row.ShowCombinator = !first;
                    if (!first && !string.IsNullOrWhiteSpace(comb))
                        row.SelectedCombinator = comb.ToUpperInvariant();

                    // map operator by Label (case-insensitive)
                    var opMatch = row.Operators.FirstOrDefault(o => string.Equals(o.Label, opLabel, StringComparison.OrdinalIgnoreCase));
                    if (opMatch is not null) row.SelectedOperator = opMatch;

                    if (!string.IsNullOrWhiteSpace(rawVal))
                    {
                        if (rawVal.StartsWith("'") && rawVal.EndsWith("'") && rawVal.Length >= 2)
                        {
                            var inner = rawVal.Substring(1, rawVal.Length - 2);
                            if (field.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                row.TextValue = inner;
                            else if (field.Equals("Category", StringComparison.OrdinalIgnoreCase))
                                row.SelectedCategoryValue = inner;
                        }
                        else if (rawVal.StartsWith("$"))
                        {
                            var cleaned = rawVal.Replace("$", "").Replace(",", "");
                            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                                row.AmountDollarsText = d.ToString("0.##", CultureInfo.InvariantCulture);
                        }
                        else if (DateTime.TryParseExact(rawVal, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        {
                            row.DateValue = dt;
                        }
                        else
                        {
                            if (field.Equals("Description", StringComparison.OrdinalIgnoreCase))
                                row.TextValue = rawVal.Trim('\'');
                        }
                    }

                    // ensure RemoveMe behaves same as AddConditionRow so user can delete parsed conditions
                    row.RemoveMe = ReactiveCommand.Create(() =>
                    {
                        if (AddRuleDraft.Conditions.Contains(row))
                            AddRuleDraft.Conditions.Remove(row);
                        if (AddRuleDraft.Conditions.Count == 0)
                            AddRuleDraft.AddCondition.Execute().Subscribe();
                    });

                    AddRuleDraft.Conditions.Add(row);
                    first = false;
                }
            }
        }

        _editTarget = rule;
        IsEditRuleModalOpen = true;
    }

    public int IndexOf(RuleRowViewModel rule) => Rules.IndexOf(rule);

    public void MoveRuleToIndex(RuleRowViewModel dragging, int insertionIndex)
    {
        var from = Rules.IndexOf(dragging);
        if (from < 0) return;

        if (insertionIndex < 0) insertionIndex = 0;
        if (insertionIndex > Rules.Count) insertionIndex = Rules.Count;

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

    public sealed record OperatorOption(string Key, string Label);
}
