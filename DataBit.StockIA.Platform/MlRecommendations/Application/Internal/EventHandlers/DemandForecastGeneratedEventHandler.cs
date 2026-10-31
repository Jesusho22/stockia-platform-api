using DataBit.StockIA.Platform.MlRecommendations.Application.Services;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Commands;
using DataBit.StockIA.Platform.MlRecommendations.Domain.Model.Events;
using DataBit.StockIA.Platform.Shared.Application.Internal.EventHandlers;

namespace DataBit.StockIA.Platform.MlRecommendations.Application.Internal.EventHandlers;

/// <summary>Every new forecast refreshes the purchase and menu recommendations.</summary>
public class DemandForecastGeneratedEventHandler(IRecommendationCommandService recommendationCommandService)
    : IEventHandler<DemandForecastGeneratedEvent>
{
    public Task Handle(DemandForecastGeneratedEvent domainEvent, CancellationToken cancellationToken) =>
        recommendationCommandService.Handle(new GenerateRecommendationsCommand(domainEvent.RestaurantId, domainEvent.DemandForecastId),
            cancellationToken);
}
