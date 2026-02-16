using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using ReactiveUI;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.UI.ViewModels;

namespace ExpenseTracker.UI.Features.Rules;

public sealed class RulesViewModel : ViewModelBase
{
    private readonly IRuleService _ruleService;
    private readonly ICategoryService _categoryService;
    private readonly IAppLogger _logger;

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
            : $"Are you sure you want to delete '{_deleteTarget.Title}'?";

    public string DeletePromptLine2 => "This action cannot be undone.";

    public ReactiveCommand<Unit, Unit> CloseAddRule { get; }
    public ReactiveCommand<Unit, Unit> SaveAddRule { get; }

    public ReactiveCommand<RuleRowViewModel, Unit> OpenEdit { get; private set; }
    public ReactiveCommand<Unit, Unit> CloseEdit { get; private set; }
    public ReactiveCommand<Unit, Unit> SaveEdit { get; private set; }

    public ReactiveCommand<RuleRowViewModel, Unit> OpenDelete { get; private set; }
    public ReactiveCommand<Unit, Unit> CloseModal { get; private set; }
    public ReactiveCommand<Unit, Unit> ConfirmDelete { get; private set; }

    public RulesViewModel(IRuleService ruleService, ICategoryService categoryService, IAppLogger logger)
    {
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        AddRule = ReactiveCommand.Create(() =>
        {
            AddRuleDraft.Reset();
            LoadCategoriesIntoDraft();
            IsAddRuleModalOpen = true;
        });

        CloseAddRule = ReactiveCommand.Create(() => { IsAddRuleModalOpen = false; });

        SaveAddRule = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var title = string.IsNullOrWhiteSpace(AddRuleDraft.RuleTitle) ? "New Rule" : AddRuleDraft.RuleTitle.Trim();
                var ifText = AddRuleDraft.BuildIfText();
                var thenText = AddRuleDraft.BuildThenText();

                var condition = BuildConditionFromDraft();
                var categoryId = await ResolveCategoryIdAsync(AddRuleDraft.SelectedCategory);
                var priority = Rules.Count;

                var rule = await _ruleService.CreateRuleAsync(title, condition, categoryId, priority);
                var ruleRow = new RuleRowViewModel(title, ifText, thenText, isEnabled: true, id: rule.Id);
                SubscribeToEnabledToggle(ruleRow);
                Rules.Add(ruleRow);
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
        SaveEdit = ReactiveCommand.CreateFromTask(
            async () =>
            {
                if (_editTarget is null) return;
                var title = string.IsNullOrWhiteSpace(AddRuleDraft.RuleTitle) ? "New Rule" : AddRuleDraft.RuleTitle.Trim();
                var ifText = AddRuleDraft.BuildIfText();
                var thenText = AddRuleDraft.BuildThenText();

                var condition = BuildConditionFromDraft();
                var categoryId = await ResolveCategoryIdAsync(AddRuleDraft.SelectedCategory);
                var idx = Rules.IndexOf(_editTarget);
                var enabled = _editTarget.IsEnabled;

                await _ruleService.UpdateRuleAsync(_editTarget.Id, title, condition, categoryId, idx >= 0 ? idx : 0, enabled);

                if (idx >= 0)
                {
                    var updatedRow = new RuleRowViewModel(title, ifText, thenText, enabled, id: _editTarget.Id);
                    SubscribeToEnabledToggle(updatedRow);
                    Rules[idx] = updatedRow;
                }

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

        ConfirmDelete = ReactiveCommand.CreateFromTask(async () =>
        {
            if (_deleteTarget is null) return;
            await _ruleService.DeleteRuleAsync(_deleteTarget.Id);
            Rules.Remove(_deleteTarget);
            Reindex();
            IsDeleteModalOpen = false;
            _delete_target_clear();
        });

        Reindex();

        // Load persisted rules from database
        _ = LoadRulesAsync();
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
        LoadCategoriesIntoDraft();
        AddRuleDraft.RuleTitle = rule.Title ?? "";

        // THEN: parse Set category to 'X' (exact format produced by BuildThenText)
        if (!string.IsNullOrWhiteSpace(rule.ThenText) && rule.ThenText.StartsWith("Set category to", StringComparison.OrdinalIgnoreCase))
        {
            // Use regex to reliably extract the quoted category (handles edge cases)
            var m = Regex.Match(rule.ThenText, @"'([^']+)'");
            if (m.Success)
            {
                var cat = m.Groups[1].Value;
                // Categories are displayed as "Name (Type)" — match by name prefix
                var match = AddRuleDraft.AvailableCategories.FirstOrDefault(c =>
                    c.StartsWith(cat + " (", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c, cat, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    AddRuleDraft.SelectedCategory = match;
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
        PersistReorderAsync();
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

    // =====================================================
    // Persistence helpers
    // =====================================================

    /// <summary>
    /// Loads rules from the database into the Rules collection.
    /// Called once during initialization.
    /// </summary>
    public async Task LoadRulesAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

            var rules = await _ruleService.GetAllRulesAsync();
            Rules.Clear();

            foreach (var rule in rules.OrderBy(r => r.Priority))
            {
                var categoryName = categoryLookup.TryGetValue(rule.CategoryId, out var name)
                    ? name
                    : "Uncategorized";

                var ifText = BuildIfTextFromCondition(rule.Condition);
                var thenText = $"Set category to '{categoryName}'";

                var ruleRow = new RuleRowViewModel(
                    title: rule.Name,
                    ifText: ifText,
                    thenText: thenText,
                    isEnabled: rule.Enabled,
                    id: rule.Id);

                SubscribeToEnabledToggle(ruleRow);
                Rules.Add(ruleRow);
            }

            Reindex();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load rules from database.", ex);
        }
    }

    private void SubscribeToEnabledToggle(RuleRowViewModel ruleRow)
    {
        ruleRow.WhenAnyValue(x => x.IsEnabled)
            .Skip(1) // skip initial value
            .Subscribe(async enabled =>
            {
                try
                {
                    await _ruleService.SetEnabledAsync(ruleRow.Id, enabled);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to persist enabled state for rule '{ruleRow.Id}'.", ex);
                }
            });
    }

    private async void PersistReorderAsync()
    {
        try
        {
            var orderedIds = Rules.Select(r => r.Id).ToList();
            await _ruleService.ReorderAsync(orderedIds);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to persist rule reorder.", ex);
        }
    }

    private void LoadCategoriesIntoDraft()
    {
        try
        {
            var categories = _categoryService.GetAllCategoriesAsync().GetAwaiter().GetResult();
            AddRuleDraft.AvailableCategories.Clear();
            foreach (var cat in categories)
                AddRuleDraft.AvailableCategories.Add($"{cat.Name} ({cat.Type})");

            if (AddRuleDraft.AvailableCategories.Count > 0)
                AddRuleDraft.SelectedCategory = AddRuleDraft.AvailableCategories.First();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load categories for rule draft.", ex);
        }
    }

    private async Task<Guid> ResolveCategoryIdAsync(string? categoryDisplay)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();

        if (!string.IsNullOrWhiteSpace(categoryDisplay))
        {
            // Parse "Name (Type)" format back to just the name
            var categoryName = categoryDisplay;
            var parenIdx = categoryDisplay.LastIndexOf(" (", StringComparison.Ordinal);
            if (parenIdx > 0)
                categoryName = categoryDisplay[..parenIdx];

            var match = categories.FirstOrDefault(c =>
                string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                return match.Id;
        }

        return categories.First(c =>
            string.Equals(c.Name, "Uncategorized", StringComparison.OrdinalIgnoreCase)).Id;
    }

    /// <summary>
    /// Builds a domain <see cref="IRuleCondition"/> from the current draft conditions.
    /// </summary>
    private IRuleCondition BuildConditionFromDraft()
    {
        var conditions = new List<IRuleCondition>();

        foreach (var row in AddRuleDraft.Conditions)
        {
            var condition = BuildSingleCondition(row);
            if (condition is not null)
                conditions.Add(condition);
        }

        if (conditions.Count == 0)
            return new ContainsCondition("*");

        if (conditions.Count == 1)
            return conditions[0];

        // Determine combinator from the second condition row
        var combinator = AddRuleDraft.Conditions.Count > 1
            ? AddRuleDraft.Conditions[1].SelectedCombinator
            : "AND";

        return combinator == "OR"
            ? new OrCondition(conditions)
            : new AndCondition(conditions);
    }

    private static IRuleCondition? BuildSingleCondition(ConditionRowViewModel row)
    {
        var operatorKey = row.SelectedOperator?.Key;
        if (string.IsNullOrWhiteSpace(operatorKey))
            return null;

        if (row.SelectedField == "Description")
        {
            var text = (row.TextValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;

            IRuleCondition cond = operatorKey switch
            {
                "contains" => new ContainsCondition(text),
                "not_contains" => new NotCondition(new ContainsCondition(text)),
                "starts_with" => new StartsWithCondition(text),
                "ends_with" => new EndsWithCondition(text),
                _ => new ContainsCondition(text)
            };

            return cond;
        }

        // For non-description fields, fall back to a Contains condition
        // with a descriptive text (since the domain model is text-based in v1)
        if (row.SelectedField == "Amount" || row.SelectedField == "Date" || row.SelectedField == "Category")
        {
            var value = row.RenderValueForPreview();
            if (string.IsNullOrWhiteSpace(value)) return null;
            return new ContainsCondition($"{row.SelectedField}:{operatorKey}:{value}");
        }

        return null;
    }

    /// <summary>
    /// Converts a domain condition tree back into display text for the IF column.
    /// </summary>
    private static string BuildIfTextFromCondition(IRuleCondition condition)
    {
        return condition switch
        {
            ContainsCondition c => $"Description Contains '{c.Fragment}'",
            StartsWithCondition c => $"Description Starts with '{c.Prefix}'",
            EndsWithCondition c => $"Description Ends with '{c.Suffix}'",
            NotCondition n => BuildNotText(n),
            AndCondition and => BuildCompositeText("AND", and),
            OrCondition or => BuildCompositeText("OR", or),
            _ => condition.ToString() ?? ""
        };
    }

    private static string BuildNotText(NotCondition not)
    {
        var field = not.GetType().GetField("_inner",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var inner = (IRuleCondition?)field?.GetValue(not);
        if (inner is null) return "NOT (unknown)";

        return inner switch
        {
            ContainsCondition c => $"Description Does not contain '{c.Fragment}'",
            _ => $"NOT ({BuildIfTextFromCondition(inner)})"
        };
    }

    private static string BuildCompositeText(string combinator, IRuleCondition composite)
    {
        var field = composite.GetType().GetField("_conditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var conditions = (IReadOnlyList<IRuleCondition>?)field?.GetValue(composite);
        if (conditions is null || conditions.Count == 0) return "";

        var parts = conditions.Select((c, i) =>
        {
            var text = BuildIfTextFromCondition(c);
            return i == 0 ? text : $"{combinator} {text}";
        });

        return string.Join(" ", parts);
    }

    public sealed record OperatorOption(string Key, string Label);
}
