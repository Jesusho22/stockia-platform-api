using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;

public interface ISubscriptionCommandService
{
    Task<Result<RestaurantSubscription>> Handle(CreateDefaultSubscriptionCommand command, CancellationToken cancellationToken);
    Task<Result<RestaurantSubscription>> Handle(ChangePlanCommand command, CancellationToken cancellationToken);
}
