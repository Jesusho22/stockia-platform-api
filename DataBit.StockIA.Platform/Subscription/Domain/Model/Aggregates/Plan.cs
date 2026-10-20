namespace DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;

/// <summary>
///     A plan of the catalog (Esencial, Profesional, IoT Completo).
/// </summary>
public class Plan(string name, decimal monthlyPrice, List<string> features, bool highlighted)
{
    public Plan() : this(string.Empty, 0, [], false)
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = name;

    /// <summary>Price in soles (PEN).</summary>
    public decimal MonthlyPrice { get; private set; } = monthlyPrice;

    public List<string> Features { get; private set; } = features;
    public bool Highlighted { get; private set; } = highlighted;

    public bool IsFree => MonthlyPrice == 0;
}
