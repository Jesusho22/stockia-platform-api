using DataBit.StockIA.Platform.Subscription.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Commands;
using DataBit.StockIA.Platform.Shared.Application.Model;

namespace DataBit.StockIA.Platform.Subscription.Application.CommandServices;

public interface ISubscriptionCommandService
{
    Task<Result<RestaurantSubscription>> Handle(CreateDefaultSubscriptionCommand command, CancellationToken cancellationToken);
    Task<Result<RestaurantSubscription>> Handle(ChangePlanCommand command, CancellationToken cancellationToken);
}
