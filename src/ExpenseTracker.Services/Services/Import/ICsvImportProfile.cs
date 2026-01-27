using ExpenseTracker.Services.DTOs;
namespace ExpenseTracker.Services.Services.Import;

/// <summary>
/// Provides CSV import parsing behavior for a configured import profile.
/// </summary>
public interface ICsvImportProfile
{
    /// <summary>Unique key for selecting this profile (e.g., "amex.v1").</summary>
    string ProfileKey { get; }

    /// <summary>Display name used for messages and error context.</summary>
    string ProfileName { get; }

    /// <summary>Expected header columns in exact order.</summary>
    IReadOnlyList<string> ExpectedHeader { get; }

    /// <summary>Header name for date column.</summary>
    string DateHeader { get; }

    /// <summary>Header name for description column.</summary>
    string DescriptionHeader { get; }

    /// <summary>Header name for amount column.</summary>
    string AmountHeader { get; }

    /// <summary>
    /// Parses a CSV file and returns preview rows using this configured profile.
    /// </summary>
    Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(
        Guid accountId,
        string csvPath,
        CancellationToken ct = default);
}