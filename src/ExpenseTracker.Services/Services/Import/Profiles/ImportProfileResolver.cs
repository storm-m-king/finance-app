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
    public ICsvImportProfile Resolve(string profileKey)
    {
        if (string.IsNullOrEmpty(profileKey)) throw new ArgumentException("profileKey cannot be empty.", nameof(profileKey));
        
        return _registry.Get(profileKey);
    }
}