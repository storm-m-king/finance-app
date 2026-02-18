using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Services.DTOs;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Service contract for transaction business operations.
/// Orchestrates persistence, import, and invariant enforcement.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Imports transactions from preview rows into the database.
    /// Handles fingerprint deduplication, category assignment, and status rules.
    /// Returns the number of transactions actually imported (after dedup).
    /// </summary>
    Task<int> ImportTransactionsAsync(
        IReadOnlyList<TransactionPreviewRow> previewRows,
        string sourceFileName,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all transactions within an optional date range (inclusive).
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the category of a transaction.
    /// If the category is Transfer, IsTransfer is set to true.
    /// </summary>
    Task UpdateCategoryAsync(Guid transactionId, Guid categoryId, CancellationToken ct = default);

    /// <summary>
    /// Updates the workflow status of a transaction.
    /// </summary>
    Task UpdateStatusAsync(Guid transactionId, TransactionStatus status, CancellationToken ct = default);

    /// <summary>
    /// Toggles the transfer flag on a transaction.
    /// ON → Category = Transfer.
    /// OFF → Category = Uncategorized, Status = NeedsReview.
    /// </summary>
    Task ToggleTransferAsync(Guid transactionId, bool isTransfer, CancellationToken ct = default);

    /// <summary>
    /// Updates optional notes on a transaction.
    /// </summary>
    Task UpdateNotesAsync(Guid transactionId, string? notes, CancellationToken ct = default);
}
