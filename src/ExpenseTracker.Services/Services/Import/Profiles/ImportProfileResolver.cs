using ExpenseTracker.Services.Contracts;
namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// Resolves import profiles using account configuration stored in persistence.
/// </summary>
public sealed class ImportProfileResolver : IImportProfileResolver
{
    private readonly IAccountRepository _accounts;
    private readonly IImportProfileRegistry _registry;

    /// <summary>
    /// Creates a resolver.
    /// </summary>
    public ImportProfileResolver(IAccountRepository accounts, IImportProfileRegistry registry)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public async Task<ICsvImportProfile> ResolveAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));

        var account = await _accounts.GetByIdAsync(accountId, ct).ConfigureAwait(false)
                      ?? throw new InvalidOperationException($"Account not found: {accountId}");

        if (string.IsNullOrWhiteSpace(account.ImportProfileKey))
            throw new InvalidOperationException($"Account '{account.Name}' has no ImportProfileKey configured.");

        return _registry.Get(account.ImportProfileKey);
    }
}