namespace ExpenseTracker.UI.Features.Rules.Helpers;

public static class OperatorCatalog
{
    public static IReadOnlyList<OperatorChoice> ForField(RuleField field) => field switch
    {
        RuleField.Description => TextOps(),
        RuleField.Category    => CategoryOps(),
        RuleField.Amount      => AmountOps(),
        RuleField.Date        => DateOps(),
        _ => Array.Empty<OperatorChoice>()
    };

    private static IReadOnlyList<OperatorChoice> TextOps() => new[]
    {
        Choice(RuleOperator.Contains,   "Contains",            "Does not contain",          RuleValueKind.Text),
        Choice(RuleOperator.StartsWith, "Starts with",         "Does not start with",       RuleValueKind.Text),
        Choice(RuleOperator.EndsWith,   "Ends with",           "Does not end with",         RuleValueKind.Text),
        Choice(RuleOperator.Equals,     "Equals",              "Does not equal",            RuleValueKind.Text),
    }.SelectMany(x => x).ToList();

    private static IReadOnlyList<OperatorChoice> CategoryOps() => new[]
    {
        Choice(RuleOperator.Equals, "Is", "Is not", RuleValueKind.Category),
    }.SelectMany(x => x).ToList();

    private static IReadOnlyList<OperatorChoice> AmountOps() => new[]
    {
        Choice(RuleOperator.GreaterThan,        "Is greater than", "Is not greater than", RuleValueKind.AmountCents),
        Choice(RuleOperator.GreaterThanOrEqual, "Is at least",     "Is not at least",     RuleValueKind.AmountCents),
        Choice(RuleOperator.LessThan,           "Is less than",    "Is not less than",    RuleValueKind.AmountCents),
        Choice(RuleOperator.LessThanOrEqual,    "Is at most",      "Is not at most",      RuleValueKind.AmountCents),
        Choice(RuleOperator.Equals,             "Is exactly",      "Is not exactly",      RuleValueKind.AmountCents),
    }.SelectMany(x => x).ToList();

    private static IReadOnlyList<OperatorChoice> DateOps() => new[]
    {
        Choice(RuleOperator.Before, "Is before", "Is not before", RuleValueKind.Date),
        Choice(RuleOperator.After,  "Is after",  "Is not after",  RuleValueKind.Date),
        Choice(RuleOperator.On,     "Is on",     "Is not on",     RuleValueKind.Date),
    }.SelectMany(x => x).ToList();

    private static IEnumerable<OperatorChoice> Choice(
        RuleOperator op,
        string positiveLabel,
        string negativeLabel,
        RuleValueKind kind)
    {
        yield return new OperatorChoice(op, false, positiveLabel, kind);
        yield return new OperatorChoice(op, true,  negativeLabel, kind);
    }
}
