using ExpenseTracker.Domain.Rules;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Application service responsible for orchestrating rule-related use cases.
/// </summary>
/// <remarks>
/// <para>
/// This service acts as the boundary between UI/API layers and the domain layer for rule operations.
/// </para>
/// <para>
/// Responsibilities include:
/// </para>
/// <list type="bullet">
/// <item><description>Creating rules with composite conditions.</description></item>
/// <item><description>Retrieving rules for presentation.</description></item>
/// <item><description>Updating existing rules.</description></item>
/// <item><description>Deleting rules.</description></item>
/// <item><description>Enabling and disabling rules.</description></item>
/// </list>
/// </remarks>
public interface IRuleService
{
    /// <summary>
    /// Creates and persists a new rule.
    /// </summary>
    /// <param name="name">The user-defined display name for the rule.</param>
    /// <param name="condition">The matching condition for the rule.</param>
    /// <param name="categoryId">The target category to assign when the rule matches.</param>
    /// <param name="priority">The evaluation priority (lower is evaluated first).</param>
    /// <param name="enabled">Whether the rule is initially enabled.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted rule.</returns>
    Task<Rule> CreateRuleAsync(
        string name,
        IRuleCondition condition,
        Guid categoryId,
        int priority,
        bool enabled = true,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all persisted rules, ordered by priority ascending.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only collection of all rules.</returns>
    Task<IReadOnlyList<Rule>> GetAllRulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="name">The updated display name.</param>
    /// <param name="condition">The updated matching condition.</param>
    /// <param name="categoryId">The updated target category.</param>
    /// <param name="priority">The updated evaluation priority.</param>
    /// <param name="enabled">The updated enabled state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated rule.</returns>
    Task<Rule> UpdateRuleAsync(
        Guid id,
        string name,
        IRuleCondition condition,
        Guid categoryId,
        int priority,
        bool enabled,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an existing rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no rule exists with the specified id.
    /// </exception>
    Task DeleteRuleAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Enables or disables a rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="enabled">The desired enabled state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no rule exists with the specified id.
    /// </exception>
    Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Updates the priority of multiple rules in a single batch.
    /// Used after drag-and-drop reordering in the UI.
    /// </summary>
    /// <param name="orderedIds">Rule identifiers in the desired priority order (index = priority).</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);

    /// <summary>
    /// Evaluates all enabled rules against a candidate text (e.g., transaction description)
    /// in priority order. Returns the CategoryId of the first matching rule, or null if none match.
    /// </summary>
    /// <param name="candidateText">The text to evaluate (typically a transaction description).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The CategoryId of the first matching rule, or null if no rules match.</returns>
    Task<Guid?> EvaluateAsync(string candidateText, CancellationToken ct = default);
}
