namespace ExpenseTracker.Domain.Category;

/// <summary>
/// Represents a transaction category used for classification and reporting
/// (e.g., Groceries, Rent, Transfer).
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Category"/> is an immutable domain entity identified by a stable <see cref="Id"/>.
/// Categories may be either <em>system-defined</em> or <em>user-defined</em>.
/// </para>
/// <para>
/// System categories are owned by the application and cannot be modified by the user.
/// User categories may be renamed, subject to domain validation rules.
/// </para>
/// </remarks>
public sealed class Category
{
    /// <summary>
    /// Gets the unique identifier for this category.
    /// </summary>
    /// <remarks>
    /// This identifier is stable for the lifetime of the category and is used
    /// for persistence, rule targeting, and transaction classification.
    /// </remarks>
    public Guid Id { get; }

    /// <summary>
    /// Gets the human-friendly name of the category.
    /// </summary>
    /// <remarks>
    /// The name is intended for display in the user interface and reporting outputs.
    /// This value is guaranteed to be non-null, non-empty, and trimmed of
    /// leading and trailing whitespace.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this category is system-defined.
    /// </summary>
    /// <remarks>
    /// <para>
    /// System categories are created and managed by the application and are typically
    /// required for core workflows (e.g., Transfers, Uncategorized).
    /// </para>
    /// <para>
    /// System categories are not user-editable.
    /// </para>
    /// </remarks>
    public bool IsSystemCategory { get; }

    /// <summary>
    /// Gets a value indicating whether this category may be modified by the user.
    /// </summary>
    /// <remarks>
    /// This is a convenience property derived from <see cref="IsSystemCategory"/>.
    /// User-editable categories are those that are not system-defined.
    /// </remarks>
    public bool IsUserEditable => !IsSystemCategory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Category"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the category.</param>
    /// <param name="name">The display name of the category.</param>
    /// <param name="isSystemCategory">
    /// A value indicating whether the category is system-defined.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/> or when
    /// <paramref name="name"/> is null, empty, or consists only of whitespace.
    /// </exception>
    public Category(Guid id, string name, bool isSystemCategory)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        Id = id;
        Name = name.Trim();
        IsSystemCategory = isSystemCategory;
    }

    /// <summary>
    /// Returns a new <see cref="Category"/> instance with an updated name.
    /// </summary>
    /// <param name="newName">The new display name for the category.</param>
    /// <returns>
    /// A new <see cref="Category"/> instance with the same <see cref="Id"/> and
    /// updated <see cref="Name"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this category is system-defined and renaming is not permitted.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newName"/> is null, empty, or consists only of whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Categories are immutable. Renaming does not mutate the existing instance;
    /// instead, a new instance is returned.
    /// </para>
    /// <para>
    /// System categories cannot be renamed. Attempting to do so results in an exception.
    /// </para>
    /// </remarks>
    public Category Rename(string newName)
    {
        if (!IsUserEditable)
            throw new InvalidOperationException("System categories cannot be renamed.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be null or empty.", nameof(newName));

        return new Category(Id, newName.Trim(), isSystemCategory: false);
    }
}