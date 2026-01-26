using ExpenseTracker.Domain.Transaction;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Repository contract for persisting and querying transactions.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Gets a transaction by its identifier.</summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns transactions for an account within an optional date range (inclusive).
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetByAccountAsync
    (
        Guid accountId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Returns all transactions within an optional date range (inclusive).
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetAllAsync
    (
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Checks whether a transaction with the same fingerprint already exists.
    /// Used for deduplication across imports.
    /// </summary>
    Task<bool> ExistsByFingerprintAsync(string fingerprint, CancellationToken ct = default);

    /// <summary>Creates/updates atransaction.</summary>
    Task AddOrUpdateAsync(Transaction transaction, CancellationToken ct = default);

    /// <summary>Creates/updates many transactions efficiently (batch insert/update).</summary>
    Task AddOrUpdateRangeAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default);

    /// <summary>Deletes a transaction by identifier (hard delete).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}