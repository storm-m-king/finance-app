using ExpenseTracker.Domain.Account;
using ExpenseTracker.Services.Contracts;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

/// <summary>
/// SQLite-backed repository for <see cref="Account"/> persistence and queries.
/// </summary>
public sealed class AccountRepository : IAccountRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates a new <see cref="AccountRepository"/>.
    /// </summary>
    /// <param name="connectionFactory">Factory that creates open SQLite connections.</param>
    public AccountRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Account id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              SELECT id, name, type, is_archived, credit_sign_convention, import_profile_key
              FROM accounts
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapAccount(reader);
    }
    
    /// <inheritdoc />
    public async Task<Account?> GetByImportProfileKeyAsync(string importProfileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(importProfileKey)) throw new ArgumentException("profile key cannot be empty.", nameof(importProfileKey));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              SELECT id, name, type, is_archived, credit_sign_convention, import_profile_key
              FROM accounts
              WHERE import_profile_key = @import_profile_key
              LIMIT 1;
             ";

        AddParam(cmd, "@import_profile_key", importProfileKey);

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapAccount(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              SELECT id, name, type, is_archived, credit_sign_convention, import_profile_key
              FROM accounts
              ORDER BY name ASC;
             ";

        var results = new List<Account>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapAccount(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetActiveAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              SELECT id, name, type, is_archived, credit_sign_convention, import_profile_key
              FROM accounts
              WHERE is_archived = 0
              ORDER BY name ASC;
             ";

        var results = new List<Account>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapAccount(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Account id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              SELECT 1
              FROM accounts
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        var scalar = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
        return scalar is not null && scalar is not DBNull;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(Account account, CancellationToken ct = default)
    {
        if (account is null) throw new ArgumentNullException(nameof(account));
        if (account.Id == Guid.Empty) throw new ArgumentException("Account id cannot be empty.", nameof(account));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = 
            @"
              INSERT INTO accounts (id, name, type, is_archived, credit_sign_convention, import_profile_key)
              VALUES (@id, @name, @type, @is_archived, @credit_sign_convention, @import_profile_key)
              ON CONFLICT(id) DO UPDATE SET
              name = excluded.name,
              type = excluded.type,
              is_archived = excluded.is_archived,
              credit_sign_convention = excluded.credit_sign_convention,
              import_profile_key = excluded.import_profile_key;
             ";

        AddParam(cmd, "@id", account.Id.ToString());
        AddParam(cmd, "@name", account.Name);
        AddParam(cmd, "@type", (int)account.Type);
        AddParam(cmd, "@is_archived", account.IsArchived ? 1 : 0);
        AddParam(cmd, "@credit_sign_convention", (int)account.CreditSignConvention);
        AddParam(cmd, "@import_profile_key", account.ImportProfileKey);

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Account id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM accounts WHERE id = @id;";
        AddParam(cmd, "@id", id.ToString());

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    private static Account MapAccount(IDataRecord record)
    {
        // SELECT order:
        // 0 id
        // 1 name
        // 2 type
        // 3 is_archived
        // 4 credit_sign_convention (nullable)
        // 5 import_profile_key
        var id = Guid.Parse(record.GetString(0));
        var name = record.GetString(1);

        var typeInt = record.GetInt32(2);
        var type = (AccountType)typeInt;

        var isArchived = record.GetInt32(3) != 0;

        CreditSignConvention convention = CreditSignConvention.Unknown;
        if (!record.IsDBNull(4))
        {
            var convInt = record.GetInt32(4);
            convention = (CreditSignConvention)convInt;
        }

        var importProfileKey = record.GetString(5);

        var account = Account.Create(
            name: name,
            type: type,
            importProfileKey: importProfileKey,
            creditSignConvention: convention,
            id: id);

        if (isArchived)
            account.Archive();

        return account;
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
