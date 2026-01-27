namespace ExpenseTracker.Services.DTOs;

public sealed record TransactionPreviewRow(
    Guid TransactionId,
    Guid AccountId,
    DateOnly PostedDate,
    string RawDescription,
    long AmountCents,
    string NormalizedDescription,
    string Fingerprint
);