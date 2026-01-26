namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Represents a predicate that determines whether a rule applies
/// to a candidate transaction description.
/// </summary>
/// <remarks>
/// <para>
/// Implementations encapsulate matching semantics such as string comparison,
/// regular expressions, or composite logic (AND / OR / NOT).
/// </para>
/// <para>
/// This abstraction enables rules to be composed without embedding
/// complex conditional logic directly inside <see cref="Rule"/> or services.
/// </para>
/// </remarks>
public interface IRuleCondition
{
    /// <summary>
    /// Determines whether the provided candidate text satisfies this condition.
    /// </summary>
    /// <param name="candidateText">
    /// The candidate string to evaluate (e.g., a transaction description).
    /// </param>
    /// <returns>
    /// <c>true</c> if the condition is satisfied; otherwise, <c>false</c>.
    /// </returns>
    bool IsMatch(string candidateText);
}