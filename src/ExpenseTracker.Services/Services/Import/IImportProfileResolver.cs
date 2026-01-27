namespace ExpenseTracker.Services.Services.Import;

/// <summary>
/// Resolves the correct CSV import profile for an account.
/// </summary>
public interface IImportProfileResolver
{
    /// <summary>
    /// Resolves a CSV import profile for the given account.
    /// </summary>
    ICsvImportProfile Resolve(string profileKey);
}