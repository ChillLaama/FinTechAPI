namespace FinTechAPI.Application.Utilities;

/// <summary>
/// Converts monetary amounts between decimal representation and minor currency units (e.g. cents).
/// Storing amounts as integer minor units in Firestore avoids floating-point precision loss.
/// </summary>
public static class AmountConverter
{
    /// <summary>
    /// Converts a decimal amount to integer minor units (e.g. 10.99 USD → 1099 cents).
    /// </summary>
    public static long ToMinorUnits(decimal amount) =>
        Convert.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Converts integer minor units back to a decimal amount (e.g. 1099 cents → 10.99 USD).
    /// </summary>
    public static decimal FromMinorUnits(long minorUnits) => minorUnits / 100m;
}
