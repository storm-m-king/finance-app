namespace ExpenseTracker.UI.Features.Rules.Helpers;

public static class RuleTextFormatter
{
    public static string Format(ConditionExpr expr) =>
        expr switch
        {
            PredicateExpr p => FormatPredicate(p),
            ConditionGroup g => string.Join(
                g.Logic == GroupLogic.And ? " AND " : " OR ",
                g.Items.Select(Format)
            ),
            _ => ""
        };

    private static string FormatPredicate(PredicateExpr p)
    {
        var label = OperatorCatalog
            .ForField(p.Field)
            .First(x => x.Operator == p.Operator && x.IsNegated == p.IsNegated)
            .Label;

        return $"{FieldLabel(p.Field)} {label} {FormatValue(p.Value)}";
    }

    private static string FieldLabel(RuleField f) => f switch
    {
        RuleField.Description => "Description",
        RuleField.Category    => "Category",
        RuleField.Amount      => "Amount",
        RuleField.Date        => "Date",
        _ => f.ToString()
    };

    private static string FormatValue(object v) =>
        v switch
        {
            string s => $"'{s}'",
            long cents => FormatMoney(cents),
            DateOnly d => d.ToString("MMM d, yyyy"),
            Guid g => g.ToString(),
            _ => v.ToString() ?? ""
        };

    private static string FormatMoney(long cents)
    {
        var abs = Math.Abs(cents);
        var dollars = abs / 100m;
        var sign = cents < 0 ? "-" : "";
        return $"{sign}${dollars:0.00}";
    }
}
