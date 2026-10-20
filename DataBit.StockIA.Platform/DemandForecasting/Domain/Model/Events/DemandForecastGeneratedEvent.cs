using DataBit.StockIA.Platform.Shared.Domain.Model.Events;

namespace DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Events;

/// <summary>Alerts &amp; Recommendations reacts creating purchase and menu recommendations.</summary>
public record DemandForecastGeneratedEvent(int RestaurantId, int DemandForecastId) : IEvent;
