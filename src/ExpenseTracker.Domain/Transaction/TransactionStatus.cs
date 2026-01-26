namespace ExpenseTracker.Domain.Transaction;

/// <summary>
/// Represents the review state of a transaction within the system.
/// </summary>
/// <remarks>
/// <para>
/// Transaction status indicates whether a transaction requires user attention,
/// has been reviewed, or has been intentionally excluded from further workflows.
/// </para>
/// <para>
/// Status is orthogonal to categorization; a transaction may be categorized
/// regardless of its review state, depending on application policy.
/// </para>
/// </remarks>
public enum TransactionStatus
{
    /// <summary>
    /// Indicates that the transaction requires user review.
    /// </summary>
    /// <remarks>
    /// Transactions in this state may be newly imported, automatically categorized,
    /// or flagged due to ambiguity or incomplete information.
    /// </remarks>
    NeedsReview,

    /// <summary>
    /// Indicates that the transaction has been reviewed and confirmed by the user.
    /// </summary>
    /// <remarks>
    /// Reviewed transactions are typically considered finalized for reporting,
    /// budgeting, and analytics workflows.
    /// </remarks>
    Reviewed,

    /// <summary>
    /// Indicates that the transaction has been intentionally ignored by the user.
    /// </summary>
    /// <remarks>
    /// Ignored transactions are excluded from categorization, reporting, and budgeting.
    /// This status is commonly used for test entries, duplicates, or non-financial records.
    /// </remarks>
    Ignored
}