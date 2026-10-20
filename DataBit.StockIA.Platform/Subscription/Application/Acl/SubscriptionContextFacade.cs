using DataBit.StockIA.Platform.Subscription.Application.CommandServices;
using DataBit.StockIA.Platform.Subscription.Domain.Model.Commands;
using DataBit.StockIA.Platform.Subscription.Interfaces.Acl;

namespace DataBit.StockIA.Platform.Subscription.Application.Acl;

public class SubscriptionContextFacade(ISubscriptionCommandService subscriptionCommandService) : ISubscriptionContextFacade
{
    public async Task<int> CreateDefaultSubscription(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await subscriptionCommandService.Handle(new CreateDefaultSubscriptionCommand(restaurantId), cancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Message);
        return result.Value!.Id;
    }
}
