using Dapper;
using ExpenseTracker.Infrastructure.Logging;

namespace ExpenseTracker.Infrastructure.Persistence.Seed;

/// <summary>
/// Responsible for seeding the database with default values.
/// </summary>
public sealed class SystemSeeder
{
    private readonly ISqliteConnectionFactory _factory;
    
    private static readonly Guid UncategorizedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TransferId      = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public SystemSeeder(ISqliteConnectionFactory factory) => _factory = factory;

    /// <summary>
    /// Seeds the database with default categories.
    /// </summary>
    public void Seed()
    {
        using var conn = _factory.CreateOpenConnection();
        int rowsEffected = conn.Execute("""
                                          INSERT OR IGNORE INTO Categories (id, name, is_system, is_user_editable)
                                          VALUES (@Id, @Name, 1, 0);
                                        """, new[]
        {
            new { Id = UncategorizedId.ToString(), Name = "Uncategorized" },
            new { Id = TransferId.ToString(), Name = "Transfer" }
        });
                
        AppLogger.Info($"Applied System Seeder. Rows effected={rowsEffected}");
    }
}