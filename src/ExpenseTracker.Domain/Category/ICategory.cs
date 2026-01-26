namespace ExpenseTracker.Domain.Category;

/// <summary>
/// Represents a transaction category used for classification and reporting
/// (e.g., Groceries, Rent, Transfer).
/// </summary>
/// <remarks>
/// <para>
/// A category provides a stable identity and a human-friendly name that may be
/// assigned to transactions directly or via categorization rules.
/// </para>
/// <para>
/// Categories may be either system-defined or user-defined. System categories
/// are owned by the application and are not user-editable.
/// </para>
/// </remarks>
public interface ICategory
{
    /// <summary>
    /// Gets the unique identifier for this category.
    /// </summary>
    /// <remarks>
    /// This identifier is stable for the lifetime of the category and is used
    /// for persistence, rule targeting, and transaction classification.
    /// </remarks>
    Guid Id { get; }

    /// <summary>
    /// Gets the human-friendly display name of the category.
    /// </summary>
    /// <remarks>
    /// The name is intended for UI presentation and reporting outputs.
    /// Implementations should ensure this value is never null, empty,
    /// or composed solely of whitespace.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this category is system-defined.
    /// </summary>
    /// <remarks>
    /// System categories are created and managed by the application and typically
    /// represent core concepts (e.g., Transfers, Uncategorized).
    /// </remarks>
    bool IsSystemCategory { get; }

    /// <summary>
    /// Gets a value indicating whether this category may be modified by the user.
    /// </summary>
    /// <remarks>
    /// This is a convenience property derived from <see cref="IsSystemCategory"/>.
    /// User-editable categories are those that are not system-defined.
    /// </remarks>
    bool IsUserEditable { get; }

    /// <summary>
    /// Returns a renamed version of this category.
    /// </summary>
    /// <param name="newName">The new display name for the category.</param>
    /// <returns>
    /// A new category instance with the same <see cref="Id"/> and an updated
    /// <see cref="Name"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the category is system-defined and renaming is not permitted.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newName"/> is null, empty, or consists only of whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Categories are expected to be immutable. This operation should not mutate
    /// the existing instance but instead return a new instance.
    /// </para>
    /// <para>
    /// Implementations must enforce domain invariants when renaming.
    /// </para>
    /// </remarks>
    ICategory Rename(string newName);
}
