using System.Data;
using Microsoft.Data.Sqlite;
using ExpenseTracker.Infrastructure.Configuration;
using ExpenseTracker.Infrastructure.Logging;

namespace ExpenseTracker.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    // <inheritdoc>
    public IDbConnection CreateOpenConnection()
    {
        var dir = AppPaths.GetAppDataDirectory();
        System.IO.Directory.CreateDirectory(dir);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = AppPaths.GetDatabasePath(),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var conn = new SqliteConnection(cs);
        return conn;
    }
}