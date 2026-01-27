using ExpenseTracker.Services.Contracts;
namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// CSV import profile for American Express exports.
/// </summary>
public sealed class AmexCsvProfile : CsvImportProfileBase
{
    /// <summary>
    /// Creates an Amex profile.
    /// </summary>
    public AmexCsvProfile(IFingerprintService fingerprintService) : base(fingerprintService) { }

    /// <inheritdoc />
    public override string ProfileKey => "amex.v1";

    protected override string ProfileName => "Amex";

    protected override string[] ExpectedHeader => new[]
    {
        "Date", "Description", "Card Member", "Account #", "Amount"
    };

    protected override string DateHeader => "Date";
    protected override string DescriptionHeader => "Description";
    protected override string AmountHeader => "Amount";

    /// <inheritdoc />
    protected override string BuildNormalizedDescription(Dictionary<string, string> row)
    {
        var description = Require(row["Description"], "Amex: Description");
        var cardMember = Require(row["Card Member"], "Amex: Card Member");
        var accountNo = Require(row["Account #"], "Amex: Account #");

        return $"{description},{cardMember},{accountNo}";
    }
}