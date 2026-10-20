using DataBit.StockIA.Platform.Alerts.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Alerts.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Alerts.Domain.Services;

namespace DataBit.StockIA.Platform.Tests.Alerts;

public class AlertRulesTests
{
    [Fact]
    public void A_critical_alert_needs_whatsapp_and_email_before_it_is_attended()
    {
        var alert = new Alert(1, AlertType.CriticalStock, AlertSeverity.Critical, "Lomo fino sin stock disponible.", AlertChannel.Whatsapp,
            DateTimeOffset.UtcNow);
        alert.RegisterDelivery(AlertChannel.Whatsapp);
        Assert.Equal(AlertChannel.Email, alert.PendingChannel);
        Assert.False(alert.Acknowledge());

        alert.RegisterDelivery(AlertChannel.Email);
        Assert.True(alert.Delivered);
        Assert.True(alert.Acknowledge());
        Assert.Equal(new[] { "WHATSAPP", "EMAIL" }, alert.DeliveredChannels);
    }

    [Fact]
    public void Policy_proposes_the_right_alerts()
    {
        var low = InventoryAlertPolicy.Evaluate(new StockSnapshot(1, "Queso mozzarella", "kg", 4, 6, 8, "LOW"));
        Assert.Equal(AlertType.LowStock, Assert.Single(low).Type);

        var empty = InventoryAlertPolicy.Evaluate(new StockSnapshot(2, "Lomo fino", "kg", 0, 5, 20, "CRITICAL"));
        Assert.Equal(AlertSeverity.Critical, Assert.Single(empty).Severity);

        var expiring = InventoryAlertPolicy.Evaluate(new StockSnapshot(3, "Tomate", "kg", 9, 8, 1, "AVAILABLE"));
        Assert.Equal("Tomate vence mañana.", Assert.Single(expiring).Message);

        var expired = InventoryAlertPolicy.Evaluate(new StockSnapshot(4, "Pechuga de pollo", "kg", 18, 10, -3, "EXPIRED"));
        Assert.Equal(AlertType.ExpiringSoon, Assert.Single(expired).Type);
    }

    [Fact]
    public void Recommendations_suggest_buying_what_the_forecast_needs()
    {
        var dishes = new List<DishPlan>
        {
            new("Lomo Saltado", 200, 150, [new IngredientNeed("lomo fino|kg", "Lomo fino", "kg", 0.25m)]),
            new("Pizza Margarita", 100, 160, [new IngredientNeed("harina de trigo|kg", "Harina de trigo", "kg", 0.3m)])
        };
        var stock = new List<ProductStock> { new("lomo fino|kg", "Lomo fino", "kg", 10), new("harina de trigo|kg", "Harina de trigo", "kg", 32) };
        var proposals = RecommendationPolicy.Propose(dishes, stock, new DateOnly(2026, 10, 26));

        var purchase = Assert.Single(proposals, proposal => proposal.Type == RecommendationType.PurchaseSuggestion);
        Assert.StartsWith("Comprar 40 kg de Lomo fino", purchase.Message);
        Assert.Equal(2, proposals.Count(proposal => proposal.Type == RecommendationType.MenuAdjustment));
    }
}
