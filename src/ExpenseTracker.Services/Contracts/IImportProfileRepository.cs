using ExpenseTracker.Domain.ImportProfile;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Repository abstraction for accessing persisted import profile configurations.
/// </summary>
public interface IImportProfileRepository
{
    /// <summary>
    /// Retrieves an import profile by its unique key.
    /// </summary>
    /// <param name="profileKey">The profile key (e.g., "amex.v1").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="ImportProfile"/> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<IImportProfile?> GetByKeyAsync(string profileKey, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all import profiles.
    /// </summary>
    Task<IReadOnlyList<IImportProfile>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Determines whether an import profile exists.
    /// </summary>
    Task<bool> ExistsAsync(string profileKey, CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates an import profile.
    /// </summary>
    Task AddOrUpdateAsync(IImportProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes an import profile by key.
    /// </summary>
    Task DeleteAsync(string profileKey, CancellationToken ct = default);
}
