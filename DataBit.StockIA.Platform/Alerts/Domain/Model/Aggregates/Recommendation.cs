using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;

/// <summary>
///     Automatic suggestion to buy supplies or adjust the menu, generated from
///     the demand forecast and the inventory.
/// </summary>
public class Recommendation : IAuditableEntity
{
    private Recommendation()
    {
    }

    public Recommendation(int restaurantId, RecommendationType type, string message, string expectedImpact, int? demandForecastId)
    {
        RestaurantId = restaurantId;
        Type = type;
        Message = message;
        ExpectedImpact = expectedImpact;
        DemandForecastId = demandForecastId;
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public RecommendationType Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string ExpectedImpact { get; private set; } = string.Empty;
    public bool Applied { get; private set; }
    public int? DemandForecastId { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Recommendation Apply()
    {
        Applied = true;
        return this;
    }
}
