namespace ExpenseTracker.Domain;
/// <summary>
/// Defines how a financial institution represents credits and debits
/// when exporting transaction amounts.
/// </summary>
/// <remarks>
/// This enum is used during transaction imports to correctly interpret
/// the sign of monetary values based on the source institution’s convention.
/// </remarks>
public enum CreditSignConvention
{
    /// <summary>
    /// Credits (money coming in) are represented as positive values,
    /// and debits (money going out) are represented as negative values.
    /// This is the most common convention.
    /// </summary>
    CreditPositive_DebitNegative = 0,

    /// <summary>
    /// Credits (money coming in) are represented as negative values,
    /// and debits (money going out) are represented as positive values.
    /// Commonly seen in some credit card exports.
    /// </summary>
    CreditNegative_DebitPositive = 1
}
