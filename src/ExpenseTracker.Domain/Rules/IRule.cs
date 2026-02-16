namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Represents a categorization rule that may assign a category
/// when evaluated against a transaction.
/// </summary>
/// <remarks>
/// <para>
/// A rule combines a matching condition with a target category and an evaluation
/// priority. Rules are typically evaluated by an application service in priority
/// order to determine which category should be applied.
/// </para>
/// <para>
/// Implementations are expected to be immutable. Any change in rule behavior
/// (e.g., enabling or disabling) should result in a new instance rather than
/// mutating the existing one.
/// </para>
/// </remarks>
public interface IRule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the user-defined display name for this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the condition that determines whether this rule applies.
    /// </summary>
    /// <remarks>
    /// The condition encapsulates the matching semantics and may be a simple
    /// predicate or a composite of multiple predicates (e.g., AND / OR / NOT).
    /// </remarks>
    IRuleCondition Condition { get; }

    /// <summary>
    /// Gets the identifier of the category that should be assigned
    /// when this rule matches.
    /// </summary>
    Guid CategoryId { get; }

    /// <summary>
    /// Gets the evaluation priority of this rule.
    /// </summary>
    /// <remarks>
    /// Lower values are evaluated first. The precise evaluation policy
    /// (e.g., first match wins vs. best match wins) is defined by the
    /// application service responsible for rule execution.
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this rule is enabled.
    /// </summary>
    /// <remarks>
    /// Disabled rules are ignored during evaluation and do not participate
    /// in categorization workflows.
    /// </remarks>
    bool Enabled { get; }

    /// <summary>
    /// Determines whether this rule matches the provided candidate text.
    /// </summary>
    /// <param name="candidateText">
    /// The text to evaluate (e.g., a transaction description).
    /// </param>
    /// <returns>
    /// <c>true</c> if the rule is enabled and its <see cref="Condition"/>
    /// matches the candidate text; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Implementations should treat matching as case-insensitive unless
    /// explicitly documented otherwise.
    /// </remarks>
    bool Matches(string? candidateText);

    /// <summary>
    /// Returns an enabled version of this rule.
    /// </summary>
    /// <returns>
    /// The same instance if already enabled; otherwise, a new rule instance
    /// with <see cref="Enabled"/> set to <c>true</c>.
    /// </returns>
    IRule Enable();

    /// <summary>
    /// Returns a disabled version of this rule.
    /// </summary>
    /// <returns>
    /// The same instance if already disabled; otherwise, a new rule instance
    /// with <see cref="Enabled"/> set to <c>false</c>.
    /// </returns>
    IRule Disable();
}
