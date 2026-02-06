using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ExpenseTracker.Services.Services.Import;

internal static class CsvParsingHelper
{
    public static async Task<IReadOnlyList<IDictionary<string, string>>> ReadRowsAsync(
        string filePath,
        CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,        
            MissingFieldFound = null    
        };

        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        var rows = new List<IDictionary<string, string>>();

        if (!await csv.ReadAsync().ConfigureAwait(false))
            return rows;

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                // GetField reads the parsed field (handles quoted commas, unquoted values, etc.)
                var value = csv.GetField(header) ?? string.Empty;
                record[header] = value;
            }

            rows.Add(record);
        }

        return rows;
    }
}
