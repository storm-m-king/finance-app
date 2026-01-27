using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Services.Services.FingerPrint;

public class Sha256FingerprintService : IFingerprintService
{
    public string Compute(Guid accountId, DateOnly postedDate, string rawDescription, long amountCents)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
            throw new ArgumentException("RawDescription is required.", nameof(rawDescription));

        // Normalize the inputs so equivalent rows hash the same way.
        var key = $"{accountId}{postedDate:yyyy-MM-dd}|{rawDescription.Trim()}|{amountCents}";

        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);

        // 64 hex chars
        return Convert.ToHexString(hash);
    }
}