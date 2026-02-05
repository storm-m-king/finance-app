using ExpenseTracker.Domain.Category;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Application service responsible for creating user-defined categories from user-provided inputs.
/// </summary>
/// <remarks>
/// <para>
/// This service validates user input, converts category type text into a valid <see cref="CategoryType"/>,
/// constructs a new <see cref="Category"/> domain object, and persists it using
/// <see cref="ICategoryRepository"/>.
/// </para>
/// <para>
/// This service represents a use-case orchestration boundary between UI/API layers and the domain layer.
/// </para>
/// </remarks>
public interface ICategoryService
{
    /// <summary>
    /// Creates and persists a new user-defined category.
    /// </summary>
    /// <param name="name">
    /// The display name of the category. Must be non-null, non-empty, and not whitespace.
    /// Leading and trailing whitespace will be trimmed.
    /// </param>
    /// <param name="typeText">
    /// The category type expressed as user input (e.g., "Income", "Expense", "Transfer").
    /// Parsing is case-insensitive.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description><paramref name="name"/> is null, empty, or whitespace.</description></item>
    /// <item><description><paramref name="typeText"/> is null, empty, whitespace, or invalid.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description>The parsed category type is <see cref="CategoryType.Default"/>.</description></item>
    /// <item><description>A category already exists with the same name.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b></para>
    /// <list type="bullet">
    /// <item><description><paramref name="name"/> is valid user input.</description></item>
    /// <item><description><paramref name="typeText"/> represents a valid non-default category type.</description></item>
    /// </list>
    ///
    /// <para><b>Postconditions:</b></para>
    /// <list type="bullet">
    /// <item><description>A new category is persisted.</description></item>
    /// <item><description>The created category is user-editable.</description></item>
    /// </list>
    /// </remarks>
    Task CreateUserCategoryAsync(string name, string typeText, CancellationToken ct = default);
}