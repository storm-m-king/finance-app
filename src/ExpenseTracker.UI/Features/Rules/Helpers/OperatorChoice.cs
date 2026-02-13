namespace ExpenseTracker.UI.Features.Rules.Helpers;

public enum RuleField
{
    Description,
    Category,
    Amount,
    Date
}

public enum RuleOperator
{
    // Text
    Contains,
    StartsWith,
    EndsWith,
    Equals,

    // Numeric comparisons (AmountCents)
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,

    // Date comparisons (DateOnly)
    Before,
    After,
    On
}

public enum RuleValueKind
{
    Text,
    AmountCents,
    Date,
    Category
}

public sealed record OperatorChoice(
    RuleOperator Operator,
    bool IsNegated,
    string Label,
    RuleValueKind ValueKind
);
