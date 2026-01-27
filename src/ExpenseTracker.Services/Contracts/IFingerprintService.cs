namespace ExpenseTracker.Services.Contracts;

public interface IFingerprintService
{
    string Compute(Guid accountId, DateOnly postedDate, string rawDescription, long amountCents);
}