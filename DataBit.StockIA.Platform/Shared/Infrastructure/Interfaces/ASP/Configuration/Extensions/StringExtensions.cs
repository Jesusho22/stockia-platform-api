using System.Text.RegularExpressions;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Interfaces.ASP.Configuration.Extensions;

public static partial class StringExtensions
{
    /// <summary>"InventoryItems" → "inventory-items".</summary>
    public static string ToKebabCase(this string text) =>
        string.IsNullOrEmpty(text) ? text : KebabBoundary().Replace(text, "$1-$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex KebabBoundary();
}
