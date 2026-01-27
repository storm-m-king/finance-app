namespace ExpenseTracker.Domain.ImportProfile;

/// <summary>
/// Represents a persisted CSV import profile configuration.
/// </summary>
/// <remarks>
/// <para>
/// Import profiles describe how provider-specific CSV exports should be interpreted,
/// including which headers are expected and how transaction descriptions should be
/// normalized for matching and deduplication.
/// </para>
/// <para>
/// This interface is intentionally configuration-focused and free of persistence or
/// import execution concerns.
/// </para>
/// </remarks>
public interface IImportProfile
{
    /// <summary>
    /// Gets the unique key used to select this profile.
    /// </summary>
    /// <remarks>
    /// Versioned keys are recommended (e.g., <c>"amex.v1"</c>) to allow schema evolution
    /// without breaking historical imports.
    /// </remarks>
    string ProfileKey { get; }

    /// <summary>
    /// Gets the human-friendly display name for the profile.
    /// </summary>
    /// <remarks>
    /// Used for user-facing messages and to provide context in error reporting.
    /// </remarks>
    string ProfileName { get; }

    /// <summary>
    /// Gets the expected header columns in exact order, persisted as a single CSV string.
    /// </summary>
    /// <remarks>
    /// This value is used to validate that an incoming CSV matches the profile exactly.
    /// Example: <c>"Date,Description,Card Member,Account #,Amount"</c>.
    /// </remarks>
    string ExpectedHeaderCsv { get; }

    /// <summary>
    /// Gets the header name for the posted date column.
    /// </summary>
    string DateHeader { get; }

    /// <summary>
    /// Gets the header name for the raw description column.
    /// </summary>
    string DescriptionHeader { get; }

    /// <summary>
    /// Gets the header name for the amount column.
    /// </summary>
    string AmountHeader { get; }

    /// <summary>
    /// Gets the CSV list of header names used to build the normalized description.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The listed headers are read from the CSV row in order and concatenated
    /// to produce the normalized description used for matching and deduplication.
    /// </para>
    /// <para>
    /// Example: <c>"Description,Card Member,Account #"</c>.
    /// </para>
    /// </remarks>
    string NormalizedDescriptionCsv { get; }

    /// <summary>
    /// Gets the delimiter used when concatenating normalized description fields.
    /// </summary>
    /// <remarks>
    /// Common values include <c>","</c> and <c>"|"</c>. If not explicitly set by the profile,
    /// implementations should default this value to a comma (<c>,</c>).
    /// </remarks>
    string NormalizedDescriptionDelimiter { get; }

    /// <summary>
    /// Splits <see cref="ExpectedHeaderCsv"/> into individual header tokens.
    /// </summary>
    /// <returns>The expected header columns in exact order.</returns>
    /// <remarks>
    /// Implementations should treat commas as separators and trim whitespace around tokens.
    /// </remarks>
    string[] GetExpectedHeader();

    /// <summary>
    /// Splits <see cref="NormalizedDescriptionCsv"/> into the ordered header names used to
    /// construct the normalized description.
    /// </summary>
    /// <returns>Header names in the order they should be concatenated.</returns>
    /// <remarks>
    /// Implementations should treat commas as separators and trim whitespace around tokens.
    /// </remarks>
    string[] GetNormalizedDescriptionHeaders();
}
