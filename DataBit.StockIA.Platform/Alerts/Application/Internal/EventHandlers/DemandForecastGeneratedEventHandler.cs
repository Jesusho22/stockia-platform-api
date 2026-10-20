using DataBit.StockIA.Platform.Alerts.Application.CommandServices;
using DataBit.StockIA.Platform.Alerts.Domain.Model.Commands;
using DataBit.StockIA.Platform.DemandForecasting.Domain.Model.Events;
using DataBit.StockIA.Platform.Shared.Application.Internal.EventHandlers;

namespace DataBit.StockIA.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>Every new forecast refreshes the purchase and menu recommendations.</summary>
public class DemandForecastGeneratedEventHandler(IRecommendationCommandService recommendationCommandService)
    : IEventHandler<DemandForecastGeneratedEvent>
{
    public Task Handle(DemandForecastGeneratedEvent domainEvent, CancellationToken cancellationToken) =>
        recommendationCommandService.Handle(new GenerateRecommendationsCommand(domainEvent.RestaurantId, domainEvent.DemandForecastId),
            cancellationToken);
}
