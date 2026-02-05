using ExpenseTracker.Domain.Category;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Application service responsible for orchestrating category-related use cases.
/// </summary>
/// <remarks>
/// <para>
/// This service acts as the boundary between UI/API layers and the domain layer for category operations.
/// </para>
/// <para>
/// Responsibilities include:
/// </para>
/// <list type="bullet">
/// <item><description>Creating user-defined categories.</description></item>
/// <item><description>Retrieving categories for presentation.</description></item>
/// <item><description>Updating user-defined categories.</description></item>
/// <item><description>Deleting user-defined categories.</description></item>
/// </list>
/// <para>
/// System-defined categories are protected and cannot be modified or deleted.
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
    /// <item><description>The category type is invalid or Default.</description></item>
    /// <item><description>A category already exists with the same name.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b></para>
    /// <list type="bullet">
    /// <item><description>Valid category name and category type text are provided.</description></item>
    /// </list>
    ///
    /// <para><b>Postconditions:</b></para>
    /// <list type="bullet">
    /// <item><description>A new user-editable category is persisted.</description></item>
    /// </list>
    /// </remarks>
    Task CreateUserCategoryAsync(string name, string typeText, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all persisted categories.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A read-only collection of all categories. Returns an empty collection if no categories exist.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> None.</para>
    /// <para><b>Postconditions:</b> A non-null collection is returned.</para>
    /// </remarks>
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user-defined category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="newName">The new category display name.</param>
    /// <param name="typeText">The new category type expressed as user input.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description><paramref name="id"/> is empty.</description></item>
    /// <item><description><paramref name="newName"/> is null, empty, or whitespace.</description></item>
    /// <item><description><paramref name="typeText"/> is invalid.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the category does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description>The category is system-defined.</description></item>
    /// <item><description>The rename violates uniqueness rules.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> Category exists and is user-editable.</para>
    /// <para><b>Postconditions:</b> The category is updated with the provided values.</para>
    /// </remarks>
    Task UpdateUserCategoryAsync(Guid id, string newName, string typeText, CancellationToken ct = default);

    /// <summary>
    /// Deletes an existing user-defined category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the category does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the category is system-defined.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> Category exists and is user-editable.</para>
    /// <para><b>Postconditions:</b> The category is removed from persistence.</para>
    /// </remarks>
    Task DeleteUserCategoryAsync(Guid id, CancellationToken ct = default);
}