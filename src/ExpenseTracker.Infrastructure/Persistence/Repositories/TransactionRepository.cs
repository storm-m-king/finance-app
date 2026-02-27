using ExpenseTracker.Domain.Transaction;
using ExpenseTracker.Services.Contracts;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

/// <summary>
/// SQLite-backed repository for <see cref="Transaction"/> persistence and queries.
/// </summary>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public TransactionRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Transaction id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT id, account_id, posted_date, amount_cents, raw_description,
                     normalized_description, category_id, status, is_transfer,
                     notes, source_file_name, import_timestamp, fingerprint
              FROM transactions
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapTransaction(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByAccountAsync(
        Guid accountId, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("Account id cannot be empty.", nameof(accountId));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        var sql = @"
              SELECT id, account_id, posted_date, amount_cents, raw_description,
                     normalized_description, category_id, status, is_transfer,
                     notes, source_file_name, import_timestamp, fingerprint
              FROM transactions
              WHERE account_id = @account_id";

        AddParam(cmd, "@account_id", accountId.ToString());

        if (from.HasValue)
        {
            sql += " AND posted_date >= @from";
            AddParam(cmd, "@from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            sql += " AND posted_date <= @to";
            AddParam(cmd, "@to", to.Value.ToString("yyyy-MM-dd"));
        }

        sql += " ORDER BY posted_date DESC;";
        cmd.CommandText = sql;

        return await ReadAllAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetAllAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        var sql = @"
              SELECT id, account_id, posted_date, amount_cents, raw_description,
                     normalized_description, category_id, status, is_transfer,
                     notes, source_file_name, import_timestamp, fingerprint
              FROM transactions
              WHERE 1=1";

        if (from.HasValue)
        {
            sql += " AND posted_date >= @from";
            AddParam(cmd, "@from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            sql += " AND posted_date <= @to";
            AddParam(cmd, "@to", to.Value.ToString("yyyy-MM-dd"));
        }

        sql += " ORDER BY posted_date DESC;";
        cmd.CommandText = sql;

        return await ReadAllAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByFingerprintAsync(string fingerprint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("Fingerprint cannot be empty.", nameof(fingerprint));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"SELECT 1 FROM transactions WHERE fingerprint = @fingerprint LIMIT 1;";
        AddParam(cmd, "@fingerprint", fingerprint);

        var scalar = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
        return scalar is not null && scalar is not DBNull;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = UpsertSql;
        AddTransactionParams(cmd, transaction);

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddOrUpdateRangeAsync(IReadOnlyList<Transaction> transactions, CancellationToken ct = default)
    {
        if (transactions is null) throw new ArgumentNullException(nameof(transactions));
        if (transactions.Count == 0) return;

        using var conn = _connectionFactory.CreateOpenConnection();

        using var txn = conn.BeginTransaction();
        try
        {
            foreach (var transaction in transactions)
            {
                ct.ThrowIfCancellationRequested();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = txn as IDbTransaction;
                cmd.CommandText = UpsertSql;
                AddTransactionParams(cmd, transaction);
                await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
            }
            txn.Commit();
        }
        catch
        {
            txn.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Transaction id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM transactions WHERE id = @id;";
        AddParam(cmd, "@id", id.ToString());

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("Category id cannot be empty.", nameof(categoryId));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(DISTINCT id) FROM transactions WHERE category_id = @category_id;";
        AddParam(cmd, "@category_id", categoryId.ToString());

        var scalar = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
        return Convert.ToInt32(scalar);
    }

    /// <inheritdoc />
    public async Task ReassignCategoryAsync(Guid fromCategoryId, Guid toCategoryId, CancellationToken ct = default)
    {
        if (fromCategoryId == Guid.Empty) throw new ArgumentException("Source category id cannot be empty.", nameof(fromCategoryId));
        if (toCategoryId == Guid.Empty) throw new ArgumentException("Target category id cannot be empty.", nameof(toCategoryId));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"UPDATE transactions SET category_id = @to_id, status = @status WHERE category_id = @from_id;";
        AddParam(cmd, "@from_id", fromCategoryId.ToString());
        AddParam(cmd, "@to_id", toCategoryId.ToString());
        AddParam(cmd, "@status", (int)TransactionStatus.NeedsReview);

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    // ─── SQL Constants ─────────────────────────────────────────────

    private const string UpsertSql = @"
        INSERT INTO transactions (id, account_id, posted_date, amount_cents, raw_description,
                                  normalized_description, category_id, status, is_transfer,
                                  notes, source_file_name, import_timestamp, fingerprint)
        VALUES (@id, @account_id, @posted_date, @amount_cents, @raw_description,
                @normalized_description, @category_id, @status, @is_transfer,
                @notes, @source_file_name, @import_timestamp, @fingerprint)
        ON CONFLICT(id) DO UPDATE SET
            account_id = excluded.account_id,
            posted_date = excluded.posted_date,
            amount_cents = excluded.amount_cents,
            raw_description = excluded.raw_description,
            normalized_description = excluded.normalized_description,
            category_id = excluded.category_id,
            status = excluded.status,
            is_transfer = excluded.is_transfer,
            notes = excluded.notes,
            source_file_name = excluded.source_file_name,
            import_timestamp = excluded.import_timestamp,
            fingerprint = excluded.fingerprint;
    ";

    // ─── Mapping ────────────────────────────────────────────────────

    private static Transaction MapTransaction(IDataRecord record)
    {
        // SELECT order:
        //  0 id, 1 account_id, 2 posted_date, 3 amount_cents, 4 raw_description,
        //  5 normalized_description, 6 category_id, 7 status, 8 is_transfer,
        //  9 notes, 10 source_file_name, 11 import_timestamp, 12 fingerprint
        var id = Guid.Parse(record.GetString(0));
        var accountId = Guid.Parse(record.GetString(1));
        var postedDate = DateOnly.Parse(record.GetString(2));
        var amountCents = record.GetInt64(3);
        var rawDescription = record.GetString(4);
        var normalizedDescription = record.GetString(5);
        var categoryId = Guid.Parse(record.GetString(6));
        var status = (TransactionStatus)record.GetInt32(7);
        var isTransfer = record.GetInt32(8) != 0;
        var notes = record.IsDBNull(9) ? null : record.GetString(9);
        var sourceFileName = record.IsDBNull(10) ? null : record.GetString(10);

        DateTimeOffset? importTimestamp = null;
        if (!record.IsDBNull(11))
            importTimestamp = DateTimeOffset.Parse(record.GetString(11));

        var fingerprint = record.GetString(12);

        return Transaction.Create(
            accountId: accountId,
            postedDate: postedDate,
            amountCents: amountCents,
            rawDescription: rawDescription,
            normalizedDescription: normalizedDescription,
            categoryId: categoryId,
            fingerprint: fingerprint,
            status: status,
            isTransfer: isTransfer,
            notes: notes,
            sourceFileName: sourceFileName,
            importTimestamp: importTimestamp,
            id: id);
    }

    private static void AddTransactionParams(IDbCommand cmd, Transaction t)
    {
        AddParam(cmd, "@id", t.Id.ToString());
        AddParam(cmd, "@account_id", t.AccountId.ToString());
        AddParam(cmd, "@posted_date", t.PostedDate.ToString("yyyy-MM-dd"));
        AddParam(cmd, "@amount_cents", t.AmountCents);
        AddParam(cmd, "@raw_description", t.RawDescription);
        AddParam(cmd, "@normalized_description", t.NormalizedDescription);
        AddParam(cmd, "@category_id", t.CategoryId.ToString());
        AddParam(cmd, "@status", (int)t.Status);
        AddParam(cmd, "@is_transfer", t.IsTransfer ? 1 : 0);
        AddParam(cmd, "@notes", (object?)t.Notes ?? DBNull.Value);
        AddParam(cmd, "@source_file_name", (object?)t.SourceFileName ?? DBNull.Value);
        AddParam(cmd, "@import_timestamp", (object?)t.ImportTimestamp?.ToString("O") ?? DBNull.Value);
        AddParam(cmd, "@fingerprint", t.Fingerprint);
    }

    // ─── ADO Helpers ────────────────────────────────────────────────

    private async Task<IReadOnlyList<Transaction>> ReadAllAsync(IDbCommand cmd, CancellationToken ct)
    {
        var results = new List<Transaction>();
        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapTransaction(reader));
        }
        return results;
    }

    private static void AddParam(IDbCommand command, string name, object? value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        command.Parameters.Add(p);
    }

    private static Task<int> ExecuteNonQueryAsync(IDbCommand cmd, CancellationToken ct)
    {
        if (cmd is SqliteCommand sc)
            return sc.ExecuteNonQueryAsync(ct);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(cmd.ExecuteNonQuery());
    }

    private static Task<object?> ExecuteScalarAsync(IDbCommand cmd, CancellationToken ct)
    {
        if (cmd is SqliteCommand sc)
            return sc.ExecuteScalarAsync(ct);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(cmd.ExecuteScalar());
    }

    private static Task<SqliteDataReader> ExecuteReaderAsync(IDbCommand cmd, CancellationToken ct)
    {
        if (cmd is SqliteCommand sc)
            return sc.ExecuteReaderAsync(ct);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult<SqliteDataReader>((SqliteDataReader)(((IDataReader)cmd.ExecuteReader()) as DbDataReader
                                                                    ?? throw new InvalidOperationException("Command did not return a DbDataReader.")));
    }
}
