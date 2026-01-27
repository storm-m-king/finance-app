namespace ExpenseTracker.Domain.ImportProfile;

/// <summary>
/// Represents a CSV import profile configuration stored in persistence.
/// </summary>
/// <remarks>
/// <para>
/// Import profiles describe how provider-specific CSV exports should be interpreted,
/// including which headers are expected and how transaction descriptions should be
/// normalized for matching and deduplication.
/// </para>
/// <para>
/// This type is a configuration holder only; it does not perform import logic.
/// </para>
/// </remarks>
public sealed class ImportProfile : IImportProfile 
{
    /// <summary>Unique key for selecting this profile.</summary>
    /// <remarks>
    /// Versioned keys are recommended (e.g., "amex.v1") to allow schema evolution
    /// without breaking existing imports.
    /// </remarks>
    public string ProfileKey { get; }

    /// <summary>Display name used for user-facing messages and error context.</summary>
    public string ProfileName { get; }

    /// <summary>
    /// The expected header columns in exact order, persisted as a single CSV string.
    /// </summary>
    /// <remarks>
    /// Used to validate that an incoming CSV matches the profile exactly.
    /// </remarks>
    public string ExpectedHeaderCsv { get; }

    /// <summary>Header name for the posted date column.</summary>
    public string DateHeader { get; }

    /// <summary>Header name for the raw description column.</summary>
    public string DescriptionHeader { get; }

    /// <summary>Header name for the amount column.</summary>
    public string AmountHeader { get; }

    /// <summary>
    /// CSV list of header names used to build the normalized description.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The listed headers are read from the CSV row in order and concatenated
    /// to produce the normalized description.
    /// </para>
    /// <para>
    /// Example value:
    /// <c>"Description,Card Member,Account #"</c>
    /// </para>
    /// </remarks>
    public string NormalizedDescriptionCsv { get; }

    /// <summary>
    /// Delimiter used when concatenating normalized description fields.
    /// </summary>
    /// <remarks>
    /// Defaults to a comma (<c>,</c>) when not otherwise specified.
    /// </remarks>
    public string NormalizedDescriptionDelimiter { get; }

    /// <summary>
    /// Creates a new <see cref="ImportProfile"/>.
    /// </summary>
    /// <param name="profileKey">Unique key for selecting the profile.</param>
    /// <param name="profileName">Display name for messages and error context.</param>
    /// <param name="expectedHeaderCsv">Expected header columns in exact order as a CSV string.</param>
    /// <param name="dateHeader">Header name for the posted date column.</param>
    /// <param name="descriptionHeader">Header name for the raw description column.</param>
    /// <param name="amountHeader">Header name for the amount column.</param>
    /// <param name="normalizedDescriptionCsv">
    /// CSV list of headers used to construct the normalized description.
    /// </param>
    /// <param name="normalizedDescriptionDelimiter">
    /// Delimiter used when joining normalized description fields.
    /// </param>
    public ImportProfile(
        string profileKey,
        string profileName,
        string expectedHeaderCsv,
        string dateHeader,
        string descriptionHeader,
        string amountHeader,
        string normalizedDescriptionCsv,
        string? normalizedDescriptionDelimiter = null)
    {
        ProfileKey = Require(profileKey, nameof(profileKey));
        ProfileName = Require(profileName, nameof(profileName));
        ExpectedHeaderCsv = Require(expectedHeaderCsv, nameof(expectedHeaderCsv));
        DateHeader = Require(dateHeader, nameof(dateHeader));
        DescriptionHeader = Require(descriptionHeader, nameof(descriptionHeader));
        AmountHeader = Require(amountHeader, nameof(amountHeader));

        NormalizedDescriptionCsv = Require(
            normalizedDescriptionCsv,
            nameof(normalizedDescriptionCsv));

        NormalizedDescriptionDelimiter =
            string.IsNullOrWhiteSpace(normalizedDescriptionDelimiter)
                ? ","
                : normalizedDescriptionDelimiter.Trim();
    }

    /// <summary>
    /// Splits <see cref="ExpectedHeaderCsv"/> into individual header tokens.
    /// </summary>
    /// <returns>The expected header columns in exact order.</returns>
    public string[] GetExpectedHeader()
    {
        return SplitCsv(ExpectedHeaderCsv);
    }

    /// <summary>
    /// Splits <see cref="NormalizedDescriptionCsv"/> into header names used
    /// to build the normalized description.
    /// </summary>
    /// <returns>
    /// Header names in the order they should be concatenated.
    /// </returns>
    public string[] GetNormalizedDescriptionHeaders()
    {
        return SplitCsv(NormalizedDescriptionCsv);
    }

    private static string[] SplitCsv(string csv)
    {
        return csv.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string Require(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);

        return value.Trim();
    }
}

