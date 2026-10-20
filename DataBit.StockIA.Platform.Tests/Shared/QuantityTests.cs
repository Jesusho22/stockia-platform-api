using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Tests.Shared;

public class QuantityTests
{
    [Theory]
    [InlineData("18", "18")]
    [InlineData("0.125", "0.125")]
    [InlineData("0,125", "0.125")]
    [InlineData("12.50000", "12.5")]
    [InlineData("1e-7", "0.0000001")]
    [InlineData("1.5E3", "1500")]
    [InlineData("1.12345678901234567890123456789012", "1.12345678901234567890123456789012")]
    public void Parses_and_prints_the_canonical_text(string text, string expected)
    {
        Assert.Equal(expected, Quantity.Parse(text).ToString());
    }

    [Theory]
    [InlineData("0.123456789012345678901234567890123")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.2.3")]
    public void Rejects_invalid_text_or_more_than_32_decimals(string text)
    {
        Assert.False(Quantity.TryParse(text, out _));
    }

    [Fact]
    public void Keeps_every_decimal_in_arithmetic()
    {
        var stock = Quantity.Parse("1.12345678901234567890123456789012");
        var used = Quantity.Parse("0.18");
        Assert.Equal("0.94345678901234567890123456789012", (stock - used).ToString());
        Assert.Equal("0.54", (used * 3).ToString());
        Assert.True(used < stock);
    }

    [Fact]
    public void Converts_to_decimal_for_money()
    {
        Assert.Equal(2.61m, Quantity.Parse("0.18").ToDecimal() * 14.5m);
    }

    [Fact]
    public void EnumCode_uses_upper_snake_case()
    {
        Assert.Equal("LOW_STOCK", DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects.AlertType.LowStock.ToCode());
        Assert.True(EnumCode.TryParse<DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects.AlertType>("CRITICAL_STOCK", out var type));
        Assert.Equal(DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects.AlertType.CriticalStock, type);
    }
}
