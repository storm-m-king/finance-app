using System.Data;
using Dapper;
using ExpenseTracker.Infrastructure.Logging;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Persistence.Seed;

/// <summary>
/// Responsible for seeding the database with default system data.
/// </summary>
public sealed class SystemSeeder
{
    // System category IDs (stable, well-known)
    private static readonly Guid UncategorizedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TransferId      = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // Seeded account IDs (stable, so imports/rules can rely on them)
    private static readonly Guid AmexAccountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SofiAccountId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    private readonly IAppLogger _appLogger;
    private readonly ISqliteConnectionFactory _factory;

    public SystemSeeder(IAppLogger appLogger, ISqliteConnectionFactory factory)
    {
        _appLogger = appLogger;
        _factory = factory;
    }

    /// <summary>
    /// Seeds the database with required system data.
    /// Safe to run multiple times.
    /// </summary>
    public void Seed()
    {
        using var conn = _factory.CreateOpenConnection();

        _appLogger.Info("Seeding system");
        _appLogger.Info($"Seeding categories... Rows effected: {SeedCategories(conn)}");
        _appLogger.Info($"Seeding import profiles... Rows effected: {SeedImportProfiles(conn)}");
        _appLogger.Info($"Seeding Accounts... Rows effected: {SeedAccounts(conn)}");
        _appLogger.Info("SystemSeeder completed successfully.");
    }

    private static int SeedCategories(IDbConnection conn)
    {
        return conn.Execute(
            """
            INSERT OR IGNORE INTO categories (id, name, is_system, is_user_editable)
            VALUES (@Id, @Name, 1, 0);
            """,
            new[]
            {
                new { Id = UncategorizedId.ToString(), Name = "Uncategorized" },
                new { Id = TransferId.ToString(),      Name = "Transfer" }
            });
    }

    private static int SeedImportProfiles(IDbConnection conn)
    {
        return conn.Execute(
            """
            INSERT OR IGNORE INTO import_profiles
                (profile_key, profile_name, expected_header_csv, date_header, description_header, amount_header, normalized_description_csv)
            VALUES
                (@ProfileKey, @ProfileName, @ExpectedHeaderCsv, @DateHeader, @DescriptionHeader, @AmountHeader, @NormalizedDescriptionHeader);
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
                    AmountHeader = "Amount",
                    NormalizedDescriptionHeader = "Description,Card Member,Account #"
                },
                new
                {
                    ProfileKey = "sofi.v1",
                    ProfileName = "SoFi",
                    ExpectedHeaderCsv = "Date,Description,Type,Amount,Current balance,Status",
                    DateHeader = "Date",
                    DescriptionHeader = "Description",
                    AmountHeader = "Amount",
                    NormalizedDescriptionHeader = "Description,Type,Current balance,Status",
                }
            });
    }

    private static int SeedAccounts(IDbConnection conn)
    {
        return conn.Execute(
            """
            INSERT OR IGNORE INTO accounts
                (id, name, type, is_archived, credit_sign_convention, import_profile_key)
            VALUES
                (@Id, @Name, @Type, 0, @CreditSignConvention, @ImportProfileKey);
            """,
            new[]
            {
                new
                {
                    Id = AmexAccountId.ToString(),
                    Name = "American Express",
                    Type = 1, // Credit
                    CreditSignConvention = 2, // CreditNegative_DebitPositive
                    ImportProfileKey = "amex.v1"
                },
                new
                {
                    Id = SofiAccountId.ToString(),
                    Name = "SoFi Checking",
                    Type = 0, // Checking
                    CreditSignConvention = 1, // CreditPositive_DebitNegative
                    ImportProfileKey = "sofi.v1"
                }
            });
    }
}