using System.Data;

namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Factory for retrieving an open connection to the database.
/// </summary>
public interface ISqliteConnectionFactory
{
    /// <summary>
    /// Creates an open connection to the database.
    /// </summary>
    /// <returns>An open connection to the database.</returns>
    IDbConnection CreateOpenConnection();
}