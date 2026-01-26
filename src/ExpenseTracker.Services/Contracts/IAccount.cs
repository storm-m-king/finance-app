using ExpenseTracker.Domain.Account;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Represents a financial account (e.g., checking, savings, credit card)
/// that owns transactions and participates in categorization and reporting workflows.
///
/// This interface exposes the observable state and supported behaviors of an account
/// without leaking persistence or implementation details.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to enforce domain invariants such as:
/// <list type="bullet">
/// <item><description>A non-empty <see cref="Id"/></description></item>
/// <item><description>A non-null, non-whitespace <see cref="Name"/></description></item>
/// <item><description>A valid <see cref="CreditSignConvention"/></description></item>
/// </list>
/// </para>
/// <para>
/// Mutating operations may throw <see cref="InvalidOperationException"/> if the
/// account is in a state that disallows modification (e.g., archived).
/// </para>
/// </remarks>
public interface IAccount
{
    /// <summary>
    /// Gets the unique identifier for this account.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the human-friendly display name of the account.
    /// </summary>
    /// <remarks>
    /// The name is intended for UI presentation and user-facing workflows.
    /// Implementations should ensure this value is never null or whitespace.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the logical account type (e.g., Checking, Savings, CreditCard).
    /// </summary>
    AccountType Type { get; }

    /// <summary>
    /// Gets a value indicating whether this account is archived.
    /// </summary>
    /// <remarks>
    /// Archived accounts are typically hidden from standard workflows and may
    /// disallow certain mutations depending on domain policy.
    /// </remarks>
    bool IsArchived { get; }

    /// <summary>
    /// Gets the convention describing how the account provider represents
    /// credits and debits during transaction imports.
    /// </summary>
    /// <remarks>
    /// This value is used during import normalization to determine whether
    /// positive or negative amounts represent credits for this account.
    /// </remarks>
    CreditSignConvention CreditSignConvention { get; }

    /// <summary>
    /// Renames the account.
    /// </summary>
    /// <param name="name">The new display name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the account is archived and renaming is disallowed by policy.
    /// </exception>
    void Rename(string name);

    /// <summary>
    /// Archives the account, marking it as inactive for normal workflows.
    /// </summary>
    /// <remarks>
    /// Archiving is typically reversible via <see cref="Unarchive"/>.
    /// </remarks>
    void Archive();

    /// <summary>
    /// Restores an archived account to an active state.
    /// </summary>
    void Unarchive();

    /// <summary>
    /// Updates the sign convention used when importing transactions for this account.
    /// </summary>
    /// <param name="convention">The new credit sign convention.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="convention"/> is invalid (e.g., Unknown).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the account is archived and mutation is disallowed by policy.
    /// </exception>
    void SetCreditSignConvention(CreditSignConvention convention);

    /// <summary>
    /// Changes the logical account type.
    /// </summary>
    /// <param name="type">The new account type.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the account is archived and mutation is disallowed by policy.
    /// </exception>
    void SetType(AccountType type);
}
