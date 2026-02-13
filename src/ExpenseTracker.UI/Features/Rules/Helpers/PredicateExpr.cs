namespace ExpenseTracker.UI.Features.Rules.Helpers;

public abstract record ConditionExpr;

public enum GroupLogic { And, Or }

public sealed record ConditionGroup(GroupLogic Logic, IReadOnlyList<ConditionExpr> Items) : ConditionExpr;

public sealed record PredicateExpr(
    RuleField Field,
    RuleOperator Operator,
    bool IsNegated,
    object Value
) : ConditionExpr;
