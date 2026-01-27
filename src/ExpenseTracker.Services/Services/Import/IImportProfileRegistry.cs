using ExpenseTracker.Domain.ImportProfile;

namespace ExpenseTracker.Services.Services.Import;

/// <summary>
/// Provides CSV import profiles by key.
/// </summary>
public interface IImportProfileRegistry
{
    /// <summary>
    /// Initializes the registry with all the profiles.
    /// </summary>
    void InitializeRegistry(IReadOnlyList<IImportProfile> profiles);
    
    /// <summary>
    /// Gets a CSV import profile for the specified key.
    /// </summary>
    ICsvImportProfile Get(string profileKey);
}