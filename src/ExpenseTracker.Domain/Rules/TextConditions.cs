namespace ExpenseTracker.Domain.Rules;

/// <summary>
/// Matches when the candidate text starts with a specified prefix.
/// </summary>
public sealed class StartsWithCondition : IRuleCondition
{
    /// <summary>The prefix to match.</summary>
    public string Prefix { get; }

    /// <summary>
    /// Initializes a new <see cref="StartsWithCondition"/>.
    /// </summary>
    public StartsWithCondition(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

        Prefix = prefix.Trim();
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => candidateText.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Matches when the candidate text ends with a specified suffix.
/// </summary>
public sealed class EndsWithCondition : IRuleCondition
{
    /// <summary>The suffix to match.</summary>
    public string Suffix { get; }

    /// <summary>
    /// Initializes a new <see cref="EndsWithCondition"/>.
    /// </summary>
    public EndsWithCondition(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            throw new ArgumentException("Suffix cannot be null or empty.", nameof(suffix));

        Suffix = suffix.Trim();
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => candidateText.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Matches when the candidate text contains the specified fragment.
/// </summary>
public sealed class ContainsCondition : IRuleCondition
{
    /// <summary>The fragment to search for.</summary>
    public string Fragment { get; }

    /// <summary>
    /// Initializes a new <see cref="ContainsCondition"/>.
    /// </summary>
    public ContainsCondition(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            throw new ArgumentException("Fragment cannot be null or empty.", nameof(fragment));

        Fragment = fragment.Trim();
    }

    /// <inheritdoc />
    public bool IsMatch(string candidateText)
        => candidateText.Contains(Fragment, StringComparison.OrdinalIgnoreCase);
}
