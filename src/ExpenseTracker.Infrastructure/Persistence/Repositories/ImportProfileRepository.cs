using ExpenseTracker.Domain.ImportProfile;
using ExpenseTracker.Services.Contracts;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

/// <summary>
/// SQLite-backed repository for <see cref="ImportProfile"/> persistence and queries.
/// </summary>
public sealed class ImportProfileRepository : IImportProfileRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates a new <see cref="ImportProfileRepository"/>.
    /// </summary>
    public ImportProfileRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<IImportProfile?> GetByKeyAsync(string profileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
            throw new ArgumentException("Profile key cannot be null or empty.", nameof(profileKey));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT
                profile_key,
                profile_name,
                expected_header_csv,
                date_header,
                description_header,
                amount_header,
                normalized_description_csv,
                normalized_description_delimiter
              FROM import_profiles
              WHERE profile_key = @profile_key
              LIMIT 1;
             ";

        AddParam(cmd, "@profile_key", profileKey.Trim());

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapImportProfile(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IImportProfile>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT
                profile_key,
                profile_name,
                expected_header_csv,
                date_header,
                description_header,
                amount_header,
                normalized_description_csv,
                normalized_description_delimiter
              FROM import_profiles
              ORDER BY profile_name ASC;
             ";

        var results = new List<ImportProfile>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapImportProfile(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string profileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
            throw new ArgumentException("Profile key cannot be null or empty.", nameof(profileKey));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT 1
              FROM import_profiles
              WHERE profile_key = @profile_key
              LIMIT 1;
             ";

        AddParam(cmd, "@profile_key", profileKey.Trim());

        var scalar = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
        return scalar is not null && scalar is not DBNull;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(IImportProfile profile, CancellationToken ct = default)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              INSERT INTO import_profiles (
                profile_key,
                profile_name,
                expected_header_csv,
                date_header,
                description_header,
                amount_header,
                normalized_description_csv,
                normalized_description_delimiter
              )
              VALUES (
                @profile_key,
                @profile_name,
                @expected_header_csv,
                @date_header,
                @description_header,
                @amount_header,
                @normalized_description_csv,
                @normalized_description_delimiter
              )
              ON CONFLICT(profile_key) DO UPDATE SET
                profile_name = excluded.profile_name,
                expected_header_csv = excluded.expected_header_csv,
                date_header = excluded.date_header,
                description_header = excluded.description_header,
                amount_header = excluded.amount_header,
                normalized_description_csv = excluded.normalized_description_csv,
                normalized_description_delimiter = excluded.normalized_description_delimiter;
             ";

        AddParam(cmd, "@profile_key", profile.ProfileKey);
        AddParam(cmd, "@profile_name", profile.ProfileName);
        AddParam(cmd, "@expected_header_csv", profile.ExpectedHeaderCsv);
        AddParam(cmd, "@date_header", profile.DateHeader);
        AddParam(cmd, "@description_header", profile.DescriptionHeader);
        AddParam(cmd, "@amount_header", profile.AmountHeader);
        AddParam(cmd, "@normalized_description_csv", profile.NormalizedDescriptionCsv);
        AddParam(cmd, "@normalized_description_delimiter", profile.NormalizedDescriptionDelimiter);

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string profileKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
            throw new ArgumentException("Profile key cannot be null or empty.", nameof(profileKey));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM import_profiles WHERE profile_key = @profile_key;";
        AddParam(cmd, "@profile_key", profileKey.Trim());

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }

    private static ImportProfile MapImportProfile(IDataRecord record)
    {
        // SELECT order:
        // 0 profile_key
        // 1 profile_name
        // 2 expected_header_csv
        // 3 date_header
        // 4 description_header
        // 5 amount_header
        // 6 normalized_description_csv
        // 7 normalized_description_delimiter
        return new ImportProfile(
            profileKey: record.GetString(0),
            profileName: record.GetString(1),
            expectedHeaderCsv: record.GetString(2),
            dateHeader: record.GetString(3),
            descriptionHeader: record.GetString(4),
            amountHeader: record.GetString(5),
            normalizedDescriptionCsv: record.GetString(6),
            normalizedDescriptionDelimiter: record.GetString(7)
        );
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
        return Task.FromResult<SqliteDataReader>(
            (SqliteDataReader)(((IDataReader)cmd.ExecuteReader()) as DbDataReader
                               ?? throw new InvalidOperationException("Command did not return a DbDataReader.")));
    }
}

