using FinTechAPI.Application.Utilities;

namespace FinTechAPI.Tests.Utilities;

public class AmountConverterTests
{
    [Theory]
    [InlineData("10.99", 1099)]
    [InlineData("0.01", 1)]
    [InlineData("1.00", 100)]
    [InlineData("999.99", 99999)]
    [InlineData("0.005", 1)]   // rounds half-away-from-zero
    [InlineData("0.004", 0)]   // rounds down
    public void ToMinorUnits_RoundsCorrectly(string amountStr, long expected)
    {
        var amount = decimal.Parse(amountStr, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, AmountConverter.ToMinorUnits(amount));
    }

    [Fact]
    public void ToMinorUnits_ZeroAmount_ReturnsZero()
    {
        Assert.Equal(0L, AmountConverter.ToMinorUnits(0m));
    }

    [Theory]
    [InlineData(1099, "10.99")]
    [InlineData(1, "0.01")]
    [InlineData(100, "1.00")]
    [InlineData(99999, "999.99")]
    public void FromMinorUnits_ReturnsCorrectDecimal(long minorUnits, string expectedStr)
    {
        var expected = decimal.Parse(expectedStr, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, AmountConverter.FromMinorUnits(minorUnits));
    }

    [Theory]
    [InlineData("10.99")]
    [InlineData("1.00")]
    [InlineData("0.50")]
    [InlineData("250.00")]
    public void RoundTrip_IsLossless(string amountStr)
    {
        var original = decimal.Parse(amountStr, System.Globalization.CultureInfo.InvariantCulture);
        var roundTripped = AmountConverter.FromMinorUnits(AmountConverter.ToMinorUnits(original));
        Assert.Equal(original, roundTripped);
    }
}
