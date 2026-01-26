using ExpenseTracker.Domain.Account;
using ExpenseTracker.Domain.Transaction;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Repository contract for persisting and querying accounts.
/// Implementations may use SQLite or other storage, but the contract stays storage-agnostic.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Gets an account by its identifier.</summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all accounts.</summary>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all non-archived accounts.</summary>
    Task<IReadOnlyList<Account>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Checks whether an account exists for the given identifier.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates/updates an account.</summary>
    Task AddOrUpdateAsync(Account account, CancellationToken ct = default);

    /// <summary>Deletes an account by identifier (hard delete).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
