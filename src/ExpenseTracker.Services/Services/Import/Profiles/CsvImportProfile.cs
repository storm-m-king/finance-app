using System.Globalization;
using ExpenseTracker.Domain.ImportProfile;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.Services.DTOs;
using Microsoft.VisualBasic.FileIO;

namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// Database-backed CSV import profile that is configured from a persisted <see cref="ImportProfile"/>.
/// </summary>
/// <remarks>
/// This replaces provider-specific classes like AmexCsvProfile and SofiCsvProfile.
/// Create instances using <see cref="CreateAsync"/>.
/// </remarks>
public sealed class CsvImportProfile : ICsvImportProfile
{
    private readonly IFingerprintService _fingerprintService;
    private readonly IImportProfile _profile;

    private readonly string[] _normalizedHeaders;

    /// <inheritdoc />
    public string ProfileKey => _profile.ProfileKey;

    /// <inheritdoc />
    public string ProfileName => _profile.ProfileName;

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedHeader { get; }

    /// <inheritdoc />
    public string DateHeader => _profile.DateHeader;

    /// <inheritdoc />
    public string DescriptionHeader => _profile.DescriptionHeader;

    /// <inheritdoc />
    public string AmountHeader => _profile.AmountHeader;
    public CsvImportProfile(IImportProfile profile, IFingerprintService fingerprintService)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _fingerprintService = fingerprintService ?? throw new ArgumentNullException(nameof(fingerprintService));

        // Cache parsed headers once (avoid re-splitting every row).
        ExpectedHeader = _profile.GetExpectedHeader();
        _normalizedHeaders = _profile.GetNormalizedDescriptionHeaders();

        ValidateNormalizationHeaders(ProfileName, ExpectedHeader, _normalizedHeaders);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionPreviewRow>> PreviewAsync(
        Guid accountId,
        string csvPath,
        CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("CSV path cannot be null/empty.", nameof(csvPath));
        if (!File.Exists(csvPath)) throw new FileNotFoundException("CSV file not found.", csvPath);

        var results = new List<TransactionPreviewRow>();

        var rows = await CsvParsingHelper.ReadRowsAsync(csvPath, ct);
        var header = rows.FirstOrDefault()?.Keys.ToArray() ?? throw new InvalidDataException($"{ProfileName}: CSV header row missing.");
        ValidateHeaderExact(header);

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            var postedDate = ParseDate(row[DateHeader], ProfileName);
            var rawDescription = Require(row[DescriptionHeader], $"{ProfileName}: {DescriptionHeader}");

            var amountCents = 0L;
            try
            {
                // TODO: Handle this better. If a csv is missing amount, give an alternate amount column to parse.
                // TODO: Example: CapitalOne CSV has Credit and Debit columns separate (right now we only use credit).
                amountCents = ParseAmountToCents(row[AmountHeader], ProfileName);
            }
            catch (Exception)
            {
                continue; // Not all csv's have amount filled for the 'amount' column. 
            }

            var normalized = BuildNormalizedDescription(row);

            var fingerprint = _fingerprintService.Compute(accountId, postedDate, rawDescription, amountCents);

            results.Add(new TransactionPreviewRow(
                TransactionId: Guid.NewGuid(),
                AccountId: accountId,
                PostedDate: postedDate,
                RawDescription: rawDescription,
                AmountCents: amountCents,
                NormalizedDescription: normalized,
                Fingerprint: fingerprint
            ));
        }

        // No awaits inside; keep signature async for interface consistency / future.
        await Task.CompletedTask;
        return results;
    }

    private string BuildNormalizedDescription(IDictionary<string, string> row)
    {
        var parts = _normalizedHeaders.Select(h => row[h]);
        return string.Join(_profile.NormalizedDescriptionDelimiter, parts);
    }

    private void ValidateHeaderExact(string[] header)
    {
        if (header.Length != ExpectedHeader.Count)
            throw new InvalidDataException($"{ProfileName}: CSV header does not match expected format.");

        for (var i = 0; i < header.Length; i++)
        {
            var actual = (header[i] ?? string.Empty).Trim();
            var expected = ExpectedHeader[i];

            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"{ProfileName}: CSV header mismatch at column {i + 1}. Expected '{expected}', got '{actual}'.");
            }
        }
    }

    private static void ValidateNormalizationHeaders(string profileName, IReadOnlyList<string> expectedHeader, string[] normalizedHeaders)
    {
        var expected = new HashSet<string>(expectedHeader, StringComparer.Ordinal);

        foreach (var h in normalizedHeaders)
        {
            if (!expected.Contains(h))
            {
                throw new InvalidOperationException(
                    $"{profileName}: NormalizedDescriptionCsv references header '{h}', but it is not present in ExpectedHeaderCsv.");
            }
        }
    }

    private static Dictionary<string, string> ToRowDictionary(string[] header, string[] fields)
    {
        var row = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var i = 0; i < header.Length; i++)
        {
            var value = i < fields.Length ? fields[i] : string.Empty;
            row[header[i]] = value ?? string.Empty;
        }

        return row;
    }

    private static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"{name} is required.");

        return value.Trim();
    }

    private static DateOnly ParseDate(string raw, string profileName)
    {
        raw = Require(raw, $"{profileName}: Date");

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateOnly.FromDateTime(dt);

        throw new InvalidDataException($"{profileName}: Could not parse date '{raw}'.");
    }

    private static long ParseAmountToCents(string raw, string profileName)
    {
        raw = Require(raw, $"{profileName}: Amount");
        raw = raw.Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal).Trim();

        if (!decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            throw new InvalidDataException($"{profileName}: Could not parse amount '{raw}'.");

        var cents = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        return (long)cents;
    }
}
