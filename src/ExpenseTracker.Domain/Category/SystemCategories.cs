namespace ExpenseTracker.Domain.Category;

/// <summary>
/// Defines the set of system-owned categories required for core application workflows.
/// </summary>
/// <remarks>
/// <para>
/// System categories are created and managed by the application and represent
/// fundamental financial concepts that must always be available.
/// </para>
/// <para>
/// Unlike user-defined categories, system categories are not editable or removable
/// by the user and may have special behavior within the system.
/// </para>
/// </remarks>
public enum SystemCategories
{
    /// <summary>
    /// A fallback category applied when a transaction cannot be classified
    /// by user rules or manual assignment.
    /// </summary>
    /// <remarks>
    /// This category ensures that all transactions are always associated with
    /// a category, preserving data integrity and simplifying reporting logic.
    /// </remarks>
    Uncategorized,

    /// <summary>
    /// A category representing transfers between accounts owned by the user.
    /// </summary>
    /// <remarks>
    /// Transfer transactions typically do not represent income or expenses and
    /// are often excluded from budgeting and spending reports.
    /// </remarks>
    Transfer,
}