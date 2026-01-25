using System.IO;
using Dapper;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Initialize the Database, ensuring the schema is setup.
/// </summary>
public sealed class DbInitializer
{
    private readonly ISqliteConnectionFactory _factory;

    public DbInitializer(ISqliteConnectionFactory factory) => _factory = factory;

    /// <summary>
    /// Executes the schema.sql to set up the sqlite tables if they don't exist.
    /// </summary>
    public void Initialize()
    {
        using var conn = _factory.CreateOpenConnection();
        var schemaPath = AppPaths.GetSchemaSqlPath();
        var sql = File.ReadAllText(schemaPath); 
        conn.Execute(sql);
    }
}