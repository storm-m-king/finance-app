using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.Services.DTOs;

namespace ExpenseTracker.Services.Services.Transaction;

/// <summary>
/// Orchestrates transaction business operations including import, updates,
/// and invariant enforcement (transfer toggle, status rules).
/// </summary>
public sealed class TransactionService : ITransactionService
{
    // Well-known system category GUIDs (from SystemSeeder)
    private static readonly Guid UncategorizedExpenseId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TransferCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly ITransactionRepository _transactionRepository;
    private readonly IAppLogger _logger;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IAppLogger logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> ImportTransactionsAsync(
        IReadOnlyList<TransactionPreviewRow> previewRows,
        string sourceFileName,
        CancellationToken ct = default)
    {
        if (previewRows is null) throw new ArgumentNullException(nameof(previewRows));

        var toInsert = new List<Domain.Transaction.Transaction>();
        var skippedDuplicates = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var row in previewRows)
        {
            ct.ThrowIfCancellationRequested();

            // Deduplication: skip if fingerprint already exists
            if (await _transactionRepository.ExistsByFingerprintAsync(row.Fingerprint, ct).ConfigureAwait(false))
            {
                skippedDuplicates++;
                continue;
            }

            // Determine category and status per design doc Section 10.4
            var categoryId = row.CategoryId ?? UncategorizedExpenseId;
            var isAutoCategorized = row.CategoryId.HasValue && row.CategoryId.Value != UncategorizedExpenseId;
            var status = isAutoCategorized ? TransactionStatus.Reviewed : TransactionStatus.NeedsReview;
            var isTransfer = categoryId == TransferCategoryId;

            var transaction = Domain.Transaction.Transaction.Create(
                accountId: row.AccountId,
                postedDate: row.PostedDate,
                amountCents: row.AmountCents,
                rawDescription: row.RawDescription,
                normalizedDescription: row.NormalizedDescription,
                categoryId: categoryId,
                fingerprint: row.Fingerprint,
                status: status,
                isTransfer: isTransfer,
                sourceFileName: sourceFileName,
                importTimestamp: now);

            toInsert.Add(transaction);
        }

        if (toInsert.Count > 0)
        {
            await _transactionRepository.AddOrUpdateRangeAsync(toInsert, ct).ConfigureAwait(false);
        }

        _logger.Info($"[TransactionService] Imported {toInsert.Count} transactions " +
                     $"({skippedDuplicates} duplicates skipped).");

        return toInsert.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Transaction.Transaction>> GetAllTransactionsAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        return await _transactionRepository.GetAllAsync(from, to, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateCategoryAsync(Guid transactionId, Guid categoryId, CancellationToken ct = default)
    {
        var transaction = await GetRequiredTransactionAsync(transactionId, ct).ConfigureAwait(false);

        transaction.SetCategory(categoryId);

        // If category is Transfer, set IsTransfer = true
        if (categoryId == TransferCategoryId)
        {
            transaction.SetTransfer(true);
        }

        await _transactionRepository.AddOrUpdateAsync(transaction, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid transactionId, TransactionStatus status, CancellationToken ct = default)
    {
        var transaction = await GetRequiredTransactionAsync(transactionId, ct).ConfigureAwait(false);
        transaction.SetStatus(status);
        await _transactionRepository.AddOrUpdateAsync(transaction, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ToggleTransferAsync(Guid transactionId, bool isTransfer, CancellationToken ct = default)
    {
        var transaction = await GetRequiredTransactionAsync(transactionId, ct).ConfigureAwait(false);

        transaction.SetTransfer(isTransfer);

        if (isTransfer)
        {
            // Toggle ON: Category → Transfer
            transaction.SetCategory(TransferCategoryId);
        }
        else
        {
            // Toggle OFF: Category → Uncategorized, Status → NeedsReview
            transaction.SetCategory(UncategorizedExpenseId);
            transaction.SetStatus(TransactionStatus.NeedsReview);
        }

        await _transactionRepository.AddOrUpdateAsync(transaction, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateNotesAsync(Guid transactionId, string? notes, CancellationToken ct = default)
    {
        var transaction = await GetRequiredTransactionAsync(transactionId, ct).ConfigureAwait(false);
        transaction.SetNotes(notes);
        await _transactionRepository.AddOrUpdateAsync(transaction, ct).ConfigureAwait(false);
    }

    private async Task<Domain.Transaction.Transaction> GetRequiredTransactionAsync(Guid id, CancellationToken ct)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (transaction is null)
            throw new InvalidOperationException($"Transaction {id} not found.");
        return transaction;
    }
}
