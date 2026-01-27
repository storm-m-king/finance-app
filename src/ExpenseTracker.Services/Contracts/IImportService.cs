using ExpenseTracker.Services.DTOs;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Provides CSV import operations.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Loads and maps CSV rows into a normalized preview form.
    /// </summary>
    Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(Guid accountId, string csvPath, CancellationToken ct = default);
}