namespace ExpenseTracker.Domain;
using System.Text.RegularExpressions;
/// <summary>
/// Represents a categorization rule that can be applied to a transaction description.
/// A rule matches against a candidate text (e.g., a transaction's raw or normalized description)
/// using a configured <see cref="MatchType"/> and, when matched, indicates the target
/// <see cref="CategoryId"/> that should be assigned.
/// </summary>
/// <remarks>
/// <para>
/// Rules are typically evaluated in ascending <see cref="Priority"/> order (lowest number first),
/// and only rules with <see cref="Enabled"/> set to <c>true</c> should be considered.
/// </para>
/// <para>
/// This type is designed to be immutable: once created, its state cannot be changed. Updates are
/// expressed via intent-revealing methods (e.g., <see cref="Enable"/>, <see cref="Disable"/>),
/// which return a new <see cref="Rule"/> instance with the requested changes.
/// </para>
/// </remarks>
public sealed class Rule
{
    /// <summary>
    /// Gets the unique identifier for this rule.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets how <see cref="MatchText"/> should be applied to candidate text.
    /// </summary>
    public MatchType MatchType { get; }

    /// <summary>
    /// Gets the match pattern used to evaluate a candidate string (e.g., transaction description).
    /// </summary>
    /// <remarks>
    /// <para>
    /// For <see cref="Domain.MatchType.Contains"/>, <see cref="Domain.MatchType.StartsWith"/>,
    /// and <see cref="Domain.MatchType.Equals"/>, this is treated as a case-insensitive literal string.
    /// </para>
    /// <para>
    /// For <see cref="Domain.MatchType.Regex"/>, this is treated as a regular expression pattern
    /// and is validated at construction time.
    /// </para>
    /// </remarks>
    public string MatchText { get; }

    /// <summary>
    /// Gets the identifier of the category that should be assigned when this rule matches.
    /// </summary>
    public Guid CategoryId { get; }

    /// <summary>
    /// Gets the evaluation priority for this rule.
    /// </summary>
    /// <remarks>
    /// Lower values are typically evaluated first. The specific interpretation is a policy decision
    /// made by the application service that executes rule evaluation.
    /// </remarks>
    public int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this rule is active and should be considered during evaluation.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rule"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the rule.</param>
    /// <param name="matchType">The strategy used to evaluate <paramref name="matchText"/>.</param>
    /// <param name="matchText">The literal string or regex pattern used for matching.</param>
    /// <param name="categoryId">The target category identifier to assign when the rule matches.</param>
    /// <param name="priority">The rule's evaluation priority (must be &gt;= 0).</param>
    /// <param name="enabled">Whether the rule is enabled. Defaults to <c>true</c>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> or <paramref name="categoryId"/> is <see cref="Guid.Empty"/>,
    /// or when <paramref name="matchText"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="priority"/> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="matchType"/> is <see cref="Domain.MatchType.Regex"/> and
    /// <paramref name="matchText"/> is not a valid regular expression.
    /// </exception>
    public Rule(Guid id, MatchType matchType, string matchText, Guid categoryId, int priority, bool enabled = true)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(matchText))
            throw new ArgumentException("MatchText cannot be null, empty, or whitespace.", nameof(matchText));

        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be >= 0.");

        // Normalize once so comparisons are stable.
        matchText = matchText.Trim();

        // Fail fast: validate regex patterns at construction time.
        if (matchType == MatchType.Regex)
        {
            try
            {
                _ = new Regex(matchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("MatchText is not a valid regular expression.", nameof(matchText), ex);
            }
        }

        Id = id;
        MatchType = matchType;
        MatchText = matchText;
        CategoryId = categoryId;
        Priority = priority;
        Enabled = enabled;
    }

    /// <summary>
    /// Determines whether this rule matches the provided candidate string.
    /// </summary>
    /// <param name="candidateText">
    /// The text to evaluate (e.g., a transaction's raw or normalized description).
    /// </param>
    /// <returns>
    /// <c>true</c> if the rule is enabled and the candidate text matches according to
    /// <see cref="MatchType"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Matching is case-insensitive. If <paramref name="candidateText"/> is <c>null</c>, empty,
    /// or whitespace, this method returns <c>false</c>.
    /// </remarks>
    public bool Matches(string? candidateText)
    {
        if (!Enabled) return false;
        if (string.IsNullOrWhiteSpace(candidateText)) return false;

        var text = candidateText.Trim();

        return MatchType switch
        {
            MatchType.Contains   => text.Contains(MatchText, StringComparison.OrdinalIgnoreCase),
            MatchType.StartsWith => text.StartsWith(MatchText, StringComparison.OrdinalIgnoreCase),
            MatchType.Equals     => string.Equals(text, MatchText, StringComparison.OrdinalIgnoreCase),
            MatchType.Regex      => Regex.IsMatch(text, MatchText, RegexOptions.IgnoreCase),
            _                    => false
        };
    }

    /// <summary>
    /// Returns an enabled version of this rule.
    /// </summary>
    /// <returns>
    /// The same instance if already enabled; otherwise, a new <see cref="Rule"/> instance with
    /// <see cref="Enabled"/> set to <c>true</c>.
    /// </returns>
    public Rule Enable()
        => Enabled ? this : new Rule(Id, MatchType, MatchText, CategoryId, Priority, enabled: true);

    /// <summary>
    /// Returns a disabled version of this rule.
    /// </summary>
    /// <returns>
    /// The same instance if already disabled; otherwise, a new <see cref="Rule"/> instance with
    /// <see cref="Enabled"/> set to <c>false</c>.
    /// </returns>
    public Rule Disable()
        => !Enabled ? this : new Rule(Id, MatchType, MatchText, CategoryId, Priority, enabled: false);

    /// <summary>
    /// Returns a copy of this rule with the specified priority.
    /// </summary>
    /// <param name="newPriority">The new priority (must be &gt;= 0).</param>
    /// <returns>A new <see cref="Rule"/> instance with the updated priority.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="newPriority"/> is less than 0.
    /// </exception>
    public Rule WithPriority(int newPriority)
    {
        if (newPriority < 0)
            throw new ArgumentOutOfRangeException(nameof(newPriority), "Priority must be >= 0.");

        return new Rule(Id, MatchType, MatchText, CategoryId, newPriority, Enabled);
    }

    /// <summary>
    /// Returns a copy of this rule targeting a new category.
    /// </summary>
    /// <param name="newCategoryId">The new category id (must not be <see cref="Guid.Empty"/>).</param>
    /// <returns>A new <see cref="Rule"/> instance with the updated category id.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newCategoryId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    public Rule RetargetCategory(Guid newCategoryId)
    {
        if (newCategoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(newCategoryId));

        return new Rule(Id, MatchType, MatchText, newCategoryId, Priority, Enabled);
    }

    /// <summary>
    /// Returns a copy of this rule with updated match configuration.
    /// </summary>
    /// <param name="newMatchType">The new match type strategy.</param>
    /// <param name="newMatchText">The new literal match text or regex pattern.</param>
    /// <returns>A new <see cref="Rule"/> instance with updated match settings.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newMatchText"/> is <c>null</c>, empty, or whitespace, or when
    /// <paramref name="newMatchType"/> is <see cref="Domain.MatchType.Regex"/> and
    /// <paramref name="newMatchText"/> is not a valid regular expression.
    /// </exception>
    public Rule UpdateMatch(MatchType newMatchType, string newMatchText)
        => new Rule(Id, newMatchType, newMatchText, CategoryId, Priority, Enabled);
}