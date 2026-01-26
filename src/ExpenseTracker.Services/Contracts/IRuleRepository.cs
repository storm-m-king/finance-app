using ExpenseTracker.Domain.Rules;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Repository contract for persisting and querying classification/automation rules.
/// </summary>
public interface IRuleRepository
{
    /// <summary>Gets a rule by its identifier.</summary>
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all rules.</summary>
    Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all enabled rules, typically applied in priority order.</summary>
    Task<IReadOnlyList<Rule>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>Creates/updates a rule.</summary>
    Task AddOrUpdateAsync(Rule rule, CancellationToken ct = default);

    /// <summary>Deletes a rule by identifier (hard delete).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}