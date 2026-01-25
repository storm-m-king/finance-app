using System.IO;
using Dapper;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;

namespace ExpenseTracker.Infrastructure.Persistence;

public sealed class DbInitializer
{
    private readonly ISqliteConnectionFactory _factory;

    public DbInitializer(ISqliteConnectionFactory factory) => _factory = factory;

    public void Initialize()
    {
        using var conn = _factory.CreateOpenConnection();
        var schemaPath = AppPaths.GetSchemaSqlPath();
        var sql = File.ReadAllText(schemaPath); 
        conn.Execute(sql);
    }
}