using System.Globalization;
using ExpenseTracker.Services.Contracts;
using ExpenseTracker.Services.DTOs;
using Microsoft.VisualBasic.FileIO;

namespace ExpenseTracker.Services.Services.Import.Profiles;

/// <summary>
/// Base class that enforces a consistent CSV parsing workflow while allowing profiles
/// to customize header expectations and normalized description construction.
/// </summary>
public abstract class CsvImportProfileBase : ICsvImportProfile
{
    private readonly IFingerprintService _fingerprintService;

    /// <summary>
    /// Initializes the profile base with a fingerprint service.
    /// </summary>
    protected CsvImportProfileBase(IFingerprintService fingerprintService)
    {
        _fingerprintService = fingerprintService ?? throw new ArgumentNullException(nameof(fingerprintService));
    }

    /// <inheritdoc />
    public abstract string ProfileKey { get; }

    /// <summary>Profile display name for error messages.</summary>
    protected abstract string ProfileName { get; }

    /// <summary>Expected header columns in exact order.</summary>
    protected abstract string[] ExpectedHeader { get; }

    /// <summary>Header name for date column.</summary>
    protected abstract string DateHeader { get; }

    /// <summary>Header name for description column.</summary>
    protected abstract string DescriptionHeader { get; }

    /// <summary>Header name for amount column.</summary>
    protected abstract string AmountHeader { get; }

    /// <summary>
    /// Builds the normalized description used for grouping/searching.
    /// </summary>
    protected abstract string BuildNormalizedDescription(Dictionary<string, string> row);

    /// <inheritdoc />
    public IReadOnlyList<TransactionPreviewRow> Preview(Guid accountId, string csvPath, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("CSV path cannot be null/empty.", nameof(csvPath));
        if (!File.Exists(csvPath)) throw new FileNotFoundException("CSV file not found.", csvPath);

        var results = new List<TransactionPreviewRow>();

        using var parser = new TextFieldParser(csvPath);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = false;

        var header = parser.ReadFields() ?? throw new InvalidDataException($"{ProfileName}: CSV header row missing.");
        ValidateHeaderExact(header);

        while (!parser.EndOfData)
        {
            ct.ThrowIfCancellationRequested();

            var fields = parser.ReadFields();
            if (fields is null || fields.Length == 0) continue;

            var row = ToRowDictionary(header, fields);

            var postedDate = ParseDate(row[DateHeader], ProfileName);
            var rawDescription = Require(row[DescriptionHeader], $"{ProfileName}: Description");
            var amountCents = ParseAmountToCents(row[AmountHeader], ProfileName);

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
        return results;
    }

    /// <summary>
    /// Validates the header matches the expected format exactly.
    /// </summary>
    protected virtual void ValidateHeaderExact(string[] header)
    {
        if (header.Length != ExpectedHeader.Length)
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

    /// <summary>Requires that a value is not null/empty/whitespace.</summary>
    protected static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"{name} is required.");

        return value.Trim();
    }

    /// <summary>Parses common CSV date formats into <see cref="DateOnly"/>.</summary>
    protected static DateOnly ParseDate(string raw, string profileName)
    {
        raw = Require(raw, $"{profileName}: Date");

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateOnly.FromDateTime(dt);

        throw new InvalidDataException($"{profileName}: Could not parse date '{raw}'.");
    }

    /// <summary>Parses a currency amount string into cents.</summary>
    protected static long ParseAmountToCents(string raw, string profileName)
    {
        raw = Require(raw, $"{profileName}: Amount");
        raw = raw.Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal).Trim();

        if (!decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            throw new InvalidDataException($"{profileName}: Could not parse amount '{raw}'.");

        var cents = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        return (long)cents;
    }
}