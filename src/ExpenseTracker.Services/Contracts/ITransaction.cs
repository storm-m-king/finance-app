using ExpenseTracker.Domain.Transaction;
namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Represents a financial transaction imported or created in the system.
/// </summary>
/// <remarks>
/// <para>
/// This interface exposes the observable state and supported behaviors of a transaction
/// without leaking persistence or implementation details.
/// </para>
/// <para>
/// Implementations are expected to enforce domain invariants such as:
/// <list type="bullet">
/// <item><description>Non-empty identifiers (e.g., <see cref="Id"/>, <see cref="AccountId"/>, <see cref="CategoryId"/>)</description></item>
/// <item><description>Non-zero <see cref="AmountCents"/></description></item>
/// <item><description>Non-empty <see cref="RawDescription"/>, <see cref="NormalizedDescription"/>, and <see cref="Fingerprint"/></description></item>
/// </list>
/// </para>
/// </remarks>
public interface ITransaction
{
    /// <summary>
    /// Gets the unique identifier for this transaction.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the identifier of the account that owns this transaction.
    /// </summary>
    Guid AccountId { get; }

    /// <summary>
    /// Gets the posting date of the transaction (bank posting date, not import date).
    /// </summary>
    DateOnly PostedDate { get; }

    /// <summary>
    /// Gets the amount in minor units (cents).
    /// </summary>
    /// <remarks>
    /// A negative value typically indicates an outflow (expense) and a positive value
    /// an inflow (income), depending on account sign conventions.
    /// Implementations should reject a value of zero.
    /// </remarks>
    long AmountCents { get; }

    /// <summary>
    /// Gets the raw description as received from the source (e.g., bank export or CSV import).
    /// </summary>
    string RawDescription { get; }

    /// <summary>
    /// Gets the normalized description used for search, grouping, and rule matching.
    /// </summary>
    /// <remarks>
    /// Normalization policy should be handled outside the entity and provided to the transaction.
    /// Implementations should ensure this value is non-empty and trimmed.
    /// </remarks>
    string NormalizedDescription { get; }

    /// <summary>
    /// Gets the assigned category identifier.
    /// </summary>
    Guid CategoryId { get; }

    /// <summary>
    /// Gets the workflow status of the transaction.
    /// </summary>
    TransactionStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether this transaction represents a transfer between accounts.
    /// </summary>
    bool IsTransfer { get; }

    /// <summary>
    /// Gets optional user notes associated with the transaction.
    /// </summary>
    string? Notes { get; }

    /// <summary>
    /// Gets the optional name of the file this transaction was imported from.
    /// </summary>
    string? SourceFileName { get; }

    /// <summary>
    /// Gets the optional timestamp when this transaction was imported.
    /// </summary>
    DateTimeOffset? ImportTimestamp { get; }

    /// <summary>
    /// Gets a deterministic fingerprint used for deduplication across imports.
    /// </summary>
    /// <remarks>
    /// The fingerprint should be stable for a given "logical transaction" and is typically derived
    /// from a subset of fields such as account, date, amount, and a normalized description.
    /// </remarks>
    string Fingerprint { get; }

    /// <summary>
    /// Updates the category assigned to this transaction.
    /// </summary>
    /// <param name="categoryId">The new category identifier.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="categoryId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    void SetCategory(Guid categoryId);

    /// <summary>
    /// Marks this transaction as a transfer (or clears the flag).
    /// </summary>
    /// <param name="isTransfer"><c>true</c> to mark as transfer; otherwise, <c>false</c>.</param>
    void SetTransfer(bool isTransfer);

    /// <summary>
    /// Updates the workflow status of this transaction.
    /// </summary>
    /// <param name="status">The new status.</param>
    void SetStatus(TransactionStatus status);

    /// <summary>
    /// Updates optional notes for this transaction.
    /// </summary>
    /// <param name="notes">
    /// Notes to associate with the transaction. Null or whitespace should clear notes.
    /// </param>
    void SetNotes(string? notes);

    /// <summary>
    /// Updates the normalized description used for matching and reporting.
    /// </summary>
    /// <param name="normalizedDescription">The new normalized description.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="normalizedDescription"/> is null, empty, or whitespace.
    /// </exception>
    void SetNormalizedDescription(string normalizedDescription);

    /// <summary>
    /// Attaches or updates import metadata for this transaction.
    /// </summary>
    /// <param name="sourceFileName">
    /// The name of the file the transaction was imported from (null/whitespace clears).
    /// </param>
    /// <param name="importTimestamp">
    /// The timestamp when the import occurred (null clears).
    /// </param>
    void SetImportMetadata(string? sourceFileName, DateTimeOffset? importTimestamp);
}
