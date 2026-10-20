using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Conversions MySql.EntityFrameworkCore needs for StockIA types.
/// </summary>
public static class ValueConversionExtensions
{
    /// <summary>
    ///     MySQL DECIMAL keeps at most 30 decimals, so 32-decimal quantities are
    ///     stored as their canonical text (e.g. "0.125").
    /// </summary>
    public static PropertyBuilder<Quantity> HasQuantityConversion(this PropertyBuilder<Quantity> builder)
    {
        return builder.HasConversion(quantity => quantity.ToString(), text => Quantity.Parse(text))
            .HasColumnType("varchar(80)")
            .IsRequired();
    }

    /// <summary>MySql.EntityFrameworkCore reads "date" columns as DateTime.</summary>
    public static PropertyBuilder<DateOnly> HasDateOnlyConversion(this PropertyBuilder<DateOnly> builder)
    {
        return builder.HasConversion(date => date.ToDateTime(TimeOnly.MinValue), date => DateOnly.FromDateTime(date))
            .HasColumnType("date");
    }

    /// <summary>A short list of strings stored as a JSON array in one text column.</summary>
    public static PropertyBuilder<List<string>> HasStringListConversion(this PropertyBuilder<List<string>> builder)
    {
        var comparer = new ValueComparer<List<string>>(
            (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => list.ToList());
        builder.HasConversion(
                list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("text");
        builder.Metadata.SetValueComparer(comparer);
        return builder;
    }
}
