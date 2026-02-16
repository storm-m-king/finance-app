namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Represents a categorization rule that assigns a category
/// when its condition matches a transaction.
/// </summary>
/// <remarks>
/// <para>
/// A rule consists of:
/// <list type="bullet">
/// <item><description>A composable matching condition</description></item>
/// <item><description>A target category</description></item>
/// <item><description>A priority for evaluation ordering</description></item>
/// </list>
/// </para>
/// <para>
/// Rules are immutable. Any modification results in a new instance.
/// </para>
/// </remarks>
public sealed class Rule : IRule
{
    /// <summary>Unique identifier for this rule.</summary>
    public Guid Id { get; }

    /// <summary>User-defined display name for this rule.</summary>
    public string Name { get; }

    /// <summary>The condition that determines whether the rule applies.</summary>
    public IRuleCondition Condition { get; }

    /// <summary>The category assigned when the rule matches.</summary>
    public Guid CategoryId { get; }

    /// <summary>
    /// Rule evaluation priority. Lower values are evaluated first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Indicates whether the rule is enabled.
    /// Disabled rules are ignored during evaluation.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Initializes a new <see cref="Rule"/>.
    /// </summary>
    public Rule
    (
        Guid id,
        string name,
        IRuleCondition condition,
        Guid categoryId,
        int priority,
        bool enabled = true
    )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be >= 0.");

        Condition = condition ?? throw new ArgumentNullException(nameof(condition));

        Id = id;
        Name = name ?? "";
        CategoryId = categoryId;
        Priority = priority;
        Enabled = enabled;
    }

    /// <summary>
    /// Determines whether this rule applies to the provided candidate text.
    /// </summary>
    public bool Matches(string? candidateText)
    {
        if (!Enabled) return false;
        if (string.IsNullOrWhiteSpace(candidateText)) return false;

        return Condition.IsMatch(candidateText.Trim());
    }

    /// <summary>
    /// Returns an enabled version of this rule.
    /// </summary>
    public IRule Enable()
        => Enabled ? this : new Rule(Id, Name, Condition, CategoryId, Priority, enabled: true);

    /// <summary>
    /// Returns a disabled version of this rule.
    /// </summary>
    public IRule Disable()
        => !Enabled ? this : new Rule(Id, Name, Condition, CategoryId, Priority, enabled: false);
}