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
        // -----------------------------
        // Validate Name
        // -----------------------------
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be null or empty.", nameof(name));

        var trimmedName = name.Trim();

        // -----------------------------
        // Parse Category Type
        // -----------------------------
        var parsedType = ParseCategoryType(typeText);

        // -----------------------------
        // Enforce Duplicate Name Rule
        // -----------------------------
        var existing = await _repository.GetAllAsync(ct).ConfigureAwait(false);

        if (existing.Any(c =>
            string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A category named '{trimmedName}' already exists.");
        }

        // -----------------------------
        // Create Domain Object
        // -----------------------------
        var category = new Category(
            Guid.NewGuid(),
            trimmedName,
            isSystemCategory: false,
            parsedType);

        // -----------------------------
        // Persist
        // -----------------------------
        await _repository.AddAsync(category, ct).ConfigureAwait(false);
    }

    // =====================================================
    // Private Helpers
    // =====================================================

    /// <summary>
    /// Converts user-provided text into a valid <see cref="CategoryType"/>.
    /// </summary>
    /// <param name="typeText">User input describing the category type.</param>
    /// <returns>A validated <see cref="CategoryType"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when input is null, empty, whitespace, or not a valid enum value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the parsed enum value is <see cref="CategoryType.Default"/>.
    /// </exception>
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
