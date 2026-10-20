using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Serialization;

/// <summary>
///     Reads a quantity from a JSON number (its raw text, so no digit is lost
///     to double precision) or string, and always writes a string: JavaScript
///     numbers cannot hold 32 decimals.
/// </summary>
public class QuantityJsonConverter : JsonConverter<Quantity>
{
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType switch
        {
            JsonTokenType.Number => Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan),
            JsonTokenType.String => reader.GetString(),
            _ => throw new JsonException("A quantity must be a number or a string.")
        };
        if (!Quantity.TryParse(text, out var quantity, out var error)) throw new JsonException(error);
        return quantity;
    }

    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
