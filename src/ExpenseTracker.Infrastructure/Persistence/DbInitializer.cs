using Dapper;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Initialize the Database, ensuring the schema is setup.
/// </summary>
public sealed class DbInitializer
{
    private readonly ISqliteConnectionFactory _factory;

    public DbInitializer(ISqliteConnectionFactory factory) => _factory = factory;

    /// <summary>
    /// Executes the schema.sql to set up the sqlite tables if they don't exist,
    /// then applies any pending migrations.
    /// </summary>
    public void Initialize()
    {
        using var conn = _factory.CreateOpenConnection();
        var schemaPath = AppPaths.GetSchemaSqlPath();
        var sql = File.ReadAllText(schemaPath); 
        conn.Execute(sql);

        ApplyMigrations(conn);
    }

    /// <summary>
    /// Applies incremental schema migrations for columns added after initial release.
    /// Each migration is idempotent (checks before altering).
    /// </summary>
    private static void ApplyMigrations(System.Data.IDbConnection conn)
    {
        // Migration: add 'name' column to rules table (added for rule title persistence)
        var columns = conn.Query<string>("SELECT name FROM pragma_table_info('rules');").ToList();
        if (!columns.Contains("name", StringComparer.OrdinalIgnoreCase))
        {
            conn.Execute("ALTER TABLE rules ADD COLUMN name TEXT NOT NULL DEFAULT '';");
        }
    }
}