namespace ExpenseTracker.Domain.Account;

/// <summary>
/// Represents a financial account (e.g., checking, savings, credit card) that owns transactions.
/// Enforces basic invariants (valid ID, valid name) and provides intention-revealing operations
/// for state changes such as archiving and renaming.
/// </summary>
public sealed class Account : IAccount
{
    /// <summary>Unique identifier for this account.</summary>
    public Guid Id { get; private set; }

    /// <summary>Human-friendly account name shown in the UI.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Logical account type (e.g., Checking, CreditCard).</summary>
    public AccountType Type { get; private set; }

    /// <summary>
    /// Indicates whether this account is archived (hidden from normal workflows).
    /// Archived accounts typically cannot accept new transactions in the UI, depending on policy.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Optional convention describing how a provider represents credits/debits for this account.
    /// Useful for imports where the sign meaning varies by institution.
    /// </summary>
    public CreditSignConvention CreditSignConvention
    {
        get => CreditSignConvention;
        private set
        {
            if (value == CreditSignConvention.Unknown)
            {
                throw new ArgumentException("CreditSignConvention cannot be unknown", nameof(CreditSignConvention));
            }
            CreditSignConvention = value;
        }
    }
    
    /// <summary>
    /// Import profile key used to select the CSV parsing rules for this account.
    /// Examples: "amex.v1", "sofi.v1".
    /// </summary>
    public string ImportProfileKey { get; private set; } = string.Empty;

    // Private constructor for ORM/serialization.
    private Account() { }

    private Account
    (
        Guid id,
        string name,
        AccountType type,
        bool isArchived,
        CreditSignConvention creditSignConvention,
        string importProfileKey
    )
    {
        Id = id == Guid.Empty ? throw new ArgumentException("Id cannot be empty.", nameof(id)) : id;
        Name = ValidateRequiredName(name, nameof(name));
        Type = type;
        IsArchived = isArchived;
        CreditSignConvention = creditSignConvention;
        ImportProfileKey = ValidateRequiredKey(importProfileKey, nameof(importProfileKey));
    }

    /// <summary>
    /// Creates a new <see cref="Account"/> enforcing entity invariants.
    /// </summary>
    /// <param name="name">Display name for the account.</param>
    /// <param name="type">Account type.</param>
    /// <param name="creditSignConvention">Sign convention for imports.</param>
    /// <param name="importProfileKey">Import profile key used to select the CSV parsing rules for this account.</param>
    /// <param name="id">Optional ID; if not provided, a new GUID is generated.</param>
    public static Account Create
    (
        string name,
        AccountType type,
        CreditSignConvention creditSignConvention,
        string importProfileKey,
        Guid? id = null
    )
    {
        return new Account
        (
            id ?? Guid.NewGuid(),
            name,
            type,
            isArchived: false,
            creditSignConvention,
            importProfileKey
        );
    }
    
    /// <summary>
    /// Updates the import profile key for this account.
    /// </summary>
    public void SetImportProfileKey(string importProfileKey)
    {
        EnsureNotArchivedForMutation();
        ImportProfileKey = ValidateRequiredKey(importProfileKey, nameof(importProfileKey));
    }

    /// <summary>
    /// Renames the account.
    /// </summary>
    /// <param name="name">New display name.</param>
    public void Rename(string name)
    {
        EnsureNotArchivedForMutation();
        Name = ValidateRequiredName(name, nameof(name));
    }

    /// <summary>
    /// Archives the account.
    /// </summary>
    public void Archive() => IsArchived = true;

    /// <summary>
    /// Restores an archived account.
    /// </summary>
    public void Unarchive() => IsArchived = false;

    /// <summary>
    /// Updates the sign convention used when importing transactions for this account.
    /// </summary>
    /// <param name="convention">The sign convention to apply, or null to clear.</param>
    public void SetCreditSignConvention(CreditSignConvention convention)
    {
        EnsureNotArchivedForMutation();
        CreditSignConvention = convention;
    }

    /// <summary>
    /// Changes the account type.
    /// This is a domain decision: allow it only if your system treats type as editable.
    /// </summary>
    /// <param name="type">New account type.</param>
    public void SetType(AccountType type)
    {
        EnsureNotArchivedForMutation();
        Type = type;
    }

    private static string ValidateRequiredName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be null, empty, or whitespace.", paramName);

        var trimmed = value.Trim();

        // Optional: protect against absurd names; tune as needed.
        if (trimmed.Length > 100)
            throw new ArgumentOutOfRangeException(paramName, "Name cannot exceed 100 characters.");

        return trimmed;
    }

    private void EnsureNotArchivedForMutation()
    {
        // Policy choice: if you want archived accounts to still be editable, remove this guard.
        if (IsArchived)
            throw new InvalidOperationException("Archived accounts cannot be modified.");
    }
    
    private static string ValidateRequiredKey(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ImportProfileKey cannot be null, empty, or whitespace.", paramName);

        return value.Trim();
    }
}