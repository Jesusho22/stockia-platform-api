using System.Globalization;
using System.Numerics;
using System.Text;

namespace DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>
///     Exact stock quantity with up to 32 decimals (e.g. 0.125 kg of tomato).
/// </summary>
/// <remarks>
///     <c>decimal</c> keeps 28-29 significant digits and MySQL DECIMAL keeps 30
///     decimals at most, so the Web Application's 32-decimal quantities are
///     stored as a fixed-point integer scaled by 10^32. It is persisted as text
///     (see QuantityConversionExtensions) and serialized as a JSON string;
///     JSON numbers (also in exponent form, e.g. 1e-7) are accepted on input.
/// </remarks>
public readonly struct Quantity : IEquatable<Quantity>, IComparable<Quantity>
{
    public const int MaxDecimals = 32;
    private static readonly BigInteger Scale = BigInteger.Pow(10, MaxDecimals);

    private Quantity(BigInteger scaled)
    {
        Scaled = scaled;
    }

    /// <summary>Value multiplied by 10^32.</summary>
    public BigInteger Scaled { get; }

    public static Quantity Zero => new(BigInteger.Zero);
    public bool IsZero => Scaled.IsZero;
    public bool IsPositive => Scaled.Sign > 0;
    public bool IsNegative => Scaled.Sign < 0;

    public static Quantity FromInt(long value) => new(value * Scale);

    public static Quantity FromDecimal(decimal value) => Parse(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Parses "12", "0.125", "0,125" or "1.5e-3". Throws <see cref="FormatException" />
    ///     when the text is not a number or has more than 32 decimals.
    /// </summary>
    public static Quantity Parse(string text)
    {
        if (TryParse(text, out var quantity, out var error)) return quantity;
        throw new FormatException(error);
    }

    public static bool TryParse(string? text, out Quantity quantity) => TryParse(text, out quantity, out _);

    public static bool TryParse(string? text, out Quantity quantity, out string error)
    {
        quantity = Zero;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "The quantity is empty.";
            return false;
        }

        var value = text.Trim().Replace(',', '.');
        var exponent = 0;
        var exponentIndex = value.IndexOfAny(['e', 'E']);
        if (exponentIndex >= 0)
        {
            if (!int.TryParse(value[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
            {
                error = $"'{text}' is not a valid quantity.";
                return false;
            }

            value = value[..exponentIndex];
        }

        var negative = value.StartsWith('-');
        if (negative || value.StartsWith('+')) value = value[1..];
        var parts = value.Split('.');
        if (parts.Length > 2 || parts.Any(part => part.Any(character => !char.IsAsciiDigit(character))) ||
            parts.All(part => part.Length == 0))
        {
            error = $"'{text}' is not a valid quantity.";
            return false;
        }

        var digits = parts[0] + (parts.Length == 2 ? parts[1] : string.Empty);
        var decimals = (parts.Length == 2 ? parts[1].Length : 0) - exponent;
        if (decimals < 0)
        {
            digits += new string('0', -decimals);
            decimals = 0;
        }

        // Trailing zeros beyond the 32nd decimal do not change the value.
        while (decimals > MaxDecimals && digits.EndsWith('0'))
        {
            digits = digits[..^1];
            decimals--;
        }

        if (decimals > MaxDecimals)
        {
            error = $"'{text}' has more than {MaxDecimals} decimals.";
            return false;
        }

        var mantissa = digits.Length == 0 ? BigInteger.Zero : BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        var scaled = mantissa * BigInteger.Pow(10, MaxDecimals - decimals);
        quantity = new Quantity(negative ? -scaled : scaled);
        return true;
    }

    /// <summary>Canonical text without exponent or trailing zeros, e.g. "0.125".</summary>
    public override string ToString()
    {
        var absolute = BigInteger.Abs(Scaled);
        var integer = BigInteger.DivRem(absolute, Scale, out var fraction);
        var builder = new StringBuilder();
        if (Scaled.Sign < 0) builder.Append('-');
        builder.Append(integer.ToString(CultureInfo.InvariantCulture));
        if (!fraction.IsZero)
        {
            var fractionText = fraction.ToString(CultureInfo.InvariantCulture).PadLeft(MaxDecimals, '0').TrimEnd('0');
            builder.Append('.').Append(fractionText);
        }

        return builder.ToString();
    }

    /// <summary>Closest <c>decimal</c> (for money: quantity × unit cost).</summary>
    public decimal ToDecimal()
    {
        var text = ToString();
        var point = text.IndexOf('.');
        if (point >= 0)
        {
            var integerDigits = point - (text.StartsWith('-') ? 1 : 0);
            var keep = Math.Max(0, 27 - integerDigits);
            if (text.Length - point - 1 > keep) text = text[..(point + 1 + keep)];
            if (text.EndsWith('.')) text = text[..^1];
        }

        return decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public double ToDouble() => double.Parse(ToString(), CultureInfo.InvariantCulture);

    public static Quantity operator +(Quantity left, Quantity right) => new(left.Scaled + right.Scaled);
    public static Quantity operator -(Quantity left, Quantity right) => new(left.Scaled - right.Scaled);
    public static Quantity operator *(Quantity quantity, int times) => new(quantity.Scaled * times);
    public static bool operator ==(Quantity left, Quantity right) => left.Equals(right);
    public static bool operator !=(Quantity left, Quantity right) => !left.Equals(right);
    public static bool operator <(Quantity left, Quantity right) => left.Scaled < right.Scaled;
    public static bool operator >(Quantity left, Quantity right) => left.Scaled > right.Scaled;
    public static bool operator <=(Quantity left, Quantity right) => left.Scaled <= right.Scaled;
    public static bool operator >=(Quantity left, Quantity right) => left.Scaled >= right.Scaled;

    public static Quantity Min(Quantity left, Quantity right) => left <= right ? left : right;
    public static Quantity Max(Quantity left, Quantity right) => left >= right ? left : right;

    public bool Equals(Quantity other) => Scaled.Equals(other.Scaled);
    public override bool Equals(object? obj) => obj is Quantity other && Equals(other);
    public override int GetHashCode() => Scaled.GetHashCode();
    public int CompareTo(Quantity other) => Scaled.CompareTo(other.Scaled);
}
