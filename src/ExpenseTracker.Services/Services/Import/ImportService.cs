using ExpenseTracker.Services.Contracts;
using ExpenseTracker.Services.DTOs;
namespace ExpenseTracker.Services.Services.Import;

/// <summary>
/// Orchestrates CSV import steps.
/// </summary>
public sealed class ImportService : IImportService
{
    private readonly IAppLogger _logger;
    private readonly IImportProfileResolver _profileResolver;

    /// <summary>
    /// Creates an import service.
    /// </summary>
    public ImportService(IAppLogger applogger, IImportProfileResolver profileResolver)
    {
        _logger = applogger ?? throw new ArgumentNullException(nameof(applogger));
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(Guid accountId, string csvPath, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("CSV path cannot be null/empty.", nameof(csvPath));

        var result = await _logger.Trace("importservice.previewasync", async () =>
        {
            _logger.Info($"[ImportService] Retrieving Preview for Profile with AccountId: {accountId} and Path: {csvPath}");
            var profile = await _profileResolver.ResolveAsync(accountId, ct).ConfigureAwait(false);
            _logger.Info($"[ImportService] Found Account profile with key {profile.ProfileKey}!");
            return profile.Preview(accountId, csvPath, ct);
        });
        
        _logger.Info($"[ImportService] Retrieved Preview for Profile with AccountId: {accountId}. " +
                     $"Rows returned: {result.Count}");
        return result;
    }
}