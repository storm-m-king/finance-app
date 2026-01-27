namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// In-memory registry of CSV import profiles keyed by <see cref="ICsvImportProfile.ProfileKey"/>.
/// </summary>
public sealed class ImportProfileRegistry : IImportProfileRegistry
{
    private Dictionary<string, ICsvImportProfile> _profiles;
    private bool _isInitialized;

    /// <summary>
    /// Creates a registry from a set of profiles.
    /// </summary>
    public ImportProfileRegistry()
    {
        _profiles = new Dictionary<string, ICsvImportProfile>();
    }

    /// <summary>
    /// Initializes the registry with all the profiles.
    /// </summary>
    public void InitializeRegistry(IEnumerable<ICsvImportProfile> profiles)
    {
        if (_isInitialized)
            return;
        
        if (profiles is null) throw new ArgumentNullException(nameof(profiles));

        _profiles = new Dictionary<string, ICsvImportProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            _profiles[profile.ProfileKey] = profile;
        }

        _isInitialized = true;
    }
    
    //TODO: Add and 'AddProfile' method and store profiles in the database with a CsvImportProfileRepository.

    /// <inheritdoc />
    public ICsvImportProfile Get(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
            throw new ArgumentException("Profile key cannot be null/empty.", nameof(profileKey));

        if (!_profiles.TryGetValue(profileKey.Trim(), out var profile))
            throw new NotSupportedException($"No CSV import profile registered for key '{profileKey}'.");

        return profile;
    }
}