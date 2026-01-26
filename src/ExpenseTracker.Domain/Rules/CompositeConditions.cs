namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Matches when all inner conditions are satisfied.
/// </summary>
public sealed class AndCondition : IRuleCondition
{
    private readonly IReadOnlyList<IRuleCondition> _conditions;

    /// <summary>
    /// Initializes a new <see cref="AndCondition"/>.
    /// </summary>
    /// <param name="conditions">
    /// The conditions that must all evaluate to <c>true</c>.
    /// </param>
    public AndCondition(IEnumerable<IRuleCondition> conditions)
    {
        var list = conditions?.ToList()
            ?? throw new ArgumentNullException(nameof(conditions));

        if (list.Count < 2)
            throw new ArgumentException("AND requires at least two conditions.", nameof(conditions));

        _conditions = list;
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => _conditions.All(c => c.IsMatch(candidateText));
}

/// <summary>
/// Matches when any inner condition is satisfied.
/// </summary>
public sealed class OrCondition : IRuleCondition
{
    private readonly IReadOnlyList<IRuleCondition> _conditions;

    /// <summary>
    /// Initializes a new <see cref="OrCondition"/>.
    /// </summary>
    public OrCondition(IEnumerable<IRuleCondition> conditions)
    {
        var list = conditions?.ToList()
            ?? throw new ArgumentNullException(nameof(conditions));

        if (list.Count < 2)
            throw new ArgumentException("OR requires at least two conditions.", nameof(conditions));

        _conditions = list;
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => _conditions.Any(c => c.IsMatch(candidateText));
}

/// <summary>
/// Negates the result of an inner condition.
/// </summary>
public sealed class NotCondition : IRuleCondition
{
    private readonly IRuleCondition _inner;

    /// <summary>
    /// Initializes a new <see cref="NotCondition"/>.
    /// </summary>
    public NotCondition(IRuleCondition inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => !_inner.IsMatch(candidateText);
}
