using ExpenseTracker.Services.Contracts;
namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// CSV import profile for SoFi exports.
/// </summary>
public sealed class SofiCsvProfile : CsvImportProfileBase
{
    /// <summary>
    /// Creates a SoFi profile.
    /// </summary>
    public SofiCsvProfile(IFingerprintService fingerprintService) : base(fingerprintService) { }

    /// <inheritdoc />
    public override string ProfileKey => "sofi.v1";

    protected override string ProfileName => "SoFi";

    protected override string[] ExpectedHeader => new[]
    {
        "Date", "Description", "Type", "Amount", "Current balance", "Status"
    };

    protected override string DateHeader => "Date";
    protected override string DescriptionHeader => "Description";
    protected override string AmountHeader => "Amount";

    /// <inheritdoc />
    protected override string BuildNormalizedDescription(Dictionary<string, string> row)
    {
        var description = Require(row["Description"], "SoFi: Description");
        var type = Require(row["Type"], "SoFi: Type");
        var balance = Require(row["Current balance"], "SoFi: Current balance");
        var status = Require(row["Status"], "SoFi: Status");

        return $"{description},{type},{balance},{status}";
    }
}
