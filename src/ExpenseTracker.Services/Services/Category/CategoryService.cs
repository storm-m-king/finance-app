using ExpenseTracker.Domain.Category;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Services;

/// <summary>
/// Default implementation of <see cref="ICategoryService"/>.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task CreateUserCategoryAsync(
        string name,
        string typeText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();
        var parsedType = ParseCategoryType(typeText);

        // Enforce duplicate (name + type) rule (case-insensitive)
        var existing = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        if (existing.Any(c => string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase)
                              && c.Type == parsedType))
            throw new InvalidOperationException($"A category named '{trimmedName}' with type '{parsedType}' already exists.");

        var category = new Category(
            id: Guid.NewGuid(),
            name: trimmedName,
            isSystemCategory: false,
            type: parsedType);

        await _repository.AddOrUpdateAsync(category, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all persisted categories.
    /// </summary>
    public Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken ct = default)
        => _repository.GetAllAsync(ct);

    /// <summary>
    /// Updates an existing user category by id.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="newName">New category name.</param>
    /// <param name="typeText">New category type text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when inputs are invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the category does not exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the category is system-defined, or when the rename violates uniqueness rules.
    /// </exception>
    public async Task UpdateUserCategoryAsync(Guid id, string newName, string typeText, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name cannot be null or empty.", nameof(newName));

        var trimmedName = newName.Trim();
        var parsedType = ParseCategoryType(typeText);

        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No category exists with id '{id}'.");

        if (existing.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be modified.");

        // Enforce duplicate (name + type) rule (exclude self)
        var all = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        if (all.Any(c =>
                c.Id != id &&
                string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase)
                && c.Type == parsedType))
        {
            throw new InvalidOperationException($"A category named '{trimmedName}' already exists.");
        }

        // Keep immutability: create a new instance with updated fields.
        // Rename() already enforces user-editable rule; we already checked IsSystemCategory.
        var renamed = (Category)existing.Rename(trimmedName);

        // Category.Type has a private setter; use domain method to update it.
        renamed.UpdateCategoryType(parsedType);

        await _repository.AddOrUpdateAsync(renamed, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an existing user category by id.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the category does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the category is system-defined.</exception>
    public async Task DeleteUserCategoryAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        // Optional pre-check for better service-level error messaging.
        var existing = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No category exists with id '{id}'.");

        if (existing.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deleted.");

        await _repository.DeleteAsync(id, ct).ConfigureAwait(false);
    }

    // =====================================================
    // Private Helpers
    // =====================================================

    private static CategoryType ParseCategoryType(string typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText))
            throw new ArgumentException("Category type cannot be null or empty.", nameof(typeText));

        var normalized = typeText.Trim();

        Enum.TryParse(normalized, ignoreCase: true, out CategoryType parsed);

        if (parsed == CategoryType.Default)
            throw new ArgumentException($"Invalid category type '{normalized}'.", nameof(typeText));

        return parsed;
    }
}