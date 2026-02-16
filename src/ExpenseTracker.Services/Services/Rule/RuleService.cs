using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Services;

/// <summary>
/// Default implementation of <see cref="IRuleService"/>.
/// </summary>
public sealed class RuleService : IRuleService
{
    private readonly IRuleRepository _repository;

    public RuleService(IRuleRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Rule> CreateRuleAsync(
        string name,
        IRuleCondition condition,
        Guid categoryId,
        int priority,
        bool enabled = true,
        CancellationToken ct = default)
    {
        if (condition is null)
            throw new ArgumentNullException(nameof(condition));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be >= 0.");

        var rule = new Rule(
            id: Guid.NewGuid(),
            name: name ?? "",
            condition: condition,
            categoryId: categoryId,
            priority: priority,
            enabled: enabled);

        await _repository.AddOrUpdateAsync(rule, ct).ConfigureAwait(false);
        return rule;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Rule>> GetAllRulesAsync(CancellationToken ct = default)
        => _repository.GetAllAsync(ct);

    /// <inheritdoc />
    public async Task<Rule> UpdateRuleAsync(
        Guid id,
        string name,
        IRuleCondition condition,
        Guid categoryId,
        int priority,
        bool enabled,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (condition is null)
            throw new ArgumentNullException(nameof(condition));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No rule exists with id '{id}'.");

        var updated = new Rule(id, name ?? "", condition, categoryId, priority, enabled);
        await _repository.AddOrUpdateAsync(updated, ct).ConfigureAwait(false);
        return updated;
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        await _repository.DeleteAsync(id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No rule exists with id '{id}'.");

        var toggled = enabled ? (Rule)existing.Enable() : (Rule)existing.Disable();
        await _repository.AddOrUpdateAsync(toggled, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        if (orderedIds is null)
            throw new ArgumentNullException(nameof(orderedIds));

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var ruleId = orderedIds[i];
            var existing = await _repository.GetByIdAsync(ruleId, ct).ConfigureAwait(false);
            if (existing is null)
                continue;

            if (existing.Priority == i)
                continue;

            var reordered = new Rule(existing.Id, existing.Name, existing.Condition, existing.CategoryId, i, existing.Enabled);
            await _repository.AddOrUpdateAsync(reordered, ct).ConfigureAwait(false);
        }
    }
}
