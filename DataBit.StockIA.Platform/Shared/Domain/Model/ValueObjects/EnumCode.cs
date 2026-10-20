using System.Text.RegularExpressions;

namespace DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>
///     Converts enum members (PascalCase in C#) to the UPPER_SNAKE codes used by
///     the Web Application (e.g. StorageType.Refrigerated ⇄ "REFRIGERATED",
///     AlertType.LowStock ⇄ "LOW_STOCK").
/// </summary>
public static partial class EnumCode
{
    public static string ToCode<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        PascalBoundary().Replace(value.ToString(), "$1_$2").ToUpperInvariant();

    public static bool TryParse<TEnum>(string? code, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Enum.TryParse(code.Replace("_", string.Empty), ignoreCase: true, out value) && Enum.IsDefined(value);
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex PascalBoundary();
}
