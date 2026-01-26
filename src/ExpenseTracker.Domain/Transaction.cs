namespace ExpenseTracker.Domain;
/// <summary>
/// Represents a financial transaction imported or created in the system.
/// This entity enforces basic invariants (valid IDs, required text fields, valid amounts)
/// and exposes intention-revealing operations for state changes.
/// </summary>
public sealed class Transaction
{
    /// <summary>Unique identifier for this transaction.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning account identifier.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Date the transaction posted (bank posting date, not import date).</summary>
    public DateOnly PostedDate { get; private set; }

    /// <summary>
    /// Amount in cents (minor units). Negative typically indicates an outflow, positive an inflow.
    /// </summary>
    public long AmountCents { get; private set; }

    /// <summary>Original description as received from the source (bank/CSV).</summary>
    public string RawDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Normalized description used for grouping/searching/matching.
    /// Keep normalization logic outside the entity and pass the result in.
    /// </summary>
    public string NormalizedDescription { get; private set; } = string.Empty;

    /// <summary>Assigned category identifier.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Workflow status for the transaction.</summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>Indicates whether this transaction represents a transfer between accounts.</summary>
    public bool IsTransfer { get; private set; }

    /// <summary>Optional user notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Optional name of the file this transaction was imported from.</summary>
    public string? SourceFileName { get; private set; }

    /// <summary>Optional timestamp when this transaction was imported.</summary>
    public DateTimeOffset? ImportTimestamp { get; private set; }

    /// <summary>
    /// Deterministic fingerprint used for deduplication/matching across imports.
    /// </summary>
    public string Fingerprint { get; private set; } = string.Empty;

    // Private constructor for ORM/serialization.
    private Transaction() { }

    private Transaction
    (
        Guid id,
        Guid accountId,
        DateOnly postedDate,
        long amountCents,
        string rawDescription,
        string normalizedDescription,
        Guid categoryId,
        TransactionStatus status,
        bool isTransfer,
        string? notes,
        string? sourceFileName,
        DateTimeOffset? importTimestamp,
        string fingerprint
    )
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Id cannot be empty.", nameof(id)) : id;
        AccountId = accountId == Guid.Empty ? throw new ArgumentException("AccountId cannot be empty.", nameof(accountId)) : accountId;

        // DateOnly is a value type, but you still might want a sanity check if your domain requires it.
        PostedDate = postedDate;

        // Amount may be negative or positive; zero transactions are often invalid.
        if (amountCents == 0)
            throw new ArgumentOutOfRangeException(nameof(amountCents), "AmountCents cannot be zero.");

        AmountCents = amountCents;

        RawDescription = ValidateRequiredText(rawDescription, nameof(rawDescription));
        NormalizedDescription = ValidateRequiredText(normalizedDescription, nameof(normalizedDescription));

        CategoryId = categoryId == Guid.Empty ? throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId)) : categoryId;

        Status = status;
        IsTransfer = isTransfer;

        Notes = NormalizeOptionalText(notes);
        SourceFileName = NormalizeOptionalText(sourceFileName);
        ImportTimestamp = importTimestamp;

        Fingerprint = ValidateRequiredText(fingerprint, nameof(fingerprint));
    }

    /// <summary>
    /// Creates a new <see cref="Transaction"/> with required fields.
    /// Prefer factories to ensure invariants are enforced consistently.
    /// </summary>
    public static Transaction Create
    (
        Guid accountId,
        DateOnly postedDate,
        long amountCents,
        string rawDescription,
        string normalizedDescription,
        Guid categoryId,
        string fingerprint,
        TransactionStatus status = TransactionStatus.NeedsReview,
        bool isTransfer = false,
        string? notes = null,
        string? sourceFileName = null,
        DateTimeOffset? importTimestamp = null,
        Guid? id = null
    )
    {
        return new Transaction
        (
            id ?? Guid.NewGuid(),
            accountId,
            postedDate,
            amountCents,
            rawDescription,
            normalizedDescription,
            categoryId,
            status,
            isTransfer,
            notes,
            sourceFileName,
            importTimestamp,
            fingerprint
        );
    }

    /// <summary>Updates the category assigned to this transaction.</summary>
    public void SetCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
        CategoryId = categoryId;
    }

    /// <summary>Marks this transaction as a transfer (or clears the flag).</summary>
    public void SetTransfer(bool isTransfer) => IsTransfer = isTransfer;

    /// <summary>Updates the workflow status.</summary>
    public void SetStatus(TransactionStatus status) => Status = status;

    /// <summary>Updates optional notes (null/whitespace clears).</summary>
    public void SetNotes(string? notes) => Notes = NormalizeOptionalText(notes);

    /// <summary>Updates the normalized description (must be non-empty).</summary>
    public void SetNormalizedDescription(string normalizedDescription)
        => NormalizedDescription = ValidateRequiredText(normalizedDescription, nameof(normalizedDescription));

    /// <summary>
    /// Attaches import metadata. Intended to be called by import workflows.
    /// </summary>
    public void SetImportMetadata(string? sourceFileName, DateTimeOffset? importTimestamp)
    {
        SourceFileName = NormalizeOptionalText(sourceFileName);
        ImportTimestamp = importTimestamp;
    }

    private static string ValidateRequiredText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}