using ExpenseTracker.Domain.ImportProfile;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// In-memory registry of CSV import profiles keyed by <see cref="ICsvImportProfile.ProfileKey"/>.
/// </summary>
public sealed class ImportProfileRegistry : IImportProfileRegistry
{
    private Dictionary<string, ICsvImportProfile> _profiles;
    private bool _isInitialized;
    IFingerprintService _fingerprintService;

    /// <summary>
    /// Creates a registry from a set of profiles.
    /// </summary>
    public ImportProfileRegistry(IFingerprintService fingerprintService)
    {
        _profiles = new Dictionary<string, ICsvImportProfile>();
        _fingerprintService = fingerprintService;
    }

    /// <summary>
    /// Initializes the registry with all the profiles.
    /// </summary>
    public void InitializeRegistry(IReadOnlyList<IImportProfile> profiles)
    {
        if (_isInitialized)
            return;
        
        if (profiles is null) throw new ArgumentNullException(nameof(profiles));
        
        _profiles = new Dictionary<string, ICsvImportProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            ICsvImportProfile profileInstance = new CsvImportProfile(profile, _fingerprintService);
            _profiles[profileInstance.ProfileKey] = profileInstance;
        }

        _isInitialized = true;
    }
    
    //TODO: Add and 'AddProfile' method and store profiles in the database with a CsvImportProfileRepository.

    /// <inheritdoc />
    public ICsvImportProfile Get(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
            throw new ArgumentException("ImportProfile key cannot be null/empty.", nameof(profileKey));

        if (!_profiles.TryGetValue(profileKey.Trim(), out var profile))
            throw new NotSupportedException($"No CSV import profile registered for key '{profileKey}'.");

        return profile;
    }
}