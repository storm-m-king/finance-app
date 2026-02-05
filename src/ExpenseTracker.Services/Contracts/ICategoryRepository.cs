using ExpenseTracker.Domain.Category;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Repository contract for <see cref="Category"/> persistence and querying.
/// </summary>
/// <remarks>
/// <para>
/// Implementations provide CRUD operations for <see cref="Category"/> domain entities.
/// </para>
/// <para>
/// <b>General preconditions:</b> Methods requiring an identifier expect a non-empty <see cref="Guid"/>.
/// Methods requiring a <see cref="Category"/> expect a non-null instance that satisfies domain invariants.
/// </para>
/// <para>
/// <b>General postconditions:</b> Unless otherwise stated, repository methods do not mutate the supplied
/// <see cref="Category"/> instance.
/// </para>
/// </remarks>
public interface ICategoryRepository
{
    /// <summary>
    /// Retrieves a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// The matching <see cref="Category"/> if found; otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> <paramref name="id"/> is not <see cref="Guid.Empty"/>.</para>
    /// <para><b>Postconditions:</b> If an entity exists with the given <paramref name="id"/>, it is returned;
    /// otherwise the result is <see langword="null"/>.</para>
    /// </remarks>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all categories.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A read-only list of all persisted <see cref="Category"/> entities. If none exist, returns an empty list.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> None.</para>
    /// <para><b>Postconditions:</b> The returned list is non-null; it may be empty.</para>
    /// </remarks>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all categories matching the given <see cref="CategoryType"/>.
    /// </summary>
    /// <param name="type">The category type to filter by.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// A read-only list of categories that match <paramref name="type"/>. If none exist, returns an empty list.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="type"/> is <see cref="CategoryType.Default"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> <paramref name="type"/> is not <see cref="CategoryType.Default"/>.</para>
    /// <para><b>Postconditions:</b> The returned list is non-null and contains only categories whose
    /// <see cref="Category.Type"/> equals <paramref name="type"/>; it may be empty.</para>
    /// </remarks>
    Task<IReadOnlyList<Category>> GetByTypeAsync(CategoryType type, CancellationToken ct = default);

    /// <summary>
    /// Persists a new category.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="category"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="category"/> has an empty <see cref="Category.Id"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a category with the same <see cref="Category.Id"/> already exists (implementation-dependent).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b>
    /// <paramref name="category"/> is not <see langword="null"/> and <paramref name="category"/> satisfies domain invariants
    /// (e.g., non-empty Id, non-empty Name, non-default Type).
    /// </para>
    /// <para><b>Postconditions:</b>
    /// On success, the repository contains a persisted category with the same identity and state as
    /// <paramref name="category"/>.
    /// </para>
    /// </remarks>
    Task AddAsync(Category category, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="category">The category state to persist.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="category"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="category"/> has an empty <see cref="Category.Id"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no category exists with <paramref name="category"/>'s <see cref="Category.Id"/> (implementation-dependent).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b>
    /// <paramref name="category"/> is not <see langword="null"/> and identifies an existing persisted category.
    /// </para>
    /// <para><b>Postconditions:</b>
    /// On success, the persisted category with the same <see cref="Category.Id"/> reflects the state carried by
    /// <paramref name="category"/>.
    /// </para>
    /// </remarks>
    Task UpdateAsync(Category category, CancellationToken ct = default);

    /// <summary>
    /// Deletes a category by its identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no category exists with <paramref name="id"/> (implementation-dependent).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the category is system-defined and deletion is not permitted (implementation-dependent).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> <paramref name="id"/> is not <see cref="Guid.Empty"/>.</para>
    /// <para><b>Postconditions:</b> On success, no persisted category exists with the given <paramref name="id"/>.</para>
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Determines whether a category exists with the given identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// <see langword="true"/> if a category exists with <paramref name="id"/>; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para><b>Preconditions:</b> <paramref name="id"/> is not <see cref="Guid.Empty"/>.</para>
    /// <para><b>Postconditions:</b> Returns <see langword="true"/> iff a persisted category with the given
    /// <paramref name="id"/> exists at the time of the call.</para>
    /// </remarks>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
