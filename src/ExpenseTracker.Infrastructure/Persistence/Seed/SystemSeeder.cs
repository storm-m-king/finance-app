using Dapper;
using ExpenseTracker.Infrastructure.Logging;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Persistence.Seed;

/// <summary>
/// Responsible for seeding the database with default values.
/// </summary>
public sealed class SystemSeeder
{
    private static readonly Guid UncategorizedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TransferId      = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly IAppLogger _appLogger;
    private readonly ISqliteConnectionFactory _factory;

    public SystemSeeder(IAppLogger appLogger, ISqliteConnectionFactory factory)
    {
        _appLogger = appLogger;
        _factory = factory;
    }

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
                
        _appLogger.Info($"Applied System Seeder. Rows effected={rowsEffected}");

        int rowsAffected = conn.Execute("""
                                        INSERT OR IGNORE INTO import_profiles
                                            (profile_key, profile_name, expected_header_csv, date_header, description_header, amount_header)
                                        VALUES
                                            (@ProfileKey, @ProfileName, @ExpectedHeaderCsv, @DateHeader, @DescriptionHeader, @AmountHeader);
                                        """,
            new[]
            {
                new
                {
                    ProfileKey = "amex.v1",
                    ProfileName = "American Express",
                    ExpectedHeaderCsv = "Date,Description,Card Member,Account #,Amount",
                    DateHeader = "Date",
                    DescriptionHeader = "Description",
                    AmountHeader = "Amount"
                },
                new
                {
                    ProfileKey = "sofi.v1",
                    ProfileName = "SoFi",
                    ExpectedHeaderCsv = "Date,Description,Type,Amount,Current balance,Status",
                    DateHeader = "Date",
                    DescriptionHeader = "Description",
                    AmountHeader = "Amount"
                }
            });
    }
}