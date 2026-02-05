using ExpenseTracker.Domain.Category;
using ExpenseTracker.Services.Contracts;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

/// <summary>
/// SQLite-backed repository for <see cref="Category"/> persistence and queries.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates a new <see cref="CategoryRepository"/>.
    /// </summary>
    public CategoryRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT
                id,
                name,
                is_system,
                type
              FROM categories
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return MapCategory(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT
                id,
                name,
                is_system,
                type
              FROM categories
              ORDER BY name ASC;
             ";

        var results = new List<Category>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapCategory(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetByTypeAsync(CategoryType type, CancellationToken ct = default)
    {
        if (type == CategoryType.Default)
            throw new ArgumentException("Type cannot be default.", nameof(type));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT
                id,
                name,
                is_system,
                type
              FROM categories
              WHERE type = @type
              ORDER BY name ASC;
             ";

        AddParam(cmd, "@type", type.ToString());

        var results = new List<Category>();

        using var reader = await ExecuteReaderAsync(cmd, ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            results.Add(MapCategory(reader));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(Category category, CancellationToken ct = default)
    {
        if (category is null)
            throw new ArgumentNullException(nameof(category));

        if (category.Id == Guid.Empty)
            throw new ArgumentException("Category id cannot be empty.", nameof(category));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        // Uses SQLite UPSERT semantics (same pattern as ImportProfileRepository)
        cmd.CommandText =
            @"
          INSERT INTO categories (
            id,
            name,
            is_system,
            type
          )
          VALUES (
            @id,
            @name,
            @is_system,
            @type
          )
          ON CONFLICT(id) DO UPDATE SET
            name = excluded.name,
            is_system = excluded.is_system,
            type = excluded.type;
         ";

        AddParam(cmd, "@id", category.Id.ToString());
        AddParam(cmd, "@name", category.Name);
        AddParam(cmd, "@is_system", category.IsSystemCategory ? 1 : 0);
        AddParam(cmd, "@type", category.Type.ToString());

        await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        // Enforce contract behavior: not found -> KeyNotFoundException; system -> InvalidOperationException
        var existing = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No category exists with id '{id}'.");

        if (existing.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deleted.");

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"DELETE FROM categories WHERE id = @id;";
        AddParam(cmd, "@id", id.ToString());

        var affected = await ExecuteNonQueryAsync(cmd, ct).ConfigureAwait(false);
        if (affected == 0)
            throw new KeyNotFoundException($"No category exists with id '{id}'.");
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        using var conn = _connectionFactory.CreateOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText =
            @"
              SELECT 1
              FROM categories
              WHERE id = @id
              LIMIT 1;
             ";

        AddParam(cmd, "@id", id.ToString());

        var scalar = await ExecuteScalarAsync(cmd, ct).ConfigureAwait(false);
        return scalar is not null && scalar is not DBNull;
    }

    private static Category MapCategory(IDataRecord record)
    {
        // SELECT order:
        // 0 id
        // 1 name
        // 2 is_system
        // 3 type
        var id = Guid.Parse(record.GetString(0));
        var name = record.GetString(1);
        var isSystem = ReadBool(record.GetValue(2));
        var type = ParseCategoryType(record.GetString(3));

        return new Category(id, name, isSystem, type);
    }

    private static bool ReadBool(object value)
    {
        return value switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            short s => s != 0,
            byte by => by != 0,
            string str when int.TryParse(str, out var i) => i != 0,
            _ => Convert.ToInt32(value) != 0
        };
    }

    private static CategoryType ParseCategoryType(string? persisted)
    {
        if (string.IsNullOrWhiteSpace(persisted))
            throw new DataException("Category 'type' column is null/empty.");

        if (!Enum.TryParse<CategoryType>(persisted, ignoreCase: true, out var type))
            throw new DataException($"Unknown CategoryType persisted value '{persisted}'.");

        if (type == CategoryType.Default)
            throw new DataException("Persisted CategoryType cannot be Default.");

        return type;
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