using ExpenseTracker.Domain.ImportProfile;
using ExpenseTracker.Services.DTOs;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Provides CSV import operations.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Get all profiles
    /// </summary>
    /// <returns>list of profiles</returns>
    Task<IReadOnlyList<IImportProfile>> GetAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Parses csv and returns transaction preview
    /// </summary>
    /// <param name="profileKey"></param>
    /// <param name="csvPath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(string profileKey, string csvPath,
        CancellationToken ct = default);
}