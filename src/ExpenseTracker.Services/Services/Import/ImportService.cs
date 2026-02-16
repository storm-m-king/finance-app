using ExpenseTracker.Domain.Account;
using ExpenseTracker.Domain.ImportProfile;
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
    private readonly IImportProfileRepository _importProfileRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IRuleService _ruleService;
    private readonly ICategoryRepository _categoryRepository;

    /// <summary>
    /// Creates an import service.
    /// </summary>
    public ImportService(
        IAppLogger applogger, 
        IImportProfileResolver profileResolver, 
        IImportProfileRepository profileRepository, 
        IAccountRepository accountRepository,
        IRuleService ruleService,
        ICategoryRepository categoryRepository)
    {
        _logger = applogger ?? throw new ArgumentNullException(nameof(applogger));
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
        _importProfileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<IReadOnlyList<IImportProfile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        return await _importProfileRepository.GetAllAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(string importProfileKey, string csvPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(importProfileKey)) throw new ArgumentException("profileKey cannot be empty.", nameof(importProfileKey));
        if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("CSV path cannot be null/empty.", nameof(csvPath));
        Guid accountId = Guid.Empty;
        var result = await _logger.Trace("importservice.previewasync", async () =>
        {
            var profile = _profileResolver.Resolve(importProfileKey);
            Account? account = await _accountRepository.GetByImportProfileKeyAsync(importProfileKey);
            if(account == null) throw new ArgumentNullException(nameof(account));
            accountId = account.Id;
            _logger.Info($"[ImportService] Retrieving Preview for ImportProfile with AccountId: {accountId} and Path: {csvPath}");
            return await profile.PreviewAsync(accountId, csvPath, ct);
        });
        
        _logger.Info($"[ImportService] Retrieved Preview for ImportProfile with AccountId: {accountId}. " +
                     $"Rows returned: {result.Count}");

        // Apply rule engine to assign categories (first match wins by priority)
        var categorized = await ApplyRulesAsync(result, ct);
        return categorized;
    }

    private async Task<IReadOnlyList<TransactionPreviewRow>> ApplyRulesAsync(
        IReadOnlyList<TransactionPreviewRow> rows,
        CancellationToken ct)
    {
        // Build category lookup for resolving names
        var categories = await _categoryRepository.GetAllAsync(ct);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        var results = new List<TransactionPreviewRow>(rows.Count);
        var matchCount = 0;

        foreach (var row in rows)
        {
            var matchedCategoryId = await _ruleService.EvaluateAsync(row.RawDescription, ct);

            if (matchedCategoryId is not null)
            {
                var categoryName = categoryLookup.TryGetValue(matchedCategoryId.Value, out var name)
                    ? name
                    : "Uncategorized";

                results.Add(row with { CategoryId = matchedCategoryId, CategoryName = categoryName });
                matchCount++;
            }
            else
            {
                results.Add(row with { CategoryName = "Uncategorized" });
            }
        }

        _logger.Info($"[ImportService] Rule engine applied: {matchCount}/{rows.Count} transactions matched a rule.");
        return results;
    }
}