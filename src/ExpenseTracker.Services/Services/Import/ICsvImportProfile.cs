using ExpenseTracker.Services.DTOs;
namespace ExpenseTracker.Services.Services.Import;

/// <summary>
/// Describes a CSV import profile for a specific institution/export format.
/// </summary>
public interface ICsvImportProfile
{
    /// <summary>
    /// Unique key used to select this profile for an account.
    /// Examples: "amex.v1", "sofi.v1".
    /// </summary>
    string ProfileKey { get; }

    /// <summary>
    /// Produces a preview of mapped transactions from the CSV file.
    /// </summary>
    IReadOnlyList<TransactionPreviewRow> Preview(Guid accountId, string csvPath, CancellationToken ct = default);
}