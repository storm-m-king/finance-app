namespace ExpenseTracker.Domain.Account;

/// <summary>
/// Defines the logical type of financial account.
/// </summary>
/// <remarks>
/// <para>
/// The account type influences how transactions are interpreted, displayed,
/// and reported within the system.
/// </para>
/// <para>
/// Account types are distinct from institutions or providers and represent
/// high-level behavioral categories (e.g., deposit accounts vs. revolving credit).
/// </para>
/// </remarks>
public enum AccountType
{
    /// <summary>
    /// A deposit account used for day-to-day transactions such as purchases,
    /// bill payments, and direct deposits.
    /// </summary>
    /// <remarks>
    /// Checking accounts typically allow both debits and credits and are often
    /// the primary source of transaction volume in a personal finance system.
    /// </remarks>
    Checking,

    /// <summary>
    /// A revolving credit account where purchases increase a balance owed
    /// and payments reduce the outstanding balance.
    /// </summary>
    /// <remarks>
    /// Credit accounts often invert the meaning of positive and negative transaction
    /// amounts compared to deposit accounts, making them sensitive to
    /// credit/debit sign conventions during imports.
    /// </remarks>
    Credit,
}